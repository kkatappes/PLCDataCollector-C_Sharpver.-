using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Utilities
{
    /// <summary>
    /// SLMPデータパース用ユーティリティクラス
    /// Python PySLMPClientのutil.pyに相当する機能を提供
    /// </summary>
    public static class SlmpDataParser
    {
        /// <summary>
        /// 4bit BCD配列のデコード
        /// [0x12, 0x34] → [1, 2, 3, 4]
        /// </summary>
        /// <param name="data">BCDエンコードされたバイト配列</param>
        /// <returns>デコード後のバイト配列</returns>
        public static byte[] DecodeBcd(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            var result = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                result[i * 2] = (byte)((data[i] >> 4) & 0x0F);      // 上位4bit
                result[i * 2 + 1] = (byte)(data[i] & 0x0F);         // 下位4bit
            }
            return result;
        }

        /// <summary>
        /// ビット配列の展開（LSBから順）
        /// [0x85] → [1, 0, 1, 0, 0, 0, 0, 1]
        /// </summary>
        /// <param name="data">ビットパックされたバイト配列</param>
        /// <returns>展開後のbool配列</returns>
        public static bool[] UnpackBits(byte[] data)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<bool>();

            var result = new bool[data.Length * 8];
            for (int i = 0; i < data.Length; i++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    result[i * 8 + bit] = ((data[i] >> bit) & 1) == 1;  // LSBから
                }
            }
            return result;
        }

        /// <summary>
        /// 16進数文字列からバイト列への変換
        /// "1A2B" → [0x1A, 0x2B]
        /// </summary>
        /// <param name="hexString">16進数文字列（偶数長）</param>
        /// <returns>バイト配列</returns>
        public static byte[] HexStringToBytes(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return Array.Empty<byte>();

            if (hexString.Length % 2 != 0)
                throw new ArgumentException("16進数文字列の長さは偶数である必要があります", nameof(hexString));

            var result = new byte[hexString.Length / 2];
            for (int i = 0; i < hexString.Length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
            return result;
        }

        /// <summary>
        /// バイト配列から16進数文字列への変換
        /// [0x1A, 0x2B] → "1A2B"
        /// </summary>
        /// <param name="data">バイト配列</param>
        /// <returns>16進数文字列（大文字）</returns>
        public static string BytesToHexString(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            return BitConverter.ToString(data).Replace("-", "");
        }

        /// <summary>
        /// Word/Dwordデータの分離抽出
        /// バイナリデータを2バイトデータ（Word）と4バイトデータ（Dword）に分離
        /// </summary>
        /// <param name="buffer">入力バイト配列</param>
        /// <param name="splitPos">分割位置（Wordデータの終了位置）</param>
        /// <returns>Dwordデータリストとwordデータリストのタプル</returns>
        public static (List<byte[]> DwordData, List<byte[]> WordData) ExtractWordDwordData(byte[] buffer, int splitPos)
        {
            if (buffer == null || buffer.Length == 0)
                return (new List<byte[]>(), new List<byte[]>());

            if (splitPos < 0 || splitPos > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(splitPos), "分割位置が範囲外です");

            var wordData = new List<byte[]>();
            var dwordData = new List<byte[]>();

            // Wordデータ抽出（2バイト単位）
            for (int i = 0; i < splitPos; i += 2)
            {
                if (i + 1 < splitPos)
                {
                    wordData.Add(new byte[] { buffer[i], buffer[i + 1] });
                }
            }

            // Dwordデータ抽出（4バイト単位）
            for (int i = splitPos; i < buffer.Length; i += 4)
            {
                if (i + 3 < buffer.Length)
                {
                    dwordData.Add(new byte[] { buffer[i], buffer[i + 1], buffer[i + 2], buffer[i + 3] });
                }
            }

            return (dwordData, wordData);
        }

        /// <summary>
        /// ASCII形式の応答データから数値を抽出
        /// </summary>
        /// <param name="asciiData">ASCII形式データ文字列</param>
        /// <param name="startIndex">開始インデックス</param>
        /// <param name="length">抽出する文字数</param>
        /// <returns>16進数として解釈した整数値</returns>
        public static int ParseAsciiHex(string asciiData, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(asciiData))
                throw new ArgumentException("ASCIIデータが空です", nameof(asciiData));

            if (startIndex < 0 || startIndex + length > asciiData.Length)
                throw new ArgumentOutOfRangeException("インデックスまたは長さが範囲外です");

            string hexString = asciiData.Substring(startIndex, length);
            return Convert.ToInt32(hexString, 16);
        }

        /// <summary>
        /// ASCIIビットデータのパース
        /// "0110" → [false, true, true, false]
        /// </summary>
        /// <param name="asciiData">ASCIIビットデータ文字列（'0'/'1'）</param>
        /// <returns>bool配列</returns>
        public static bool[] ParseAsciiBitData(string asciiData)
        {
            if (string.IsNullOrEmpty(asciiData))
                return Array.Empty<bool>();

            return asciiData.Select(c => c == '1').ToArray();
        }

        /// <summary>
        /// ASCIIワードデータのパース
        /// "12AB34CD" → [0x12AB, 0x34CD]
        /// </summary>
        /// <param name="asciiData">ASCIIワードデータ文字列（4文字単位）</param>
        /// <returns>ushort配列</returns>
        public static ushort[] ParseAsciiWordData(string asciiData)
        {
            if (string.IsNullOrEmpty(asciiData))
                return Array.Empty<ushort>();

            if (asciiData.Length % 4 != 0)
                throw new ArgumentException("ASCIIワードデータの長さは4の倍数である必要があります", nameof(asciiData));

            var result = new ushort[asciiData.Length / 4];
            for (int i = 0; i < asciiData.Length; i += 4)
            {
                result[i / 4] = Convert.ToUInt16(asciiData.Substring(i, 4), 16);
            }
            return result;
        }

        /// <summary>
        /// Binaryビットデータのパース
        /// BCDデコード後、ビット値に変換
        /// </summary>
        /// <param name="binaryData">バイナリビットデータ</param>
        /// <param name="count">ビット数</param>
        /// <returns>bool配列</returns>
        public static bool[] ParseBinaryBitData(byte[] binaryData, int count)
        {
            if (binaryData == null || binaryData.Length == 0)
                return Array.Empty<bool>();

            var decoded = DecodeBcd(binaryData);
            var result = decoded.Select(b => b == 1).ToArray();

            // 奇数個の場合、最後の余分なビットを削除
            if (count % 2 == 1 && result.Length > count)
            {
                return result.Take(count).ToArray();
            }

            return result;
        }

        /// <summary>
        /// Binaryワードデータのパース
        /// バイナリデータを直接ushort配列として解釈（リトルエンディアン）
        /// </summary>
        /// <param name="binaryData">バイナリワードデータ</param>
        /// <returns>ushort配列</returns>
        public static ushort[] ParseBinaryWordData(byte[] binaryData)
        {
            if (binaryData == null || binaryData.Length == 0)
                return Array.Empty<ushort>();

            if (binaryData.Length % 2 != 0)
                throw new ArgumentException("バイナリワードデータの長さは2の倍数である必要があります", nameof(binaryData));

            var result = new ushort[binaryData.Length / 2];
            Buffer.BlockCopy(binaryData, 0, result, 0, binaryData.Length);
            return result;
        }
    }
}
