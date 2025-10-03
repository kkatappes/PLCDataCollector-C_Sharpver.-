using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;
using SlmpClient.Exceptions;

namespace SlmpClient.Core
{
    /// <summary>
    /// 強化版SLMPクライアント
    /// 詳細ログ、パフォーマンス監視、品質監視、リアルタイムダッシュボード機能を統合
    /// </summary>
    public class EnhancedSlmpClient : ISlmpClient, IDisposable
    {
        #region Private Fields

        private readonly SlmpClient _innerClient;
        private readonly ConnectionInfoLogger? _connectionLogger;
        private readonly PerformanceMonitor? _performanceMonitor;
        private readonly NetworkQualityMonitor? _networkQualityMonitor;
        private readonly CommunicationDashboard? _dashboard;
        private readonly ILogger<EnhancedSlmpClient> _logger;

        private DateTime _sessionStartTime = DateTime.Now;
        private long _totalOperations = 0;
        private bool _disposed = false;

        #endregion

        #region ISlmpClient Properties

        public string Address => _innerClient.Address;
        public SlmpTarget Target
        {
            get => _innerClient.Target;
            set => _innerClient.Target = value;
        }
        public SlmpConnectionSettings Settings => _innerClient.Settings;
        public bool IsConnected => _innerClient.IsConnected;

        #endregion

        public EnhancedSlmpClient(
            string address,
            SlmpConnectionSettings? settings = null,
            ILogger<EnhancedSlmpClient>? logger = null,
            ConnectionInfoLogger? connectionLogger = null,
            PerformanceMonitor? performanceMonitor = null,
            NetworkQualityMonitor? networkQualityMonitor = null,
            CommunicationDashboard? dashboard = null)
        {
            _innerClient = new SlmpClient(address, settings, null);
            _logger = logger;
            _connectionLogger = connectionLogger;
            _performanceMonitor = performanceMonitor;
            _networkQualityMonitor = networkQualityMonitor;
            _dashboard = dashboard;

            _sessionStartTime = DateTime.Now;

            // ダッシュボードに接続情報を設定
            _dashboard?.UpdateConnectionInfo(address, settings?.Port ?? 5007, false);
        }

        #region Connection Management

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 接続開始ログ
                if (_connectionLogger != null)
                {
                    await _connectionLogger.LogConnectionStart(Address, Settings.Port, Settings, TimeSpan.Zero);
                }

                // ネットワーク品質監視に接続イベント記録
                _networkQualityMonitor?.RecordConnectionEvent(ConnectionEventType.Connected,
                    $"接続試行開始: {Address}:{Settings.Port}");

                // 実際の接続
                await _innerClient.ConnectAsync(cancellationToken);
                stopwatch.Stop();

                // 接続成功ログ
                if (_connectionLogger != null)
                {
                    await _connectionLogger.LogConnectionSuccess(Address, Settings.Port, stopwatch.Elapsed);
                }

                // ダッシュボード更新
                _dashboard?.UpdateConnectionInfo(Address, Settings.Port, true);

