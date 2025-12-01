using System;
using System.Threading;
using System.Threading.Tasks;
using Andon.Core.Interfaces;
using Andon.Services;
using Xunit;

namespace Andon.Tests.Unit.Services
{
    public class TimerServiceTests
    {
        [Fact]
        public async Task StartPeriodicExecution_実行間隔に従って処理を繰り返し実行する()
        {
            // Arrange
            var mockLogger = new TestLoggingManager();
            var timerService = new TimerService(mockLogger);
            int executionCount = 0;
            var interval = TimeSpan.FromMilliseconds(100);
            var cts = new CancellationTokenSource();

            // Act
            var task = Task.Run(async () =>
            {
                await timerService.StartPeriodicExecution(
                    async () => { executionCount++; await Task.CompletedTask; },
                    interval,
                    cts.Token);
            });

            await Task.Delay(350); // 3回実行される時間待機
            cts.Cancel();
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // 期待される例外
            }

            // Assert
            Assert.InRange(executionCount, 3, 4); // タイミングのずれを考慮
        }

        [Fact]
        public async Task StartPeriodicExecution_前回処理未完了時は重複実行しない()
        {
            // Arrange
            var mockLogger = new TestLoggingManager();
            var timerService = new TimerService(mockLogger);
            int executionCount = 0;
            int concurrentExecutions = 0;
            int maxConcurrent = 0;
            var interval = TimeSpan.FromMilliseconds(5); // 非常に短い間隔
            var cts = new CancellationTokenSource();

            // Act
            var task = Task.Run(async () =>
            {
                await timerService.StartPeriodicExecution(
                    async () =>
                    {
                        Interlocked.Increment(ref concurrentExecutions);
                        maxConcurrent = Math.Max(maxConcurrent, concurrentExecutions);
                        executionCount++;
                        await Task.Delay(50); // 長時間処理をシミュレート（interval * 10）
                        Interlocked.Decrement(ref concurrentExecutions);
                    },
                    interval,
                    cts.Token);
            });

            await Task.Delay(200); // 十分な時間待機して複数回のタイマーティックを確保
            cts.Cancel();
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // 期待される例外
            }

            // Assert (executionCountをデバッグ出力)
            Console.WriteLine($"ExecutionCount: {executionCount}, WarningCount: {mockLogger.WarningCount}, MaxConcurrent: {maxConcurrent}");
            Assert.Equal(1, maxConcurrent); // 同時実行は1つのみ
            Assert.True(mockLogger.WarningCount > 0, $"警告ログが出力されるべき (実際: {mockLogger.WarningCount}, 実行回数: {executionCount})");
        }

        [Fact]
        public async Task StartPeriodicExecution_処理中の例外をログに記録して継続する()
        {
            // Arrange
            var mockLogger = new TestLoggingManager();
            var timerService = new TimerService(mockLogger);
            int executionCount = 0;
            var interval = TimeSpan.FromMilliseconds(50);
            var cts = new CancellationTokenSource();

            // Act
            var task = Task.Run(async () =>
            {
                await timerService.StartPeriodicExecution(
                    async () =>
                    {
                        executionCount++;
                        if (executionCount == 2)
                        {
                            throw new InvalidOperationException("Test exception");
                        }
                        await Task.CompletedTask;
                    },
                    interval,
                    cts.Token);
            });

            await Task.Delay(250); // 4-5回実行される時間待機
            cts.Cancel();
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // 期待される例外
            }

            // Assert
            Console.WriteLine($"ExecutionCount: {executionCount}, ErrorCount: {mockLogger.ErrorCount}");
            Assert.True(executionCount >= 3, $"例外後も実行継続すべき (実際: {executionCount}回)"); // 例外後も実行継続
            Assert.Equal(1, mockLogger.ErrorCount); // エラーログが1回出力される
        }

        /// <summary>
        /// テスト用のシンプルなロギングマネージャー実装
        /// </summary>
        private class TestLoggingManager : ILoggingManager
        {
            public int WarningCount { get; private set; }
            public int ErrorCount { get; private set; }

            public Task LogInfo(string message) => Task.CompletedTask;

            public Task LogWarning(string message)
            {
                WarningCount++;
                return Task.CompletedTask;
            }

            public Task LogError(Exception? ex, string message)
            {
                ErrorCount++;
                return Task.CompletedTask;
            }

            public Task LogDebug(string message) => Task.CompletedTask;
            public Task CloseAndFlushAsync() => Task.CompletedTask;
        }
    }
}
