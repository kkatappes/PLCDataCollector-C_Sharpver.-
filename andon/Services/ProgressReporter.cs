using Andon.Core.Interfaces;
using Andon.Core.Models;

namespace Andon.Services;

/// <summary>
/// 進捗報告実装クラス
/// IProgress<T>実装、リアルタイム進捗報告、UI・コンソール出力対応
/// </summary>
/// <typeparam name="T">進捗情報型（ProgressInfo または string）</typeparam>
public class ProgressReporter<T> : IProgressReporter<T>
{
    private readonly ILoggingManager _loggingManager;
    private readonly Action<T>? _customHandler;

    /// <summary>
    /// コンストラクタ（LoggingManager依存）
    /// </summary>
    public ProgressReporter(ILoggingManager loggingManager)
    {
        _loggingManager = loggingManager ?? throw new ArgumentNullException(nameof(loggingManager));
    }

    /// <summary>
    /// コンストラクタ（カスタムハンドラ付き）
    /// </summary>
    public ProgressReporter(ILoggingManager loggingManager, Action<T> customHandler)
        : this(loggingManager)
    {
        _customHandler = customHandler;
    }

    /// <summary>
    /// 進捗報告実行
    /// </summary>
    public void Report(T value)
    {
        if (value == null)
            return;

        // カスタムハンドラがあれば実行
        _customHandler?.Invoke(value);

        // ログ出力
        var message = FormatProgressMessage(value);
        _loggingManager.LogInfo(message).Wait();

        // コンソール出力
        Console.WriteLine(message);
    }

    /// <summary>
    /// 進捗メッセージのフォーマット
    /// </summary>
    private string FormatProgressMessage(T value)
    {
        return value switch
        {
            ProgressInfo progressInfo => FormatProgressInfo(progressInfo),
            string stringValue => stringValue,
            _ => value.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// ProgressInfo のフォーマット
    /// </summary>
    private string FormatProgressInfo(ProgressInfo progressInfo)
    {
        var elapsed = progressInfo.ElapsedTime.ToString(@"hh\:mm\:ss");
        var progressPercent = (progressInfo.Progress * 100).ToString("F1");

        if (progressInfo is ParallelProgressInfo parallelInfo)
        {
            return $"[{progressInfo.CurrentStep}] {progressPercent}% - {progressInfo.Message} " +
                   $"(Active: {parallelInfo.ActivePlcCount}, Completed: {parallelInfo.CompletedPlcCount}, Elapsed: {elapsed})";
        }

        var remaining = progressInfo.EstimatedTimeRemaining?.ToString(@"hh\:mm\:ss") ?? "Unknown";
        return $"[{progressInfo.CurrentStep}] {progressPercent}% - {progressInfo.Message} " +
               $"(Elapsed: {elapsed}, Remaining: {remaining})";
    }
}
