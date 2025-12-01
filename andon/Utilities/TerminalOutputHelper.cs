using System;
using System.Text;

namespace Andon.Utilities
{
    /// <summary>
    /// ターミナル出力用ヘルパークラス
    /// conmoni_testのPlcSingleTest.csから移植
    /// </summary>
    public static class TerminalOutputHelper
    {
        /// <summary>
        /// バイナリ生データ表示（1行HEX）
        /// </summary>
        /// <param name="data">表示するバイトデータ</param>
        /// <param name="prefix">プレフィックス文字列</param>
        public static void LogBinaryRawData(byte[] data, string prefix = "  ")
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine($"{prefix}(データなし)");
                return;
            }

            var hex = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                hex.Append($"{data[i]:X2} ");
            }
            Console.WriteLine($"{prefix}{hex.ToString().TrimEnd()}");
        }

        /// <summary>
        /// HEXダンプ（16バイト/行、アドレス+HEX+ASCII）
        /// </summary>
        /// <param name="data">表示するバイトデータ</param>
        /// <param name="prefix">プレフィックス文字列</param>
        public static void LogHexDump(byte[] data, string prefix = "  ")
        {
            if (data == null || data.Length == 0)
            {
                Console.WriteLine($"{prefix}(データなし)");
                return;
            }

            for (int i = 0; i < data.Length; i += 16)
            {
                var hex = new StringBuilder();
                var ascii = new StringBuilder();

                for (int j = 0; j < 16 && i + j < data.Length; j++)
                {
                    byte b = data[i + j];
                    hex.Append($"{b:X2} ");
                    ascii.Append((b >= 32 && b < 127) ? (char)b : '.');
                }

                Console.WriteLine($"{prefix}{i:X4}: {hex.ToString().PadRight(48)} {ascii}");
            }
        }

        /// <summary>
        /// フレームタイプ判定（3E/4E自動判定）
        /// </summary>
        /// <param name="data">判定するバイトデータ</param>
        /// <returns>フレームタイプ文字列</returns>
        public static string DetermineFrameType(byte[] data)
        {
            if (data == null || data.Length < 2)
            {
                return "不明";
            }

            // 4Eフレーム判定: サブヘッダ = 0xD400 (リトルエンディアン: D4 00)
            if (data[0] == 0xD4 && data[1] == 0x00)
            {
                return "4Eフレーム (Binary)";
            }

            // 3Eフレーム判定: サブヘッダ = 0xD000 (リトルエンディアン: D0 00)
            if (data[0] == 0xD0 && data[1] == 0x00)
            {
                return "3Eフレーム (Binary)";
            }

            // ASCII形式判定
            if (data[0] == 0x44) // 'D'
            {
                if (data.Length >= 2)
                {
                    if (data[1] == 0x30) // '0'
                        return "3Eフレーム (ASCII)";
                    if (data[1] == 0x34) // '4'
                        return "4Eフレーム (ASCII)";
                }
            }

            return $"不明 (0x{data[0]:X2} 0x{data[1]:X2})";
        }

        /// <summary>
        /// 3Eフレーム構造解析
        /// </summary>
        /// <param name="data">3Eフレームデータ</param>
        /// <returns>終了コードとデバイスデータのタプル</returns>
        public static (ushort EndCode, byte[] DeviceData) Parse3EFrame(byte[] data)
        {
            // 3Eフレーム構造（フレーム構築方法.md準拠）:
            // [サブヘッダ2] + [ネットワーク1] + [PC1] + [I/O2] + [局番1] + [データ長2] + [終了コード2] + [デバイスデータ]
            // 最小長: 11バイト
            if (data == null || data.Length < 11)
            {
                throw new FormatException($"3Eフレーム長不足: 最小11バイト必要、実際={data?.Length ?? 0}バイト");
            }

            // 終了コード抽出（オフセット9-10、リトルエンディアン）
            ushort endCode = (ushort)(data[9] | (data[10] << 8));

            // デバイスデータ抽出（オフセット11以降）
            byte[] deviceData = new byte[data.Length - 11];
            if (data.Length > 11)
            {
                Array.Copy(data, 11, deviceData, 0, deviceData.Length);
            }

            return (endCode, deviceData);
        }

        /// <summary>
        /// 4Eフレーム構造解析
        /// </summary>
        /// <param name="data">4Eフレームデータ</param>
        /// <returns>終了コードとデバイスデータのタプル</returns>
        public static (ushort EndCode, byte[] DeviceData) Parse4EFrame(byte[] data)
        {
            // 4Eフレーム構造（フレーム構築方法.md準拠）:
            // [サブヘッダ2] + [シーケンス2] + [予約2] + [ネットワーク1] + [PC1] + [I/O2] + [局番1] + [データ長2] + [終了コード2] + [デバイスデータ]
            // ⚠️ 重要: 応答フレームには監視タイマフィールドは含まれない（要求フレームのみ）
            // 最小長: 15バイト
            if (data == null || data.Length < 15)
            {
                throw new FormatException($"4Eフレーム長不足: 最小15バイト必要、実際={data?.Length ?? 0}バイト");
            }

            // 終了コード抽出（オフセット13-14、リトルエンディアン）
            ushort endCode = (ushort)(data[13] | (data[14] << 8));

            // デバイスデータ抽出（オフセット15以降）
            byte[] deviceData = new byte[data.Length - 15];
            if (data.Length > 15)
            {
                Array.Copy(data, 15, deviceData, 0, deviceData.Length);
            }

            return (endCode, deviceData);
        }

        /// <summary>
        /// 受信データ詳細解析（conmoni_test相当）
        /// </summary>
        /// <param name="data">解析するバイトデータ</param>
        public static void AnalyzeReceivedData(byte[] data)
        {
            try
            {
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine("[解析] 受信データ詳細解析開始");
                Console.WriteLine("=".PadRight(80, '='));

                // 1. フレームタイプ判定
                var frameType = DetermineFrameType(data);
                Console.WriteLine($"[解析] フレームタイプ: {frameType}");
                Console.WriteLine();

                // 2. フレーム構造解析
                ushort endCode;
                byte[] deviceData;

                if (frameType.Contains("3E"))
                {
                    (endCode, deviceData) = Parse3EFrame(data);
                }
                else if (frameType.Contains("4E"))
                {
                    (endCode, deviceData) = Parse4EFrame(data);
                }
                else
                {
                    Console.WriteLine("[解析] ✗ 不明なフレーム形式です");
                    Console.WriteLine($"[解析] サブヘッダ: 0x{data[0]:X2} 0x{data[1]:X2}");
                    Console.WriteLine();
                    Console.WriteLine("[解析] 生データ:");
                    LogHexDump(data);
                    return;
                }

                // 3. 終了コード確認
                Console.WriteLine($"[解析] 終了コード: 0x{endCode:X4} ({(endCode == 0x0000 ? "正常終了" : "エラー")})");
                Console.WriteLine();

                if (endCode != 0x0000)
                {
                    Console.WriteLine($"[解析] ✗ PLCエラー応答（終了コード: 0x{endCode:X4}）");
                    return;
                }

                // 4. デバイスデータ解析
                Console.WriteLine($"[解析] デバイスデータ長: {deviceData.Length}バイト");
                Console.WriteLine();

                // デバイスデータ表示（HEXダンプ）
                Console.WriteLine("[解析] デバイスデータ（HEX）:");
                LogHexDump(deviceData);
                Console.WriteLine();

                // 5. デバイス値抽出
                // ワード型（Dデバイス等）の場合
                if (deviceData.Length >= 2 && deviceData.Length % 2 == 0)
                {
                    Console.WriteLine("[解析] デバイス値抽出（ワード型、リトルエンディアン）:");
                    ExtractWordDevices(deviceData, startAddress: 0, count: Math.Min(10, deviceData.Length / 2));
                }

                Console.WriteLine();
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine("[解析] 受信データ詳細解析完了");
                Console.WriteLine("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[解析] ✗ エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ワードデバイス値抽出（リトルエンディアン対応）
        /// </summary>
        /// <param name="deviceData">デバイスデータ</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">抽出数</param>
        public static void ExtractWordDevices(byte[] deviceData, int startAddress, int count)
        {
            if (deviceData == null || deviceData.Length == 0)
            {
                Console.WriteLine("  (データなし)");
                return;
            }

            if (deviceData.Length < count * 2)
            {
                Console.WriteLine($"  [警告] データ長不足: 期待={count * 2}バイト、実際={deviceData.Length}バイト");
                count = deviceData.Length / 2;
            }

            for (int i = 0; i < count; i++)
            {
                int byteOffset = i * 2;
                if (byteOffset + 1 < deviceData.Length)
                {
                    // リトルエンディアンでワード値変換
                    ushort wordValue = (ushort)(deviceData[byteOffset] | (deviceData[byteOffset + 1] << 8));
                    Console.WriteLine($"  D{startAddress + i,3}: 0x{wordValue:X4} ({wordValue,5}) [Byte: 0x{deviceData[byteOffset]:X2} 0x{deviceData[byteOffset + 1]:X2}]");
                }
            }
        }

        /// <summary>
        /// 送信フレーム詳細表示
        /// </summary>
        /// <param name="data">送信データ</param>
        /// <param name="ipAddress">送信先IPアドレス</param>
        /// <param name="port">送信先ポート</param>
        public static void LogSendFrame(byte[] data, string ipAddress, int port)
        {
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"[送信] フレーム送信開始 → {ipAddress}:{port}");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"[送信] データサイズ: {data.Length}バイト");
            Console.WriteLine();
            Console.WriteLine("[送信] 生データ (HEX 1行):");
            LogBinaryRawData(data);
            Console.WriteLine();
            Console.WriteLine("[送信] HEXダンプ:");
            LogHexDump(data);
            Console.WriteLine("=".PadRight(80, '='));
        }

        /// <summary>
        /// 受信フレーム詳細表示
        /// </summary>
        /// <param name="data">受信データ</param>
        /// <param name="receiveTimeMs">受信時間（ミリ秒）</param>
        public static void LogReceiveFrame(byte[] data, double receiveTimeMs)
        {
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"[受信] フレーム受信完了 ({receiveTimeMs:F2}ms)");
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"[受信] データサイズ: {data.Length}バイト");
            Console.WriteLine();
            Console.WriteLine("[受信] 生データ (HEX 1行):");
            LogBinaryRawData(data);
            Console.WriteLine();
            Console.WriteLine("[受信] HEXダンプ:");
            LogHexDump(data);
            Console.WriteLine("=".PadRight(80, '='));
        }
    }
}
