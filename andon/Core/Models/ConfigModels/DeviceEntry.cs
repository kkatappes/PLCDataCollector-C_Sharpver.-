using Andon.Core.Constants;

namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// 設定ファイルから読み込むデバイスエントリ
/// appsettings.jsonのDevicesリスト要素に対応
/// </summary>
public class DeviceEntry
{
    /// <summary>
    /// デバイスタイプ（"D", "M", "W"等）
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// デバイス番号（10進表記）
    /// </summary>
    public int DeviceNumber { get; set; }

    /// <summary>
    /// デバイス番号が16進表記かどうか（省略可）
    /// </summary>
    public bool IsHexAddress { get; set; } = false;

    /// <summary>
    /// デバイスの説明（省略可）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// DeviceSpecificationに変換
    /// </summary>
    /// <returns>DeviceSpecification</returns>
    /// <exception cref="ArgumentException">不正なDeviceType</exception>
    public DeviceSpecification ToDeviceSpecification()
    {
        // DeviceType文字列をDeviceCodeに変換
        if (!Enum.TryParse<DeviceCode>(DeviceType, true, out var deviceCode))
        {
            throw new ArgumentException(
                $"不正なDeviceType: {DeviceType}（有効な値: D, M, W, X, Y等）",
                nameof(DeviceType));
        }

        return new DeviceSpecification(deviceCode, DeviceNumber, IsHexAddress);
    }
}
