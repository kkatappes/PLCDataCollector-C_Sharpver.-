using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlmpClient.Constants;

namespace SlmpClient.Utils
{
    /// <summary>
    /// データ処理ユーティリティ
    /// Python版のstr2bytes_buf/extracts_word_dword_data/device2ascii相当機能
    /// </summary>
    public static class DataProcessor
    {
        /// <summary>
        /// 2バイトの16進数が連続した文字列表現からバイト配列へ変換
        /// Python版のstr2bytes_buf相当
        /// </summary>
        /// <param name="hexString">16進表現の連なった文字列</param>
        /// <param name="encoding">文字エンコーディング（デフォルト: ASCII）</param>
        /// <returns>バイト配列</returns>
        /// <exception cref="ArgumentNullException">hexStringがnullの場合</exception>
        /// <exception cref="ArgumentException">不正な16進文字列の場合</exception>
        public static byte[] StringToBytesBuffer(string hexString, Encoding? encoding = null)
        {
            if (hexString == null)
                throw new ArgumentNullException(nameof(hexString));

            encoding ??= Encoding.ASCII;

            if (string.IsNullOrEmpty(hexString))
                return Array.Empty<byte>();

            // 16進文字列を逆順でペアごとに処理
            var chars = hexString.ToCharArray();
            Array.Reverse(chars);

            var result = new List<byte>();
            
            for (int i = 0; i < chars.Length; i += 2)
            {
                if (i + 1 >= chars.Length)
                    throw new ArgumentException("Hex string length must be even", nameof(hexString));

                var s1 = chars[i];
                var s2 = chars[i + 1];
                var hexByte = $"{s1}{s2}";

                if (!byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                    throw new ArgumentException($"Invalid hex string: {hexByte}", nameof(hexString));

                result.Add(value);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 2バイトデータ列と4バイトデータ列を切り分ける
        /// Python版のextracts_word_dword_data相当
        /// </summary>
        /// <param name="buffer">切り分け元のバイナリデータ</param>
        /// <param name="splitPosition">2バイトデータの数（バイト数ではなく要素数）</param>
        /// <returns>2バイトデータ列と4バイトデータ列のタプル</returns>
        /// <exception cref="ArgumentNullException">bufferがnullの場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">splitPositionが範囲外の場合</exception>
        public static (byte[][] wordData, byte[][] dwordData) ExtractWordDwordData(byte[] buffer, int splitPosition)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (splitPosition < 0)
                throw new ArgumentOutOfRangeException(nameof(splitPosition), "Split position cannot be negative");

            // 2バイトデータ部分のバイト数
            int wordByteCount = splitPosition * 2;
            
            if (wordByteCount > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(splitPosition), "Split position exceeds buffer size");

            var wordDataList = new List<byte[]>();
            var dwordDataList = new List<byte[]>();

            // 2バイトデータを切り分け（リトルエンディアン考慮）
            var wordBuffer = new byte[wordByteCount];
            Array.Copy(buffer, 0, wordBuffer, 0, wordByteCount);
            Array.Reverse(wordBuffer);

            for (int i = 0; i < wordBuffer.Length; i += 2)
            {
                if (i + 1 < wordBuffer.Length)
                {
                    var wordData = new byte[] { wordBuffer[i], wordBuffer[i + 1] };
                    wordDataList.Add(wordData);
                }
            }

            // 4バイトデータを切り分け（リトルエンディアン考慮）
            if (wordByteCount < buffer.Length)
            {
                var dwordBuffer = new byte[buffer.Length - wordByteCount];
                Array.Copy(buffer, wordByteCount, dwordBuffer, 0, dwordBuffer.Length);
                Array.Reverse(dwordBuffer);

                for (int i = 0; i < dwordBuffer.Length; i += 4)
                {
                    if (i + 3 < dwordBuffer.Length)
                    {
                        var dwordData = new byte[] { dwordBuffer[i], dwordBuffer[i + 1], dwordBuffer[i + 2], dwordBuffer[i + 3] };
                        dwordDataList.Add(dwordData);
                    }
                }
            }

            return (wordDataList.ToArray(), dwordDataList.ToArray());
        }

        /// <summary>
        /// ASCII形式時のデバイス表記へ変換する
        /// Python版のdevice2ascii相当
        /// </summary>
        /// <param name="deviceCode">デバイス種別</param>
        /// <param name="address">アドレス</param>
        /// <returns>ASCII形式時のデバイス表記</returns>
        /// <exception cref="ArgumentException">サポートされていないデバイスコードの場合</exception>
        public static string DeviceToAscii(DeviceCode deviceCode, uint address)
        {
            var deviceName = deviceCode.ToString();
            
            // デバイス名にアスタリスクを追加（1文字デバイス名の場合）
            var devicePrefix = deviceName.Length == 1 ? deviceName + "*" : deviceName;
            
            // 16進アドレスデバイスかどうかを判定
            if (deviceCode.IsHexAddress())
            {
                return $"{devicePrefix}{address:X6}";
            }
            else
            {
                return $"{devicePrefix}{address:D6}";
            }
        }

        /// <summary>
        /// バイト配列をushort配列に変換（リトルエンディアン）
        /// </summary>
        /// <param name="data">変換元バイト配列</param>
        /// <returns>ushort配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        /// <exception cref="ArgumentException">データ長が2の倍数でない場合</exception>
        public static ushort[] BytesToUshortArray(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length % 2 != 0)
                throw new ArgumentException("Data length must be even for ushort conversion", nameof(data));

            var result = new ushort[data.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
            }
            return result;
        }

        /// <summary>
        /// バイト配列をuint配列に変換（リトルエンディアン）
        /// </summary>
        /// <param name="data">変換元バイト配列</param>
        /// <returns>uint配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        /// <exception cref="ArgumentException">データ長が4の倍数でない場合</exception>
        public static uint[] BytesToUintArray(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length % 4 != 0)
                throw new ArgumentException("Data length must be multiple of 4 for uint conversion", nameof(data));

            var result = new uint[data.Length / 4];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (uint)(data[i * 4] | 
                                  (data[i * 4 + 1] << 8) | 
                                  (data[i * 4 + 2] << 16) | 
                                  (data[i * 4 + 3] << 24));
            }
            return result;
        }