                _logger?.LogInformation("✅ 強化版SLMPクライアント接続完了: {Address}:{Port} ({Time:F2}ms)",
                    Address, Settings.Port, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // 接続失敗ログ
                if (_connectionLogger != null)
                {
                    await _connectionLogger.LogConnectionFailure(Address, Settings.Port, ex, stopwatch.Elapsed);
                }

                // ネットワーク品質監視にエラーイベント記録
                _networkQualityMonitor?.RecordConnectionEvent(ConnectionEventType.Error, ex.Message);

                _logger?.LogError("❌ 強化版SLMPクライアント接続失敗: {Error}", ex.Message);
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _innerClient.DisconnectAsync(cancellationToken);

                // セッション統計を計算
                var sessionDuration = DateTime.Now - _sessionStartTime;
                var averageResponseTime = _performanceMonitor?.AverageResponseTime ?? 0;

                // 切断ログ
                if (_connectionLogger != null)
                {
                    await _connectionLogger.LogDisconnection(Address, Settings.Port, sessionDuration,
                        _totalOperations, averageResponseTime);
                }

                // ネットワーク品質監視に切断イベント記録
                _networkQualityMonitor?.RecordConnectionEvent(ConnectionEventType.Disconnected,
                    $"正常切断: セッション時間{sessionDuration.TotalSeconds:F1}秒");

                // ダッシュボード更新
                _dashboard?.UpdateConnectionInfo(Address, Settings.Port, false);

                _logger?.LogInformation("🔌 強化版SLMPクライアント切断完了");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "強化版SLMPクライアント切断中にエラーが発生しました");
                throw;
            }
        }

        public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
        {
            return await _innerClient.IsAliveAsync(cancellationToken);
        }

        #endregion

        #region Enhanced Device Operations

        /// <summary>
        /// ワードデバイス読み取り（全機能統合版）
        /// </summary>
        public async Task<ushort[]> ReadWordDevicesAsync(
            DeviceCode deviceCode,
            uint startDevice,
            uint deviceCount,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 実際のSLMP通信を実行
                var result = await ((ISlmpClientFull)_innerClient).ReadWordDevicesAsync(
                    deviceCode, startDevice, (ushort)deviceCount, 0, cancellationToken);
                stopwatch.Stop();

                // 操作カウント更新
                Interlocked.Increment(ref _totalOperations);

                // パフォーマンス監視に記録
                _performanceMonitor?.RecordResponseTime("WordDeviceRead", deviceCode, startDevice,
                    deviceCount, stopwatch.Elapsed, true);

                // テストログ出力: RealMachineTestLoggerの機能はDeviceScanner+UnifiedLogWriterに統合済み
                // if (_testLogger != null)
                // {
                //     await _testLogger.LogWordDeviceReadSuccess(deviceCode, startDevice, result, stopwatch.Elapsed);
                // }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // パフォーマンス監視にエラー記録
                _performanceMonitor?.RecordResponseTime("WordDeviceRead", deviceCode, startDevice,
                    deviceCount, stopwatch.Elapsed, false);

                // エラーログ出力: RealMachineTestLoggerの機能はDeviceScanner+UnifiedLogWriterに統合済み
                // if (_testLogger != null)
                // {
                //     await _testLogger.LogCommunicationError("WordDeviceRead",
                //         $"{deviceCode}{startDevice}~{startDevice + deviceCount - 1}", ex, stopwatch.Elapsed);
                // }

                // ネットワーク品質監視にエラー記録
                if (ex is SlmpTimeoutException)
                {
                    _networkQualityMonitor?.RecordConnectionEvent(ConnectionEventType.Timeout, ex.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// ビットデバイス読み取り（全機能統合版）
        /// </summary>
        public async Task<bool[]> ReadBitDevicesAsync(
            DeviceCode deviceCode,
            uint startDevice,
            uint deviceCount,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await ((ISlmpClientFull)_innerClient).ReadBitDevicesAsync(
                    deviceCode, startDevice, (ushort)deviceCount, 0, cancellationToken);
                stopwatch.Stop();

                Interlocked.Increment(ref _totalOperations);

                _performanceMonitor?.RecordResponseTime("BitDeviceRead", deviceCode, startDevice,
                    deviceCount, stopwatch.Elapsed, true);

                // テストログ出力: RealMachineTestLoggerの機能はDeviceScanner+UnifiedLogWriterに統合済み
                // if (_testLogger != null)
                // {
                //     await _testLogger.LogBitDeviceReadSuccess(deviceCode, startDevice, result, stopwatch.Elapsed);
                // }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _performanceMonitor?.RecordResponseTime("BitDeviceRead", deviceCode, startDevice,
                    deviceCount, stopwatch.Elapsed, false);

                // エラーログ出力: RealMachineTestLoggerの機能はDeviceScanner+UnifiedLogWriterに統合済み
                // if (_testLogger != null)
                // {
                //     await _testLogger.LogCommunicationError("BitDeviceRead",
                //         $"{deviceCode}{startDevice}~{startDevice + deviceCount - 1}", ex, stopwatch.Elapsed);
                // }

                if (ex is SlmpTimeoutException)
                {
                    _networkQualityMonitor?.RecordConnectionEvent(ConnectionEventType.Timeout, ex.Message);
                }

                throw;
            }
        }

        /// <summary>
        /// 混合デバイス読み取り（全機能統合版）
        /// </summary>
        public async Task<(ushort[] wordData, bool[] bitData, uint[] dwordData)> ReadMixedDevicesAsync(
            (DeviceCode, uint)[] wordDevices,
            (DeviceCode, uint)[] bitDevices,
            (DeviceCode, uint)[] dwordDevices,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await ((ISlmpClientFull)_innerClient).ReadMixedDevicesAsync(
                    wordDevices?.ToList(), bitDevices?.ToList(), dwordDevices?.ToList(), 0, cancellationToken);
                stopwatch.Stop();

                Interlocked.Increment(ref _totalOperations);

                var totalDevices = (uint)((wordDevices?.Length ?? 0) + (bitDevices?.Length ?? 0) + (dwordDevices?.Length ?? 0));
                _performanceMonitor?.RecordResponseTime("MixedDeviceRead", DeviceCode.D, 0,
                    totalDevices, stopwatch.Elapsed, true);

                // テストログ出力: RealMachineTestLoggerの機能はDeviceScanner+UnifiedLogWriterに統合済み
                // if (_testLogger != null)
                // {
                //     await _testLogger.LogMixedDeviceReadSuccess(wordDevices, result.wordData,
                //         bitDevices, result.bitData, dwordDevices, result.dwordData, stopwatch.Elapsed);
                // }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var totalDevices = (uint)((wordDevices?.Length ?? 0) + (bitDevices?.Length ?? 0) + (dwordDevices?.Length ?? 0));
                _performanceMonitor?.RecordResponseTime("MixedDeviceRead", DeviceCode.D, 0,
                    totalDevices, stopwatch.Elapsed, false);

                // エラーログ出力: RealMachineTestLoggerの機能はDeviceScanner+UnifiedLogWriterに統合済み
                // if (_testLogger != null)
                // {
                //     await _testLogger.LogCommunicationError("MixedDeviceRead", "Mixed devices", ex, stopwatch.Elapsed);
                // }

                if (ex is SlmpTimeoutException)
                {
                    _networkQualityMonitor?.RecordConnectionEvent(ConnectionEventType.Timeout, ex.Message);
                }

                throw;
            }
        }

        #endregion

        #region Dashboard and Reporting

        /// <summary>
        /// リアルタイムダッシュボードを表示
        /// </summary>
        public void ShowDashboard()
        {
            _dashboard?.DisplayNow();
        }

        /// <summary>
        /// 詳細パフォーマンスレポートを表示
        /// </summary>
        public void ShowDetailedReport()
        {
            _dashboard?.DisplayDetailedReport();
        }

        /// <summary>
        /// ネットワーク品質レポートを表示
        /// </summary>
        public void ShowNetworkQualityReport()
        {
            _networkQualityMonitor?.DisplayDetailedQualityReport();
        }

        /// <summary>
        /// 全アラートをチェック
        /// </summary>
        public void CheckAllAlerts()
        {
            _performanceMonitor?.CheckPerformanceAlerts();
            _networkQualityMonitor?.CheckQualityAlerts();
            _dashboard?.DisplayAlerts();
        }

        /// <summary>
        /// 現在の統計サマリーを取得
        /// </summary>
        public object GetCurrentStatsSummary()
        {
            var perfStats = _performanceMonitor?.GetCurrentStatistics();
            var networkStats = _networkQualityMonitor?.GetQualityReport();

            return new
            {
                SessionDuration = DateTime.Now - _sessionStartTime,
                TotalOperations = _totalOperations,
                IsConnected = IsConnected,
                Performance = perfStats != null ? new
                {
                    AverageResponseTime = perfStats.AverageResponseTime,
                    OperationsPerSecond = perfStats.OperationsPerSecond
                } : null,
                NetworkQuality = networkStats != null ? new
                {
                    PacketLossRate = networkStats.PacketLossRate,
                    AveragePingTime = networkStats.AveragePingTime,
                    ConnectionStabilityScore = networkStats.ConnectionStabilityScore
                } : null
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _dashboard?.Dispose();
            _performanceMonitor?.Dispose();
            _networkQualityMonitor?.Dispose();
            _innerClient?.Dispose();

            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            _dashboard?.Dispose();
            _performanceMonitor?.Dispose();
            _networkQualityMonitor?.Dispose();

            if (_innerClient != null)
            {
                await _innerClient.DisposeAsync();
            }

            _disposed = true;
        }

        #endregion
    }
}