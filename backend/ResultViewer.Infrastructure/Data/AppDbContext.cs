using Microsoft.EntityFrameworkCore;
using ResultViewer.Domain.Entities;

namespace ResultViewer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TestRun> TestRuns => Set<TestRun>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<SystemInfo> SystemInfos => Set<SystemInfo>();
    public DbSet<Measurement> Measurements => Set<Measurement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- TestRun ---
        var testRun = modelBuilder.Entity<TestRun>();

        testRun.ToTable("TestRuns");
        testRun.HasKey(t => t.Id);

        testRun.HasIndex(t => new { t.Host, t.Pdc, t.RunId })
               .IsUnique()
               .HasDatabaseName("IX_TestRuns_Host_Pdc_RunId");

        testRun.HasIndex(t => t.StartTime).HasDatabaseName("IX_TestRuns_StartTime");
        testRun.HasIndex(t => t.OverallResult).HasDatabaseName("IX_TestRuns_OverallResult");
        testRun.HasIndex(t => t.IsDeleted).HasDatabaseName("IX_TestRuns_IsDeleted");
        testRun.HasIndex(t => t.CreatedAt).HasDatabaseName("IX_TestRuns_CreatedAt");

        testRun.Property(t => t.Host).HasMaxLength(256).IsRequired();
        testRun.Property(t => t.Pdc).HasMaxLength(256).IsRequired();
        testRun.Property(t => t.RunId).HasMaxLength(256).IsRequired();
        testRun.Property(t => t.OverallResult).HasMaxLength(64);
        testRun.Property(t => t.ImportStatus).HasMaxLength(64);
        testRun.Property(t => t.ArchivePath).HasMaxLength(1024);

        testRun.HasMany(t => t.IndexedResults)
               .WithOne(r => r.TestRun)
               .HasForeignKey(r => r.TestRunId)
               .OnDelete(DeleteBehavior.Cascade);

        testRun.HasOne(t => t.SystemInfo)
               .WithOne(s => s.TestRun)
               .HasForeignKey<SystemInfo>(s => s.TestRunId)
               .OnDelete(DeleteBehavior.Cascade);

        testRun.HasMany(t => t.Measurements)
               .WithOne(m => m.TestRun)
               .HasForeignKey(m => m.TestRunId)
               .OnDelete(DeleteBehavior.Cascade);

        testRun.HasQueryFilter(t => !t.IsDeleted);

        // --- TestResult ---
        var testResult = modelBuilder.Entity<TestResult>();

        testResult.ToTable("TestResults");
        testResult.HasKey(r => r.Id);

        testResult.Property(r => r.TestName).HasMaxLength(512).IsRequired();
        testResult.Property(r => r.Result).HasMaxLength(64);
        testResult.Property(r => r.ErrorMessage).HasMaxLength(4000);

        testResult.HasIndex(r => r.TestRunId).HasDatabaseName("IX_TestResults_TestRunId");

        // --- SystemInfo ---
        var sysInfo = modelBuilder.Entity<SystemInfo>();

        sysInfo.ToTable("SystemInfos");
        sysInfo.HasKey(s => s.Id);

        sysInfo.Property(s => s.SystemName).HasMaxLength(256);
        sysInfo.Property(s => s.STM).HasMaxLength(64);
        sysInfo.Property(s => s.MSIVersion).HasMaxLength(64);
        sysInfo.Property(s => s.PDCVersion).HasMaxLength(64);
        sysInfo.Property(s => s.MonoplaneOrBiplane).HasMaxLength(64);
        sysInfo.Property(s => s.FrontalStandType).HasMaxLength(128);
        sysInfo.Property(s => s.TableType).HasMaxLength(128);
        sysInfo.Property(s => s.TableTopType).HasMaxLength(128);
        sysInfo.Property(s => s.DetectorNameFrontal).HasMaxLength(128);
        sysInfo.Property(s => s.DetectorNameLateral).HasMaxLength(128);
        sysInfo.Property(s => s.SystemType).HasMaxLength(256);
        sysInfo.Property(s => s.ProductFamily).HasMaxLength(128);
        sysInfo.Property(s => s.DetectorType).HasMaxLength(128);
        sysInfo.Property(s => s.LateralStandType).HasMaxLength(128);
        sysInfo.Property(s => s.SystemConfigType).HasMaxLength(64);

        sysInfo.HasIndex(s => s.TestRunId).IsUnique().HasDatabaseName("IX_SystemInfos_TestRunId");

        // --- Measurement ---
        var measurement = modelBuilder.Entity<Measurement>();

        measurement.ToTable("Measurements");
        measurement.HasKey(m => m.Id);

        measurement.Property(m => m.TestName).HasMaxLength(512);
        measurement.Property(m => m.MeasurementName).HasMaxLength(512);
        measurement.Property(m => m.Result).HasMaxLength(64);
        measurement.Property(m => m.MeasurementUnit).HasMaxLength(64);
        measurement.Property(m => m.Description).HasMaxLength(1024);
        measurement.Property(m => m.MeasuredValue).HasMaxLength(256);
        measurement.Property(m => m.SpecErrorUpper).HasMaxLength(64);
        measurement.Property(m => m.SpecErrorLower).HasMaxLength(64);
        measurement.Property(m => m.SpecWarningUpper).HasMaxLength(64);
        measurement.Property(m => m.SpecWarningLower).HasMaxLength(64);

        measurement.HasIndex(m => m.TestRunId).HasDatabaseName("IX_Measurements_TestRunId");
    }
}
