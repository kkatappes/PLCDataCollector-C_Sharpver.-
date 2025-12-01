using Andon.Core.Interfaces;
using Andon.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Andon.Services;

/// <summary>
/// 並行実行制御
/// </summary>
public class ParallelExecutionController : IParallelExecutionController
{
    private readonly ILogger<ParallelExecutionController> _logger;

    public ParallelExecutionController(ILogger<ParallelExecutionController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ParallelExecutionResult> ExecuteParallelPlcOperationsAsync<T>(
        IEnumerable<T> configManagers,
        Func<T, CancellationToken, Task<CycleExecutionResult>> executeAsync,
        CancellationToken cancellationToken) where T : class
    {
        if (configManagers == null)
            throw new ArgumentNullException(nameof(configManagers));
        if (executeAsync == null)
            throw new ArgumentNullException(nameof(executeAsync));

        cancellationToken.ThrowIfCancellationRequested();

        var managersList = configManagers.ToList();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting parallel execution for {Count} PLCs", managersList.Count);

        // 並行実行用のタスクリストを作成
        var tasks = managersList.Select(async (manager, index) =>
        {
            var plcId = $"PLC{index + 1}";
            try
            {
                _logger.LogDebug("Starting execution for {PlcId}", plcId);
                var result = await executeAsync(manager, cancellationToken);
                _logger.LogDebug("Completed execution for {PlcId}: Success={IsSuccess}",
                    plcId, result.IsSuccess);
                return (PlcId: plcId, Result: result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing for {PlcId}", plcId);
                return (PlcId: plcId, Result: new CycleExecutionResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTime.Now
                });
            }
        }).ToList();

        // すべてのタスクが完了するまで待機
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // 結果を集計
        var parallelResult = new ParallelExecutionResult
        {
            TotalPlcCount = managersList.Count,
            OverallExecutionTime = stopwatch.Elapsed
        };

        foreach (var (plcId, result) in results)
        {
            parallelResult.PlcResults[plcId] = result;
            if (result.IsSuccess)
            {
                parallelResult.SuccessfulPlcCount++;
            }
            else
            {
                parallelResult.FailedPlcCount++;
            }
        }

        _logger.LogInformation(
            "Parallel execution completed: Total={Total}, Success={Success}, Failed={Failed}, Duration={Duration}ms",
            parallelResult.TotalPlcCount,
            parallelResult.SuccessfulPlcCount,
            parallelResult.FailedPlcCount,
            stopwatch.ElapsedMilliseconds);

        return parallelResult;
    }

    /// <inheritdoc/>
    public async Task MonitorParallelExecutionAsync(
        IEnumerable<Task<CycleExecutionResult>> tasks,
        IProgress<ParallelProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        if (tasks == null)
            throw new ArgumentNullException(nameof(tasks));

        var tasksList = tasks.ToList();
        if (tasksList.Count == 0)
            return;

        _logger.LogInformation("Starting parallel execution monitoring for {Count} tasks", tasksList.Count);

        var plcProgresses = tasksList.Select((t, i) => ($"PLC{i + 1}", 0.0))
            .ToDictionary(x => x.Item1, x => x.Item2);

        // 監視ループ
        while (tasksList.Any(t => !t.IsCompleted))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 各タスクの進捗を更新
            for (int i = 0; i < tasksList.Count; i++)
            {
                var plcId = $"PLC{i + 1}";
                plcProgresses[plcId] = tasksList[i].IsCompleted ? 1.0 : 0.5;
            }

            // 進捗を報告
            if (progress != null)
            {
                var progressInfo = new ParallelProgressInfo(
                    "Parallel Execution",
                    new Dictionary<string, double>(plcProgresses),
                    TimeSpan.Zero);
                progress.Report(progressInfo);
            }

            await Task.Delay(100, cancellationToken);
        }

        // 最終進捗報告
        if (progress != null)
        {
            var finalProgress = plcProgresses.Keys.ToDictionary(k => k, k => 1.0);
            var finalInfo = new ParallelProgressInfo(
                "Parallel Execution",
                finalProgress,
                TimeSpan.Zero);
            progress.Report(finalInfo);
        }

        _logger.LogInformation("Parallel execution monitoring completed");
    }
}
