namespace Andon.Core.Models;

/// <summary>
/// 適切な終了処理結果
/// </summary>
public class ShutdownResult
{
    /// <summary>
    /// シャットダウンが成功したかどうか
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// シャットダウン開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// シャットダウン完了時刻
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// シャットダウンにかかった時間
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}
