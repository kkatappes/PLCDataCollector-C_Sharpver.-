namespace Andon.Core.Models;

/// <summary>
/// 複数フレーム送信結果
/// </summary>
public class MultiFrameTransmissionResult
{
    /// <summary>
    /// 全フレーム送信成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 送信対象フレーム数
    /// </summary>
    public int TotalFrameCount { get; set; }

    /// <summary>
    /// 送信成功フレーム数
    /// </summary>
    public int SuccessfulFrameCount { get; set; }

    /// <summary>
    /// 送信失敗フレーム数
    /// </summary>
    public int FailedFrameCount { get; set; }

    /// <summary>
    /// デバイス種別別結果（キー: デバイス種別 "M", "D"）
    /// </summary>
    public Dictionary<string, FrameTransmissionResult> FrameResults { get; set; } = new();

    /// <summary>
    /// 全フレーム送信総時間
    /// </summary>
    public TimeSpan TotalTransmissionTime { get; set; }

    /// <summary>
    /// 対象デバイス種別一覧（例: ["M", "D"]）
    /// </summary>
    public List<string> TargetDeviceTypes { get; set; } = new();

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
