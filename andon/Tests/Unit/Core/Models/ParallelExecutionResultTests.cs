using Andon.Core.Models;
using Xunit;

namespace Andon.Tests.Unit.Core.Models;

public class ParallelExecutionResultTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var result = new ParallelExecutionResult();

        // Assert
        Assert.Equal(0, result.TotalPlcCount);
        Assert.Equal(0, result.SuccessfulPlcCount);
        Assert.Equal(0, result.FailedPlcCount);
        Assert.NotNull(result.PlcResults);
        Assert.Empty(result.PlcResults);
        Assert.Equal(TimeSpan.Zero, result.OverallExecutionTime);
        Assert.NotNull(result.ContinuingPlcIds);
        Assert.Empty(result.ContinuingPlcIds);
    }

    [Fact]
    public void IsOverallSuccess_ReturnsTrueWhenNoFailuresAndHasSuccess()
    {
        // Arrange
        var result = new ParallelExecutionResult
        {
            TotalPlcCount = 3,
            SuccessfulPlcCount = 3,
            FailedPlcCount = 0
        };

        // Act & Assert
        Assert.True(result.IsOverallSuccess);
    }

    [Fact]
    public void IsOverallSuccess_ReturnsFalseWhenHasFailures()
    {
        // Arrange
        var result = new ParallelExecutionResult
        {
            TotalPlcCount = 3,
            SuccessfulPlcCount = 2,
            FailedPlcCount = 1
        };

        // Act & Assert
        Assert.False(result.IsOverallSuccess);
    }

    [Fact]
    public void IsOverallSuccess_ReturnsFalseWhenNoSuccess()
    {
        // Arrange
        var result = new ParallelExecutionResult
        {
            TotalPlcCount = 3,
            SuccessfulPlcCount = 0,
            FailedPlcCount = 0
        };

        // Act & Assert
        Assert.False(result.IsOverallSuccess);
    }

    [Fact]
    public void SuccessRate_ReturnsCorrectPercentage()
    {
        // Arrange
        var result = new ParallelExecutionResult
        {
            TotalPlcCount = 4,
            SuccessfulPlcCount = 3
        };

        // Act
        var rate = result.SuccessRate;

        // Assert
        Assert.Equal(75.0, rate);
    }

    [Fact]
    public void SuccessRate_ReturnsZeroWhenTotalPlcCountIsZero()
    {
        // Arrange
        var result = new ParallelExecutionResult
        {
            TotalPlcCount = 0,
            SuccessfulPlcCount = 0
        };

        // Act
        var rate = result.SuccessRate;

        // Assert
        Assert.Equal(0.0, rate);
    }

    [Fact]
    public void PlcResults_CanAddAndRetrieveResults()
    {
        // Arrange
        var result = new ParallelExecutionResult();
        var cycleResult = new CycleExecutionResult
        {
            IsSuccess = true,
            CompletedAt = DateTime.Now
        };

        // Act
        result.PlcResults["PLC1"] = cycleResult;

        // Assert
        Assert.Single(result.PlcResults);
        Assert.Equal(cycleResult, result.PlcResults["PLC1"]);
    }

    [Fact]
    public void ContinuingPlcIds_CanAddAndRetrieveIds()
    {
        // Arrange
        var result = new ParallelExecutionResult();

        // Act
        result.ContinuingPlcIds.Add("PLC1");
        result.ContinuingPlcIds.Add("PLC2");

        // Assert
        Assert.Equal(2, result.ContinuingPlcIds.Count);
        Assert.Contains("PLC1", result.ContinuingPlcIds);
        Assert.Contains("PLC2", result.ContinuingPlcIds);
    }

    [Fact]
    public void OverallExecutionTime_CanBeSet()
    {
        // Arrange
        var result = new ParallelExecutionResult();
        var duration = TimeSpan.FromSeconds(5.5);

        // Act
        result.OverallExecutionTime = duration;

        // Assert
        Assert.Equal(duration, result.OverallExecutionTime);
    }

    [Fact]
    public void SuccessRate_ReturnsOneHundredWhenAllSuccess()
    {
        // Arrange
        var result = new ParallelExecutionResult
        {
            TotalPlcCount = 5,
            SuccessfulPlcCount = 5,
            FailedPlcCount = 0
        };

        // Act
        var rate = result.SuccessRate;

        // Assert
        Assert.Equal(100.0, rate);
    }
}
