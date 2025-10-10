using System;
using System.ComponentModel.DataAnnotations;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// アプリケーション設定の統合クラス
    /// appsettings.jsonの構造に対応
    /// </summary>
    public class ApplicationConfiguration
    {
        /// <summary>PLC接続設定</summary>
        public PlcConnectionConfiguration PlcConnection { get; set; } = new();

        /// <summary>継続動作設定</summary>
        public ContinuityConfiguration ContinuitySettings { get; set; } = new();

        /// <summary>タイムアウト設定</summary>
        public TimeoutConfiguration TimeoutSettings { get; set; } = new();

        /// <summary>リトライ設定</summary>
        public RetryConfiguration RetrySettings { get; set; } = new();

        /// <summary>ログ設定</summary>
        public LoggingConfiguration Logging { get; set; } = new();

        /// <summary>監視設定</summary>
        public MonitoringConfiguration MonitoringSettings { get; set; } = new();

        /// <summary>アプリケーション設定</summary>
        public ApplicationSettingsConfiguration ApplicationSettings { get; set; } = new();

        /// <summary>生データログ設定</summary>
        public RawDataLoggingConfiguration RawDataLoggingSettings { get; set; } = new();

        /// <summary>診断設定</summary>
        public DiagnosticConfiguration DiagnosticSettings { get; set; } = new();

        /// <summary>統合ログ設定</summary>
        public UnifiedLoggingConfiguration UnifiedLoggingSettings { get; set; } = new();
    }

    /// <summary>
    /// PLC接続設定
    /// </summary>
    public class PlcConnectionConfiguration
    {
        /// <summary>IPアドレス</summary>
        [Required]
        public string IpAddress { get; set; }

        /// <summary>ポート番号</summary>
        [Range(1, 65535)]
        public int Port { get; set; }

        /// <summary>TCP使用フラグ</summary>
        public bool UseTcp { get; set; }

        /// <summary>バイナリモード使用フラグ</summary>
        public bool IsBinary { get; set; }

        /// <summary>フレームバージョン</summary>
        public string FrameVersion { get; set; }

        /// <summary>パイプライニング有効フラグ</summary>
        public bool EnablePipelining { get; set; }

        /// <summary>最大同時リクエスト数</summary>
        [Range(1, 32)]
        public int MaxConcurrentRequests { get; set; }
    }

    /// <summary>
    /// 継続動作設定
    /// </summary>
    public class ContinuityConfiguration
    {
        /// <summary>エラーハンドリングモード</summary>
        public string ErrorHandlingMode { get; set; } = "ReturnDefaultAndContinue";

        /// <summary>通知レベル</summary>
        public string NotificationLevel { get; set; } = "Warning";

        /// <summary>ビットデバイスのデフォルト値</summary>
        public bool DefaultBitValue { get; set; } = false;

        /// <summary>ワードデバイスのデフォルト値</summary>
        public ushort DefaultWordValue { get; set; } = 0;

        /// <summary>エラー統計有効フラグ</summary>
        public bool EnableErrorStatistics { get; set; } = true;

        /// <summary>継続動作ログ有効フラグ</summary>
        public bool EnableContinuityLogging { get; set; } = true;

        /// <summary>デバッグ出力有効フラグ</summary>
        public bool EnableDebugOutput { get; set; } = false;

        /// <summary>エラー通知最大頻度（秒）</summary>
        [Range(1, 3600)]
        public int MaxNotificationFrequencySeconds { get; set; } = 60;
    }

    /// <summary>
    /// タイムアウト設定
    /// </summary>
    public class TimeoutConfiguration
    {
        /// <summary>受信タイムアウト（ミリ秒）</summary>
        [Range(100, 60000)]
        public int ReceiveTimeoutMs { get; set; }

        /// <summary>接続タイムアウト（ミリ秒）</summary>
        [Range(1000, 300000)]
        public int ConnectTimeoutMs { get; set; }
    }

    /// <summary>
    /// リトライ設定
    /// </summary>
    public class RetryConfiguration
    {
        /// <summary>最大リトライ回数</summary>
        [Range(0, 10)]
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>初期遅延（ミリ秒）</summary>
        [Range(0, 10000)]
        public int InitialDelayMs { get; set; } = 100;

        /// <summary>最大遅延（ミリ秒）</summary>
        [Range(100, 60000)]
        public int MaxDelayMs { get; set; } = 5000;

        /// <summary>バックオフ倍率</summary>
        [Range(1.0, 10.0)]
        public double BackoffMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// ログ設定
    /// </summary>
    public class LoggingConfiguration
    {
        /// <summary>コンソール出力有効フラグ</summary>
        public bool EnableConsoleOutput { get; set; } = true;

        /// <summary>ファイル出力有効フラグ</summary>
        public bool EnableFileOutput { get; set; } = false;

        /// <summary>ログレベル</summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>ログファイルパス</summary>
        public string LogFilePath { get; set; } = "logs/andon.log";

        /// <summary>最大ログファイルサイズ（MB）</summary>
        [Range(1, 1000)]
        public int MaxLogFileSizeMB { get; set; } = 10;

        /// <summary>最大ログファイル数</summary>
        [Range(1, 100)]
        public int MaxLogFiles { get; set; } = 5;
    }

    /// <summary>
    /// 監視設定
    /// </summary>
    public class MonitoringConfiguration
    {
        /// <summary>サイクル間隔（ミリ秒）</summary>
        [Range(100, 60000)]
        public int CycleIntervalMs { get; set; } = 1000;

        /// <summary>最大サイクル数</summary>
        [Range(1, 10000)]
        public int MaxCycles { get; set; } = 10;

        /// <summary>パフォーマンス監視有効フラグ</summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>データ収集間隔（ミリ秒）</summary>
        [Range(100, 10000)]
        public int DataCollectionIntervalMs { get; set; } = 500;
    }

    /// <summary>
    /// アプリケーション設定
    /// </summary>
    public class ApplicationSettingsConfiguration
    {
        /// <summary>アプリケーション名</summary>
        public string ApplicationName { get; set; } = "Andon SLMP Client";

        /// <summary>バージョン</summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>環境</summary>
        public string Environment { get; set; } = "Production";
    }

    /// <summary>
    /// 生データログ設定
    /// </summary>
    public class RawDataLoggingConfiguration
    {
        /// <summary>生データ解析有効フラグ</summary>
        public bool EnableRawDataAnalysis { get; set; } = true;

        /// <summary>16進数ダンプ有効フラグ</summary>
        public bool EnableHexDump { get; set; } = true;

        /// <summary>詳細フレーム解析有効フラグ</summary>
        public bool EnableDetailedFrameAnalysis { get; set; } = true;

        /// <summary>16進数ダンプ最大バイト数</summary>
        [Range(0, 1024)]
        public int MaxHexDumpBytes { get; set; } = 256;

        /// <summary>ファイルログ有効フラグ</summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>ログファイルパス</summary>
        public string LogFilePath { get; set; } = "logs/rawdata_analysis.log";

        /// <summary>JSON出力有効フラグ</summary>
        public bool EnableJsonExport { get; set; } = true;

        /// <summary>JSON出力パス</summary>
        public string JsonExportPath { get; set; } = "logs/rawdata_analysis.json";

        /// <summary>最大ログファイルサイズ（MB）</summary>
        [Range(1, 1000)]
        public int MaxLogFileSizeMB { get; set; } = 50;

        /// <summary>最大ログファイル数</summary>
        [Range(1, 100)]
        public int MaxLogFiles { get; set; } = 10;
    }

    /// <summary>
    /// 診断設定
    /// </summary>
    public class DiagnosticConfiguration
    {
        /// <summary>起動時診断有効フラグ</summary>
        public bool EnableStartupDiagnostic { get; set; } = true;

        /// <summary>継続監視有効フラグ</summary>
        public bool EnableContinuousMonitoring { get; set; } = true;

        /// <summary>診断間隔（分）</summary>
        [Range(1, 1440)]
        public int DiagnosticIntervalMinutes { get; set; } = 15;

        /// <summary>ネットワーク接続性設定</summary>
        public NetworkConnectivityConfiguration NetworkConnectivity { get; set; } = new();

        /// <summary>PLCシステム情報設定</summary>
        public PlcSystemInfoConfiguration PlcSystemInfo { get; set; } = new();

        /// <summary>デバイスアクセス性設定</summary>
        public DeviceAccessibilityConfiguration DeviceAccessibility { get; set; } = new();

        /// <summary>通信品質設定</summary>
        public CommunicationQualityConfiguration CommunicationQuality { get; set; } = new();

        /// <summary>アラート設定</summary>
        public AlertConfiguration AlertSettings { get; set; } = new();

        /// <summary>レポート設定</summary>
        public ReportConfiguration ReportSettings { get; set; } = new();
    }

    /// <summary>
    /// ネットワーク接続性設定
    /// </summary>
    public class NetworkConnectivityConfiguration
    {
        /// <summary>テスト有効フラグ</summary>
        public bool EnableTest { get; set; } = true;

        /// <summary>タイムアウト（ミリ秒）</summary>
        [Range(1000, 30000)]
        public int TimeoutMs { get; set; } = 5000;

        /// <summary>リトライ回数</summary>
        [Range(1, 10)]
        public int RetryCount { get; set; } = 3;
    }

    /// <summary>
    /// PLCシステム情報設定
    /// </summary>
    public class PlcSystemInfoConfiguration
    {
        /// <summary>テスト有効フラグ</summary>
        public bool EnableTest { get; set; } = true;

        /// <summary>CPU状態チェック</summary>
        public bool CheckCpuStatus { get; set; } = true;

        /// <summary>エラー状態チェック</summary>
        public bool CheckErrorStatus { get; set; } = true;

        /// <summary>SLMPバージョン検証</summary>
        public bool ValidateSlmpVersion { get; set; } = true;
    }

    /// <summary>
    /// デバイスアクセス性設定
    /// </summary>
    public class DeviceAccessibilityConfiguration
    {
        /// <summary>テスト有効フラグ</summary>
        public bool EnableTest { get; set; } = true;

        /// <summary>テスト対象ビットデバイス</summary>
        public DeviceTestInfo[] BitDevicesToTest { get; set; } = Array.Empty<DeviceTestInfo>();

        /// <summary>テスト対象ワードデバイス</summary>
        public DeviceTestInfo[] WordDevicesToTest { get; set; } = Array.Empty<DeviceTestInfo>();
    }

    /// <summary>
    /// デバイステスト情報
    /// </summary>
    public class DeviceTestInfo
    {
        /// <summary>デバイスコード</summary>
        public string DeviceCode { get; set; } = string.Empty;

        /// <summary>デバイス番号</summary>
        public uint DeviceNumber { get; set; }
    }

    /// <summary>
    /// 通信品質設定
    /// </summary>
    public class CommunicationQualityConfiguration
    {
        /// <summary>テスト有効フラグ</summary>
        public bool EnableTest { get; set; } = true;

        /// <summary>サンプル数</summary>
        [Range(1, 100)]
        public int SampleCount { get; set; } = 10;

        /// <summary>許容成功率（%）</summary>
        [Range(0.0, 100.0)]
        public double AcceptableSuccessRatePercent { get; set; } = 95.0;

        /// <summary>最大許容応答時間（ms）</summary>
        [Range(1.0, 10000.0)]
        public double MaxAcceptableResponseTimeMs { get; set; } = 100.0;

        /// <summary>品質チェック間隔（分）</summary>
        [Range(1, 60)]
        public int QualityCheckIntervalMinutes { get; set; } = 5;
    }

    /// <summary>
    /// アラート設定
    /// </summary>
    public class AlertConfiguration
    {
        /// <summary>アラート有効フラグ</summary>
        public bool EnableAlerts { get; set; } = true;

        /// <summary>ネットワーク障害時アラート</summary>
        public bool AlertOnNetworkFailure { get; set; } = true;

        /// <summary>PLCエラー時アラート</summary>
        public bool AlertOnPlcError { get; set; } = true;

        /// <summary>デバイスアクセス障害時アラート</summary>
        public bool AlertOnDeviceAccessFailure { get; set; } = true;

        /// <summary>品質劣化時アラート</summary>
        public bool AlertOnQualityDegradation { get; set; } = true;

        /// <summary>アラート発生閾値品質</summary>
        [Range(0.0, 100.0)]
        public double MinQualityForAlert { get; set; } = 80.0;
    }

    /// <summary>
    /// レポート設定
    /// </summary>
    public class ReportConfiguration
    {
        /// <summary>詳細レポート生成</summary>
        public bool GenerateDetailedReports { get; set; } = true;

        /// <summary>パフォーマンスメトリクス含める</summary>
        public bool IncludePerformanceMetrics { get; set; } = true;

        /// <summary>エラー分析含める</summary>
        public bool IncludeErrorAnalysis { get; set; } = true;

        /// <summary>レポート出力パス</summary>
        public string ReportOutputPath { get; set; } = "logs/diagnostic_reports";
    }

    /// <summary>
    /// 統合ログ設定
    /// </summary>
    public class UnifiedLoggingConfiguration
    {
        /// <summary>ログファイルパス</summary>
        public string LogFilePath { get; set; } = "logs/slmp_unified_log.log";

        /// <summary>最大ログファイルサイズ（MB）</summary>
        [Range(1, 1000)]
        public int MaxLogFileSizeMB { get; set; } = 50;

        /// <summary>ログローテーション有効フラグ</summary>
        public bool LogRotationEnabled { get; set; } = true;

        /// <summary>保持日数</summary>
        [Range(1, 365)]
        public int RetentionDays { get; set; } = 14;

        /// <summary>ログレベル</summary>
        public string LogLevel { get; set; } = "Information";

        /// <summary>タイムスタンプ含める</summary>
        public bool IncludeTimestamps { get; set; } = true;

        /// <summary>セッションID含める</summary>
        public bool IncludeSessionIds { get; set; } = true;

        /// <summary>構造化ログ有効フラグ</summary>
        public bool EnableStructuredLogging { get; set; } = true;

        /// <summary>古いログ圧縮</summary>
        public bool CompressOldLogs { get; set; } = true;
    }

    /// <summary>
    /// 設定の拡張メソッド
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// PlcConnectionConfigurationからSlmpConnectionSettingsに変換
        /// </summary>
        public static SlmpConnectionSettings ToSlmpConnectionSettings(
            this ApplicationConfiguration config)
        {
            var settings = new SlmpConnectionSettings
            {
                Port = config.PlcConnection.Port,
                IsBinary = config.PlcConnection.IsBinary,
                UseTcp = config.PlcConnection.UseTcp,
                EnablePipelining = config.PlcConnection.EnablePipelining,
                MaxConcurrentRequests = config.PlcConnection.MaxConcurrentRequests,
                ReceiveTimeout = TimeSpan.FromMilliseconds(config.TimeoutSettings.ReceiveTimeoutMs),
                ConnectTimeout = TimeSpan.FromMilliseconds(config.TimeoutSettings.ConnectTimeoutMs)
            };

            // フレームバージョンの変換
            settings.Version = config.PlcConnection.FrameVersion == "4E"
                ? SlmpFrameVersion.Version4E
                : SlmpFrameVersion.Version3E;

            // 継続設定の適用
            ApplyContinuitySettings(settings, config.ContinuitySettings);

            // リトライ設定の適用
            ApplyRetrySettings(settings, config.RetrySettings);

            return settings;
        }

        /// <summary>
        /// 継続設定を適用
        /// </summary>
        private static void ApplyContinuitySettings(
            SlmpConnectionSettings settings,
            ContinuityConfiguration continuityConfig)
        {
            var continuitySettings = settings.ContinuitySettings;

            // エラーハンドリングモードの変換
            continuitySettings.Mode = continuityConfig.ErrorHandlingMode switch
            {
                "ThrowException" => ErrorHandlingMode.ThrowException,
                "ReturnDefaultAndContinue" => ErrorHandlingMode.ReturnDefaultAndContinue,
                "RetryThenDefault" => ErrorHandlingMode.RetryThenDefault,
                _ => ErrorHandlingMode.ReturnDefaultAndContinue
            };

            // 通知レベルの変換
            continuitySettings.NotificationLevel = continuityConfig.NotificationLevel switch
            {
                "None" => ErrorNotificationLevel.None,
                "Warning" => ErrorNotificationLevel.Warning,
                "Error" => ErrorNotificationLevel.Error,
                "Critical" => ErrorNotificationLevel.Critical,
                _ => ErrorNotificationLevel.Warning
            };

            continuitySettings.DefaultBitValue = continuityConfig.DefaultBitValue;
            continuitySettings.DefaultWordValue = continuityConfig.DefaultWordValue;
            continuitySettings.EnableErrorStatistics = continuityConfig.EnableErrorStatistics;
            continuitySettings.EnableContinuityLogging = continuityConfig.EnableContinuityLogging;
            continuitySettings.EnableDebugOutput = continuityConfig.EnableDebugOutput;
            continuitySettings.MaxNotificationFrequencySeconds = continuityConfig.MaxNotificationFrequencySeconds;
        }

        /// <summary>
        /// リトライ設定を適用
        /// </summary>
        private static void ApplyRetrySettings(
            SlmpConnectionSettings settings,
            RetryConfiguration retryConfig)
        {
            var retrySettings = settings.RetrySettings;
            retrySettings.MaxRetryCount = retryConfig.MaxRetryCount;
            retrySettings.InitialDelay = TimeSpan.FromMilliseconds(retryConfig.InitialDelayMs);
            retrySettings.MaxDelay = TimeSpan.FromMilliseconds(retryConfig.MaxDelayMs);
            retrySettings.BackoffMultiplier = retryConfig.BackoffMultiplier;
        }
    }
}