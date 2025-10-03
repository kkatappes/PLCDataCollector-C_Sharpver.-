using System;
using SlmpClient.Constants;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// フォールバック処理インターフェース
    /// 単一責任原則: フォールバック処理のみに特化
    /// </summary>
    public interface IFallbackProcessor
    {
        /// <summary>
        /// フォーマット判定エラー時のフォールバック処理
        /// </summary>
        /// <param name="responseFrame">レスポンスフレーム</param>
        /// <param name="originalException">元の例外</param>
        /// <param name="isBinary">元の解析モード</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>フォールバック処理結果</returns>
        SlmpResponse ProcessFallback(byte[] responseFrame, Exception originalException, bool isBinary, SlmpFrameVersion version);
    }
}