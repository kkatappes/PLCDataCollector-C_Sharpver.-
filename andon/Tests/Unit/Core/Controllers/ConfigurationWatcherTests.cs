using Xunit;
using System.IO;

namespace Andon.Tests.Unit.Core.Controllers;

/// <summary>
/// ConfigurationWatcher単体テスト
/// TDDサイクル: Red Phase
/// </summary>
public class ConfigurationWatcherTests
{
    private readonly string _testConfigDir = Path.Combine(Path.GetTempPath(), "andon_test_config");

    public ConfigurationWatcherTests()
    {
        // テスト用ディレクトリ作成
        if (Directory.Exists(_testConfigDir))
        {
            Directory.Delete(_testConfigDir, true);
        }
        Directory.CreateDirectory(_testConfigDir);
    }

    /// <summary>
    /// StartWatchingテスト: 設定ファイル変更時にイベント発火
    /// </summary>
    [Fact]
    public async Task StartWatching_設定ファイル変更時にイベント発火()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();
        bool eventRaised = false;
        string? changedFile = null;

        watcher.OnConfigurationChanged += (sender, args) =>
        {
            eventRaised = true;
            changedFile = args.FilePath;
        };

        var testFile = Path.Combine(_testConfigDir, "test.json");

        // Act
        watcher.StartWatching(_testConfigDir);
        await File.WriteAllTextAsync(testFile, "{\"test\": \"data\"}");
        await Task.Delay(500); // イベント発火待機

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(changedFile);
        Assert.Contains("test.json", changedFile);

        // Cleanup
        watcher.StopWatching();
    }

    /// <summary>
    /// StartWatchingテスト: JSONファイルのみ監視
    /// </summary>
    [Fact]
    public async Task StartWatching_JSONファイルのみ監視()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();
        int eventCount = 0;

        watcher.OnConfigurationChanged += (sender, args) =>
        {
            eventCount++;
        };

        var jsonFile = Path.Combine(_testConfigDir, "config.json");
        var txtFile = Path.Combine(_testConfigDir, "readme.txt");

        // Act
        watcher.StartWatching(_testConfigDir);
        await File.WriteAllTextAsync(jsonFile, "{}");
        await File.WriteAllTextAsync(txtFile, "text");
        await Task.Delay(500); // イベント発火待機

        // Assert
        Assert.Equal(1, eventCount); // JSONファイルのみカウント

        // Cleanup
        watcher.StopWatching();
    }

    /// <summary>
    /// StopWatchingテスト: 監視停止後はイベント発火しない
    /// </summary>
    [Fact]
    public async Task StopWatching_監視停止後はイベント発火しない()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();
        bool eventRaised = false;

        watcher.OnConfigurationChanged += (sender, args) =>
        {
            eventRaised = true;
        };

        var testFile = Path.Combine(_testConfigDir, "test.json");

        watcher.StartWatching(_testConfigDir);

        // Act
        watcher.StopWatching();
        await File.WriteAllTextAsync(testFile, "{}");
        await Task.Delay(500);

        // Assert
        Assert.False(eventRaised);
    }

    /// <summary>
    /// IsWatchingテスト: 監視状態を正しく返す
    /// </summary>
    [Fact]
    public void IsWatching_監視状態を正しく返す()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();

        // Assert - 初期状態
        Assert.False(watcher.IsWatching);

        // Act - 監視開始
        watcher.StartWatching(_testConfigDir);

        // Assert - 監視中
        Assert.True(watcher.IsWatching);

        // Act - 監視停止
        watcher.StopWatching();

        // Assert - 監視停止
        Assert.False(watcher.IsWatching);
    }

    /// <summary>
    /// Disposeテスト: リソース解放
    /// </summary>
    [Fact]
    public void Dispose_リソース解放()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();
        watcher.StartWatching(_testConfigDir);

        // Act
        watcher.Dispose();

        // Assert
        Assert.False(watcher.IsWatching);
    }

    // ========== Phase3 Part7: Excelファイル監視対応テスト ==========

    /// <summary>
    /// StartWatching_Excelファイル監視: *.xlsx ファイル変更時にイベント発火
    /// </summary>
    [Fact]
    public async Task StartWatching_Excelファイル監視_変更時にイベント発火()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();
        bool eventRaised = false;
        string? changedFile = null;

        watcher.OnConfigurationChanged += (sender, args) =>
        {
            eventRaised = true;
            changedFile = args.FilePath;
        };

        var testFile = Path.Combine(_testConfigDir, "5JRS_N2.xlsx");

        // Act
        watcher.StartWatchingExcel(_testConfigDir);
        await File.WriteAllTextAsync(testFile, "dummy excel content");
        await Task.Delay(500); // イベント発火待機

        // Assert
        Assert.True(eventRaised);
        Assert.NotNull(changedFile);
        Assert.Contains(".xlsx", changedFile);

        // Cleanup
        watcher.StopWatching();
    }

    /// <summary>
    /// StartWatching_Excelファイル監視: Excelファイルのみ監視（JSON無視）
    /// </summary>
    [Fact]
    public async Task StartWatching_Excelファイル監視_Excelのみ監視()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();
        int eventCount = 0;

        watcher.OnConfigurationChanged += (sender, args) =>
        {
            eventCount++;
        };

        var xlsxFile = Path.Combine(_testConfigDir, "config.xlsx");
        var jsonFile = Path.Combine(_testConfigDir, "config.json");

        // Act
        watcher.StartWatchingExcel(_testConfigDir);
        await File.WriteAllTextAsync(xlsxFile, "dummy excel");
        await File.WriteAllTextAsync(jsonFile, "{}");
        await Task.Delay(500);

        // Assert
        Assert.Equal(1, eventCount); // Excelファイルのみカウント

        // Cleanup
        watcher.StopWatching();
    }

    /// <summary>
    /// IsWatchingExcel: Excel監視状態を正しく返す
    /// </summary>
    [Fact]
    public void IsWatchingExcel_監視状態を正しく返す()
    {
        // Arrange
        var watcher = new Andon.Core.Controllers.ConfigurationWatcher();

        // Assert - 初期状態
        Assert.False(watcher.IsWatching);

        // Act - Excel監視開始
        watcher.StartWatchingExcel(_testConfigDir);

        // Assert - 監視中
        Assert.True(watcher.IsWatching);

        // Act - 監視停止
        watcher.StopWatching();

        // Assert - 監視停止
        Assert.False(watcher.IsWatching);
    }
}
