using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Core
{
    /// <summary>
    /// デバイス探索管理クラス
    /// TDD手法REFACTOR Phase: 定数統一・責任分離・SOLID原則強化
    /// PLC型式（TypeCode）に基づいて適切なデバイス探索設定を提供
    /// </summary>
    public class DeviceDiscoveryManager
    {
        #region Constants

        // バッチサイズ定数（PLCシリーズ別）
        private const int BatchSize_FX_Basic = 16;
        private const int BatchSize_FX5U_Enhanced = 32;
        private const int BatchSize_Q_Standard = 64;
        private const int BatchSize_Q_HighPerf = 96;
        private const int BatchSize_R_Maximum = 96;
        private const int BatchSize_L_Safety = 32;
        private const int BatchSize_QS_SingleAxis = 16;
        private const int BatchSize_Default = 32;

        // 最大同時スキャン数定数（PLCシリーズ別）
        private const int MaxConcurrentScans_FX_Basic = 2;
        private const int MaxConcurrentScans_FX5U_Enhanced = 3;
        private const int MaxConcurrentScans_Q_Standard = 4;
        private const int MaxConcurrentScans_Q_HighPerf = 6;
        private const int MaxConcurrentScans_R_Maximum = 8;
        private const int MaxConcurrentScans_L_Safety = 3;
        private const int MaxConcurrentScans_QS_SingleAxis = 2;
        private const int MaxConcurrentScans_Default = 4;

        // デバイス範囲定数（共通使用）
        private const uint DefaultPriority_High = 3;
        private const uint DefaultPriority_Medium = 2;
        private const uint DefaultPriority_Low = 1;
        private const uint DefaultPriority_Recommended = 4;

        // FXシリーズ標準範囲
        private const uint FX_M_Start = 0, FX_M_End = 7999;
        private const uint FX_X_Start = 0, FX_X_End = 177;
        private const uint FX_Y_Start = 0, FX_Y_End = 177;
        private const uint FX_D_Start = 0, FX_D_End = 7999;

        // FX5Uシリーズ拡張範囲
        private const uint FX5U_M_Start = 0, FX5U_M_End = 15999;
        private const uint FX5U_D_Start = 0, FX5U_D_End = 15999;

        // Qシリーズ標準範囲
        private const uint Q_M_Start = 0, Q_M_End = 8191;
        private const uint Q_X_Start = 0, Q_X_End = 2047;
        private const uint Q_Y_Start = 0, Q_Y_End = 2047;
        private const uint Q_B_Start = 0, Q_B_End = 8191;
        private const uint Q_F_Start = 0, Q_F_End = 2047;
        private const uint Q_V_Start = 0, Q_V_End = 2047;
        private const uint Q_D_Start = 0, Q_D_End = 12287;
        private const uint Q_W_Start = 0, Q_W_End = 8191;
        private const uint Q_D_HighPerf_End = 65535;

        // Rシリーズ大容量範囲
        private const uint R_M_Start = 0, R_M_End = 32767;
        private const uint R_X_Start = 0, R_X_End = 8191;
        private const uint R_Y_Start = 0, R_Y_End = 8191;
        private const uint R_B_Start = 0, R_B_End = 32767;
        private const uint R_F_Start = 0, R_F_End = 32767;
        private const uint R_V_Start = 0, R_V_End = 32767;
        private const uint R_L_Start = 0, R_L_End = 32767;
        private const uint R_D_Start = 0, R_D_End = 1048575;
        private const uint R_W_Start = 0, R_W_End = 32767;

        // Lシリーズ安全PLC範囲
        private const uint L_M_Start = 0, L_M_End = 8191;
        private const uint L_X_Start = 0, L_X_End = 1023;
        private const uint L_Y_Start = 0, L_Y_End = 1023;
        private const uint L_B_Start = 0, L_B_End = 8191;
        private const uint L_D_Start = 0, L_D_End = 32767;

        // QSシリーズ単軸位置決め範囲
        private const uint QS_M_Start = 0, QS_M_End = 2047;
        private const uint QS_X_Start = 0, QS_X_End = 255;
        private const uint QS_Y_Start = 0, QS_Y_End = 255;
        private const uint QS_D_Start = 0, QS_D_End = 4095;

        // デフォルト範囲（未知型式用）
        private const uint Default_M_Start = 0, Default_M_End = 1023;
        private const uint Default_X_Start = 0, Default_X_End = 255;
        private const uint Default_Y_Start = 0, Default_Y_End = 255;
        private const uint Default_D_Start = 0, Default_D_End = 1023;

        // バリデーション定数
        private const int MinBatchSize = 1;
        private const int MaxBatchSize = 960;
        private const int MinConcurrentScans = 1;
        private const int MaxConcurrentScans = 16;

        // ログメッセージ定数
        private const string LogMessage_ConfigConstruction = "TypeCode {0} に基づくデバイス探索設定を構築中（モード: {1}）";
        private const string LogMessage_ConfigCompleted = "探索設定構築完了: ビットデバイス {0}種類, ワードデバイス {1}種類, 総デバイス数 {2}";
        private const string LogMessage_ComprehensiveConfig = "TypeCode {0} の完全対応デバイス設定を構築中";
        private const string LogMessage_BasicConfig = "TypeCode {0} の基本デバイス設定を構築中（従来互換モード）";
        private const string LogMessage_UnknownTypeCode = "未知のTypeCode {0} に対してデフォルト設定を適用";

        #endregion
        private readonly ILogger<DeviceDiscoveryManager> _logger;
        private readonly ActiveDeviceThreshold _activeThreshold;

        /// <summary>
        /// 探索モード設定
        /// </summary>
        public enum DiscoveryMode
        {
            /// <summary>基本デバイスのみ（従来互換）</summary>
            Basic,
            /// <summary>完全対応デバイス（推奨）</summary>
            Comprehensive,
            /// <summary>カスタム設定</summary>
            Custom
        }

        /// <summary>探索モード（デフォルト：Comprehensive）</summary>
        public DiscoveryMode Mode { get; set; } = DiscoveryMode.Comprehensive;

        public DeviceDiscoveryManager(ILogger<DeviceDiscoveryManager> logger, ActiveDeviceThreshold? activeThreshold = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeThreshold = activeThreshold ?? new ActiveDeviceThreshold();
        }

        /// <summary>
        /// TypeCodeに基づいてデバイス探索設定を取得
        /// </summary>
        /// <param name="typeCode">PLC TypeCode</param>
        /// <returns>探索設定</returns>
        public DeviceDiscoveryConfiguration GetDiscoveryConfigurationForTypeCode(TypeCode typeCode)
        {
            _logger.LogInformation(string.Format(LogMessage_ConfigConstruction, typeCode, Mode));

            var config = Mode switch
            {
                DiscoveryMode.Basic => CreateBasicConfiguration(typeCode),
                DiscoveryMode.Comprehensive => CreateComprehensiveConfiguration(typeCode),
                DiscoveryMode.Custom => CreateCustomConfigurationFromTypeCode(typeCode),
                _ => CreateComprehensiveConfiguration(typeCode)
            };

            var totalDevices = config.BitDevices.Length + config.WordDevices.Length;
            _logger.LogInformation(string.Format(LogMessage_ConfigCompleted, config.BitDevices.Length, config.WordDevices.Length, totalDevices));

            return config;
        }

        /// <summary>
        /// 完全対応デバイス探索設定を作成（全39デバイス対応）
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>完全対応探索設定</returns>
        private DeviceDiscoveryConfiguration CreateComprehensiveConfiguration(TypeCode typeCode)
        {
            _logger.LogDebug(string.Format(LogMessage_ComprehensiveConfig, typeCode));

            // CompleteDeviceMapから完全なデバイスマップを取得
            var (devices, ranges) = CompleteDeviceMap.GetCompleteDeviceMap(typeCode);

            // ビットデバイスとワードデバイスを分離
            var bitDevices = devices.Where(d => CompleteDeviceMap.AllBitDevices.Contains(d)).ToArray();
            var wordDevices = devices.Where(d => CompleteDeviceMap.AllWordDevices.Contains(d)).ToArray();

            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = bitDevices,
                WordDevices = wordDevices,
                ScanRanges = new Dictionary<DeviceCode, DeviceRange>(ranges)
            };

            // TypeCode別の最適化設定を適用
            ApplyOptimizedSettings(config, typeCode);

            _logger.LogInformation("完全対応設定構築完了: {TypeCode} - ビット{BitCount}種類, ワード{WordCount}種類",
                typeCode, bitDevices.Length, wordDevices.Length);

            return config;
        }

        /// <summary>
        /// 基本デバイス探索設定を作成（従来互換モード）
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>基本探索設定</returns>
        private DeviceDiscoveryConfiguration CreateBasicConfiguration(TypeCode typeCode)
        {
            _logger.LogDebug("TypeCode {TypeCode} の基本デバイス設定を構築中（従来互換モード）", typeCode);

            var config = typeCode switch
            {
                // FXシリーズ: コンパクトPLC、基本的なデバイスのみ
                _ when typeCode.IsFXSeries() => CreateFXSeriesConfiguration(typeCode),

                // Qシリーズ: 高機能PLC、幅広いデバイス対応
                _ when typeCode.IsQSeries() => CreateQSeriesConfiguration(typeCode),

                // Rシリーズ: 新世代PLC、最新デバイス対応
                _ when typeCode.IsRSeries() => CreateRSeriesConfiguration(typeCode),

                // Lシリーズ: 安全PLC、特殊デバイス対応
                _ when typeCode.IsLSeries() => CreateLSeriesConfiguration(typeCode),

                // QSシリーズ: 単軸位置決めPLC
                _ when typeCode.IsQSSeries() => CreateQSSeriesConfiguration(typeCode),

                // 不明または未対応の型式：デフォルト設定
                _ => CreateDefaultConfiguration(typeCode)
            };

            return config;
        }

        /// <summary>
        /// TypeCode別最適化設定を適用
        /// </summary>
        /// <param name="config">設定</param>
        /// <param name="typeCode">TypeCode</param>
        private void ApplyOptimizedSettings(DeviceDiscoveryConfiguration config, TypeCode typeCode)
        {
            // DeviceCompatibilityMatrixから最適化パラメータを取得して適用
            var optimizedSettings = GetOptimizedSettingsForTypeCode(typeCode);

            config.BatchSize = optimizedSettings.BatchSize;
            config.MaxConcurrentScans = optimizedSettings.MaxConcurrentScans;

            _logger.LogDebug("最適化設定適用: BatchSize={BatchSize}, MaxConcurrentScans={MaxConcurrentScans}",
                config.BatchSize, config.MaxConcurrentScans);
        }

        /// <summary>
        /// TypeCode別最適化設定を取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>最適化設定</returns>
        private (int BatchSize, int MaxConcurrentScans) GetOptimizedSettingsForTypeCode(TypeCode typeCode)
        {
            return typeCode switch
            {
                // FXシリーズ: 小容量のため小さめの設定
                _ when typeCode.IsFXSeries() => (16, 2),

                // FX5Uシリーズ: 性能向上版
                TypeCode.FX5U or TypeCode.FX5UC or TypeCode.FX5UJ => (32, 3),

                // Qシリーズ: 高性能設定
                _ when typeCode.IsQSeries() => (64, 4),

                // Rシリーズ: 最新世代最大性能
                _ when typeCode.IsRSeries() => (96, 8),

                // Lシリーズ: 安全性重視で中程度
                _ when typeCode.IsLSeries() => (32, 3),

                // QSシリーズ: 単軸位置決め用小容量
                _ when typeCode.IsQSSeries() => (16, 2),

                // デフォルト
                _ => (32, 4)
            };
        }

        /// <summary>
        /// カスタム設定（TypeCodeベース）を作成
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>カスタム設定</returns>
        private DeviceDiscoveryConfiguration CreateCustomConfigurationFromTypeCode(TypeCode typeCode)
        {
            // カスタムモードでは、ユーザーが設定した内容を優先
            // 今回は完全対応をベースにカスタマイズ可能な設定を返す
            return CreateComprehensiveConfiguration(typeCode);
        }

        /// <summary>
        /// FXシリーズ用の基本探索設定を作成（従来互換）
        /// </summary>
        private DeviceDiscoveryConfiguration CreateFXSeriesConfiguration(TypeCode typeCode)
        {
            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y },
                WordDevices = new[] { DeviceCode.D },
                BatchSize = 16, // FXシリーズは小容量のため小さめのバッチサイズ
                MaxConcurrentScans = 2
            };

            // FXシリーズ共通の基本範囲
            config.ScanRanges[DeviceCode.M] = new DeviceRange { Start = 0, End = 7999, Priority = 3 };
            config.ScanRanges[DeviceCode.X] = new DeviceRange { Start = 0, End = 177, Priority = 2 };
            config.ScanRanges[DeviceCode.Y] = new DeviceRange { Start = 0, End = 177, Priority = 2 };
            config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 7999, Priority = 3 };

            // FX5Uシリーズ専用の拡張範囲
            if (typeCode == TypeCode.FX5U || typeCode == TypeCode.FX5UC || typeCode == TypeCode.FX5UJ)
            {
                _logger.LogDebug("FX5U系列の拡張範囲を適用");
                config.ScanRanges[DeviceCode.M] = new DeviceRange { Start = 0, End = 15999, Priority = 3 };
                config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 15999, Priority = 3 };
                config.BatchSize = 32; // FX5Uは性能が向上しているため
            }

            return config;
        }

        /// <summary>
        /// Qシリーズ用の探索設定を作成
        /// </summary>
        private DeviceDiscoveryConfiguration CreateQSeriesConfiguration(TypeCode typeCode)
        {
            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.B, DeviceCode.F, DeviceCode.V },
                WordDevices = new[] { DeviceCode.D, DeviceCode.W },
                BatchSize = 64, // Qシリーズは高性能のため大きなバッチサイズ
                MaxConcurrentScans = 4
            };

            // Qシリーズ標準範囲
            config.ScanRanges[DeviceCode.M] = new DeviceRange { Start = 0, End = 8191, Priority = 3 };
            config.ScanRanges[DeviceCode.X] = new DeviceRange { Start = 0, End = 2047, Priority = 2 };
            config.ScanRanges[DeviceCode.Y] = new DeviceRange { Start = 0, End = 2047, Priority = 2 };
            config.ScanRanges[DeviceCode.B] = new DeviceRange { Start = 0, End = 8191, Priority = 1 };
            config.ScanRanges[DeviceCode.F] = new DeviceRange { Start = 0, End = 2047, Priority = 1 };
            config.ScanRanges[DeviceCode.V] = new DeviceRange { Start = 0, End = 2047, Priority = 1 };
            config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 12287, Priority = 3 };
            config.ScanRanges[DeviceCode.W] = new DeviceRange { Start = 0, End = 8191, Priority = 2 };

            // 高性能CPU（Q25H以上）の拡張範囲
            if (typeCode.ToString().Contains("25H") || typeCode.ToString().Contains("100"))
            {
                _logger.LogDebug("Qシリーズ高性能CPU向けの拡張範囲を適用");
                config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 65535, Priority = 3 };
                config.BatchSize = 96;
                config.MaxConcurrentScans = 6;
            }

            return config;
        }

        /// <summary>
        /// Rシリーズ用の探索設定を作成
        /// </summary>
        private DeviceDiscoveryConfiguration CreateRSeriesConfiguration(TypeCode typeCode)
        {
            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.B, DeviceCode.F, DeviceCode.V, DeviceCode.L },
                WordDevices = new[] { DeviceCode.D, DeviceCode.W },
                BatchSize = 96, // Rシリーズは最新世代のため最大性能
                MaxConcurrentScans = 8
            };

            // Rシリーズ標準範囲（大容量）
            config.ScanRanges[DeviceCode.M] = new DeviceRange { Start = 0, End = 32767, Priority = 3 };
            config.ScanRanges[DeviceCode.X] = new DeviceRange { Start = 0, End = 8191, Priority = 2 };
            config.ScanRanges[DeviceCode.Y] = new DeviceRange { Start = 0, End = 8191, Priority = 2 };
            config.ScanRanges[DeviceCode.B] = new DeviceRange { Start = 0, End = 32767, Priority = 1 };
            config.ScanRanges[DeviceCode.F] = new DeviceRange { Start = 0, End = 32767, Priority = 1 };
            config.ScanRanges[DeviceCode.V] = new DeviceRange { Start = 0, End = 32767, Priority = 1 };
            config.ScanRanges[DeviceCode.L] = new DeviceRange { Start = 0, End = 32767, Priority = 1 };
            config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 1048575, Priority = 3 };
            config.ScanRanges[DeviceCode.W] = new DeviceRange { Start = 0, End = 32767, Priority = 2 };

            // プロセス制御対応CPUの場合の特殊範囲
            if (typeCode.IsProcessControlCapable())
            {
                _logger.LogDebug("Rシリーズプロセス制御CPU向けの特殊デバイスを追加");
                // プロセス制御専用デバイスの追加設定は将来実装
            }

            return config;
        }

        /// <summary>
        /// Lシリーズ用の探索設定を作成
        /// </summary>
        private DeviceDiscoveryConfiguration CreateLSeriesConfiguration(TypeCode typeCode)
        {
            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.B },
                WordDevices = new[] { DeviceCode.D },
                BatchSize = 32, // Lシリーズは安全性重視のため中程度
                MaxConcurrentScans = 3
            };

            // Lシリーズ標準範囲（安全PLC向け）
            config.ScanRanges[DeviceCode.M] = new DeviceRange { Start = 0, End = 8191, Priority = 3 };
            config.ScanRanges[DeviceCode.X] = new DeviceRange { Start = 0, End = 1023, Priority = 2 };
            config.ScanRanges[DeviceCode.Y] = new DeviceRange { Start = 0, End = 1023, Priority = 2 };
            config.ScanRanges[DeviceCode.B] = new DeviceRange { Start = 0, End = 8191, Priority = 1 };
            config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 32767, Priority = 3 };

            return config;
        }

        /// <summary>
        /// QSシリーズ用の探索設定を作成
        /// </summary>
        private DeviceDiscoveryConfiguration CreateQSSeriesConfiguration(TypeCode typeCode)
        {
            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y },
                WordDevices = new[] { DeviceCode.D },
                BatchSize = 16, // QSシリーズは単軸位置決め用のため小容量
                MaxConcurrentScans = 2
            };

            // QSシリーズ標準範囲（単軸位置決め用）
            config.ScanRanges[DeviceCode.M] = new DeviceRange { Start = 0, End = 2047, Priority = 3 };
            config.ScanRanges[DeviceCode.X] = new DeviceRange { Start = 0, End = 255, Priority = 2 };
            config.ScanRanges[DeviceCode.Y] = new DeviceRange { Start = 0, End = 255, Priority = 2 };
            config.ScanRanges[DeviceCode.D] = new DeviceRange { Start = 0, End = 4095, Priority = 3 };

            return config;
        }

        /// <summary>
        /// デフォルト探索設定を作成（不明な型式用）
        /// </summary>
        private DeviceDiscoveryConfiguration CreateDefaultConfiguration(TypeCode typeCode)
        {
            _logger.LogWarning("未知のTypeCode {TypeCode} に対してデフォルト設定を適用", typeCode);

            return new DeviceDiscoveryConfiguration
            {
                BitDevices = new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y },
                WordDevices = new[] { DeviceCode.D },
                BatchSize = 32,
                MaxConcurrentScans = 2,
                ScanRanges = new Dictionary<DeviceCode, DeviceRange>
                {
                    [DeviceCode.M] = new DeviceRange { Start = 0, End = 1023, Priority = 3 },
                    [DeviceCode.X] = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                    [DeviceCode.Y] = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                    [DeviceCode.D] = new DeviceRange { Start = 0, End = 1023, Priority = 3 }
                }
            };
        }

        /// <summary>
        /// カスタム探索設定を作成（ユーザー定義）
        /// </summary>
        /// <param name="bitDevices">ビットデバイス一覧</param>
        /// <param name="wordDevices">ワードデバイス一覧</param>
        /// <param name="customRanges">カスタム範囲設定</param>
        /// <returns>カスタム探索設定</returns>
        public DeviceDiscoveryConfiguration CreateCustomConfiguration(
            DeviceCode[] bitDevices,
            DeviceCode[] wordDevices,
            Dictionary<DeviceCode, DeviceRange> customRanges)
        {
            _logger.LogInformation("カスタム探索設定を作成中: ビット {BitCount}種類, ワード {WordCount}種類",
                bitDevices.Length, wordDevices.Length);

            return new DeviceDiscoveryConfiguration
            {
                BitDevices = bitDevices,
                WordDevices = wordDevices,
                ScanRanges = customRanges,
                BatchSize = 32,
                MaxConcurrentScans = 4
            };
        }

        /// <summary>
        /// 推奨デバイス探索設定を作成（高頻度使用デバイスのみ）
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>推奨デバイス設定</returns>
        public DeviceDiscoveryConfiguration CreateRecommendedConfiguration(TypeCode typeCode)
        {
            _logger.LogDebug("TypeCode {TypeCode} の推奨デバイス設定を構築中", typeCode);

            // CompleteDeviceMapから推奨デバイスを取得
            var (allDevices, allRanges) = CompleteDeviceMap.GetCompleteDeviceMap(typeCode);

            // 使用頻度の高いデバイスのみ選択（優先度4以上）
            var recommendedDevices = allDevices.Where(d =>
            {
                if (allRanges.TryGetValue(d, out var range))
                {
                    return range.Priority >= 4;
                }
                return CompleteDeviceMap.DeviceUsageStatistics.UsageFrequencyPriority.GetValueOrDefault(d, 3) >= 4;
            }).ToArray();

            var bitDevices = recommendedDevices.Where(d => CompleteDeviceMap.AllBitDevices.Contains(d)).ToArray();
            var wordDevices = recommendedDevices.Where(d => CompleteDeviceMap.AllWordDevices.Contains(d)).ToArray();

            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = bitDevices,
                WordDevices = wordDevices,
                ScanRanges = allRanges.Where(kvp => recommendedDevices.Contains(kvp.Key))
                                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            ApplyOptimizedSettings(config, typeCode);

            _logger.LogInformation("推奨デバイス設定構築完了: {TypeCode} - ビット{BitCount}種類, ワード{WordCount}種類",
                typeCode, bitDevices.Length, wordDevices.Length);

            return config;
        }

        /// <summary>
        /// 互換性情報を使用した高精度設定を作成
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>高精度設定</returns>
        public DeviceDiscoveryConfiguration CreateCompatibilityBasedConfiguration(TypeCode typeCode)
        {
            _logger.LogDebug("TypeCode {TypeCode} の互換性ベース設定を構築中", typeCode);

            var bitDeviceList = new List<DeviceCode>();
            var wordDeviceList = new List<DeviceCode>();
            var scanRanges = new Dictionary<DeviceCode, DeviceRange>();

            // 全デバイスコードに対して互換性をチェック
            foreach (var deviceCode in CompleteDeviceMap.AllBitDevices.Concat(CompleteDeviceMap.AllWordDevices))
            {
                var compatibilityInfo = DeviceCompatibilityMatrix.GetCompatibilityInfo(typeCode, deviceCode);

                // 標準対応以上のデバイスのみ含める
                if (compatibilityInfo.CompatibilityLevel >= DeviceCompatibilityMatrix.DeviceCompatibilityLevel.StandardSupport)
                {
                    if (CompleteDeviceMap.AllBitDevices.Contains(deviceCode))
                    {
                        bitDeviceList.Add(deviceCode);
                    }
                    else
                    {
                        wordDeviceList.Add(deviceCode);
                    }

                    // 推奨範囲を使用
                    scanRanges[deviceCode] = compatibilityInfo.RecommendedRange;
                }
            }

            var config = new DeviceDiscoveryConfiguration
            {
                BitDevices = bitDeviceList.ToArray(),
                WordDevices = wordDeviceList.ToArray(),
                ScanRanges = scanRanges
            };

            ApplyOptimizedSettings(config, typeCode);

            _logger.LogInformation("互換性ベース設定構築完了: {TypeCode} - ビット{BitCount}種類, ワード{WordCount}種類",
                typeCode, bitDeviceList.Count, wordDeviceList.Count);

            return config;
        }

        /// <summary>
        /// スマートプライオリティ設定を適用
        /// </summary>
        /// <param name="config">設定</param>
        /// <param name="typeCode">TypeCode</param>
        public void ApplySmartPriority(DeviceDiscoveryConfiguration config, TypeCode typeCode)
        {
            _logger.LogDebug("TypeCode {TypeCode} にスマートプライオリティを適用中", typeCode);

            // 使用頻度統計に基づいてスキャン順序を最適化
            var allDevices = config.BitDevices.Concat(config.WordDevices).ToArray();
            var prioritizedRanges = new Dictionary<DeviceCode, DeviceRange>();

            foreach (var device in allDevices)
            {
                if (config.ScanRanges.TryGetValue(device, out var range))
                {
                    var usagePriority = CompleteDeviceMap.DeviceUsageStatistics.UsageFrequencyPriority
                                       .GetValueOrDefault(device, 3);

                    // 使用頻度に基づいて優先度を調整
                    var adjustedRange = new DeviceRange
                    {
                        Start = range.Start,
                        End = range.End,
                        Priority = Math.Max(range.Priority, usagePriority)
                    };

                    prioritizedRanges[device] = adjustedRange;
                }
            }

            config.ScanRanges = prioritizedRanges;
            _logger.LogDebug("スマートプライオリティ適用完了: {DeviceCount}デバイス", allDevices.Length);
        }

        /// <summary>
        /// 設定の妥当性を検証
        /// </summary>
        /// <param name="config">検証対象の設定</param>
        /// <returns>検証結果</returns>
        public bool ValidateConfiguration(DeviceDiscoveryConfiguration config)
        {
            if (config == null)
            {
                _logger.LogError("探索設定がnullです");
                return false;
            }

            if (config.BitDevices.Length == 0 && config.WordDevices.Length == 0)
            {
                _logger.LogError("ビットデバイスとワードデバイスが両方とも空です");
                return false;
            }

            if (config.BatchSize <= 0 || config.BatchSize > 960)
            {
                _logger.LogError("バッチサイズが無効です: {BatchSize}", config.BatchSize);
                return false;
            }

            if (config.MaxConcurrentScans <= 0 || config.MaxConcurrentScans > 16)
            {
                _logger.LogError("最大同時スキャン数が無効です: {MaxScans}", config.MaxConcurrentScans);
                return false;
            }

            // 各デバイスの範囲設定をチェック
            var allDevices = config.BitDevices.Concat(config.WordDevices);
            foreach (var device in allDevices)
            {
                if (!config.ScanRanges.ContainsKey(device))
                {
                    _logger.LogWarning("デバイス {Device} の範囲設定が見つかりません", device);
                }
                else
                {
                    var range = config.ScanRanges[device];
                    if (range.Start > range.End)
                    {
                        _logger.LogError("デバイス {Device} の範囲設定が無効です: {Start} > {End}",
                            device, range.Start, range.End);
                        return false;
                    }
                }
            }

            _logger.LogDebug("探索設定の検証が完了しました");
            return true;
        }

        /// <summary>
        /// 探索設定の概要情報を取得
        /// </summary>
        /// <param name="config">設定</param>
        /// <returns>概要文字列</returns>
        public string GetConfigurationSummary(DeviceDiscoveryConfiguration config)
        {
            if (config == null) return "設定なし";

            var totalDevices = config.ScanRanges.Values.Sum(r => (int)r.Count);
            var priorityDevices = config.ScanRanges.Where(kvp => kvp.Value.Priority >= 3).Count();
            var highFrequencyDevices = config.ScanRanges.Count(kvp =>
                CompleteDeviceMap.DeviceUsageStatistics.UsageFrequencyPriority.GetValueOrDefault(kvp.Key, 3) >= 4);

            return $"探索設定概要: " +
                   $"ビット{config.BitDevices.Length}種類, " +
                   $"ワード{config.WordDevices.Length}種類, " +
                   $"総アドレス数{totalDevices}, " +
                   $"高優先度{priorityDevices}種類, " +
                   $"高頻度使用{highFrequencyDevices}種類, " +
                   $"バッチサイズ{config.BatchSize}, " +
                   $"最大同時スキャン{config.MaxConcurrentScans}";
        }

        /// <summary>
        /// デバイス統計情報を取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>統計情報</returns>
        public DeviceDiscoveryStatistics GetDeviceStatistics(TypeCode typeCode)
        {
            var (allDevices, allRanges) = CompleteDeviceMap.GetCompleteDeviceMap(typeCode);

            var statistics = new DeviceDiscoveryStatistics
            {
                TypeCode = typeCode,
                TotalSupportedDevices = allDevices.Length,
                SupportedBitDevices = allDevices.Count(d => CompleteDeviceMap.AllBitDevices.Contains(d)),
                SupportedWordDevices = allDevices.Count(d => CompleteDeviceMap.AllWordDevices.Contains(d)),
                TotalAddressSpace = allRanges.Values.Sum(r => (long)r.Count),
                HighPriorityDevices = allRanges.Count(kvp => kvp.Value.Priority >= 4),
                RecommendedBatchSize = GetOptimizedSettingsForTypeCode(typeCode).BatchSize
            };

            return statistics;
        }
    }

    /// <summary>
    /// デバイス探索統計情報
    /// </summary>
    public class DeviceDiscoveryStatistics
    {
        /// <summary>TypeCode</summary>
        public TypeCode TypeCode { get; set; }

        /// <summary>総対応デバイス数</summary>
        public int TotalSupportedDevices { get; set; }

        /// <summary>対応ビットデバイス数</summary>
        public int SupportedBitDevices { get; set; }

        /// <summary>対応ワードデバイス数</summary>
        public int SupportedWordDevices { get; set; }

        /// <summary>総アドレス空間</summary>
        public long TotalAddressSpace { get; set; }

        /// <summary>高優先度デバイス数</summary>
        public int HighPriorityDevices { get; set; }

        /// <summary>推奨バッチサイズ</summary>
        public int RecommendedBatchSize { get; set; }

        /// <summary>
        /// 統計情報の文字列表現
        /// </summary>
        /// <returns>統計情報文字列</returns>
        public override string ToString()
        {
            return $"デバイス統計: {TypeCode} - 総デバイス{TotalSupportedDevices}個 " +
                   $"(ビット{SupportedBitDevices}/ワード{SupportedWordDevices}), " +
                   $"総アドレス{TotalAddressSpace:N0}, 高優先度{HighPriorityDevices}個, " +
                   $"推奨バッチ{RecommendedBatchSize}";
        }
    }
}