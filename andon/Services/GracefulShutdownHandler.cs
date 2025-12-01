namespace Andon.Services;

/// <summary>
/// 適切な終了処理・リソース解放
/// </summary>
/// <summary>
/// 適切な終了処理・リソース解放
/// </summary>
public class GracefulShutdownHandler
{
    private readonly Andon.Core.Interfaces.ILoggingManager _loggingManager;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="loggingManager">ロギングマネージャー</param>
    public GracefulShutdownHandler(Andon.Core.Interfaces.ILoggingManager loggingManager)
    {
        _loggingManager = loggingManager ?? throw new ArgumentNullException(nameof(loggingManager));
    }

    /// <summary>
    /// 適切な終了処理を実行する
    /// </summary>
    /// <param name="controller">アプリケーションコントローラー</param>
    /// <param name="timeout">タイムアウト時間（デフォルト: 30秒）</param>
    /// <returns>終了処理結果</returns>
    public async Task<Andon.Core.Models.ShutdownResult> ExecuteGracefulShutdown(
        Andon.Core.Interfaces.IApplicationController controller,
        TimeSpan timeout = default)
    {
        var startTime = DateTime.Now;
        
        if (timeout == default)
        {
            timeout = TimeSpan.FromSeconds(30);
        }

        try
        {
            await _loggingManager.LogInfo("Starting graceful shutdown...");

            using var timeoutCts = new CancellationTokenSource(timeout);
            await controller.StopAsync(timeoutCts.Token);

            await _loggingManager.LogInfo("Graceful shutdown completed successfully");

            return new Andon.Core.Models.ShutdownResult
            {
                Success = true,
                StartTime = startTime,
                EndTime = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            await _loggingManager.LogError(ex, "Graceful shutdown failed");

            return new Andon.Core.Models.ShutdownResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                StartTime = startTime,
                EndTime = DateTime.Now
            };
        }
    }
}
