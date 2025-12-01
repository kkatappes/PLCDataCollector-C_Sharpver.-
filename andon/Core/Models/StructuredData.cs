using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// ParseRawToStructuredData戻り値
/// 3Eフレーム解析により構造化されたデータ情報
/// </summary>
public class StructuredData
{
    /// <summary>
    /// 処理成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 構造化デバイス一覧
    /// </summary>
    public List<StructuredDevice> StructuredDevices { get; set; } = new();

    /// <summary>
    /// 処理完了時刻
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// 処理時間（ミリ秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// 3Eフレーム情報
    /// </summary>
    public FrameInfo FrameInfo { get; set; } = new();

    /// <summary>
    /// 解析ステップ記録
    /// </summary>
    public List<string> ParseSteps { get; set; } = new();

    /// <summary>
    /// エラー情報
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告情報
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 構造化デバイス総数
    /// </summary>
    public int TotalStructuredDevices { get; set; }

    /// <summary>
    /// 4Eフレーム解析かどうか（4Eフレーム解析対応）
    /// </summary>
    public bool Is4EFrame => FrameInfo?.FrameType == SlmpConstants.Frame4E;

    /// <summary>
    /// 構造化デバイス追加
    /// </summary>
    /// <param name="device">追加するデバイス</param>
    public void AddStructuredDevice(StructuredDevice device)
    {
        StructuredDevices.Add(device);
        TotalStructuredDevices = StructuredDevices.Count;
    }

    /// <summary>
    /// 構造化デバイス取得
    /// </summary>
    /// <param name="deviceName">デバイス名</param>
    /// <returns>構造化デバイス（見つからない場合はnull）</returns>
    public StructuredDevice? GetStructuredDevice(string deviceName)
    {
        return StructuredDevices.FirstOrDefault(d => d.DeviceName == deviceName);
    }

    /// <summary>
    /// 解析ステップ追加
    /// </summary>
    /// <param name="step">ステップ説明</param>
    public void AddParseStep(string step)
    {
        ParseSteps.Add(step);
    }
}
