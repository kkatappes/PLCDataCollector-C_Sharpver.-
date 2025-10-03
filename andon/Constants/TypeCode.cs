using System;
using System.Collections.Generic;

namespace SlmpClient.Constants
{
    /// <summary>
    /// SLMP CPU タイプコード定義
    /// Python: const.TypeCode (61項目)
    /// </summary>
    public enum TypeCode : ushort
    {
        #region Qシリーズ (Q Series) - 29項目
        
        /// <summary>Q00JCPU</summary>
        Q00JCPU = 0x250,
        
        /// <summary>Q00CPU</summary>
        Q00CPU = 0x251,
        
        /// <summary>Q01CPU</summary>
        Q01CPU = 0x252,
        
        /// <summary>Q02CPU</summary>
        Q02CPU = 0x41,
        
        /// <summary>Q06HCPU</summary>
        Q06HCPU = 0x42,
        
        /// <summary>Q12HCPU</summary>
        Q12HCPU = 0x43,
        
        /// <summary>Q25HCPU</summary>
        Q25HCPU = 0x44,
        
        /// <summary>Q12PRHCPU</summary>
        Q12PRHCPU = 0x4B,
        
        /// <summary>Q25PRHCPU</summary>
        Q25PRHCPU = 0x4C,
        
        /// <summary>Q00UJCPU</summary>
        Q00UJCPU = 0x260,
        
        /// <summary>Q00UCPU</summary>
        Q00UCPU = 0x261,
        
        /// <summary>Q01UCPU</summary>
        Q01UCPU = 0x262,
        
        /// <summary>Q02UCPU</summary>
        Q02UCPU = 0x263,
        
        /// <summary>Q03UDCPU</summary>
        Q03UDCPU = 0x268,
        
        /// <summary>Q03UDVCPU</summary>
        Q03UDVCPU = 0x366,
        
        /// <summary>Q04UDHCPU</summary>
        Q04UDHCPU = 0x269,
        
        /// <summary>Q04UDVCPU</summary>
        Q04UDVCPU = 0x367,
        
        /// <summary>Q06UDHCPU</summary>
        Q06UDHCPU = 0x26A,
        
        /// <summary>Q06UDVCPU</summary>
        Q06UDVCPU = 0x368,
        
        /// <summary>Q10UDHCPU</summary>
        Q10UDHCPU = 0x266,
        
        /// <summary>Q13UDHCPU</summary>
        Q13UDHCPU = 0x26B,
        
        /// <summary>Q13UDVCPU</summary>
        Q13UDVCPU = 0x36A,
        
        /// <summary>Q20UDHCPU</summary>
        Q20UDHCPU = 0x267,
        
        /// <summary>Q26UDHCPU</summary>
        Q26UDHCPU = 0x26C,
        
        /// <summary>Q26UDVCPU</summary>
        Q26UDVCPU = 0x36C,
        
        /// <summary>Q50UDEHCPU</summary>
        Q50UDEHCPU = 0x26D,
        
        /// <summary>Q100UDEHCPU</summary>
        Q100UDEHCPU = 0x26E,
        
        #endregion

        #region FXシリーズ (FX Series) - 7項目

        /// <summary>FX5U-32MR/ES</summary>
        FX5U = 0x01F0,

        /// <summary>FX5UC-32MT/DS</summary>
        FX5UC = 0x01F1,

        /// <summary>FX5UJ-24MT/ES</summary>
        FX5UJ = 0x01F2,

        /// <summary>FX3U-32MR/ES</summary>
        FX3U = 0x01E0,

        /// <summary>FX3UC-32MT/D</summary>
        FX3UC = 0x01E1,

        /// <summary>FX3G-24MR/ES</summary>
        FX3G = 0x01D0,

        /// <summary>FX3GC-32MT/D</summary>
        FX3GC = 0x01D1,

        #endregion

        #region QSシリーズ (QS Series) - 1項目

        /// <summary>QS001CPU</summary>
        QS001CPU = 0x230,

        #endregion
        
        #region Lシリーズ (L Series) - 7項目
        
        /// <summary>L02SCPU</summary>
        L02SCPU = 0x543,
        
        /// <summary>L02CPU</summary>
        L02CPU = 0x541,
        
