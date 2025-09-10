using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMP通信クライアントの基本インターフェース
    /// 4Phase_Implementation_Plan.md に基づく設計
    /// </summary>
    public interface ISlmpClient : IDisposable, IAsyncDisposable
    {
        #region Properties
        
        /// <summary>
        /// 通信先アドレス
        /// </summary>
        string Address { get; }
        
        /// <summary>
        /// 通信対象設定
        /// </summary>
        SlmpTarget Target { get; set; }
        
        /// <summary>
        /// 接続設定
        /// </summary>
        SlmpConnectionSettings Settings { get; }
        
        /// <summary>
        /// 接続状態
        /// </summary>
        bool IsConnected { get; }
        
        #endregion
        
        #region Connection Management
        
        /// <summary>
        /// PLC接続を確立
        /// </summary>
        Task ConnectAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// PLC接続を切断
        /// </summary>
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 接続状態を確認
        /// </summary>
        Task<bool> IsAliveAsync(CancellationToken cancellationToken = default);
        
        #endregion
    }

    /// <summary>
    /// SLMP通信の全機能を提供するインターフェース
    /// Python版PySLMPClientとの互換性を重視
    /// </summary>
    public interface ISlmpClientFull : ISlmpClient
    {
        #region Device Read/Write Operations
        
        /// <summary>
        /// ビットデバイス読み取り
        /// Python: read_bit_devices(device_code, start_num, count, timeout=0)
        /// </summary>
        Task<bool[]> ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// ワードデバイス読み取り
        /// Python: read_word_devices(device_code, start_num, count, timeout=0)
        /// </summary>
        Task<ushort[]> ReadWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// ビットデバイス書き込み
        /// Python: write_bit_devices(dc2, start_num, data, timeout=0)
        /// </summary>
        Task WriteBitDevicesAsync(DeviceCode deviceCode, uint startAddress, bool[] data, ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// ワードデバイス書き込み
        /// Python: write_word_devices(dc2, start_num, data, timeout=0)
        /// </summary>
        Task WriteWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort[] data, ushort timeout = 0, CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Random Access Operations
        
        /// <summary>
        /// ランダムデバイス読み取り
        /// Python: read_random_devices(word_list, dword_list, timeout=0)
        /// </summary>
        Task<(ushort[] wordData, uint[] dwordData)> ReadRandomDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// ランダムビットデバイス書き込み
        /// Python: write_random_bit_devices(device_list, timeout=0)
        /// </summary>
        Task WriteRandomBitDevicesAsync(
            IList<(DeviceCode deviceCode, uint address, bool value)> devices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// ランダムワードデバイス書き込み
        /// Python: write_random_word_devices(word_list, dword_list, timeout=0)
        /// </summary>
        Task WriteRandomWordDevicesAsync(
            IList<(DeviceCode deviceCode, uint address, ushort value)> wordDevices,
            IList<(DeviceCode deviceCode, uint address, uint value)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Block Operations
        
        /// <summary>
        /// ブロック読み取り
        /// Python: read_block(word_list, bit_list, timeout=0)
        /// </summary>
        Task<(ushort[][] wordBlocks, bool[][] bitBlocks)> ReadBlockAsync(
            IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// ブロック書き込み
        /// Python: write_block(word_list, bit_list, timeout=0)
        /// </summary>
        Task WriteBlockAsync(
            IList<(DeviceCode deviceCode, uint address, ushort count, ushort[] data)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count, bool[] data)> bitBlocks,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Monitor Operations
        
        /// <summary>
        /// モニタデバイス登録
        /// Python: entry_monitor_device(word_list, dword_list, timeout=0)
        /// </summary>
        Task EntryMonitorDeviceAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// モニタ実行
        /// Python: execute_monitor(timeout=0)
        /// </summary>
        Task<(ushort[] wordData, uint[] dwordData)> ExecuteMonitorAsync(ushort timeout = 0, CancellationToken cancellationToken = default);
        
        #endregion
        
        #region System Commands
        
        /// <summary>
        /// 型名読み取り
        /// Python: read_type_name(timeout=0)
        /// </summary>
        Task<(string typeName, Constants.TypeCode typeCode)> ReadTypeNameAsync(ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// セルフテスト
        /// Python: self_test(data=None, timeout=0)
        /// </summary>
        Task<bool> SelfTestAsync(string? data = null, ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// エラークリア
        /// Python: clear_error(timeout=0)
        /// </summary>
        Task ClearErrorAsync(ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// オンデマンドデータ確認
        /// Python: check_on_demand_data()
        /// </summary>
        byte[]? CheckOnDemandData();
        
        #endregion
        
        #region Memory Access
        
        /// <summary>
        /// メモリ読み取り
        /// Python: memory_read(addr, length, timeout=0)
        /// </summary>
        Task<byte[]> MemoryReadAsync(uint address, ushort length, ushort timeout = 0, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// メモリ書き込み
        /// Python: memory_write(addr, data, timeout=0)
        /// </summary>
        Task MemoryWriteAsync(uint address, byte[] data, ushort timeout = 0, CancellationToken cancellationToken = default);
        
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
        Task<(ushort[] wordData, bool[] bitData, uint[] dwordData)> ReadMixedDevicesAsync(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> bitDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            ushort timeout = 0,
            CancellationToken cancellationToken = default);
        
        #endregion
        
        #region Synchronous Methods (Python compatibility)
        
        /// <summary>
        /// 同期版ビットデバイス読み取り（Python互換）
        /// </summary>
        bool[] ReadBitDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0);
        
        /// <summary>
        /// 同期版ワードデバイス読み取り（Python互換）
        /// </summary>
        ushort[] ReadWordDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0);
        
        /// <summary>
        /// 同期版ビットデバイス書き込み（Python互換）
        /// </summary>
        void WriteBitDevices(DeviceCode deviceCode, uint startAddress, bool[] data, ushort timeout = 0);
        
        /// <summary>
        /// 同期版ワードデバイス書き込み（Python互換）
        /// </summary>
        void WriteWordDevices(DeviceCode deviceCode, uint startAddress, ushort[] data, ushort timeout = 0);
        
        #endregion
    }
}