namespace Andon.Core.Models;

/// <summary>
/// TC121用：Step3（接続）→Step4（送受信）→Step5（切断）→Step6（データ処理）の完全サイクル実行結果
/// </summary>
public class FullCycleExecutionResult
{
    /// <summary>
    /// 完全サイクル実行が成功したかどうか
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// エラーメッセージ（失敗時のみ）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 完全サイクル完了時刻
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 総実行時間
    /// </summary>
    public TimeSpan? TotalExecutionTime { get; set; }

    // ===== 各ステップの結果 =====

    /// <summary>
    /// Step3: 接続結果
    /// </summary>
    public ConnectionResponse? ConnectResult { get; set; }

    /// <summary>
    /// Step4: 送信結果
    /// </summary>
    public SendResponse? SendResult { get; set; }

    /// <summary>
    /// Step4: 受信結果
    /// </summary>
    public RawResponseData? ReceiveResult { get; set; }

    /// <summary>
    /// Step6-1: 基本処理結果（デバイス値抽出）
    /// Phase13: BasicProcessedResponseDataからProcessedResponseDataに変更
    /// </summary>
    public ProcessedResponseData? BasicProcessedData { get; set; }

    /// <summary>
    /// Step6-2: DWord結合処理結果
    /// </summary>
    public ProcessedResponseData? ProcessedData { get; set; }

    /// <summary>
    /// Step6-3: 構造化処理結果（最終出力）
    /// </summary>
    public StructuredData? StructuredData { get; set; }

    // ===== サイクル統計 =====

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
    public List<string> StepErrors { get; set; } = new List<string>();

    /// <summary>
    /// ステップ別実行時間
    /// </summary>
    public Dictionary<string, TimeSpan> StepExecutionTimes { get; set; } = new Dictionary<string, TimeSpan>();

    // ===== ヘルパーメソッド =====

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

    /// <summary>
    /// 最終的な構造化データが正常に取得できたかチェック
    /// </summary>
    public bool HasValidStructuredData()
    {
        return StructuredData?.IsSuccess == true &&
               StructuredData.StructuredDevices?.Count > 0;
    }

    /// <summary>
    /// DWord結合が正常に実行されたかチェック
    /// Phase13修正: ProcessedDataのDictionaryを使用
    /// </summary>
    public bool HasValidDWordData()
    {
        return ProcessedData?.IsSuccess == true &&
               ProcessedData.ProcessedData.Values.Any(d => d.IsDWord);
    }

    /// <summary>
    /// 全てのStep6処理が成功したかチェック
    /// </summary>
    public bool IsStep6Success()
    {
        return BasicProcessedData?.IsSuccess == true &&
               ProcessedData?.IsSuccess == true &&
               StructuredData?.IsSuccess == true;
    }
}