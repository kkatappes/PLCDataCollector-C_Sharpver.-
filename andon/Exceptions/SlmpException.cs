using System;
using SlmpClient.Constants;

namespace SlmpClient.Exceptions
{
    /// <summary>
    /// SLMP関連例外の基底クラス
    /// Python: util.SLMPError
    /// </summary>
    public class SlmpException : Exception
    {
        /// <summary>
        /// 関連するSLMPコマンド
        /// </summary>
        public SlmpCommand? Command { get; }
        
        /// <summary>
        /// シーケンスID（4Eフレーム使用時）
        /// </summary>
        public ushort? SequenceId { get; }
        
        /// <summary>
        /// 例外発生時刻
        /// </summary>
        public DateTime Timestamp { get; }
        
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public SlmpException() : base("SLMP operation failed")
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// メッセージ付きコンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        public SlmpException(string message) : base(message)
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// メッセージと内部例外付きコンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="innerException">内部例外</param>
        public SlmpException(string message, Exception innerException) : base(message, innerException)
        {
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 詳細コンテキスト情報付きコンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="command">関連するSLMPコマンド</param>
        /// <param name="sequenceId">シーケンスID</param>
        /// <param name="innerException">内部例外</param>
        public SlmpException(string message, SlmpCommand? command = null, ushort? sequenceId = null, Exception? innerException = null)
            : base(message, innerException)
        {
            Command = command;
            SequenceId = sequenceId;
            Timestamp = DateTime.UtcNow;
        }
        
        /// <summary>
        /// 例外の詳細情報を文字列として取得
        /// </summary>
        /// <returns>詳細情報文字列</returns>
        public override string ToString()
        {
            var baseMessage = base.ToString();
            
            if (Command.HasValue)
                baseMessage += $"\nCommand: {Command.Value}";
            
            if (SequenceId.HasValue)
                baseMessage += $"\nSequenceId: {SequenceId.Value}";
            
            baseMessage += $"\nTimestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC";
            
            return baseMessage;
        }
    }
    
    /// <summary>
    /// SLMP通信エラー例外
    /// Python: util.SLMPCommunicationError
    /// </summary>
    public class SlmpCommunicationException : SlmpException
    {
        /// <summary>
        /// SLMPエンドコード
        /// </summary>
        public EndCode EndCode { get; }
        
        /// <summary>
        /// レスポンスデータ（エラー時）
        /// </summary>
        public byte[] ResponseData { get; }
        
        /// <summary>
        /// デバイスコード（関連する場合）
        /// </summary>
        public DeviceCode? DeviceCode { get; }
        
        /// <summary>
        /// 開始アドレス（関連する場合）
        /// </summary>
        public uint? StartAddress { get; }
        
        /// <summary>
        /// データ個数（関連する場合）
        /// </summary>
        public ushort? Count { get; }
        
        /// <summary>
        /// 基本コンストラクタ
        /// </summary>
        /// <param name="endCode">SLMPエンドコード</param>
        public SlmpCommunicationException(EndCode endCode)
            : base($"SLMP communication error: {endCode} (0x{(ushort)endCode:X4}) - {endCode.GetJapaneseMessage()}")
        {
            EndCode = endCode;
            ResponseData = Array.Empty<byte>();
        }
        
        /// <summary>
        /// 文字列メッセージコンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        public SlmpCommunicationException(string message)
            : base(message)
        {
            EndCode = Constants.EndCode.OtherNetworkError;
            ResponseData = Array.Empty<byte>();
        }
        
        /// <summary>
        /// 文字列メッセージと内部例外コンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="innerException">内部例外</param>
        public SlmpCommunicationException(string message, Exception innerException)
            : base(message, innerException)
        {
            EndCode = Constants.EndCode.OtherNetworkError;
            ResponseData = Array.Empty<byte>();
        }
        
