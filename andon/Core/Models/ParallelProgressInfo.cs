namespace Andon.Core.Models;

/// <summary>
/// 並行実行進捗報告専用
/// </summary>
public class ParallelProgressInfo : ProgressInfo
{
    /// <summary>実行中PLC数</summary>
    public int ActivePlcCount { get; private set; }

    /// <summary>完了PLC数</summary>
    public int CompletedPlcCount { get; private set; }

    /// <summary>失敗PLC数</summary>
    public int FailedPlcCount { get; private set; }

    /// <summary>PLC別進捗率（PlcId→進捗率）</summary>
    public Dictionary<string, double> PlcProgresses { get; }

    /// <summary>全体進捗率（全PLC平均進捗）</summary>
    public double OverallProgress { get; private set; }

    /// <summary>
    /// コンストラクタ（並行実行用）
    /// </summary>
    public ParallelProgressInfo(
        string currentStep,
        Dictionary<string, double> plcProgresses,
        TimeSpan elapsedTime)
        : base(currentStep, CalculateInitialOverallProgress(plcProgresses), GenerateMessage(plcProgresses), elapsedTime)
    {
        if (plcProgresses == null)
            throw new ArgumentNullException(nameof(plcProgresses));

        PlcProgresses = new Dictionary<string, double>(plcProgresses);
        UpdatePlcProgress();
    }

    /// <summary>
    /// PLC進捗更新
    /// </summary>
    public void UpdatePlcProgress(string plcId, double progress)
    {
        if (string.IsNullOrWhiteSpace(plcId))
            throw new ArgumentException("PlcId cannot be null or whitespace.", nameof(plcId));
        if (progress < 0.0 || progress > 1.0)
            throw new ArgumentOutOfRangeException(nameof(progress), "Progress must be between 0.0 and 1.0.");

        PlcProgresses[plcId] = progress;
        UpdatePlcProgress();
    }

    /// <summary>
    /// 全体進捗率計算（内部処理）
    /// </summary>
    private void UpdatePlcProgress()
    {
        ActivePlcCount = PlcProgresses.Count(p => p.Value > 0.0 && p.Value < 1.0);
        CompletedPlcCount = PlcProgresses.Count(p => p.Value >= 1.0);
        FailedPlcCount = 0; // 失敗情報は別途管理
        OverallProgress = PlcProgresses.Count > 0
            ? PlcProgresses.Values.Average()
            : 0.0;
    }

    /// <summary>
    /// 初期全体進捗率計算（コンストラクタ用）
    /// </summary>
    private static double CalculateInitialOverallProgress(Dictionary<string, double> plcProgresses)
    {
        if (plcProgresses == null || plcProgresses.Count == 0)
            return 0.0;
        return plcProgresses.Values.Average();
    }

    /// <summary>
    /// 初期メッセージ生成（コンストラクタ用）
    /// </summary>
    private static string GenerateMessage(Dictionary<string, double> plcProgresses)
    {
        if (plcProgresses == null || plcProgresses.Count == 0)
            return "No PLCs";
        return $"{plcProgresses.Count} PLCs in parallel";
    }
}
