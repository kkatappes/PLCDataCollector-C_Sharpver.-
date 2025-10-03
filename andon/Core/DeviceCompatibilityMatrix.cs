using System;
using System.Collections.Generic;
using System.Linq;
using SlmpClient.Constants;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Core
{
    /// <summary>
    /// デバイス互換性マトリックス
    /// TypeCode × DeviceCode の完全な互換性マトリックスを提供
    /// 各PLCシリーズで利用可能なデバイスの正確な判定を行う
    /// </summary>
    public static class DeviceCompatibilityMatrix
    {
        /// <summary>
        /// デバイス互換性情報
        /// </summary>
        public class DeviceCompatibilityInfo
        {
            /// <summary>対応状況</summary>
            public DeviceCompatibilityLevel CompatibilityLevel { get; set; }

            /// <summary>推奨範囲</summary>
            public DeviceRange RecommendedRange { get; set; } = new();

            /// <summary>最大範囲</summary>
            public DeviceRange MaximumRange { get; set; } = new();

            /// <summary>推奨バッチサイズ</summary>
            public int RecommendedBatchSize { get; set; } = 32;

            /// <summary>制限事項</summary>
            public List<string> Limitations { get; set; } = new();

            /// <summary>使用頻度優先度（1-5, 5が最高）</summary>
            public int UsagePriority { get; set; } = 3;

            /// <summary>備考</summary>
            public string Notes { get; set; } = string.Empty;
        }

        /// <summary>
        /// デバイス対応レベル
        /// </summary>
        public enum DeviceCompatibilityLevel
        {
            /// <summary>非対応</summary>
            NotSupported = 0,
            /// <summary>制限付き対応</summary>
            LimitedSupport = 1,
            /// <summary>標準対応</summary>
            StandardSupport = 2,
            /// <summary>完全対応</summary>
            FullSupport = 3,
            /// <summary>推奨対応</summary>
            RecommendedSupport = 4
        }

        /// <summary>
        /// 完全互換性マトリックス
        /// [TypeCode][DeviceCode] = DeviceCompatibilityInfo
        /// </summary>
        private static readonly Dictionary<TypeCode, Dictionary<DeviceCode, DeviceCompatibilityInfo>> _compatibilityMatrix
            = new();

        /// <summary>
        /// 静的コンストラクタ：互換性マトリックスを初期化
        /// </summary>
        static DeviceCompatibilityMatrix()
        {
            InitializeCompatibilityMatrix();
        }

        /// <summary>
        /// デバイス互換性情報を取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <param name="deviceCode">DeviceCode</param>
        /// <returns>互換性情報</returns>
        public static DeviceCompatibilityInfo GetCompatibilityInfo(TypeCode typeCode, DeviceCode deviceCode)
        {
            if (_compatibilityMatrix.TryGetValue(typeCode, out var deviceMap) &&
                deviceMap.TryGetValue(deviceCode, out var info))
            {
                return info;
            }

            // デフォルト情報を返す
            return new DeviceCompatibilityInfo
            {
                CompatibilityLevel = DeviceCompatibilityLevel.NotSupported,
                Notes = "未対応または不明なデバイス"
            };
        }

        /// <summary>
        /// 指定されたTypeCodeで対応可能なデバイス一覧を取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <param name="minimumLevel">最低対応レベル</param>
        /// <returns>対応デバイス一覧</returns>
        public static DeviceCode[] GetSupportedDevices(TypeCode typeCode, DeviceCompatibilityLevel minimumLevel = DeviceCompatibilityLevel.StandardSupport)
        {
            if (!_compatibilityMatrix.TryGetValue(typeCode, out var deviceMap))
                return Array.Empty<DeviceCode>();

            return deviceMap
                .Where(kvp => kvp.Value.CompatibilityLevel >= minimumLevel)
                .OrderByDescending(kvp => kvp.Value.UsagePriority)
                .ThenBy(kvp => kvp.Key.ToString())
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        /// <summary>
        /// 推奨デバイス一覧を取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>推奨デバイス一覧</returns>
        public static DeviceCode[] GetRecommendedDevices(TypeCode typeCode)
        {
            return GetSupportedDevices(typeCode, DeviceCompatibilityLevel.RecommendedSupport);
        }

        /// <summary>
        /// 完全対応デバイス一覧を取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>完全対応デバイス一覧</returns>
        public static DeviceCode[] GetFullySupportedDevices(TypeCode typeCode)
        {
            return GetSupportedDevices(typeCode, DeviceCompatibilityLevel.FullSupport);
        }

        /// <summary>
        /// 基本デバイス一覧を取得（使用頻度の高いデバイス）
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>基本デバイス一覧</returns>
        public static DeviceCode[] GetBasicDevices(TypeCode typeCode)
        {
            if (!_compatibilityMatrix.TryGetValue(typeCode, out var deviceMap))
                return Array.Empty<DeviceCode>();

            return deviceMap
                .Where(kvp => kvp.Value.CompatibilityLevel >= DeviceCompatibilityLevel.StandardSupport &&
                             kvp.Value.UsagePriority >= 4)
                .OrderByDescending(kvp => kvp.Value.UsagePriority)
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        /// <summary>
        /// デバイスが4バイトアドレス必須かどうかを判定
        /// </summary>
        /// <param name="deviceCode">DeviceCode</param>
        /// <returns>4バイトアドレス必須の場合はtrue</returns>
        public static bool Requires4ByteAddress(DeviceCode deviceCode)
        {
            return deviceCode.Is4ByteAddress();
        }

        /// <summary>
        /// デバイスが16進アドレス表現を使用するかどうかを判定
        /// </summary>
        /// <param name="deviceCode">DeviceCode</param>
        /// <returns>16進アドレス表現を使用する場合はtrue</returns>
        public static bool UsesHexAddress(DeviceCode deviceCode)
        {
            return deviceCode.IsHexAddress();
        }

        /// <summary>
        /// 互換性マトリックスを初期化
        /// </summary>
        private static void InitializeCompatibilityMatrix()
        {
            // FXシリーズの互換性情報を初期化
            InitializeFXSeriesCompatibility();

            // Qシリーズの互換性情報を初期化
            InitializeQSeriesCompatibility();

            // Rシリーズの互換性情報を初期化
            InitializeRSeriesCompatibility();

            // Lシリーズの互換性情報を初期化
            InitializeLSeriesCompatibility();

            // QSシリーズの互換性情報を初期化
            InitializeQSSeriesCompatibility();
        }

        /// <summary>
        /// FXシリーズ互換性情報を初期化
        /// </summary>
        private static void InitializeFXSeriesCompatibility()
        {
            var fxTypes = new[] { TypeCode.FX3G, TypeCode.FX3GC, TypeCode.FX3U, TypeCode.FX3UC };
            var fx5Types = new[] { TypeCode.FX5U, TypeCode.FX5UC, TypeCode.FX5UJ };

            // FX3系の設定
            foreach (var typeCode in fxTypes)
            {
                _compatibilityMatrix[typeCode] = new Dictionary<DeviceCode, DeviceCompatibilityInfo>
                {
                    // 高頻度使用デバイス（推奨）
                    [DeviceCode.M] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 7999, Priority = 5 },
                        MaximumRange = new DeviceRange { Start = 0, End = 7999, Priority = 5 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 5,
                        Notes = "メインの内部リレー"
                    },
                    [DeviceCode.D] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 7999, Priority = 5 },
                        MaximumRange = new DeviceRange { Start = 0, End = 7999, Priority = 5 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 5,
                        Notes = "メインのデータレジスタ"
                    },
                    [DeviceCode.X] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 177, Priority = 4 },
                        MaximumRange = new DeviceRange { Start = 0, End = 177, Priority = 4 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 5,
                        Notes = "入力リレー（16進アドレス）"
                    },
                    [DeviceCode.Y] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 177, Priority = 4 },
                        MaximumRange = new DeviceRange { Start = 0, End = 177, Priority = 4 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 5,
                        Notes = "出力リレー（16進アドレス）"
                    },

                    // 標準対応デバイス
                    [DeviceCode.SM] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 2,
                        Notes = "特殊リレー"
                    },
                    [DeviceCode.SD] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 2,
                        Notes = "特殊レジスタ"
                    },
                    [DeviceCode.TS] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 4,
                        Notes = "タイマー接点"
                    },
                    [DeviceCode.TC] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 4,
                        Notes = "タイマーコイル"
                    },
                    [DeviceCode.TN] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 4,
                        Notes = "タイマー現在値"
                    },
                    [DeviceCode.CS] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 4,
                        Notes = "カウンタ接点"
                    },
                    [DeviceCode.CC] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 4,
                        Notes = "カウンタコイル"
                    },
                    [DeviceCode.CN] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 3 },
                        RecommendedBatchSize = 8,
                        UsagePriority = 4,
                        Notes = "カウンタ現在値"
                    }
                };
            }

            // FX5系の設定（FX3系に加えて拡張デバイス対応）
            foreach (var typeCode in fx5Types)
            {
                _compatibilityMatrix[typeCode] = new Dictionary<DeviceCode, DeviceCompatibilityInfo>
                {
                    // FX3系と同じ設定をベースに拡張
                    [DeviceCode.M] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 15999, Priority = 5 },
                        MaximumRange = new DeviceRange { Start = 0, End = 15999, Priority = 5 },
                        RecommendedBatchSize = 32,
                        UsagePriority = 5,
                        Notes = "FX5系拡張範囲"
                    },
                    [DeviceCode.D] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 15999, Priority = 5 },
                        MaximumRange = new DeviceRange { Start = 0, End = 15999, Priority = 5 },
                        RecommendedBatchSize = 32,
                        UsagePriority = 5,
                        Notes = "FX5系拡張範囲"
                    },
                    [DeviceCode.X] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 4 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 4 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 5,
                        Notes = "FX5系拡張範囲"
                    },
                    [DeviceCode.Y] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 4 },
                        MaximumRange = new DeviceRange { Start = 0, End = 255, Priority = 4 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 5,
                        Notes = "FX5系拡張範囲"
                    },

                    // FX5系専用拡張デバイス
                    [DeviceCode.L] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 3999, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 3999, Priority = 3 },
                        RecommendedBatchSize = 16,
                        UsagePriority = 3,
                        Notes = "FX5系ラッチリレー"
                    },
                    [DeviceCode.R] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 32767, Priority = 3 },
                        MaximumRange = new DeviceRange { Start = 0, End = 32767, Priority = 3 },
                        RecommendedBatchSize = 32,
                        UsagePriority = 3,
                        Notes = "FX5系ファイルレジスタ"
                    },
                    [DeviceCode.Z] = new DeviceCompatibilityInfo
                    {
                        CompatibilityLevel = DeviceCompatibilityLevel.StandardSupport,
                        RecommendedRange = new DeviceRange { Start = 0, End = 15, Priority = 2 },
                        MaximumRange = new DeviceRange { Start = 0, End = 15, Priority = 2 },
                        RecommendedBatchSize = 4,
                        UsagePriority = 2,
                        Notes = "FX5系インデックスレジスタ"
                    }
                };

                // FX3系の基本設定も追加
                var fx3Settings = _compatibilityMatrix[TypeCode.FX3U];
                foreach (var kvp in fx3Settings)
                {
                    if (!_compatibilityMatrix[typeCode].ContainsKey(kvp.Key))
                    {
                        _compatibilityMatrix[typeCode][kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Qシリーズ互換性情報を初期化
        /// </summary>
        private static void InitializeQSeriesCompatibility()
        {
            var basicQTypes = new[] { TypeCode.Q00JCPU, TypeCode.Q00CPU, TypeCode.Q01CPU, TypeCode.Q02CPU };
            var highPerfQTypes = new[] { TypeCode.Q06HCPU, TypeCode.Q12HCPU, TypeCode.Q25HCPU };

            // 基本Q CPUの設定
            foreach (var typeCode in basicQTypes)
            {
                _compatibilityMatrix[typeCode] = CreateQSeriesBasicDeviceMap();
            }

            // 高性能Q CPUの設定
            foreach (var typeCode in highPerfQTypes)
            {
                _compatibilityMatrix[typeCode] = CreateQSeriesHighPerfDeviceMap();
            }
        }

        /// <summary>
        /// Rシリーズ互換性情報を初期化
        /// </summary>
        private static void InitializeRSeriesCompatibility()
        {
            var rTypes = new[]
            {
                TypeCode.R00CPU, TypeCode.R01CPU, TypeCode.R02CPU,
                TypeCode.R04CPU, TypeCode.R08CPU, TypeCode.R16CPU,
                TypeCode.R32CPU, TypeCode.R120CPU
            };

            foreach (var typeCode in rTypes)
            {
                _compatibilityMatrix[typeCode] = CreateRSeriesDeviceMap();
            }
        }

        /// <summary>
        /// Lシリーズ互換性情報を初期化
        /// </summary>
        private static void InitializeLSeriesCompatibility()
        {
            var lTypes = new[]
            {
                TypeCode.L02SCPU, TypeCode.L02CPU, TypeCode.L06CPU,
                TypeCode.L26CPU, TypeCode.L26CPU_BT
            };

            foreach (var typeCode in lTypes)
            {
                _compatibilityMatrix[typeCode] = CreateLSeriesDeviceMap();
            }
        }

        /// <summary>
        /// QSシリーズ互換性情報を初期化
        /// </summary>
        private static void InitializeQSSeriesCompatibility()
        {
            _compatibilityMatrix[TypeCode.QS001CPU] = new Dictionary<DeviceCode, DeviceCompatibilityInfo>
            {
                [DeviceCode.M] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 5 },
                    UsagePriority = 5,
                    Notes = "QS単軸位置決め用"
                },
                [DeviceCode.X] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 4 },
                    UsagePriority = 5
                },
                [DeviceCode.Y] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 255, Priority = 4 },
                    UsagePriority = 5
                },
                [DeviceCode.D] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 4095, Priority = 5 },
                    UsagePriority = 5
                }
            };
        }

        private static Dictionary<DeviceCode, DeviceCompatibilityInfo> CreateQSeriesBasicDeviceMap()
        {
            return new Dictionary<DeviceCode, DeviceCompatibilityInfo>
            {
                // 推奨デバイス
                [DeviceCode.M] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 8191, Priority = 5 },
                    RecommendedBatchSize = 64,
                    UsagePriority = 5
                },
                [DeviceCode.D] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 12287, Priority = 5 },
                    RecommendedBatchSize = 64,
                    UsagePriority = 5
                },
                [DeviceCode.X] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 4 },
                    RecommendedBatchSize = 64,
                    UsagePriority = 5
                },
                [DeviceCode.Y] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 4 },
                    RecommendedBatchSize = 64,
                    UsagePriority = 5
                },

                // フル対応デバイス
                [DeviceCode.B] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 8191, Priority = 3 },
                    RecommendedBatchSize = 64,
                    UsagePriority = 3
                },
                [DeviceCode.W] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 8191, Priority = 3 },
                    RecommendedBatchSize = 64,
                    UsagePriority = 3
                },
                [DeviceCode.F] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 2 },
                    RecommendedBatchSize = 32,
                    UsagePriority = 2
                },
                [DeviceCode.V] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 2 },
                    RecommendedBatchSize = 32,
                    UsagePriority = 2
                }
            };
        }

        private static Dictionary<DeviceCode, DeviceCompatibilityInfo> CreateQSeriesHighPerfDeviceMap()
        {
            var baseMap = CreateQSeriesBasicDeviceMap();

            // 高性能CPU専用の拡張デバイスを追加
            baseMap[DeviceCode.LTS] = new DeviceCompatibilityInfo
            {
                CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 2 }
            };
            baseMap[DeviceCode.LTC] = new DeviceCompatibilityInfo
            {
                CompatibilityLevel = DeviceCompatibilityLevel.FullSupport,
                RecommendedRange = new DeviceRange { Start = 0, End = 2047, Priority = 2 }
            };

            return baseMap;
        }

        private static Dictionary<DeviceCode, DeviceCompatibilityInfo> CreateRSeriesDeviceMap()
        {
            return new Dictionary<DeviceCode, DeviceCompatibilityInfo>
            {
                [DeviceCode.M] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 32767, Priority = 5 },
                    RecommendedBatchSize = 96,
                    UsagePriority = 5
                },
                [DeviceCode.D] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 1048575, Priority = 5 },
                    RecommendedBatchSize = 96,
                    UsagePriority = 5
                },
                // 他のRシリーズデバイスも同様に設定...
            };
        }

        private static Dictionary<DeviceCode, DeviceCompatibilityInfo> CreateLSeriesDeviceMap()
        {
            return new Dictionary<DeviceCode, DeviceCompatibilityInfo>
            {
                [DeviceCode.M] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 8191, Priority = 5 },
                    RecommendedBatchSize = 32,
                    UsagePriority = 5
                },
                [DeviceCode.D] = new DeviceCompatibilityInfo
                {
                    CompatibilityLevel = DeviceCompatibilityLevel.RecommendedSupport,
                    RecommendedRange = new DeviceRange { Start = 0, End = 32767, Priority = 5 },
                    RecommendedBatchSize = 32,
                    UsagePriority = 5
                }
                // 他のLシリーズデバイスも同様に設定...
            };
        }
    }
}