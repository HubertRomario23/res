using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResultViewer.Application.Interfaces;
using ResultViewer.Domain.Interfaces;

namespace ResultViewer.Infrastructure.Services;

public sealed class RetentionService : IRetentionService
{
    private readonly ITestRunRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RetentionService> _logger;

    public RetentionService(
        ITestRunRepository repository,
        IConfiguration configuration,
        ILogger<RetentionService> logger)
    {
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task CleanupExpiredRunsAsync(CancellationToken ct = default)
    {
        var retentionYearsStr = _configuration["Retention:Years"];
        var retentionYears = int.TryParse(retentionYearsStr, out var y) ? y : 2;
        var cutoff = DateTime.UtcNow.AddYears(-retentionYears);

        _logger.LogInformation("Starting DB retention cleanup. Retention={Years}y, Cutoff={Cutoff}",
            retentionYears, cutoff);

        var count = await _repository.SoftDeleteExpiredAsync(cutoff, ct);

        _logger.LogInformation("Retention cleanup complete: {Count} runs soft-deleted", count);
    }

    public async Task CleanupExpiredArchivesAsync(CancellationToken ct = default)
    {
        var archiveRoot = _configuration["Archive:RootPath"];
        var archiveYearsStr = _configuration["Retention:ArchiveYears"];
        var retentionYears = int.TryParse(archiveYearsStr, out var y) ? y : 5;
        var cutoff = DateTime.UtcNow.AddYears(-retentionYears);

        if (string.IsNullOrWhiteSpace(archiveRoot) || !Directory.Exists(archiveRoot))
        {
            _logger.LogWarning("Archive root path not configured or does not exist. Skipping archive cleanup.");
            return;
        }

        _logger.LogInformation("Starting archive cleanup. Retention={Years}y, Cutoff={Cutoff}",
            retentionYears, cutoff);

        var deleted = 0;
        foreach (var file in Directory.EnumerateFiles(archiveRoot, "*.zip", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            if (File.GetLastWriteTimeUtc(file) < cutoff)
            {
                File.Delete(file);
                deleted++;
            }
        }

        _logger.LogInformation("Archive cleanup complete: {Count} archive files deleted", deleted);
        await Task.CompletedTask;
    }
}
