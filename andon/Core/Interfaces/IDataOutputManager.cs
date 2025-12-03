using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;

namespace Andon.Core.Interfaces;

/// <summary>
/// データ出力インターフェース
/// </summary>
public interface IDataOutputManager
{
    /// <summary>
    /// ReadRandomデータをJSON形式で出力します
    /// </summary>
    /// <param name="data">処理済みレスポンスデータ</param>
    /// <param name="outputDirectory">出力ディレクトリパス</param>
    /// <param name="ipAddress">PLC IPアドレス（設定ファイルのConnection.IpAddressから取得）</param>
    /// <param name="port">PLCポート番号（設定ファイルのConnection.Portから取得）</param>
    /// <param name="plcModel">PLCモデル（デバイス名）</param>
    /// <param name="deviceConfig">デバイス設定情報（設定ファイルのTargetDevices.Devicesから構築）
    /// キー: デバイス名（"M0", "D100"など）
    /// 値: DeviceEntryInfo（Name=Description, Digits=1）</param>
    void OutputToJson(
        ProcessedResponseData data,
        string outputDirectory,
        string ipAddress,
        int port,
        string plcModel,
        Dictionary<string, DeviceEntryInfo> deviceConfig);
}
