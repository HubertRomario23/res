namespace ResultViewer.Domain.Interfaces;

public interface IFileSystemService
{
    Task<bool> ExistsAsync(string host, string pdc, string runId, CancellationToken ct = default);
    Task<RawRunData> LoadRawRunAsync(string host, string pdc, string runId, CancellationToken ct = default);
    Task ArchiveAsync(string host, string pdc, string runId, CancellationToken ct = default);
}

/// <summary>
/// Holds the raw XML content from each file in the TestOutput folder.
/// </summary>
public record RawRunData(
    string TestResultXml,
    string? SystemInfoXml,
    string? SystemFingerPrintXml,
    string? SpecflowLog
);
