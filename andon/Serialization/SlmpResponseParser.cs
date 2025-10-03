using System;
using System.Buffers;
using System.Linq;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Utils;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// SLMPレスポンス解析ファサード（SOLID原則適用版）
    /// ファサードパターン: 複雑なサブシステムを単純なインターフェースで提供
    /// 既存APIとの互換性を保ちながらSOLID原則を適用
    /// </summary>
    public static class SlmpResponseParser
    {
        // 依存性注入用のデフォルトインスタンス
        private static readonly IResponseFormatDetector _defaultFormatDetector = new SuspiciousByteResponseFormatDetector();
        private static readonly ISlmpResponseParser _defaultParser = new SlmpResponseParserCore();
        private static readonly IFallbackProcessor _defaultFallbackProcessor = new AutoDetectionFallbackProcessor(_defaultFormatDetector, _defaultParser);

        /// <summary>
        /// バイナリ/ASCII自動判定機能
        /// SOLID原則: 実装を戦略パターンで分離
        /// 後方互換性: 既存APIを維持
        /// </summary>
        /// <param name="responseFrame">判定対象のレスポンスフレーム</param>
        /// <returns>バイナリ形式の場合true、ASCII形式の場合false</returns>
        public static bool IsBinaryResponse(byte[] responseFrame)
        {
            return _defaultFormatDetector.IsBinaryResponse(responseFrame);
        }

        /// <summary>
        /// SLMPレスポンスフレームを解析（SOLID原則適用版）
        /// ファサードパターン: 内部の複雑性を隠蔽し、シンプルなAPIを提供
        /// フォールバック処理付き
        /// </summary>
        /// <param name="responseFrame">受信したレスポンスフレーム</param>
        /// <param name="isBinary">バイナリモードかどうか</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>解析されたレスポンス</returns>
        /// <exception cref="ArgumentNullException">responseFrameがnullの場合</exception>
        /// <exception cref="SlmpCommunicationException">レスポンス解析エラーまたはSLMPエラーの場合</exception>
        public static SlmpResponse ParseResponse(byte[] responseFrame, bool isBinary, SlmpFrameVersion version)
        {
            try
            {
                // 通常の解析を試行
                return _defaultParser.ParseResponse(responseFrame, isBinary, version);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("無効な16進文字"))
            {
                // フォールバック処理を実行
                return _defaultFallbackProcessor.ProcessFallback(responseFrame, ex, isBinary, version);
            }
        }

        /// <summary>
        /// 依存性注入対応版（高度な利用者向け）
        /// 開放/閉鎖原則: カスタム戦略を注入可能
        /// </summary>
        /// <param name="responseFrame">受信したレスポンスフレーム</param>
        /// <param name="isBinary">バイナリモードかどうか</param>
        /// <param name="version">フレームバージョン</param>
        /// <param name="formatDetector">カスタムフォーマット検出器</param>
        /// <param name="parser">カスタム解析器</param>
        /// <param name="fallbackProcessor">カスタムフォールバック処理器</param>
        /// <returns>解析されたレスポンス</returns>
        public static SlmpResponse ParseResponse(
            byte[] responseFrame,
            bool isBinary,
            SlmpFrameVersion version,
            IResponseFormatDetector formatDetector = null,
            ISlmpResponseParser parser = null,
            IFallbackProcessor fallbackProcessor = null)
        {
            var actualParser = parser ?? _defaultParser;
            var actualFallbackProcessor = fallbackProcessor ?? _defaultFallbackProcessor;

            try
            {
                return actualParser.ParseResponse(responseFrame, isBinary, version);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("無効な16進文字"))
            {
                return actualFallbackProcessor.ProcessFallback(responseFrame, ex, isBinary, version);
            }
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
                _ => throw new ArgumentException($"無効な16進文字: バイト値=0x{hexChar:X2}, 文字='{(char)hexChar}' (ASCII={hexChar}), 有効な16進文字は 0-9, A-F, a-f です")
            };
        }

        /// <summary>
        /// 16進文字の有効性チェック
        /// </summary>
        /// <param name="hexChar">チェック対象の文字</param>
        /// <returns>有効な16進文字の場合true</returns>
        public static bool IsValidHexChar(byte hexChar)
        {
            return (hexChar >= 0x30 && hexChar <= 0x39) ||  // '0'-'9'
                   (hexChar >= 0x41 && hexChar <= 0x46) ||  // 'A'-'F'
                   (hexChar >= 0x61 && hexChar <= 0x66);    // 'a'-'f'
        }

        /// <summary>
        /// 16進文字から数値を取得（フォールバック処理付き）
        /// エラー発生時に0を返すセーフモード
        /// </summary>
        /// <param name="hexChar">16進文字</param>
        /// <param name="allowFallback">フォールバック処理を許可するか</param>
        /// <returns>数値（エラー時は0）</returns>
        public static int GetHexValueSafe(byte hexChar, bool allowFallback = true)
        {
            try
            {
                return GetHexValue(hexChar);
            }
            catch (ArgumentException)
            {
                if (allowFallback)
                {
                    // フォールバック: 無効文字は0として処理
                    return 0;
                }
                throw; // フォールバック無効時は例外を再スロー
            }
        }

        /// <summary>
        /// 16進文字列の健全性チェック
        /// </summary>
        /// <param name="hexString">チェック対象の16進文字列</param>
        /// <returns>全て有効な16進文字の場合true</returns>
        public static bool IsValidHexString(ReadOnlySpan<byte> hexString)
        {
            for (int i = 0; i < hexString.Length; i++)
            {
                if (!IsValidHexChar(hexString[i]))
                {
                    return false;
                }
            }
            return true;
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
            // 手順書に基づく修正: 実際のPLC応答（20文字）を受け入れるよう調整
            int headerSize = version == SlmpFrameVersion.Version4E ? 20 : 18;

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

            // レスポンスヘッダー解析（境界チェック付き）
            if (offset + 2 <= responseSpan.Length)
            {
                result.Network = SlmpResponseParserHelper.ParseHexByte(responseSpan.Slice(offset, 2));
                offset += 2;
            }
            else
            {
                result.Network = 0;
            }

            if (offset + 2 <= responseSpan.Length)
            {
                result.Node = SlmpResponseParserHelper.ParseHexByte(responseSpan.Slice(offset, 2));
                offset += 2;
            }
            else
            {
                result.Node = 0;
            }

            if (offset + 4 <= responseSpan.Length)
            {
                result.DestinationProcessor = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
                offset += 4;
            }
            else
            {
                result.DestinationProcessor = 0;
            }

            if (offset + 2 <= responseSpan.Length)
            {
                result.MultiDropStation = SlmpResponseParserHelper.ParseHexByte(responseSpan.Slice(offset, 2));
                offset += 2;
            }
            else
            {
                result.MultiDropStation = 0;
            }

            // データ長とエンドコード（20文字応答では省略されている場合がある）
            if (offset + 8 <= responseSpan.Length)
            {
                // 通常の応答（22文字以上）
                result.DataLength = SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
                offset += 4;
                result.EndCode = (EndCode)SlmpResponseParserHelper.ParseHexUshort(responseSpan.Slice(offset, 4));
                offset += 4;
            }
            else
            {
                // 簡略化された応答（20文字）- データ長とエンドコードを既定値に設定
                result.DataLength = 0;
                result.EndCode = EndCode.Success;
            }

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