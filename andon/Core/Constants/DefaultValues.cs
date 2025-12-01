namespace Andon.Core.Constants
{
    /// <summary>
    /// アプリケーション全体で使用する既定値を定義します。
    /// これらの値は設定ファイルで明示的に指定されていない場合に使用されます。
    /// </summary>
    public static class DefaultValues
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 接続設定
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 既定の接続方式（TCP または UDP）
        /// </summary>
        public const string ConnectionMethod = "UDP";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // SLMP設定
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 既定のSLMPフレームバージョン（3E または 4E）
        /// </summary>
        public const string FrameVersion = "4E";

        /// <summary>
        /// 既定のタイムアウト値（ミリ秒単位）
        /// </summary>
        public const int TimeoutMs = 1000;

        /// <summary>
        /// 既定のタイムアウト値（SLMP単位: 250ms単位）
        /// TimeoutMs / 250 = 1000 / 250 = 4
        /// </summary>
        public const ushort TimeoutSlmp = 4;

        /// <summary>
        /// 既定の通信形式（true: Binary形式、false: ASCII形式）
        /// </summary>
        public const bool IsBinary = true;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // データ処理設定
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 既定のデータ取得間隔（ミリ秒単位）
        /// </summary>
        public const int MonitoringIntervalMs = 1000;
    }
}
