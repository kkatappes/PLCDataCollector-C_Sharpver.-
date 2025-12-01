using Andon.Core.Models.ConfigModels;
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
}
