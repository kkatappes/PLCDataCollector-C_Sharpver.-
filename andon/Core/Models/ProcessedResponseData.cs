using System;
using System.Collections.Generic;
using System.Linq;
using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// パース済みレスポンスデータ（Phase5実装版、2025-11-21）
/// 新旧構造の共存期（Phase10で旧構造削除予定）
/// </summary>
public class ProcessedResponseData
{
    // ========================================
    // 新構造（Phase5～Phase10以降で使用）
    // ========================================

    /// <summary>
    /// 元の受信生データ（16進数文字列）
    /// </summary>
    public string OriginalRawData { get; set; } = string.Empty;

    /// <summary>
    /// 処理済みデータ（デバイス名キー構造）
    /// キー例: "M0", "D100", "W0x11AA"
    /// 値: DeviceData（DeviceName, Code, Address, Value, IsDWord, IsHexAddress）
    /// Phase7: DataOutputManager/LoggingManagerで使用
    /// </summary>
    public Dictionary<string, DeviceData> ProcessedData { get; set; } = new();

    /// <summary>
    /// 処理完了時刻
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// 処理時間（ミリ秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    // ========================================
    // エラー情報
    // ========================================

    /// <summary>
    /// 処理成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// エラー情報
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告情報
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    // ========================================
    // 統計情報（自動計算）
    // ========================================

    /// <summary>
    /// 処理済みデバイス総数
    /// </summary>
    public int TotalProcessedDevices => ProcessedData.Count;

    /// <summary>
    /// ビットデバイス数（DeviceCode.IsBitDevice()で判定）
    /// </summary>
    public int BitDeviceCount => ProcessedData.Values.Count(d => d.Code.IsBitDevice());

    /// <summary>
    /// ワードデバイス数（非ビット、非DWord）
    /// </summary>
    public int WordDeviceCount => ProcessedData.Values.Count(d => !d.Code.IsBitDevice() && !d.IsDWord);

    /// <summary>
    /// ダブルワードデバイス数
    /// </summary>
    public int DWordDeviceCount => ProcessedData.Values.Count(d => d.IsDWord);

    /// <summary>
    /// フレームタイプ（4Eフレーム解析対応）
    /// </summary>
    public FrameType FrameType { get; set; } = FrameType.Frame3E;

    // ========================================
    // ユーティリティメソッド
    // ========================================

    /// <summary>
    /// デバイス名から値を取得
    /// </summary>
    public uint? GetDeviceValue(string deviceName)
    {
        return ProcessedData.TryGetValue(deviceName, out var data) ? data.Value : null;
    }

    /// <summary>
    /// ビットデバイス一覧を取得
    /// </summary>
    public List<string> GetBitDevices()
    {
        return ProcessedData.Where(kv => kv.Value.Code.IsBitDevice()).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// ワードデバイス一覧を取得
    /// </summary>
    public List<string> GetWordDevices()
    {
        return ProcessedData.Where(kv => !kv.Value.Code.IsBitDevice() && !kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// ダブルワードデバイス一覧を取得
    /// </summary>
    public List<string> GetDWordDevices()
    {
        return ProcessedData.Where(kv => kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }

}
