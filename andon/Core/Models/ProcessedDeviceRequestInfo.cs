using System;

namespace Andon.Core.Models;

/// <summary>
/// 処理済みデバイス要求情報
/// TC029テスト実装で使用、TC037での構造化処理にも利用
/// </summary>
public class ProcessedDeviceRequestInfo
{
    /// <summary>
    /// デバイス型 ("D", "M", "X", "Y" 等)
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// 開始アドレス
    /// </summary>
    public int StartAddress { get; set; }

    /// <summary>
    /// 要求デバイス数
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// フレーム型
    /// </summary>
    public FrameType FrameType { get; set; }

    /// <summary>
    /// 要求日時
    /// </summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>
    /// 解析設定（TC037構造化処理用）
    /// </summary>
    public ParseConfiguration? ParseConfiguration { get; set; }

    /// <summary>
    /// ReadRandomデバイス指定一覧（Phase8.5暫定対策）
    /// ReadRandom(0x0403)コマンドで複数デバイスを指定する場合に使用
    /// nullの場合は既存のDeviceType/StartAddress/Countを使用（後方互換性）
    /// </summary>
    public List<DeviceSpecification>? DeviceSpecifications { get; set; }

    // Phase3.5で削除されたプロパティ (2025-11-27)
    // DWordCombineTargets: DWord結合対象設定一覧 - 削除理由: DWord結合機能廃止
    // 削除行数: 4行 (41-44行目)
}
