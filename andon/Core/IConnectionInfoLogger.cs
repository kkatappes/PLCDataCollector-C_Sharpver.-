namespace SlmpClient.Core
{
    /// <summary>
    /// 接続情報ログインターフェース
    /// SOLID原則: インターフェース分離原則適用
    /// </summary>
    public interface IConnectionInfoLogger
    {
        /// <summary>接続情報をログに記録</summary>
        void LogConnectionInfo(string targetAddress, int port, bool isConnected);
    }
}