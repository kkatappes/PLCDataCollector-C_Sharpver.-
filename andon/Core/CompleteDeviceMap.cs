using System;
using System.Collections.Generic;
using System.Linq;
using SlmpClient.Constants;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Core
{
    /// <summary>
    /// 完全デバイスマップ
    /// 全39種類のデバイスコードとPLCシリーズ別対応状況の完全マップ
    /// 「抜けなく確認できる仕様」を実現するための基盤クラス
    /// </summary>
    public static class CompleteDeviceMap
    {
        /// <summary>
        /// 全ビットデバイス一覧（21種類）
        /// </summary>
        public static readonly DeviceCode[] AllBitDevices = new[]
        {
            // 基本ビットデバイス
            DeviceCode.SM,     // 特殊リレー
            DeviceCode.X,      // 入力リレー
            DeviceCode.Y,      // 出力リレー
            DeviceCode.M,      // 内部リレー
            DeviceCode.L,      // ラッチリレー
            DeviceCode.F,      // アナンシエータ
            DeviceCode.V,      // エッジリレー
            DeviceCode.B,      // リンクリレー
            DeviceCode.SB,     // リンク特殊リレー
            DeviceCode.DX,     // 直接入力
            DeviceCode.DY,     // 直接出力

            // タイマーデバイス
            DeviceCode.TS,     // タイマー接点
            DeviceCode.TC,     // タイマーコイル
            DeviceCode.LTS,    // 長時間タイマー接点
            DeviceCode.LTC,    // 長時間タイマーコイル

            // カウンタデバイス
            DeviceCode.SS,     // 積算タイマー接点
            DeviceCode.SC,     // 積算タイマーコイル
            DeviceCode.CS,     // カウンタ接点
            DeviceCode.CC,     // カウンタコイル
            DeviceCode.LSTS,   // 長時間積算タイマー接点
            DeviceCode.LSTC,   // 長時間積算タイマーコイル
            DeviceCode.LCS,    // 長カウンタ接点
            DeviceCode.LCC,    // 長カウンタコイル
        };

        /// <summary>
        /// 全ワードデバイス一覧（16種類）
        /// </summary>
        public static readonly DeviceCode[] AllWordDevices = new[]
        {
            // 基本ワードデバイス
            DeviceCode.SD,     // 特殊レジスタ
            DeviceCode.D,      // データレジスタ
            DeviceCode.W,      // リンクレジスタ
            DeviceCode.SW,     // リンク特殊レジスタ
            DeviceCode.Z,      // インデックスレジスタ
            DeviceCode.LZ,     // 長インデックスレジスタ
            DeviceCode.R,      // ファイルレジスタ
            DeviceCode.ZR,     // 拡張ファイルレジスタ
            DeviceCode.RD,     // 拡張データレジスタ

            // タイマー・カウンタ現在値
            DeviceCode.TN,     // タイマー現在値
            DeviceCode.LTN,    // 長時間タイマー現在値
            DeviceCode.SN,     // 積算タイマー現在値
            DeviceCode.CN,     // カウンタ現在値
            DeviceCode.LSTN,   // 長時間積算タイマー現在値
            DeviceCode.LCN,    // 長カウンタ現在値
        };

        /// <summary>
        /// FXシリーズ対応デバイスマップ
        /// </summary>
        public static class FXSeries
        {
            /// <summary>基本デバイス（従来実装）</summary>
            public static readonly DeviceCode[] BasicDevices = new[]
            {
                DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.D
            };

            /// <summary>完全対応デバイス（FX3G/FX3U/FX3UC）</summary>
            public static readonly DeviceCode[] FX3_CompleteDevices = new[]
            {
                // ビットデバイス
                DeviceCode.SM, DeviceCode.X, DeviceCode.Y, DeviceCode.M,
                DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC,

                // ワードデバイス
                DeviceCode.SD, DeviceCode.D, DeviceCode.TN, DeviceCode.CN
            };

            /// <summary>完全対応デバイス（FX5U/FX5UC/FX5UJ）</summary>
            public static readonly DeviceCode[] FX5_CompleteDevices = new[]
            {
                // ビットデバイス
                DeviceCode.SM, DeviceCode.X, DeviceCode.Y, DeviceCode.M, DeviceCode.L,
                DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC,

                // ワードデバイス
                DeviceCode.SD, DeviceCode.D, DeviceCode.R, DeviceCode.Z,
                DeviceCode.TN, DeviceCode.CN
            };

            /// <summary>推奨範囲（FX3系）</summary>
            public static readonly Dictionary<DeviceCode, DeviceRange> FX3_RecommendedRanges = new()
            {
                [DeviceCode.SM] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.X] = new DeviceRange { Start = 0, End = 177, Priority = 2 },
                [DeviceCode.Y] = new DeviceRange { Start = 0, End = 177, Priority = 2 },
                [DeviceCode.M] = new DeviceRange { Start = 0, End = 7999, Priority = 3 },
                [DeviceCode.TS] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.TC] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.CS] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.CC] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.SD] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.D] = new DeviceRange { Start = 0, End = 7999, Priority = 3 },
                [DeviceCode.TN] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.CN] = new DeviceRange { Start = 0, End = 255, Priority = 1 }
            };

            /// <summary>推奨範囲（FX5系）</summary>
            public static readonly Dictionary<DeviceCode, DeviceRange> FX5_RecommendedRanges = new()
            {
                [DeviceCode.SM] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.X] = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                [DeviceCode.Y] = new DeviceRange { Start = 0, End = 255, Priority = 2 },
                [DeviceCode.M] = new DeviceRange { Start = 0, End = 15999, Priority = 3 },
                [DeviceCode.L] = new DeviceRange { Start = 0, End = 3999, Priority = 2 },
                [DeviceCode.TS] = new DeviceRange { Start = 0, End = 511, Priority = 1 },
                [DeviceCode.TC] = new DeviceRange { Start = 0, End = 511, Priority = 1 },
                [DeviceCode.CS] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.CC] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.SD] = new DeviceRange { Start = 0, End = 511, Priority = 1 },
                [DeviceCode.D] = new DeviceRange { Start = 0, End = 15999, Priority = 3 },
                [DeviceCode.R] = new DeviceRange { Start = 0, End = 32767, Priority = 2 },
                [DeviceCode.Z] = new DeviceRange { Start = 0, End = 15, Priority = 1 },
                [DeviceCode.TN] = new DeviceRange { Start = 0, End = 511, Priority = 1 },
                [DeviceCode.CN] = new DeviceRange { Start = 0, End = 255, Priority = 1 }
            };
        }

        /// <summary>
        /// Qシリーズ対応デバイスマップ
        /// </summary>
        public static class QSeries
        {
            /// <summary>基本デバイス（従来実装）</summary>
            public static readonly DeviceCode[] BasicDevices = new[]
            {
                DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.B,
                DeviceCode.F, DeviceCode.V, DeviceCode.D, DeviceCode.W
            };

            /// <summary>完全対応デバイス（Q00/Q01/Q02 基本CPU）</summary>
            public static readonly DeviceCode[] BasicCPU_CompleteDevices = new[]
            {
                // ビットデバイス
                DeviceCode.SM, DeviceCode.X, DeviceCode.Y, DeviceCode.M, DeviceCode.L,
                DeviceCode.F, DeviceCode.V, DeviceCode.B, DeviceCode.SB,
                DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC,
                DeviceCode.SS, DeviceCode.SC,

                // ワードデバイス
                DeviceCode.SD, DeviceCode.D, DeviceCode.W, DeviceCode.SW,
                DeviceCode.R, DeviceCode.ZR, DeviceCode.Z,
                DeviceCode.TN, DeviceCode.CN, DeviceCode.SN
            };

            /// <summary>完全対応デバイス（Q06H/Q12H/Q25H 高性能CPU）</summary>
            public static readonly DeviceCode[] HighPerformanceCPU_CompleteDevices = new[]
            {
                // 基本CPU対応デバイス + 拡張デバイス
                DeviceCode.SM, DeviceCode.X, DeviceCode.Y, DeviceCode.M, DeviceCode.L,
                DeviceCode.F, DeviceCode.V, DeviceCode.B, DeviceCode.SB,
                DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC,
                DeviceCode.SS, DeviceCode.SC, DeviceCode.LTS, DeviceCode.LTC,
                DeviceCode.LSTS, DeviceCode.LSTC, DeviceCode.LCS, DeviceCode.LCC,

                // ワードデバイス
                DeviceCode.SD, DeviceCode.D, DeviceCode.W, DeviceCode.SW,
                DeviceCode.R, DeviceCode.ZR, DeviceCode.Z, DeviceCode.LZ,
                DeviceCode.TN, DeviceCode.CN, DeviceCode.SN,
                DeviceCode.LTN, DeviceCode.LSTN, DeviceCode.LCN
            };

            /// <summary>推奨範囲（基本CPU）</summary>
            public static readonly Dictionary<DeviceCode, DeviceRange> BasicCPU_RecommendedRanges = new()
            {
                [DeviceCode.SM] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.X] = new DeviceRange { Start = 0, End = 2047, Priority = 2 },
                [DeviceCode.Y] = new DeviceRange { Start = 0, End = 2047, Priority = 2 },
                [DeviceCode.M] = new DeviceRange { Start = 0, End = 8191, Priority = 3 },
                [DeviceCode.L] = new DeviceRange { Start = 0, End = 8191, Priority = 2 },
                [DeviceCode.F] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.V] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.B] = new DeviceRange { Start = 0, End = 8191, Priority = 1 },
                [DeviceCode.SB] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.TS] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.TC] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.CS] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.CC] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SS] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SC] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SD] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.D] = new DeviceRange { Start = 0, End = 12287, Priority = 3 },
                [DeviceCode.W] = new DeviceRange { Start = 0, End = 8191, Priority = 2 },
                [DeviceCode.SW] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.R] = new DeviceRange { Start = 0, End = 32767, Priority = 2 },
                [DeviceCode.ZR] = new DeviceRange { Start = 0, End = 1048575, Priority = 2 },
                [DeviceCode.Z] = new DeviceRange { Start = 0, End = 15, Priority = 1 },
                [DeviceCode.TN] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.CN] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SN] = new DeviceRange { Start = 0, End = 1023, Priority = 1 }
            };
        }

        /// <summary>
        /// Rシリーズ対応デバイスマップ
        /// </summary>
        public static class RSeries
        {
            /// <summary>基本デバイス（従来実装）</summary>
            public static readonly DeviceCode[] BasicDevices = new[]
            {
                DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.B,
                DeviceCode.F, DeviceCode.V, DeviceCode.L, DeviceCode.D, DeviceCode.W
            };

            /// <summary>完全対応デバイス（R00/R01/R02 基本CPU）</summary>
            public static readonly DeviceCode[] BasicCPU_CompleteDevices = new[]
            {
                // ビットデバイス
                DeviceCode.SM, DeviceCode.X, DeviceCode.Y, DeviceCode.M, DeviceCode.L,
                DeviceCode.F, DeviceCode.V, DeviceCode.B, DeviceCode.SB,
                DeviceCode.TS, DeviceCode.TC, DeviceCode.CS, DeviceCode.CC,
                DeviceCode.SS, DeviceCode.SC, DeviceCode.LTS, DeviceCode.LTC,
                DeviceCode.LSTS, DeviceCode.LSTC, DeviceCode.LCS, DeviceCode.LCC,

                // ワードデバイス
                DeviceCode.SD, DeviceCode.D, DeviceCode.W, DeviceCode.SW,
                DeviceCode.R, DeviceCode.ZR, DeviceCode.RD, DeviceCode.Z, DeviceCode.LZ,
                DeviceCode.TN, DeviceCode.CN, DeviceCode.SN,
                DeviceCode.LTN, DeviceCode.LSTN, DeviceCode.LCN
            };

            /// <summary>推奨範囲（基本CPU）</summary>
            public static readonly Dictionary<DeviceCode, DeviceRange> BasicCPU_RecommendedRanges = new()
            {
                [DeviceCode.SM] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.X] = new DeviceRange { Start = 0, End = 8191, Priority = 2 },
                [DeviceCode.Y] = new DeviceRange { Start = 0, End = 8191, Priority = 2 },
                [DeviceCode.M] = new DeviceRange { Start = 0, End = 32767, Priority = 3 },
                [DeviceCode.L] = new DeviceRange { Start = 0, End = 32767, Priority = 2 },
                [DeviceCode.F] = new DeviceRange { Start = 0, End = 32767, Priority = 1 },
                [DeviceCode.V] = new DeviceRange { Start = 0, End = 32767, Priority = 1 },
                [DeviceCode.B] = new DeviceRange { Start = 0, End = 32767, Priority = 1 },
                [DeviceCode.SB] = new DeviceRange { Start = 0, End = 8191, Priority = 1 },
                [DeviceCode.TS] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.TC] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.CS] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.CC] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SS] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.SC] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LTS] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LTC] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LSTS] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LSTC] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LCS] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.LCC] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SD] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.D] = new DeviceRange { Start = 0, End = 1048575, Priority = 3 },
                [DeviceCode.W] = new DeviceRange { Start = 0, End = 32767, Priority = 2 },
                [DeviceCode.SW] = new DeviceRange { Start = 0, End = 8191, Priority = 1 },
                [DeviceCode.R] = new DeviceRange { Start = 0, End = 1048575, Priority = 2 },
                [DeviceCode.ZR] = new DeviceRange { Start = 0, End = 1048575, Priority = 2 },
                [DeviceCode.RD] = new DeviceRange { Start = 0, End = 1048575, Priority = 2 },
                [DeviceCode.Z] = new DeviceRange { Start = 0, End = 15, Priority = 1 },
                [DeviceCode.LZ] = new DeviceRange { Start = 0, End = 15, Priority = 1 },
                [DeviceCode.TN] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.CN] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.SN] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LTN] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LSTN] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.LCN] = new DeviceRange { Start = 0, End = 1023, Priority = 1 }
            };
        }

        /// <summary>
        /// Lシリーズ対応デバイスマップ
        /// </summary>
        public static class LSeries
        {
            /// <summary>基本デバイス（従来実装）</summary>
            public static readonly DeviceCode[] BasicDevices = new[]
            {
                DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.B, DeviceCode.D
            };

            /// <summary>完全対応デバイス（L02/L06/L26）</summary>
            public static readonly DeviceCode[] CompleteDevices = new[]
            {
                // ビットデバイス
                DeviceCode.SM, DeviceCode.X, DeviceCode.Y, DeviceCode.M, DeviceCode.L,
                DeviceCode.B, DeviceCode.SB, DeviceCode.TS, DeviceCode.TC,
                DeviceCode.CS, DeviceCode.CC,

                // ワードデバイス
                DeviceCode.SD, DeviceCode.D, DeviceCode.W, DeviceCode.SW,
                DeviceCode.R, DeviceCode.Z, DeviceCode.TN, DeviceCode.CN
            };

            /// <summary>推奨範囲</summary>
            public static readonly Dictionary<DeviceCode, DeviceRange> RecommendedRanges = new()
            {
                [DeviceCode.SM] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.X] = new DeviceRange { Start = 0, End = 1023, Priority = 2 },
                [DeviceCode.Y] = new DeviceRange { Start = 0, End = 1023, Priority = 2 },
                [DeviceCode.M] = new DeviceRange { Start = 0, End = 8191, Priority = 3 },
                [DeviceCode.L] = new DeviceRange { Start = 0, End = 8191, Priority = 2 },
                [DeviceCode.B] = new DeviceRange { Start = 0, End = 8191, Priority = 1 },
                [DeviceCode.SB] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.TS] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.TC] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.CS] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.CC] = new DeviceRange { Start = 0, End = 255, Priority = 1 },
                [DeviceCode.SD] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.D] = new DeviceRange { Start = 0, End = 32767, Priority = 3 },
                [DeviceCode.W] = new DeviceRange { Start = 0, End = 8191, Priority = 2 },
                [DeviceCode.SW] = new DeviceRange { Start = 0, End = 2047, Priority = 1 },
                [DeviceCode.R] = new DeviceRange { Start = 0, End = 32767, Priority = 2 },
                [DeviceCode.Z] = new DeviceRange { Start = 0, End = 15, Priority = 1 },
                [DeviceCode.TN] = new DeviceRange { Start = 0, End = 1023, Priority = 1 },
                [DeviceCode.CN] = new DeviceRange { Start = 0, End = 255, Priority = 1 }
            };
        }

        /// <summary>
        /// TypeCodeからシリーズ種別とCPU性能を判定してデバイスマップを取得
        /// </summary>
        /// <param name="typeCode">TypeCode</param>
        /// <returns>対応デバイス一覧と推奨範囲</returns>
        public static (DeviceCode[] devices, Dictionary<DeviceCode, DeviceRange> ranges) GetCompleteDeviceMap(TypeCode typeCode)
        {
            return typeCode switch
            {
                // FXシリーズ
                _ when typeCode.IsFXSeries() => GetFXSeriesDeviceMap(typeCode),

                // Qシリーズ
                _ when typeCode.IsQSeries() => GetQSeriesDeviceMap(typeCode),

                // Rシリーズ
                _ when typeCode.IsRSeries() => GetRSeriesDeviceMap(typeCode),

                // Lシリーズ
                _ when typeCode.IsLSeries() => GetLSeriesDeviceMap(typeCode),

                // QSシリーズ
                _ when typeCode.IsQSSeries() => (new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.D },
                                                new Dictionary<DeviceCode, DeviceRange>()),

                // デフォルト
                _ => (new[] { DeviceCode.M, DeviceCode.X, DeviceCode.Y, DeviceCode.D },
                     new Dictionary<DeviceCode, DeviceRange>())
            };
        }

        private static (DeviceCode[], Dictionary<DeviceCode, DeviceRange>) GetFXSeriesDeviceMap(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.FX5U or TypeCode.FX5UC or TypeCode.FX5UJ =>
                    (FXSeries.FX5_CompleteDevices, FXSeries.FX5_RecommendedRanges),
                _ =>
                    (FXSeries.FX3_CompleteDevices, FXSeries.FX3_RecommendedRanges)
            };
        }

        private static (DeviceCode[], Dictionary<DeviceCode, DeviceRange>) GetQSeriesDeviceMap(TypeCode typeCode)
        {
            var isHighPerformance = typeCode.ToString().Contains("H") ||
                                   typeCode.ToString().Contains("25") ||
                                   typeCode.ToString().Contains("100");

            return isHighPerformance ?
                (QSeries.HighPerformanceCPU_CompleteDevices, QSeries.BasicCPU_RecommendedRanges) :
                (QSeries.BasicCPU_CompleteDevices, QSeries.BasicCPU_RecommendedRanges);
        }

        private static (DeviceCode[], Dictionary<DeviceCode, DeviceRange>) GetRSeriesDeviceMap(TypeCode typeCode)
        {
            return (RSeries.BasicCPU_CompleteDevices, RSeries.BasicCPU_RecommendedRanges);
        }

        private static (DeviceCode[], Dictionary<DeviceCode, DeviceRange>) GetLSeriesDeviceMap(TypeCode typeCode)
        {
            return (LSeries.CompleteDevices, LSeries.RecommendedRanges);
        }

        /// <summary>
        /// デバイス使用統計情報
        /// </summary>
        public static class DeviceUsageStatistics
        {
            /// <summary>使用頻度別優先度マップ</summary>
            public static readonly Dictionary<DeviceCode, int> UsageFrequencyPriority = new()
            {
                // 最高頻度（優先度5）
                [DeviceCode.M] = 5,  // 内部リレー
                [DeviceCode.D] = 5,  // データレジスタ
                [DeviceCode.X] = 5,  // 入力リレー
                [DeviceCode.Y] = 5,  // 出力リレー

                // 高頻度（優先度4）
                [DeviceCode.TS] = 4, // タイマー接点
                [DeviceCode.TC] = 4, // タイマーコイル
                [DeviceCode.TN] = 4, // タイマー現在値
                [DeviceCode.CS] = 4, // カウンタ接点
                [DeviceCode.CC] = 4, // カウンタコイル
                [DeviceCode.CN] = 4, // カウンタ現在値

                // 中頻度（優先度3）
                [DeviceCode.L] = 3,  // ラッチリレー
                [DeviceCode.B] = 3,  // リンクリレー
                [DeviceCode.W] = 3,  // リンクレジスタ
                [DeviceCode.R] = 3,  // ファイルレジスタ

                // 低頻度（優先度2）
                [DeviceCode.F] = 2,  // アナンシエータ
                [DeviceCode.V] = 2,  // エッジリレー
                [DeviceCode.Z] = 2,  // インデックスレジスタ
                [DeviceCode.ZR] = 2, // 拡張ファイルレジスタ

                // 特殊用途（優先度1）
                [DeviceCode.SM] = 1, // 特殊リレー
                [DeviceCode.SD] = 1, // 特殊レジスタ
                [DeviceCode.SB] = 1, // リンク特殊リレー
                [DeviceCode.SW] = 1, // リンク特殊レジスタ
            };
        }
    }
}