using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Core.Interfaces;

/// <summary>
/// 実行制御インターフェース
/// Phase 継続実行モード: PlcConfiguration追加
/// </summary>
public interface IExecutionOrchestrator
{
    /// <summary>
    /// 継続データサイクル実行
    /// Phase 継続実行モード: PlcConfiguration追加
    /// </summary>
    Task RunContinuousDataCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken);

    /// <summary>
    /// 継続データサイクル実行（進捗報告機能付き）
    /// Phase 4-2: ProgressReporter統合
    /// </summary>
    Task RunContinuousDataCycleAsync(
        List<PlcConfiguration> plcConfigs,
        List<IPlcCommunicationManager> plcManagers,
        CancellationToken cancellationToken,
        IProgress<ProgressInfo>? progress);
}
