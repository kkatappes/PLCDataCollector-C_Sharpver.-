using System;
using System.Threading;
using System.Threading.Tasks;
using Andon.Core.Interfaces;

namespace Andon.Services
{
    /// <summary>
    /// 周期的なタイマー実行を提供するサービス
    /// </summary>
    public class TimerService : ITimerService
    {
        private readonly ILoggingManager _loggingManager;

        public TimerService(ILoggingManager loggingManager)
        {
            _loggingManager = loggingManager ?? throw new ArgumentNullException(nameof(loggingManager));
        }

        /// <summary>
        /// 指定した間隔で処理を繰り返し実行します。
        /// </summary>
        public async Task StartPeriodicExecution(
            Func<Task> action,
            TimeSpan interval,
            CancellationToken cancellationToken)
        {
            using var timer = new PeriodicTimer(interval);
            bool isExecuting = false;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await timer.WaitForNextTickAsync(cancellationToken);

                    // 前回処理未完了時の重複実行防止
                    if (isExecuting)
                    {
                        await _loggingManager.LogWarning("Previous cycle still running, skipping this interval");
                        continue;
                    }

                    isExecuting = true;
                    
                    // Fire and Forget: 非同期で実行して完了を待たない
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await action();
                        }
                        catch (Exception ex)
                        {
                            await _loggingManager.LogError(ex, "Error in periodic action execution");
                        }
                        finally
                        {
                            isExecuting = false;
                        }
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
