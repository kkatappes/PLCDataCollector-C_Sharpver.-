using System;

namespace Andon.Core.Exceptions
{
    /// <summary>
    /// PLC通信の基底例外クラス
    /// </summary>
    public abstract class PlcCommunicationException : Exception
    {
        public string ErrorCode { get; }
        public string FailedStep { get; }

        protected PlcCommunicationException(string message, string errorCode, string failedStep)
            : base(message)
        {
            ErrorCode = errorCode;
            FailedStep = failedStep;
        }

        protected PlcCommunicationException(string message, Exception innerException, string errorCode, string failedStep)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            FailedStep = failedStep;
        }
    }

    /// <summary>
    /// PLC接続エラー例外（Step3）
    /// </summary>
    public class PlcConnectionException : PlcCommunicationException
    {
        public PlcConnectionException(string message)
            : base(message, "E001", "Step3")
        {
        }

        public PlcConnectionException(string message, Exception innerException)
            : base(message, innerException, "E001", "Step3")
        {
        }
    }

    /// <summary>
    /// PLC送信エラー例外（Step4）
    /// </summary>
    public class PlcSendException : PlcCommunicationException
    {
        public PlcSendException(string message)
            : base(message, "E002", "Step4")
        {
        }

        public PlcSendException(string message, Exception innerException)
            : base(message, innerException, "E002", "Step4")
        {
        }
    }

    /// <summary>
    /// PLC受信エラー例外（Step5）
    /// </summary>
    public class PlcReceiveException : PlcCommunicationException
    {
        public PlcReceiveException(string message)
            : base(message, "E003", "Step5")
        {
        }

        public PlcReceiveException(string message, Exception innerException)
            : base(message, innerException, "E003", "Step5")
        {
        }
    }

    /// <summary>
    /// PLCデータ処理エラー例外（Step6）
    /// </summary>
    public class PlcDataProcessingException : PlcCommunicationException
    {
        public PlcDataProcessingException(string message)
            : base(message, "E004", "Step6")
        {
        }

        public PlcDataProcessingException(string message, Exception innerException)
            : base(message, innerException, "E004", "Step6")
        {
        }
    }

    /// <summary>
    /// 不正フレーム形式例外
    /// </summary>
    public class InvalidFrameException : PlcSendException
    {
        public InvalidFrameException(string message)
            : base($"不正フレーム形式: {message}")
        {
        }
    }

    /// <summary>
    /// データ解析エラー例外
    /// </summary>
    public class DataParseException : PlcDataProcessingException
    {
        public DataParseException(string message)
            : base($"データ解析エラー: {message}")
        {
        }
    }

    /// <summary>
    /// データ整合性エラー例外
    /// </summary>
    public class DataIntegrityException : PlcDataProcessingException
    {
        public DataIntegrityException(string message)
            : base($"データ整合性エラー: {message}")
        {
        }
    }
}