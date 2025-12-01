namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// デバイス範囲設定
/// ReadRandom(0x0403)で取得するデバイスのリストを保持
/// </summary>
public class TargetDeviceConfig
{
    /// <summary>
    /// 取得対象デバイスのリスト
    /// ReadRandom(0x0403)コマンドで一括取得するデバイス指定
    /// </summary>
    public List<DeviceEntry> Devices { get; set; } = new();

    /// <summary>
    /// フレームタイプ（"3E" or "4E"、デフォルト: "4E"）
    /// </summary>
    public string FrameType { get; set; } = "4E";

    /// <summary>
    /// タイムアウト値（デフォルト: 32）
    /// </summary>
    public ushort Timeout { get; set; } = 32;
}
