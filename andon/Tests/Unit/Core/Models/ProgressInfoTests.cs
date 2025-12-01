using Andon.Core.Models;
using Xunit;

namespace Andon.Tests.Unit.Core.Models;

public class ProgressInfoTests
{
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var currentStep = "Step3";
        var progress = 0.5;
        var message = "Processing...";
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act
        var progressInfo = new ProgressInfo(currentStep, progress, message, elapsedTime);

        // Assert
        Assert.Equal(currentStep, progressInfo.CurrentStep);
        Assert.Equal(progress, progressInfo.Progress);
        Assert.Equal(message, progressInfo.Message);
        Assert.Equal(elapsedTime, progressInfo.ElapsedTime);
        Assert.Null(progressInfo.EstimatedTimeRemaining);
        Assert.True((DateTime.Now - progressInfo.ReportedAt).TotalSeconds < 1);
    }

    [Fact]
    public void Constructor_WithEstimatedTime_CreatesInstance()
    {
        // Arrange
        var currentStep = "Step4";
        var progress = 0.75;
        var message = "Almost done...";
        var elapsedTime = TimeSpan.FromSeconds(30);
        var estimatedTimeRemaining = TimeSpan.FromSeconds(10);

        // Act
        var progressInfo = new ProgressInfo(currentStep, progress, message, elapsedTime, estimatedTimeRemaining);

        // Assert
        Assert.Equal(currentStep, progressInfo.CurrentStep);
        Assert.Equal(progress, progressInfo.Progress);
        Assert.Equal(message, progressInfo.Message);
        Assert.Equal(elapsedTime, progressInfo.ElapsedTime);
        Assert.Equal(estimatedTimeRemaining, progressInfo.EstimatedTimeRemaining);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidCurrentStep_ThrowsArgumentException(string invalidStep)
    {
        // Arrange
        var progress = 0.5;
        var message = "Test message";
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ProgressInfo(invalidStep, progress, message, elapsedTime));
        Assert.Contains("CurrentStep", exception.Message);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_InvalidProgress_ThrowsArgumentOutOfRangeException(double invalidProgress)
    {
        // Arrange
        var currentStep = "Step1";
        var message = "Test message";
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProgressInfo(currentStep, invalidProgress, message, elapsedTime));
        Assert.Contains("Progress must be between 0.0 and 1.0", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidMessage_ThrowsArgumentException(string invalidMessage)
    {
        // Arrange
        var currentStep = "Step2";
        var progress = 0.5;
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new ProgressInfo(currentStep, progress, invalidMessage, elapsedTime));
        Assert.Contains("Message", exception.Message);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void Constructor_ValidProgressRange_CreatesInstance(double validProgress)
    {
        // Arrange
        var currentStep = "Step3";
        var message = "Test message";
        var elapsedTime = TimeSpan.FromSeconds(10);

        // Act
        var progressInfo = new ProgressInfo(currentStep, validProgress, message, elapsedTime);

        // Assert
        Assert.Equal(validProgress, progressInfo.Progress);
    }
}
