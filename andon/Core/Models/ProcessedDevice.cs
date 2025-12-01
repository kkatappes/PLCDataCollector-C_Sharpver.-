using System;
using System.Linq;

namespace Andon.Core.Models
{
    /// <summary>
    /// 処理済みデバイス情報を表すモデル
    /// TC029テスト実装で使用
    /// Phase2: ビット展開機能拡張
    /// </summary>
    public class ProcessedDevice
    {
        /// <summary>
        /// デバイス型 ("D", "M", "X", "Y" 等)
        /// </summary>
        public string DeviceType { get; set; } = string.Empty;

        /// <summary>
        /// デバイス番号
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// デバイス値
        /// </summary>
        public object Value { get; set; } = null!;

        /// <summary>
        /// データ型 ("Word", "Bit", "DWord" 等)
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 処理時刻
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// デバイス名 ("D100", "M000" 等)
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        // ===== Phase2: ビット展開機能拡張 =====

        /// <summary>
        /// ワード値（元の値、ushort型）
        /// </summary>
        public ushort RawValue { get; set; }

        /// <summary>
        /// 変換係数適用後の値
        /// </summary>
        public double ConvertedValue { get; set; }

        /// <summary>
        /// 変換係数（digitControl互換）
        /// </summary>
        public double ConversionFactor { get; set; } = 1.0;

        /// <summary>
        /// ビット展開するかどうか
        /// </summary>
        public bool IsBitExpanded { get; set; } = false;

        /// <summary>
        /// 展開されたビット配列（IsBitExpanded=trueの場合）
        /// [0]=bit0, [15]=bit15（LSB first）
        /// </summary>
        public bool[]? ExpandedBits { get; set; }

        /// <summary>
        /// ビット値を名前付きで取得
        /// </summary>
        /// <param name="bitPosition">ビット位置（0-15）</param>
        /// <returns>ビット値とビット名</returns>
        /// <exception cref="InvalidOperationException">ビット展開されていない場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">ビット位置が範囲外の場合</exception>
        public (bool Value, string BitName) GetBit(int bitPosition)
        {
            if (!IsBitExpanded || ExpandedBits == null)
            {
                throw new InvalidOperationException("Device is not bit-expanded");
            }

            if (bitPosition < 0 || bitPosition >= 16)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPosition), "Bit position must be 0-15");
            }

            string bitName = $"{DeviceName}.{bitPosition}";
            return (ExpandedBits[bitPosition], bitName);
        }

        /// <summary>
        /// デバイス情報の文字列表現（ビット展開対応）
        /// </summary>
        public override string ToString()
        {
            if (IsBitExpanded && ExpandedBits != null)
            {
                string bitsStr = string.Join("", ExpandedBits.Select(b => b ? "1" : "0"));
                return $"{DeviceName}: Raw={RawValue:X4}, Bits=[{bitsStr}]";
            }
            else
            {
                return $"{DeviceName}: Value={ConvertedValue} (Raw={RawValue}, Factor={ConversionFactor})";
            }
        }
    }
}