using System.Text;
using Andon.Core.Models;

namespace Andon.Core.Exceptions;

/// <summary>
/// 複数フレーム送信時の部分失敗を表現する例外クラス
/// 一部のフレーム送信が成功し、一部が失敗した場合にスローされます。
/// </summary>
public class PartialFailureException : Exception
{
    /// <summary>
    /// 成功したフレーム情報の詳細
    /// </summary>
    public Dictionary<string, FrameTransmissionResult> SuccessfulFrames { get; }

    /// <summary>
    /// 失敗したフレーム情報の詳細
    /// </summary>
    public Dictionary<string, FrameTransmissionResult> FailedFrames { get; }

    /// <summary>
    /// 総フレーム数
    /// </summary>
    public int TotalFrameCount { get; }

    /// <summary>
    /// 成功フレーム数
    /// </summary>
    public int SuccessfulFrameCount { get; }

    /// <summary>
    /// 失敗フレーム数
    /// </summary>
    public int FailedFrameCount { get; }

    /// <summary>
    /// PartialFailureExceptionを初期化します
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <param name="successfulFrames">成功したフレーム情報</param>
    /// <param name="failedFrames">失敗したフレーム情報</param>
    public PartialFailureException(
        string message,
        Dictionary<string, FrameTransmissionResult> successfulFrames,
        Dictionary<string, FrameTransmissionResult> failedFrames)
        : base(message)
    {
        SuccessfulFrames = successfulFrames ?? new Dictionary<string, FrameTransmissionResult>();
        FailedFrames = failedFrames ?? new Dictionary<string, FrameTransmissionResult>();

        TotalFrameCount = SuccessfulFrames.Count + FailedFrames.Count;
        SuccessfulFrameCount = SuccessfulFrames.Count;
        FailedFrameCount = FailedFrames.Count;
    }

    /// <summary>
    /// PartialFailureExceptionを初期化します（内部例外付き）
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    /// <param name="innerException">内部例外</param>
    /// <param name="successfulFrames">成功したフレーム情報</param>
    /// <param name="failedFrames">失敗したフレーム情報</param>
    public PartialFailureException(
        string message,
        Exception innerException,
        Dictionary<string, FrameTransmissionResult> successfulFrames,
        Dictionary<string, FrameTransmissionResult> failedFrames)
        : base(message, innerException)
    {
        SuccessfulFrames = successfulFrames ?? new Dictionary<string, FrameTransmissionResult>();
        FailedFrames = failedFrames ?? new Dictionary<string, FrameTransmissionResult>();

        TotalFrameCount = SuccessfulFrames.Count + FailedFrames.Count;
        SuccessfulFrameCount = SuccessfulFrames.Count;
        FailedFrameCount = FailedFrames.Count;
    }

    /// <summary>
    /// 部分失敗の詳細情報を文字列として取得します
    /// </summary>
    /// <returns>詳細情報を含む文字列</returns>
    public string GetDetailedReport()
    {
        var report = new StringBuilder();
        report.AppendLine($"複数フレーム送信の部分失敗: {SuccessfulFrameCount}/{TotalFrameCount} 成功");
        report.AppendLine();

        if (SuccessfulFrames.Any())
        {
            report.AppendLine("■ 成功したフレーム:");
            foreach (var frame in SuccessfulFrames)
            {
                report.AppendLine($"  - {frame.Key}機器 ({frame.Value.DeviceRange}): {frame.Value.SentBytes}バイト送信, {frame.Value.TransmissionTime.TotalMilliseconds:F1}ms");
            }
            report.AppendLine();
        }

        if (FailedFrames.Any())
        {
            report.AppendLine("■ 失敗したフレーム:");
            foreach (var frame in FailedFrames)
            {
                report.AppendLine($"  - {frame.Key}機器 ({frame.Value.DeviceRange}): {frame.Value.ErrorMessage}");
            }
        }

        return report.ToString();
    }
}