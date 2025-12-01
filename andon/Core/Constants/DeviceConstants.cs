namespace Andon.Core.Constants;

/// <summary>
/// SLMPデバイスコード定義（SLMP仕様書準拠）
/// ReadRandom(0x0403)コマンドで使用
/// </summary>
public enum DeviceCode : byte
{
    // ビットデバイス（16点=1ワード）
    SM = 0x91,   // 特殊リレー
    X = 0x9C,    // 入力
    Y = 0x9D,    // 出力
    M = 0x90,    // 内部リレー
    L = 0x92,    // ラッチリレー
    F = 0x93,    // アナンシエータ
    B = 0xA0,    // リンクリレー

    // ワードデバイス
    SD = 0xA9,   // 特殊レジスタ
    D = 0xA8,    // データレジスタ
    W = 0xB4,    // リンクレジスタ
    R = 0xAF,    // ファイルレジスタ
    ZR = 0xB0,   // ファイルレジスタ（拡張）

    // タイマー（ReadRandomでは制約あり）
    TN = 0xC2,   // タイマ現在値
    TS = 0xC1,   // タイマ接点（ReadRandom非対応）
    TC = 0xC0,   // タイマコイル（ReadRandom非対応）

    // カウンタ
    CN = 0xC5,   // カウンタ現在値
    CS = 0xC4,   // カウンタ接点（ReadRandom非対応）
    CC = 0xC3,   // カウンタコイル（ReadRandom非対応）
}

/// <summary>
/// DeviceCode拡張メソッド
/// </summary>
public static class DeviceCodeExtensions
{
    /// <summary>
    /// 16進アドレス表記のデバイスセット
    /// </summary>
    private static readonly HashSet<DeviceCode> HexAddressDevices = new()
    {
        DeviceCode.X,
        DeviceCode.Y,
        DeviceCode.B,
        DeviceCode.W,
        DeviceCode.ZR
    };

    /// <summary>
    /// ビット型デバイスセット
    /// </summary>
    private static readonly HashSet<DeviceCode> BitDevices = new()
    {
        DeviceCode.SM,
        DeviceCode.X,
        DeviceCode.Y,
        DeviceCode.M,
        DeviceCode.L,
        DeviceCode.F,
        DeviceCode.B,
        DeviceCode.TS,
        DeviceCode.TC,
        DeviceCode.CS,
        DeviceCode.CC
    };

    /// <summary>
    /// ReadRandomコマンドで指定不可のデバイスセット（SLMP仕様書 page_64.png準拠）
    /// </summary>
    private static readonly HashSet<DeviceCode> ReadRandomRestrictedDevices = new()
    {
        DeviceCode.TS,  // タイマ接点
        DeviceCode.TC,  // タイマコイル
        DeviceCode.CS,  // カウンタ接点
        DeviceCode.CC,  // カウンタコイル
    };

    /// <summary>
    /// デバイスコードが16進アドレス表記かを判定
    /// </summary>
    /// <param name="code">デバイスコード</param>
    /// <returns>16進アドレス表記の場合true</returns>
    public static bool IsHexAddress(this DeviceCode code)
        => HexAddressDevices.Contains(code);

    /// <summary>
    /// デバイスコードがビット型かを判定
    /// </summary>
    /// <param name="code">デバイスコード</param>
    /// <returns>ビット型デバイスの場合true</returns>
    public static bool IsBitDevice(this DeviceCode code)
        => BitDevices.Contains(code);

    /// <summary>
    /// ReadRandomコマンドで指定可能かを判定
    /// </summary>
    /// <param name="code">デバイスコード</param>
    /// <returns>ReadRandom指定可能な場合true</returns>
    public static bool IsReadRandomSupported(this DeviceCode code)
        => !ReadRandomRestrictedDevices.Contains(code);
}


/// <summary>
/// デバイスタイプ文字列からデバイスコード・属性情報への変換マップ（Phase1）
/// 24種類全てのSLMPデバイスタイプに対応
/// </summary>
public static class DeviceCodeMap
{
    /// <summary>
    /// デバイス情報（コード、16進フラグ、ビットフラグ）
    /// </summary>
    private record DeviceInfo(byte Code, bool IsHex, bool IsBit);

