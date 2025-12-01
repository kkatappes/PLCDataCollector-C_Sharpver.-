namespace Andon.Core.Models;

/// <summary>
/// メモリ使用レベル列挙型
/// </summary>
public enum MemoryLevel
{
    /// <summary>
    /// 正常レベル（0-70%）
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 警告レベル（70-85%）
    /// </summary>
    Warning = 1,

    /// <summary>
    /// 危険レベル（85%以上）
    /// </summary>
    Critical = 2
}
