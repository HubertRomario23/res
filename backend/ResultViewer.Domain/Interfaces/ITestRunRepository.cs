using ResultViewer.Domain.Entities;

namespace ResultViewer.Domain.Interfaces;

public interface ITestRunRepository
{
    Task<TestRun?> GetByCompositeKeyAsync(string host, string pdc, string runId, CancellationToken ct = default);
    Task<(IReadOnlyList<TestRun> Items, int TotalCount)> GetAllAsync(int page, int pageSize, string? host, string? pdc, string? overallResult, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task InsertAsync(TestRun testRun, CancellationToken ct = default);
    Task UpsertAsync(TestRun testRun, CancellationToken ct = default);
    Task<int> DeleteExpiredAsync(DateTime cutoffDate, CancellationToken ct = default);
    Task<int> SoftDeleteExpiredAsync(DateTime cutoffDate, CancellationToken ct = default);
}
