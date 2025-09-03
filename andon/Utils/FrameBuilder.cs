using System;
using System.Text;
using SlmpClient.Constants;
using SlmpClient.Core;

namespace SlmpClient.Utils
{
    /// <summary>
    /// SLMPフレーム構築ユーティリティ
    /// Python版のmake_binary_frame/make_ascii_frame相当機能
    /// </summary>
    public static class FrameBuilder
    {
        /// <summary>
        /// バイナリモードの場合のコマンドフレームを作成する
        /// </summary>
        /// <param name="sequence">シーケンス番号 (0-255)</param>
        /// <param name="target">通信対象設定</param>
        /// <param name="timeout">監視タイマ 250msec単位</param>
        /// <param name="command">SLMPコマンド</param>
        /// <param name="subCommand">サブコマンド</param>
        /// <param name="data">送信データ</param>
        /// <param name="version">フレームバージョン（3E or 4E）</param>
        /// <returns>構築されたバイナリフレーム</returns>
        /// <exception cref="ArgumentNullException">targetまたはdataがnullの場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">パラメータが範囲外の場合</exception>
        /// <exception cref="ArgumentException">フレームサイズが制限を超える場合</exception>
        public static byte[] BuildBinaryFrame(
            byte sequence,
            SlmpTarget target,
            ushort timeout,
            SlmpCommand command,
            ushort subCommand,
            byte[] data,
            SlmpFrameVersion version)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ValidateFrameParameters(timeout, subCommand, data.Length);

            // データ長 + 固定部分の長さ（6バイト）
            ushort dataLength = (ushort)(data.Length + 6);

            // コマンド部分を構築
            var commandPart = new byte[14];
            var index = 0;

            // ターゲット情報
            commandPart[index++] = target.Network;
            commandPart[index++] = target.Node;
            
            // 要求先プロセッサ番号（リトルエンディアン）
            commandPart[index++] = (byte)(target.DestinationProcessor & 0xFF);
            commandPart[index++] = (byte)((target.DestinationProcessor >> 8) & 0xFF);
            
            commandPart[index++] = target.MultiDropStation;

            // データ長（リトルエンディアン）
            commandPart[index++] = (byte)(dataLength & 0xFF);
            commandPart[index++] = (byte)((dataLength >> 8) & 0xFF);

            // タイムアウト（リトルエンディアン）
            commandPart[index++] = (byte)(timeout & 0xFF);
            commandPart[index++] = (byte)((timeout >> 8) & 0xFF);

            // コマンド（リトルエンディアン）
            var commandValue = (ushort)command;
            commandPart[index++] = (byte)(commandValue & 0xFF);
            commandPart[index++] = (byte)((commandValue >> 8) & 0xFF);

            // サブコマンド（リトルエンディアン）
            commandPart[index++] = (byte)(subCommand & 0xFF);
            commandPart[index++] = (byte)((subCommand >> 8) & 0xFF);

            // フレーム全体を構築
            byte[] frame;
            if (version == SlmpFrameVersion.Version4E)
            {
                // 4Eフレーム: ヘッダー(5バイト) + コマンド部(12バイト) + データ
                frame = new byte[5 + 12 + data.Length];
                var frameIndex = 0;

                // 4Eヘッダー
                frame[frameIndex++] = 0x54;
                frame[frameIndex++] = 0x00;
                frame[frameIndex++] = sequence;
                frame[frameIndex++] = 0x00;
                frame[frameIndex++] = 0x00;

                // コマンド部をコピー
                Array.Copy(commandPart, 0, frame, frameIndex, 12);
                frameIndex += 12;

                // データ部をコピー
                Array.Copy(data, 0, frame, frameIndex, data.Length);
            }
            else if (version == SlmpFrameVersion.Version3E)
            {
                // 3Eフレーム: ヘッダー(2バイト) + コマンド部(12バイト) + データ
                frame = new byte[2 + 12 + data.Length];
                var frameIndex = 0;

                // 3Eヘッダー
                frame[frameIndex++] = 0x50;
                frame[frameIndex++] = 0x00;

                // コマンド部をコピー
                Array.Copy(commandPart, 0, frame, frameIndex, 12);
                frameIndex += 12;

                // データ部をコピー
                Array.Copy(data, 0, frame, frameIndex, data.Length);
            }
            else
            {
                throw new ArgumentException($"Unsupported frame version: {version}", nameof(version));
            }

            // フレームサイズ制限チェック
            if (frame.Length >= 8194)
                throw new ArgumentException($"Frame size ({frame.Length} bytes) exceeds limit (8194 bytes)");

