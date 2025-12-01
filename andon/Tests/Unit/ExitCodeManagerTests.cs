using Xunit;

namespace Andon.Tests.Unit;

/// <summary>
/// ExitCodeManager単体テスト
/// TDDサイクル: Red Phase
/// </summary>
public class ExitCodeManagerTests
{
    /// <summary>
    /// 終了コード変換テスト: TimeoutException → TimeoutError
    /// </summary>
    [Fact]
    public void FromException_TimeoutException_ReturnsTimeoutError()
    {
        // Arrange
        var ex = new TimeoutException("Connection timeout");

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.TimeoutError, exitCode);
    }

    /// <summary>
    /// 終了コード変換テスト: SocketException → ConnectionError
    /// </summary>
    [Fact]
    public void FromException_SocketException_ReturnsConnectionError()
    {
        // Arrange
        var ex = new System.Net.Sockets.SocketException();

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.ConnectionError, exitCode);
    }

    /// <summary>
    /// 終了コード変換テスト: MultiConfigLoadException → ConfigurationError
    /// </summary>
    [Fact]
    public void FromException_MultiConfigLoadException_ReturnsConfigurationError()
    {
        // Arrange
        var ex = new Andon.Core.Exceptions.MultiConfigLoadException("Config load failed");

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.ConfigurationError, exitCode);
    }

    /// <summary>
    /// 終了コード変換テスト: InvalidOperationException → DataProcessingError
    /// </summary>
    [Fact]
    public void FromException_InvalidOperationException_ReturnsDataProcessingError()
    {
        // Arrange
        var ex = new InvalidOperationException("Data processing failed");

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.DataProcessingError, exitCode);
    }

    /// <summary>
    /// 終了コード変換テスト: IOException → NetworkError
    /// </summary>
    [Fact]
    public void FromException_IOException_ReturnsNetworkError()
    {
        // Arrange
        var ex = new System.IO.IOException("Network I/O error");

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.NetworkError, exitCode);
    }

    /// <summary>
    /// 終了コード変換テスト: ArgumentNullException → ValidationError
    /// </summary>
    [Fact]
    public void FromException_ArgumentNullException_ReturnsValidationError()
    {
        // Arrange
        var ex = new ArgumentNullException("parameter");

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.ValidationError, exitCode);
    }

    /// <summary>
    /// 終了コード変換テスト: 汎用Exception → UnknownError
    /// </summary>
    [Fact]
    public void FromException_GenericException_ReturnsUnknownError()
    {
        // Arrange
        var ex = new Exception("Unknown error");

        // Act
        var exitCode = ExitCodeManager.FromException(ex);

        // Assert
        Assert.Equal(ExitCodeManager.UnknownError, exitCode);
    }

    /// <summary>
    /// 終了コード定数テスト: Success = 0
    /// </summary>
    [Fact]
    public void Success_Equals0()
    {
        Assert.Equal(0, ExitCodeManager.Success);
    }

    /// <summary>
    /// 終了コード定数テスト: ConfigurationError = 1
    /// </summary>
    [Fact]
    public void ConfigurationError_Equals1()
    {
        Assert.Equal(1, ExitCodeManager.ConfigurationError);
    }

    /// <summary>
    /// 終了コード定数テスト: ConnectionError = 2
    /// </summary>
    [Fact]
    public void ConnectionError_Equals2()
    {
        Assert.Equal(2, ExitCodeManager.ConnectionError);
    }

    /// <summary>
    /// 終了コード定数テスト: TimeoutError = 3
    /// </summary>
    [Fact]
    public void TimeoutError_Equals3()
    {
        Assert.Equal(3, ExitCodeManager.TimeoutError);
    }

    /// <summary>
    /// 終了コード定数テスト: DataProcessingError = 4
    /// </summary>
    [Fact]
    public void DataProcessingError_Equals4()
    {
        Assert.Equal(4, ExitCodeManager.DataProcessingError);
    }

    /// <summary>
    /// 終了コード定数テスト: ValidationError = 5
    /// </summary>
    [Fact]
    public void ValidationError_Equals5()
    {
        Assert.Equal(5, ExitCodeManager.ValidationError);
    }

    /// <summary>
    /// 終了コード定数テスト: NetworkError = 6
    /// </summary>
    [Fact]
    public void NetworkError_Equals6()
    {
        Assert.Equal(6, ExitCodeManager.NetworkError);
    }

    /// <summary>
    /// 終了コード定数テスト: UnknownError = 99
    /// </summary>
    [Fact]
    public void UnknownError_Equals99()
    {
        Assert.Equal(99, ExitCodeManager.UnknownError);
    }
}
