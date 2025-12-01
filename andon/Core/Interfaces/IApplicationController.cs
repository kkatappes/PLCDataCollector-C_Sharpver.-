using Andon.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Core.Interfaces;

/// <summary>
/// アプリケーション全体制御インターフェース
/// </summary>
public interface IApplicationController
{
    /// <summary>
    /// Step1初期化実行
    /// </summary>
    Task<InitializationResult> ExecuteStep1InitializationAsync(
        string configDirectory = "./config/",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 継続データサイクル開始
    /// </summary>
    Task StartContinuousDataCycleAsync(
        InitializationResult initResult,
        CancellationToken cancellationToken);

    /// <summary>
    /// アプリケーション開始
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// アプリケーション停止
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
