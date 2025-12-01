namespace Andon.Core.Models;

/// <summary>
/// Step1初期化結果
/// </summary>
public class InitializationResult
{
    /// <summary>
    /// 初期化成功フラグ
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// PLC数
    /// </summary>
    public int PlcCount { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
