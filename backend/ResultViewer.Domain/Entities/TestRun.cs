namespace ResultViewer.Domain.Entities;

public class TestRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Host { get; set; } = string.Empty;
    public string Pdc { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string OverallResult { get; set; } = string.Empty;
    public int TestCount { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public string RawJson { get; set; } = string.Empty;
    public string? ArchivePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ImportStatus { get; set; } = "Completed";
    public bool IsDeleted { get; set; } = false;

    public ICollection<TestResult> IndexedResults { get; set; } = new List<TestResult>();
    public SystemInfo? SystemInfo { get; set; }
    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
}
