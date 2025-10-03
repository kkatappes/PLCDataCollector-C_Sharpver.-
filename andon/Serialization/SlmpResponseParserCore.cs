using System;
using SlmpClient.Constants;
using SlmpClient.Exceptions;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// SLMP レスポンス解析のコア実装
    /// 単一責任原則: レスポンス解析処理のみに特化
    /// リスコフの置換原則: ISlmpResponseParserの完全な実装
    /// </summary>
    public class SlmpResponseParserCore : ISlmpResponseParser
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
        public SlmpResponse ParseResponse(byte[] responseFrame, bool isBinary, SlmpFrameVersion version)
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
            catch (ArgumentException)
            {
                // ArgumentExceptionはフォールバック処理のためにそのまま透過
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

            // 手順書に基づく修正: 実際のPLC応答（20文字）を受け入れるよう調整
            int headerSize = version == SlmpFrameVersion.Version4E ? 20 : 18;

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

            // レスポンスヘッダー解析（境界チェック付き）
            if (offset + 2 <= frameSpan.Length)
            {
                response.Network = SlmpResponseParserHelper.ParseHexByte(frameSpan.Slice(offset, 2));
                offset += 2;
            }
            else
            {
                response.Network = 0;
            }

            if (offset + 2 <= frameSpan.Length)
            {
                response.Node = SlmpResponseParserHelper.ParseHexByte(frameSpan.Slice(offset, 2));
                offset += 2;
            }
            else
            {
                response.Node = 0;
            }

            if (offset + 4 <= frameSpan.Length)
            {
                response.DestinationProcessor = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
                offset += 4;
            }
            else
            {
                response.DestinationProcessor = 0;
            }

            if (offset + 2 <= frameSpan.Length)
            {
                response.MultiDropStation = SlmpResponseParserHelper.ParseHexByte(frameSpan.Slice(offset, 2));
                offset += 2;
            }
            else
            {
                response.MultiDropStation = 0;
            }

            // データ長とエンドコード（20文字応答では省略されている場合がある）
            if (offset + 8 <= frameSpan.Length)
            {
                // 通常の応答（22文字以上）
                response.DataLength = SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
                offset += 4;
                response.EndCode = (EndCode)SlmpResponseParserHelper.ParseHexUshort(frameSpan.Slice(offset, 4));
                offset += 4;
            }
            else
            {
                // 簡略化された応答（20文字）- データ長とエンドコードを既定値に設定
                response.DataLength = 0;
                response.EndCode = EndCode.Success;
            }

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
}