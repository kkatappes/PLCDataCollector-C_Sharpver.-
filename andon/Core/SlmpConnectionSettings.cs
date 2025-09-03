using System;
using System.Text;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// エラーハンドリングモード
    /// 稼働第一の思想に基づく動作制御
    /// </summary>
    public enum ErrorHandlingMode
    {
        /// <summary>例外をスローしてシステム停止（従来動作）</summary>
        ThrowException = 0,
        
        /// <summary>デフォルト値を返却してシステム継続（推奨）</summary>
        ReturnDefaultAndContinue = 1,
        
        /// <summary>リトライ後にデフォルト値返却</summary>
        RetryThenDefault = 2
    }

    /// <summary>
    /// エラー通知レベル
    /// </summary>
    public enum ErrorNotificationLevel
    {
        /// <summary>通知なし</summary>
        None = 0,
        
        /// <summary>警告レベル（デフォルト）</summary>
        Warning = 1,
        
        /// <summary>エラーレベル</summary>
        Error = 2,
        
        /// <summary>重要エラーレベル</summary>
        Critical = 3
    }

    /// <summary>
    /// システム継続動作設定クラス
    /// 稼働第一の思想に基づく設定
    /// </summary>
    public class ContinuitySettings
    {
        /// <summary>
        /// エラーハンドリングモード（デフォルト：継続動作）
        /// </summary>
        public ErrorHandlingMode Mode { get; set; } = ErrorHandlingMode.ReturnDefaultAndContinue;

        /// <summary>
        /// エラー通知レベル（デフォルト：警告）
        /// </summary>
        public ErrorNotificationLevel NotificationLevel { get; set; } = ErrorNotificationLevel.Warning;

        /// <summary>
        /// ビットデバイスのデフォルト値（デフォルト：false）
        /// </summary>
        public bool DefaultBitValue { get; set; } = false;

        /// <summary>
        /// ワードデバイスのデフォルト値（デフォルト：0）
        /// </summary>
        public ushort DefaultWordValue { get; set; } = 0;

        /// <summary>
        /// エラー統計収集フラグ（デフォルト：有効）
        /// </summary>
        public bool EnableErrorStatistics { get; set; } = true;

        /// <summary>
        /// エラー通知の最大頻度（秒単位、デフォルト：30秒）
        /// 同じエラーの連続通知を抑制
        /// </summary>
        public int MaxNotificationFrequencySeconds { get; set; } = 30;

        /// <summary>
        /// システム継続動作のログ出力フラグ（デフォルト：有効）
        /// </summary>
        public bool EnableContinuityLogging { get; set; } = true;

        /// <summary>
        /// 継続動作時のデバッグ情報出力フラグ（デフォルト：無効）
        /// </summary>
        public bool EnableDebugOutput { get; set; } = false;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public ContinuitySettings()
        {
        }

        /// <summary>
        /// コピーコンストラクタ
        /// </summary>
        /// <param name="source">コピー元設定</param>
        public ContinuitySettings(ContinuitySettings source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Mode = source.Mode;
            NotificationLevel = source.NotificationLevel;
            DefaultBitValue = source.DefaultBitValue;
            DefaultWordValue = source.DefaultWordValue;
            EnableErrorStatistics = source.EnableErrorStatistics;
            MaxNotificationFrequencySeconds = source.MaxNotificationFrequencySeconds;
            EnableContinuityLogging = source.EnableContinuityLogging;
            EnableDebugOutput = source.EnableDebugOutput;
        }

        /// <summary>
        /// 稼働第一モードを適用
        /// システム継続を最優先にした設定
        /// </summary>
        public void ApplyOperationFirstMode()
        {
            Mode = ErrorHandlingMode.ReturnDefaultAndContinue;
            NotificationLevel = ErrorNotificationLevel.Warning;
            EnableErrorStatistics = true;
            EnableContinuityLogging = true;
            MaxNotificationFrequencySeconds = 60; // 1分間隔
        }

        /// <summary>
        /// 高信頼性モードを適用
        /// エラー検出を重視した設定
        /// </summary>
        public void ApplyHighReliabilityMode()
        {
            Mode = ErrorHandlingMode.RetryThenDefault;
            NotificationLevel = ErrorNotificationLevel.Error;
            EnableErrorStatistics = true;
            EnableContinuityLogging = true;
            EnableDebugOutput = true;
            MaxNotificationFrequencySeconds = 10; // 10秒間隔
        }
    }
    /// <summary>
    /// SLMP接続設定クラス
    /// Python: util.py での設定項目相当
    /// </summary>
    public class SlmpConnectionSettings
    {
        private int _port = 5000;
        private TimeSpan _receiveTimeout = TimeSpan.FromSeconds(1);
        private TimeSpan _connectTimeout = TimeSpan.FromSeconds(5);
        private int _maxConcurrentRequests = 4;
        private Encoding _textEncoding = Encoding.ASCII;

        /// <summary>
        /// 通信ポート番号 (1-65535)
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (value < 1 || value > 65535)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Port must be between 1 and 65535");
                _port = value;
            }
        }

        /// <summary>
        /// バイナリモード使用フラグ
        /// true: バイナリフレーム, false: ASCIIフレーム
        /// </summary>
        public bool IsBinary { get; set; } = true;

        /// <summary>
        /// SLMPフレームバージョン
        /// </summary>
        public SlmpFrameVersion Version { get; set; } = SlmpFrameVersion.Version4E;

        /// <summary>
        /// TCP使用フラグ
        /// true: TCP, false: UDP
        /// </summary>
        public bool UseTcp { get; set; } = false;

        /// <summary>
        /// 受信タイムアウト時間
        /// </summary>
        public TimeSpan ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "ReceiveTimeout must be positive");
                if (value > TimeSpan.FromMinutes(10))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "ReceiveTimeout must not exceed 10 minutes");
                _receiveTimeout = value;
            }
        }

        /// <summary>
        /// 接続タイムアウト時間
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get => _connectTimeout;
            set
            {
                if (value <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "ConnectTimeout must be positive");
                if (value > TimeSpan.FromMinutes(5))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "ConnectTimeout must not exceed 5 minutes");
                _connectTimeout = value;
            }
        }

        /// <summary>
        /// 最大同時リクエスト数 (1-32)
        /// </summary>
        public int MaxConcurrentRequests
        {
            get => _maxConcurrentRequests;
            set
            {
                if (value < 1 || value > 32)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxConcurrentRequests must be between 1 and 32");
                _maxConcurrentRequests = value;
            }
        }

        /// <summary>
        /// パイプライニング有効フラグ
        /// </summary>
        public bool EnablePipelining { get; set; } = true;

        /// <summary>
        /// テキストエンコーディング (ASCIIモード使用時)
        /// </summary>
        public Encoding TextEncoding
        {
            get => _textEncoding;
            set => _textEncoding = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// リトライ設定
        /// </summary>
        public SlmpRetrySettings RetrySettings { get; set; } = new();

        /// <summary>
        /// システム継続動作設定（稼働第一）
        /// </summary>
        public ContinuitySettings ContinuitySettings { get; set; } = new();

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public SlmpConnectionSettings()
        {
        }

        /// <summary>
        /// コピーコンストラクタ
        /// </summary>
        /// <param name="source">コピー元設定</param>
        public SlmpConnectionSettings(SlmpConnectionSettings source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Port = source.Port;
            IsBinary = source.IsBinary;
            Version = source.Version;
            UseTcp = source.UseTcp;
            ReceiveTimeout = source.ReceiveTimeout;
            ConnectTimeout = source.ConnectTimeout;
            MaxConcurrentRequests = source.MaxConcurrentRequests;
            EnablePipelining = source.EnablePipelining;
            TextEncoding = source.TextEncoding;
            RetrySettings = new SlmpRetrySettings(source.RetrySettings);
            ContinuitySettings = new ContinuitySettings(source.ContinuitySettings);
        }

        /// <summary>
        /// 設定値の妥当性を検証
        /// </summary>
        /// <returns>すべて有効な場合はtrue</returns>
        public bool IsValid()
        {
            return Port >= 1 && Port <= 65535 &&
                   ReceiveTimeout > TimeSpan.Zero && ReceiveTimeout <= TimeSpan.FromMinutes(10) &&
                   ConnectTimeout > TimeSpan.Zero && ConnectTimeout <= TimeSpan.FromMinutes(5) &&
                   MaxConcurrentRequests >= 1 && MaxConcurrentRequests <= 32 &&
                   TextEncoding != null;
        }

        /// <summary>
        /// TCP使用時の推奨設定を適用
        /// </summary>
        public void ApplyTcpRecommendedSettings()
        {
            UseTcp = true;
            EnablePipelining = true;
            MaxConcurrentRequests = 8;
            ReceiveTimeout = TimeSpan.FromSeconds(3);
            ConnectTimeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// UDP使用時の推奨設定を適用
        /// </summary>
        public void ApplyUdpRecommendedSettings()
        {
            UseTcp = false;
            EnablePipelining = false;
            MaxConcurrentRequests = 1;
            ReceiveTimeout = TimeSpan.FromSeconds(1);
            ConnectTimeout = TimeSpan.FromSeconds(5);
            // UDP特性を考慮した継続動作設定
            ContinuitySettings.ApplyOperationFirstMode();
        }

        /// <summary>
        /// 製造現場向け稼働第一設定を適用
        /// システム継続を最優先にした設定
        /// </summary>
        public void ApplyManufacturingOperationFirstSettings()
        {
            // UDP通信を基本とする
            UseTcp = false;
            EnablePipelining = false;
            MaxConcurrentRequests = 1;
            ReceiveTimeout = TimeSpan.FromMilliseconds(800); // 短めのタイムアウト
            ConnectTimeout = TimeSpan.FromSeconds(3);
            
            // リトライ設定を積極的に
            RetrySettings.MaxRetryCount = 2;
            RetrySettings.InitialDelay = TimeSpan.FromMilliseconds(50);
            RetrySettings.BackoffMultiplier = 1.5;
            
            // 稼働第一の継続動作設定
            ContinuitySettings.ApplyOperationFirstMode();
            ContinuitySettings.MaxNotificationFrequencySeconds = 120; // 2分間隔
        }

        /// <summary>
        /// 高速通信用設定を適用
        /// </summary>
        public void ApplyHighPerformanceSettings()
        {
            IsBinary = true;
            Version = SlmpFrameVersion.Version4E;
            UseTcp = true;
            EnablePipelining = true;
            MaxConcurrentRequests = 16;
            ReceiveTimeout = TimeSpan.FromMilliseconds(500);
            ConnectTimeout = TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// 設定の文字列表現を取得
        /// </summary>
        /// <returns>設定内容を表示する文字列</returns>
        public override string ToString()
        {
            return $"SlmpConnectionSettings(Port:{Port}, " +
                   $"{(IsBinary ? "Binary" : "ASCII")}, " +
                   $"Version{(int)Version}E, " +
                   $"{(UseTcp ? "TCP" : "UDP")}, " +
                   $"RxTimeout:{ReceiveTimeout.TotalMilliseconds}ms, " +
                   $"ConnTimeout:{ConnectTimeout.TotalMilliseconds}ms, " +
                   $"MaxReq:{MaxConcurrentRequests}, " +
                   $"Pipelining:{EnablePipelining})";
        }
    }

    /// <summary>
    /// SLMPリトライ設定クラス
    /// </summary>
    public class SlmpRetrySettings
    {
        private int _maxRetryCount = 3;
        private TimeSpan _initialDelay = TimeSpan.FromMilliseconds(100);
        private TimeSpan _maxDelay = TimeSpan.FromSeconds(5);
        private double _backoffMultiplier = 2.0;

        /// <summary>
        /// 最大リトライ回数 (0-10)
        /// </summary>
        public int MaxRetryCount
        {
            get => _maxRetryCount;
            set
            {
                if (value < 0 || value > 10)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxRetryCount must be between 0 and 10");
                _maxRetryCount = value;
            }
        }

        /// <summary>
        /// 初期遅延時間
        /// </summary>
        public TimeSpan InitialDelay
        {
            get => _initialDelay;
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "InitialDelay must not be negative");
                _initialDelay = value;
            }
        }

        /// <summary>
        /// 最大遅延時間
        /// </summary>
        public TimeSpan MaxDelay
        {
            get => _maxDelay;
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "MaxDelay must not be negative");
                _maxDelay = value;
            }
        }

        /// <summary>
        /// バックオフ倍率 (1.0-10.0)
        /// </summary>
        public double BackoffMultiplier
        {
            get => _backoffMultiplier;
            set
            {
                if (value < 1.0 || value > 10.0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "BackoffMultiplier must be between 1.0 and 10.0");
                _backoffMultiplier = value;
            }
        }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public SlmpRetrySettings()
        {
        }

        /// <summary>
        /// コピーコンストラクタ
        /// </summary>
        /// <param name="source">コピー元設定</param>
        public SlmpRetrySettings(SlmpRetrySettings source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            MaxRetryCount = source.MaxRetryCount;
            InitialDelay = source.InitialDelay;
            MaxDelay = source.MaxDelay;
            BackoffMultiplier = source.BackoffMultiplier;
        }

        /// <summary>
        /// 指定回数のリトライ遅延時間を計算
        /// </summary>
        /// <param name="retryAttempt">リトライ回数 (0から開始)</param>
        /// <returns>遅延時間</returns>
        public TimeSpan CalculateDelay(int retryAttempt)
        {
            if (retryAttempt < 0)
                return TimeSpan.Zero;

            var delay = TimeSpan.FromMilliseconds(
                InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, retryAttempt));

            return delay > MaxDelay ? MaxDelay : delay;
        }
    }
}