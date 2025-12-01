namespace Andon.Core.Models
{
    /// <summary>
    /// PLC切断結果を表すクラス
    /// </summary>
    public class DisconnectResult
    {
        /// <summary>
        /// 切断ステータス
        /// </summary>
        public DisconnectStatus Status { get; set; }

        /// <summary>
        /// 接続統計情報（切断成功時のみ設定）
        /// </summary>
        public ConnectionStats? ConnectionStats { get; set; }

        /// <summary>
        /// 切断結果メッセージ
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 切断ステータス
    /// </summary>
    public enum DisconnectStatus
    {
        /// <summary>
        /// 切断成功
        /// </summary>
        Success,

        /// <summary>
        /// 切断失敗
        /// </summary>
        Failed,

        /// <summary>
        /// 未接続状態
        /// </summary>
        NotConnected
    }
}