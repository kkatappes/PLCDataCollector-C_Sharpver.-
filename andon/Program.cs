using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlmpClient.Core;
using SlmpClient.Constants;

namespace SlmpClient
{
    /// <summary>
    /// メインプログラム - 完全デバイス探索システム統合実行
    /// run_rawdata_logging.bat → andon.exe で実行される
    /// ユーザー要求の4ステップフロー + 全39デバイス対応を統合実行
    /// </summary>
    public class Program
    {
        /// <summary>
        /// メインエントリーポイント
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        /// <returns>実行結果</returns>
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("===================================================");
            Console.WriteLine("    SLMP インテリジェント監視システム v2.0");
            Console.WriteLine("    全39デバイス対応・完全探索システム");
            Console.WriteLine("===================================================");
            Console.WriteLine();

            try
            {
                // 設定ファイル読み込み
                var config = LoadConfiguration();

                // ログ設定
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole()
                           .SetMinimumLevel(LogLevel.Information);
                });

                // 依存性注入コンテナを早期構築して統合出力システムを取得
                var earlyServiceProvider = BuildServiceProvider(config, loggerFactory);
                var earlyConsoleOutputManager = earlyServiceProvider.GetService<ConsoleOutputManager>();

                // アプリケーション開始ヘッダーを統合出力
                if (earlyConsoleOutputManager != null)
                {
                    await earlyConsoleOutputManager.WriteHeaderAsync("SLMP インテリジェント監視システム v2.0", "ApplicationStart",
                        context: new {
                            Version = "v2.0",
                            Description = "全39デバイス対応・完全探索システム",
                            ExecutionMode = "IntelligentMonitoring (6ステップフロー)"
                        });
                }

                Console.WriteLine("実行モード: IntelligentMonitoring (6ステップフロー)");
                Console.WriteLine();

                // 6ステップフローのみ実行
                await RunIntelligentMonitoringAsync(config, loggerFactory, earlyServiceProvider);

                // アプリケーション完了ヘッダーを統合出力
                if (earlyConsoleOutputManager != null)
                {
                    await earlyConsoleOutputManager.WriteHeaderAsync("アプリケーション実行完了", "ApplicationComplete",
                        context: new {
                            Status = "Success",
                            CompletionTime = DateTime.Now
                        });
                }

