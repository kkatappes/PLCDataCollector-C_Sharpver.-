using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Andon.Core.Managers;

/// <summary>
/// リソース管理クラス
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly SystemResourcesConfig _config;
    private readonly Process _currentProcess;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="options">システムリソース設定</param>
    /// <exception cref="ArgumentNullException">options が null の場合</exception>
    public ResourceManager(IOptions<SystemResourcesConfig> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _config = options.Value;
        _currentProcess = Process.GetCurrentProcess();
    }

    /// <summary>
    /// 現在のメモリ使用量を取得（MB単位）
    /// </summary>
    /// <returns>メモリ使用量（MB）</returns>
    public double GetCurrentMemoryUsageMb()
    {
        // プロセスの最新情報を取得
        _currentProcess.Refresh();

        // プロセスの現在の物理メモリ使用量を取得（WorkingSet64）
        long memoryBytes = _currentProcess.WorkingSet64;

        // バイトからMBに変換
        return memoryBytes / (1024.0 * 1024.0);
    }

    /// <summary>
    /// 現在のメモリレベルを判定
    /// </summary>
    /// <returns>メモリレベル（Normal/Warning/Critical）</returns>
    public MemoryLevel GetMemoryLevel()
    {
        double currentUsageMb = GetCurrentMemoryUsageMb();
        double maxMemoryMb = _config.MaxMemoryUsageMb;

        // 使用率を計算
        double usagePercentage = (currentUsageMb / maxMemoryMb) * 100.0;

        // レベル判定
        if (usagePercentage >= 85.0)
        {
            return MemoryLevel.Critical;
        }
        else if (usagePercentage >= 70.0)
        {
            return MemoryLevel.Warning;
        }
        else
        {
            return MemoryLevel.Normal;
        }
    }

    /// <summary>
    /// 強制ガベージコレクション実行
    /// </summary>
    public void ForceGarbageCollection()
    {
        // 強制GC実行
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
