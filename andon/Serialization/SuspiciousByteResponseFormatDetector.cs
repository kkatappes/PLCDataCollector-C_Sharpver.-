using System;
using System.Linq;

namespace SlmpClient.Serialization
{
    /// <summary>
    /// 疑わしいバイト検出によるレスポンス形式判定器
    /// 戦略パターン: フォーマット判定の具体戦略
    /// 開放/閉鎖原則: 新しい判定戦略を追加可能
    /// </summary>
    public class SuspiciousByteResponseFormatDetector : IResponseFormatDetector
    {
        private readonly byte[] _suspiciousBytes;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="suspiciousBytes">疑わしいバイトパターン（nullの場合はデフォルト使用）</param>
        public SuspiciousByteResponseFormatDetector(byte[] suspiciousBytes = null)
        {
            _suspiciousBytes = suspiciousBytes ?? new byte[] { 0xD0, 0xDE, 0xAD, 0xBE, 0xEF };
        }

        /// <summary>
        /// レスポンスがバイナリ形式かどうかを判定
        /// 疑わしいバイトまたは非ASCII文字の存在をチェック
        /// </summary>
        /// <param name="responseFrame">判定対象のレスポンスフレーム</param>
        /// <returns>バイナリ形式の場合true、ASCII形式の場合false</returns>
        public bool IsBinaryResponse(byte[] responseFrame)
        {
            if (responseFrame == null || responseFrame.Length == 0)
                return false;

            // フレームの先頭部分をチェック（最大16バイト）
            var checkLength = Math.Min(16, responseFrame.Length);

            for (int i = 0; i < checkLength; i++)
            {
                var b = responseFrame[i];

                // 疑わしいバイトパターンをチェック
                if (_suspiciousBytes.Contains(b))
                    return true;

                // ASCII印字可能文字範囲外をチェック
                if (b < 0x20 || b > 0x7E)
                    return true;
            }

            return false;
        }
    }
}