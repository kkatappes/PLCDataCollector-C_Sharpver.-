using Andon.Core.Constants;

namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// Excelから読み込んだPLC設定情報を保持するクラス
/// </summary>
public class PlcConfiguration
{
    /// <summary>
    /// PLCのIPアドレス（Excel "settings"シート B8セル）
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// PLCのポート（Excel "settings"シート B9セル）
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// デバイス名/PLC識別名（Excel "settings"シート B12セル）
    /// </summary>
    public string PlcModel { get; set; } = string.Empty;

    /// <summary>
    /// データ保存先パス（Excel "settings"シート B13セル）
    /// </summary>
    public string SavePath { get; set; } = string.Empty;

    /// <summary>
    /// 接続方式（Excel "settings"シート B10セル、省略時: DefaultValues.ConnectionMethod）
    /// </summary>
    public string ConnectionMethod { get; set; } = DefaultValues.ConnectionMethod;

    /// <summary>
    /// SLMPフレームバージョン（省略時: DefaultValues.FrameVersion）
    /// 注意: 将来的な拡張用プロパティ
    /// </summary>
    public string FrameVersion { get; set; } = DefaultValues.FrameVersion;

    /// <summary>
    /// タイムアウト値(ミリ秒)（省略時: DefaultValues.TimeoutMs）
    /// 注意: 将来的な拡張用プロパティ
    /// </summary>
    public int Timeout { get; set; } = DefaultValues.TimeoutMs;

    /// <summary>
    /// Binary/ASCII形式切替（省略時: DefaultValues.IsBinary）
    /// 注意: 将来的な拡張用プロパティ
    /// </summary>
    public bool IsBinary { get; set; } = DefaultValues.IsBinary;

    /// <summary>
    /// データ取得周期/監視間隔(ミリ秒)（Excel "settings"シート B11セル）
    /// </summary>
    public int MonitoringIntervalMs { get; set; } = DefaultValues.MonitoringIntervalMs;

    /// <summary>
    /// PLC識別子（自動生成: "{IpAddress}_{Port}"）
    /// </summary>
    public string PlcId { get; set; } = string.Empty;

    /// <summary>
    /// PLC名称（Excel "settings"シート B15セル、省略時: PlcIdを使用）
    /// </summary>
    public string PlcName { get; set; } = string.Empty;

    /// <summary>
    /// ログ出力用PLC識別名（フォールバック処理付き）
    /// PlcNameが設定されている場合はPlcNameを返し、
    /// 未設定の場合はPlcIdを返す
    /// </summary>
    public string EffectivePlcName =>
        string.IsNullOrWhiteSpace(PlcName) ? PlcId : PlcName;

    /// <summary>
    /// 設定元のExcelファイルパス
    /// </summary>
    public string SourceExcelFile { get; set; } = string.Empty;

    /// <summary>
    /// 設定名（ファイル名から拡張子を除いたもの）
    /// </summary>
    public string ConfigurationName =>
        string.IsNullOrEmpty(SourceExcelFile)
            ? string.Empty
            : Path.GetFileNameWithoutExtension(SourceExcelFile);

    /// <summary>
    /// データ収集デバイスリスト（Excel "データ収集デバイス"シートから読み込み）
    /// </summary>
    public List<DeviceSpecification> Devices { get; set; } = new();
}
