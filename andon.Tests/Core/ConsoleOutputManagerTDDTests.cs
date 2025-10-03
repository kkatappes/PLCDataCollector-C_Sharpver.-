using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using SlmpClient.Core;
using Xunit;

namespace andon.Tests.Core
{
    /// <summary>
    /// コンソール出力JSON統合システム TDD実装テスト
    /// t-wadaの手法に基づくテストファースト開発
    /// RED-GREEN-REFACTORサイクルでコンソール出力JSON統合機能を実装
    /// </summary>
    public class ConsoleOutputManagerTDDTests : IDisposable
    {
        private readonly string _testLogPath;
        private readonly Mock<ILogger<ConsoleOutputManager>> _mockLogger;

        public ConsoleOutputManagerTDDTests()
        {
            // GREEN: より一意な一時ファイルパス生成（複数テストの干渉防止）
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            _testLogPath = Path.Combine(Path.GetTempPath(), $"test_console_output_{timestamp}_{uniqueId}.log");
            _mockLogger = new Mock<ILogger<ConsoleOutputManager>>();
        }

        /// <summary>
        /// RED: 情報メッセージのJSON変換テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteInfoAsync_ShouldConvertToJsonEntry_WithInfoFields()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);
            var message = "PLC接続確立";
            var category = "Connection";
            var stepNumber = 2;
            var context = new { PlcAddress = "192.168.1.100", Port = 5007 };

            // Act
            await manager.WriteInfoAsync(message, category, stepNumber, context);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100); // ファイル書き込み完了まで少し待機