        /// <summary>L06CPU</summary>
        L06CPU = 0x544,
        
        /// <summary>L26CPU</summary>
        L26CPU = 0x545,
        
        /// <summary>L26CPU_BT (Bluetooth対応)</summary>
        L26CPU_BT = 0x542,
        
        /// <summary>L04HCPU</summary>
        L04HCPU = 0x48C0,
        
        /// <summary>L08HCPU</summary>
        L08HCPU = 0x48C1,
        
        /// <summary>L16HCPU</summary>
        L16HCPU = 0x48C2,
        
        /// <summary>LJ72GF15-T2</summary>
        LJ72GF15_T2 = 0x0641,
        
        #endregion
        
        #region Rシリーズ (R Series) - 24項目
        
        /// <summary>R00CPU</summary>
        R00CPU = 0x48A0,
        
        /// <summary>R01CPU</summary>
        R01CPU = 0x48A1,
        
        /// <summary>R02CPU</summary>
        R02CPU = 0x48A2,
        
        /// <summary>R04CPU</summary>
        R04CPU = 0x4800,
        
        /// <summary>R04ENCPU (Ethernet対応)</summary>
        R04ENCPU = 0x4805,
        
        /// <summary>R08CPU</summary>
        R08CPU = 0x4801,
        
        /// <summary>R08ENCPU (Ethernet対応)</summary>
        R08ENCPU = 0x4806,
        
        /// <summary>R08PCPU (プロセス制御対応)</summary>
        R08PCPU = 0x4841,
        
        /// <summary>R08PSFCPU (プロセス・SFC対応)</summary>
        R08PSFCPU = 0x4851,
        
        /// <summary>R08SFCPU (SFC対応)</summary>
        R08SFCPU = 0x4891,
        
        /// <summary>R16CPU</summary>
        R16CPU = 0x4802,
        
        /// <summary>R16ENCPU (Ethernet対応)</summary>
        R16ENCPU = 0x4807,
        
        /// <summary>R16PCPU (プロセス制御対応)</summary>
        R16PCPU = 0x4842,
        
        /// <summary>R16PSFCPU (プロセス・SFC対応)</summary>
        R16PSFCPU = 0x4852,
        
        /// <summary>R16SFCPU (SFC対応)</summary>
        R16SFCPU = 0x4892,
        
        /// <summary>R32CPU</summary>
        R32CPU = 0x4803,
        
        /// <summary>R32ENCPU (Ethernet対応)</summary>
        R32ENCPU = 0x4808,
        
        /// <summary>R32PCPU (プロセス制御対応)</summary>
        R32PCPU = 0x4843,
        
        /// <summary>R32PSFCPU (プロセス・SFC対応)</summary>
        R32PSFCPU = 0x4853,
        
        /// <summary>R32SFCPU (SFC対応)</summary>
        R32SFCPU = 0x4893,
        
        /// <summary>R120CPU</summary>
        R120CPU = 0x4804,
        
        /// <summary>R120ENCPU (Ethernet対応)</summary>
        R120ENCPU = 0x4809,
        
        /// <summary>R120PCPU (プロセス制御対応)</summary>
        R120PCPU = 0x4844,
        
        /// <summary>R120PSFCPU (プロセス・SFC対応)</summary>
        R120PSFCPU = 0x4854,
        
        /// <summary>R120SFCPU (SFC対応)</summary>
        R120SFCPU = 0x4894,
        
        /// <summary>R12CCPU_V</summary>
        R12CCPU_V = 0x4820,
        
        /// <summary>MI5122-VW</summary>
        MI5122_VW = 0x4E01,
        
        /// <summary>RJ72GF15-T2</summary>
        RJ72GF15_T2 = 0x4860,
        
        /// <summary>RJ72GF15-T2-D1</summary>
        RJ72GF15_T2_D1 = 0x4861,
        
        /// <summary>RJ72GF15-T2-D2</summary>
        RJ72GF15_T2_D2 = 0x4862,
        
        /// <summary>NZ2GF-ETB</summary>
        NZ2GF_ETB = 0x0642,
        
        #endregion
    }
    