                Console.WriteLine();
                Console.WriteLine("===================================================");
                Console.WriteLine("実行完了");
                Console.WriteLine("===================================================");

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ エラーが発生しました: {ex.Message}");
                Console.WriteLine($"詳細: {ex}");
                return 1;
            }
        }

        /// <summary>
        /// インテリジェント監視システム実行（統合出力対応版）
        /// </summary>
        private static async Task RunIntelligentMonitoringAsync(IConfiguration config, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {

            // 統合出力管理システムを取得
            var integratedOutputManager = serviceProvider.GetService<IntegratedOutputManager>();
            var consoleOutputManager = serviceProvider.GetService<ConsoleOutputManager>();

            try
            {
                // アプリケーション開始出力（統合版）
                if (integratedOutputManager != null)
                {
                    var sessionInfo = new SessionStartInfo
                    {
                        SessionId = $"session_{DateTime.Now:yyyyMMdd_HHmmss}",
                        ProcessId = Environment.ProcessId,
                        ApplicationName = "SLMP インテリジェント監視システム",
                        Version = "v2.0",
                        Environment = "Production"
                    };

                    var configDetails = new ConfigurationDetails
                    {
                        ConfigFile = "appsettings.json",
                        ConnectionTarget = "全39デバイス対応・完全探索システム",
                        SlmpSettings = "6ステップフロー統合実行",
                        ContinuityMode = "ReturnDefaultAndContinue",
                        RawDataLogging = "Enabled",
                        LogOutputPath = "logs/rawdata_analysis.log"
                    };

                    await integratedOutputManager.WriteSessionStartAsync(sessionInfo, configDetails);
                }

                // 統合出力での6ステップフロー説明
                if (consoleOutputManager != null)
                {
                    await consoleOutputManager.WriteHeaderAsync("インテリジェント監視システム開始", "SystemStart",
                        context: new {
                            Steps = new string[] {
                                "1. 設定ファイルで接続するPLCを決定",
                                "2. PLCに接続し機器情報を取得",
                                "3. 機器情報からシリーズを判定し、デバイスコードを抽出",
                                "4. 全デバイスコード＋一般的な機器番号で網羅的スキャン",
                                "5. 応答があった(非ゼロデータ)デバイスを抽出",
                                "6. 抽出したデバイスのデータのみを継続的に取得"
                            }
                        });
                }

                // 従来のConsole出力も並行実行（視覚的な表示のため）
                Console.WriteLine("🚀 インテリジェント監視システム開始");
                Console.WriteLine("6ステップフロー:");
                Console.WriteLine("1. 設定ファイルで接続するPLCを決定");
                Console.WriteLine("2. PLCに接続し機器情報を取得");
                Console.WriteLine("3. 機器情報からシリーズを判定し、デバイスコードを抽出");
                Console.WriteLine("4. 全デバイスコード＋一般的な機器番号で網羅的スキャン");
                Console.WriteLine("5. 応答があった(非ゼロデータ)デバイスを抽出");
                Console.WriteLine("6. 抽出したデバイスのデータのみを継続的に取得");
                Console.WriteLine();

                // IntelligentMonitoringSystemを取得
                var monitoringSystem = serviceProvider.GetService<IntelligentMonitoringSystem>();
                if (monitoringSystem == null)
                {
                    throw new InvalidOperationException("IntelligentMonitoringSystemの初期化に失敗しました");
                }

                // 統合出力での進捗表示
                if (consoleOutputManager != null)
                {
                    await consoleOutputManager.WriteInfoAsync("依存性注入設定完了 - IntelligentMonitoringSystem準備完了", "SystemInitialization");
                }
                Console.WriteLine("✅ 依存性注入設定完了 - IntelligentMonitoringSystem準備完了");
                Console.WriteLine();

                // キャンセレーショントークン設定（Ctrl+Cで停止可能）
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // プロセス終了をキャンセル
                    cts.Cancel(); // 監視システムを停止
                };

                // 6ステップフロー実行（統合出力版）
                if (consoleOutputManager != null)
                {
                    await consoleOutputManager.WriteProgressAsync("6ステップフロー実行開始", 0, "SystemExecution");
                }
                Console.WriteLine("🎯 6ステップフロー実行開始");

                var result = await monitoringSystem.RunSixStepFlowAsync(config, cts.Token);

                if (result.Success)
                {
                    // 成功結果の統合出力
                    if (consoleOutputManager != null)
                    {
                        await consoleOutputManager.WriteResultAsync("6ステップフロー実行完了", 6, "SystemExecution",
                            new {
                                PlcTypeName = result.PlcTypeName,
                                PlcTypeCode = result.PlcTypeCode,
                                Duration = result.Duration.TotalSeconds,
                                MonitoringStarted = result.MonitoringStarted
                            });
                    }

                    Console.WriteLine("✅ 6ステップフロー実行完了");
                    Console.WriteLine($"📊 PLC型名: {result.PlcTypeName} ({result.PlcTypeCode})");
                    Console.WriteLine($"⏱️ 実行時間: {result.Duration.TotalSeconds:F1}秒");
                    Console.WriteLine($"🔄 監視開始: {(result.MonitoringStarted ? "Yes" : "No")}");

                    if (result.MonitoringStarted)
                    {
                        // 継続監視開始の統合出力
                        if (consoleOutputManager != null)
                        {
                            await consoleOutputManager.WriteInfoAsync("継続監視が開始されました", "ContinuousMonitoring",
                                stepNumber: null, context: new { LogOutputPath = "logs/rawdata_analysis.log", ControlInfo = "Ctrl+C で停止可能" });
                        }

                        Console.WriteLine();
                        Console.WriteLine("🚀 継続監視が開始されました");
                        Console.WriteLine("📄 ログ出力先: logs/rawdata_analysis.log");
                        Console.WriteLine("💡 Ctrl+C で停止できます");
                        Console.WriteLine();

                        // 継続監視中の状態表示
                        await DisplayMonitoringStatusAsync(monitoringSystem, cts.Token);
                    }
                }
                else
                {
                    // エラー結果の統合出力
                    if (consoleOutputManager != null)
                    {
                        await consoleOutputManager.WriteErrorAsync("6ステップフロー実行失敗", "SystemExecution", 6, result.ErrorMessage);
                    }
                    Console.WriteLine($"❌ 6ステップフロー実行失敗: {result.ErrorMessage}");
                    throw new InvalidOperationException(result.ErrorMessage);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
                Console.WriteLine("⏹️ ユーザーによりキャンセルされました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ インテリジェント監視実行エラー: {ex.Message}");
                Console.WriteLine($"詳細: {ex}");
                throw;
            }
        }

        /// <summary>
        /// 依存性注入コンテナを構築
        /// </summary>
        private static IServiceProvider BuildServiceProvider(IConfiguration config, ILoggerFactory loggerFactory)
        {
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            // 設定とロガーを追加
            services.AddSingleton(config);
            services.AddSingleton(loggerFactory);
            services.AddSingleton(typeof(ILogger<>), typeof(Microsoft.Extensions.Logging.Logger<>));

            // PLC接続設定の読み込み
            var plcSettings = config.GetSection("PlcConnection");
            var address = plcSettings["IpAddress"] ?? "192.168.1.10";
            var port = plcSettings.GetValue<int>("Port", 5007);
            var useTcp = plcSettings.GetValue<bool>("UseTcp", true);
            var frameVersion = plcSettings["FrameVersion"] ?? "4E";
            var isBinary = plcSettings.GetValue<bool>("IsBinary", true);

            // SLMP接続設定
            var slmpSettings = new SlmpConnectionSettings
            {
                Port = port,
                UseTcp = useTcp,
                Version = frameVersion == "3E" ? SlmpFrameVersion.Version3E : SlmpFrameVersion.Version4E,
                IsBinary = isBinary,
                ReceiveTimeout = TimeSpan.FromMilliseconds(config.GetSection("TimeoutSettings").GetValue<int>("ReceiveTimeoutMs", 3000)),
                ConnectTimeout = TimeSpan.FromMilliseconds(config.GetSection("TimeoutSettings").GetValue<int>("ConnectTimeoutMs", 10000))
            };

            // SlmpClientを登録
            services.AddSingleton<ISlmpClientFull>(provider =>
            {
                var logger = provider.GetService<ILogger<SlmpClient.Core.SlmpClient>>();
                return new SlmpClient.Core.SlmpClient(address, slmpSettings, logger);
            });

            // UnifiedLogWriterを登録
            services.AddSingleton<UnifiedLogWriter>(provider =>
            {
                var logger = provider.GetService<ILogger<UnifiedLogWriter>>();
                var logPath = config.GetSection("UnifiedLoggingSettings")["LogFilePath"] ?? "logs/rawdata_analysis.log";
                return new UnifiedLogWriter(logger!, logPath);
            });

            // ConsoleOutputManagerを登録
            services.AddSingleton<ConsoleOutputManager>(provider =>
            {
                var logger = provider.GetService<ILogger<ConsoleOutputManager>>();
                var outputPath = config.GetSection("ConsoleOutputSettings")["OutputFilePath"] ?? "logs/terminal_output.txt";
                return new ConsoleOutputManager(logger!, outputPath);
            });

            // IntegratedOutputManagerを登録
            services.AddSingleton<IntegratedOutputManager>(provider =>
            {
                var logger = provider.GetService<ILogger<IntegratedOutputManager>>();
                var unifiedLogWriter = provider.GetService<UnifiedLogWriter>();
                return new IntegratedOutputManager(logger!, unifiedLogWriter!);
            });

            // IntelligentMonitoringSystemを登録
            services.AddSingleton<IntelligentMonitoringSystem>(provider =>
            {
                var slmpClient = provider.GetService<ISlmpClientFull>();
                var logger = provider.GetService<ILogger<IntelligentMonitoringSystem>>();
                var unifiedLogWriter = provider.GetService<UnifiedLogWriter>();
                var configuration = provider.GetService<IConfiguration>();
                return new IntelligentMonitoringSystem(slmpClient!, logger!, unifiedLogWriter!, configuration!);
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 監視状態を継続表示
        /// </summary>
        private static async Task DisplayMonitoringStatusAsync(IntelligentMonitoringSystem monitoringSystem, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken); // 5秒間隔で状態表示

                    var statusReport = monitoringSystem.GetStatusReport();
                    Console.WriteLine($"📊 {DateTime.Now:HH:mm:ss} - {statusReport}");
                }
            }
            catch (OperationCanceledException)
            {
                // 正常なキャンセル
            }
        }




        /// <summary>
        /// 設定ファイル読み込み
        /// </summary>
        private static IConfiguration LoadConfiguration()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine($"📁 現在のディレクトリ: {currentDirectory}");

            var configPath = Path.Combine(currentDirectory, "appsettings.json");
            var configExists = File.Exists(configPath);
            Console.WriteLine($"📄 設定ファイル存在確認: {configPath} -> {(configExists ? "✅ 存在" : "❌ 不存在")}");

            var builder = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var config = builder.Build();

            // 設定値確認
            var discoveryMode = config.GetSection("DeviceDiscoverySettings")["DiscoveryMode"];
            Console.WriteLine($"🔧 設定確認: DeviceDiscoverySettings:DiscoveryMode = '{discoveryMode}'");

            return config;
        }
    }

}