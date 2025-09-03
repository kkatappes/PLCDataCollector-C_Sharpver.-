using System;
using System.Collections.Generic;

namespace SlmpClient.Constants
{
    /// <summary>
    /// SLMP デバイスコード定義
    /// Python: const.DeviceCode (39項目)
    /// </summary>
    public enum DeviceCode : byte
    {
        #region 基本デバイス (Basic Devices)
        
        /// <summary>特殊リレー (Special Relay)</summary>
        SM = 0x91,
        
        /// <summary>特殊レジスタ (Special Register)</summary>
        SD = 0xA9,
        
        /// <summary>入力リレー (Input Relay)</summary>
        X = 0x9C,
        
        /// <summary>出力リレー (Output Relay)</summary>
        Y = 0x9D,
        
        /// <summary>内部リレー (Internal Relay)</summary>
        M = 0x90,
        
        /// <summary>ラッチリレー (Latch Relay)</summary>
        L = 0x92,
        
        /// <summary>アナンシエータ (Annunciator)</summary>
        F = 0x93,
        
        /// <summary>エッジリレー (Edge Relay)</summary>
        V = 0x94,
        
        /// <summary>リンクリレー (Link Relay)</summary>
        B = 0xA0,
        
        /// <summary>データレジスタ (Data Register)</summary>
        D = 0xA8,
        
        /// <summary>リンクレジスタ (Link Register)</summary>
        W = 0xB4,
        
        #endregion
        
        #region タイマーデバイス (Timer Devices)
        
        /// <summary>タイマー接点 (Timer Contact)</summary>
        TS = 0xC1,
        
        /// <summary>タイマーコイル (Timer Coil)</summary>
        TC = 0xC0,
        
        /// <summary>タイマー現在値 (Timer Present Value)</summary>
        TN = 0xC2,
        
        /// <summary>長時間タイマー接点 (Long Timer Contact)</summary>
        LTS = 0x51,
        
        /// <summary>長時間タイマーコイル (Long Timer Coil)</summary>
        LTC = 0x50,
        
        /// <summary>長時間タイマー現在値 (Long Timer Present Value)</summary>
        LTN = 0x52,
        
        #endregion
        
        #region カウンタデバイス (Counter Devices)
        
        /// <summary>積算タイマー接点 (Accumulated Timer Contact)</summary>
        SS = 0xC7,
        
        /// <summary>積算タイマーコイル (Accumulated Timer Coil)</summary>
        SC = 0xC6,
        
        /// <summary>積算タイマー現在値 (Accumulated Timer Present Value)</summary>
        SN = 0xC8,
        
        /// <summary>長時間積算タイマー接点 (Long Accumulated Timer Contact)</summary>
        LSTS = 0x59,
        
        /// <summary>長時間積算タイマーコイル (Long Accumulated Timer Coil)</summary>
        LSTC = 0x58,
        
        /// <summary>長時間積算タイマー現在値 (Long Accumulated Timer Present Value)</summary>
        LSTN = 0x5A,
        
        /// <summary>カウンタ接点 (Counter Contact)</summary>
        CS = 0xC4,
        
        /// <summary>カウンタコイル (Counter Coil)</summary>
        CC = 0xC3,
        
        /// <summary>カウンタ現在値 (Counter Present Value)</summary>
        CN = 0xC5,
        
        #endregion
        
        #region 特殊デバイス (Special Devices)
        
        /// <summary>リンク特殊リレー (Link Special Relay)</summary>
        SB = 0xA1,
        
        /// <summary>リンク特殊レジスタ (Link Special Register)</summary>
        SW = 0xB5,
        
        /// <summary>直接入力 (Direct Input)</summary>
        DX = 0xA2,
        
        /// <summary>直接出力 (Direct Output)</summary>
        DY = 0xA3,
        
        /// <summary>インデックスレジスタ (Index Register)</summary>
        Z = 0xCC,
        
        /// <summary>長インデックスレジスタ (Long Index Register)</summary>
        LZ = 0x62,
        
        /// <summary>ファイルレジスタ (File Register)</summary>
        R = 0xAF,
        
        /// <summary>拡張ファイルレジスタ (Extended File Register)</summary>
        ZR = 0xB0,
        
        /// <summary>拡張データレジスタ (Extended Data Register)</summary>
        RD = 0x2C,
        
        /// <summary>長カウンタ接点 (Long Counter Contact)</summary>
        LCS = 0x55,
        
        /// <summary>長カウンタコイル (Long Counter Coil)</summary>
        LCC = 0x54,
        
        /// <summary>長カウンタ現在値 (Long Counter Present Value)</summary>
        LCN = 0x56,
        
        #endregion
    }
    
    /// <summary>
    /// DeviceCode拡張メソッド
    /// Python: const.py の D_ADDR_16, D_ADDR_4BYTE, D_STRANGE_NAME 相当
    /// </summary>
    public static class DeviceCodeExtensions
    {
        /// <summary>
        /// 16進アドレス表現を使用するデバイス
        /// Python: D_ADDR_16
        /// </summary>
        private static readonly HashSet<DeviceCode> HexAddressDevices = new()
        {
            DeviceCode.X,
            DeviceCode.Y,
            DeviceCode.B,
            DeviceCode.W,
            DeviceCode.SB,
            DeviceCode.SW,
            DeviceCode.DX,
            DeviceCode.DY,
            DeviceCode.ZR
        };
        
