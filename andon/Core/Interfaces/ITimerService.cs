namespace Andon.Core.Interfaces;

/// <summary>
/// タイマー制御インターフェース
/// </summary>
public interface ITimerService
{
    /// <summary>
    /// 指定した間隔で処理を繰り返し実行します。
    /// </summary>
    /// <param name="action">実行する処理</param>
    /// <param name="interval">実行間隔</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task StartPeriodicExecution(
        Func<Task> action,
        TimeSpan interval,
        CancellationToken cancellationToken);
}
