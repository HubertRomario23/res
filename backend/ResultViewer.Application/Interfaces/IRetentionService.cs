namespace ResultViewer.Application.Interfaces;

public interface IRetentionService
{
    Task CleanupExpiredRunsAsync(CancellationToken ct = default);
    Task CleanupExpiredArchivesAsync(CancellationToken ct = default);
}
