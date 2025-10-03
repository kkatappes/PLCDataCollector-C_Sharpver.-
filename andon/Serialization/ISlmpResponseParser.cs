using System;
using SlmpClient.Constants;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// SLMPレスポンス解析インターフェース
    /// 単一責任原則: レスポンス解析のみに特化
    /// </summary>
    public interface ISlmpResponseParser
    {
        /// <summary>
        /// SLMPレスポンスフレームを解析
        /// </summary>
        /// <param name="responseFrame">受信したレスポンスフレーム</param>
        /// <param name="isBinary">バイナリモードかどうか</param>
        /// <param name="version">フレームバージョン</param>
        /// <returns>解析されたレスポンス</returns>
        SlmpResponse ParseResponse(byte[] responseFrame, bool isBinary, SlmpFrameVersion version);
    }
}