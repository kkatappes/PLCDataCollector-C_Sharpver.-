using System;
using System.Collections.Generic;
using SlmpClient.Constants;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Core
{
    /// <summary>
    /// デバイス探索設定
    /// PLC型式に応じたスキャン対象デバイスと範囲を定義
    /// </summary>
    public class DeviceDiscoveryConfiguration
    {
        /// <summary>
        /// スキャン対象のビットデバイス
        /// </summary>
        public DeviceCode[] BitDevices { get; set; } = Array.Empty<DeviceCode>();

        /// <summary>
        /// スキャン対象のワードデバイス
        /// </summary>
        public DeviceCode[] WordDevices { get; set; } = Array.Empty<DeviceCode>();

        /// <summary>
        /// デバイス別スキャン範囲設定
        /// </summary>
        public Dictionary<DeviceCode, DeviceRange> ScanRanges { get; set; } = new();

        /// <summary>
        /// 一度に読み取るデバイス数（バッチサイズ）
        /// </summary>
        public int BatchSize { get; set; } = 32;

        /// <summary>
        /// 最大同時スキャン数
        /// </summary>
        public int MaxConcurrentScans { get; set; } = 4;
    }

    /// <summary>
    /// デバイスアドレス範囲定義
    /// </summary>
    public class DeviceRange
    {
        /// <summary>
        /// 開始アドレス
        /// </summary>
        public uint Start { get; set; }

        /// <summary>
        /// 終了アドレス
        /// </summary>
        public uint End { get; set; }

        /// <summary>
        /// スキャン優先度（高いほど優先）
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        /// 範囲内のアドレス数を取得
        /// </summary>
        public uint Count => End >= Start ? End - Start + 1 : 0;

        public override string ToString()
        {
            return $"{Start}-{End} ({Count} addresses)";
        }
    }

    /// <summary>
    /// デバイススキャン結果
    /// </summary>
    public class DeviceScanResult
    {
        /// <summary>
        /// スキャン対象デバイスコード
        /// </summary>
        public DeviceCode DeviceCode { get; set; }

        /// <summary>
        /// スキャン範囲
        /// </summary>
        public DeviceRange ScannedRange { get; set; } = new();

        /// <summary>
        /// 検出されたアクティブデバイスのアドレス一覧
        /// </summary>
        public List<uint> ActiveDevices { get; set; } = new();

        /// <summary>
        /// スキャン統計情報
        /// </summary>
        public ScanStatistics Statistics { get; set; } = new();

        /// <summary>
        /// スキャン完了時刻
        /// </summary>
        public DateTime CompletedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 個別デバイス値情報（全デバイスの値を保存）
        /// </summary>
        public List<DeviceValueInfo> DeviceValues { get; set; } = new();
    }

    /// <summary>
    /// スキャン統計情報
    /// </summary>
    public class ScanStatistics
    {
        /// <summary>
        /// 成功したスキャン回数
        /// </summary>
        public int SuccessfulScans { get; set; }

        /// <summary>
        /// 失敗したスキャン回数
        /// </summary>
        public int FailedScans { get; set; }

        /// <summary>
        /// 総スキャン時間（ミリ秒）
        /// </summary>
        public double TotalScanTimeMs { get; set; }

        /// <summary>
        /// 平均応答時間（ミリ秒）
        /// </summary>
        public double AverageResponseTimeMs => SuccessfulScans > 0 ? TotalScanTimeMs / SuccessfulScans : 0;

        /// <summary>
        /// 成功率（%）
        /// </summary>
        public double SuccessRate => (SuccessfulScans + FailedScans) > 0
            ? (double)SuccessfulScans / (SuccessfulScans + FailedScans) * 100
            : 0;
    }

    /// <summary>
    /// デバイス探索全体の結果
    /// </summary>
    public class DeviceDiscoveryResult
    {
        /// <summary>
        /// PLC型式情報
        /// </summary>
        public string PlcTypeName { get; set; } = string.Empty;

        /// <summary>
        /// PLC TypeCode
        /// </summary>
        public TypeCode PlcTypeCode { get; set; }

        /// <summary>
        /// デバイス別スキャン結果
        /// </summary>
        public List<DeviceScanResult> ScanResults { get; set; } = new();

        /// <summary>
        /// 探索開始時刻
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 探索完了時刻
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 総探索時間
        /// </summary>
        public TimeSpan TotalDuration => EndTime - StartTime;

        /// <summary>
        /// 発見されたアクティブデバイス総数
        /// </summary>
        public int TotalActiveDevices => ScanResults.Sum(r => r.ActiveDevices.Count);

        /// <summary>
        /// 全体統計情報
        /// </summary>
        public ScanStatistics OverallStatistics
        {
            get
            {
                var overall = new ScanStatistics();
                foreach (var result in ScanResults)
                {
                    overall.SuccessfulScans += result.Statistics.SuccessfulScans;
                    overall.FailedScans += result.Statistics.FailedScans;
                    overall.TotalScanTimeMs += result.Statistics.TotalScanTimeMs;
                }
                return overall;
            }
        }
    }

    /// <summary>
    /// デバイス値（監視結果）
    /// </summary>
    public class DeviceValue
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
        /// デバイス値
        /// </summary>
        public object Value { get; set; } = false;

        /// <summary>
        /// 値の型
        /// </summary>
        public DeviceValueType ValueType { get; set; }

        /// <summary>
        /// 読み取り時刻
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// デバイスアドレス文字列表現
        /// </summary>
        public string DeviceAddress => $"{DeviceCode}{Address}";

        public override string ToString()
        {
            return $"{DeviceAddress} = {Value} ({ValueType})";
        }
    }

    /// <summary>
    /// デバイス値の型
    /// </summary>
    public enum DeviceValueType
    {
        /// <summary>
        /// ビット値（bool）
        /// </summary>
        Bit,

        /// <summary>
        /// ワード値（ushort）
        /// </summary>
        Word,

        /// <summary>
        /// ダブルワード値（uint）
        /// </summary>
        DoubleWord
    }

    /// <summary>
    /// 監視サイクル結果
    /// </summary>
    public class MonitoringCycleResult
    {
        /// <summary>
        /// サイクル開始時刻
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// サイクル完了時刻
        /// </summary>
        public DateTime EndTime { get; set; } = DateTime.Now;

        /// <summary>
        /// サイクル実行時間
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// 読み取ったデバイス値一覧
        /// </summary>
        public List<DeviceValue> DeviceValues { get; set; } = new();

        /// <summary>
        /// 成功した読み取り数
        /// </summary>
        public int SuccessfulReads { get; set; }

        /// <summary>
        /// 失敗した読み取り数
        /// </summary>
        public int FailedReads { get; set; }

        /// <summary>
        /// 変化のあったデバイス数
        /// </summary>
        public int ChangedDevicesCount { get; set; }

        /// <summary>
        /// エラーメッセージ（エラー発生時）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 監視サイクルが成功したかどうか
        /// </summary>
        public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage) && FailedReads == 0;
    }

    /// <summary>
    /// デバイスバッチ（連続デバイスの効率的読み取り用）
    /// </summary>
    public class DeviceBatch
    {
        /// <summary>
        /// デバイスコード
        /// </summary>
        public DeviceCode DeviceCode { get; set; }

        /// <summary>
        /// 開始アドレス
        /// </summary>
        public uint StartAddress { get; set; }

        /// <summary>
        /// 読み取りデバイス数
        /// </summary>
        public ushort Count { get; set; }

        /// <summary>
        /// バッチ内のデバイスアドレス一覧
        /// </summary>
        public List<uint> Addresses { get; set; } = new();

        public override string ToString()
        {
            return $"{DeviceCode}{StartAddress}-{StartAddress + Count - 1} ({Count} devices)";
        }
    }

    /// <summary>
    /// アクティブデバイス判定設定
    /// </summary>
    public class ActiveDeviceThreshold
    {
        /// <summary>
        /// ビットデバイスの判定基準
        /// </summary>
        public BitDeviceThreshold BitDevice { get; set; } = BitDeviceThreshold.AnyTrue;

        /// <summary>
        /// ワードデバイスの判定基準
        /// </summary>
        public WordDeviceThreshold WordDevice { get; set; } = WordDeviceThreshold.NonZero;
    }

    /// <summary>
    /// ビットデバイスのアクティブ判定基準
    /// </summary>
    public enum BitDeviceThreshold
    {
        /// <summary>
        /// いずれかがTrue
        /// </summary>
        AnyTrue,

        /// <summary>
        /// 全てがTrue
        /// </summary>
        AllTrue,

        /// <summary>
        /// 過半数がTrue
        /// </summary>
        MajorityTrue
    }

    /// <summary>
    /// ワードデバイスのアクティブ判定基準
    /// </summary>
    public enum WordDeviceThreshold
    {
        /// <summary>
        /// 非ゼロ値
        /// </summary>
        NonZero,

        /// <summary>
        /// 指定値以上
        /// </summary>
        AboveThreshold,

        /// <summary>
        /// 変化検出
        /// </summary>
        HasChanged
    }

    /// <summary>
    /// 個別デバイス値情報
    /// バッチ読み取りで取得した各デバイスの具体的な値を保存
    /// </summary>
    public class DeviceValueInfo
    {
        /// <summary>
        /// デバイスアドレス（100, 101等）
        /// </summary>
        public uint Address { get; set; }

        /// <summary>
        /// 実際の値（bool または ushort/uint）
        /// </summary>
        public object Value { get; set; } = 0;

        /// <summary>
        /// デバイス種類（M, D等）
        /// </summary>
        public DeviceCode DeviceCode { get; set; }

        /// <summary>
        /// 表示名（"M105", "D250"等）
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// 読み取り時刻
        /// </summary>
        public DateTime ReadAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 値のタイプ（ビット/ワード）
        /// </summary>
        public DeviceValueType ValueType { get; set; }

        /// <summary>
        /// このデバイスがアクティブか（非ゼロ値か）
        /// </summary>
        public bool IsActive { get; set; }
    }

}