namespace Andon.Core.Models;

/// <summary>
/// SLMP フレームタイプを表す列挙型（Binary/ASCII対応）
/// </summary>
public enum FrameType
{
    /// <summary>
    /// 3Eフレーム Binary形式
    /// </summary>
    Frame3E_Binary = 0,

    /// <summary>
    /// 4Eフレーム Binary形式
    /// </summary>
    Frame4E_Binary = 1,

    /// <summary>
    /// 3Eフレーム ASCII形式
    /// </summary>
    Frame3E_ASCII = 2,

    /// <summary>
    /// 4Eフレーム ASCII形式
    /// </summary>
    Frame4E_ASCII = 3,

    /// <summary>
    /// 不明なフレーム形式
    /// </summary>
    Unknown = 99,

    // 下位互換性のためのエイリアス
    /// <summary>
    /// 3Eフレーム（標準フレーム）- Frame3E_Binaryと同じ
    /// </summary>
    Frame3E = Frame3E_Binary,

    /// <summary>
    /// 4Eフレーム（拡張フレーム）- Frame4E_Binaryと同じ
    /// </summary>
    Frame4E = Frame4E_Binary
}