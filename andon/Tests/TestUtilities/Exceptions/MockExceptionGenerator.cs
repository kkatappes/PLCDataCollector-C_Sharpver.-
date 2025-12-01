using System;
using System.Net.Sockets;
using Andon.Core.Exceptions;

namespace Andon.Tests.TestUtilities.Exceptions
{
    /// <summary>
    /// テスト用例外生成ユーティリティ
    /// </summary>
    public static class MockExceptionGenerator
    {
        /// <summary>
        /// Step3エラー（接続失敗）を生成
        /// </summary>
        public static PlcConnectionException CreateStep3Error()
        {
            return new PlcConnectionException("接続失敗: PLCが応答しません");
        }

        /// <summary>
        /// Step3エラー（ソケット例外）を生成
        /// </summary>
        public static PlcConnectionException CreateStep3SocketError()
        {
            var socketException = new SocketException(10061); // Connection refused
            return new PlcConnectionException("接続失敗: ソケット接続エラー", socketException);
        }

        /// <summary>
        /// Step3エラー（タイムアウト）を生成
        /// </summary>
        public static PlcConnectionException CreateStep3TimeoutError()
        {
            var timeoutException = new TimeoutException("接続タイムアウト");
            return new PlcConnectionException("接続失敗: タイムアウトが発生しました", timeoutException);
        }

        /// <summary>
        /// Step4エラー（送信失敗）を生成
        /// </summary>
        public static PlcSendException CreateStep4Error()
        {
            return new PlcSendException("送信失敗: ソケットが切断されました");
        }

        /// <summary>
        /// Step4エラー（不正フレーム）を生成
        /// </summary>
        public static InvalidFrameException CreateStep4InvalidFrameError()
        {
            return new InvalidFrameException("フレーム長が不正です");
        }

        /// <summary>
        /// Step4エラー（ソケット例外）を生成
        /// </summary>
        public static PlcSendException CreateStep4SocketError()
        {
            var socketException = new SocketException(10054); // Connection reset by peer
            return new PlcSendException("送信失敗: ソケットエラー", socketException);
        }

        /// <summary>
        /// Step5エラー（受信失敗）を生成
        /// </summary>
        public static PlcReceiveException CreateStep5Error()
        {
            return new PlcReceiveException("受信失敗: タイムアウトが発生しました");
        }

        /// <summary>
        /// Step5エラー（受信タイムアウト）を生成
        /// </summary>
        public static PlcReceiveException CreateStep5TimeoutError()
        {
            var timeoutException = new TimeoutException("受信タイムアウト");
            return new PlcReceiveException("受信失敗: 受信タイムアウト", timeoutException);
        }

        /// <summary>
        /// Step5エラー（ソケット例外）を生成
        /// </summary>
        public static PlcReceiveException CreateStep5SocketError()
        {
            var socketException = new SocketException(10054); // Connection reset by peer
            return new PlcReceiveException("受信失敗: ソケット受信エラー", socketException);
        }

        /// <summary>
        /// Step6エラー（データ処理失敗）を生成
        /// </summary>
        public static PlcDataProcessingException CreateStep6Error()
        {
            return new PlcDataProcessingException("データ処理失敗: 不正なレスポンス形式です");
        }

        /// <summary>
        /// Step6エラー（データ解析失敗）を生成
        /// </summary>
        public static DataParseException CreateStep6ParseError()
        {
            return new DataParseException("レスポンスデータの形式が不正です");
        }

        /// <summary>
        /// Step6エラー（データ整合性エラー）を生成
        /// </summary>
        public static DataIntegrityException CreateStep6IntegrityError()
        {
            return new DataIntegrityException("受信データの整合性チェックに失敗しました");
        }

        /// <summary>
        /// 汎用例外を生成（予期しないエラー用）
        /// </summary>
        public static Exception CreateUnexpectedException()
        {
            return new InvalidOperationException("予期しないエラーが発生しました");
        }

        /// <summary>
        /// ArgumentNullException（設定不備）を生成
        /// </summary>
        public static ArgumentNullException CreateArgumentNullError(string paramName)
        {
            return new ArgumentNullException(paramName, $"必須パラメータ '{paramName}' がnullです");
        }

        /// <summary>
        /// ArgumentException（設定値不正）を生成
        /// </summary>
        public static ArgumentException CreateArgumentError(string paramName, string message)
        {
            return new ArgumentException($"パラメータ '{paramName}' の値が不正です: {message}", paramName);
        }
    }
}