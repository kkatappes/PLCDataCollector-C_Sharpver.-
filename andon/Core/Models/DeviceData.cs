using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// デバイスデータを表現するクラス
/// Phase5実装（2025-11-21）
/// Random READレスポンスのパース結果を格納
/// </summary>
public class DeviceData
{
    /// <summary>
    /// デバイス名（"M000", "D000", "D002", "W0x11AA"等）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// デバイスコード（M, D, W等）
    /// </summary>
    public DeviceCode Code { get; set; }

    /// <summary>
    /// デバイス番号（アドレス）
    /// </summary>
    public int Address { get; set; }

    /// <summary>
    /// デバイス値（16bit: ワードデバイス、32bit: ダブルワードデバイス）
    /// </summary>
    public uint Value { get; set; }

    /// <summary>
    /// ダブルワードデバイスかどうか
    /// </summary>
    public bool IsDWord { get; set; }

    /// <summary>
    /// 16進アドレス表記かどうか
    /// </summary>
    public bool IsHexAddress { get; set; }

    /// <summary>
    /// デバイス型（"Bit", "Word", "DWord"）
    /// Phase7データ出力で使用（unit値："bit", "word", "dword"への変換に利用）
    /// </summary>
    public string Type { get; set; } = "Word";  // デフォルトはWord

    /// <summary>
    /// DeviceSpecificationからワードデバイスデータを生成
    /// </summary>
    /// <param name="device">デバイス指定</param>
    /// <param name="value">ワード値（16bit）</param>
    /// <returns>DeviceDataインスタンス</returns>
    public static DeviceData FromDeviceSpecification(DeviceSpecification device, ushort value)
    {
        return new DeviceData
        {
            DeviceName = device.ToString(),
            Code = device.Code,
            Address = device.DeviceNumber,
            Value = value,
            IsDWord = false,
            IsHexAddress = device.IsHexAddress,
            Type = device.Code.IsBitDevice() ? "Bit" : "Word"
        };
    }

    /// <summary>
    /// ダブルワードデバイスの生成（2ワード分結合）
    /// </summary>
    /// <param name="device">デバイス指定</param>
    /// <param name="lowerWord">下位ワード</param>
    /// <param name="upperWord">上位ワード</param>
    /// <returns>DeviceDataインスタンス（32bit値）</returns>
    public static DeviceData FromDWordDevice(DeviceSpecification device, ushort lowerWord, ushort upperWord)
    {
        uint dwordValue = ((uint)upperWord << 16) | lowerWord;
        return new DeviceData
        {
            DeviceName = device.ToString(),
            Code = device.Code,
            Address = device.DeviceNumber,
            Value = dwordValue,
            IsDWord = true,
            IsHexAddress = device.IsHexAddress,
            Type = "DWord"
        };
    }
}