            // Assert - JSON構造化されたINFOエントリを確認
            Assert.True(File.Exists(_testLogPath), "コンソール出力ログファイルが作成されていません");
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"INFO\"", logContent);
            Assert.Contains("\"Level\": \"Information\"", logContent);
            Assert.Contains("\"Category\": \"Connection\"", logContent);
            Assert.Contains("\"Message\": \"PLC接続確立\"", logContent);
            Assert.Contains("\"StepNumber\": 2", logContent);
            Assert.Contains("\"PlcAddress\"", logContent);
        }

        /// <summary>
        /// RED: 進捗メッセージのJSON変換テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteProgressAsync_ShouldConvertToJsonEntry_WithProgressFields()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);
            var message = "デバイススキャン実行中";
            var stepNumber = 4;
            var phaseInfo = "DeviceScanInProgress";
            var progressPercentage = 45.5;

            // Act
            await manager.WriteProgressAsync(message, stepNumber, phaseInfo, progressPercentage);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100);

            // Assert - JSON構造化されたPROGRESSエントリを確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"PROGRESS\"", logContent);
            Assert.Contains("\"Category\": \"Step4\"", logContent);
            Assert.Contains("\"Message\": \"デバイススキャン実行中\"", logContent);
            Assert.Contains("\"StepNumber\": 4", logContent);
            Assert.Contains("\"PhaseInfo\": \"DeviceScanInProgress\"", logContent);
            Assert.Contains("\"ProgressPercentage\": 45.5", logContent);
        }

        /// <summary>
        /// RED: 結果メッセージのJSON変換テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteResultAsync_ShouldConvertToJsonEntry_WithResultData()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);
            var message = "デバイススキャン完了";
            var stepNumber = 4;
            var phaseInfo = "DeviceScanCompleted";
            var resultData = new { ActiveDevices = 15, ScannedRange = "D100-D199", ExecutionTime = "2.5s" };

            // Act
            await manager.WriteResultAsync(message, stepNumber, phaseInfo, resultData);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100);

            // Assert - JSON構造化されたRESULTエントリを確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"RESULT\"", logContent);
            Assert.Contains("\"Category\": \"Step4\"", logContent);
            Assert.Contains("\"Message\": \"デバイススキャン完了\"", logContent);
            Assert.Contains("\"ResultData\"", logContent);
            Assert.Contains("\"ActiveDevices\": 15", logContent);
            Assert.Contains("\"ScannedRange\": \"D100-D199\"", logContent);
        }

        /// <summary>
        /// RED: エラーメッセージのJSON変換テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteErrorAsync_ShouldConvertToJsonEntry_WithErrorDetails()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);
            var message = "SLMP通信エラー: タイムアウト";
            var category = "Communication";
            var stepNumber = 4;
            var errorDetails = new { ErrorCode = "E001", Timeout = 5000, RetryCount = 3 };

            // Act
            await manager.WriteErrorAsync(message, category, stepNumber, errorDetails);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100);

            // Assert - JSON構造化されたERRORエントリを確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"ERROR\"", logContent);
            Assert.Contains("\"Level\": \"Error\"", logContent);
            Assert.Contains("\"Category\": \"Communication\"", logContent);
            Assert.Contains("\"Message\": \"SLMP通信エラー: タイムアウト\"", logContent);
            Assert.Contains("\"ErrorDetails\"", logContent);
            Assert.Contains("\"ErrorCode\": \"E001\"", logContent);
        }

        /// <summary>
        /// RED: ヘッダーメッセージのJSON変換テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task WriteHeaderAsync_ShouldConvertToJsonEntry_WithHeaderContext()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);
            var message = "=== Step4: デバイススキャン開始 ===";
            var headerType = "StepHeader";
            var context = new { StepDescription = "対応デバイスコードで網羅的スキャン、応答確認", ExpectedDuration = "30-60秒" };

            // Act
            await manager.WriteHeaderAsync(message, headerType, context);

            // GREEN: キューイングシステムによる非同期書き込み完了を待機
            await Task.Delay(100);

            // Assert - JSON構造化されたHEADERエントリを確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);
            Assert.Contains("\"EntryType\": \"HEADER\"", logContent);
            Assert.Contains("\"Category\": \"StepHeader\"", logContent);
            Assert.Contains("\"Message\": \"=== Step4: デバイススキャン開始 ===\"", logContent);
            Assert.Contains("\"AdditionalData\"", logContent);
            Assert.Contains("\"StepDescription\"", logContent);
        }

        /// <summary>
        /// RED: 複数エントリの順次処理テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task MultipleEntries_ShouldProcessInOrder_WithCorrectJsonStructure()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);

            // Act - 複数の異なるタイプのエントリを順次追加
            await manager.WriteHeaderAsync("=== テストセッション開始 ===", "SessionHeader");
            await manager.WriteInfoAsync("設定ファイル読み込み", "Configuration");
            await manager.WriteProgressAsync("接続処理中", 1, "Connecting", 25.0);
            await manager.WriteResultAsync("接続成功", 1, "Connected", new { ResponseTime = "150ms" });
            await manager.WriteErrorAsync("軽微なエラー", "Warning", 2, new { Warning = "非推奨API使用" });

            // GREEN: 全エントリの非同期書き込み完了を待機
            await Task.Delay(200);

            // Assert - 全エントリタイプが正しい順序でJSON形式で記録されていることを確認
            Assert.True(File.Exists(_testLogPath));
            var logContent = await File.ReadAllTextAsync(_testLogPath);

            // 各エントリタイプの存在確認
            Assert.Contains("\"EntryType\": \"HEADER\"", logContent);
            Assert.Contains("\"EntryType\": \"INFO\"", logContent);
            Assert.Contains("\"EntryType\": \"PROGRESS\"", logContent);
            Assert.Contains("\"EntryType\": \"RESULT\"", logContent);
            Assert.Contains("\"EntryType\": \"ERROR\"", logContent);

            // JSON構造の整合性確認
            Assert.Contains("\"SessionId\"", logContent);
            Assert.Contains("\"Timestamp\"", logContent);
            Assert.Contains("\"Level\"", logContent);
            Assert.Contains("\"Category\"", logContent);
            Assert.Contains("\"Message\"", logContent);
        }

        /// <summary>
        /// RED: セッションID生成と一貫性テスト（失敗させる）
        /// </summary>
        [Fact]
        public async Task SessionId_ShouldBeConsistent_AcrossMultipleEntries()
        {
            // Arrange
            var manager = new ConsoleOutputManager(_mockLogger.Object, _testLogPath);

            // Act - 同一セッション内で複数エントリ作成
            await manager.WriteInfoAsync("第1エントリ", "Test");
            await manager.WriteInfoAsync("第2エントリ", "Test");
            await manager.WriteInfoAsync("第3エントリ", "Test");

            // GREEN: 適切な非同期処理完了を待機
            await Task.Delay(200); // 待機時間を増加

            // GREEN: DisposeAsyncを呼び出してキューを適切に処理
            await manager.DisposeAsync();

            // 追加の安全な待機時間
            await Task.Delay(100);

            // Assert - 同一セッションID、異なるタイムスタンプで記録されていることを確認
            Assert.True(File.Exists(_testLogPath), $"ログファイルが存在しません: {_testLogPath}");

            // GREEN: ファイルが存在することを確認してから読み取り
            var fileInfo = new FileInfo(_testLogPath);
            Assert.True(fileInfo.Length > 0, "ログファイルが空です");

            var logContent = await File.ReadAllTextAsync(_testLogPath);

            // GREEN: より詳細なデバッグ情報出力
            Console.WriteLine($"ログファイルパス: {_testLogPath}");
            Console.WriteLine($"ログファイルサイズ: {fileInfo.Length} bytes");
            Console.WriteLine($"ログファイル内容長: {logContent.Length}");
            Console.WriteLine($"ログファイル全内容:\n{logContent}");

            // GREEN: 改善されたアサーション - 全体の内容をチェック
            Assert.Contains("\"SessionId\"", logContent, StringComparison.Ordinal);
            Assert.Contains("session_", logContent, StringComparison.Ordinal);
            Assert.Contains("\"EntryType\": \"INFO\"", logContent, StringComparison.Ordinal);
            Assert.Contains("第1エントリ", logContent, StringComparison.Ordinal);
            Assert.Contains("第2エントリ", logContent, StringComparison.Ordinal);
            Assert.Contains("第3エントリ", logContent, StringComparison.Ordinal);

            // GREEN: JSONエントリの個数確認（EntryTypeフィールドで正確にカウント）
            var entryTypeMatches = System.Text.RegularExpressions.Regex.Matches(logContent, @"""EntryType"":");
            Assert.True(entryTypeMatches.Count >= 3, $"3つのJSONエントリが記録されていません。実際のJSONオブジェクト数: {entryTypeMatches.Count}");

            // GREEN: SessionIDの一貫性確認（全エントリで同じSessionID）
            var sessionIdMatches = System.Text.RegularExpressions.Regex.Matches(logContent, @"""SessionId"": ""([^""]+)""");
            Assert.True(sessionIdMatches.Count >= 3, $"SessionIdが3つ見つかりません。実際の数: {sessionIdMatches.Count}");

            var firstSessionId = sessionIdMatches[0].Groups[1].Value;
            foreach (System.Text.RegularExpressions.Match match in sessionIdMatches)
            {
                var currentSessionId = match.Groups[1].Value;
                Assert.Equal(firstSessionId, currentSessionId);
            }
        }

        public void Dispose()
        {
            if (File.Exists(_testLogPath))
            {
                try
                {
                    File.Delete(_testLogPath);
                }
                catch
                {
                    // テスト後のクリーンアップ失敗は無視
                }
            }
        }
    }
}