using Xunit;

namespace Andon.Tests.Unit;

/// <summary>
/// CommandLineOptions単体テスト
/// TDDサイクル: Red Phase
/// </summary>
public class CommandLineOptionsTests
{
    /// <summary>
    /// コンストラクタテスト: デフォルト値
    /// </summary>
    [Fact]
    public void Constructor_デフォルト値が正しく設定される()
    {
        // Act
        var options = new CommandLineOptions();

        // Assert
        Assert.Equal("./config/", options.ConfigPath);
        Assert.False(options.ShowVersion);
        Assert.False(options.ShowHelp);
    }

    /// <summary>
    /// Parseテスト: ConfigPathオプション（--config）
    /// </summary>
    [Fact]
    public void Parse_ConfigPathオプション_正しくパースされる()
    {
        // Arrange
        var args = new[] { "--config", "/custom/path/" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("/custom/path/", options.ConfigPath);
    }

    /// <summary>
    /// Parseテスト: ConfigPathオプション（短縮形 -c）
    /// </summary>
    [Fact]
    public void Parse_ConfigPathオプション短縮形_正しくパースされる()
    {
        // Arrange
        var args = new[] { "-c", "/another/path/" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("/another/path/", options.ConfigPath);
    }

    /// <summary>
    /// Parseテスト: Versionオプション（--version）
    /// </summary>
    [Fact]
    public void Parse_Versionオプション_正しくパースされる()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.True(options.ShowVersion);
    }

    /// <summary>
    /// Parseテスト: Versionオプション（短縮形 -v）
    /// </summary>
    [Fact]
    public void Parse_Versionオプション短縮形_正しくパースされる()
    {
        // Arrange
        var args = new[] { "-v" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.True(options.ShowVersion);
    }

    /// <summary>
    /// Parseテスト: Helpオプション（--help）
    /// </summary>
    [Fact]
    public void Parse_Helpオプション_正しくパースされる()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.True(options.ShowHelp);
    }

    /// <summary>
    /// Parseテスト: Helpオプション（短縮形 -h）
    /// </summary>
    [Fact]
    public void Parse_Helpオプション短縮形_正しくパースされる()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.True(options.ShowHelp);
    }

    /// <summary>
    /// Parseテスト: 複数オプション組み合わせ
    /// </summary>
    [Fact]
    public void Parse_複数オプション_正しくパースされる()
    {
        // Arrange
        var args = new[] { "--config", "/test/path/", "--version" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("/test/path/", options.ConfigPath);
        Assert.True(options.ShowVersion);
    }

    /// <summary>
    /// Parseテスト: 引数なし（デフォルト値）
    /// </summary>
    [Fact]
    public void Parse_引数なし_デフォルト値が返される()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("./config/", options.ConfigPath);
        Assert.False(options.ShowVersion);
        Assert.False(options.ShowHelp);
    }

    /// <summary>
    /// Parseテスト: 不明なオプション（無視される）
    /// </summary>
    [Fact]
    public void Parse_不明なオプション_無視される()
    {
        // Arrange
        var args = new[] { "--unknown", "value" };

        // Act
        var options = CommandLineOptions.Parse(args);

        // Assert
        Assert.Equal("./config/", options.ConfigPath); // デフォルト値
    }

    /// <summary>
    /// GetHelpMessageテスト: ヘルプメッセージ取得
    /// </summary>
    [Fact]
    public void GetHelpMessage_ヘルプメッセージが返される()
    {
        // Act
        var helpMessage = CommandLineOptions.GetHelpMessage();

        // Assert
        Assert.NotNull(helpMessage);
        Assert.Contains("Usage:", helpMessage);
        Assert.Contains("--config", helpMessage);
        Assert.Contains("--version", helpMessage);
        Assert.Contains("--help", helpMessage);
    }

    /// <summary>
    /// GetVersionMessageテスト: バージョン情報取得
    /// </summary>
    [Fact]
    public void GetVersionMessage_バージョン情報が返される()
    {
        // Act
        var versionMessage = CommandLineOptions.GetVersionMessage();

        // Assert
        Assert.NotNull(versionMessage);
        Assert.Contains("Andon", versionMessage);
        Assert.Contains("Version", versionMessage);
    }
}
