// 必要なライブラリのインポート
using System;                    // 基本的なシステム機能
using System.Collections.Generic; // ジェネリックコレクション
using System.IO;                  // ファイル入出力操作
using System.Linq;                // LINQ拡張メソッド
using System.Net.Sockets;         // ネットワークソケット通信
using System.Threading;           // スレッド操作
using Newtonsoft.Json;            // JSON データの変換
using Npgsql;                     // PostgreSQL データベース接続
using static ConMoni3.Program;

namespace ConMoni3
{
    /// <summary>
    /// メインプログラムクラス
    /// アプリケーションのエントリーポイントと設定データ構造を定義
    /// </summary>
    class Program
    {
        /// <summary>
        /// PLC接続設定データを格納するクラス
        /// JSONファイルから読み込まれる設定情報を保持
        /// </summary>
        public class SettingData
        {
            /// <summary>PLCアクセス用のコマンドデータ（バイト配列として送信される）</summary>
            public List<int> AccessPlcSetting { get; set; }

            /// <summary>接続先のIPアドレス</summary>
            public string IPAddress { get; set; }

            /// <summary>接続ポート番号</summary>
            public int Port { get; set; }

            /// <summary>接続方式（TCP または UDP）</summary>
            public string ConnectionMethod { get; set; }

            /// <summary>データベース保存時に使用する識別キー</summary>
            public string Key { get; set; }
        }

        /// <summary>
        /// アプリケーションのメインエントリーポイント
        /// 無限ループでアプリケーションを実行し、エラー発生時は自動復旧を行う
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        static void Main(string[] args)
        {
            // 無限ループでアプリケーションを継続実行
            while (true)
            {
                try
                {
                    // アプリケーションインスタンスを作成して実行
                    var app = new App();
                    app.Run();
                }
                catch (Exception ex)
                {
                    // エラーが発生した場合はログを出力
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    // 5秒間待機してから再試行（CPU使用率を抑制）
                    Thread.Sleep(5000);
                }
            }
        }
    }

    /// <summary>
    /// メインアプリケーションクラス
    /// PLC機器との通信とデータベースへのデータ保存を管理
    /// </summary>
    public class App
    {
        /// <summary>設定ファイル（JSON）のパス配列</summary>
        private readonly string[] _targetFiles;

        /// <summary>読み込まれた設定データのリスト</summary>
        private readonly List<SettingData> _targetList;

        /// <summary>PostgreSQLデータベース接続オブジェクト</summary>
        private NpgsqlConnection _connection;

        /// <summary>データベース接続文字列</summary>
        private readonly string _connString;

        /// <summary>
        /// Appクラスのコンストラクタ
        /// 設定ファイルの読み込みとデータベース接続の初期化を行う
        /// </summary>
        public App()
        {
            // アプリケーションの実行ディレクトリを取得
            string currentDir = AppContext.BaseDirectory;
            // 設定ファイル格納用のdataディレクトリパスを構築
            string dataDir = Path.Combine(currentDir, "data");

            // dataディレクトリの存在確認
            if (!Directory.Exists(dataDir))
            {
                throw new DirectoryNotFoundException($"The data directory '{dataDir}' does not exist.");
            }

            // dataディレクトリ内のすべてのJSONファイルを取得
            _targetFiles = Directory.GetFiles(dataDir, "*.json");

            // 設定データリストを初期化
            _targetList = new List<SettingData>();

            // 各JSONファイルを読み込んで設定データに変換
            foreach (var path in _targetFiles)
            {
                var data = JsonConvert.DeserializeObject<SettingData>(File.ReadAllText(path));
                _targetList.Add(data);
            }

            // PostgreSQLデータベースの接続文字列を設定
            _connString = "Host=172.17.104.10;Username=postgres;Password=Password123!;Database=cbm_stream";

            // 初回データベース接続を確立
            EstablishConnection();
        }

        /// <summary>
        /// データベース接続を確立するメソッド
        /// 接続に失敗した場合は無限ループで再接続を試行する
        /// </summary>
        private void EstablishConnection()
        {
            // 接続が成功するまで無限ループで試行
            while (true)
            {
                try
                {
                    // 新しいPostgreSQLコネクションを作成
                    _connection = new NpgsqlConnection(_connString);
                    // データベースに接続
                    _connection.Open();
                    Console.WriteLine("Database connection established.");
                    // 接続に成功したらループを抜ける
                    break;
                }
                catch (Exception ex)
                {
                    // 接続失敗時はエラーメッセージを出力
                    Console.WriteLine($"Failed to connect to database: {ex.Message}");
                    // 3秒間待機してから再試行（サーバーへの負荷を軽減）
                    Thread.Sleep(3000);
                }
            }
        }

