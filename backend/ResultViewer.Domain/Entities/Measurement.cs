namespace ResultViewer.Domain.Entities;

public class Measurement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // From SystemFingerPrint.xml → Test → Measurement
    public string TestName { get; set; } = string.Empty;
    public string MeasurementName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? MeasurementUnit { get; set; }
    public string? Description { get; set; }
    public string? MeasuredValue { get; set; }
    public string? SpecErrorUpper { get; set; }
    public string? SpecErrorLower { get; set; }
    public string? SpecWarningUpper { get; set; }
    public string? SpecWarningLower { get; set; }

    // Foreign key
    public Guid TestRunId { get; set; }
    public TestRun TestRun { get; set; } = null!;
}
