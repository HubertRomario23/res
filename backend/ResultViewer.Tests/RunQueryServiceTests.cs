using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ResultViewer.Application.Services;
using ResultViewer.Domain.Entities;
using ResultViewer.Domain.Interfaces;
using Xunit;

namespace ResultViewer.Tests;

public class RunQueryServiceTests
{
    private readonly Mock<ITestRunRepository> _repoMock = new();
    private readonly Mock<IFileSystemService> _fileSystemMock = new();
    private readonly Mock<IXmlParserService> _xmlParserMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<ILogger<RunQueryService>> _loggerMock = new();
    private readonly RunQueryService _sut;

    public RunQueryServiceTests()
    {
        _sut = new RunQueryService(
            _repoMock.Object,
            _fileSystemMock.Object,
            _xmlParserMock.Object,
            _cache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetTestRunAsync_WhenFoundInDb_ReturnsDto()
    {
        // Arrange
        var entity = CreateTestRun();
        _repoMock.Setup(r => r.GetByCompositeKeyAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        // Act
        var result = await _sut.GetTestRunAsync("hostA", "pdcA", "run1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("hostA", result!.Host);
        Assert.Equal("pdcA", result.Pdc);
        Assert.Equal("run1", result.RunId);

        // Verify filesystem was never called
        _fileSystemMock.Verify(f => f.ExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTestRunAsync_WhenNotInDb_FallsBackToFileSystem()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByCompositeKeyAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((TestRun?)null);

        _fileSystemMock.Setup(f => f.ExistsAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);

        _fileSystemMock.Setup(f => f.LoadRawRunAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new RawRunData("<test-run />", null, null));

        var parsed = CreateTestRun();
        _xmlParserMock.Setup(p => p.ParseAsync(It.IsAny<RawRunData>(), "hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                      .ReturnsAsync(parsed);

        // Act
        var result = await _sut.GetTestRunAsync("hostA", "pdcA", "run1");

        // Assert
        Assert.NotNull(result);
        _repoMock.Verify(r => r.UpsertAsync(It.IsAny<TestRun>(), It.IsAny<CancellationToken>()), Times.Once);
        _fileSystemMock.Verify(f => f.ArchiveAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTestRunAsync_WhenNotInDbAndNotOnDisk_ReturnsNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByCompositeKeyAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((TestRun?)null);

        _fileSystemMock.Setup(f => f.ExistsAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                       .ReturnsAsync(false);

        // Act
        var result = await _sut.GetTestRunAsync("hostA", "pdcA", "run1");

        // Assert
        Assert.Null(result);
        _xmlParserMock.Verify(p => p.ParseAsync(It.IsAny<RawRunData>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTestRunAsync_SecondCall_ReturnsCachedResult()
    {
        // Arrange
        var entity = CreateTestRun();
        _repoMock.Setup(r => r.GetByCompositeKeyAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(entity);

        // Act
        var first = await _sut.GetTestRunAsync("hostA", "pdcA", "run1");
        var second = await _sut.GetTestRunAsync("hostA", "pdcA", "run1");

        // Assert — only one DB call, second came from cache
        _repoMock.Verify(r => r.GetByCompositeKeyAsync("hostA", "pdcA", "run1", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(first!.Host, second!.Host);
    }

    [Fact]
    public async Task GetAllRunsAsync_ReturnsPaged()
    {
        // Arrange
        var items = new List<TestRun> { CreateTestRun(), CreateTestRun() };
        _repoMock.Setup(r => r.GetAllAsync(1, 10, null, null, null, null, null, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((items as IReadOnlyList<TestRun>, 2));

        // Act
        var result = await _sut.GetAllRunsAsync(1, 10);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.TotalPages);
    }

    private static TestRun CreateTestRun() => new()
    {
        Id = Guid.NewGuid(),
        Host = "hostA",
        Pdc = "pdcA",
        RunId = "run1",
        StartTime = DateTime.UtcNow.AddHours(-1),
        EndTime = DateTime.UtcNow,
        OverallResult = "Passed",
        TestCount = 10,
        PassedCount = 9,
        FailedCount = 1,
        SkippedCount = 0,
        CreatedAt = DateTime.UtcNow,
        ImportStatus = "Imported",
        IndexedResults = new List<TestResult>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TestName = "Test1",
                Result = "Passed",
                Duration = TimeSpan.FromSeconds(1)
            }
        }
    };
}
