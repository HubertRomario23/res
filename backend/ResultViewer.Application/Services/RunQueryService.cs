using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ResultViewer.Application.DTOs;
using ResultViewer.Application.Interfaces;
using ResultViewer.Application.Mappings;
using ResultViewer.Domain.Entities;
using ResultViewer.Domain.Interfaces;

namespace ResultViewer.Application.Services;

public sealed class RunQueryService : IRunQueryService
{
    private readonly ITestRunRepository _repository;
    private readonly IFileSystemService _fileSystem;
    private readonly IXmlParserService _xmlParser;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RunQueryService> _logger;

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        SlidingExpiration = TimeSpan.FromMinutes(2)
    };

    public RunQueryService(
        ITestRunRepository repository,
        IFileSystemService fileSystem,
        IXmlParserService xmlParser,
        IMemoryCache cache,
        ILogger<RunQueryService> logger)
    {
        _repository = repository;
        _fileSystem = fileSystem;
        _xmlParser = xmlParser;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TestRunDto?> GetTestRunAsync(string host, string pdc, string runId, CancellationToken ct = default)
    {
        var cacheKey = $"run:{host}:{pdc}:{runId}";

        if (_cache.TryGetValue(cacheKey, out TestRunDto? cached))
        {
            _logger.LogDebug("Cache HIT for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache MISS for {CacheKey}. Querying database…", cacheKey);

        // 1) DB-first lookup
        var entity = await _repository.GetByCompositeKeyAsync(host, pdc, runId, ct);

        if (entity is not null)
        {
            _logger.LogInformation("DB HIT for Host={Host}, Pdc={Pdc}, RunId={RunId}", host, pdc, runId);
            var dto = entity.ToDto();
            _cache.Set(cacheKey, dto, CacheOptions);
            return dto;
        }

        // 2) NightBatch filesystem fallback
        _logger.LogInformation("DB MISS – falling back to NightBatch filesystem for Host={Host}, Pdc={Pdc}, RunId={RunId}",
            host, pdc, runId);

        var fileExists = await _fileSystem.ExistsAsync(host, pdc, runId, ct);

        if (!fileExists)
        {
            _logger.LogWarning("File NOT FOUND on NightBatch for Host={Host}, Pdc={Pdc}, RunId={RunId}",
                host, pdc, runId);
            return null;
        }

        // 3) Load raw XML from NightBatch
        _logger.LogInformation("File HIT on NightBatch for Host={Host}, Pdc={Pdc}, RunId={RunId}",
            host, pdc, runId);
        var rawData = await _fileSystem.LoadRawRunAsync(host, pdc, runId, ct);

        // 4) Parse XML → Domain entity
        TestRun parsed;
        try
        {
            parsed = await _xmlParser.ParseAsync(rawData, host, pdc, runId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse raw XML for Host={Host}, Pdc={Pdc}, RunId={RunId}",
                host, pdc, runId);
            throw;
        }
        parsed.CreatedAt = DateTime.UtcNow;
        parsed.ImportStatus = "Imported";

        // 5) Upsert into DB
        await _repository.UpsertAsync(parsed, ct);
        _logger.LogInformation("Insert success – stored run from NightBatch into DB: Host={Host}, Pdc={Pdc}, RunId={RunId}",
            host, pdc, runId);

        // 6) Archive to Server Machine
        try
        {
            await _fileSystem.ArchiveAsync(host, pdc, runId, ct);
            _logger.LogInformation("Archived run to Server Machine: Host={Host}, Pdc={Pdc}, RunId={RunId}",
                host, pdc, runId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive run Host={Host}, Pdc={Pdc}, RunId={RunId}. " +
                                 "Data is in DB; archive can be retried.", host, pdc, runId);
        }

        // 7) Return DTO
        var resultDto = parsed.ToDto();
        _cache.Set(cacheKey, resultDto, CacheOptions);
        return resultDto;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<TestRunDto>> GetAllRunsAsync(
        int page, int pageSize,
        string? host = null,
        string? pdc = null,
        string? overallResult = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _repository.GetAllAsync(page, pageSize, host, pdc, overallResult, fromDate, toDate, ct);

        var dtos = items.Select(e => e.ToSummaryDto()).ToList();

        return new PagedResultDto<TestRunDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