            return frame;
        }

        /// <summary>
        /// ASCIIモードの場合のコマンドフレームを作成する
        /// </summary>
        /// <param name="sequence">シーケンス番号 (0-255)</param>
        /// <param name="target">通信対象設定</param>
        /// <param name="timeout">監視タイマ 250msec単位</param>
        /// <param name="command">SLMPコマンド</param>
        /// <param name="subCommand">サブコマンド</param>
        /// <param name="data">送信データ（ASCIIバイト列）</param>
        /// <param name="version">フレームバージョン（3E or 4E）</param>
        /// <returns>構築されたASCIIフレーム</returns>
        /// <exception cref="ArgumentNullException">targetまたはdataがnullの場合</exception>
        /// <exception cref="ArgumentOutOfRangeException">パラメータが範囲外の場合</exception>
        /// <exception cref="ArgumentException">フレームサイズが制限を超える場合</exception>
        public static byte[] BuildAsciiFrame(
            byte sequence,
            SlmpTarget target,
            ushort timeout,
            SlmpCommand command,
            ushort subCommand,
            byte[] data,
            SlmpFrameVersion version)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            ValidateFrameParameters(timeout, subCommand, data.Length);

            // データ長 + 固定部分の長さ（12バイト：ASCIIでは文字数）
            ushort dataLength = (ushort)(data.Length + 12);

            // コマンド部分をASCII形式で構築
            var commandText = string.Format(
                "{0:X2}{1:X2}{2:X4}{3:X2}{4:X4}{5:X4}{6:X4}{7:X4}",
                target.Network,
                target.Node,
                target.DestinationProcessor,
                target.MultiDropStation,
                dataLength,
                timeout,
                (ushort)command,
                subCommand);

            byte[] commandBytes = Encoding.ASCII.GetBytes(commandText);

            // フレーム全体を構築
            byte[] frame;
            if (version == SlmpFrameVersion.Version4E)
            {
                // 4Eフレーム: ヘッダー + コマンド部 + データ
                var headerText = $"5400{sequence:X4}0000";
                var headerBytes = Encoding.ASCII.GetBytes(headerText);
                
                frame = new byte[headerBytes.Length + commandBytes.Length + data.Length];
                var frameIndex = 0;

                Array.Copy(headerBytes, 0, frame, frameIndex, headerBytes.Length);
                frameIndex += headerBytes.Length;

                Array.Copy(commandBytes, 0, frame, frameIndex, commandBytes.Length);
                frameIndex += commandBytes.Length;

                Array.Copy(data, 0, frame, frameIndex, data.Length);
            }
            else if (version == SlmpFrameVersion.Version3E)
            {
                // 3Eフレーム: ヘッダー + コマンド部 + データ
                var headerBytes = Encoding.ASCII.GetBytes("5000");
                
                frame = new byte[headerBytes.Length + commandBytes.Length + data.Length];
                var frameIndex = 0;

                Array.Copy(headerBytes, 0, frame, frameIndex, headerBytes.Length);
                frameIndex += headerBytes.Length;

                Array.Copy(commandBytes, 0, frame, frameIndex, commandBytes.Length);
                frameIndex += commandBytes.Length;

                Array.Copy(data, 0, frame, frameIndex, data.Length);
            }
            else
            {
                throw new ArgumentException($"Unsupported frame version: {version}", nameof(version));
            }

            // フレームサイズ制限チェック
            if (frame.Length >= 8194)
                throw new ArgumentException($"Frame size ({frame.Length} bytes) exceeds limit (8194 bytes)");

            return frame;
        }

        /// <summary>
        /// フレーム構築パラメータの妥当性をチェック
        /// </summary>
        /// <param name="timeout">タイムアウト値</param>
        /// <param name="subCommand">サブコマンド値</param>
        /// <param name="dataLength">データ長</param>
        /// <exception cref="ArgumentOutOfRangeException">パラメータが範囲外の場合</exception>
        private static void ValidateFrameParameters(ushort timeout, ushort subCommand, int dataLength)
        {
            if (timeout > 0xFFFF)
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be 0-65535");

            if (subCommand > 0xFFFF)
                throw new ArgumentOutOfRangeException(nameof(subCommand), subCommand, "SubCommand must be 0-65535");

            if (dataLength < 0)
                throw new ArgumentOutOfRangeException(nameof(dataLength), dataLength, "Data length cannot be negative");
        }
    }
}