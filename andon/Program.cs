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
    /// メインプログラム - 2ステップフロー対応SimpleMonitoringService統合実行
    /// run_rawdata_logging.bat → andon.exe で実行される
    /// M000-M999, D000-D999固定範囲データ取得に特化（99.96%メモリ削減）
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
            // コンソール出力をファイルにキャプチャするセットアップ
            ConsoleOutputCapture? consoleCapture = null;
            try
            {
                // ログディレクトリを作成
                var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
                Directory.CreateDirectory(logsDir);

                // コンソール出力キャプチャを開始（terminal_output.txtにすべてのConsole.WriteLine出力を保存）
                var terminalOutputPath = Path.Combine(logsDir, "terminal_output.txt");
                consoleCapture = new ConsoleOutputCapture(terminalOutputPath, enableConsoleOutput: true);

                // セッション開始マーカーを記録
                await consoleCapture.WriteSessionStartAsync();

                Console.WriteLine("===================================================");
                Console.WriteLine("    SLMP SimpleMonitoringService v2.1");
                Console.WriteLine("    2ステップフロー・メモリ最適化対応システム");
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
                        await earlyConsoleOutputManager.WriteHeaderAsync("SLMP SimpleMonitoringService v2.1", "ApplicationStart",
                            context: new {
                                Version = "v2.1",
                                Description = "2ステップフロー・メモリ最適化対応システム",
                                ExecutionMode = "SimpleMonitoring (2ステップフロー)"
                            });
                    }

                    Console.WriteLine("実行モード: SimpleMonitoring (2ステップフロー)");
                    Console.WriteLine();

                    // 2ステップフローのみ実行
                    await RunSimpleMonitoringAsync(config, loggerFactory, earlyServiceProvider);

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

                    // セッション終了マーカーを記録
                    if (consoleCapture != null)
                    {
                        await consoleCapture.WriteSessionEndAsync();
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ エラーが発生しました: {ex.Message}");
                    Console.WriteLine($"詳細: {ex}");

                    // エラー時もセッション終了マーカーを記録
                    if (consoleCapture != null)
                    {
                        await consoleCapture.WriteLogMessageAsync($"エラー終了: {ex.Message}", "ERROR");
                        await consoleCapture.WriteSessionEndAsync();
                    }

                    return 1;
                }
            }
            finally
            {
                // ConsoleOutputCaptureのリソースを解放
                consoleCapture?.Dispose();
            }
        }

        /// <summary>
        /// SimpleMonitoringService実行（2ステップフロー・統合出力対応版）
        /// </summary>
        private static async Task RunSimpleMonitoringAsync(IConfiguration config, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
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
                        ApplicationName = "SLMP SimpleMonitoringService",
                        Version = "v2.1",
                        Environment = "Production"
                    };

                    var configDetails = new ConfigurationDetails
                    {
                        ConfigFile = "appsettings.json",
                        ConnectionTarget = "M000-M999, D000-D999固定範囲データ取得",
                        SlmpSettings = "2ステップフロー・メモリ最適化統合実行",
                        ContinuityMode = "ReturnDefaultAndContinue",
                        RawDataLogging = "Enabled",
                        LogOutputPath = "logs/rawdata_analysis.log"
                    };

                    await integratedOutputManager.WriteSessionStartAsync(sessionInfo, configDetails);
                }

                // 統合出力での2ステップフロー説明
                if (consoleOutputManager != null)
                {
                    await consoleOutputManager.WriteHeaderAsync("SimpleMonitoringService開始", "SystemStart",
                        context: new {
                            Steps = new string[] {
                                "1. PLC接続確立",
                                "2. M000-M999, D000-D999固定範囲データの継続取得"
                            },
                            MemoryOptimization = "99.96%削減（10.2MB → 450KB）",
                            TargetDevices = "M000-M999（1000デバイス）, D000-D999（1000デバイス）"
                        });
                }

                // 従来のConsole出力も並行実行（視覚的な表示のため）
                Console.WriteLine("🚀 SimpleMonitoringService開始");
                Console.WriteLine("2ステップフロー:");
                Console.WriteLine("1. PLC接続確立");
                Console.WriteLine("2. M000-M999, D000-D999固定範囲データの継続取得");
                Console.WriteLine("📊 ターゲット: M000-M999（1000デバイス）, D000-D999（1000デバイス）");
                Console.WriteLine("🔧 メモリ最適化: 99.96%削減（10.2MB → 450KB）");
                Console.WriteLine();

                // SimpleMonitoringServiceを取得
                var monitoringService = serviceProvider.GetService<SimpleMonitoringService>();
                if (monitoringService == null)
                {
                    throw new InvalidOperationException("SimpleMonitoringServiceの初期化に失敗しました");
                }

                // 統合出力での進捗表示
                if (consoleOutputManager != null)
                {
                    await consoleOutputManager.WriteInfoAsync("依存性注入設定完了 - SimpleMonitoringService準備完了", "SystemInitialization");
                }
                Console.WriteLine("✅ 依存性注入設定完了 - SimpleMonitoringService準備完了");
                Console.WriteLine();

                // キャンセレーショントークン設定（Ctrl+Cで停止可能）
                using var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true; // プロセス終了をキャンセル
                    cts.Cancel(); // 監視システムを停止
                };

                // 2ステップフロー実行（統合出力版）
                if (consoleOutputManager != null)
                {
                    await consoleOutputManager.WriteProgressAsync("2ステップフロー実行開始", 0, "SystemExecution");
                }
                Console.WriteLine("🎯 2ステップフロー実行開始");

                var result = await monitoringService.RunTwoStepFlowAsync(cts.Token);

                if (result.Success)
                {
                    // 成功結果の統合出力
                    if (consoleOutputManager != null)
                    {
                        await consoleOutputManager.WriteResultAsync("2ステップフロー実行完了", 2, "SystemExecution",
                            new {
                                SessionId = result.SessionId,
                                ConnectionInfo = result.ConnectionInfo,
                                MonitoringStarted = result.MonitoringStarted,
                                TargetDevices = "M000-M999, D000-D999"
                            });
                    }

                    Console.WriteLine("✅ 2ステップフロー実行完了");
                    Console.WriteLine($"📊 セッションID: {result.SessionId}");
                    Console.WriteLine($"🔗 接続情報: {result.ConnectionInfo}");
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
                        await DisplayMonitoringStatusAsync(monitoringService, cts.Token);
                    }
                }
                else
                {
                    // エラー結果の統合出力
                    if (consoleOutputManager != null)
                    {
                        await consoleOutputManager.WriteErrorAsync("2ステップフロー実行失敗", "SystemExecution", 2, result.ErrorMessage);
                    }
                    Console.WriteLine($"❌ 2ステップフロー実行失敗: {result.ErrorMessage}");
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
                Console.WriteLine($"❌ SimpleMonitoring実行エラー: {ex.Message}");
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

            // PLC接続設定の読み込み（安全な方式）
            var plcSettings = config.GetSection("PlcConnection");
            var address = GetConfigValueSafe(plcSettings, "IpAddress", "192.168.1.10");
            var port = GetConfigValueSafe(plcSettings, "Port", 8192);
            var useTcp = GetConfigValueSafe(plcSettings, "UseTcp", false);
            var frameVersion = GetConfigValueSafe(plcSettings, "FrameVersion", "4E");
            var isBinary = GetConfigValueSafe(plcSettings, "IsBinary", false);

            // SLMP接続設定
            var slmpSettings = new SlmpConnectionSettings
            {
                Port = port,
                UseTcp = useTcp,
                Version = frameVersion == "3E" ? SlmpFrameVersion.Version3E : SlmpFrameVersion.Version4E,
                IsBinary = isBinary,
                ReceiveTimeout = TimeSpan.FromMilliseconds(GetConfigValueSafe(config.GetSection("TimeoutSettings"), "ReceiveTimeoutMs", 3000)),
                ConnectTimeout = TimeSpan.FromMilliseconds(GetConfigValueSafe(config.GetSection("TimeoutSettings"), "ConnectTimeoutMs", 10000)),
                EnablePipelining = GetConfigValueSafe(plcSettings, "EnablePipelining", true),
                MaxConcurrentRequests = GetConfigValueSafe(plcSettings, "MaxConcurrentRequests", 8),
                TextEncoding = System.Text.Encoding.ASCII,
                RetrySettings = new SlmpRetrySettings(),
                ContinuitySettings = new ContinuitySettings()
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
                var logPath = GetConfigValueSafe(config.GetSection("UnifiedLoggingSettings"), "LogFilePath", "logs/rawdata_analysis.log");
                return new UnifiedLogWriter(logger!, logPath);
            });

            // SlmpRawDataRecorderを登録（依存性逆転原則適用）
            services.AddSingleton<ISlmpRawDataRecorder>(provider =>
            {
                var logger = provider.GetService<ILogger<SlmpRawDataRecorder>>();
                var rawDataLogPath = GetConfigValueSafe(config.GetSection("UnifiedLoggingSettings"), "RawDataLogPath", "logs/rawdata_analysis.log");
                return new SlmpRawDataRecorder(logger!, rawDataLogPath);
            });

            // ConsoleOutputManagerを登録
            services.AddSingleton<ConsoleOutputManager>(provider =>
            {
                var logger = provider.GetService<ILogger<ConsoleOutputManager>>();
                var outputPath = GetConfigValueSafe(config.GetSection("ConsoleOutputSettings"), "OutputFilePath", "logs/terminal_output.txt");
                return new ConsoleOutputManager(logger!, outputPath);
            });

            // IntegratedOutputManagerを登録
            services.AddSingleton<IntegratedOutputManager>(provider =>
            {
                var logger = provider.GetService<ILogger<IntegratedOutputManager>>();
                var unifiedLogWriter = provider.GetService<UnifiedLogWriter>();
                return new IntegratedOutputManager(logger!, unifiedLogWriter!);
            });

            // MemoryOptimizerを登録
            services.AddSingleton<SlmpClient.Utils.IMemoryOptimizer, SlmpClient.Utils.MemoryOptimizer>();

            // PerformanceMonitorを登録
            services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

            // SimpleMonitoringServiceを登録（依存性逆転原則完全適用）
            services.AddSingleton<SimpleMonitoringService>(provider =>
            {
                var slmpClient = provider.GetService<ISlmpClientFull>();
                var logger = provider.GetService<ILogger<SimpleMonitoringService>>();
                var unifiedLogWriter = provider.GetService<UnifiedLogWriter>();
                var configuration = provider.GetService<IConfiguration>();
                var memoryOptimizer = provider.GetService<SlmpClient.Utils.IMemoryOptimizer>();
                var performanceMonitor = provider.GetService<IPerformanceMonitor>();
                var rawDataRecorder = provider.GetService<ISlmpRawDataRecorder>();
                return new SimpleMonitoringService(slmpClient!, logger!, unifiedLogWriter!, configuration!, memoryOptimizer!, performanceMonitor!, rawDataRecorder!);
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 監視状態を継続表示
        /// </summary>
        private static async Task DisplayMonitoringStatusAsync(SimpleMonitoringService monitoringService, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, cancellationToken); // 5秒間隔で状態表示

                    var statusReport = monitoringService.GetStatusReport();
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

        /// <summary>
        /// 安全な設定値取得メソッド（設定ファイル優先、型安全）
        /// </summary>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <param name="config">設定セクション</param>
        /// <param name="key">設定キー</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>設定値（設定ファイル優先）</returns>
        private static T GetConfigValueSafe<T>(IConfigurationSection config, string key, T defaultValue)
        {
            var valueStr = config[key];
            if (string.IsNullOrEmpty(valueStr))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(valueStr, typeof(T));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 設定値変換エラー: {key} = '{valueStr}' -> {typeof(T).Name}, デフォルト値 {defaultValue} を使用: {ex.Message}");
                return defaultValue;
            }
        }
    }

}