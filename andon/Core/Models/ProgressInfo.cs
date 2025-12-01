namespace Andon.Core.Models;

/// <summary>
/// 進捗報告基底クラス
/// </summary>
/// <summary>
/// 進捗報告基底クラス
/// </summary>
public class ProgressInfo
{
    /// <summary>現在実行ステップ（例："Step3", "PLC接続中"）</summary>
    public string CurrentStep { get; init; }

    /// <summary>進捗率（0.0-1.0）</summary>
    public double Progress { get; init; }

    /// <summary>進捗メッセージ（人間向け表示用）</summary>
    public string Message { get; init; }

    /// <summary>推定残り時間</summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>経過時間</summary>
    public TimeSpan ElapsedTime { get; init; }

    /// <summary>報告時刻</summary>
    public DateTime ReportedAt { get; init; }

    /// <summary>
    /// コンストラクタ（基本情報指定）
    /// </summary>
    public ProgressInfo(string currentStep, double progress, string message, TimeSpan elapsedTime)
    {
        if (string.IsNullOrWhiteSpace(currentStep))
            throw new ArgumentException("CurrentStep cannot be null or whitespace.", nameof(currentStep));
        if (progress < 0.0 || progress > 1.0)
            throw new ArgumentOutOfRangeException(nameof(progress), "Progress must be between 0.0 and 1.0.");
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or whitespace.", nameof(message));

        CurrentStep = currentStep;
        Progress = progress;
        Message = message;
        ElapsedTime = elapsedTime;
        ReportedAt = DateTime.Now;
    }

    /// <summary>
    /// コンストラクタ（推定残り時間指定）
    /// </summary>
    public ProgressInfo(
        string currentStep, 
        double progress, 
        string message, 
        TimeSpan elapsedTime,
        TimeSpan? estimatedTimeRemaining) 
        : this(currentStep, progress, message, elapsedTime)
    {
        EstimatedTimeRemaining = estimatedTimeRemaining;
    }
}
