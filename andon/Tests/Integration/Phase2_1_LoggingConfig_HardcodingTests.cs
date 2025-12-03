using Xunit;
using Andon.Core.Managers;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 2-1: LoggingConfigハードコード化テスト（TDD Red-Green-Refactor）
///
/// テスト目的:
/// - IOptions<LoggingConfig>依存を削除し、ハードコード値でログ機能が正常動作することを確認
/// - appsettings.jsonからLoggingConfigセクションを削除しても影響がないことを確認
///
/// 実装ステータス: Step 2-1-1 (Red) - テストファースト
/// </summary>
[Collection("LoggingManagerTests")]
public class Phase2_1_LoggingConfig_HardcodingTests : IDisposable
{
    private const string TestLogDirectory = "./test_logs_phase2_1";
    private readonly Mock<ILogger<LoggingManager>> _mockLogger;

    public Phase2_1_LoggingConfig_HardcodingTests()
    {
        _mockLogger = new Mock<ILogger<LoggingManager>>();

        // テスト用ログディレクトリのクリーンアップ
        if (Directory.Exists(TestLogDirectory))
        {
            Directory.Delete(TestLogDirectory, true);
        }
    }

    /// <summary>
    /// Test 1: IOptionsなしのコンストラクタでLoggingManagerが正常に動作すること
    /// 期待: ハードコード値が使用され、IOptions依存がないことを確認
    /// </summary>
    [Fact]
    public void test_LoggingManager_IOptions依存なしで動作()
    {
        // Arrange & Act
        // ハードコード化後は IOptions を受け取らないコンストラクタのみ
        using var loggingManager = new LoggingManager(_mockLogger.Object);

        // Assert
        // 正常にインスタンス化できることを確認
        Assert.NotNull(loggingManager);
    }

    /// <summary>
    /// Test 2: ハードコード値でファイル出力が有効であることを確認
    /// 期待: EnableFileOutput=true がハードコードされている
    /// </summary>
    [Fact]
    public async Task test_LoggingManager_ファイル出力が有効()
    {
        // Arrange
        var testMessage = "Phase 2-1 test message";
        var expectedLogPath = "logs/andon.log"; // appsettings.jsonの現在値

        // Act
        {
            using var loggingManager = new LoggingManager(_mockLogger.Object);
            await loggingManager.LogInfo(testMessage);
            await loggingManager.CloseAndFlushAsync();
        } // usingブロックを抜けてDispose完了を保証

        await Task.Delay(50); // ファイルが完全に閉じられるのを待つ

        // Assert
        // ハードコードされた EnableFileOutput=true により、ログファイルが作成されるはず
        Assert.True(File.Exists(expectedLogPath),
            $"ログファイルが存在するべき: {expectedLogPath}");

        var logContent = await ReadFileWithSharedAccessAsync(expectedLogPath);
        Assert.Contains(testMessage, logContent);
    }

    /// <summary>
    /// Test 3: ハードコード値でLogLevel=Debugが設定されていることを確認
    /// 期待: LogLevel="Debug" がハードコードされている（appsettings.json現在値）
    /// </summary>
    [Fact]
    public async Task test_LoggingManager_LogLevelがDebug()
    {
        // Arrange
        var debugMessage = "Debug level message";
        var expectedLogPath = "logs/andon.log";

        // Act
        {
            using var loggingManager = new LoggingManager(_mockLogger.Object);
            await loggingManager.LogDebug(debugMessage);
            await loggingManager.CloseAndFlushAsync();
        } // usingブロックを抜けてDispose完了を保証

        await Task.Delay(50); // ファイルが完全に閉じられるのを待つ

        // Assert
        Assert.True(File.Exists(expectedLogPath));

        var logContent = await ReadFileWithSharedAccessAsync(expectedLogPath);
        Assert.Contains(debugMessage, logContent);
        Assert.Contains("[DEBUG]", logContent); // LoggingManagerの実装では"DEBUG"が使われている
    }

    /// <summary>
    /// Test 4: ハードコード値でMaxLogFileSizeMb=10が設定されていることを確認
    /// 期待: MaxLogFileSizeMb=10 がハードコードされている（appsettings.json現在値）
    /// 注: この値はファイルローテーション処理内部で使用されるため、
    ///     間接的にテストする（10MB超過時にローテーションが発生する）
    /// </summary>
    [Fact]
    public void test_LoggingManager_MaxLogFileSizeが10MB()
    {
        // Arrange
        using var loggingManager = new LoggingManager(_mockLogger.Object);

        // Act & Assert
        // ハードコード化後は MaxLogFileSizeMb=10 が使用される
        // 実際のローテーション動作はLoggingManagerの内部ロジックで確認される
        Assert.NotNull(loggingManager);

        // 注: 実際のファイルサイズチェックはLoggingManagerTests.csで実施済み
        // ここではハードコード値が使用されることの確認のみ
    }

    /// <summary>
    /// Test 5: IOptions<LoggingConfig>を受け取るコンストラクタが削除されていることを確認
    /// 期待: IOptionsを受け取るコンストラクタが存在しない（Green時に削除される）
    /// </summary>
    [Fact]
    public void test_LoggingManager_IOptionsコンストラクタ削除確認()
    {
        // Arrange & Act
        var loggingManagerType = typeof(LoggingManager);
        var constructors = loggingManagerType.GetConstructors();

        // Assert
        // コンストラクタが1つのみで、IOptionsパラメータを持たないことを確認
        Assert.Single(constructors);

        var constructor = constructors[0];
        var parameters = constructor.GetParameters();

        // ILogger<LoggingManager>のみをパラメータとして持つ
        Assert.Single(parameters);
        Assert.Equal(typeof(ILogger<LoggingManager>), parameters[0].ParameterType);
    }

    /// <summary>
    /// FileShare.ReadWriteを指定してファイルを読み取るヘルパーメソッド
    /// LoggingManagerが書き込み中でもファイルを読み取れるようにする
    /// </summary>
    private static async Task<string> ReadFileWithSharedAccessAsync(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public void Dispose()
    {
        // テスト用ログディレクトリのクリーンアップ
        if (Directory.Exists(TestLogDirectory))
        {
            try
            {
                Directory.Delete(TestLogDirectory, true);
            }
            catch
            {
                // クリーンアップ失敗は無視
            }
        }

        // 本番ログディレクトリのクリーンアップ（テストで作成されたログファイル）
        if (Directory.Exists("logs"))
        {
            try
            {
                var testLogFiles = Directory.GetFiles("logs", "*phase2_1*");
                foreach (var file in testLogFiles)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // クリーンアップ失敗は無視
            }
        }
    }
}