        /// <summary>
        /// 4バイトアドレスでしかアクセスできないデバイス
        /// Python: D_ADDR_4BYTE
        /// </summary>
        private static readonly HashSet<DeviceCode> FourByteAddressDevices = new()
        {
            DeviceCode.LTS,
            DeviceCode.LTC,
            DeviceCode.LTN,
            DeviceCode.LSTS,
            DeviceCode.LSTC,
            DeviceCode.LSTN,
            DeviceCode.LCS,
            DeviceCode.LCC,
            DeviceCode.LCN,
            DeviceCode.LZ,
            DeviceCode.RD
        };
        
        /// <summary>
        /// 4バイトアドレスと2バイトアドレスで名前の異なるデバイス
        /// Python: D_STRANGE_NAME
        /// </summary>
        private static readonly HashSet<DeviceCode> StrangeNameDevices = new()
        {
            DeviceCode.SS,
            DeviceCode.SC,
            DeviceCode.SN
        };
        
        /// <summary>
        /// デバイスが16進アドレス表現を使用するかどうかを判定
        /// Python: device_code in const.D_ADDR_16
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>16進アドレス表現を使用する場合はtrue</returns>
        public static bool IsHexAddress(this DeviceCode deviceCode)
        {
            return HexAddressDevices.Contains(deviceCode);
        }
        
        /// <summary>
        /// デバイスが4バイトアドレスでしかアクセスできないかどうかを判定
        /// Python: device_code in const.D_ADDR_4BYTE
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>4バイトアドレス必須の場合はtrue</returns>
        public static bool Is4ByteAddress(this DeviceCode deviceCode)
        {
            return FourByteAddressDevices.Contains(deviceCode);
        }
        
        /// <summary>
        /// デバイスが4バイトアドレスと2バイトアドレスで名前が異なるかどうかを判定
        /// Python: device_code in const.D_STRANGE_NAME
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>名前が異なる場合はtrue</returns>
        public static bool HasStrangeName(this DeviceCode deviceCode)
        {
            return StrangeNameDevices.Contains(deviceCode);
        }
        
        /// <summary>
        /// デバイスコードから表示名を取得
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>デバイスの表示名</returns>
        public static string GetDisplayName(this DeviceCode deviceCode)
        {
            return deviceCode switch
            {
                DeviceCode.SM => "特殊リレー",
                DeviceCode.SD => "特殊レジスタ",
                DeviceCode.X => "入力リレー",
                DeviceCode.Y => "出力リレー",
                DeviceCode.M => "内部リレー",
                DeviceCode.L => "ラッチリレー",
                DeviceCode.F => "アナンシエータ",
                DeviceCode.V => "エッジリレー",
                DeviceCode.B => "リンクリレー",
                DeviceCode.D => "データレジスタ",
                DeviceCode.W => "リンクレジスタ",
                DeviceCode.TS => "タイマー接点",
                DeviceCode.TC => "タイマーコイル",
                DeviceCode.TN => "タイマー現在値",
                DeviceCode.LTS => "長時間タイマー接点",
                DeviceCode.LTC => "長時間タイマーコイル",
                DeviceCode.LTN => "長時間タイマー現在値",
                DeviceCode.SS => "積算タイマー接点",
                DeviceCode.SC => "積算タイマーコイル",
                DeviceCode.SN => "積算タイマー現在値",
                DeviceCode.LSTS => "長時間積算タイマー接点",
                DeviceCode.LSTC => "長時間積算タイマーコイル",
                DeviceCode.LSTN => "長時間積算タイマー現在値",
                DeviceCode.CS => "カウンタ接点",
                DeviceCode.CC => "カウンタコイル",
                DeviceCode.CN => "カウンタ現在値",
                DeviceCode.SB => "リンク特殊リレー",
                DeviceCode.SW => "リンク特殊レジスタ",
                DeviceCode.DX => "直接入力",
                DeviceCode.DY => "直接出力",
                DeviceCode.Z => "インデックスレジスタ",
                DeviceCode.LZ => "長インデックスレジスタ",
                DeviceCode.R => "ファイルレジスタ",
                DeviceCode.ZR => "拡張ファイルレジスタ",
                DeviceCode.RD => "拡張データレジスタ",
                DeviceCode.LCS => "長カウンタ接点",
                DeviceCode.LCC => "長カウンタコイル",
                DeviceCode.LCN => "長カウンタ現在値",
                _ => deviceCode.ToString()
            };
        }
        
        /// <summary>
        /// デバイスがビットデバイスかどうかを判定
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>ビットデバイスの場合はtrue</returns>
        public static bool IsBitDevice(this DeviceCode deviceCode)
        {
            return deviceCode switch
            {
                DeviceCode.SM or DeviceCode.X or DeviceCode.Y or DeviceCode.M or 
                DeviceCode.L or DeviceCode.F or DeviceCode.V or DeviceCode.B or
                DeviceCode.TS or DeviceCode.TC or DeviceCode.LTS or DeviceCode.LTC or
                DeviceCode.SS or DeviceCode.SC or DeviceCode.LSTS or DeviceCode.LSTC or
                DeviceCode.CS or DeviceCode.CC or DeviceCode.SB or DeviceCode.DX or
                DeviceCode.DY or DeviceCode.LCS or DeviceCode.LCC => true,
                _ => false
            };
        }
        
        /// <summary>
        /// デバイスがワードデバイスかどうかを判定
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <returns>ワードデバイスの場合はtrue</returns>
        public static bool IsWordDevice(this DeviceCode deviceCode)
        {
            return !deviceCode.IsBitDevice();
        }
    }
}