        /// <summary>
        /// 指定された設定でPLC機器にソケット接続し、データを送受信するメソッド
        /// </summary>
        /// <param name="settingData">接続設定データ（IP、ポート、プロトコル等）</param>
        /// <returns>受信データ、キー、タイムスタンプのタプル（失敗時はnullを返す）</returns>
        private (byte[] Response, string Key, DateTime TimeStamp) SocketConnection(SettingData settingData)
        {
            // 接続方式に応じてソケットタイプを決定（TCP: Stream, UDP: Dgram）
            SocketType socketType = settingData.ConnectionMethod == "TCP" ? SocketType.Stream : SocketType.Dgram;
            // 接続方式に応じてプロトコルタイプを決定
            ProtocolType protocolType = settingData.ConnectionMethod == "TCP" ? ProtocolType.Tcp : ProtocolType.Udp;

            var startTime = DateTime.Now;
            Console.WriteLine($"[{settingData.Key}] === Socket Connection Start ===");
            Console.WriteLine($"[{settingData.Key}] Target: {settingData.IPAddress}:{settingData.Port} ({settingData.ConnectionMethod})");

            try
            {
                // usingステートメントでソケットリソースの自動解放を保証
                using (Socket sock = new Socket(AddressFamily.InterNetwork, socketType, protocolType))
                {
                    // 受信タイムアウトを3ミリ秒に設定（応答性を重視）
                    sock.ReceiveTimeout = 3;

                    var connectStart = DateTime.Now;
                    // 指定されたIPアドレスとポートに接続
                    sock.Connect(settingData.IPAddress, settingData.Port);
                    var connectTime = (DateTime.Now - connectStart).TotalMilliseconds;
                    Console.WriteLine($"[{settingData.Key}] Connected in {connectTime:F2}ms");

                    // PLCアクセス設定（int配列）をbyte配列に変換
                    var accessPlcSetting = settingData.AccessPlcSetting.Select(i => (byte)i).ToArray();

                    // ログ: 送信データの詳細
                    Console.WriteLine($"[{settingData.Key}] Sending {accessPlcSetting.Length} bytes");
                    Console.WriteLine($"[{settingData.Key}] TX Hex: {string.Join(" ", accessPlcSetting.Take(Math.Min(20, accessPlcSetting.Length)).Select(b => $"{b:X2}"))}...");

                    var sendStart = DateTime.Now;
                    // PLCにコマンドデータを送信
                    sock.Send(accessPlcSetting);
                    var sendTime = (DateTime.Now - sendStart).TotalMilliseconds;
                    Console.WriteLine($"[{settingData.Key}] Sent in {sendTime:F2}ms");

                    // 受信用バッファ（4KB）を準備
                    byte[] buffer = new byte[4096];

                    var receiveStart = DateTime.Now;
                    // PLCからの応答データを受信
                    int bytesRead = sock.Receive(buffer);
                    var receiveTime = (DateTime.Now - receiveStart).TotalMilliseconds;

                    Console.WriteLine($"[{settingData.Key}] Received {bytesRead} bytes in {receiveTime:F2}ms");

                    // データが受信されなかった場合は失敗として扱う
                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"[{settingData.Key}] ERROR: No data received");
                        return (null, null, default);
                    }

                    // ログ: 受信データの詳細
                    var receivedData = buffer.Take(bytesRead).ToArray();
                    Console.WriteLine($"[{settingData.Key}] RX Hex (first 20 bytes): {string.Join(" ", receivedData.Take(Math.Min(20, bytesRead)).Select(b => $"{b:X2}"))}...");
                    Console.WriteLine($"[{settingData.Key}] RX Full Hex: {BitConverter.ToString(receivedData).Replace("-", "")}");

                    // 戻り値用のデータを準備
                    string key = settingData.Key;
                    DateTime dateTime = DateTime.Now;

                    var totalTime = (dateTime - startTime).TotalMilliseconds;
                    Console.WriteLine($"[{settingData.Key}] Total time: {totalTime:F2}ms");
                    Console.WriteLine($"[{settingData.Key}] === Socket Connection Complete ===\n");

                    // 実際に受信したサイズ分だけのデータを返す
                    return (receivedData, key, dateTime);
                }
            }
            catch (SocketException sockEx)
            {
                // ソケット関連のエラー（接続失敗、タイムアウト等）
                Console.WriteLine($"[{settingData.Key}] Socket error ({settingData.ConnectionMethod}): {sockEx.Message}");
                Console.WriteLine($"[{settingData.Key}] SocketErrorCode: {sockEx.SocketErrorCode}");
                Console.WriteLine($"[{settingData.Key}] === Socket Connection Failed ===\n");
                return (null, null, default);
            }
            catch (Exception ex)
            {
                // その他の予期しないエラー
                Console.WriteLine($"[{settingData.Key}] Unexpected error: {ex.Message}");
                Console.WriteLine($"[{settingData.Key}] Stack trace: {ex.StackTrace}");
                Console.WriteLine($"[{settingData.Key}] === Socket Connection Failed ===\n");
                return (null, null, default);
            }
        }

        /// <summary>
        /// メインの処理ループを実行するメソッド
        /// 各PLC設定に対してデータを取得し、データベースに保存する処理を無限ループで実行
        /// </summary>
        public void Run()
        {
            // アプリケーションのメイン実行ループ
            while (true)
            {
                // 設定リスト内の各PLC設定に対して処理を実行
                foreach (var settings in _targetList)
                {
                    // PLC機器に接続してデータを取得
                    var (response, key, dateTime) = SocketConnection(settings);

                    // データ取得に失敗した場合は次の設定にスキップ
                    if (response == null || key == null || dateTime == default)
                        continue;

                    // 受信データの簡易解析とログ出力
                    AnalyzeResponseFrame(response, key);

                    // データベースINSERT用のSQL文を定義
                    string sql = "INSERT INTO cbm (c_datetime, key, value) VALUES (@datetime, @key, @value)";

                    // データベース挿入が成功するまでリトライ
                    bool success = false;
                    while (!success)
                    {
                        try
                        {
                            // パラメータ化クエリでデータを安全に挿入
                            using (var cmd = new NpgsqlCommand(sql, _connection))
                            {
                                cmd.Parameters.AddWithValue("datetime", dateTime);
                                cmd.Parameters.AddWithValue("key", key);
                                cmd.Parameters.AddWithValue("value", response);
                                cmd.ExecuteNonQuery();
                            }
                            // 挿入が成功した場合にフラグを更新
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            // データベースエラーが発生した場合
                            Console.WriteLine($"Database error: {ex.Message}");
                            try
                            {
                                // 既存の接続を閉じる
                                _connection.Close();
                            }
                            catch
                            {
                                // 接続クローズ時のエラーは無視（既に切断されている可能性があるため）
                            }
                            // データベースへの再接続を試行
                            EstablishConnection();
                        }
                    }
                }
                // CPU使用率を抑制するため、短時間（10ms）スリープ
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// 受信フレーム解析（andon実装を参考）
        /// 連続実行モード用の簡易版
        /// </summary>
        private static void AnalyzeResponseFrame(byte[] rawData, string key)
        {
            try
            {
                // 最小長チェック
                if (rawData == null || rawData.Length < 2)
                {
                    Console.WriteLine($"[{key}] フレーム解析エラー: データ長不足 ({rawData?.Length ?? 0}バイト)");
                    return;
                }

                // フレームタイプ判定
                string frameType = DetectFrameType(rawData);

                // フレーム構造解析
                if (frameType == "3E")
                {
                    ParseAndLog3EFrame(rawData, key);
                }
                else if (frameType == "4E")
                {
                    ParseAndLog4EFrame(rawData, key);
                }
                else
                {
                    Console.WriteLine($"[{key}] 未知のフレームタイプ: 0x{rawData[0]:X2} 0x{rawData[1]:X2}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{key}] フレーム解析エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// フレームタイプ判定（サブヘッダから）
        /// </summary>
        private static string DetectFrameType(byte[] rawData)
        {
            // 3Eフレーム応答: 0xD0 0x00
            if (rawData[0] == 0xD0 && rawData[1] == 0x00)
                return "3E";

            // 4Eフレーム応答: 0xD4 0x00
            if (rawData[0] == 0xD4 && rawData[1] == 0x00)
                return "4E";

            return "不明";
        }

        /// <summary>
        /// 3Eフレーム解析とログ出力（簡易版）
        /// </summary>
        private static void ParseAndLog3EFrame(byte[] rawData, string key)
        {
            if (rawData.Length < 11)
            {
                Console.WriteLine($"[{key}] 3Eフレーム: データ長不足 ({rawData.Length}バイト)");
                return;
            }

            ushort endCode = (ushort)(rawData[9] | (rawData[10] << 8));
            int deviceDataLength = rawData.Length - 11;

            Console.WriteLine($"[{key}] 3Eフレーム: 終了コード=0x{endCode:X4} {(endCode == 0 ? "正常" : "エラー")}, データ長={deviceDataLength}バイト");
        }

        /// <summary>
        /// 4Eフレーム解析とログ出力（簡易版）
        /// </summary>
        private static void ParseAndLog4EFrame(byte[] rawData, string key)
        {
            if (rawData.Length < 13)
            {
                Console.WriteLine($"[{key}] 4Eフレーム: データ長不足 ({rawData.Length}バイト)");
                return;
            }

            ushort endCode = (ushort)(rawData[11] | (rawData[12] << 8));
            int deviceDataLength = rawData.Length - 13;

            Console.WriteLine($"[{key}] 4Eフレーム: 終了コード=0x{endCode:X4} {(endCode == 0 ? "正常" : "エラー")}, データ長={deviceDataLength}バイト");
        }
    }
}