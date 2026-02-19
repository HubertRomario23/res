using ResultViewer.Application.DTOs;

namespace ResultViewer.Application.Interfaces;

public interface IRunQueryService
{
    Task<TestRunDto?> GetTestRunAsync(string host, string pdc, string runId, CancellationToken ct = default);
    Task<PagedResultDto<TestRunDto>> GetAllRunsAsync(int page, int pageSize, string? host, string? pdc, string? overallResult, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
}
