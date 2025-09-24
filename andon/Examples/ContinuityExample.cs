using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlmpClient.Core;
using SlmpClient.Constants;

namespace SlmpClient.Examples
{
    /// <summary>
    /// 稼働第一の継続機能使用例
    /// UDP通信でデータ欠損が発生してもシステムが動作し続ける例
    /// </summary>
    public class ContinuityExample
    {
        /// <summary>
        /// メイン実行例
        /// </summary>
        public static async Task RunExample()
        {
            // 設定ファイルを読み込み
            var config = LoadConfiguration();

            // 簡単なロガー設定
            ILogger<SlmpClient.Core.SlmpClient>? logger = null;

            // 設定ファイルから SLMP設定を生成
            var settings = config.ToSlmpConnectionSettings();

            Console.WriteLine("=== 稼働第一のSLMP継続機能デモ ===");
            Console.WriteLine($"設定ファイル: appsettings.json");
            Console.WriteLine($"接続先: {config.PlcConnection.IpAddress}:{config.PlcConnection.Port}");
            Console.WriteLine($"設定: {settings}");
            Console.WriteLine($"継続モード: {settings.ContinuitySettings.Mode}");
            Console.WriteLine();

            // PLCに接続（設定ファイルのIPアドレスを使用）
            using var client = new SlmpClient.Core.SlmpClient(config.PlcConnection.IpAddress, settings, logger);

            try
            {
                await client.ConnectAsync();
                Console.WriteLine("PLC接続成功");

                // 製造ラインの監視ループをシミュレート
                Console.WriteLine("製造ライン監視開始...");

                for (int cycle = 1; cycle <= config.MonitoringSettings.MaxCycles; cycle++)
                {
                    Console.WriteLine($"\n--- サイクル {cycle} ---");
                    
                    try
                    {
                        // センサー状態読み取り（ビットデバイス）
                        Console.WriteLine("センサー状態読み取り中...");
                        var sensorStates = await client.ReadBitDevicesAsync(DeviceCode.M, 100, 8, timeout: 3);
                        Console.WriteLine($"センサー状態: [{string.Join(", ", sensorStates)}]");

                        // 生産カウンター読み取り（ワードデバイス）
                        Console.WriteLine("生産カウンター読み取り中...");
                        var counters = await client.ReadWordDevicesAsync(DeviceCode.D, 200, 4, timeout: 3);
                        Console.WriteLine($"生産カウンター: [{string.Join(", ", counters)}]");

                        // 正常動作時の処理
                        Console.WriteLine("✓ データ読み取り成功 - 正常処理継続");
                    }
                    catch (Exception ex)
                    {
                        // この例では継続機能により例外は発生しないはずだが、
                        // 予期しないエラーに備えたフォールバック
                        Console.WriteLine($"⚠ 予期しないエラー: {ex.Message}");
                    }

                    // サイクル間の待機（設定ファイルから読み込み）
                    await Task.Delay(config.MonitoringSettings.CycleIntervalMs);
                }

                Console.WriteLine("\n=== 統計情報 ===");
                var stats = client.ErrorStatistics.GetSummary();
                Console.WriteLine($"総操作数: {stats.TotalOperations}");
                Console.WriteLine($"総エラー数: {stats.TotalErrors}");
                Console.WriteLine($"継続動作数: {stats.TotalContinuedOperations}");
                Console.WriteLine($"エラー率: {stats.ErrorRate:F1}%");
                Console.WriteLine($"継続率: {stats.ContinuityRate:F1}%");

                if (stats.TopErrors.Any())
                {
                    Console.WriteLine("\n=== 主要エラー ===");
                    foreach (var error in stats.TopErrors.Take(3))
                    {
                        Console.WriteLine($"- {error.ErrorType}: {error.DeviceCode}:{error.StartAddress} ({error.Count}回)");
                        Console.WriteLine($"  最終発生: {error.LastOccurred:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"  メッセージ: {error.LastExceptionMessage}");
                    }
                }

                Console.WriteLine("\n✓ システムは全期間を通じて動作し続けました！");
                Console.WriteLine("製造ラインの稼働を止めることなく監視が完了しました。");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ システムレベルエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 設定ファイルを読み込み
        /// </summary>
        private static ApplicationConfiguration LoadConfiguration()
        {
            // 設定ファイルのパスを決定
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            // 設定ファイルが存在しない場合の警告とデフォルト設定
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"⚠ 設定ファイルが見つかりません: {configPath}");
                Console.WriteLine("デフォルト設定を使用します。");
                return new ApplicationConfiguration();
            }

            try
            {
                // 設定ビルダーを作成
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

                var config = builder.Build();

                // ApplicationConfigurationオブジェクトにバインド
                var appConfig = new ApplicationConfiguration();
                config.Bind(appConfig);

                Console.WriteLine($"✓ 設定ファイルを読み込みました: {configPath}");
                return appConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 設定ファイル読み込みエラー: {ex.Message}");
                Console.WriteLine("デフォルト設定を使用します。");
                return new ApplicationConfiguration();
            }
        }

        /// <summary>
        /// カスタム設定例
        /// </summary>
        public static SlmpConnectionSettings CreateCustomContinuitySettings()
        {
            var settings = new SlmpConnectionSettings();
            
            // 基本通信設定
            settings.UseTcp = false;                    // UDP使用
            settings.ReceiveTimeout = TimeSpan.FromMilliseconds(500);
            settings.RetrySettings.MaxRetryCount = 1;  // 高速リトライ

            // 継続機能設定
            settings.ContinuitySettings.Mode = ErrorHandlingMode.ReturnDefaultAndContinue;
            settings.ContinuitySettings.DefaultBitValue = false;
            settings.ContinuitySettings.DefaultWordValue = 0;
            settings.ContinuitySettings.EnableErrorStatistics = true;
            settings.ContinuitySettings.EnableContinuityLogging = true;
            settings.ContinuitySettings.NotificationLevel = ErrorNotificationLevel.Warning;
            settings.ContinuitySettings.MaxNotificationFrequencySeconds = 30;

            return settings;
        }

        /// <summary>
        /// 高信頼性モード例
        /// </summary>
        public static async Task HighReliabilityModeExample()
        {
            // 設定ファイルを読み込み
            var config = LoadConfiguration();
            var settings = config.ToSlmpConnectionSettings();
            settings.ContinuitySettings.ApplyHighReliabilityMode(); // 高信頼性設定

            using var client = new SlmpClient.Core.SlmpClient(config.PlcConnection.IpAddress, settings);
            
            try
            {
                await client.ConnectAsync();
                
                // より厳密なエラー監視とログ出力
                var data = await client.ReadWordDevicesAsync(DeviceCode.D, 100, 10);
                Console.WriteLine($"高信頼性モード: 読み取り完了 [{string.Join(", ", data)}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"高信頼性モード: エラー検出 {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 実行可能なサンプルプログラム
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SLMP継続機能デモを開始します...");
            Console.WriteLine("※ 実際のPLCが接続されていない場合、継続機能によりデフォルト値が返却されます");
            Console.WriteLine();

            try
            {
                await ContinuityExample.RunExample();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"プログラムエラー: {ex.Message}");
            }

            Console.WriteLine("\nデモ完了。何かキーを押してください...");
            Console.ReadKey();
        }
    }
}