    /// <summary>
    /// TypeCode拡張メソッド
    /// CPUタイプに関する追加情報を提供
    /// </summary>
    public static class TypeCodeExtensions
    {
        /// <summary>
        /// FXシリーズ CPUタイプコード
        /// </summary>
        private static readonly HashSet<TypeCode> FXSeriesTypes = new()
        {
            TypeCode.FX5U, TypeCode.FX5UC, TypeCode.FX5UJ, TypeCode.FX3U,
            TypeCode.FX3UC, TypeCode.FX3G, TypeCode.FX3GC
        };

        /// <summary>
        /// Qシリーズ CPUタイプコード
        /// </summary>
        private static readonly HashSet<TypeCode> QSeriesTypes = new()
        {
            TypeCode.Q00JCPU, TypeCode.Q00CPU, TypeCode.Q01CPU, TypeCode.Q02CPU,
            TypeCode.Q06HCPU, TypeCode.Q12HCPU, TypeCode.Q25HCPU, TypeCode.Q12PRHCPU,
            TypeCode.Q25PRHCPU, TypeCode.Q00UJCPU, TypeCode.Q00UCPU, TypeCode.Q01UCPU,
            TypeCode.Q02UCPU, TypeCode.Q03UDCPU, TypeCode.Q03UDVCPU, TypeCode.Q04UDHCPU,
            TypeCode.Q04UDVCPU, TypeCode.Q06UDHCPU, TypeCode.Q06UDVCPU, TypeCode.Q10UDHCPU,
            TypeCode.Q13UDHCPU, TypeCode.Q13UDVCPU, TypeCode.Q20UDHCPU, TypeCode.Q26UDHCPU,
            TypeCode.Q26UDVCPU, TypeCode.Q50UDEHCPU, TypeCode.Q100UDEHCPU
        };
        
        /// <summary>
        /// Lシリーズ CPUタイプコード
        /// </summary>
        private static readonly HashSet<TypeCode> LSeriesTypes = new()
        {
            TypeCode.L02SCPU, TypeCode.L02CPU, TypeCode.L06CPU, TypeCode.L26CPU,
            TypeCode.L26CPU_BT, TypeCode.L04HCPU, TypeCode.L08HCPU, TypeCode.L16HCPU,
            TypeCode.LJ72GF15_T2
        };
        
        /// <summary>
        /// Rシリーズ CPUタイプコード
        /// </summary>
        private static readonly HashSet<TypeCode> RSeriesTypes = new()
        {
            TypeCode.R00CPU, TypeCode.R01CPU, TypeCode.R02CPU, TypeCode.R04CPU,
            TypeCode.R04ENCPU, TypeCode.R08CPU, TypeCode.R08ENCPU, TypeCode.R08PCPU,
            TypeCode.R08PSFCPU, TypeCode.R08SFCPU, TypeCode.R16CPU, TypeCode.R16ENCPU,
            TypeCode.R16PCPU, TypeCode.R16PSFCPU, TypeCode.R16SFCPU, TypeCode.R32CPU,
            TypeCode.R32ENCPU, TypeCode.R32PCPU, TypeCode.R32PSFCPU, TypeCode.R32SFCPU,
            TypeCode.R120CPU, TypeCode.R120ENCPU, TypeCode.R120PCPU, TypeCode.R120PSFCPU,
            TypeCode.R120SFCPU, TypeCode.R12CCPU_V, TypeCode.MI5122_VW, TypeCode.RJ72GF15_T2,
            TypeCode.RJ72GF15_T2_D1, TypeCode.RJ72GF15_T2_D2, TypeCode.NZ2GF_ETB
        };
        
        /// <summary>
        /// Ethernet対応CPUタイプコード
        /// </summary>
        private static readonly HashSet<TypeCode> EthernetCapableTypes = new()
        {
            TypeCode.R04ENCPU, TypeCode.R08ENCPU, TypeCode.R16ENCPU, TypeCode.R32ENCPU,
            TypeCode.R120ENCPU, TypeCode.RJ72GF15_T2, TypeCode.RJ72GF15_T2_D1,
            TypeCode.RJ72GF15_T2_D2, TypeCode.LJ72GF15_T2, TypeCode.NZ2GF_ETB
        };
        
