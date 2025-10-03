using System;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// レスポンス形式検出インターフェース
    /// 単一責任原則: フォーマット判定のみに特化
    /// </summary>
    public interface IResponseFormatDetector
    {
        /// <summary>
        /// レスポンスがバイナリ形式かどうかを判定
        /// </summary>
        /// <param name="responseFrame">判定対象のレスポンスフレーム</param>
        /// <returns>バイナリ形式の場合true、ASCII形式の場合false</returns>
        bool IsBinaryResponse(byte[] responseFrame);
    }
}