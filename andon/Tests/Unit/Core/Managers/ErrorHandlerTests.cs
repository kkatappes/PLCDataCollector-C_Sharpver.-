using Xunit;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// ErrorHandler単体テスト
/// </summary>
public class ErrorHandlerTests
{
    /// <summary>
    /// エラー分類テスト: TimeoutException
    /// </summary>
    [Fact]
    public void DetermineErrorCategory_TimeoutException_ReturnsTimeout()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();
        var ex = new TimeoutException("Connection timeout");

        // Act
        var category = errorHandler.DetermineErrorCategory(ex);

        // Assert
        Assert.Equal(Andon.Core.Constants.ErrorCategory.Timeout, category);
    }

    /// <summary>
    /// エラー分類テスト: SocketException
    /// </summary>
    [Fact]
    public void DetermineErrorCategory_SocketException_ReturnsConnection()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();
        var ex = new System.Net.Sockets.SocketException();

        // Act
        var category = errorHandler.DetermineErrorCategory(ex);

        // Assert
        Assert.Equal(Andon.Core.Constants.ErrorCategory.Connection, category);
    }

    /// <summary>
    /// エラー分類テスト: MultiConfigLoadException
    /// </summary>
    [Fact]
    public void DetermineErrorCategory_MultiConfigLoadException_ReturnsConfiguration()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();
        var ex = new Andon.Core.Exceptions.MultiConfigLoadException("Config load failed");

        // Act
        var category = errorHandler.DetermineErrorCategory(ex);

        // Assert
        Assert.Equal(Andon.Core.Constants.ErrorCategory.Configuration, category);
    }

    /// <summary>
    /// エラー分類テスト: InvalidOperationException
    /// </summary>
    [Fact]
    public void DetermineErrorCategory_InvalidOperationException_ReturnsDataProcessing()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();
        var ex = new InvalidOperationException("Data processing failed");

        // Act
        var category = errorHandler.DetermineErrorCategory(ex);

        // Assert
        Assert.Equal(Andon.Core.Constants.ErrorCategory.DataProcessing, category);
    }

    /// <summary>
    /// エラー分類テスト: Exception（汎用）
    /// </summary>
    [Fact]
    public void DetermineErrorCategory_GenericException_ReturnsUnknown()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();
        var ex = new Exception("Unknown error");

        // Act
        var category = errorHandler.DetermineErrorCategory(ex);

        // Assert
        Assert.Equal(Andon.Core.Constants.ErrorCategory.Unknown, category);
    }

    /// <summary>
    /// リトライポリシーテスト: Timeout - リトライ可能
    /// </summary>
    [Fact]
    public void ShouldRetry_TimeoutCategory_ReturnsTrue()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();

        // Act
        var shouldRetry = errorHandler.ShouldRetry(Andon.Core.Constants.ErrorCategory.Timeout);

        // Assert
        Assert.True(shouldRetry);
    }

    /// <summary>
    /// リトライポリシーテスト: Connection - リトライ可能
    /// </summary>
    [Fact]
    public void ShouldRetry_ConnectionCategory_ReturnsTrue()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();

        // Act
        var shouldRetry = errorHandler.ShouldRetry(Andon.Core.Constants.ErrorCategory.Connection);

        // Assert
        Assert.True(shouldRetry);
    }

    /// <summary>
    /// リトライポリシーテスト: Configuration - リトライ不可
    /// </summary>
    [Fact]
    public void ShouldRetry_ConfigurationCategory_ReturnsFalse()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();

        // Act
        var shouldRetry = errorHandler.ShouldRetry(Andon.Core.Constants.ErrorCategory.Configuration);

        // Assert
        Assert.False(shouldRetry);
    }

    /// <summary>
    /// リトライ回数テスト: Timeout
    /// </summary>
    [Fact]
    public void GetMaxRetryCount_TimeoutCategory_Returns3()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();

        // Act
        var retryCount = errorHandler.GetMaxRetryCount(Andon.Core.Constants.ErrorCategory.Timeout);

        // Assert
        Assert.Equal(3, retryCount);
    }

    /// <summary>
    /// リトライ遅延時間テスト: Connection
    /// </summary>
    [Fact]
    public void GetRetryDelayMs_ConnectionCategory_Returns2000()
    {
        // Arrange
        var errorHandler = new Andon.Core.Managers.ErrorHandler();

        // Act
        var delayMs = errorHandler.GetRetryDelayMs(Andon.Core.Constants.ErrorCategory.Connection);

        // Assert
        Assert.Equal(2000, delayMs);
    }
}
