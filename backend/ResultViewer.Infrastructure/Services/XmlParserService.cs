using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ResultViewer.Domain.Entities;
using ResultViewer.Domain.Interfaces;

namespace ResultViewer.Infrastructure.Services;

public sealed class XmlParserService : IXmlParserService
{
    private readonly ILogger<XmlParserService> _logger;

    public XmlParserService(ILogger<XmlParserService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses all three XMLs (TestResult, system-info, SystemFingerPrint) into a single TestRun entity.
    /// </summary>
    public Task<TestRun> ParseAsync(RawRunData rawData, string host, string pdc, string runId, CancellationToken ct = default)
    {
        var testRun = new TestRun
        {
            Id = Guid.NewGuid(),
            Host = host,
            Pdc = pdc,
            RunId = runId,
            RawJson = rawData.TestResultXml
        };

        // ── 1) Parse TestResult.xml (NUnit 3 format) ──
        ParseTestResult(rawData.TestResultXml, testRun);

        // ── 2) Parse system-info.xml ──
        if (!string.IsNullOrEmpty(rawData.SystemInfoXml))
        {
            try
            {
                testRun.SystemInfo = ParseSystemInfo(rawData.SystemInfoXml, testRun.Id);
                _logger.LogInformation("Parsed system-info.xml: SystemName={SystemName}, ProductFamily={Family}",
                    testRun.SystemInfo.SystemName, testRun.SystemInfo.ProductFamily);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse system-info.xml for Host={Host}, Pdc={Pdc}, RunId={RunId}",
                    host, pdc, runId);
            }
        }

        // ── 3) Parse SystemFingerPrint.xml (measurements) ──
        if (!string.IsNullOrEmpty(rawData.SystemFingerPrintXml))
        {
            try
            {
                var measurements = ParseFingerPrint(rawData.SystemFingerPrintXml, testRun.Id);
                foreach (var m in measurements)
                    testRun.Measurements.Add(m);

                _logger.LogInformation("Parsed SystemFingerPrint.xml: {Count} measurements extracted",
                    testRun.Measurements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse SystemFingerPrint.xml for Host={Host}, Pdc={Pdc}, RunId={RunId}",
                    host, pdc, runId);
            }
        }

        _logger.LogInformation(
            "Parsed TestRun: {TestCount} tests ({Passed} passed, {Failed} failed), " +
            "SystemInfo={HasSysInfo}, Measurements={MeasCount}",
            testRun.TestCount, testRun.PassedCount, testRun.FailedCount,
            testRun.SystemInfo is not null, testRun.Measurements.Count);

        return Task.FromResult(testRun);
    }

