namespace Andon.Core.Models;

/// <summary>
/// 3Eフレーム情報
/// </summary>
public class FrameInfo
{
    /// <summary>
    /// フレーム種別（例：3E）
    /// </summary>
    public string FrameType { get; set; } = string.Empty;

    /// <summary>
    /// データ形式（例：Binary）
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;

    /// <summary>
    /// ヘッダーサイズ（バイト）
    /// </summary>
    public int HeaderSize { get; set; }

    /// <summary>
    /// データ部サイズ（バイト）
    /// </summary>
    public int DataSize { get; set; }

    /// <summary>
    /// 終了コード
    /// </summary>
    public ushort EndCode { get; set; }

    /// <summary>
    /// 解析時刻
    /// </summary>
    public DateTime ParsedAt { get; set; }
}