using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// 接続情報詳細出力クラス
    /// PLC接続時の詳細情報、ネットワーク状態、接続品質を記録・出力
    /// </summary>
    public class ConnectionInfoLogger : IConnectionInfoLogger
    {
        private readonly ILogger<ConnectionInfoLogger> _logger;
        private readonly string? _logFilePath;
        private readonly bool _enableFileLogging;
        private readonly bool _enableNetworkDiagnostics;

        public ConnectionInfoLogger(
            ILogger<ConnectionInfoLogger> logger,
            string? logFilePath = null,
            bool enableFileLogging = true,
            bool enableNetworkDiagnostics = true)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _enableFileLogging = enableFileLogging;
            _enableNetworkDiagnostics = enableNetworkDiagnostics;

            if (enableFileLogging)
            {
                _logFilePath = logFilePath ?? Path.Combine(
                    Environment.CurrentDirectory,
                    $"slmp_connection_log_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            }
        }

        /// <summary>
        /// 接続開始時の詳細情報をログ出力
        /// </summary>
        public async Task LogConnectionStart(
            string targetAddress,
            int port,
            SlmpConnectionSettings settings,
            TimeSpan connectTime)
        {
            var timestamp = DateTime.Now;
            var connectionInfo = new
            {
                Timestamp = timestamp,
                EventType = "ConnectionStart",
                TargetAddress = targetAddress,
                Port = port,
                Protocol = settings.UseTcp ? "TCP" : "UDP",
                FrameVersion = settings.Version.ToString(),
                BinaryMode = settings.IsBinary,
                Timeout = settings.ReceiveTimeout,
                ConnectTimeMs = connectTime.TotalMilliseconds,
                LocalInfo = await GetLocalNetworkInfo(),
                NetworkDiagnostics = _enableNetworkDiagnostics ? await GetNetworkDiagnostics(targetAddress, port) : null
            };

            // コンソール出力
            _logger.LogInformation("🔗 PLC接続開始");
            _logger.LogInformation("  対象: {TargetAddress}:{Port}", targetAddress, port);
            _logger.LogInformation("  プロトコル: {Protocol} ({FrameVersion})",
                settings.UseTcp ? "TCP" : "UDP", settings.Version);
            _logger.LogInformation("  モード: {Mode}", settings.IsBinary ? "Binary" : "ASCII");
            _logger.LogInformation("  タイムアウト: {Timeout}ms", settings.ReceiveTimeout);
            _logger.LogInformation("  接続時間: {ConnectTime:F2}ms", connectTime.TotalMilliseconds);

            // ネットワーク診断情報の表示は簡略化
            if (_enableNetworkDiagnostics && connectionInfo.NetworkDiagnostics != null)
            {
                _logger.LogInformation("  ネットワーク診断: 完了");
            }

            // ファイル出力
            if (_enableFileLogging)
            {
                await WriteLogEntryToFile(connectionInfo);
            }
        }

        /// <summary>
        /// 接続成功時の詳細情報をログ出力
        /// </summary>
        public async Task LogConnectionSuccess(
            string targetAddress,
            int port,
            TimeSpan totalConnectionTime,
            object? additionalInfo = null)
        {
            var timestamp = DateTime.Now;
            var connectionInfo = new
            {
                Timestamp = timestamp,
                EventType = "ConnectionSuccess",
                TargetAddress = targetAddress,
                Port = port,
                TotalConnectionTimeMs = totalConnectionTime.TotalMilliseconds,
                AdditionalInfo = additionalInfo,
                Success = true
            };

            // コンソール出力
            _logger.LogInformation("✅ PLC接続成功");
            _logger.LogInformation("  総接続時間: {TotalTime:F2}ms", totalConnectionTime.TotalMilliseconds);

            // ファイル出力
            if (_enableFileLogging)
            {
                await WriteLogEntryToFile(connectionInfo);
            }
        }

        /// <summary>
        /// 接続失敗時の詳細情報をログ出力
        /// </summary>
        public async Task LogConnectionFailure(
            string targetAddress,
            int port,
            Exception exception,
            TimeSpan attemptTime,
            int retryCount = 0)
        {
            var timestamp = DateTime.Now;
            var connectionInfo = new
            {
                Timestamp = timestamp,
                EventType = "ConnectionFailure",
                TargetAddress = targetAddress,
                Port = port,
                ErrorMessage = exception.Message,
                ExceptionType = exception.GetType().Name,
                AttemptTimeMs = attemptTime.TotalMilliseconds,
                RetryCount = retryCount,
                NetworkDiagnostics = _enableNetworkDiagnostics ? await GetNetworkDiagnostics(targetAddress, port) : null,
                Success = false
            };

            // コンソール出力
            _logger.LogError("❌ PLC接続失敗");
            _logger.LogError("  対象: {TargetAddress}:{Port}", targetAddress, port);
            _logger.LogError("  エラー: {ErrorMessage}", exception.Message);
            _logger.LogError("  試行時間: {AttemptTime:F2}ms", attemptTime.TotalMilliseconds);
            if (retryCount > 0)
            {
                _logger.LogError("  リトライ回数: {RetryCount}", retryCount);
            }

            // ネットワーク診断情報の表示は簡略化
            if (_enableNetworkDiagnostics && connectionInfo.NetworkDiagnostics != null)
            {
                _logger.LogWarning("  ネットワーク診断: エラー検出");
            }

            // ファイル出力
            if (_enableFileLogging)
            {
                await WriteLogEntryToFile(connectionInfo);
            }
        }

        /// <summary>
        /// 切断時の詳細情報をログ出力
        /// </summary>
        public async Task LogDisconnection(
            string targetAddress,
            int port,
            TimeSpan sessionDuration,
            long totalOperations,
            double averageResponseTime)
        {
            var timestamp = DateTime.Now;
            var disconnectionInfo = new
            {
                Timestamp = timestamp,
                EventType = "Disconnection",
                TargetAddress = targetAddress,
                Port = port,
                SessionDurationMs = sessionDuration.TotalMilliseconds,
                TotalOperations = totalOperations,
                AverageResponseTimeMs = averageResponseTime,
                OperationsPerSecond = totalOperations / Math.Max(sessionDuration.TotalSeconds, 1)
            };

            // コンソール出力
            _logger.LogInformation("🔌 PLC切断");
            _logger.LogInformation("  セッション時間: {SessionDuration:F1}秒", sessionDuration.TotalSeconds);
            _logger.LogInformation("  総操作数: {TotalOperations}", totalOperations);
            _logger.LogInformation("  平均応答時間: {AverageResponseTime:F2}ms", averageResponseTime);
            _logger.LogInformation("  操作/秒: {OperationsPerSecond:F2}", disconnectionInfo.OperationsPerSecond);

            // ファイル出力
            if (_enableFileLogging)
            {
                await WriteLogEntryToFile(disconnectionInfo);
            }
        }

        /// <summary>
        /// ローカルネットワーク情報を取得
        /// </summary>
        private async Task<object> GetLocalNetworkInfo()
        {
            try
            {
                var hostName = Environment.MachineName;
                var hostEntry = await Dns.GetHostEntryAsync(hostName);
                var localIPs = new List<string>();

                foreach (var ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIPs.Add(ip.ToString());
                    }
                }

                return new
                {
                    HostName = hostName,
                    LocalIPs = localIPs,
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message };
            }
        }

        /// <summary>
        /// ネットワーク診断情報を取得
        /// </summary>
        private async Task<object?> GetNetworkDiagnostics(string targetAddress, int port)
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync(targetAddress, 5000);

                return new
                {
                    PingResult = new
                    {
                        Success = reply.Status == IPStatus.Success,
                        Status = reply.Status.ToString(),
                        RoundtripTime = reply.RoundtripTime,
                        Options = reply.Options != null ? new
                        {
                            Ttl = reply.Options.Ttl,
                            DontFragment = reply.Options.DontFragment
                        } : null
                    },
                    Timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                return new { Error = ex.Message };
            }
        }

        /// <summary>
        /// ログエントリをJSONファイルに出力
        /// </summary>
        private async Task WriteLogEntryToFile(object logEntry)
        {
            if (!_enableFileLogging || string.IsNullOrEmpty(_logFilePath))
                return;

            try
            {
                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await File.AppendAllTextAsync(_logFilePath, json + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "接続ログファイル出力に失敗しました: {LogFilePath}", _logFilePath);
            }
        }

        /// <summary>
        /// ログファイルパスを取得
        /// </summary>
        public string? GetLogFilePath() => _logFilePath;

        /// <summary>接続情報をログに記録</summary>
        public void LogConnectionInfo(string targetAddress, int port, bool isConnected)
        {
            var status = isConnected ? "接続成功" : "接続失敗";
            _logger.LogInformation("📡 接続情報: {TargetAddress}:{Port} - {Status}",
                targetAddress, port, status);
        }
    }
}