using System;
using Andon.Utilities;

namespace VerifyFix
{
    /// <summary>
    /// TerminalOutputHelper.Parse4EFrame()修正の検証
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== TerminalOutputHelper.Parse4EFrame() 修正検証 ===");
            Console.WriteLine();

            // 実機データ: D4 00 04 00 00 00 00 FF FF 03 00 04 00 00 00 21 05
            byte[] testData = new byte[]
            {
                0xD4, 0x00, // サブヘッダ (4E Binary)
                0x04, 0x00, // シーケンス番号 = 4
                0x00, 0x00, // 予約
                0x00,       // ネットワーク番号
                0xFF,       // PC番号
                0xFF, 0x03, // I/O番号 (LE: 0x03FF)
                0x00,       // 局番
                0x04, 0x00, // データ長 = 4バイト
                0x00, 0x00, // 終了コード = 0x0000 (正常終了)
                0x21, 0x05  // デバイスデータ = 0x0521 (LE) = 1313
            };

            Console.WriteLine("テストデータ:");
            Console.Write("  ");
            foreach (byte b in testData)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            Console.WriteLine();

            try
            {
                // Parse4EFrame を呼び出し
                var (endCode, deviceData) = TerminalOutputHelper.Parse4EFrame(testData);

                Console.WriteLine("解析結果:");
                Console.WriteLine($"  終了コード: 0x{endCode:X4}");
                Console.WriteLine($"  デバイスデータ長: {deviceData.Length}バイト");

                if (deviceData.Length >= 2)
                {
                    ushort deviceValue = (ushort)(deviceData[0] | (deviceData[1] << 8));
                    Console.WriteLine($"  デバイス値: 0x{deviceValue:X4} ({deviceValue} decimal)");
                }

                Console.WriteLine();
                Console.WriteLine("検証:");
                bool endCodeOk = (endCode == 0x0000);
                bool dataLengthOk = (deviceData.Length == 2);
                bool deviceDataOk = (deviceData.Length >= 2 &&
                                     deviceData[0] == 0x21 &&
                                     deviceData[1] == 0x05);

                Console.WriteLine($"  ✓ 終了コード = 0x0000: {(endCodeOk ? "OK" : "NG")}");
                Console.WriteLine($"  ✓ デバイスデータ長 = 2: {(dataLengthOk ? "OK" : "NG")}");
                Console.WriteLine($"  ✓ デバイスデータ = 0x21 0x05: {(deviceDataOk ? "OK" : "NG")}");

                Console.WriteLine();
                if (endCodeOk && dataLengthOk && deviceDataOk)
                {
                    Console.WriteLine("結果: ✅ 修正が正しく動作しています");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("結果: ❌ 修正に問題があります");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラー: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
