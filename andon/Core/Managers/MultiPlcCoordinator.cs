namespace Andon.Core.Managers;

using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 複数PLC並列実行調整ヘルパー（軽量クラス）
/// Phase6-2: ハイブリッド方式実装
/// </summary>
public class MultiPlcCoordinator
{
    /// <summary>
    /// 並列実行（Task.WhenAll）
    /// </summary>
    /// <param name="plcConfigs">PLC設定リスト</param>
    /// <param name="parallelConfig">並列処理設定</param>
    /// <param name="executeSinglePlcAsync">単一PLC実行処理のデリゲート</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>各PLCの実行結果リスト</returns>
    public static async Task<List<PlcExecutionResult>> ExecuteParallelAsync(
        List<PlcConnectionConfig> plcConfigs,
        ParallelProcessingConfig parallelConfig,
        Func<PlcConnectionConfig, CancellationToken, Task<PlcExecutionResult>> executeSinglePlcAsync,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PlcExecutionResult>();

        // タイムアウト設定
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(parallelConfig.OverallTimeoutMs);

        try
        {
            // 各PLC用のタスク生成（優先度降順）
            var tasks = plcConfigs
                .OrderByDescending(p => p.Priority)
                .Select(plcConfig => executeSinglePlcAsync(plcConfig, cts.Token))
                .ToList();

            // 並列実行
            var taskResults = await Task.WhenAll(tasks);
            results.AddRange(taskResults);
        }
        catch (OperationCanceledException)
        {
            // タイムアウトまたはキャンセル
            // 既に完了したタスクの結果は results に含まれている
            throw;
        }

        return results;
    }

    /// <summary>
    /// 順次実行（ConMoni3互換）
    /// </summary>
    /// <param name="plcConfigs">PLC設定リスト</param>
    /// <param name="executeSinglePlcAsync">単一PLC実行処理のデリゲート</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>各PLCの実行結果リスト</returns>
    public static async Task<List<PlcExecutionResult>> ExecuteSequentialAsync(
        List<PlcConnectionConfig> plcConfigs,
        Func<PlcConnectionConfig, CancellationToken, Task<PlcExecutionResult>> executeSinglePlcAsync,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PlcExecutionResult>();

        foreach (var plcConfig in plcConfigs)
        {
            var result = await executeSinglePlcAsync(plcConfig, cancellationToken);
            results.Add(result);

            // スロットリング（次のPLC処理まで10ms待機）
            await Task.Delay(10, cancellationToken);
        }

        return results;
    }
}
