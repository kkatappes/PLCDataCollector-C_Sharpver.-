using System;
using System.Linq;

namespace SlmpClient.Utils
{
    /// <summary>
    /// BCD (Binary Coded Decimal) 変換ユーティリティ
    /// Python版のencode_bcd/decode_bcd相当機能
    /// </summary>
    public static class BcdConverter
    {
        /// <summary>
        /// 4bit BCD配列にエンコード
        /// [1,2,3,4,...] --> [0x12,0x34,...]
        /// 入力が奇数個の場合は最後の4bitを0埋め
        /// </summary>
        /// <param name="data">エンコード対象の整数配列</param>
        /// <returns>4bit毎にパックされた結果</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        /// <exception cref="ArgumentException">データに0-15の範囲外の値が含まれる場合</exception>
        public static byte[] Encode(int[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                return Array.Empty<byte>();

            // 値の範囲チェック（0-15のみ有効）
            if (data.Any(x => x < 0 || x > 15))
                throw new ArgumentException("BCD data must be in range 0-15", nameof(data));

            var result = new byte[(data.Length + 1) / 2];

            for (int i = 0; i < result.Length; i++)
            {
                byte high = (byte)(data[i * 2] & 0x0F);
                byte low = (i * 2 + 1 < data.Length) ? (byte)(data[i * 2 + 1] & 0x0F) : (byte)0;
                
                result[i] = (byte)((high << 4) | low);
            }

            return result;
        }

        /// <summary>
        /// 4bit BCD配列にエンコード（ushort配列版）
        /// </summary>
        /// <param name="data">エンコード対象のushort配列</param>
        /// <returns>4bit毎にパックされた結果</returns>
        public static byte[] Encode(ushort[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return Encode(data.Select(x => (int)x).ToArray());
        }

        /// <summary>
        /// 4bit BCD配列にエンコード（uint配列版）
        /// </summary>
        /// <param name="data">エンコード対象のuint配列</param>
        /// <returns>4bit毎にパックされた結果</returns>
        public static byte[] Encode(uint[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return Encode(data.Select(x => (int)x).ToArray());
        }

        /// <summary>
        /// 4bit BCD配列をデコード
        /// [0x12,0x34,...] --> [1,2,3,4,...]
        /// </summary>
        /// <param name="data">4bitにパックされたデータ列</param>
        /// <returns>デコードされた整数配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        public static int[] DecodeToInt(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                return Array.Empty<int>();

            var result = new int[data.Length * 2];

            for (int i = 0; i < data.Length; i++)
            {
                result[i * 2] = (data[i] >> 4) & 0x0F;
                result[i * 2 + 1] = data[i] & 0x0F;
            }

            return result;
        }

        /// <summary>
        /// 4bit BCD配列をデコード（ushort配列として返却）
        /// </summary>
        /// <param name="data">4bitにパックされたデータ列</param>
        /// <returns>デコードされたushort配列</returns>
        public static ushort[] DecodeToUshort(byte[] data)
        {
            var intArray = DecodeToInt(data);
            return intArray.Select(x => (ushort)x).ToArray();
        }

        /// <summary>
        /// 4bit BCD配列をデコード（uint配列として返却）
        /// </summary>
        /// <param name="data">4bitにパックされたデータ列</param>
        /// <returns>デコードされたuint配列</returns>
        public static uint[] DecodeToUint(byte[] data)
        {
            var intArray = DecodeToInt(data);
            return intArray.Select(x => (uint)x).ToArray();
        }
    }
}