        /// <summary>
        /// ushort配列をバイト配列に変換（リトルエンディアン）
        /// </summary>
        /// <param name="data">変換元ushort配列</param>
        /// <returns>バイト配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        public static byte[] UshortArrayToBytes(ushort[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var result = new byte[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                result[i * 2] = (byte)(data[i] & 0xFF);
                result[i * 2 + 1] = (byte)((data[i] >> 8) & 0xFF);
            }
            return result;
        }

        /// <summary>
        /// uint配列をバイト配列に変換（リトルエンディアン）
        /// </summary>
        /// <param name="data">変換元uint配列</param>
        /// <returns>バイト配列</returns>
        /// <exception cref="ArgumentNullException">dataがnullの場合</exception>
        public static byte[] UintArrayToBytes(uint[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var result = new byte[data.Length * 4];
            for (int i = 0; i < data.Length; i++)
            {
                result[i * 4] = (byte)(data[i] & 0xFF);
                result[i * 4 + 1] = (byte)((data[i] >> 8) & 0xFF);
                result[i * 4 + 2] = (byte)((data[i] >> 16) & 0xFF);
                result[i * 4 + 3] = (byte)((data[i] >> 24) & 0xFF);
            }
            return result;
        }

        /// <summary>
        /// 16進文字列をバイト配列に変換
        /// セルフテスト用のシンプルな16進文字列変換
        /// </summary>
        /// <param name="hexString">16進文字列（例: "ABCD1234"）</param>
        /// <returns>変換されたバイト配列</returns>
        /// <exception cref="ArgumentNullException">hexStringがnullの場合</exception>
        /// <exception cref="ArgumentException">不正な16進文字列の場合</exception>
        public static byte[] HexStringToBytes(string hexString)
        {
            if (hexString == null)
                throw new ArgumentNullException(nameof(hexString));

            if (string.IsNullOrEmpty(hexString))
                return Array.Empty<byte>();

            // 16進文字列は偶数長でなければならない
            if (hexString.Length % 2 != 0)
                throw new ArgumentException("Hex string length must be even", nameof(hexString));

            var result = new byte[hexString.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                var hexByte = hexString.Substring(i * 2, 2);
                if (!byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                    throw new ArgumentException($"Invalid hex string: {hexByte}", nameof(hexString));
                result[i] = value;
            }
            return result;
        }
    }
}