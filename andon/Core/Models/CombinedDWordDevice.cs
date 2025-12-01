namespace Andon.Core.Models;

/// <summary>
/// DWord結合済みデバイス情報
/// 2つの16bitデバイスを結合して32bitデータとしたもの
/// </summary>
public class CombinedDWordDevice
{
    /// <summary>
    /// 結合デバイス名（例：D100_32bit）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 結合後の32bit値
    /// </summary>
    public uint CombinedValue { get; set; }

    /// <summary>
    /// 下位ワードアドレス
    /// </summary>
    public int LowWordAddress { get; set; }

    /// <summary>
    /// 上位ワードアドレス
    /// </summary>
    public int HighWordAddress { get; set; }

    /// <summary>
    /// 下位ワード値
    /// </summary>
    public ushort LowWordValue { get; set; }

    /// <summary>
    /// 上位ワード値
    /// </summary>
    public ushort HighWordValue { get; set; }

    /// <summary>
    /// 結合処理時刻
    /// </summary>
    public DateTime CombinedAt { get; set; }

    /// <summary>
    /// デバイス種別（D、Wなど）
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;
}