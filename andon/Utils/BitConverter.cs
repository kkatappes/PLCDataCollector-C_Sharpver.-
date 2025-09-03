using System;

namespace SlmpClient.Utils
{
    /// <summary>
    /// ビット操作ユーティリティ 
    /// Python版のunpack_bits/pack_bits相当機能
    /// </summary>
    public static class SlmpBitConverter
    {
        /// <summary>
        /// LSBから順に格納されているビット列を配列に展開する
        /// [&lt;M107 ... M100&gt;, &lt;M115 ... M108&gt;] --&gt;
        ///     [&lt;M100&gt;, ... ,&lt;M107&gt;, &lt;M108&gt;, ... ,&lt;M115&gt;]
        /// </summary>
        /// <param name="data">パック済みのデータ列</param>
        /// <param name="count">展開するビット数（0の場合は全ビット展開）</param>
        /// <returns>1項目１デバイスとしたビット配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        /// <exception cref="ArgumentException">countが負の場合、またはデータ長を超える場合</exception>
        public static bool[] UnpackBits(byte[] data, int count = 0)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (count < 0)
                throw new ArgumentException("Count cannot be negative", nameof(count));

            if (data.Length == 0)
                return Array.Empty<bool>();

            // countが0または未指定の場合は全ビット数を使用
            int bitCount = count > 0 ? count : data.Length * 8;
            
            if (bitCount > data.Length * 8)
                throw new ArgumentException("Count exceeds available bits", nameof(count));

            var result = new bool[bitCount];

            for (int byteIndex = 0; byteIndex < data.Length && byteIndex * 8 < bitCount; byteIndex++)
            {
                byte currentByte = data[byteIndex];
                
                // 各バイト内でLSBから順に処理（ビット順序を反転）
                for (int bitIndex = 0; bitIndex < 8 && byteIndex * 8 + bitIndex < bitCount; bitIndex++)
                {
                    result[byteIndex * 8 + bitIndex] = (currentByte & (1 << bitIndex)) != 0;
                }
            }

            return result;
        }

        /// <summary>
        /// ビットデータの配列をLSBから順に格納されたバイト列にパックする
        /// [&lt;M100&gt;, ... ,&lt;M107&gt;, &lt;M108&gt;, ... ,&lt;M115&gt;]  --&gt;
        ///     [&lt;M107 ... M100&gt;, &lt;M115 ... M108&gt;]
        /// </summary>
        /// <param name="data">デバイスごとのビットデータの配列</param>
        /// <returns>パックしたバイト配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        public static byte[] PackBits(bool[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length == 0)
                return Array.Empty<byte>();

            // データ数が8の倍数になるようにする
            int byteCount = (data.Length + 7) / 8;
            var result = new byte[byteCount];

            for (int byteIndex = 0; byteIndex < byteCount; byteIndex++)
            {
                byte currentByte = 0;
                
                // 各バイト内でLSBから順に処理（ビット順序を反転）
                for (int bitIndex = 0; bitIndex < 8; bitIndex++)
                {
                    int dataIndex = byteIndex * 8 + bitIndex;
                    if (dataIndex < data.Length && data[dataIndex])
                    {
                        currentByte |= (byte)(1 << bitIndex);
                    }
                }
                
                result[byteIndex] = currentByte;
            }

            return result;
        }

        /// <summary>
        /// 整数配列をビット配列に変換
        /// </summary>
        /// <param name="data">整数配列（0=false, 非0=true）</param>
        /// <returns>ビット配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        public static bool[] IntArrayToBits(int[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var result = new bool[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = data[i] != 0;
            }
            return result;
        }

        /// <summary>
        /// ビット配列を整数配列に変換
        /// </summary>
        /// <param name="data">ビット配列</param>
        /// <returns>整数配列（false=0, true=1）</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        public static int[] BitsToIntArray(bool[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var result = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = data[i] ? 1 : 0;
            }
            return result;
        }
    }
}