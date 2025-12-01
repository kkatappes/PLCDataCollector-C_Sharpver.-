namespace Andon.Core.Models;

/// <summary>
/// サイクル実行結果
/// Step3-5の完全サイクル実行結果を保持
/// </summary>
public class CycleExecutionResult
{
    /// <summary>
    /// サイクル実行が成功したかどうか
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// サイクル完了時刻
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 総実行時間
    /// </summary>
    public TimeSpan? TotalExecutionTime { get; set; }

    /// <summary>
    /// Step3: 接続結果
    /// </summary>
    public ConnectionResponse? ConnectResult { get; set; }

    /// <summary>
    /// Step4: 送信結果（詳細は省略、成功/失敗のみ）
    /// </summary>
    public SendResponse? SendResult { get; set; }

    /// <summary>
    /// Step4: 受信結果
    /// </summary>
    public RawResponseData? ReceiveResult { get; set; }

    /// <summary>
    /// Step5: 切断結果
    /// </summary>
    public DisconnectResult? DisconnectResult { get; set; }

    /// <summary>
    /// 実行されたステップ数
    /// </summary>
    public int TotalStepsExecuted { get; set; }

    /// <summary>
    /// 成功したステップ数
    /// </summary>
    public int SuccessfulSteps { get; set; }

    /// <summary>
    /// ステップ別エラーリスト
    /// </summary>
    public List<string> StepErrors { get; set; } = new();

    /// <summary>
    /// ステップ別実行時間
    /// </summary>
    public Dictionary<string, TimeSpan> StepExecutionTimes { get; set; } = new();

    /// <summary>
    /// ステップエラーを追加
    /// </summary>
    public void AddStepError(string step, string error)
    {
        StepErrors.Add($"{step}: {error}");
    }

    /// <summary>
    /// ステップ実行時間を記録
    /// </summary>
    public void RecordStepTime(string step, TimeSpan elapsed)
    {
        StepExecutionTimes[step] = elapsed;
    }

    /// <summary>
    /// ステップ成功率を取得
    /// </summary>
    public double GetStepSuccessRate()
    {
        if (TotalStepsExecuted == 0) return 0.0;
        return (double)SuccessfulSteps / TotalStepsExecuted * 100.0;
    }
}

/// <summary>
/// 送信結果（簡略版）
/// </summary>
public class SendResponse
{
    /// <summary>
    /// 送信成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 送信バイト数
    /// </summary>
    public int SentBytes { get; set; }

    /// <summary>
    /// 送信時刻
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }
}
