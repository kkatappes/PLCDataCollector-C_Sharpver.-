using System;
using System.Collections.Generic;
using SlmpClient.Constants;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Core
{
    /// <summary>
    /// PLC接続情報
    /// Step1で設定ファイルから読み込まれる接続設定
    /// </summary>
    public class PlcConnectionInfo
    {
        /// <summary>
        /// PLC表示名（設定ファイルから取得）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// IPアドレス
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// ポート番号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// TCP使用フラグ
        /// </summary>
        public bool UseTcp { get; set; }

        /// <summary>
        /// フレームバージョン ("3E" or "4E")
        /// </summary>
        public string FrameVersion { get; set; } = "4E";

        /// <summary>
        /// 受信タイムアウト(ms)
        /// </summary>
        public int ReceiveTimeoutMs { get; set; }

        /// <summary>
        /// 接続タイムアウト(ms)
        /// </summary>
        public int ConnectTimeoutMs { get; set; }

        /// <summary>
        /// バイナリモード使用フラグ
        /// </summary>
        public bool IsBinary { get; set; } = true;

        /// <summary>
        /// 接続情報の文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"{DisplayName} ({IpAddress}:{Port})";
        }
    }

    /// <summary>
    /// アクティブデバイス情報
    /// Step5で非ゼロデータが検出されたデバイスの情報
    /// </summary>
    public class ActiveDeviceInfo
    {
        /// <summary>
        /// デバイスコード
        /// </summary>
        public DeviceCode DeviceCode { get; set; }

        /// <summary>
        /// デバイスアドレス
        /// </summary>
        public uint Address { get; set; }

        /// <summary>
        /// 検出された値（非ゼロ値）
        /// </summary>
        public object Value { get; set; } = new object();

        /// <summary>
        /// デバイス値の型
        /// </summary>
        public DeviceValueType ValueType { get; set; }

        /// <summary>
        /// 検出日時
        /// </summary>
        public DateTime DetectedAt { get; set; }

        /// <summary>
        /// デバイス名（DeviceCode + Address）
        /// </summary>
        public string DeviceName => $"{DeviceCode}{Address}";

        /// <summary>
        /// アクティブデバイス情報の文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"{DeviceName} = {Value} (Type: {ValueType}, Detected: {DetectedAt:HH:mm:ss})";
        }
    }


    /// <summary>
    /// Step2のPLC接続・機器情報取得結果
    /// </summary>
    public class PlcConnectionResult
    {
        /// <summary>
        /// PLC型名
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// TypeCode
        /// </summary>
        public TypeCode TypeCode { get; set; }

        /// <summary>
        /// 接続情報詳細
        /// </summary>
        public string ConnectionInfo { get; set; } = string.Empty;

        /// <summary>
        /// 接続成功フラグ
        /// </summary>
        public bool IsConnectionSuccessful { get; set; }

        /// <summary>
        /// 接続日時
        /// </summary>
        public DateTime ConnectedAt { get; set; }

        /// <summary>
        /// フォールバック処理使用フラグ
        /// ReadTypeName失敗時にフォールバック処理が使用された場合true
        /// </summary>
        public bool FallbackUsed { get; set; } = false;

        /// <summary>
        /// 元のエラーメッセージ
        /// フォールバック処理が使用された場合の元の例外メッセージ
        /// </summary>
        public string? OriginalError { get; set; }

        /// <summary>
        /// 結果の文字列表現
        /// </summary>
        public override string ToString()
        {
            var fallbackInfo = FallbackUsed ? " [Fallback]" : "";
            return $"Connection: {(IsConnectionSuccessful ? "✅" : "❌")} | Type: {TypeName} ({TypeCode}){fallbackInfo} | {ConnectionInfo}";
        }
    }

    /// <summary>
    /// Step5の非ゼロデータ抽出結果
    /// </summary>
    public class NonZeroDataExtractionResult
    {
        /// <summary>
        /// 抽出されたアクティブデバイス一覧
        /// </summary>
        public List<ActiveDeviceInfo> ActiveDevices { get; set; } = new();

        /// <summary>
        /// 総スキャンデバイス数
        /// </summary>
        public int TotalScannedDevices { get; set; }

        /// <summary>
        /// 抽出実行日時
        /// </summary>
        public DateTime ExtractedAt { get; set; }

        /// <summary>
        /// 抽出率（%）
        /// </summary>
        public double ExtractionRate => TotalScannedDevices > 0
            ? (double)ActiveDevices.Count / TotalScannedDevices * 100
            : 0;

        /// <summary>
        /// 結果の文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"Active: {ActiveDevices.Count}/{TotalScannedDevices} devices ({ExtractionRate:F1}%) | Extracted at: {ExtractedAt:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Step6の継続監視開始結果
    /// </summary>
    public class ContinuousMonitoringResult
    {
        /// <summary>
        /// 監視開始成功フラグ
        /// </summary>
        public bool IsStarted { get; set; }

        /// <summary>
        /// 監視対象デバイス数
        /// </summary>
        public int MonitoringDeviceCount { get; set; }

        /// <summary>
        /// 監視開始日時
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// 監視設定情報
        /// </summary>
        public string MonitoringSettings { get; set; } = string.Empty;

        /// <summary>
        /// 結果の文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"Monitoring: {(IsStarted ? "✅ Started" : "❌ Failed")} | Devices: {MonitoringDeviceCount} | {MonitoringSettings}";
        }
    }
}