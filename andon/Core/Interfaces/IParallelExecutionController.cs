using Andon.Core.Models;

namespace Andon.Core.Interfaces;

/// <summary>
/// 並行実行制御インターフェース
/// </summary>
public interface IParallelExecutionController
{
    /// <summary>
    /// 複数PLC並行実行
    /// </summary>
    /// <typeparam name="T">実行結果の型</typeparam>
    /// <param name="configManagers">PLC用設定管理インスタンス群</param>
    /// <param name="executeAsync">各PLCに対する実行処理</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>並行実行結果</returns>
    Task<ParallelExecutionResult> ExecuteParallelPlcOperationsAsync<T>(
        IEnumerable<T> configManagers,
        Func<T, CancellationToken, Task<CycleExecutionResult>> executeAsync,
        CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// 並行実行監視
    /// </summary>
    /// <param name="tasks">実行中タスク群</param>
    /// <param name="progress">進捗レポーター</param>
    /// <param name="cancellationToken">監視制御トークン</param>
    /// <returns>監視タスク</returns>
    Task MonitorParallelExecutionAsync(
        IEnumerable<Task<CycleExecutionResult>> tasks,
        IProgress<ParallelProgressInfo>? progress,
        CancellationToken cancellationToken);
}
