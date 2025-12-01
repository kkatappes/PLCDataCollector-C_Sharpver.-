using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// デバイス指定情報（ReadRandom用）
/// SLMP仕様書準拠: デバイス番号3バイト(LE) + デバイスコード1バイト
/// </summary>
public class DeviceSpecification
{
    /// <summary>
    /// デバイスコード
    /// </summary>
    public DeviceCode Code { get; set; }

    /// <summary>
    /// デバイス番号（10進表記）
    /// </summary>
    /// <remarks>
    /// 16進デバイス（X, Y等）も内部では10進で格納
    /// 例: W0x11AA → 4522（10進）
    /// </remarks>
    public int DeviceNumber { get; set; }

    /// <summary>
    /// デバイス番号が16進表記かどうか
    /// </summary>
    public bool IsHexAddress { get; set; }

    // ========== Excel読み込み用プロパティ（Phase2追加） ==========

    /// <summary>
    /// 項目名（Excel "データ収集デバイス"シート A列）
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// デバイスタイプ文字列（Excel "データ収集デバイス"シート B列）
    /// 例: "D", "M", "W"
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// 桁数（Excel "データ収集デバイス"シート D列）
    /// </summary>
    public int Digits { get; set; }

    /// <summary>
    /// 単位（Excel "データ収集デバイス"シート E列）
    /// 例: "word", "bit", "dword"
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="code">デバイスコード</param>
    /// <param name="deviceNumber">デバイス番号（10進）</param>
    /// <param name="isHexAddress">16進表記フラグ（省略時はデバイスコードから自動判定）</param>
    public DeviceSpecification(DeviceCode code, int deviceNumber, bool? isHexAddress = null)
    {
        Code = code;
        DeviceNumber = deviceNumber;
        // isHexAddressがnullの場合、デバイスコードから自動判定
        IsHexAddress = isHexAddress ?? code.IsHexAddress();
        // DeviceTypeをDeviceCodeから設定
        DeviceType = code.ToString();
    }

    /// <summary>
    /// 16進デバイス番号文字列から生成（例: "11AA" → 0x11AA = 4522）
    /// </summary>
    /// <param name="code">デバイスコード</param>
    /// <param name="hexString">16進文字列（例: "11AA"）</param>
    /// <returns>DeviceSpecificationインスタンス</returns>
    public static DeviceSpecification FromHexString(DeviceCode code, string hexString)
    {
        if (string.IsNullOrWhiteSpace(hexString))
        {
            throw new ArgumentException("16進文字列が空です", nameof(hexString));
        }

        try
        {
            int deviceNumber = Convert.ToInt32(hexString, 16);
            return new DeviceSpecification(code, deviceNumber, isHexAddress: true);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException($"不正な16進文字列: {hexString}", nameof(hexString), ex);
        }
        catch (OverflowException ex)
        {
            throw new ArgumentException($"16進文字列が範囲外: {hexString}", nameof(hexString), ex);
        }
    }

    /// <summary>
    /// デバイス番号を3バイト配列に変換（リトルエンディアン）
    /// SLMP仕様書準拠: デバイス番号は3バイトのリトルエンディアン形式
    /// </summary>
    /// <returns>3バイト配列 [下位, 中位, 上位]</returns>
    public byte[] ToDeviceNumberBytes()
    {
        return new byte[]
        {
            (byte)(DeviceNumber & 0xFF),           // 下位バイト
            (byte)((DeviceNumber >> 8) & 0xFF),    // 中位バイト
            (byte)((DeviceNumber >> 16) & 0xFF)    // 上位バイト
        };
    }

    /// <summary>
    /// 4バイトデバイス指定配列に変換（ReadRandom用）
    /// SLMP仕様書準拠: [デバイス番号3バイト(LE), デバイスコード1バイト]
    /// </summary>
    /// <returns>4バイト配列</returns>
    public byte[] ToDeviceSpecificationBytes()
    {
        var result = new byte[4];
        var deviceNumberBytes = ToDeviceNumberBytes();

        // デバイス番号（3バイト）
        Array.Copy(deviceNumberBytes, 0, result, 0, 3);

        // デバイスコード（1バイト）
        result[3] = (byte)Code;

        return result;
    }

    /// <summary>
    /// デバッグ用文字列表現
    /// </summary>
    /// <returns>デバイス表記文字列（例: "D100", "W0x11AA"）</returns>
    public override string ToString()
    {
        if (IsHexAddress)
        {
            return $"{Code}0x{DeviceNumber:X}";
        }
        else
        {
            return $"{Code}{DeviceNumber}";
        }
    }

    /// <summary>
    /// 等価性比較（デバイスコードとデバイス番号で判定）
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is DeviceSpecification other)
        {
            return Code == other.Code && DeviceNumber == other.DeviceNumber;
        }
        return false;
    }

    /// <summary>
    /// ハッシュコード計算
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Code, DeviceNumber);
    }

    /// <summary>
    /// ReadRandomコマンドで指定可能かを検証
    /// </summary>
    /// <exception cref="InvalidOperationException">ReadRandom非対応デバイスの場合</exception>
    public void ValidateForReadRandom()
    {
        if (!Code.IsReadRandomSupported())
        {
            throw new InvalidOperationException(
                $"デバイス {ToString()} はReadRandomコマンドで指定できません（SLMP仕様書 page_64.png参照）");
        }
    }

    /// <summary>
    /// デバイス番号の範囲検証（3バイト範囲: 0～16777215）
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">範囲外の場合</exception>
    public void ValidateDeviceNumberRange()
    {
        const int MaxDeviceNumber = 0xFFFFFF; // 3バイト最大値（16777215）

        if (DeviceNumber < 0 || DeviceNumber > MaxDeviceNumber)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DeviceNumber),
                DeviceNumber,
                $"デバイス番号が範囲外です: {DeviceNumber}（有効範囲: 0～{MaxDeviceNumber}）");
        }
    }
}
