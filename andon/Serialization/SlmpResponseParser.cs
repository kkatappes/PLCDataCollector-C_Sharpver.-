using System;
using System.Linq;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Utils;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// SLMPレスポンス解析クラス
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
        /// ASCIIレスポンスを解析
        /// </summary>
        /// <param name="responseFrame">レスポンスフレーム</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>解析されたレスポンス</returns>
        private static SlmpResponse ParseAsciiResponse(byte[] responseFrame, SlmpFrameVersion version)
        {
            // ASCIIフレームは16進文字列なので、文字列として解析
            string responseText = System.Text.Encoding.ASCII.GetString(responseFrame);
            
            int headerSize = version == SlmpFrameVersion.Version4E ? 22 : 18;

            if (responseText.Length < headerSize)
                throw new SlmpCommunicationException($"Response frame too short: {responseText.Length} chars, expected at least {headerSize}");

            var response = new SlmpResponse();
            int offset = 0;

            // ヘッダー解析
            if (version == SlmpFrameVersion.Version4E)
            {
                // 4Eフレーム: サブヘッダー(4) + シーケンス(4) + 予約(4) + レスポンスヘッダー(10)
                response.SubHeader = Convert.ToUInt16(responseText.Substring(offset, 4), 16);
                offset += 4;
                
                response.Sequence = Convert.ToUInt16(responseText.Substring(offset, 4), 16);
                offset += 4;
                
                // 予約領域をスキップ
                offset += 4;
            }
            else
            {
                // 3Eフレーム: サブヘッダー(4) + レスポンスヘッダー(14)
                response.SubHeader = Convert.ToUInt16(responseText.Substring(offset, 4), 16);
                offset += 4;
            }

            // レスポンスヘッダー解析
            response.Network = Convert.ToByte(responseText.Substring(offset, 2), 16);
            offset += 2;
            response.Node = Convert.ToByte(responseText.Substring(offset, 2), 16);
            offset += 2;
            response.DestinationProcessor = Convert.ToUInt16(responseText.Substring(offset, 4), 16);
            offset += 4;
            response.MultiDropStation = Convert.ToByte(responseText.Substring(offset, 2), 16);
            offset += 2;

            // データ長とエンドコード
            response.DataLength = Convert.ToUInt16(responseText.Substring(offset, 4), 16);
            offset += 4;
            response.EndCode = (EndCode)Convert.ToUInt16(responseText.Substring(offset, 4), 16);
            offset += 4;

            // エンドコードチェック
            if (response.EndCode != EndCode.Success)
            {
                throw new SlmpCommunicationException(response.EndCode, responseFrame);
            }

            // データ部分を抽出（16進文字列をバイナリに変換）
            if (offset < responseText.Length)
            {
                string dataText = responseText.Substring(offset);
                if (dataText.Length % 2 != 0)
                    throw new SlmpCommunicationException("Invalid ASCII response data length");

                response.Data = new byte[dataText.Length / 2];
                for (int i = 0; i < response.Data.Length; i++)
                {
                    response.Data[i] = Convert.ToByte(dataText.Substring(i * 2, 2), 16);
                }
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
}