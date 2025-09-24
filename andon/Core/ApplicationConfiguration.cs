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
    }

    /// <summary>
    /// PLC接続設定
    /// </summary>
    public class PlcConnectionConfiguration
    {
        /// <summary>IPアドレス</summary>
        [Required]
        public string IpAddress { get; set; } = "192.168.1.10";

        /// <summary>ポート番号</summary>
        [Range(1, 65535)]
        public int Port { get; set; } = 5007;

        /// <summary>TCP使用フラグ</summary>
        public bool UseTcp { get; set; } = true;

        /// <summary>バイナリモード使用フラグ</summary>
        public bool IsBinary { get; set; } = true;

        /// <summary>フレームバージョン</summary>
        public string FrameVersion { get; set; } = "4E";

        /// <summary>パイプライニング有効フラグ</summary>
        public bool EnablePipelining { get; set; } = true;

        /// <summary>最大同時リクエスト数</summary>
        [Range(1, 32)]
        public int MaxConcurrentRequests { get; set; } = 8;
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
        public int ReceiveTimeoutMs { get; set; } = 3000;

        /// <summary>接続タイムアウト（ミリ秒）</summary>
        [Range(1000, 300000)]
        public int ConnectTimeoutMs { get; set; } = 10000;
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