        /// <summary>
        /// プロセス制御対応CPUタイプコード
        /// </summary>
        private static readonly HashSet<TypeCode> ProcessControlCapableTypes = new()
        {
            TypeCode.R08PCPU, TypeCode.R08PSFCPU, TypeCode.R16PCPU, TypeCode.R16PSFCPU,
            TypeCode.R32PCPU, TypeCode.R32PSFCPU, TypeCode.R120PCPU, TypeCode.R120PSFCPU
        };
        
        /// <summary>
        /// CPUがFXシリーズかどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>FXシリーズの場合はtrue</returns>
        public static bool IsFXSeries(this TypeCode typeCode)
        {
            return FXSeriesTypes.Contains(typeCode);
        }

        /// <summary>
        /// CPUがQシリーズかどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>Qシリーズの場合はtrue</returns>
        public static bool IsQSeries(this TypeCode typeCode)
        {
            return QSeriesTypes.Contains(typeCode);
        }
        
        /// <summary>
        /// CPUがLシリーズかどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>Lシリーズの場合はtrue</returns>
        public static bool IsLSeries(this TypeCode typeCode)
        {
            return LSeriesTypes.Contains(typeCode);
        }
        
        /// <summary>
        /// CPUがRシリーズかどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>Rシリーズの場合はtrue</returns>
        public static bool IsRSeries(this TypeCode typeCode)
        {
            return RSeriesTypes.Contains(typeCode);
        }
        
        /// <summary>
        /// CPUがQSシリーズかどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>QSシリーズの場合はtrue</returns>
        public static bool IsQSSeries(this TypeCode typeCode)
        {
            return typeCode == TypeCode.QS001CPU;
        }
        
        /// <summary>
        /// CPUがEthernet対応かどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>Ethernet対応の場合はtrue</returns>
        public static bool IsEthernetCapable(this TypeCode typeCode)
        {
            return EthernetCapableTypes.Contains(typeCode);
        }
        
        /// <summary>
        /// CPUがプロセス制御対応かどうかを判定
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>プロセス制御対応の場合はtrue</returns>
        public static bool IsProcessControlCapable(this TypeCode typeCode)
        {
            return ProcessControlCapableTypes.Contains(typeCode);
        }
        
        /// <summary>
        /// CPUシリーズ名を取得
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>CPUシリーズ名</returns>
        public static string GetSeriesName(this TypeCode typeCode)
        {
            return typeCode switch
            {
                _ when typeCode.IsFXSeries() => "FXシリーズ",
                _ when typeCode.IsQSeries() => "Qシリーズ",
                _ when typeCode.IsQSSeries() => "QSシリーズ",
                _ when typeCode.IsLSeries() => "Lシリーズ",
                _ when typeCode.IsRSeries() => "Rシリーズ",
                _ => "不明シリーズ"
            };
        }
        
        /// <summary>
        /// CPU機能説明を取得
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>CPU機能説明</returns>
        public static string GetFeatureDescription(this TypeCode typeCode)
        {
            var features = new List<string>();
            
            if (typeCode.IsEthernetCapable())
                features.Add("Ethernet対応");
            
            if (typeCode.IsProcessControlCapable())
                features.Add("プロセス制御対応");
            
            if (typeCode.ToString().Contains("SF"))
                features.Add("SFC対応");
            
            if (typeCode.ToString().Contains("BT"))
                features.Add("Bluetooth対応");
            
            return features.Count > 0 ? string.Join(", ", features) : "標準機能";
        }
        
        /// <summary>
        /// CPUの推奨最大同時接続数を取得
        /// </summary>
        /// <param name="typeCode">CPUタイプコード</param>
        /// <returns>推奨最大同時接続数</returns>
        public static int GetMaxConcurrentConnections(this TypeCode typeCode)
        {
            return typeCode switch
            {
                // 高性能CPU
                _ when typeCode.ToString().Contains("120") => 32,
                _ when typeCode.ToString().Contains("32") => 16,
                _ when typeCode.ToString().Contains("16") => 8,
                // 中性能CPU
                _ when typeCode.ToString().Contains("08") => 4,
                _ when typeCode.ToString().Contains("06") => 4,
                _ when typeCode.ToString().Contains("04") => 4,
                // 低性能CPU
                _ => 2
            };
        }
    }
}