using System;
using System.Buffers;
using System.Linq;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Utils;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// SLMPレスポンス解析クラス（ゼロアロケーション版）
    /// SpanとReadOnlySpanを活用したメモリ効率化
    /// </summary>
    public static class SlmpResponseParser
    {
        /// <summary>
        /// SLMPレスポンスフレームを解析
        /// </summary>
        /// <param name="responseFrame">受信したレスポンスフレーム</param>
        /// <param name="isBinary">バイナリモードかどうか</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>解析されたレスポンス</returns>
        /// <exception cref="ArgumentNullException">responseFrameがnullの場合</exception>
        /// <exception cref="SlmpCommunicationException">レスポンス解析エラーまたはSLMPエラーの場合</exception>
        public static SlmpResponse ParseResponse(byte[] responseFrame, bool isBinary, SlmpFrameVersion version)
        {
            if (responseFrame == null)
                throw new ArgumentNullException(nameof(responseFrame));

            if (responseFrame.Length == 0)
                throw new SlmpCommunicationException("Empty response frame");

            try
            {
                if (isBinary)
                {
                    return ParseBinaryResponse(responseFrame, version);
                }
                else
                {
                    return ParseAsciiResponse(responseFrame, version);
                }
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SlmpCommunicationException("Failed to parse SLMP response", ex);
            }
        }

        /// <summary>
        /// バイナリレスポンスを解析
        /// </summary>
        /// <param name="responseFrame">レスポンスフレーム</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>解析されたレスポンス</returns>
        private static SlmpResponse ParseBinaryResponse(byte[] responseFrame, SlmpFrameVersion version)
        {
            int headerSize = version == SlmpFrameVersion.Version4E ? 11 : 9;

            if (responseFrame.Length < headerSize)
                throw new SlmpCommunicationException($"Response frame too short: {responseFrame.Length} bytes, expected at least {headerSize}");

            var response = new SlmpResponse();
            int offset = 0;

            // ヘッダー解析
            if (version == SlmpFrameVersion.Version4E)
            {
                // 4Eフレーム: サブヘッダー(2) + シーケンス(2) + 予約(2) + レスポンスヘッダー(5)
                response.SubHeader = System.BitConverter.ToUInt16(responseFrame, offset);
                offset += 2;
                
                response.Sequence = System.BitConverter.ToUInt16(responseFrame, offset);
                offset += 2;
                
                // 予約領域をスキップ
                offset += 2;
            }
            else
            {
                // 3Eフレーム: サブヘッダー(2) + レスポンスヘッダー(7)
                response.SubHeader = System.BitConverter.ToUInt16(responseFrame, offset);
                offset += 2;
            }

            // レスポンスヘッダー解析
            response.Network = responseFrame[offset++];
            response.Node = responseFrame[offset++];
            response.DestinationProcessor = System.BitConverter.ToUInt16(responseFrame, offset);
            offset += 2;
            response.MultiDropStation = responseFrame[offset++];

            // データ長とエンドコード
            response.DataLength = System.BitConverter.ToUInt16(responseFrame, offset);
            offset += 2;
            response.EndCode = (EndCode)System.BitConverter.ToUInt16(responseFrame, offset);
            offset += 2;

            // エンドコードチェック
            if (response.EndCode != EndCode.Success)
            {
                throw new SlmpCommunicationException(response.EndCode, responseFrame);
            }

            // データ部分を抽出
            int dataSize = responseFrame.Length - offset;
            if (dataSize > 0)
            {
                response.Data = new byte[dataSize];
                Array.Copy(responseFrame, offset, response.Data, 0, dataSize);
            }
            else
            {
                response.Data = Array.Empty<byte>();
            }

            return response;
        }

        /// <summary>
        /// ASCIIレスポンスを解析（ゼロアロケーション版）
        /// </summary>
        /// <param name="responseFrame">レスポンスフレーム</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>解析されたレスポンス</returns>
        private static SlmpResponse ParseAsciiResponse(byte[] responseFrame, SlmpFrameVersion version)
        {
            // ASCIIフレームをSpanで効率的に処理
            var frameSpan = responseFrame.AsSpan();
            
            int headerSize = version == SlmpFrameVersion.Version4E ? 22 : 18;

            if (frameSpan.Length < headerSize)
                throw new SlmpCommunicationException($"Response frame too short: {frameSpan.Length} chars, expected at least {headerSize}");

            var response = new SlmpResponse();
            int offset = 0;

            // ヘッダー解析（Spanを使用）
            if (version == SlmpFrameVersion.Version4E)
            {
                // 4Eフレーム: サブヘッダー(4) + シーケンス(4) + 予約(4) + レスポンスヘッダー(10)
                response.SubHeader = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
                offset += 4;
                
                response.Sequence = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
                offset += 4;
                
                // 予約領域をスキップ
                offset += 4;
            }
            else
            {
                // 3Eフレーム: サブヘッダー(4) + レスポンスヘッダー(14)
                response.SubHeader = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
                offset += 4;
            }

            // レスポンスヘッダー解析
            response.Network = SlmpResponseParserHelper.ParseHexByte(frameSpan.Slice(offset, 2));
            offset += 2;
            response.Node = SlmpResponseParserHelper.ParseHexByte(frameSpan.Slice(offset, 2));
            offset += 2;
            response.DestinationProcessor = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
            offset += 4;
            response.MultiDropStation = SlmpResponseParserHelper.ParseHexByte(frameSpan.Slice(offset, 2));
            offset += 2;

            // データ長とエンドコード
            response.DataLength = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
            offset += 4;
            response.EndCode = (EndCode)SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
            offset += 4;

            // エンドコードチェック
            if (response.EndCode != EndCode.Success)
            {
                throw new SlmpCommunicationException(response.EndCode, responseFrame);
            }

            // データ部分を抽出（ゼロアロケーション版）
            if (offset < frameSpan.Length)
            {
                var dataSpan = frameSpan.Slice(offset);
                if (dataSpan.Length % 2 != 0)
                    throw new SlmpCommunicationException("Invalid ASCII response data length");

                response.Data = new byte[dataSpan.Length / 2];
                SlmpResponseParserHelper.ParseHexBytesToArray(dataSpan, response.Data);
            }
            else
            {
                response.Data = Array.Empty<byte>();
            }

            return response;
        }
    }

    /// <summary>
    /// SLMPレスポンス構造体
    /// </summary>
    public class SlmpResponse
    {
        /// <summary>サブヘッダー</summary>
        public ushort SubHeader { get; set; }

        /// <summary>シーケンス番号（4Eフレームのみ）</summary>
        public ushort? Sequence { get; set; }

        /// <summary>ネットワーク番号</summary>
        public byte Network { get; set; }

        /// <summary>ノード番号</summary>
        public byte Node { get; set; }

        /// <summary>要求先プロセッサ番号</summary>
        public ushort DestinationProcessor { get; set; }

        /// <summary>要求先マルチドロップ局番</summary>
        public byte MultiDropStation { get; set; }

        /// <summary>データ長</summary>
        public ushort DataLength { get; set; }

        /// <summary>エンドコード</summary>
        public EndCode EndCode { get; set; }

        /// <summary>データ部分</summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// ゼロアロケーション処理用ヘルパーメソッド
    /// </summary>
    public static class SlmpResponseParserHelper
    {
        /// <summary>
        /// 16進文字列をbyteに変換（ゼロアロケーション版）
        /// </summary>
        /// <param name="hexSpan">16進文字列のSpan（2文字）</param>
        /// <returns>変換されたbyte</returns>
        public static byte ParseHexByte(ReadOnlySpan<byte> hexSpan)
        {
            if (hexSpan.Length != 2)
                throw new ArgumentException("Hex byte span must be 2 characters");

            return (byte)((GetHexValue(hexSpan[0]) << 4) | GetHexValue(hexSpan[1]));
        }

        /// <summary>
        /// 16進文字列をushortに変換（ゼロアロケーション版）
        /// </summary>
        /// <param name="hexSpan">16進文字列のSpan（4文字）</param>
        /// <returns>変換されたushort</returns>
        public static ushort ParseHexUshort(ReadOnlySpan<byte> hexSpan)
        {
            if (hexSpan.Length != 4)
                throw new ArgumentException("Hex ushort span must be 4 characters");

            return (ushort)((GetHexValue(hexSpan[0]) << 12) |
                           (GetHexValue(hexSpan[1]) << 8) |
                           (GetHexValue(hexSpan[2]) << 4) |
                           GetHexValue(hexSpan[3]));
        }

        /// <summary>
        /// 16進文字列をバイト配列に変換（ゼロアロケーション版）
        /// </summary>
        /// <param name="hexSpan">16進文字列のSpan</param>
        /// <param name="result">結果格納先配列</param>
        public static void ParseHexBytesToArray(ReadOnlySpan<byte> hexSpan, Span<byte> result)
        {
            if (hexSpan.Length % 2 != 0)
                throw new ArgumentException("Hex span length must be even");

            if (result.Length != hexSpan.Length / 2)
                throw new ArgumentException("Result array size mismatch");

            for (int i = 0; i < result.Length; i++)
            {
                var hexByteSpan = hexSpan.Slice(i * 2, 2);
                result[i] = ParseHexByte(hexByteSpan);
            }
        }

        /// <summary>
        /// 16進文字から数値を取得（ゼロアロケーション版）
        /// </summary>
        /// <param name="hexChar">16進文字</param>
        /// <returns>数値</returns>
        public static int GetHexValue(byte hexChar)
        {
            return hexChar switch
            {
                >= (byte)'0' and <= (byte)'9' => hexChar - '0',
                >= (byte)'A' and <= (byte)'F' => hexChar - 'A' + 10,
                >= (byte)'a' and <= (byte)'f' => hexChar - 'a' + 10,
                _ => throw new ArgumentException($"Invalid hex character: {(char)hexChar}")
            };
        }
    }

    /// <summary>
    /// メモリ効率的なレスポンスパーサーユーティリティ
    /// </summary>
    public static class ZeroAllocationResponseParser
    {
        /// <summary>
        /// レスポンスをメモリコピーなしで解析
        /// </summary>
        /// <param name="responseSpan">レスポンスフレームSpan</param>
        /// <param name="isBinary">バイナリモードかどうか</param>
        /// <param name="version">フレームバージョン</param>
        /// <param name="result">結果格納先</param>
        /// <returns>データ部分のSpan</returns>
        public static ReadOnlySpan<byte> ParseResponseSpan(
            ReadOnlySpan<byte> responseSpan,
            bool isBinary,
            SlmpFrameVersion version,
            out SlmpResponseHeader result)
        {
            if (responseSpan.IsEmpty)
                throw new SlmpCommunicationException("Empty response frame");

            try
            {
                if (isBinary)
                {
                    return ParseBinaryResponseSpan(responseSpan, version, out result);
                }
                else
                {
                    return ParseAsciiResponseSpan(responseSpan, version, out result);
                }
            }
            catch (SlmpCommunicationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SlmpCommunicationException("Failed to parse SLMP response", ex);
            }
        }

        /// <summary>
        /// バイナリレスポンスをSpanで解析
        /// </summary>
        private static ReadOnlySpan<byte> ParseBinaryResponseSpan(
            ReadOnlySpan<byte> responseSpan,
            SlmpFrameVersion version,
            out SlmpResponseHeader result)
        {
            int headerSize = version == SlmpFrameVersion.Version4E ? 11 : 9;

            if (responseSpan.Length < headerSize)
                throw new SlmpCommunicationException($"Response frame too short: {responseSpan.Length} bytes, expected at least {headerSize}");

            result = new SlmpResponseHeader();
            int offset = 0;

            // ヘッダー解析
            if (version == SlmpFrameVersion.Version4E)
            {
                result.SubHeader = System.BitConverter.ToUInt16(responseSpan.Slice(offset, 2));
                offset += 2;
                result.Sequence = System.BitConverter.ToUInt16(responseSpan.Slice(offset, 2));
                offset += 4; // 予約領域もスキップ
            }
            else
            {
                result.SubHeader = System.BitConverter.ToUInt16(responseSpan.Slice(offset, 2));
                offset += 2;
            }

            // レスポンスヘッダー解析
            result.Network = responseSpan[offset++];
            result.Node = responseSpan[offset++];
            result.DestinationProcessor = System.BitConverter.ToUInt16(responseSpan.Slice(offset, 2));
            offset += 2;
            result.MultiDropStation = responseSpan[offset++];

            // データ長とエンドコード
            result.DataLength = System.BitConverter.ToUInt16(responseSpan.Slice(offset, 2));
            offset += 2;
            result.EndCode = (EndCode)System.BitConverter.ToUInt16(responseSpan.Slice(offset, 2));
            offset += 2;

            // エンドコードチェック
            if (result.EndCode != EndCode.Success)
            {
                throw new SlmpCommunicationException(result.EndCode, responseSpan.ToArray());
            }

            // データ部分を返却
            return responseSpan.Slice(offset);
        }

        /// <summary>
        /// ASCIIレスポンスをSpanで解析
        /// </summary>
        private static ReadOnlySpan<byte> ParseAsciiResponseSpan(
            ReadOnlySpan<byte> responseSpan,
            SlmpFrameVersion version,
            out SlmpResponseHeader result)
        {
            int headerSize = version == SlmpFrameVersion.Version4E ? 22 : 18;

            if (responseSpan.Length < headerSize)
                throw new SlmpCommunicationException($"Response frame too short: {responseSpan.Length} chars, expected at least {headerSize}");

            result = new SlmpResponseHeader();
            int offset = 0;

            // ヘッダー解析
            if (version == SlmpFrameVersion.Version4E)
            {
                result.SubHeader = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
                offset += 4;
                result.Sequence = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
                offset += 8; // 予約領域もスキップ
            }
            else
            {
                result.SubHeader = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
                offset += 4;
            }

            // レスポンスヘッダー解析
            result.Network = SlmpResponseParserHelper.ParseHexByte(responseSpan.Slice(offset, 2));
            offset += 2;
            result.Node = SlmpResponseParserHelper.ParseHexByte(responseSpan.Slice(offset, 2));
            offset += 2;
            result.DestinationProcessor = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
            offset += 4;
            result.MultiDropStation = SlmpResponseParserHelper.ParseHexByte(responseSpan.Slice(offset, 2));
            offset += 2;

            // データ長とエンドコード
            result.DataLength = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
            offset += 4;
            result.EndCode = (EndCode)SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
            offset += 4;

            // エンドコードチェック
            if (result.EndCode != EndCode.Success)
            {
                throw new SlmpCommunicationException(result.EndCode, responseSpan.ToArray());
            }

            // データ部分を返却
            return responseSpan.Slice(offset);
        }
    }

    /// <summary>
    /// SLMPレスポンスヘッダー構造体（メモリ効率版）
    /// </summary>
    public struct SlmpResponseHeader
    {
        /// <summary>サブヘッダー</summary>
        public ushort SubHeader { get; set; }

        /// <summary>シーケンス番号（4Eフレームのみ）</summary>
        public ushort? Sequence { get; set; }

        /// <summary>ネットワーク番号</summary>
        public byte Network { get; set; }

        /// <summary>ノード番号</summary>
        public byte Node { get; set; }

        /// <summary>要求先プロセッサ番号</summary>
        public ushort DestinationProcessor { get; set; }

        /// <summary>要求先マルチドロップ局番</summary>
        public byte MultiDropStation { get; set; }

        /// <summary>データ長</summary>
        public ushort DataLength { get; set; }

        /// <summary>エンドコード</summary>
        public EndCode EndCode { get; set; }
    }
}