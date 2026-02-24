using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResultViewer.Domain.Entities;
using ResultViewer.Domain.Interfaces;
using ResultViewer.Infrastructure.Data;

namespace ResultViewer.Infrastructure.Repositories;

public sealed class TestRunRepository : ITestRunRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<TestRunRepository> _logger;

    public TestRunRepository(AppDbContext db, ILogger<TestRunRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TestRun?> GetByCompositeKeyAsync(string host, string pdc, string runId, CancellationToken ct = default)
    {
        // Load the run header first (fast, no joins)
        var run = await _db.TestRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Host == host && t.Pdc == pdc && t.RunId == runId, ct);

        if (run is null) return null;

        // Load children separately to avoid Cartesian explosion
        run.IndexedResults = await _db.Set<TestResult>()
            .AsNoTracking()
            .Where(r => r.TestRunId == run.Id)
            .ToListAsync(ct);

        run.SystemInfo = await _db.Set<SystemInfo>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TestRunId == run.Id, ct);

        run.Measurements = await _db.Set<Measurement>()
            .AsNoTracking()
            .Where(m => m.TestRunId == run.Id)
            .ToListAsync(ct);

        return run;
    }

    public async Task<(IReadOnlyList<TestRun> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize,
        string? host = null,
        string? pdc = null,
        string? overallResult = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        var query = _db.TestRuns.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(host))
            query = query.Where(t => t.Host.Contains(host));

        if (!string.IsNullOrWhiteSpace(pdc))
            query = query.Where(t => t.Pdc.Contains(pdc));

        if (!string.IsNullOrWhiteSpace(overallResult))
            query = query.Where(t => t.OverallResult != null && t.OverallResult.Contains(overallResult));

        if (fromDate.HasValue)
            query = query.Where(t => t.StartTime >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(t => t.StartTime <= toDate.Value);

        var totalCount = await query.CountAsync(ct);

        // Use projection to exclude large columns (RawJson, SpecflowLog, RawFingerPrintXml)
        // This significantly reduces data transfer from SQL Server
        var items = await query
            .OrderByDescending(t => t.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TestRun
            {
                Id = t.Id,
                Host = t.Host,
                Pdc = t.Pdc,
                RunId = t.RunId,
                StartTime = t.StartTime,
                EndTime = t.EndTime,
                OverallResult = t.OverallResult,
                TestCount = t.TestCount,
                PassedCount = t.PassedCount,
                FailedCount = t.FailedCount,
                SkippedCount = t.SkippedCount,
                ArchivePath = t.ArchivePath,
                CreatedAt = t.CreatedAt,
                ImportStatus = t.ImportStatus,
                IsDeleted = t.IsDeleted
                // RawJson, SpecflowLog excluded - not needed for list view
            })
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task InsertAsync(TestRun entity, CancellationToken ct = default)
    {
        _db.TestRuns.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogDebug("Inserted TestRun Id={Id}", entity.Id);
    }

    public async Task UpsertAsync(TestRun entity, CancellationToken ct = default)
    {
        var existing = await _db.TestRuns
            .IgnoreQueryFilters()
            .Include(t => t.IndexedResults)
            .Include(t => t.SystemInfo)
            .Include(t => t.Measurements)
            .FirstOrDefaultAsync(t => t.Host == entity.Host && t.Pdc == entity.Pdc && t.RunId == entity.RunId, ct);

        if (existing is null)
        {
            _db.TestRuns.Add(entity);
            _logger.LogDebug("Upsert: Inserting new TestRun Host={Host}, Pdc={Pdc}, RunId={RunId}",
                entity.Host, entity.Pdc, entity.RunId);
        }
        else
        {
            // Update scalar properties
            existing.StartTime = entity.StartTime;
            existing.EndTime = entity.EndTime;
            existing.OverallResult = entity.OverallResult;
            existing.TestCount = entity.TestCount;
            existing.PassedCount = entity.PassedCount;
            existing.FailedCount = entity.FailedCount;
            existing.SkippedCount = entity.SkippedCount;
            existing.RawJson = entity.RawJson;
            existing.ArchivePath = entity.ArchivePath;
            existing.CreatedAt = entity.CreatedAt;
            existing.ImportStatus = entity.ImportStatus;
            existing.IsDeleted = false; // re-activate if soft-deleted

            // Replace child results
            _db.TestResults.RemoveRange(existing.IndexedResults);
            foreach (var result in entity.IndexedResults)
            {
                result.TestRunId = existing.Id;
                _db.TestResults.Add(result);
            }

            // Replace SystemInfo
            if (existing.SystemInfo is not null)
                _db.SystemInfos.Remove(existing.SystemInfo);
            if (entity.SystemInfo is not null)
            {
                entity.SystemInfo.TestRunId = existing.Id;
                _db.SystemInfos.Add(entity.SystemInfo);
            }

            // Replace Measurements
            _db.Measurements.RemoveRange(existing.Measurements);
            foreach (var m in entity.Measurements)
            {
                m.TestRunId = existing.Id;
                _db.Measurements.Add(m);
            }

            _logger.LogDebug("Upsert: Updating existing TestRun Host={Host}, Pdc={Pdc}, RunId={RunId}",
                entity.Host, entity.Pdc, entity.RunId);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var count = await _db.TestRuns
            .IgnoreQueryFilters()
            .Where(t => t.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        _logger.LogInformation("Hard-deleted {Count} expired test runs older than {Cutoff}", count, cutoff);
        return count;
    }

    public async Task<int> SoftDeleteExpiredAsync(DateTime cutoff, CancellationToken ct = default)
    {
        var count = await _db.TestRuns
            .Where(t => t.CreatedAt < cutoff && !t.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true), ct);

        _logger.LogInformation("Soft-deleted {Count} expired test runs older than {Cutoff}", count, cutoff);
        return count;
    }
}
