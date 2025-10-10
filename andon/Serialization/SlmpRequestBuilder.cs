using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Utils;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// SLMP要求フレーム構築クラス
    /// </summary>
    public static class SlmpRequestBuilder
    {
        /// <summary>
        /// ビットデバイス読み取り要求を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">読み取り個数</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildBitDeviceReadRequest(
            DeviceCode deviceCode,
            uint startAddress,
            ushort count,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildDeviceReadData(deviceCode, startAddress, count);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Read,
                    0x0001, // ビットデバイス読み取りサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Read,
                    0x0001, // ビットデバイス読み取りサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ワードデバイス読み取り要求を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">読み取り個数</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildWordDeviceReadRequest(
            DeviceCode deviceCode,
            uint startAddress,
            ushort count,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildDeviceReadData(deviceCode, startAddress, count);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Read,
                    0x0000, // ワードデバイス読み取りサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Read,
                    0x0000, // ワードデバイス読み取りサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ビットデバイス書き込み要求を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="data">書き込みデータ</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildBitDeviceWriteRequest(
            DeviceCode deviceCode,
            uint startAddress,
            bool[] data,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var requestData = BuildDeviceWriteData(deviceCode, startAddress, data);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Write,
                    0x0001, // ビットデバイス書き込みサブコマンド
                    requestData,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(requestData);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Write,
                    0x0001, // ビットデバイス書き込みサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ワードデバイス書き込み要求を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="data">書き込みデータ</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildWordDeviceWriteRequest(
            DeviceCode deviceCode,
            uint startAddress,
            ushort[] data,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var requestData = BuildDeviceWriteData(deviceCode, startAddress, data);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Write,
                    0x0000, // ワードデバイス書き込みサブコマンド
                    requestData,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(requestData);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_Write,
                    0x0000, // ワードデバイス書き込みサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// デバイス読み取りデータ部分を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">読み取り個数</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildDeviceReadData(DeviceCode deviceCode, uint startAddress, ushort count)
        {
            var data = new List<byte>();

            // デバイスコード（1バイト）
            data.Add((byte)deviceCode);

            // 開始アドレス（3バイト、リトルエンディアン）
            data.Add((byte)(startAddress & 0xFF));
            data.Add((byte)((startAddress >> 8) & 0xFF));
            data.Add((byte)((startAddress >> 16) & 0xFF));

            // データ個数（2バイト、リトルエンディアン）
            data.Add((byte)(count & 0xFF));
            data.Add((byte)((count >> 8) & 0xFF));

            return data.ToArray();
        }

        /// <summary>
        /// ビットデバイス書き込みデータ部分を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="bitData">書き込みビットデータ</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildDeviceWriteData(DeviceCode deviceCode, uint startAddress, bool[] bitData)
        {
            var data = new List<byte>();

            // デバイスコード（1バイト）
            data.Add((byte)deviceCode);

            // 開始アドレス（3バイト、リトルエンディアン）
            data.Add((byte)(startAddress & 0xFF));
            data.Add((byte)((startAddress >> 8) & 0xFF));
            data.Add((byte)((startAddress >> 16) & 0xFF));

            // データ個数（2バイト、リトルエンディアン）
            ushort count = (ushort)bitData.Length;
            data.Add((byte)(count & 0xFF));
            data.Add((byte)((count >> 8) & 0xFF));

            // ビットデータをパック
            var packedBits = SlmpBitConverter.PackBits(bitData);
            data.AddRange(packedBits);

            return data.ToArray();
        }

        /// <summary>
        /// ワードデバイス書き込みデータ部分を構築
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="wordData">書き込みワードデータ</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildDeviceWriteData(DeviceCode deviceCode, uint startAddress, ushort[] wordData)
        {
            var data = new List<byte>();

            // デバイスコード（1バイト）
            data.Add((byte)deviceCode);

            // 開始アドレス（3バイト、リトルエンディアン）
            data.Add((byte)(startAddress & 0xFF));
            data.Add((byte)((startAddress >> 8) & 0xFF));
            data.Add((byte)((startAddress >> 16) & 0xFF));

            // データ個数（2バイト、リトルエンディアン）
            ushort count = (ushort)wordData.Length;
            data.Add((byte)(count & 0xFF));
            data.Add((byte)((count >> 8) & 0xFF));

            // ワードデータを追加（リトルエンディアン）
            var wordBytes = DataProcessor.UshortArrayToBytes(wordData);
            data.AddRange(wordBytes);

            return data.ToArray();
        }

        /// <summary>
        /// ランダムデバイス読み取り要求を構築
        /// </summary>
        /// <param name="wordDevices">ワードデバイスリスト</param>
        /// <param name="dwordDevices">ダブルワードデバイスリスト</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildRandomDeviceReadRequest(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildRandomReadData(wordDevices, dwordDevices);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_ReadRandom,
                    0x0000, // ランダム読み取りサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_ReadRandom,
                    0x0000, // ランダム読み取りサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ランダムビットデバイス書き込み要求を構築
        /// </summary>
        /// <param name="devices">ビットデバイスリスト</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildRandomBitDeviceWriteRequest(
            IList<(DeviceCode deviceCode, uint address, bool value)> devices,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildRandomBitWriteData(devices);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_WriteRandom,
                    0x0001, // ランダムビット書き込みサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_WriteRandom,
                    0x0001, // ランダムビット書き込みサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ランダムワードデバイス書き込み要求を構築
        /// </summary>
        /// <param name="wordDevices">ワードデバイスリスト</param>
        /// <param name="dwordDevices">ダブルワードデバイスリスト</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildRandomWordDeviceWriteRequest(
            IList<(DeviceCode deviceCode, uint address, ushort value)> wordDevices,
            IList<(DeviceCode deviceCode, uint address, uint value)> dwordDevices,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildRandomWordWriteData(wordDevices, dwordDevices);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_WriteRandom,
                    0x0000, // ランダムワード書き込みサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_WriteRandom,
                    0x0000, // ランダムワード書き込みサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ランダム読み取りデータ部分を構築
        /// </summary>
        /// <param name="wordDevices">ワードデバイスリスト</param>
        /// <param name="dwordDevices">ダブルワードデバイスリスト</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildRandomReadData(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices)
        {
            var data = new List<byte>();

            // ワードデバイス数（1バイト）
            data.Add((byte)wordDevices.Count);

            // ダブルワードデバイス数（1バイト）
            data.Add((byte)dwordDevices.Count);

            // ワードデバイス指定を追加
            foreach (var device in wordDevices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
            }

            // ダブルワードデバイス指定を追加
            foreach (var device in dwordDevices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
            }

            return data.ToArray();
        }

        /// <summary>
        /// ランダムビット書き込みデータ部分を構築
        /// </summary>
        /// <param name="devices">ビットデバイスリスト</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildRandomBitWriteData(
            IList<(DeviceCode deviceCode, uint address, bool value)> devices)
        {
            var data = new List<byte>();

            // デバイス数（1バイト）
            data.Add((byte)devices.Count);

            // デバイス指定を追加
            foreach (var device in devices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
                data.Add((byte)(device.value ? 1 : 0));
            }

            return data.ToArray();
        }

        /// <summary>
        /// ランダムワード書き込みデータ部分を構築
        /// </summary>
        /// <param name="wordDevices">ワードデバイスリスト</param>
        /// <param name="dwordDevices">ダブルワードデバイスリスト</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildRandomWordWriteData(
            IList<(DeviceCode deviceCode, uint address, ushort value)> wordDevices,
            IList<(DeviceCode deviceCode, uint address, uint value)> dwordDevices)
        {
            var data = new List<byte>();

            // ワードデバイス数（1バイト）
            data.Add((byte)wordDevices.Count);

            // ダブルワードデバイス数（1バイト）
            data.Add((byte)dwordDevices.Count);

            // ワードデバイス指定を追加
            foreach (var device in wordDevices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
                data.Add((byte)(device.value & 0xFF));
                data.Add((byte)((device.value >> 8) & 0xFF));
            }

            // ダブルワードデバイス指定を追加
            foreach (var device in dwordDevices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
                data.Add((byte)(device.value & 0xFF));
                data.Add((byte)((device.value >> 8) & 0xFF));
                data.Add((byte)((device.value >> 16) & 0xFF));
                data.Add((byte)((device.value >> 24) & 0xFF));
            }

            return data.ToArray();
        }

        /// <summary>
        /// ブロック読み取り要求を構築
        /// Python: read_block(word_list, bit_list, timeout=0)
        /// </summary>
        /// <param name="wordBlocks">ワードブロックリスト</param>
        /// <param name="bitBlocks">ビットブロックリスト</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildBlockReadRequest(
            IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildBlockReadData(wordBlocks, bitBlocks);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_ReadBlock,
                    0x0000, // ブロック読み取りサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_ReadBlock,
                    0x0000, // ブロック読み取りサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ブロック書き込み要求を構築
        /// Python: write_block(word_list, bit_list, timeout=0)
        /// </summary>
        /// <param name="wordBlocks">ワードブロックリスト</param>
        /// <param name="bitBlocks">ビットブロックリスト</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildBlockWriteRequest(
            IList<(DeviceCode deviceCode, uint address, ushort count, ushort[] data)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count, bool[] data)> bitBlocks,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildBlockWriteData(wordBlocks, bitBlocks);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_WriteBlock,
                    0x0000, // ブロック書き込みサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_WriteBlock,
                    0x0000, // ブロック書き込みサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// ブロック読み取りデータ部分を構築
        /// </summary>
        /// <param name="wordBlocks">ワードブロックリスト</param>
        /// <param name="bitBlocks">ビットブロックリスト</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildBlockReadData(
            IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks)
        {
            var data = new List<byte>();

            // ワードブロック数（1バイト）
            data.Add((byte)wordBlocks.Count);

            // ビットブロック数（1バイト）
            data.Add((byte)bitBlocks.Count);

            // ワードブロック指定を追加
            foreach (var block in wordBlocks)
            {
                data.Add((byte)block.deviceCode);
                data.Add((byte)(block.address & 0xFF));
                data.Add((byte)((block.address >> 8) & 0xFF));
                data.Add((byte)((block.address >> 16) & 0xFF));
                data.Add((byte)(block.count & 0xFF));
                data.Add((byte)((block.count >> 8) & 0xFF));
            }

            // ビットブロック指定を追加
            foreach (var block in bitBlocks)
            {
                data.Add((byte)block.deviceCode);
                data.Add((byte)(block.address & 0xFF));
                data.Add((byte)((block.address >> 8) & 0xFF));
                data.Add((byte)((block.address >> 16) & 0xFF));
                data.Add((byte)(block.count & 0xFF));
                data.Add((byte)((block.count >> 8) & 0xFF));
            }

            return data.ToArray();
        }

        /// <summary>
        /// ブロック書き込みデータ部分を構築
        /// </summary>
        /// <param name="wordBlocks">ワードブロックリスト</param>
        /// <param name="bitBlocks">ビットブロックリスト</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildBlockWriteData(
            IList<(DeviceCode deviceCode, uint address, ushort count, ushort[] data)> wordBlocks,
            IList<(DeviceCode deviceCode, uint address, ushort count, bool[] data)> bitBlocks)
        {
            var dataList = new List<byte>();

            // ワードブロック数（1バイト）
            dataList.Add((byte)wordBlocks.Count);

            // ビットブロック数（1バイト）
            dataList.Add((byte)bitBlocks.Count);

            // ワードブロック指定とデータを追加
            foreach (var block in wordBlocks)
            {
                dataList.Add((byte)block.deviceCode);
                dataList.Add((byte)(block.address & 0xFF));
                dataList.Add((byte)((block.address >> 8) & 0xFF));
                dataList.Add((byte)((block.address >> 16) & 0xFF));
                dataList.Add((byte)(block.count & 0xFF));
                dataList.Add((byte)((block.count >> 8) & 0xFF));

                // ワードデータを追加（リトルエンディアン）
                var wordBytes = DataProcessor.UshortArrayToBytes(block.data);
                dataList.AddRange(wordBytes);
            }

            // ビットブロック指定とデータを追加
            foreach (var block in bitBlocks)
            {
                dataList.Add((byte)block.deviceCode);
                dataList.Add((byte)(block.address & 0xFF));
                dataList.Add((byte)((block.address >> 8) & 0xFF));
                dataList.Add((byte)((block.address >> 16) & 0xFF));
                dataList.Add((byte)(block.count & 0xFF));
                dataList.Add((byte)((block.count >> 8) & 0xFF));

                // ビットデータをパック
                var packedBits = SlmpBitConverter.PackBits(block.data);
                dataList.AddRange(packedBits);
            }

            return dataList.ToArray();
        }

        /// <summary>
        /// モニタデバイス登録要求を構築
        /// Python: entry_monitor_device(word_list, dword_list, timeout=0)
        /// </summary>
        /// <param name="wordDevices">ワードデバイスリスト</param>
        /// <param name="dwordDevices">ダブルワードデバイスリスト</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildMonitorDeviceEntryRequest(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildMonitorDeviceEntryData(wordDevices, dwordDevices);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_EntryMonitorDevice,
                    0x0000, // モニタデバイス登録サブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_EntryMonitorDevice,
                    0x0000, // モニタデバイス登録サブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// モニタ実行要求を構築
        /// Python: execute_monitor(timeout=0)
        /// </summary>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildMonitorExecuteRequest(
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            // モニタ実行はデータ部分なし
            var data = new byte[0];

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_ExecuteMonitor,
                    0x0000, // モニタ実行サブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Device_ExecuteMonitor,
                    0x0000, // モニタ実行サブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// モニタデバイス登録データ部分を構築
        /// </summary>
        /// <param name="wordDevices">ワードデバイスリスト</param>
        /// <param name="dwordDevices">ダブルワードデバイスリスト</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildMonitorDeviceEntryData(
            IList<(DeviceCode deviceCode, uint address)> wordDevices,
            IList<(DeviceCode deviceCode, uint address)> dwordDevices)
        {
            var data = new List<byte>();

            // ワードデバイス数（1バイト）
            data.Add((byte)wordDevices.Count);

            // ダブルワードデバイス数（1バイト）
            data.Add((byte)dwordDevices.Count);

            // ワードデバイス指定を追加
            foreach (var device in wordDevices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
            }

            // ダブルワードデバイス指定を追加
            foreach (var device in dwordDevices)
            {
                data.Add((byte)device.deviceCode);
                data.Add((byte)(device.address & 0xFF));
                data.Add((byte)((device.address >> 8) & 0xFF));
                data.Add((byte)((device.address >> 16) & 0xFF));
            }

            return data.ToArray();
        }

        /// <summary>
        /// 型名読み取り要求を構築
        /// Python: read_type_name(timeout=0)
        /// </summary>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildReadTypeNameRequest(
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            // 型名読み取りはデータ部分なし
            var data = new byte[0];

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.RemoteControl_ReadTypeName,
                    0x0000, // 型名読み取りサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.RemoteControl_ReadTypeName,
                    0x0000, // 型名読み取りサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// セルフテスト要求を構築
        /// Python: self_test(data=None, timeout=0)
        /// </summary>
        /// <param name="testData">テストデータ（16進文字列、nullの場合は空データ）</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildSelfTestRequest(
            string? testData,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            byte[] data;
            
            if (string.IsNullOrEmpty(testData))
            {
                // テストデータなしの場合
                data = new byte[0];
            }
            else
            {
                // 16進文字列をバイト配列に変換
                data = DataProcessor.HexStringToBytes(testData);
            }

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.SelfTest,
                    0x0000, // セルフテストサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.SelfTest,
                    0x0000, // セルフテストサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// エラークリア要求を構築
        /// Python: clear_error(timeout=0)
        /// </summary>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildClearErrorRequest(
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            // エラークリアはデータ部分なし
            var data = new byte[0];

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.ClearError_Code,
                    0x0000, // エラークリアサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.ClearError_Code,
                    0x0000, // エラークリアサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// メモリ読み取り要求を構築
        /// Python: memory_read(addr, length, timeout=0)
        /// </summary>
        /// <param name="address">メモリアドレス</param>
        /// <param name="length">読み取り長さ（ワード単位）</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildMemoryReadRequest(
            uint address,
            ushort length,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var data = BuildMemoryReadData(address, length);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Memory_Read,
                    0x0000, // メモリ読み取りサブコマンド
                    data,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(data);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Memory_Read,
                    0x0000, // メモリ読み取りサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// メモリ書き込み要求を構築
        /// Python: memory_write(addr, data, timeout=0)
        /// </summary>
        /// <param name="address">メモリアドレス</param>
        /// <param name="data">書き込みデータ</param>
        /// <param name="settings">接続設定</param>
        /// <param name="target">通信対象</param>
        /// <param name="sequence">シーケンス番号</param>
        /// <param name="timeout">タイムアウト（250ms単位）</param>
        /// <returns>構築された要求フレーム</returns>
        public static byte[] BuildMemoryWriteRequest(
            uint address,
            byte[] data,
            SlmpConnectionSettings settings,
            SlmpTarget target,
            byte sequence,
            ushort timeout)
        {
            var requestData = BuildMemoryWriteData(address, data);

            // settings.IsBinaryに応じてフレーム構築方式を選択
            if (settings.IsBinary)
            {
                return FrameBuilder.BuildBinaryFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Memory_Write,
                    0x0000, // メモリ書き込みサブコマンド
                    requestData,
                    settings.Version);
            }
            else
            {
                var asciiData = ConvertToAsciiData(requestData);
                return FrameBuilder.BuildAsciiFrame(
                    sequence,
                    target,
                    timeout,
                    SlmpCommand.Memory_Write,
                    0x0000, // メモリ書き込みサブコマンド
                    asciiData,
                    settings.Version);
            }
        }

        /// <summary>
        /// メモリ読み取りデータ部分を構築
        /// </summary>
        /// <param name="address">メモリアドレス</param>
        /// <param name="length">読み取り長さ（ワード単位）</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildMemoryReadData(uint address, ushort length)
        {
            var data = new List<byte>();

            // メモリアドレス（4バイト、リトルエンディアン）
            data.Add((byte)(address & 0xFF));
            data.Add((byte)((address >> 8) & 0xFF));
            data.Add((byte)((address >> 16) & 0xFF));
            data.Add((byte)((address >> 24) & 0xFF));

            // 読み取り長さ（2バイト、リトルエンディアン）
            data.Add((byte)(length & 0xFF));
            data.Add((byte)((length >> 8) & 0xFF));

            return data.ToArray();
        }

        /// <summary>
        /// メモリ書き込みデータ部分を構築
        /// </summary>
        /// <param name="address">メモリアドレス</param>
        /// <param name="writeData">書き込みデータ</param>
        /// <returns>データ部分</returns>
        private static byte[] BuildMemoryWriteData(uint address, byte[] writeData)
        {
            var data = new List<byte>();

            // メモリアドレス（4バイト、リトルエンディアン）
            data.Add((byte)(address & 0xFF));
            data.Add((byte)((address >> 8) & 0xFF));
            data.Add((byte)((address >> 16) & 0xFF));
            data.Add((byte)((address >> 24) & 0xFF));

            // データ長（2バイト、リトルエンディアン）
            ushort dataLength = (ushort)(writeData.Length / 2); // バイト→ワード変換
            data.Add((byte)(dataLength & 0xFF));
            data.Add((byte)((dataLength >> 8) & 0xFF));

            // 書き込みデータを追加
            data.AddRange(writeData);

            return data.ToArray();
        }

        /// <summary>
        /// バイナリデータをASCII形式に変換
        /// </summary>
        /// <param name="binaryData">変換対象のバイナリデータ</param>
        /// <returns>ASCII形式に変換されたデータ</returns>
        private static byte[] ConvertToAsciiData(byte[] binaryData)
        {
            // バイナリデータを16進ASCII文字列に変換
            var hexString = Convert.ToHexString(binaryData);
            return Encoding.ASCII.GetBytes(hexString);
        }
    }
}