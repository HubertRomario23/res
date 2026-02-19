namespace ResultViewer.Domain.Entities;

public class TestResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TestName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }

    // Foreign key
    public Guid TestRunId { get; set; }
    public TestRun TestRun { get; set; } = null!;
}
