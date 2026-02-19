using ResultViewer.Application.DTOs;
using ResultViewer.Domain.Entities;

namespace ResultViewer.Application.Mappings;

public static class TestRunMapper
{
    public static TestRunDto ToDto(this TestRun entity)
    {
        return new TestRunDto
        {
            Id = entity.Id,
            Host = entity.Host,
            Pdc = entity.Pdc,
            RunId = entity.RunId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            OverallResult = entity.OverallResult,
            TestCount = entity.TestCount,
            PassedCount = entity.PassedCount,
            FailedCount = entity.FailedCount,
            SkippedCount = entity.SkippedCount,
            RawJson = "",  // Excluded from API responses — too large
            ArchivePath = entity.ArchivePath,
            CreatedAt = entity.CreatedAt,
            IndexedResults = entity.IndexedResults.Select(r => new TestResultDto
            {
                Id = r.Id,
                TestName = r.TestName,
                Result = r.Result,
                ErrorMessage = r.ErrorMessage,
                Duration = r.Duration
            }).ToList(),
            SystemInfo = entity.SystemInfo is not null ? new SystemInfoDto
            {
                SystemName = entity.SystemInfo.SystemName,
                STM = entity.SystemInfo.STM,
                MSIVersion = entity.SystemInfo.MSIVersion,
                PDCVersion = entity.SystemInfo.PDCVersion,
                MonoplaneOrBiplane = entity.SystemInfo.MonoplaneOrBiplane,
                FrontalStandType = entity.SystemInfo.FrontalStandType,
                TableType = entity.SystemInfo.TableType,
                TableTopType = entity.SystemInfo.TableTopType,
                DetectorNameFrontal = entity.SystemInfo.DetectorNameFrontal,
                DetectorNameLateral = entity.SystemInfo.DetectorNameLateral,
                SystemType = entity.SystemInfo.SystemType,
                ProductFamily = entity.SystemInfo.ProductFamily,
                DetectorType = entity.SystemInfo.DetectorType,
                LateralStandType = entity.SystemInfo.LateralStandType,
                SystemConfigType = entity.SystemInfo.SystemConfigType
            } : null,
            Measurements = entity.Measurements.Select(m => new MeasurementDto
            {
                TestName = m.TestName,
                MeasurementName = m.MeasurementName,
                Result = m.Result,
                MeasurementUnit = m.MeasurementUnit,
                Description = m.Description,
                MeasuredValue = m.MeasuredValue,
                SpecErrorUpper = m.SpecErrorUpper,
                SpecErrorLower = m.SpecErrorLower,
                SpecWarningUpper = m.SpecWarningUpper,
                SpecWarningLower = m.SpecWarningLower
            }).ToList()
        };
    }

    public static TestRunDto ToSummaryDto(this TestRun entity)
    {
        return new TestRunDto
        {
            Id = entity.Id,
            Host = entity.Host,
            Pdc = entity.Pdc,
            RunId = entity.RunId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            OverallResult = entity.OverallResult,
            TestCount = entity.TestCount,
            PassedCount = entity.PassedCount,
            FailedCount = entity.FailedCount,
            SkippedCount = entity.SkippedCount,
            ArchivePath = entity.ArchivePath,
            CreatedAt = entity.CreatedAt,
            IndexedResults = []
        };
    }
}