    // ═══════════════════════════════════════════════════════════
    //  TestResult.xml — NUnit 3 format
    // ═══════════════════════════════════════════════════════════
    private void ParseTestResult(string xml, TestRun testRun)
    {
        _logger.LogDebug("Parsing TestResult.xml ({Length} chars)", xml.Length);

        var doc = XDocument.Parse(xml);
        var root = doc.Root;

        if (root is null) return;

        // Root <test-run> attributes
        testRun.OverallResult = root.Attribute("result")?.Value ?? "Unknown";
        testRun.TestCount = ParseInt(root.Attribute("total")?.Value);
        testRun.PassedCount = ParseInt(root.Attribute("passed")?.Value);
        testRun.FailedCount = ParseInt(root.Attribute("failed")?.Value);
        testRun.SkippedCount = ParseInt(root.Attribute("skipped")?.Value)
                               + ParseInt(root.Attribute("inconclusive")?.Value);

        testRun.StartTime = ParseDateTime(root.Attribute("start-time")?.Value) ?? DateTime.MinValue;
        testRun.EndTime = ParseDateTime(root.Attribute("end-time")?.Value) ?? DateTime.MinValue;

        // Parse individual <test-case> elements (leaf-level results)
        var testCases = doc.Descendants("test-case");

        foreach (var tc in testCases)
        {
            var result = new TestResult
            {
                Id = Guid.NewGuid(),
                TestRunId = testRun.Id,
                TestName = tc.Attribute("fullname")?.Value ?? tc.Attribute("name")?.Value ?? "Unknown",
                Result = tc.Attribute("result")?.Value ?? "Unknown",
                Duration = ParseDurationSeconds(tc.Attribute("duration")?.Value),
                ErrorMessage = tc.Descendants("message").FirstOrDefault()?.Value
            };

            testRun.IndexedResults.Add(result);
        }

        // If root counters were missing, compute from test cases
        if (testRun.TestCount == 0 && testRun.IndexedResults.Count > 0)
        {
            testRun.TestCount = testRun.IndexedResults.Count;
            testRun.PassedCount = testRun.IndexedResults.Count(r => r.Result == "Passed");
            testRun.FailedCount = testRun.IndexedResults.Count(r => r.Result == "Failed");
            testRun.SkippedCount = testRun.TestCount - testRun.PassedCount - testRun.FailedCount;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  system-info.xml
    // ═══════════════════════════════════════════════════════════
    private static SystemInfo ParseSystemInfo(string xml, Guid testRunId)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root!; // <SystemInfo>

        return new SystemInfo
        {
            Id = Guid.NewGuid(),
            TestRunId = testRunId,
            SystemName = root.Element("SystemName")?.Value ?? string.Empty,
            STM = root.Element("STM")?.Value ?? string.Empty,
            MSIVersion = root.Element("MSIVersion")?.Value ?? string.Empty,
            PDCVersion = root.Element("PDCVersion")?.Value ?? string.Empty,
            MonoplaneOrBiplane = root.Element("MonoplaneOrBiplane")?.Value ?? string.Empty,
            FrontalStandType = root.Element("FrontalStandType")?.Value ?? string.Empty,
            TableType = root.Element("TableType")?.Value ?? string.Empty,
            TableTopType = root.Element("TableTopType")?.Value ?? string.Empty,
            DetectorNameFrontal = root.Element("DetectorNameFrontal")?.Value ?? string.Empty,
            DetectorNameLateral = root.Element("DetectorNameLateral")?.Value ?? string.Empty,
            SystemType = root.Element("SystemType")?.Value ?? string.Empty,
            ProductFamily = root.Element("ProductFamily")?.Value ?? string.Empty,
            DetectorType = root.Element("DetectorType")?.Value ?? string.Empty,
            LateralStandType = root.Element("LateralStandType")?.Value ?? string.Empty,
            SystemConfigType = root.Element("SystemConfigType")?.Value ?? string.Empty,
            RawXml = xml
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  SystemFingerPrint.xml — <MedicalSystem> / <Test> / <Test> / <Measurement>
    // ═══════════════════════════════════════════════════════════
    private static List<Measurement> ParseFingerPrint(string xml, Guid testRunId)
    {
        var doc = XDocument.Parse(xml);
        var measurements = new List<Measurement>();

        // Structure: <MedicalSystem> → <Test> → <Test Name="..." ...> → <Measurement ...>
        var outerTests = doc.Root?.Elements("Test");
        if (outerTests is null) return measurements;

        foreach (var outerTest in outerTests)
        {
            // Inner <Test> elements are the actual tests with measurements
            var innerTests = outerTest.Elements("Test");

            foreach (var test in innerTests)
            {
                var testName = test.Attribute("Name")?.Value ?? "Unknown";

                foreach (var m in test.Elements("Measurement"))
                {
                    var measuredValueEl = m.Element("MeasuredValue");
                    var valueEl = measuredValueEl?.Element("Value")?.Element("StringValue");

                    string? errorUpper = null, errorLower = null, warnUpper = null, warnLower = null;

                    // Parse spec ranges
                    foreach (var spec in measuredValueEl?.Elements("Spec") ?? Enumerable.Empty<XElement>())
                    {
                        var context = spec.Attribute("Context")?.Value;
                        var interval = spec.Element("Range")?.Element("RangeInterval");
                        if (interval is null) continue;

                        var upper = interval.Element("UpperValue")?.Element("StringValue")?.Value;
                        var lower = interval.Element("LowerValue")?.Element("StringValue")?.Value;

                        if (context == "Error")
                        {
                            errorUpper = upper;
                            errorLower = lower;
                        }
                        else if (context == "Warning")
                        {
                            warnUpper = upper;
                            warnLower = lower;
                        }
                    }

                    measurements.Add(new Measurement
                    {
                        Id = Guid.NewGuid(),
                        TestRunId = testRunId,
                        TestName = testName,
                        MeasurementName = m.Attribute("Name")?.Value ?? "Unknown",
                        Result = m.Attribute("Result")?.Value ?? "Unknown",
                        MeasurementUnit = m.Attribute("MeasurementUnit")?.Value,
                        Description = m.Attribute("Description")?.Value,
                        MeasuredValue = valueEl?.Value,
                        SpecErrorUpper = errorUpper,
                        SpecErrorLower = errorLower,
                        SpecWarningUpper = warnUpper,
                        SpecWarningLower = warnLower
                    });
                }
            }
        }

        return measurements;
    }

    private static DateTime? ParseDateTime(string? value) =>
        DateTime.TryParse(value, out var dt) ? dt.ToUniversalTime() : null;

    private static int ParseInt(string? value) =>
        int.TryParse(value, out var n) ? n : 0;

    /// <summary>
    /// NUnit 3 uses duration in seconds (e.g. "197.780999")
    /// </summary>
    private static TimeSpan ParseDurationSeconds(string? value) =>
        double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var secs)
            ? TimeSpan.FromSeconds(secs)
            : TimeSpan.Zero;
}
