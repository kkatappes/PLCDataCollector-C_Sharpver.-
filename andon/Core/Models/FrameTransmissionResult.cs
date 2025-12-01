namespace Andon.Core.Models;

/// <summary>
/// 個別フレーム送信結果
/// </summary>
public class FrameTransmissionResult
{
    /// <summary>
    /// 個別フレーム送信成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 送信バイト数
    /// </summary>
    public int SentBytes { get; set; }

    /// <summary>
    /// 送信所要時間
    /// </summary>
    public TimeSpan TransmissionTime { get; set; }

    /// <summary>
    /// デバイス種別（"M", "D"）
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// デバイス範囲（"M001-M999", "D001-D999"）
    /// </summary>
    public string DeviceRange { get; set; } = string.Empty;

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
