using System;
using System.Collections.Generic;

namespace Andon.Utilities
{
    /// <summary>
    /// ビット展開ユーティリティ（ConMoni互換）
    /// ワード値を16ビット配列に展開する機能を提供
    /// </summary>
    /// <remarks>
    /// ConMoniのgetPlcData()メソッドのビット展開処理を再現:
    /// - binary = format(r.astype(np.uint16), '016b')
    /// - binary[::-1]  # LSB first化
    /// - binary_list = list(map(int, binary))
    /// </remarks>
    public static class BitExpansionUtility
    {
        /// <summary>
        /// ワード値を16ビット配列に展開（LSB first）
        /// </summary>
        /// <param name="wordValue">16ビットワード値</param>
        /// <returns>ビット配列（[0]=bit0, [1]=bit1, ..., [15]=bit15）</returns>
        /// <remarks>
        /// ConMoniのbinary[::-1]ロジックを再現。
        /// PLCのビットデバイス仕様に合わせてLSB first順序で展開:
        /// - 配列の先頭(index 0) = bit0（最下位ビット）
        /// - 配列の最後(index 15) = bit15（最上位ビット）
        ///
        /// 例: 0x0003 (10進: 3)
        ///   2進数: 0000 0000 0000 0011
        ///   展開結果: [1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0]
        ///             ↑ ↑
        ///           bit0 bit1
        /// </remarks>
        public static bool[] ExpandWordToBits(ushort wordValue)
        {
            var bits = new bool[16];
            for (int i = 0; i < 16; i++)
            {
                // ビットマスクで各ビットを抽出（LSB first）
                // (1 << i) でi番目のビット位置を作成し、AND演算で抽出
                bits[i] = (wordValue & (1 << i)) != 0;
            }
            return bits;
        }

        /// <summary>
        /// ワード値を16ビット配列に展開（int版オーバーロード）
        /// </summary>
        /// <param name="wordValue">ワード値（下位16ビットのみ使用）</param>
        /// <returns>ビット配列（[0]=bit0, [15]=bit15）</returns>
        /// <remarks>
        /// 32ビットint値の下位16ビットのみを使用してビット展開。
        /// 上位16ビットは無視される。
        /// </remarks>
        public static bool[] ExpandWordToBits(int wordValue)
        {
            // 下位16ビットのみ使用（上位16ビットをマスク）
            return ExpandWordToBits((ushort)(wordValue & 0xFFFF));
        }

        /// <summary>
        /// 複数ワードを一括ビット展開
        /// </summary>
        /// <param name="wordValues">ワード値配列</param>
        /// <returns>ビット配列（各ワード16ビット × ワード数）</returns>
        /// <remarks>
        /// 複数のワード値を順番にビット展開し、連結して返す。
        ///
        /// 例: [0x0001, 0x0002]
        ///   → [1,0,0,...,0, 0,1,0,...,0]
        ///      └─ 16bits ─┘ └─ 16bits ─┘
        /// </remarks>
        public static bool[] ExpandMultipleWordsToBits(ushort[] wordValues)
        {
            var allBits = new List<bool>(wordValues.Length * 16);
            foreach (var word in wordValues)
            {
                allBits.AddRange(ExpandWordToBits(word));
            }
            return allBits.ToArray();
        }

        /// <summary>
        /// 選択的ビット展開（ConMoniの accessBitDataLoc 互換）
        /// </summary>
        /// <param name="wordValues">ワード値配列</param>
        /// <param name="bitExpansionMask">ビット展開フラグ配列（true=展開、false=ワード値のまま）</param>
        /// <param name="conversionFactors">変換係数配列（nullの場合は1.0）</param>
        /// <returns>混合データリスト（boolまたはdouble）</returns>
        /// <exception cref="ArgumentException">配列長が一致しない場合</exception>
        /// <remarks>
        /// ConMoniのgetPlcData()処理を再現:
        /// 1. 変換係数適用（digitControl互換）
        /// 2. ビット展開フラグに応じて処理分岐
        ///    - true: 16ビット展開（LSB first）
        ///    - false: ワード値のまま
        ///
        /// 例: words=[3, 255], mask=[false, true], factors=[1.0, 1.0]
        ///   → [3.0, 1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0]
        /// </remarks>
        public static List<object> ExpandWithSelectionMask(
            ushort[] wordValues,
            bool[] bitExpansionMask,
            double[]? conversionFactors = null)
        {
            // 配列長チェック
            if (wordValues.Length != bitExpansionMask.Length)
            {
                throw new ArgumentException(
                    $"Array length mismatch: wordValues={wordValues.Length}, bitExpansionMask={bitExpansionMask.Length}");
            }

            if (conversionFactors != null && conversionFactors.Length != wordValues.Length)
            {
                throw new ArgumentException(
                    $"Array length mismatch: wordValues={wordValues.Length}, conversionFactors={conversionFactors.Length}");
            }

            var result = new List<object>();

            for (int i = 0; i < wordValues.Length; i++)
            {
                // 変換係数適用（ConMoniの digitControl 互換）
                double convertedValue = wordValues[i];
                if (conversionFactors != null && i < conversionFactors.Length)
                {
                    convertedValue = wordValues[i] * conversionFactors[i];
                }

                if (bitExpansionMask[i])
                {
                    // ビット展開モード
                    var bits = ExpandWordToBits((ushort)convertedValue);
                    foreach (var bit in bits)
                    {
                        result.Add(bit);
                    }
                }
                else
                {
                    // ワード値モード
                    result.Add(convertedValue);
                }
            }

            return result;
        }
    }
}
