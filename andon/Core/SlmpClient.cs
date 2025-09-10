using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Transport;
using SlmpClient.Serialization;
using SlmpClient.Utils;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMP通信クライアント
    /// 4Phase_Implementation_Plan.md に基づく実装
    /// </summary>
    public class SlmpClient : ISlmpClientFull
    {
        #region Private Fields
        
        private readonly ILogger<SlmpClient> _logger;
        private readonly ISlmpTransport _transport;
        private readonly SlmpErrorStatistics _errorStatistics;
        private volatile bool _disposed = false;
        private volatile int _sequenceNumber = 0; // シーケンス番号（0-255で循環）
        private readonly object _connectionLock = new();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// 通信先アドレス
        /// </summary>
        public string Address { get; }
        
        /// <summary>
        /// SLMP通信対象設定
        /// </summary>
        public SlmpTarget Target { get; set; }
        
        /// <summary>
        /// 接続設定
        /// </summary>
        public SlmpConnectionSettings Settings { get; }
        
        /// <summary>
        /// 接続状態
        /// </summary>
        public bool IsConnected => _transport?.IsConnected == true;

        /// <summary>
        /// エラー統計情報
        /// </summary>
        public SlmpErrorStatistics ErrorStatistics => _errorStatistics;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// メインコンストラクタ
        /// </summary>
        /// <param name="address">通信先IPアドレスまたはホスト名</param>
        /// <param name="settings">接続設定（nullの場合はデフォルト設定）</param>
        /// <param name="logger">ロガー（nullの場合はNullLogger）</param>
        public SlmpClient(string address, SlmpConnectionSettings? settings = null, ILogger<SlmpClient>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));

            Address = address.Trim();
            Settings = settings ?? new SlmpConnectionSettings();
            Target = new SlmpTarget();
            _logger = logger ?? NullLogger<SlmpClient>.Instance;

            if (!Settings.IsValid())
                throw new ArgumentException("Invalid connection settings", nameof(settings));

            // Transport層を初期化
            _transport = CreateTransport();

            // エラー統計管理を初期化
            _errorStatistics = new SlmpErrorStatistics();

            _logger.LogDebug("SlmpClient created for address: {Address}", Address);
        }
        
        /// <summary>
        /// 簡易コンストラクタ（ポート番号指定）
        /// </summary>
        /// <param name="address">通信先IPアドレスまたはホスト名</param>
        /// <param name="port">ポート番号</param>
        /// <param name="logger">ロガー（nullの場合はNullLogger）</param>
        public SlmpClient(string address, int port = 5000, ILogger<SlmpClient>? logger = null)
            : this(address, new SlmpConnectionSettings { Port = port }, logger)
        {
        }
        
        #endregion
        
        #region Connection Management
        
        /// <summary>
        /// PLC接続を確立
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続成功時はtask</returns>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsConnected)
            {
                _logger.LogDebug("Already connected to {Address}:{Port}", Address, Settings.Port);
                return;
            }

            try
            {
                _logger.LogInformation("Connecting to {Address}:{Port}", Address, Settings.Port);
                
                await _transport.ConnectAsync(cancellationToken);
                
                _logger.LogInformation("Successfully connected to {Address}:{Port}", Address, Settings.Port);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Connection to {Address}:{Port} was cancelled", Address, Settings.Port);
                throw;
            }
            catch (SlmpConnectionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to {Address}:{Port}", Address, Settings.Port);
                throw new SlmpConnectionException("Connection failed", Address, Settings.Port, ex);
            }
        }

        /// <summary>
        /// PLC接続を切断
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>切断完了時のtask</returns>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                _logger.LogDebug("Already disconnected from {Address}:{Port}", Address, Settings.Port);
                return;
            }

            try
            {
                _logger.LogInformation("Disconnecting from {Address}:{Port}", Address, Settings.Port);

                await _transport.DisconnectAsync(cancellationToken);

                _logger.LogInformation("Successfully disconnected from {Address}:{Port}", Address, Settings.Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnection from {Address}:{Port}", Address, Settings.Port);
                // 切断時のエラーは例外をスローしない（ベストエフォート）
            }
        }

        /// <summary>
        /// 接続状態を確認
        /// </summary>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>接続中の場合はtrue</returns>
        public async Task<bool> IsAliveAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (!IsConnected)
                return false;

            try
            {
                return await _transport.IsAliveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking connection to {Address}:{Port}", Address, Settings.Port);
                return false;
            }
        }
        
        #endregion
        
        #region Device Read/Write Operations
        
        /// <summary>
        /// ビットデバイス読み取り
        /// Python: read_bit_devices(device_code, start_num, count, timeout=0)
        /// </summary>
        public async Task<bool[]> ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (count == 0)
                throw new ArgumentException("Count must be greater than 0", nameof(count));
            if (count > 7168)
                throw new ArgumentException("Count must not exceed 7168", nameof(count));
            
            _logger.LogDebug("Reading bit devices: {DeviceCode} from {StartAddress}, count: {Count}", 
                deviceCode, startAddress, count);
            
            // 操作開始を記録
            if (Settings.ContinuitySettings.EnableErrorStatistics)
            {
                _errorStatistics.RecordOperation();
            }
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildBitDeviceReadRequest(
                    deviceCode, startAddress, count, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // ビットデータを展開
                var result = SlmpBitConverter.UnpackBits(response.Data, count);
                
                _logger.LogDebug("Read {Count} bit devices successfully", count);
                return result;
            }
            catch (SlmpCommunicationException ex)
            {
                return HandleBitDeviceReadError(ex, deviceCode, startAddress, count, "SlmpCommunicationException");
            }
            catch (SlmpTimeoutException ex)
            {
                return HandleBitDeviceReadError(ex, deviceCode, startAddress, count, "SlmpTimeoutException");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading bit devices: {DeviceCode} from {StartAddress}, count: {Count}", 
                    deviceCode, startAddress, count);
                return HandleBitDeviceReadError(ex, deviceCode, startAddress, count, "UnexpectedException");
            }
        }
        
        /// <summary>
        /// ワードデバイス読み取り
        /// Python: read_word_devices(device_code, start_num, count, timeout=0)
        /// </summary>
        public async Task<ushort[]> ReadWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (count == 0)
                throw new ArgumentException("Count must be greater than 0", nameof(count));
            if (count > 960)
                throw new ArgumentException("Count must not exceed 960", nameof(count));
            
            _logger.LogDebug("Reading word devices: {DeviceCode} from {StartAddress}, count: {Count}",
                deviceCode, startAddress, count);
            
            // 操作開始を記録
            if (Settings.ContinuitySettings.EnableErrorStatistics)
            {
                _errorStatistics.RecordOperation();
            }
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildWordDeviceReadRequest(
                    deviceCode, startAddress, count, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // ワードデータを変換
                var result = DataProcessor.BytesToUshortArray(response.Data);
                
                _logger.LogDebug("Read {Count} word devices successfully", count);
                return result;
            }
            catch (SlmpCommunicationException ex)
            {
                return HandleWordDeviceReadError(ex, deviceCode, startAddress, count, "SlmpCommunicationException");
            }
            catch (SlmpTimeoutException ex)
            {
                return HandleWordDeviceReadError(ex, deviceCode, startAddress, count, "SlmpTimeoutException");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading word devices: {DeviceCode} from {StartAddress}, count: {Count}", 
                    deviceCode, startAddress, count);
                return HandleWordDeviceReadError(ex, deviceCode, startAddress, count, "UnexpectedException");
            }
        }
        
        /// <summary>
        /// ビットデバイス書き込み
        /// Python: write_bit_devices(dc2, start_num, data, timeout=0)
        /// </summary>
        public async Task WriteBitDevicesAsync(DeviceCode deviceCode, uint startAddress, bool[] data, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException("Data array cannot be empty", nameof(data));
            if (data.Length > 7168)
                throw new ArgumentException("Data array must not exceed 7168 elements", nameof(data));
            
            _logger.LogDebug("Writing bit devices: {DeviceCode} from {StartAddress}, count: {Count}",
                deviceCode, startAddress, data.Length);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildBitDeviceWriteRequest(
                    deviceCode, startAddress, data, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（書き込みは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Wrote {Count} bit devices successfully", data.Length);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing bit devices: {DeviceCode} from {StartAddress}, count: {Count}", 
                    deviceCode, startAddress, data.Length);
                throw new SlmpCommunicationException("Failed to write bit devices", ex);
            }
        }
        
        /// <summary>
        /// ワードデバイス書き込み
        /// Python: write_word_devices(dc2, start_num, data, timeout=0)
        /// </summary>
        public async Task WriteWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort[] data, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException("Data array cannot be empty", nameof(data));
            if (data.Length > 960)
                throw new ArgumentException("Data array must not exceed 960 elements", nameof(data));
            
            _logger.LogDebug("Writing word devices: {DeviceCode} from {StartAddress}, count: {Count}",
                deviceCode, startAddress, data.Length);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildWordDeviceWriteRequest(
                    deviceCode, startAddress, data, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（書き込みは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Wrote {Count} word devices successfully", data.Length);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing word devices: {DeviceCode} from {StartAddress}, count: {Count}", 
                    deviceCode, startAddress, data.Length);
                throw new SlmpCommunicationException("Failed to write word devices", ex);
            }
        }
        
        #endregion
        
        #region Random Access Operations (Phase2.2)
        
        /// <summary>
        /// ランダムデバイス読み取り
        /// Python: read_random_devices(word_devices, dword_devices, timeout=0)
        /// </summary>
        public async Task<(ushort[] wordData, uint[] dwordData)> ReadRandomDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (wordDevices == null)
                throw new ArgumentNullException(nameof(wordDevices));
            if (dwordDevices == null)
                throw new ArgumentNullException(nameof(dwordDevices));
            
            if (wordDevices.Count + dwordDevices.Count == 0)
                throw new ArgumentException("At least one device must be specified");
            if (wordDevices.Count + dwordDevices.Count > 192)
                throw new ArgumentException("Total device count must not exceed 192");
            
            _logger.LogDebug("Reading random devices: {WordCount} word devices, {DwordCount} dword devices", 
                wordDevices.Count, dwordDevices.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildRandomDeviceReadRequest(
                    wordDevices, dwordDevices, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // データを分離してワード配列とダブルワード配列に変換
                var wordData = new ushort[wordDevices.Count];
                var dwordData = new uint[dwordDevices.Count];
                
                int offset = 0;
                
                // ワードデータを抽出
                for (int i = 0; i < wordDevices.Count; i++)
                {
                    if (offset + 2 <= response.Data.Length)
                    {
                        wordData[i] = (ushort)(response.Data[offset] | (response.Data[offset + 1] << 8));
                        offset += 2;
                    }
                }
                
                // ダブルワードデータを抽出
                for (int i = 0; i < dwordDevices.Count; i++)
                {
                    if (offset + 4 <= response.Data.Length)
                    {
                        dwordData[i] = (uint)(response.Data[offset] | 
                                            (response.Data[offset + 1] << 8) | 
                                            (response.Data[offset + 2] << 16) | 
                                            (response.Data[offset + 3] << 24));
                        offset += 4;
                    }
                }
                
                _logger.LogDebug("Read {WordCount} word devices and {DwordCount} dword devices successfully", 
                    wordDevices.Count, dwordDevices.Count);
                return (wordData, dwordData);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading random devices");
                throw new SlmpCommunicationException("Failed to read random devices", ex);
            }
        }
        
        /// <summary>
        /// ランダムビットデバイス書き込み
        /// Python: write_random_bit_devices(devices, timeout=0)
        /// </summary>
        public async Task WriteRandomBitDevicesAsync(
            IList<(DeviceCode deviceCode, uint address, bool value)> devices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (devices == null)
                throw new ArgumentNullException(nameof(devices));
            if (devices.Count == 0)
                throw new ArgumentException("At least one device must be specified", nameof(devices));
            if (devices.Count > 192)
                throw new ArgumentException("Device count must not exceed 192", nameof(devices));
            
            _logger.LogDebug("Writing random bit devices: {Count} devices", devices.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildRandomBitDeviceWriteRequest(
                    devices, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（書き込みは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Wrote {Count} random bit devices successfully", devices.Count);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing random bit devices");
                throw new SlmpCommunicationException("Failed to write random bit devices", ex);
            }
        }
        
        /// <summary>
        /// ランダムワードデバイス書き込み
        /// Python: write_random_word_devices(word_devices, dword_devices, timeout=0)
        /// </summary>
        public async Task WriteRandomWordDevicesAsync(
            IList<(DeviceCode deviceCode, uint address, ushort value)> wordDevices,
            IList<(DeviceCode deviceCode, uint address, uint value)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (wordDevices == null)
                throw new ArgumentNullException(nameof(wordDevices));
            if (dwordDevices == null)
                throw new ArgumentNullException(nameof(dwordDevices));
            
            if (wordDevices.Count + dwordDevices.Count == 0)
                throw new ArgumentException("At least one device must be specified");
            if (wordDevices.Count + dwordDevices.Count > 192)
                throw new ArgumentException("Total device count must not exceed 192");
            
            _logger.LogDebug("Writing random word devices: {WordCount} word devices, {DwordCount} dword devices", 
                wordDevices.Count, dwordDevices.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildRandomWordDeviceWriteRequest(
                    wordDevices, dwordDevices, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（書き込みは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Wrote {WordCount} word devices and {DwordCount} dword devices successfully", 
                    wordDevices.Count, dwordDevices.Count);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing random word devices");
                throw new SlmpCommunicationException("Failed to write random word devices", ex);
            }
        }
        
        #endregion
        
        #region Block Operations (Phase2.3)
        
        /// <summary>
        /// ブロック読み取り
        /// Python: read_block(word_list, bit_list, timeout=0)
        /// </summary>
        public async Task<(ushort[][] wordBlocks, bool[][] bitBlocks)> ReadBlockAsync(
            IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (wordBlocks == null)
                throw new ArgumentNullException(nameof(wordBlocks));
            if (bitBlocks == null)
                throw new ArgumentNullException(nameof(bitBlocks));
                
            if (wordBlocks.Count + bitBlocks.Count == 0)
                throw new ArgumentException("At least one block must be specified");
            if (wordBlocks.Count + bitBlocks.Count > 120)
                throw new ArgumentException("Total block count must not exceed 120");
                
            // ワードブロック制限チェック (最大960ワード)
            foreach (var block in wordBlocks)
            {
                if (block.count > 960)
                    throw new ArgumentException($"Word block count must not exceed 960, got {block.count}");
            }
            
            // ビットブロック制限チェック (最大7168ビット)
            foreach (var block in bitBlocks)
            {
                if (block.count > 7168)
                    throw new ArgumentException($"Bit block count must not exceed 7168, got {block.count}");
            }
            
            _logger.LogDebug("Reading blocks: {WordBlockCount} word blocks, {BitBlockCount} bit blocks", 
                wordBlocks.Count, bitBlocks.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildBlockReadRequest(
                    wordBlocks, bitBlocks, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // ブロックデータを分離
                var wordBlockResults = new ushort[wordBlocks.Count][];
                var bitBlockResults = new bool[bitBlocks.Count][];
                
                int offset = 0;
                
                // ワードブロックデータを抽出
                for (int i = 0; i < wordBlocks.Count; i++)
                {
                    var blockCount = wordBlocks[i].count;
                    var blockSize = blockCount * 2; // ワード = 2バイト
                    
                    if (offset + blockSize <= response.Data.Length)
                    {
                        var blockData = new byte[blockSize];
                        Array.Copy(response.Data, offset, blockData, 0, blockSize);
                        wordBlockResults[i] = DataProcessor.BytesToUshortArray(blockData);
                        offset += blockSize;
                    }
                    else
                    {
                        throw new SlmpCommunicationException($"Insufficient data for word block {i}");
                    }
                }
                
                // ビットブロックデータを抽出
                for (int i = 0; i < bitBlocks.Count; i++)
                {
                    var blockCount = bitBlocks[i].count;
                    var blockSize = (blockCount + 7) / 8; // ビット→バイト変換（8ビット単位で切り上げ）
                    
                    if (offset + blockSize <= response.Data.Length)
                    {
                        var blockData = new byte[blockSize];
                        Array.Copy(response.Data, offset, blockData, 0, blockSize);
                        bitBlockResults[i] = SlmpBitConverter.UnpackBits(blockData, blockCount);
                        offset += blockSize;
                    }
                    else
                    {
                        throw new SlmpCommunicationException($"Insufficient data for bit block {i}");
                    }
                }
                
                _logger.LogDebug("Read {WordBlockCount} word blocks and {BitBlockCount} bit blocks successfully", 
                    wordBlocks.Count, bitBlocks.Count);
                return (wordBlockResults, bitBlockResults);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading blocks");
                throw new SlmpCommunicationException("Failed to read blocks", ex);
            }
        }
        
        /// <summary>
        /// ブロック書き込み
        /// Python: write_block(word_list, bit_list, timeout=0)
        /// </summary>
        public async Task WriteBlockAsync(
            IList<(DeviceCode deviceCode, uint address, ushort count, ushort[] data)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count, bool[] data)> bitBlocks,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (wordBlocks == null)
                throw new ArgumentNullException(nameof(wordBlocks));
            if (bitBlocks == null)
                throw new ArgumentNullException(nameof(bitBlocks));
                
            if (wordBlocks.Count + bitBlocks.Count == 0)
                throw new ArgumentException("At least one block must be specified");
            if (wordBlocks.Count + bitBlocks.Count > 120)
                throw new ArgumentException("Total block count must not exceed 120");
                
            // ワードブロック制限チェック
            foreach (var block in wordBlocks)
            {
                if (block.count > 960)
                    throw new ArgumentException($"Word block count must not exceed 960, got {block.count}");
                if (block.data == null)
                    throw new ArgumentNullException($"Word block data cannot be null");
                if (block.data.Length != block.count)
                    throw new ArgumentException($"Word block data length ({block.data.Length}) does not match count ({block.count})");
            }
            
            // ビットブロック制限チェック
            foreach (var block in bitBlocks)
            {
                if (block.count > 7168)
                    throw new ArgumentException($"Bit block count must not exceed 7168, got {block.count}");
                if (block.data == null)
                    throw new ArgumentNullException($"Bit block data cannot be null");
                if (block.data.Length != block.count)
                    throw new ArgumentException($"Bit block data length ({block.data.Length}) does not match count ({block.count})");
            }
            
            _logger.LogDebug("Writing blocks: {WordBlockCount} word blocks, {BitBlockCount} bit blocks", 
                wordBlocks.Count, bitBlocks.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildBlockWriteRequest(
                    wordBlocks, bitBlocks, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（書き込みは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Wrote {WordBlockCount} word blocks and {BitBlockCount} bit blocks successfully", 
                    wordBlocks.Count, bitBlocks.Count);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing blocks");
                throw new SlmpCommunicationException("Failed to write blocks", ex);
            }
        }
        
        #endregion
        
        #region Monitor Operations (Phase3.1)
        
        /// <summary>
        /// 登録デバイス情報保持用フィールド
        /// </summary>
        private IList<(DeviceCode deviceCode, uint address)>? _monitorWordDevices;
        private IList<(DeviceCode deviceCode, uint address)>? _monitorDwordDevices;
        private readonly object _monitorLock = new();
        
        /// <summary>
        /// モニタデバイス登録
        /// Python: entry_monitor_device(word_list, dword_list, timeout=0)
        /// </summary>
        public async Task EntryMonitorDeviceAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (wordDevices == null)
                throw new ArgumentNullException(nameof(wordDevices));
            if (dwordDevices == null)
                throw new ArgumentNullException(nameof(dwordDevices));
                
            if (wordDevices.Count + dwordDevices.Count == 0)
                throw new ArgumentException("At least one device must be specified");
            if (wordDevices.Count + dwordDevices.Count > 192)
                throw new ArgumentException("Total device count must not exceed 192");
            
            _logger.LogDebug("Registering monitor devices: {WordCount} word devices, {DwordCount} dword devices", 
                wordDevices.Count, dwordDevices.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildMonitorDeviceEntryRequest(
                    wordDevices, dwordDevices, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（登録は成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // 登録デバイス情報を内部状態として保持
                lock (_monitorLock)
                {
                    _monitorWordDevices = new List<(DeviceCode, uint)>(wordDevices);
                    _monitorDwordDevices = new List<(DeviceCode, uint)>(dwordDevices);
                }
                
                _logger.LogDebug("Registered {WordCount} word devices and {DwordCount} dword devices for monitoring successfully", 
                    wordDevices.Count, dwordDevices.Count);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering monitor devices");
                throw new SlmpCommunicationException("Failed to register monitor devices", ex);
            }
        }
        
        /// <summary>
        /// モニタ実行
        /// Python: execute_monitor(timeout=0)
        /// </summary>
        public async Task<(ushort[] wordData, uint[] dwordData)> ExecuteMonitorAsync(ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            // 登録デバイス数との整合性チェック
            IList<(DeviceCode, uint)> wordDevices;
            IList<(DeviceCode, uint)> dwordDevices;
            
            lock (_monitorLock)
            {
                if (_monitorWordDevices == null || _monitorDwordDevices == null)
                    throw new InvalidOperationException("Monitor devices must be registered first by calling EntryMonitorDeviceAsync");
                    
                wordDevices = _monitorWordDevices;
                dwordDevices = _monitorDwordDevices;
            }
            
            _logger.LogDebug("Executing monitor for {WordCount} word devices, {DwordCount} dword devices", 
                wordDevices.Count, dwordDevices.Count);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildMonitorExecuteRequest(
                    Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // データを分離してワード配列とダブルワード配列に変換
                var wordData = new ushort[wordDevices.Count];
                var dwordData = new uint[dwordDevices.Count];
                
                int offset = 0;
                
                // ワードデータを抽出
                for (int i = 0; i < wordDevices.Count; i++)
                {
                    if (offset + 2 <= response.Data.Length)
                    {
                        wordData[i] = (ushort)(response.Data[offset] | (response.Data[offset + 1] << 8));
                        offset += 2;
                    }
                    else
                    {
                        throw new SlmpCommunicationException($"Insufficient data for word device {i}");
                    }
                }
                
                // ダブルワードデータを抽出
                for (int i = 0; i < dwordDevices.Count; i++)
                {
                    if (offset + 4 <= response.Data.Length)
                    {
                        dwordData[i] = (uint)(response.Data[offset] | 
                                            (response.Data[offset + 1] << 8) | 
                                            (response.Data[offset + 2] << 16) | 
                                            (response.Data[offset + 3] << 24));
                        offset += 4;
                    }
                    else
                    {
                        throw new SlmpCommunicationException($"Insufficient data for dword device {i}");
                    }
                }
                
                _logger.LogDebug("Executed monitor for {WordCount} word devices and {DwordCount} dword devices successfully", 
                    wordDevices.Count, dwordDevices.Count);
                return (wordData, dwordData);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing monitor");
                throw new SlmpCommunicationException("Failed to execute monitor", ex);
            }
        }
        
        #endregion
        
        #region System Commands (Phase3.2)
        
        /// <summary>
        /// 型名読み取り
        /// Python: read_type_name(timeout=0)
        /// </summary>
        public async Task<(string typeName, Constants.TypeCode typeCode)> ReadTypeNameAsync(ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            _logger.LogDebug("Reading type name");
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildReadTypeNameRequest(
                    Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // 型名文字列を抽出（先頭16バイトが型名文字列）
                if (response.Data.Length < 16)
                    throw new SlmpCommunicationException("Insufficient type name data");
                    
                var typeNameBytes = new byte[16];
                Array.Copy(response.Data, 0, typeNameBytes, 0, 16);
                
                // 型名文字列を構築（NULL終端を考慮）
                var typeName = System.Text.Encoding.ASCII.GetString(typeNameBytes).TrimEnd('\0');
                
                // TypeCodeを型名文字列から特定
                var typeCode = GetTypeCodeFromTypeName(typeName);
                
                _logger.LogDebug("Read type name successfully: {TypeName} ({TypeCode})", typeName, typeCode);
                return (typeName, typeCode);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading type name");
                throw new SlmpCommunicationException("Failed to read type name", ex);
            }
        }
        
        /// <summary>
        /// セルフテスト
        /// Python: self_test(data=None, timeout=0)
        /// </summary>
        public async Task<bool> SelfTestAsync(string? data = null, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            // データ長制限チェック（960文字制限）
            if (!string.IsNullOrEmpty(data) && data.Length > 960)
                throw new ArgumentException("Test data length must not exceed 960 characters", nameof(data));
                
            // 16進文字列検証
            if (!string.IsNullOrEmpty(data))
            {
                foreach (char c in data)
                {
                    if (!Uri.IsHexDigit(c))
                        throw new ArgumentException("Test data must be a valid hex string", nameof(data));
                }
            }
            
            _logger.LogDebug("Executing self test with data length: {DataLength}", data?.Length ?? 0);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildSelfTestRequest(
                    data, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（セルフテストは成功すればtrue）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Self test completed successfully");
                return true;
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing self test");
                throw new SlmpCommunicationException("Failed to execute self test", ex);
            }
        }
        
        /// <summary>
        /// エラークリア
        /// Python: clear_error(timeout=0)
        /// </summary>
        public async Task ClearErrorAsync(ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            _logger.LogDebug("Clearing errors");
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildClearErrorRequest(
                    Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（エラークリアは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Cleared errors successfully");
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing errors");
                throw new SlmpCommunicationException("Failed to clear errors", ex);
            }
        }
        
        /// <summary>
        /// オンデマンドデータ確認
        /// Python: check_on_demand_data()
        /// </summary>
        public byte[]? CheckOnDemandData()
        {
            // オンデマンドデータの確認は受信キューの確認
            // 実装では非同期受信データの確認処理を行う
            // この実装では簡単な構造のみ提供
            if (_transport != null && _transport.IsConnected)
            {
                // 受信キューに未処理データがあるかチェック
                // 実際の実装では受信バッファから即座にデータを取得
                return null; // 未実装：実際には受信キューからデータを取得
            }
            return null;
        }
        
        /// <summary>
        /// 型名文字列からTypeCodeを特定
        /// </summary>
        /// <param name="typeName">型名文字列</param>
        /// <returns>対応するTypeCode</returns>
        private Constants.TypeCode GetTypeCodeFromTypeName(string typeName)
        {
            // 型名文字列とTypeCodeのマッピング
            // Python版との互換性を保つために主要な型名のみマッピング
            return typeName.ToUpper() switch
            {
                string s when s.Contains("Q00JCPU") => Constants.TypeCode.Q00JCPU,
                string s when s.Contains("Q00CPU") => Constants.TypeCode.Q00CPU,
                string s when s.Contains("Q01CPU") => Constants.TypeCode.Q01CPU,
                string s when s.Contains("Q02CPU") => Constants.TypeCode.Q02CPU,
                string s when s.Contains("Q06HCPU") => Constants.TypeCode.Q06HCPU,
                string s when s.Contains("Q12HCPU") => Constants.TypeCode.Q12HCPU,
                string s when s.Contains("Q25HCPU") => Constants.TypeCode.Q25HCPU,
                string s when s.Contains("R00CPU") => Constants.TypeCode.R00CPU,
                string s when s.Contains("R01CPU") => Constants.TypeCode.R01CPU,
                string s when s.Contains("R02CPU") => Constants.TypeCode.R02CPU,
                string s when s.Contains("L02SCPU") => Constants.TypeCode.L02SCPU,
                string s when s.Contains("L02CPU") => Constants.TypeCode.L02CPU,
                _ => Constants.TypeCode.Q00CPU // デフォルト値（不明な場合）
            };
        }
        
        #endregion
        
        #region Memory Access (Phase3.3)
        
        /// <summary>
        /// メモリ読み取り
        /// Python: memory_read(addr, length, timeout=0)
        /// </summary>
        public async Task<byte[]> MemoryReadAsync(uint address, ushort length, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (length == 0)
                throw new ArgumentException("Length must be greater than 0", nameof(length));
            if (length > 480)
                throw new ArgumentException("Length must not exceed 480 words", nameof(length));
                
            // ネットワーク/ノード制限チェック
            if (Target.Network != 0 || Target.Node != 0)
            {
                throw new ArgumentException("Memory read is only supported for local network (Network=0, Node=0)");
            }
            
            _logger.LogDebug("Reading memory: address=0x{Address:X8}, length={Length} words", address, length);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildMemoryReadRequest(
                    address, length, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                // 読み取ったデータを返却（ワード数 × 2バイト）
                var expectedDataSize = length * 2;
                if (response.Data.Length < expectedDataSize)
                    throw new SlmpCommunicationException($"Insufficient memory data: expected {expectedDataSize} bytes, got {response.Data.Length} bytes");
                
                var result = new byte[expectedDataSize];
                Array.Copy(response.Data, 0, result, 0, expectedDataSize);
                
                _logger.LogDebug("Read {Length} words ({ByteCount} bytes) from memory address 0x{Address:X8} successfully", 
                    length, result.Length, address);
                return result;
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading memory: address=0x{Address:X8}, length={Length}", address, length);
                throw new SlmpCommunicationException("Failed to read memory", ex);
            }
        }
        
        /// <summary>
        /// メモリ書き込み
        /// Python: memory_write(addr, data, timeout=0)
        /// </summary>
        public async Task MemoryWriteAsync(uint address, byte[] data, ushort timeout = 0, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                throw new ArgumentException("Data array cannot be empty", nameof(data));
            if (data.Length % 2 != 0)
                throw new ArgumentException("Data length must be even (word-aligned)", nameof(data));
                
            var wordCount = data.Length / 2;
            if (wordCount > 480)
                throw new ArgumentException("Data length must not exceed 480 words (960 bytes)", nameof(data));
                
            // ネットワーク/ノード制限チェック
            if (Target.Network != 0 || Target.Node != 0)
            {
                throw new ArgumentException("Memory write is only supported for local network (Network=0, Node=0)");
            }
            
            _logger.LogDebug("Writing memory: address=0x{Address:X8}, length={Length} words ({ByteCount} bytes)", 
                address, wordCount, data.Length);
            
            try
            {
                // 要求フレームを構築
                var sequence = GetNextSequence();
                var requestFrame = SlmpRequestBuilder.BuildMemoryWriteRequest(
                    address, data, Settings, Target, sequence, timeout);
                
                // 通信実行
                var timeoutMs = TimeSpan.FromMilliseconds(timeout > 0 ? timeout * 250 : Settings.ReceiveTimeout.TotalMilliseconds);
                var responseFrame = await _transport.SendAndReceiveAsync(requestFrame, timeoutMs, cancellationToken);
                
                // レスポンス解析（書き込みは成功確認のみ）
                var response = SlmpResponseParser.ParseResponse(responseFrame, Settings.IsBinary, Settings.Version);
                
                _logger.LogDebug("Wrote {Length} words ({ByteCount} bytes) to memory address 0x{Address:X8} successfully", 
                    wordCount, data.Length, address);
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (SlmpTimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing memory: address=0x{Address:X8}, length={Length} words", address, wordCount);
                throw new SlmpCommunicationException("Failed to write memory", ex);
            }
        }
        
        #endregion
        
        #region Phase 4: Mixed Device Reading API with Pseudo-Dword Integration
        
        /// <summary>
        /// 混合デバイス読み取り（Phase 4: 擬似ダブルワード統合）
        /// DWordデバイスを内部的にWordペアに分割してSLMP制限内で読み取り、結果を結合
        /// </summary>
        /// <param name="wordDevices">読み取り対象のWordデバイス群</param>
        /// <param name="bitDevices">読み取り対象のBitデバイス群</param>
        /// <param name="dwordDevices">読み取り対象のDWordデバイス群（内部分割される）</param>
        /// <param name="timeout">タイムアウト値（250ms単位）</param>
        /// <param name="cancellationToken">キャンセレーショントークン</param>
        /// <returns>読み取り結果（Word配列、Bit配列、DWord配列）</returns>
        /// <exception cref="ArgumentException">制限値超過時</exception>
        /// <exception cref="SlmpCommunicationException">通信エラー時</exception>
        public async Task<(ushort[] wordData, bool[] bitData, uint[] dwordData)> ReadMixedDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfNotConnected();
            
            // 入力パラメータの検証
            if (wordDevices == null)
                throw new ArgumentNullException(nameof(wordDevices));
            if (bitDevices == null)
                throw new ArgumentNullException(nameof(bitDevices));
            if (dwordDevices == null)
                throw new ArgumentNullException(nameof(dwordDevices));
            
            // Phase 4: SLMP制限との整合性検証
            ValidateMixedDeviceConstraints(wordDevices, bitDevices, dwordDevices);
            
            _logger.LogDebug("Reading mixed devices: {WordCount} words, {BitCount} bits, {DwordCount} dwords", 
                wordDevices.Count, bitDevices.Count, dwordDevices.Count);
            
            try
            {
                // Phase 4: 擬似ダブルワード分割処理
                var pseudoDwordSplitter = new PseudoDwordSplitter(
                    addressValidator: new DeviceAddressValidator(),
                    dwordConverter: new DwordConverter(),
                    options: new ConversionOptions { EnableValidation = true, EnableStatistics = true },
                    continuitySettings: Settings.ContinuitySettings,
                    errorStatistics: _errorStatistics
                );
                
                // DWordデバイスをWordペアに分割
                var dwordDevicesWithValues = dwordDevices
                    .Select(d => (d.deviceCode, d.address, (uint)0)) // 読み取り前なので値は0
                    .ToList();
                
                var wordPairs = pseudoDwordSplitter.SplitDwordToWordPairs(dwordDevicesWithValues);
                
                // 分割されたWordペアを個別のWordデバイスとして展開
                var expandedWordDevices = new List<(DeviceCode deviceCode, uint address)>(wordDevices);
                foreach (var wordPair in wordPairs)
                {
                    expandedWordDevices.Add((wordPair.LowWord.deviceCode, wordPair.LowWord.address));
                    expandedWordDevices.Add((wordPair.HighWord.deviceCode, wordPair.HighWord.address));
                }
                
                // 同時読み取り処理
                Task<ushort[]> wordTask = expandedWordDevices.Count > 0 
                    ? ReadMultipleWordDevicesAsync(expandedWordDevices, timeout, cancellationToken)
                    : Task.FromResult(new ushort[0]);
                
                Task<bool[]> bitTask = bitDevices.Count > 0 
                    ? ReadMultipleBitDevicesAsync(bitDevices, timeout, cancellationToken)
                    : Task.FromResult(new bool[0]);
                
                // 並列実行
                await Task.WhenAll(wordTask, bitTask);
                
                var allWordData = await wordTask;
                var bitData = await bitTask;
                
                // 結果データを分離
                var originalWordData = new ushort[wordDevices.Count];
                var dwordWordData = new List<ushort>();
                
                // 元のWordデバイス結果を取得
                for (int i = 0; i < wordDevices.Count; i++)
                {
                    originalWordData[i] = allWordData[i];
                }
                
                // DWord用のWordデータを取得
                for (int i = wordDevices.Count; i < allWordData.Length; i++)
                {
                    dwordWordData.Add(allWordData[i]);
                }
                
                // WordペアをDWordに結合
                var dwordData = await CombineWordPairsToDwords(wordPairs, dwordWordData, pseudoDwordSplitter);
                
                _logger.LogDebug("Successfully read mixed devices: {WordCount} words, {BitCount} bits, {DwordCount} dwords", 
                    originalWordData.Length, bitData.Length, dwordData.Length);
                
                return (originalWordData, bitData, dwordData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading mixed devices");
                
                // Phase 4: エラーハンドリング統合
                if (Settings.ContinuitySettings.Mode != ErrorHandlingMode.ThrowException)
                {
                    return HandleMixedDeviceReadError(wordDevices, bitDevices, dwordDevices, ex);
                }
                
                throw new SlmpCommunicationException("Failed to read mixed devices", ex);
            }
        }
        
        /// <summary>
        /// Phase 4: SLMP制限値検証
        /// </summary>
        private void ValidateMixedDeviceConstraints(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices)
        {
            // DWordデバイス数制限: 最大480個（960÷2）
            if (dwordDevices.Count > 480)
                throw new ArgumentException($"DWord device count must not exceed 480, got {dwordDevices.Count}");
            
            // 総Word数制限（元のWord + DWordから展開されるWord）
            var totalWordCount = wordDevices.Count + (dwordDevices.Count * 2);
            if (totalWordCount > 960)
                throw new ArgumentException($"Total word count (including expanded dwords) must not exceed 960, got {totalWordCount}");
            
            // Bitデバイス制限
            if (bitDevices.Count > 7168)
                throw new ArgumentException($"Bit device count must not exceed 7168, got {bitDevices.Count}");
            
            // 総デバイス数制限（ランダムアクセス制限）
            var totalDeviceCount = wordDevices.Count + bitDevices.Count + dwordDevices.Count;
            if (totalDeviceCount > 192)
                throw new ArgumentException($"Total device count must not exceed 192, got {totalDeviceCount}");
            
            // 各DWordデバイスのアドレス境界検証
            var addressValidator = new DeviceAddressValidator();
            foreach (var device in dwordDevices)
            {
                try
                {
                    addressValidator.ValidateAddressBoundary(device.deviceCode, device.address);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"DWord device validation failed: {ex.Message}", ex);
                }
            }
        }
        
        /// <summary>
        /// 複数WordデバイスをバッチREAD
        /// </summary>
        private async Task<ushort[]> ReadMultipleWordDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            ushort timeout,
            CancellationToken cancellationToken)
        {
            // 効率化のため、連続アドレスは一括読み取り、そうでなければランダムアクセス
            if (CanUseSequentialRead(wordDevices))
            {
                return await ReadSequentialWordDevicesAsync(wordDevices, timeout, cancellationToken);
            }
            else
            {
                return await ReadRandomWordDevicesAsync(wordDevices, timeout, cancellationToken);
            }
        }
        
        /// <summary>
        /// 複数BitデバイスをバッチREAD
        /// </summary>
        private async Task<bool[]> ReadMultipleBitDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            ushort timeout,
            CancellationToken cancellationToken)
        {
            // 効率化のため、連続アドレスは一括読み取り、そうでなければ個別読み取り
            if (CanUseSequentialRead(bitDevices))
            {
                return await ReadSequentialBitDevicesAsync(bitDevices, timeout, cancellationToken);
            }
            else
            {
                return await ReadIndividualBitDevicesAsync(bitDevices, timeout, cancellationToken);
            }
        }
        
        /// <summary>
        /// 連続アドレス読み取りが可能かチェック
        /// </summary>
        private bool CanUseSequentialRead(IList<(DeviceCode deviceCode, uint address)> devices)
        {
            if (devices.Count <= 1) return true;
            
            var sortedDevices = devices.OrderBy(d => d.deviceCode).ThenBy(d => d.address).ToList();
            
            for (int i = 1; i < sortedDevices.Count; i++)
            {
                if (sortedDevices[i].deviceCode != sortedDevices[i-1].deviceCode ||
                    sortedDevices[i].address != sortedDevices[i-1].address + 1)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 連続Wordデバイス読み取り
        /// </summary>
        private async Task<ushort[]> ReadSequentialWordDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            ushort timeout,
            CancellationToken cancellationToken)
        {
            if (wordDevices.Count == 0) return new ushort[0];
            
            var sorted = wordDevices.OrderBy(d => d.deviceCode).ThenBy(d => d.address).ToList();
            var firstDevice = sorted[0];
            
            return await ReadWordDevicesAsync(firstDevice.deviceCode, firstDevice.address, 
                (ushort)wordDevices.Count, timeout, cancellationToken);
        }
        
        /// <summary>
        /// ランダムWordデバイス読み取り
        /// </summary>
        private async Task<ushort[]> ReadRandomWordDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            ushort timeout,
            CancellationToken cancellationToken)
        {
            var (wordData, _) = await ReadRandomDevicesAsync(wordDevices, new List<(DeviceCode, uint)>(), timeout, cancellationToken);
            return wordData;
        }
        
        /// <summary>
        /// 連続Bitデバイス読み取り
        /// </summary>
        private async Task<bool[]> ReadSequentialBitDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            ushort timeout,
            CancellationToken cancellationToken)
        {
            if (bitDevices.Count == 0) return new bool[0];
            
            var sorted = bitDevices.OrderBy(d => d.deviceCode).ThenBy(d => d.address).ToList();
            var firstDevice = sorted[0];
            
            return await ReadBitDevicesAsync(firstDevice.deviceCode, firstDevice.address, 
                (ushort)bitDevices.Count, timeout, cancellationToken);
        }
        
        /// <summary>
        /// 個別Bitデバイス読み取り
        /// </summary>
        private async Task<bool[]> ReadIndividualBitDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            ushort timeout,
            CancellationToken cancellationToken)
        {
            var results = new bool[bitDevices.Count];
            var tasks = new Task[bitDevices.Count];
            
            for (int i = 0; i < bitDevices.Count; i++)
            {
                var index = i;
                var device = bitDevices[i];
                tasks[i] = Task.Run(async () =>
                {
                    var result = await ReadBitDevicesAsync(device.deviceCode, device.address, 1, timeout, cancellationToken);
                    results[index] = result[0];
                });
            }
            
            await Task.WhenAll(tasks);
            return results;
        }
        
        /// <summary>
        /// WordペアをDWordに結合
        /// </summary>
        private async Task<uint[]> CombineWordPairsToDwords(
            IList<WordPair> wordPairs,
            IList<ushort> wordData,
            PseudoDwordSplitter splitter)
        {
            return await Task.Run(() =>
            {
                var updatedWordPairs = new List<WordPair>();
                
                for (int i = 0; i < wordPairs.Count; i++)
                {
                    var pair = wordPairs[i];
                    var lowIndex = i * 2;
                    var highIndex = i * 2 + 1;
                    
                    if (lowIndex < wordData.Count && highIndex < wordData.Count)
                    {
                        var updatedPair = new WordPair
                        {
                            LowWord = (pair.LowWord.deviceCode, pair.LowWord.address, wordData[lowIndex]),
                            HighWord = (pair.HighWord.deviceCode, pair.HighWord.address, wordData[highIndex])
                        };
                        updatedWordPairs.Add(updatedPair);
                    }
                }
                
                var dwordResults = splitter.CombineWordPairsToDword(updatedWordPairs);
                return dwordResults.Select(d => d.value).ToArray();
            });
        }
        
        /// <summary>
        /// Phase 4: 混合デバイス読み取りエラーハンドリング
        /// </summary>
        private (ushort[] wordData, bool[] bitData, uint[] dwordData) HandleMixedDeviceReadError(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            Exception exception)
        {
            var continuitySettings = Settings.ContinuitySettings;
            
            // エラー統計記録
            if (continuitySettings.EnableErrorStatistics)
            {
                _errorStatistics.RecordError("MixedDeviceReadError", "Mixed", 0, exception, continuitySettings);
            }
            
            // デフォルト値配列を作成
            var defaultWordData = CreateDefaultWordArray((ushort)wordDevices.Count, continuitySettings.DefaultWordValue);
            var defaultBitData = CreateDefaultBitArray((ushort)bitDevices.Count, continuitySettings.DefaultBitValue);
            var defaultDwordData = new uint[dwordDevices.Count];
            
            // DWordのデフォルト値を設定
            var defaultDwordValue = (uint)(continuitySettings.DefaultWordValue | (continuitySettings.DefaultWordValue << 16));
            for (int i = 0; i < defaultDwordData.Length; i++)
            {
                defaultDwordData[i] = defaultDwordValue;
            }
            
            // 継続動作を記録
            if (continuitySettings.EnableErrorStatistics)
            {
                _errorStatistics.RecordContinuedOperation("MixedDeviceReadError", "Mixed", 0, 
                    $"Returned defaults: {wordDevices.Count} words, {bitDevices.Count} bits, {dwordDevices.Count} dwords");
            }
            
            return (defaultWordData, defaultBitData, defaultDwordData);
        }
        
        #endregion
        
        #region Synchronous Methods (Python compatibility)
        
        /// <summary>
        /// 同期版ビットデバイス読み取り（Python互換）
        /// </summary>
        public bool[] ReadBitDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0)
        {
            return ReadBitDevicesAsync(deviceCode, startAddress, count, timeout, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        
        /// <summary>
        /// 同期版ワードデバイス読み取り（Python互換）
        /// </summary>
        public ushort[] ReadWordDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0)
        {
            return ReadWordDevicesAsync(deviceCode, startAddress, count, timeout, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        
        /// <summary>
        /// 同期版ビットデバイス書き込み（Python互換）
        /// </summary>
        public void WriteBitDevices(DeviceCode deviceCode, uint startAddress, bool[] data, ushort timeout = 0)
        {
            WriteBitDevicesAsync(deviceCode, startAddress, data, timeout, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        
        /// <summary>
        /// 同期版ワードデバイス書き込み（Python互換）
        /// </summary>
        public void WriteWordDevices(DeviceCode deviceCode, uint startAddress, ushort[] data, ushort timeout = 0)
        {
            WriteWordDevicesAsync(deviceCode, startAddress, data, timeout, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// リソースを解放（同期版）
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを解放（非同期版）
        /// </summary>
        /// <returns>解放完了時のtask</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソース解放の実装（同期版）
        /// </summary>
        /// <param name="disposing">Disposeメソッドから呼ばれた場合はtrue</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    // 同期的な切断（タイムアウト付き）
                    DisconnectAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during synchronous disposal");
                }
                
                // Transportリソースを解放
                try
                {
                    _transport?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing transport");
                }
            }

            _disposed = true;
        }

        /// <summary>
        /// リソース解放の実装（非同期版）
        /// </summary>
        /// <returns>解放完了時のtask</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_disposed)
                return;

            try
            {
                await DisconnectAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during asynchronous disposal");
            }
            
            // Transportリソースを非同期解放
            try
            {
                if (_transport != null)
                    await _transport.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing transport asynchronously");
            }

            _disposed = true;
        }
        
        #endregion
        
        #region Transport Creation
        
        /// <summary>
        /// 設定に基づいてTransport層を作成
        /// </summary>
        /// <returns>作成されたTransport</returns>
        private ISlmpTransport CreateTransport()
        {
            if (Settings.UseTcp)
            {
                var tcpTransport = new SlmpTcpTransport(Address, Settings.Port, _logger as ILogger<SlmpTcpTransport>);
                tcpTransport.ConnectTimeout = Settings.ConnectTimeout;
                tcpTransport.ReceiveTimeout = Settings.ReceiveTimeout;
                tcpTransport.SendTimeout = Settings.ReceiveTimeout; // 送信タイムアウトも同じ値を使用
                return tcpTransport;
            }
            else
            {
                var udpTransport = new SlmpUdpTransport(Address, Settings.Port, _logger as ILogger<SlmpUdpTransport>);
                udpTransport.ReceiveTimeout = Settings.ReceiveTimeout;
                udpTransport.SendTimeout = Settings.ReceiveTimeout; // 送信タイムアウトも同じ値を使用
                return udpTransport;
            }
        }
        
        /// <summary>
        /// 次のシーケンス番号を取得（0-255で循環）
        /// </summary>
        /// <returns>シーケンス番号</returns>
        private byte GetNextSequence()
        {
            return (byte)(Interlocked.Increment(ref _sequenceNumber) & 0xFF);
        }
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// オブジェクトが破棄済みかチェックし、破棄済みの場合は例外をスロー
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SlmpClient));
        }

        /// <summary>
        /// 接続が確立されているかチェックし、未接続の場合は例外をスロー
        /// </summary>
        protected void ThrowIfNotConnected()
        {
            ThrowIfDisposed();
            if (!IsConnected)
                throw new SlmpConnectionException("Not connected to PLC", Address, Settings.Port);
        }

        /// <summary>
        /// ビットデバイス読み取りエラーを処理（稼働第一の継続機能）
        /// </summary>
        /// <param name="exception">発生した例外</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">読み取り個数</param>
        /// <param name="errorType">エラー種別</param>
        /// <returns>デフォルト値またはリトライ結果</returns>
        private bool[] HandleBitDeviceReadError(Exception exception, DeviceCode deviceCode, uint startAddress, ushort count, string errorType)
        {
            var continuitySettings = Settings.ContinuitySettings;
            
            // エラー統計を記録
            if (continuitySettings.EnableErrorStatistics)
            {
                var shouldNotify = _errorStatistics.RecordError(errorType, deviceCode.ToString(), startAddress, exception, continuitySettings);
                
                if (shouldNotify)
                {
                    _logger.LogWarning("UDP通信エラーが発生しました - システム継続中: {ErrorType} {DeviceCode}:{StartAddress} (個数: {Count}) - {Message}",
                        errorType, deviceCode, startAddress, count, exception.Message);
                }
            }
            
            // 動作モードに応じた処理
            switch (continuitySettings.Mode)
            {
                case ErrorHandlingMode.ThrowException:
                    // 従来動作：例外をスロー
                    if (exception is SlmpException slmpEx)
                        throw slmpEx;
                    throw new SlmpCommunicationException("Failed to read bit devices", exception);
                
                case ErrorHandlingMode.ReturnDefaultAndContinue:
                    // デフォルト値を返却してシステム継続
                    var defaultResult = CreateDefaultBitArray(count, continuitySettings.DefaultBitValue);
                    RecordContinuedOperation(errorType, deviceCode, startAddress, $"bool[{count}] (all {continuitySettings.DefaultBitValue})");
                    return defaultResult;
                
                case ErrorHandlingMode.RetryThenDefault:
                    // リトライ後にデフォルト値返却
                    // TODO: リトライ機能の実装（現在は即座にデフォルト値返却）
                    var retryDefaultResult = CreateDefaultBitArray(count, continuitySettings.DefaultBitValue);
                    RecordContinuedOperation(errorType, deviceCode, startAddress, $"bool[{count}] (all {continuitySettings.DefaultBitValue}) after retry");
                    return retryDefaultResult;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(continuitySettings.Mode), continuitySettings.Mode, "Unknown error handling mode");
            }
        }

        /// <summary>
        /// ワードデバイス読み取りエラーを処理（稼働第一の継続機能）
        /// </summary>
        /// <param name="exception">発生した例外</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">読み取り個数</param>
        /// <param name="errorType">エラー種別</param>
        /// <returns>デフォルト値またはリトライ結果</returns>
        private ushort[] HandleWordDeviceReadError(Exception exception, DeviceCode deviceCode, uint startAddress, ushort count, string errorType)
        {
            var continuitySettings = Settings.ContinuitySettings;
            
            // エラー統計を記録
            if (continuitySettings.EnableErrorStatistics)
            {
                var shouldNotify = _errorStatistics.RecordError(errorType, deviceCode.ToString(), startAddress, exception, continuitySettings);
                
                if (shouldNotify)
                {
                    _logger.LogWarning("UDP通信エラーが発生しました - システム継続中: {ErrorType} {DeviceCode}:{StartAddress} (個数: {Count}) - {Message}",
                        errorType, deviceCode, startAddress, count, exception.Message);
                }
            }
            
            // 動作モードに応じた処理
            switch (continuitySettings.Mode)
            {
                case ErrorHandlingMode.ThrowException:
                    // 従来動作：例外をスロー
                    if (exception is SlmpException slmpEx)
                        throw slmpEx;
                    throw new SlmpCommunicationException("Failed to read word devices", exception);
                
                case ErrorHandlingMode.ReturnDefaultAndContinue:
                    // デフォルト値を返却してシステム継続
                    var defaultResult = CreateDefaultWordArray(count, continuitySettings.DefaultWordValue);
                    RecordContinuedOperation(errorType, deviceCode, startAddress, $"ushort[{count}] (all {continuitySettings.DefaultWordValue})");
                    return defaultResult;
                
                case ErrorHandlingMode.RetryThenDefault:
                    // リトライ後にデフォルト値返却
                    // TODO: リトライ機能の実装（現在は即座にデフォルト値返却）
                    var retryDefaultResult = CreateDefaultWordArray(count, continuitySettings.DefaultWordValue);
                    RecordContinuedOperation(errorType, deviceCode, startAddress, $"ushort[{count}] (all {continuitySettings.DefaultWordValue}) after retry");
                    return retryDefaultResult;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(continuitySettings.Mode), continuitySettings.Mode, "Unknown error handling mode");
            }
        }

        /// <summary>
        /// デフォルトビット配列を作成
        /// </summary>
        /// <param name="count">配列サイズ</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>デフォルト値で初期化された配列</returns>
        private static bool[] CreateDefaultBitArray(ushort count, bool defaultValue)
        {
            var result = new bool[count];
            if (defaultValue)
            {
                for (int i = 0; i < count; i++)
                {
                    result[i] = true;
                }
            }
            return result;
        }

        /// <summary>
        /// デフォルトワード配列を作成
        /// </summary>
        /// <param name="count">配列サイズ</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>デフォルト値で初期化された配列</returns>
        private static ushort[] CreateDefaultWordArray(ushort count, ushort defaultValue)
        {
            var result = new ushort[count];
            if (defaultValue != 0)
            {
                for (int i = 0; i < count; i++)
                {
                    result[i] = defaultValue;
                }
            }
            return result;
        }

        /// <summary>
        /// 継続動作を記録
        /// </summary>
        /// <param name="errorType">エラー種別</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="defaultValueUsed">使用されたデフォルト値の説明</param>
        private void RecordContinuedOperation(string errorType, DeviceCode deviceCode, uint startAddress, string defaultValueUsed)
        {
            if (Settings.ContinuitySettings.EnableErrorStatistics)
            {
                _errorStatistics.RecordContinuedOperation(errorType, deviceCode.ToString(), startAddress, defaultValueUsed);
            }
        }

        /// <summary>
        /// SlmpClientの文字列表現を取得
        /// </summary>
        /// <returns>接続情報を含む文字列</returns>
        public override string ToString()
        {
            var status = IsConnected ? "Connected" : "Disconnected";
            return $"SlmpClient({Address}:{Settings.Port}, {status}, {Target})";
        }

        /// <summary>
        /// ファイナライザー
        /// </summary>
        ~SlmpClient()
        {
            Dispose(false);
        }
        
        #endregion
    }
}