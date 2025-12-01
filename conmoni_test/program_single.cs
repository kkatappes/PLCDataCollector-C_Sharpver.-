using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ConMoni_SingleTest
{
    /// <summary>
    /// PLC単一通信テストプログラム（フレーム解析機能統合版）
    /// 送信データはプログラムに埋め込み（ハードコード）
    /// </summary>
    class Program
    {
        // ========================================
        // PLC接続設定
        // ========================================
        private const string PLC_IP = "172.30.40.15";
        private const int PLC_PORT = 8192;
        private const int SOCKET_TIMEOUT_MS = 500;
        private const bool USE_TCP = false; // UDP使用

        // ========================================
        // 送信データ（ハードコード）
        // ========================================
        private static readonly int[] SEND_DATA = new int[]
        {
            84,0,0,0,0,0,0,255,255,3,0,200,0,32,0,3,4,0,0,48,0,72,238,0,168,75,238,0,168,82,238,0,168,92,238,0,168,170,24,1,168,220,24,1,168,164,25,1,168,184,25,1,168,204,25,1,168,224,25,1,168,32,0,0,144,178,222,0,144,122,223,0,144,222,223,0,144,80,224,0,144,97,224,0,144,166,224,0,144,187,224,0,144,206,224,0,144,224,6,0,156,240,6,0,156,4,7,0,156,32,7,0,156,64,7,0,156,80,7,0,156,103,7,0,156,119,7,0,156,137,7,0,156,154,7,0,156,174,7,0,156,190,7,0,156,222,7,0,156,34,8,0,156,50,8,0,156,69,8,0,156,85,8,0,156,104,8,0,156,8,23,0,156,32,23,0,156,48,23,0,156,72,23,0,156,96,23,0,156,112,23,0,156,130,23,0,156,160,23,0,156,0,9,0,157,32,9,0,157,64,9,0,157
        };

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                PrintHeader();

                // int配列をbyte配列に変換
                var accessPlcSetting = SEND_DATA.Select(i => (byte)i).ToArray();

                Console.WriteLine($"[確認] ✓ 送信データ準備完了: {accessPlcSetting.Length}バイト");
                Console.WriteLine();

                // 送信データ表示
                Console.WriteLine($"[構築] 送信データ ({accessPlcSetting.Length}バイト):");
                LogBinaryRawData(accessPlcSetting);
                Console.WriteLine();

                Console.WriteLine($"[構築] 送信データHEX（詳細）:");
                LogHexDump(accessPlcSetting);
                Console.WriteLine();

                // ソケット通信実行
                ExecuteSocketCommunication(accessPlcSetting);

                Console.WriteLine();
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine("[完了] プログラムが正常終了しました");
                Console.WriteLine("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine($"[エラー] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[エラー詳細] {ex.InnerException.Message}");
                }
                Console.WriteLine("=".PadRight(80, '='));
            }
            finally
            {
                Console.WriteLine("\n終了するにはEnterキーを押してください...");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// ヘッダー表示
        /// </summary>
        private static void PrintHeader()
        {
            Console.WriteLine("################################################################################");
            Console.WriteLine("# PLC単一通信テストプログラム（フレーム解析機能統合版）");
            Console.WriteLine("################################################################################");
            Console.WriteLine();
            Console.WriteLine("【テスト内容】");
            Console.WriteLine("- ハードコードされたデータをPLCに送信");
            Console.WriteLine("- 受信データの自動解析（3E/4E対応）");
            Console.WriteLine("- デバイス値の詳細表示");
            Console.WriteLine();
            Console.WriteLine("【接続設定】");
            Console.WriteLine($"  PLC IP       : {PLC_IP}");
            Console.WriteLine($"  PLC Port     : {PLC_PORT}");
            Console.WriteLine($"  プロトコル   : {(USE_TCP ? "TCP" : "UDP")}");
            Console.WriteLine($"  タイムアウト : {SOCKET_TIMEOUT_MS}ms");
            Console.WriteLine();
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();
        }

        /// <summary>
        /// ソケット通信実行
        /// </summary>
        private static void ExecuteSocketCommunication(byte[] sendData)
        {
            // ソケットタイプとプロトコルタイプを決定
            SocketType socketType = USE_TCP ? SocketType.Stream : SocketType.Dgram;
            ProtocolType protocolType = USE_TCP ? ProtocolType.Tcp : ProtocolType.Udp;

            using (Socket sock = new Socket(AddressFamily.InterNetwork, socketType, protocolType))
            {
                // タイムアウト設定
                sock.ReceiveTimeout = SOCKET_TIMEOUT_MS;
                sock.SendTimeout = SOCKET_TIMEOUT_MS;

                // 接続
                Console.WriteLine($"[接続] {PLC_IP}:{PLC_PORT} へ接続中...");
                sock.Connect(PLC_IP, PLC_PORT);
                Console.WriteLine($"[確認] ✓ 接続成功");
                Console.WriteLine();

                // 送信
                Console.WriteLine($"[送信] データ送信中 ({sendData.Length}バイト)...");
                int sentBytes = sock.Send(sendData);
                Console.WriteLine($"[確認] ✓ 送信完了 ({sentBytes}バイト)");
                Console.WriteLine();

                // 受信
                Console.WriteLine($"[受信] レスポンス受信中（タイムアウト: {SOCKET_TIMEOUT_MS}ms）...");
                byte[] buffer = new byte[4096];
                int bytesRead = 0;

                try
                {
                    bytesRead = sock.Receive(buffer);
                }
                catch (SocketException sockEx)
                {
                    if (sockEx.SocketErrorCode == SocketError.TimedOut)
                    {
                        Console.WriteLine($"[受信] ✗ タイムアウト（{SOCKET_TIMEOUT_MS}ms）");
                        Console.WriteLine("[注意] PLCからの応答がありません");
                        return;
                    }
                    throw;
                }

                if (bytesRead == 0)
                {
                    Console.WriteLine("[受信] ✗ データなし（0バイト）");
                    return;
                }

                // 受信データ表示
                byte[] response = buffer.Take(bytesRead).ToArray();
                Console.WriteLine($"[受信] 完了: {bytesRead} バイト");
                Console.WriteLine();

                Console.WriteLine($"[受信] レスポンスHEX:");
                LogHexDump(response);
                Console.WriteLine();

                Console.WriteLine($"[受信] レスポンス生データ:");
                LogBinaryRawData(response);
                Console.WriteLine();

                // フレーム解析実行（andon実装ベース）
                AnalyzeResponseFrame(response);
            }
        }

        /// <summary>
        /// 受信フレーム解析（andon実装を参考）
        /// </summary>
        private static void AnalyzeResponseFrame(byte[] rawData)
        {
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine("[解析] フレーム解析開始");
            Console.WriteLine("=".PadRight(80, '='));

            try
            {
                // 最小長チェック
                if (rawData == null || rawData.Length < 2)
                {
                    Console.WriteLine($"[エラー] フレームデータが短すぎます: {rawData?.Length ?? 0}バイト（最小2バイト必要）");
                    return;
                }

                // フレームタイプ判定（サブヘッダから）
                string frameType = DetectFrameType(rawData);
                Console.WriteLine($"[判定] フレームタイプ: {frameType}");
                Console.WriteLine($"[判定] サブヘッダ: 0x{rawData[0]:X2} 0x{rawData[1]:X2}");
                Console.WriteLine();

                // フレーム構造解析
                if (frameType == "3E")
                {
                    Parse3EFrame(rawData);
                }
                else if (frameType == "4E")
                {
                    Parse4EFrame(rawData);
                }
                else
                {
                    Console.WriteLine($"[エラー] 未知のフレームタイプです");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[エラー] フレーム解析失敗: {ex.Message}");
            }

            Console.WriteLine("=".PadRight(80, '='));
        }

        /// <summary>
        /// フレームタイプ判定（サブヘッダから）
        /// </summary>
        private static string DetectFrameType(byte[] rawData)
        {
            // 3Eフレーム応答: 0xD0 0x00
            if (rawData[0] == 0xD0 && rawData[1] == 0x00)
            {
                return "3E";
            }

            // 4Eフレーム応答: 0xD4 0x00
            if (rawData[0] == 0xD4 && rawData[1] == 0x00)
            {
                return "4E";
            }

            return "不明";
        }

        /// <summary>
        /// 3Eフレーム構造解析
        /// フレーム構造:
        ///   Idx 0-1: サブヘッダ（0xD0 0x00）
        ///   Idx 2: ネットワーク番号
        ///   Idx 3: PC番号
        ///   Idx 4-5: I/O番号（LE）
        ///   Idx 6: 局番
        ///   Idx 7-8: データ長（LE）
        ///   Idx 9-10: 終了コード（LE）
        ///   Idx 11~: 読出しデータ
        /// </summary>
        private static void Parse3EFrame(byte[] rawData)
        {
            if (rawData.Length < 11)
            {
                Console.WriteLine($"[エラー] 3Eフレームのデータ長が不足しています: {rawData.Length}バイト（最小11バイト必要）");
                return;
            }

            // ヘッダ情報抽出
            byte networkNo = rawData[2];
            byte pcNo = rawData[3];
            ushort ioNo = (ushort)(rawData[4] | (rawData[5] << 8));
            byte stationNo = rawData[6];
            ushort dataLength = (ushort)(rawData[7] | (rawData[8] << 8));
            ushort endCode = (ushort)(rawData[9] | (rawData[10] << 8));

            // ヘッダ情報表示
            Console.WriteLine("[解析] 3Eフレーム構造:");
            Console.WriteLine($"  ネットワーク番号: 0x{networkNo:X2}");
            Console.WriteLine($"  PC番号          : 0x{pcNo:X2}");
            Console.WriteLine($"  I/O番号         : 0x{ioNo:X4}");
            Console.WriteLine($"  局番            : 0x{stationNo:X2}");
            Console.WriteLine($"  データ長        : {dataLength}バイト");
            Console.WriteLine($"  終了コード      : 0x{endCode:X4} {(endCode == 0 ? "(正常)" : "(エラー)")}");
            Console.WriteLine();

            if (endCode != 0x0000)
            {
                Console.WriteLine($"[解析] ✗ PLCエラー応答（終了コード: 0x{endCode:X4}）");
                return;
            }

            // デバイスデータ抽出
            int deviceDataLength = rawData.Length - 11;
            if (deviceDataLength > 0)
            {
                byte[] deviceData = new byte[deviceDataLength];
                Array.Copy(rawData, 11, deviceData, 0, deviceDataLength);

                Console.WriteLine($"[解析] デバイスデータ ({deviceDataLength}バイト):");
                LogHexDump(deviceData);
                Console.WriteLine();

                // デバイス値抽出
                ExtractDeviceValues(deviceData);
            }
            else
            {
                Console.WriteLine("[解析] デバイスデータなし");
            }
        }

        /// <summary>
        /// 4Eフレーム構造解析
        /// フレーム構造（フレーム構築方法.md準拠）:
        ///   Idx 0-1: サブヘッダ（0xD4 0x00）
        ///   Idx 2-3: シーケンス番号（LE）
        ///   Idx 4-5: 予約（LE）
        ///   Idx 6: ネットワーク番号
        ///   Idx 7: PC番号
        ///   Idx 8-9: I/O番号（LE）
        ///   Idx 10: 局番
        ///   Idx 11-12: データ長（LE）
        ///   Idx 13-14: 終了コード（LE）
        ///   Idx 15~: 読出しデータ
        /// </summary>
        private static void Parse4EFrame(byte[] rawData)
        {
            if (rawData.Length < 15)
            {
                Console.WriteLine($"[エラー] 4Eフレームのデータ長が不足しています: {rawData.Length}バイト（最小15バイト必要）");
                return;
            }

            // ヘッダ情報抽出
            ushort sequenceNumber = (ushort)(rawData[2] | (rawData[3] << 8));
            ushort reserved = (ushort)(rawData[4] | (rawData[5] << 8));
            byte networkNo = rawData[6];
            byte pcNo = rawData[7];
            ushort ioNo = (ushort)(rawData[8] | (rawData[9] << 8));
            byte stationNo = rawData[10];
            ushort dataLength = (ushort)(rawData[11] | (rawData[12] << 8));
            ushort endCode = (ushort)(rawData[13] | (rawData[14] << 8));

            // ヘッダ情報表示
            Console.WriteLine("[解析] 4Eフレーム構造:");
            Console.WriteLine($"  シーケンス番号  : 0x{sequenceNumber:X4}");
            Console.WriteLine($"  予約            : 0x{reserved:X4}");
            Console.WriteLine($"  ネットワーク番号: 0x{networkNo:X2}");
            Console.WriteLine($"  PC番号          : 0x{pcNo:X2}");
            Console.WriteLine($"  I/O番号         : 0x{ioNo:X4}");
            Console.WriteLine($"  局番            : 0x{stationNo:X2}");
            Console.WriteLine($"  データ長        : {dataLength}バイト");
            Console.WriteLine($"  終了コード      : 0x{endCode:X4} {(endCode == 0 ? "(正常)" : "(エラー)")}");
            Console.WriteLine();

            if (endCode != 0x0000)
            {
                Console.WriteLine($"[解析] ✗ PLCエラー応答（終了コード: 0x{endCode:X4}）");
                return;
            }

            // デバイスデータ抽出（オフセット15以降）
            int deviceDataLength = rawData.Length - 15;
            if (deviceDataLength > 0)
            {
                byte[] deviceData = new byte[deviceDataLength];
                Array.Copy(rawData, 15, deviceData, 0, deviceDataLength);

                Console.WriteLine($"[解析] デバイスデータ ({deviceDataLength}バイト):");
                LogHexDump(deviceData);
                Console.WriteLine();

                // デバイス値抽出
                ExtractDeviceValues(deviceData);
            }
            else
            {
                Console.WriteLine("[解析] デバイスデータなし");
            }
        }

        /// <summary>
        /// デバイス値抽出（ワード型とビット型）
        /// </summary>
        private static void ExtractDeviceValues(byte[] deviceData)
        {
            // ワード型（Dデバイス等）の場合
            if (deviceData.Length >= 2 && deviceData.Length % 2 == 0)
            {
                Console.WriteLine("[解析] デバイス値（ワード型、リトルエンディアン）:");
                ExtractWordDevices(deviceData, startAddress: 0, count: Math.Min(10, deviceData.Length / 2));
            }

            // ビット型（Mデバイス等）の場合も表示
            Console.WriteLine();
            Console.WriteLine("[解析] デバイス値（ビット型、各バイト内のビット）:");
            ExtractBitDevices(deviceData, startAddress: 0, count: Math.Min(80, deviceData.Length * 8));
        }

        /// <summary>
        /// ワードデバイス値抽出（リトルエンディアン対応）
        /// </summary>
        private static void ExtractWordDevices(byte[] deviceData, int startAddress, int count)
        {
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
        /// ビットデバイス値抽出（Mデバイス等）
        /// </summary>
        private static void ExtractBitDevices(byte[] deviceData, int startAddress, int count)
        {
            int expectedBytes = (count + 7) / 8; // ビット数を8で割り上げ
            if (deviceData.Length < expectedBytes)
            {
                Console.WriteLine($"  [警告] データ長不足: 期待={expectedBytes}バイト、実際={deviceData.Length}バイト");
                count = deviceData.Length * 8;
            }

            // 10ビットごとに改行
            for (int i = 0; i < count; i++)
            {
                int byteIndex = i / 8;
                int bitIndex = i % 8;

                if (byteIndex >= deviceData.Length)
                    break;

                // ビット値抽出
                bool bitValue = (deviceData[byteIndex] & (1 << bitIndex)) != 0;

                if (i % 10 == 0)
                {
                    if (i > 0) Console.WriteLine();
                    Console.Write($"  M{startAddress + i,3}: ");
                }

                Console.Write(bitValue ? "1 " : "0 ");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// バイナリ生データ表示（1行HEX）
        /// </summary>
        private static void LogBinaryRawData(byte[] data)
        {
            var hex = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                hex.Append($"{data[i]:X2} ");
            }
            Console.WriteLine($"  {hex.ToString().TrimEnd()}");
        }

        /// <summary>
        /// HEXダンプ（16バイト/行、アドレス+HEX+ASCII）
        /// </summary>
        private static void LogHexDump(byte[] data)
        {
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

                Console.WriteLine($"  {i:X4}: {hex.ToString().PadRight(48)} {ascii}");
            }
        }
    }
}