        /// <summary>
        /// レスポンスデータ付きコンストラクタ
        /// </summary>
        /// <param name="endCode">SLMPエンドコード</param>
        /// <param name="responseData">レスポンスデータ</param>
        public SlmpCommunicationException(EndCode endCode, byte[] responseData)
            : base($"SLMP communication error: {endCode} (0x{(ushort)endCode:X4}) - {endCode.GetJapaneseMessage()}")
        {
            EndCode = endCode;
            ResponseData = responseData ?? Array.Empty<byte>();
        }
        
        /// <summary>
        /// 詳細コンテキスト情報付きコンストラクタ
        /// </summary>
        /// <param name="endCode">SLMPエンドコード</param>
        /// <param name="responseData">レスポンスデータ</param>
        /// <param name="command">関連するSLMPコマンド</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">データ個数</param>
        /// <param name="sequenceId">シーケンスID</param>
        public SlmpCommunicationException(
            EndCode endCode,
            byte[] responseData,
            SlmpCommand? command = null,
            DeviceCode? deviceCode = null,
            uint? startAddress = null,
            ushort? count = null,
            ushort? sequenceId = null)
            : base($"SLMP communication error: {endCode} (0x{(ushort)endCode:X4}) - {endCode.GetJapaneseMessage()}", command, sequenceId)
        {
            EndCode = endCode;
            ResponseData = responseData ?? Array.Empty<byte>();
            DeviceCode = deviceCode;
            StartAddress = startAddress;
            Count = count;
        }
        
        /// <summary>
        /// 再試行可能かどうかを判定
        /// </summary>
        public bool IsRetryable => EndCode.IsRetryable() || EndCode.IsRetryWithDelay();
        
        /// <summary>
        /// 遅延後再試行推奨かどうかを判定
        /// </summary>
        public bool IsRetryWithDelay => EndCode.IsRetryWithDelay();
        
        /// <summary>
        /// エラーの重要度を取得
        /// </summary>
        public ErrorSeverity Severity => EndCode.GetSeverity();
        
        /// <summary>
        /// 例外の詳細情報を文字列として取得
        /// </summary>
        /// <returns>詳細情報文字列</returns>
        public override string ToString()
        {
            var baseMessage = base.ToString();
            
            baseMessage += $"\nEndCode: {EndCode} (0x{(ushort)EndCode:X4})";
            baseMessage += $"\nEndCode Message: {EndCode.GetJapaneseMessage()}";
            baseMessage += $"\nSeverity: {Severity}";
            baseMessage += $"\nIsRetryable: {IsRetryable}";
            
            if (DeviceCode.HasValue)
                baseMessage += $"\nDeviceCode: {DeviceCode.Value}";
            
            if (StartAddress.HasValue)
                baseMessage += $"\nStartAddress: {StartAddress.Value}";
            
            if (Count.HasValue)
                baseMessage += $"\nCount: {Count.Value}";
            
            if (ResponseData.Length > 0)
                baseMessage += $"\nResponseData Length: {ResponseData.Length} bytes";
            
            return baseMessage;
        }
    }
    
    /// <summary>
    /// SLMPタイムアウト例外
    /// Python: TimeoutError (from util.py での使用例)
    /// </summary>
    public class SlmpTimeoutException : SlmpException
    {
        /// <summary>
        /// 経過時間
        /// </summary>
        public TimeSpan ElapsedTime { get; }
        
        /// <summary>
        /// タイムアウト期間
        /// </summary>
        public TimeSpan TimeoutDuration { get; }
        
        /// <summary>
        /// デバイスコード（関連する場合）
        /// </summary>
        public DeviceCode? DeviceCode { get; }
        
        /// <summary>
        /// 開始アドレス（関連する場合）
        /// </summary>
        public uint? StartAddress { get; }
        
        /// <summary>
        /// データ個数（関連する場合）
        /// </summary>
        public ushort? Count { get; }
        
        /// <summary>
        /// 基本コンストラクタ
        /// </summary>
        /// <param name="elapsedTime">経過時間</param>
        /// <param name="timeoutDuration">タイムアウト期間</param>
        public SlmpTimeoutException(TimeSpan elapsedTime, TimeSpan timeoutDuration)
            : base($"SLMP operation timed out after {elapsedTime.TotalMilliseconds:F0}ms (timeout: {timeoutDuration.TotalMilliseconds:F0}ms)")
        {
            ElapsedTime = elapsedTime;
            TimeoutDuration = timeoutDuration;
        }
        
