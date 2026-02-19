using ResultViewer.Domain.Entities;

namespace ResultViewer.Domain.Interfaces;

public interface IXmlParserService
{
    Task<TestRun> ParseAsync(RawRunData rawData, string host, string pdc, string runId, CancellationToken ct = default);
}
