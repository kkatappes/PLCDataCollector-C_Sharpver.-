namespace Andon.Core.Models;

/// <summary>
/// PLC受信レスポンスの生データを表すデータ転送オブジェクト
/// </summary>
public class RawResponseData
{
    /// <summary>
    /// 受信した生バイトデータ
    /// </summary>
    public byte[]? ResponseData { get; set; }

    /// <summary>
    /// 受信データサイズ
    /// </summary>
    public int DataLength { get; set; }

    /// <summary>
    /// 受信完了時刻
    /// </summary>
    public DateTime? ReceivedAt { get; set; }

    /// <summary>
    /// 受信処理時間
    /// </summary>
    public TimeSpan? ReceiveTime { get; set; }

    /// <summary>
    /// 16進数文字列表現
    /// </summary>
    public string? ResponseHex { get; set; }

    /// <summary>
    /// フレームタイプ（4E/3E）
    /// </summary>
    public FrameType FrameType { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }
}