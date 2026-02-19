namespace ResultViewer.Domain.Entities;

public class SystemInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // From system-info.xml
    public string SystemName { get; set; } = string.Empty;
    public string STM { get; set; } = string.Empty;
    public string MSIVersion { get; set; } = string.Empty;
    public string PDCVersion { get; set; } = string.Empty;
    public string MonoplaneOrBiplane { get; set; } = string.Empty;
    public string FrontalStandType { get; set; } = string.Empty;
    public string TableType { get; set; } = string.Empty;
    public string TableTopType { get; set; } = string.Empty;
    public string DetectorNameFrontal { get; set; } = string.Empty;
    public string DetectorNameLateral { get; set; } = string.Empty;
    public string SystemType { get; set; } = string.Empty;
    public string ProductFamily { get; set; } = string.Empty;
    public string DetectorType { get; set; } = string.Empty;
    public string LateralStandType { get; set; } = string.Empty;
    public string SystemConfigType { get; set; } = string.Empty;
    public string RawXml { get; set; } = string.Empty;

    // Foreign key
    public Guid TestRunId { get; set; }
    public TestRun TestRun { get; set; } = null!;
}
