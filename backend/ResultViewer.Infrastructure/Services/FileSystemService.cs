using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ResultViewer.Domain.Interfaces;

namespace ResultViewer.Infrastructure.Services;

public sealed class FileSystemService : IFileSystemService
{
    private readonly string _nightBatchRoot;
    private readonly string _archiveRoot;
    private readonly string _testOutputFolder;
    private readonly string _resultFileName;
    private readonly ILogger<FileSystemService> _logger;

    public FileSystemService(IConfiguration configuration, ILogger<FileSystemService> logger)
    {
        _nightBatchRoot = configuration["NightBatch:RootPath"]
            ?? throw new InvalidOperationException("NightBatch:RootPath is not configured.");
        _archiveRoot = configuration["Archive:RootPath"]
            ?? throw new InvalidOperationException("Archive:RootPath is not configured.");
        _testOutputFolder = configuration["NightBatch:TestOutputFolder"] ?? "TestOutput";
        _resultFileName = configuration["NightBatch:ResultFileName"] ?? "TestResult.xml";
        _logger = logger;
    }

    private string BuildRunDirectory(string host, string pdc, string runId) =>
        Path.Combine(_nightBatchRoot, host, pdc, runId);

    private string BuildTestOutputDirectory(string host, string pdc, string runId) =>
        Path.Combine(_nightBatchRoot, host, pdc, runId, _testOutputFolder);

    private string BuildArchivePath(string host, string pdc, string runId) =>
        Path.Combine(_archiveRoot, host, pdc, $"{runId}.zip");

    public Task<bool> ExistsAsync(string host, string pdc, string runId, CancellationToken ct = default)
    {
        var testOutputDir = BuildTestOutputDirectory(host, pdc, runId);
        var runDir = BuildRunDirectory(host, pdc, runId);

        var resultFile = Path.Combine(testOutputDir, _resultFileName);
        var hasResultXml = File.Exists(resultFile);
        var hasZip = Directory.Exists(runDir) && Directory.GetFiles(runDir, "*.zip").Length > 0;
        var hasAnyXml = Directory.Exists(testOutputDir) && Directory.GetFiles(testOutputDir, "*.xml").Length > 0;

        var exists = hasResultXml || hasAnyXml || hasZip;
        _logger.LogDebug("FileSystem check: testOutputDir={Dir}, resultXml={HasResult}, anyXml={HasAnyXml}, zip={HasZip}",
            testOutputDir, hasResultXml, hasAnyXml, hasZip);
        return Task.FromResult(exists);
    }

    public async Task<RawRunData> LoadRawRunAsync(string host, string pdc, string runId, CancellationToken ct = default)
    {
        var runDir = BuildRunDirectory(host, pdc, runId);
        var testOutputDir = BuildTestOutputDirectory(host, pdc, runId);

        // Step 1: Extract ZIPs if present
        var zipFiles = Directory.Exists(runDir) ? Directory.GetFiles(runDir, "*.zip") : Array.Empty<string>();
        foreach (var zipPath in zipFiles)
        {
            _logger.LogInformation("Extracting ZIP {ZipPath} into {RunDir}", zipPath, runDir);
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, runDir, overwriteFiles: true), ct);
            _logger.LogInformation("ZIP extracted successfully: {ZipPath}", zipPath);
        }

        // Step 2: Load TestResult.xml
        string testResultXml;
        var resultFile = Path.Combine(testOutputDir, _resultFileName);
        if (File.Exists(resultFile))
        {
            _logger.LogInformation("Loading TestResult XML from {FilePath}", resultFile);
            testResultXml = await File.ReadAllTextAsync(resultFile, ct);
        }
        else
        {
            // Fallback: first XML in TestOutput
            var xmlFiles = Directory.Exists(testOutputDir) ? Directory.GetFiles(testOutputDir, "*.xml") : Array.Empty<string>();
            if (xmlFiles.Length > 0)
            {
                _logger.LogInformation("Result file not found, falling back to {FilePath}", xmlFiles[0]);
                testResultXml = await File.ReadAllTextAsync(xmlFiles[0], ct);
            }
            else
            {
                throw new FileNotFoundException(
                    $"No XML files found in {testOutputDir}. Expected '{_resultFileName}'.");
            }
        }

        // Step 3: Load system-info.xml (optional)
        string? systemInfoXml = null;
        var systemInfoFile = Path.Combine(testOutputDir, "system-info.xml");
        if (File.Exists(systemInfoFile))
        {
            _logger.LogInformation("Loading system-info.xml from {FilePath}", systemInfoFile);
            systemInfoXml = await File.ReadAllTextAsync(systemInfoFile, ct);
        }
        else
        {
            _logger.LogDebug("system-info.xml not found at {FilePath}", systemInfoFile);
        }

        // Step 4: Load SystemFingerPrint.xml (measurements, optional)
        string? fingerPrintXml = null;
        var fingerPrintFile = Path.Combine(testOutputDir, "SystemFingerPrint.xml");
        if (File.Exists(fingerPrintFile))
        {
            _logger.LogInformation("Loading SystemFingerPrint.xml from {FilePath}", fingerPrintFile);
            fingerPrintXml = await File.ReadAllTextAsync(fingerPrintFile, ct);
        }
        else
        {
            _logger.LogDebug("SystemFingerPrint.xml not found at {FilePath}", fingerPrintFile);
        }

        return new RawRunData(testResultXml, systemInfoXml, fingerPrintXml);
    }

    public async Task ArchiveAsync(string host, string pdc, string runId, CancellationToken ct = default)
    {
        var sourceDir = BuildRunDirectory(host, pdc, runId);
        var archivePath = BuildArchivePath(host, pdc, runId);

        var archiveDir = Path.GetDirectoryName(archivePath)!;
        Directory.CreateDirectory(archiveDir);

        if (File.Exists(archivePath))
        {
            _logger.LogDebug("Archive already exists at {ArchivePath}, overwriting.", archivePath);
            File.Delete(archivePath);
        }

        _logger.LogInformation("Archiving {SourceDir} → {ArchivePath}", sourceDir, archivePath);

        await Task.Run(() => ZipFile.CreateFromDirectory(sourceDir, archivePath, CompressionLevel.Optimal, false), ct);

        _logger.LogInformation("Archive complete: {ArchivePath}. Removing source directory.", archivePath);

        // Move semantics: delete the source after successful archive
        Directory.Delete(sourceDir, recursive: true);
        _logger.LogInformation("Source directory deleted after archive: {SourceDir}", sourceDir);
    }
}
