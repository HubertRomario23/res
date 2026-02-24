namespace ResultViewer.Application.DTOs;

public record TestRunDto
{
    public Guid Id { get; init; }
    public string Host { get; init; } = string.Empty;
    public string Pdc { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string OverallResult { get; init; } = string.Empty;
    public int TestCount { get; init; }
    public int PassedCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
    public string RawJson { get; init; } = string.Empty;
    public string? ArchivePath { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? SpecflowLog { get; init; }
    public IReadOnlyList<TestResultDto> IndexedResults { get; init; } = [];
    public SystemInfoDto? SystemInfo { get; init; }
    public IReadOnlyList<MeasurementDto> Measurements { get; init; } = [];
}

public record TestResultDto
{
    public Guid Id { get; init; }
    public string TestName { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }
}

public record SystemInfoDto
{
    public string SystemName { get; init; } = string.Empty;
    public string STM { get; init; } = string.Empty;
    public string MSIVersion { get; init; } = string.Empty;
    public string PDCVersion { get; init; } = string.Empty;
    public string MonoplaneOrBiplane { get; init; } = string.Empty;
    public string FrontalStandType { get; init; } = string.Empty;
    public string TableType { get; init; } = string.Empty;
    public string TableTopType { get; init; } = string.Empty;
    public string DetectorNameFrontal { get; init; } = string.Empty;
    public string DetectorNameLateral { get; init; } = string.Empty;
    public string SystemType { get; init; } = string.Empty;
    public string ProductFamily { get; init; } = string.Empty;
    public string DetectorType { get; init; } = string.Empty;
    public string LateralStandType { get; init; } = string.Empty;
    public string SystemConfigType { get; init; } = string.Empty;
}

public record MeasurementDto
{
    public string TestName { get; init; } = string.Empty;
    public string MeasurementName { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public string? MeasurementUnit { get; init; }
    public string? Description { get; init; }
    public string? MeasuredValue { get; init; }
    public string? SpecErrorUpper { get; init; }
    public string? SpecErrorLower { get; init; }
    public string? SpecWarningUpper { get; init; }
    public string? SpecWarningLower { get; init; }
}
