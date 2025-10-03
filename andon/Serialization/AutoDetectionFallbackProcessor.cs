using System;
using SlmpClient.Constants;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// 自動判定フォールバック処理器
    /// 単一責任原則: フォールバック処理のみに特化
    /// </summary>
    public class AutoDetectionFallbackProcessor : IFallbackProcessor
    {
        private readonly IResponseFormatDetector _formatDetector;
        private readonly ISlmpResponseParser _parser;

        /// <summary>
        /// コンストラクタ
        /// 依存性逆転原則: 抽象（インターフェース）に依存
        /// </summary>
        /// <param name="formatDetector">フォーマット検出器</param>
        /// <param name="parser">レスポンス解析器</param>
        public AutoDetectionFallbackProcessor(IResponseFormatDetector formatDetector, ISlmpResponseParser parser)
        {
            _formatDetector = formatDetector ?? throw new ArgumentNullException(nameof(formatDetector));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        /// フォーマット判定エラー時のフォールバック処理
        /// 0xD0バイトエラーに対して自動判定による再試行を実行
        /// </summary>
        /// <param name="responseFrame">レスポンスフレーム</param>
        /// <param name="originalException">元の例外</param>
        /// <param name="isBinary">元の解析モード</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>フォールバック処理結果</returns>
        public SlmpResponse ProcessFallback(byte[] responseFrame, Exception originalException, bool isBinary, SlmpFrameVersion version)
        {
            if (responseFrame == null)
                throw new ArgumentNullException(nameof(responseFrame));

            // 0xD0バイトエラーの場合のみフォールバック処理を実行
            if (originalException is ArgumentException argEx && argEx.Message.Contains("無効な16進文字"))
            {
                // 自動判定で正しい形式を特定
                var detectedBinary = _formatDetector.IsBinaryResponse(responseFrame);

                // 検出された形式で再解析
                return _parser.ParseResponse(responseFrame, detectedBinary, version);
            }

            // その他の例外は再スロー
            throw originalException;
        }
    }
}