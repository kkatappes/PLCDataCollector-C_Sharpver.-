using Andon.Core.Models;
using Xunit;

namespace Andon.Tests.Unit.Core.Models;

public class ParallelProgressInfoTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var currentStep = "ParallelExecution";
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.3 },
            { "PLC2", 0.5 },
            { "PLC3", 0.8 }
        };
        var elapsedTime = TimeSpan.FromSeconds(20);

        // Act
        var progressInfo = new ParallelProgressInfo(currentStep, plcProgresses, elapsedTime);

        // Assert
        Assert.Equal(currentStep, progressInfo.CurrentStep);
        Assert.Equal(elapsedTime, progressInfo.ElapsedTime);
        Assert.Equal(3, progressInfo.PlcProgresses.Count);
        Assert.Equal(3, progressInfo.ActivePlcCount);
        Assert.Equal(0, progressInfo.CompletedPlcCount);
        Assert.Equal(0, progressInfo.FailedPlcCount);
        Assert.Equal((0.3 + 0.5 + 0.8) / 3.0, progressInfo.OverallProgress, precision: 5);
    }

    [Fact]
    public void Constructor_EmptyPlcProgresses_CreatesInstance()
    {
        // Arrange
        var currentStep = "ParallelExecution";
        var plcProgresses = new Dictionary<string, double>();
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act
        var progressInfo = new ParallelProgressInfo(currentStep, plcProgresses, elapsedTime);

        // Assert
        Assert.Empty(progressInfo.PlcProgresses);
        Assert.Equal(0, progressInfo.ActivePlcCount);
        Assert.Equal(0, progressInfo.CompletedPlcCount);
        Assert.Equal(0.0, progressInfo.OverallProgress);
    }

    [Fact]
    public void Constructor_NullPlcProgresses_ThrowsArgumentNullException()
    {
        // Arrange
        var currentStep = "ParallelExecution";
        Dictionary<string, double>? plcProgresses = null;
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ParallelProgressInfo(currentStep, plcProgresses!, elapsedTime));
        Assert.Contains("plcProgresses", exception.Message);
    }

    [Fact]
    public void UpdatePlcProgress_ValidProgress_UpdatesCorrectly()
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.3 },
            { "PLC2", 0.5 }
        };
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Act
        progressInfo.UpdatePlcProgress("PLC1", 0.8);

        // Assert
        Assert.Equal(0.8, progressInfo.PlcProgresses["PLC1"]);
        Assert.Equal((0.8 + 0.5) / 2.0, progressInfo.OverallProgress, precision: 5);
    }

    [Fact]
    public void UpdatePlcProgress_NewPlc_AddsAndUpdates()
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.5 }
        };
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Act
        progressInfo.UpdatePlcProgress("PLC2", 0.7);

        // Assert
        Assert.Equal(2, progressInfo.PlcProgresses.Count);
        Assert.Equal(0.7, progressInfo.PlcProgresses["PLC2"]);
        Assert.Equal((0.5 + 0.7) / 2.0, progressInfo.OverallProgress, precision: 5);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePlcProgress_InvalidPlcId_ThrowsArgumentException(string invalidPlcId)
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double> { { "PLC1", 0.5 } };
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            progressInfo.UpdatePlcProgress(invalidPlcId, 0.8));
        Assert.Contains("PlcId", exception.Message);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void UpdatePlcProgress_InvalidProgress_ThrowsArgumentOutOfRangeException(double invalidProgress)
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double> { { "PLC1", 0.5 } };
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            progressInfo.UpdatePlcProgress("PLC1", invalidProgress));
        Assert.Contains("Progress must be between 0.0 and 1.0", exception.Message);
    }

    [Fact]
    public void UpdatePlcProgress_CompletedPlc_UpdatesCompletedCount()
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.5 },
            { "PLC2", 0.8 }
        };
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Act
        progressInfo.UpdatePlcProgress("PLC1", 1.0);

        // Assert
        Assert.Equal(1, progressInfo.CompletedPlcCount);
        Assert.Equal(1, progressInfo.ActivePlcCount); // PLC2 is still active
    }

    [Fact]
    public void UpdatePlcProgress_AllCompleted_UpdatesCorrectly()
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.8 },
            { "PLC2", 0.9 }
        };
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Act
        progressInfo.UpdatePlcProgress("PLC1", 1.0);
        progressInfo.UpdatePlcProgress("PLC2", 1.0);

        // Assert
        Assert.Equal(2, progressInfo.CompletedPlcCount);
        Assert.Equal(0, progressInfo.ActivePlcCount);
        Assert.Equal(1.0, progressInfo.OverallProgress);
    }

    [Fact]
    public void OverallProgress_CalculatesAverage_Correctly()
    {
        // Arrange
        var plcProgresses = new Dictionary<string, double>
        {
            { "PLC1", 0.2 },
            { "PLC2", 0.4 },
            { "PLC3", 0.6 },
            { "PLC4", 0.8 }
        };

        // Act
        var progressInfo = new ParallelProgressInfo("Step", plcProgresses, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal((0.2 + 0.4 + 0.6 + 0.8) / 4.0, progressInfo.OverallProgress, precision: 5);
    }
}
