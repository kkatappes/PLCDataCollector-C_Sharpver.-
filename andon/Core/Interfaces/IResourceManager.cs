using Andon.Core.Models;

namespace Andon.Core.Interfaces;

/// <summary>
/// リソース管理インターフェース
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// 現在のメモリ使用量を取得（MB単位）
    /// </summary>
    /// <returns>メモリ使用量（MB）</returns>
    double GetCurrentMemoryUsageMb();

    /// <summary>
    /// 現在のメモリレベルを判定
    /// </summary>
    /// <returns>メモリレベル（Normal/Warning/Critical）</returns>
    MemoryLevel GetMemoryLevel();

    /// <summary>
    /// 強制ガベージコレクション実行
    /// </summary>
    void ForceGarbageCollection();
}