        /// <summary>
        /// 文字列メッセージコンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="timeoutDuration">タイムアウト期間</param>
        public SlmpTimeoutException(string message, TimeSpan timeoutDuration)
            : base(message)
        {
            ElapsedTime = timeoutDuration; // 簡易実装：経過時間=タイムアウト期間
            TimeoutDuration = timeoutDuration;
        }
        
        /// <summary>
        /// 詳細コンテキスト情報付きコンストラクタ
        /// </summary>
        /// <param name="elapsedTime">経過時間</param>
        /// <param name="timeoutDuration">タイムアウト期間</param>
        /// <param name="command">関連するSLMPコマンド</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="startAddress">開始アドレス</param>
        /// <param name="count">データ個数</param>
        /// <param name="sequenceId">シーケンスID</param>
        /// <param name="innerException">内部例外</param>
        public SlmpTimeoutException(
            TimeSpan elapsedTime,
            TimeSpan timeoutDuration,
            SlmpCommand? command = null,
            DeviceCode? deviceCode = null,
            uint? startAddress = null,
            ushort? count = null,
            ushort? sequenceId = null,
            Exception? innerException = null)
            : base($"SLMP operation timed out after {elapsedTime.TotalMilliseconds:F0}ms (timeout: {timeoutDuration.TotalMilliseconds:F0}ms)", command, sequenceId, innerException)
        {
            ElapsedTime = elapsedTime;
            TimeoutDuration = timeoutDuration;
            DeviceCode = deviceCode;
            StartAddress = startAddress;
            Count = count;
        }
        
        /// <summary>
        /// タイムアウト率を取得（経過時間 / タイムアウト期間）
        /// </summary>
        public double TimeoutRatio => ElapsedTime.TotalMilliseconds / TimeoutDuration.TotalMilliseconds;
        
        /// <summary>
        /// 例外の詳細情報を文字列として取得
        /// </summary>
        /// <returns>詳細情報文字列</returns>
        public override string ToString()
        {
            var baseMessage = base.ToString();
            
            baseMessage += $"\nElapsedTime: {ElapsedTime.TotalMilliseconds:F0}ms";
            baseMessage += $"\nTimeoutDuration: {TimeoutDuration.TotalMilliseconds:F0}ms";
            baseMessage += $"\nTimeoutRatio: {TimeoutRatio:P1}";
            
            if (DeviceCode.HasValue)
                baseMessage += $"\nDeviceCode: {DeviceCode.Value}";
            
            if (StartAddress.HasValue)
                baseMessage += $"\nStartAddress: {StartAddress.Value}";
            
            if (Count.HasValue)
                baseMessage += $"\nCount: {Count.Value}";
            
            return baseMessage;
        }
    }
    
    /// <summary>
    /// SLMP接続エラー例外
    /// </summary>
    public class SlmpConnectionException : SlmpException
    {
        /// <summary>
        /// 接続先アドレス
        /// </summary>
        public string Address { get; }
        
        /// <summary>
        /// 接続先ポート
        /// </summary>
        public int Port { get; }
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="address">接続先アドレス</param>
        /// <param name="port">接続先ポート</param>
        /// <param name="innerException">内部例外</param>
        public SlmpConnectionException(string message, string address, int port, Exception? innerException = null)
            : base($"SLMP connection error to {address}:{port} - {message}", innerException!)
        {
            Address = address;
            Port = port;
        }
        
        /// <summary>
        /// 例外の詳細情報を文字列として取得
        /// </summary>
        /// <returns>詳細情報文字列</returns>
        public override string ToString()
        {
            var baseMessage = base.ToString();
            baseMessage += $"\nAddress: {Address}";
            baseMessage += $"\nPort: {Port}";
            return baseMessage;
        }
    }
}