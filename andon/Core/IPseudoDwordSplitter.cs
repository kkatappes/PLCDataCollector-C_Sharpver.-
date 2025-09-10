using System;
using System.Collections.Generic;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// 擬似ダブルワード分割・結合インターフェース
    /// テスト可能な設計のための依存性注入対応
    /// </summary>
    public interface IPseudoDwordSplitter
    {
        /// <summary>
        /// DWordデバイスをWordペアに分割
        /// </summary>
        /// <param name="dwordDevices">分割対象のDWordデバイス群</param>
        /// <returns>分割されたWordペア群</returns>
        /// <exception cref="ArgumentException">アドレス境界違反時</exception>
        IList<WordPair> SplitDwordToWordPairs(
            IEnumerable<(DeviceCode deviceCode, uint address, uint value)> dwordDevices);

        /// <summary>
        /// WordペアをDWordデバイスに結合
        /// </summary>
        /// <param name="wordPairs">結合対象のWordペア群</param>
        /// <returns>結合されたDWordデバイス群</returns>
        IList<(DeviceCode deviceCode, uint address, uint value)> CombineWordPairsToDword(
            IEnumerable<WordPair> wordPairs);
    }

    /// <summary>
    /// Wordペアを表すデータ構造
    /// 不変オブジェクトとして設計（純粋関数パターン）
    /// </summary>
    public record WordPair
    {
        /// <summary>下位ワード (Low Word)</summary>
        public required (DeviceCode deviceCode, uint address, ushort value) LowWord { get; init; }
        
        /// <summary>上位ワード (High Word)</summary>
        public required (DeviceCode deviceCode, uint address, ushort value) HighWord { get; init; }
    }
}