    /// <summary>
    /// デバイスタイプ文字列からデバイス情報へのマッピング
    /// </summary>
    private static readonly Dictionary<string, DeviceInfo> _deviceMap = new()
    {
        // ビットデバイス（10進） - 11種類
        { "SM", new DeviceInfo(0x91, false, true) },  // 特殊リレー
        { "M",  new DeviceInfo(0x90, false, true) },  // 内部リレー
        { "L",  new DeviceInfo(0x92, false, true) },  // ラッチリレー
        { "F",  new DeviceInfo(0x93, false, true) },  // アナンシエータ
        { "V",  new DeviceInfo(0x94, false, true) },  // エッジリレー
        { "TS", new DeviceInfo(0xC1, false, true) },  // タイマ接点
        { "TC", new DeviceInfo(0xC0, false, true) },  // タイマコイル
        { "STS", new DeviceInfo(0xC7, false, true) }, // 積算タイマ接点
        { "STC", new DeviceInfo(0xC6, false, true) }, // 積算タイマコイル
        { "CS", new DeviceInfo(0xC4, false, true) },  // カウンタ接点
        { "CC", new DeviceInfo(0xC3, false, true) },  // カウンタコイル

        // ビットデバイス（16進） - 6種類
        { "X",  new DeviceInfo(0x9C, true, true) },   // 入力
        { "Y",  new DeviceInfo(0x9D, true, true) },   // 出力
        { "B",  new DeviceInfo(0xA0, true, true) },   // リンクリレー
        { "SB", new DeviceInfo(0xA1, true, true) },   // リンク特殊リレー
        { "DX", new DeviceInfo(0xA2, true, true) },   // ダイレクト入力
        { "DY", new DeviceInfo(0xA3, true, true) },   // ダイレクト出力

        // ワードデバイス（10進） - 7種類
        { "SD", new DeviceInfo(0xA9, false, false) }, // 特殊レジスタ
        { "D",  new DeviceInfo(0xA8, false, false) }, // データレジスタ
        { "W",  new DeviceInfo(0xB4, false, false) }, // リンクレジスタ
        { "SW", new DeviceInfo(0xB5, false, false) }, // リンク特殊レジスタ
        { "TN", new DeviceInfo(0xC2, false, false) }, // タイマ現在値
        { "STN", new DeviceInfo(0xC8, false, false) },// 積算タイマ現在値
        { "CN", new DeviceInfo(0xC5, false, false) }, // カウンタ現在値
        { "Z",  new DeviceInfo(0xCC, false, false) }, // インデックスレジスタ
        { "R",  new DeviceInfo(0xAF, false, false) }, // ファイルレジスタ
        { "ZR", new DeviceInfo(0xB0, false, false) }  // ファイルレジスタ（拡張）
    };

    /// <summary>
    /// デバイスタイプ文字列からSLMPデバイスコードを取得
    /// </summary>
    /// <param name="deviceType">デバイスタイプ文字列（例: "M", "D", "X"）</param>
    /// <returns>SLMPデバイスコード（1バイト）</returns>
    /// <exception cref="ArgumentException">未対応のデバイスタイプの場合</exception>
    public static byte GetDeviceCode(string deviceType)
    {
        if (!_deviceMap.TryGetValue(deviceType.ToUpper(), out var info))
            throw new ArgumentException($"未対応のデバイスタイプ: {deviceType}");
        return info.Code;
    }

    /// <summary>
    /// デバイスタイプが16進アドレス表記かを判定
    /// </summary>
    /// <param name="deviceType">デバイスタイプ文字列</param>
    /// <returns>16進アドレス表記の場合true</returns>
    public static bool IsHexDevice(string deviceType)
        => _deviceMap.TryGetValue(deviceType.ToUpper(), out var info) && info.IsHex;

    /// <summary>
    /// デバイスタイプがビット型かを判定
    /// </summary>
    /// <param name="deviceType">デバイスタイプ文字列</param>
    /// <returns>ビット型デバイスの場合true</returns>
    public static bool IsBitDevice(string deviceType)
        => _deviceMap.TryGetValue(deviceType.ToUpper(), out var info) && info.IsBit;

    /// <summary>
    /// デバイスタイプが有効かを検証
    /// </summary>
    /// <param name="deviceType">デバイスタイプ文字列</param>
    /// <returns>有効なデバイスタイプの場合true</returns>
    public static bool IsValidDeviceType(string deviceType)
        => _deviceMap.ContainsKey(deviceType.ToUpper());
}
