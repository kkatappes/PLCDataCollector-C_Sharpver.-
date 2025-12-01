using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Services;
using Moq;
using Xunit;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// ProgressReporter単体テスト
/// </summary>
public class ProgressReporterTests
{
    private readonly Mock<ILoggingManager> _mockLogger;

    public ProgressReporterTests()
    {
        _mockLogger = new Mock<ILoggingManager>();
        _mockLogger.Setup(m => m.LogInfo(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public void Constructor_ValidLoggingManager_CreatesInstance()
    {
        // Act
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);

        // Assert
        Assert.NotNull(reporter);
    }

    [Fact]
    public void Constructor_NullLoggingManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ProgressReporter<ProgressInfo>(null!));
        Assert.Contains("loggingManager", exception.Message);
    }

    [Fact]
    public void Constructor_WithCustomHandler_CreatesInstance()
    {
        // Arrange
        Action<ProgressInfo> customHandler = (info) => { };

        // Act
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object, customHandler);

        // Assert
        Assert.NotNull(reporter);
    }

    [Fact]
    public void Report_ProgressInfo_LogsAndFormatsCorrectly()
    {
        // Arrange
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);
        var progressInfo = new ProgressInfo("Step3", 0.5, "Processing...", TimeSpan.FromSeconds(10));

        // Act
        reporter.Report(progressInfo);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(It.Is<string>(s =>
            s.Contains("Step3") &&
            s.Contains("50.0%") &&
            s.Contains("Processing...")
        )), Times.Once);
    }

    [Fact]
    public void Report_ParallelProgressInfo_LogsWithParallelInfo()
    {
        // Arrange
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.5 },
            { "PLC2", 0.8 }
        };
        var progressInfo = new ParallelProgressInfo("ParallelExecution", plcProgresses, TimeSpan.FromSeconds(20));

        // Act
        reporter.Report(progressInfo);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(It.Is<string>(s =>
            s.Contains("ParallelExecution") &&
            s.Contains("Active:") &&
            s.Contains("Completed:")
        )), Times.Once);
    }

    [Fact]
    public void Report_StringValue_LogsDirectly()
    {
        // Arrange
        var reporter = new ProgressReporter<string>(_mockLogger.Object);
        var message = "Test progress message";

        // Act
        reporter.Report(message);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(message), Times.Once);
    }

    [Fact]
    public void Report_NullValue_DoesNotLog()
    {
        // Arrange
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);

        // Act
        reporter.Report(null!);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Report_WithCustomHandler_InvokesHandler()
    {
        // Arrange
        var handlerInvoked = false;
        ProgressInfo? capturedInfo = null;
        Action<ProgressInfo> customHandler = (info) =>
        {
            handlerInvoked = true;
            capturedInfo = info;
        };

        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object, customHandler);
        var progressInfo = new ProgressInfo("Step4", 0.75, "Almost done...", TimeSpan.FromSeconds(30));

        // Act
        reporter.Report(progressInfo);

        // Assert
        Assert.True(handlerInvoked);
        Assert.NotNull(capturedInfo);
        Assert.Equal("Step4", capturedInfo.CurrentStep);
        Assert.Equal(0.75, capturedInfo.Progress);
    }

    [Fact]
    public void Report_ProgressInfoWithEstimatedTime_FormatsCorrectly()
    {
        // Arrange
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);
        var progressInfo = new ProgressInfo(
            "Step5",
            0.6,
            "In progress...",
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(40));

        // Act
        reporter.Report(progressInfo);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(It.Is<string>(s =>
            s.Contains("Step5") &&
            s.Contains("60.0%") &&
            s.Contains("Elapsed:") &&
            s.Contains("Remaining:")
        )), Times.Once);
    }

    [Fact]
    public void Report_MultipleProgressInfos_LogsAll()
    {
        // Arrange
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);
        var progressInfo1 = new ProgressInfo("Step1", 0.25, "Starting...", TimeSpan.FromSeconds(5));
        var progressInfo2 = new ProgressInfo("Step2", 0.5, "Halfway...", TimeSpan.FromSeconds(10));
        var progressInfo3 = new ProgressInfo("Step3", 0.75, "Almost done...", TimeSpan.FromSeconds(15));

        // Act
        reporter.Report(progressInfo1);
        reporter.Report(progressInfo2);
        reporter.Report(progressInfo3);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(It.IsAny<string>()), Times.Exactly(3));
    }

    [Fact]
    public void Report_ParallelProgressInfo_FormatsPlcCounts()
    {
        // Arrange
        var reporter = new ProgressReporter<ProgressInfo>(_mockLogger.Object);
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.3 },  // Active
            { "PLC2", 1.0 },  // Completed
            { "PLC3", 0.7 }   // Active
        };
        var progressInfo = new ParallelProgressInfo("ParallelStep", plcProgresses, TimeSpan.FromSeconds(30));

        // Act
        reporter.Report(progressInfo);

        // Assert
        _mockLogger.Verify(m => m.LogInfo(It.Is<string>(s =>
            s.Contains("Active: 2") &&
            s.Contains("Completed: 1")
        )), Times.Once);
    }
}
