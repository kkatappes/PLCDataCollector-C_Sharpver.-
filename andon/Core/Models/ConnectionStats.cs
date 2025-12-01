namespace Andon.Core.Models;

/// <summary>
/// PLC通信統計情報
/// 複数サイクル実行時の統計累積をサポート
/// </summary>
public class ConnectionStats
{
    private readonly object _lock = new object();

    /// <summary>
    /// 接続開始時刻
    /// </summary>
    public DateTime ConnectedAt { get; set; }

    /// <summary>
    /// 切断時刻
    /// </summary>
    public DateTime DisconnectedAt { get; set; }

    /// <summary>
    /// 総接続時間
    /// </summary>
    public TimeSpan TotalConnectionTime { get; set; }

    /// <summary>
    /// 送信フレーム数
    /// </summary>
    public int TotalFramesSent { get; set; }

    /// <summary>
    /// 受信フレーム数
    /// </summary>
    public int TotalFramesReceived { get; set; }

    /// <summary>
    /// 送信エラー数
    /// </summary>
    public int SendErrorCount { get; set; }

    /// <summary>
    /// 受信エラー数
    /// </summary>
    public int ReceiveErrorCount { get; set; }

    // ===== TC122: 複数サイクル統計累積機能 =====

    /// <summary>
    /// 総接続試行回数（リトライ含む）
    /// </summary>
    public int TotalConnections { get; private set; }

    /// <summary>
    /// 成功した接続回数
    /// </summary>
    public int SuccessfulConnections { get; private set; }

    /// <summary>
    /// 総切断回数
    /// </summary>
    public int TotalDisconnections { get; private set; }

    /// <summary>
    /// レスポンス時間履歴（ミリ秒）
    /// </summary>
    public List<double> ResponseTimeHistory { get; private set; } = new List<double>();

    /// <summary>
    /// 平均レスポンス時間（ミリ秒）
    /// </summary>
    public double AverageResponseTime =>
        ResponseTimeHistory.Count > 0 ? ResponseTimeHistory.Average() : 0;

    /// <summary>
    /// 最短レスポンス時間（ミリ秒）
    /// </summary>
    public double MinResponseTime =>
        ResponseTimeHistory.Count > 0 ? ResponseTimeHistory.Min() : 0;

    /// <summary>
    /// 最長レスポンス時間（ミリ秒）
    /// </summary>
    public double MaxResponseTime =>
        ResponseTimeHistory.Count > 0 ? ResponseTimeHistory.Max() : 0;

    /// <summary>
    /// レスポンス時間の標準偏差（ミリ秒）
    /// </summary>
    public double ResponseTimeStandardDeviation { get; private set; }

    /// <summary>
    /// エラー総数
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// 接続エラー総数
    /// </summary>
    public int ConnectionErrors { get; private set; }

    /// <summary>
    /// タイムアウトエラー数
    /// </summary>
    public int TimeoutErrors { get; private set; }

    /// <summary>
    /// 接続拒否エラー数
    /// </summary>
    public int RefusedErrors { get; private set; }

    /// <summary>
    /// 検証エラー数
    /// </summary>
    public int ValidationErrors { get; private set; }

    /// <summary>
    /// ネットワークエラー数
    /// </summary>
    public int NetworkErrors { get; private set; }

    /// <summary>
    /// 総エラー試行回数（旧仕様との互換性のため維持）
    /// </summary>
    public int TotalErrors => ErrorCount;

    /// <summary>
    /// 総試行回数（旧仕様との互換性のため維持）
    /// </summary>
    public int TotalAttempts => TotalConnections;

    /// <summary>
    /// リトライ総数
    /// </summary>
    public int TotalRetries { get; private set; }

    /// <summary>
    /// リトライ履歴
    /// </summary>
    public List<RetryRecord> RetryHistory { get; private set; } = new List<RetryRecord>();

    /// <summary>
    /// 接続成功率（%）
    /// </summary>
    public double SuccessRate =>
        TotalConnections > 0 ? (double)SuccessfulConnections / TotalConnections * 100 : 0;

    /// <summary>
    /// 総受信レスポンス数
    /// </summary>
    public int TotalResponsesReceived { get; private set; }

    /// <summary>
    /// 接続を追加（統計更新）
    /// </summary>
    /// <param name="successful">接続が成功したかどうか</param>
    public void AddConnection(bool successful)
    {
        lock (_lock)
        {
            TotalConnections++;
            if (successful)
            {
                SuccessfulConnections++;
            }
        }
    }

    /// <summary>
    /// 切断を記録（統計更新）
    /// </summary>
    public void AddDisconnection()
    {
        lock (_lock)
        {
            TotalDisconnections++;
        }
    }

    /// <summary>
    /// フレーム送信を記録（統計更新）
    /// </summary>
    public void AddFrameSent()
    {
        lock (_lock)
        {
            TotalFramesSent++;
        }
    }

    /// <summary>
    /// レスポンス受信を記録（統計更新）
    /// </summary>
    /// <param name="responseTimeMs">レスポンス時間（ミリ秒）</param>
    public void AddResponseReceived(double responseTimeMs)
    {
        lock (_lock)
        {
            TotalResponsesReceived++;
            ResponseTimeHistory.Add(responseTimeMs);
            RecalculateStatistics();
        }
    }

    /// <summary>
    /// エラーを記録（統計更新）
    /// </summary>
    /// <param name="errorType">エラー種別</param>
    public void AddError(string errorType)
    {
        lock (_lock)
        {
            ErrorCount++;
            // 送信エラー・受信エラーの分類は既存プロパティを使用
            if (errorType.Contains("Send") || errorType.Contains("送信"))
            {
                SendErrorCount++;
            }
            else if (errorType.Contains("Receive") || errorType.Contains("受信"))
            {
                ReceiveErrorCount++;
            }
        }
    }

    /// <summary>
    /// 接続エラーを記録（統計更新）
    /// TC124用の詳細エラー統計
    /// </summary>
    /// <param name="errorType">エラー種別 ("Timeout", "Refused", "Validation", "Network")</param>
    public void AddConnectionError(string errorType)
    {
        lock (_lock)
        {
            ErrorCount++;
            ConnectionErrors++;

            switch (errorType)
            {
                case "Timeout":
                    TimeoutErrors++;
                    break;
                case "Refused":
                    RefusedErrors++;
                    break;
                case "Validation":
                    ValidationErrors++;
                    break;
                case "Network":
                    NetworkErrors++;
                    break;
            }
        }
    }

    /// <summary>
    /// リトライを記録（統計更新）
    /// </summary>
    /// <param name="reason">リトライ理由</param>
    /// <param name="attemptNumber">試行回数</param>
    public void AddRetry(string reason, int attemptNumber)
    {
        lock (_lock)
        {
            TotalRetries++;
            RetryHistory.Add(new RetryRecord
            {
                Reason = reason,
                AttemptNumber = attemptNumber,
                RetryAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// 統計再計算（標準偏差等）
    /// </summary>
    private void RecalculateStatistics()
    {
        if (ResponseTimeHistory.Count == 0)
        {
            ResponseTimeStandardDeviation = 0;
            return;
        }

        // 標準偏差計算
        double average = ResponseTimeHistory.Average();
        double sumOfSquares = ResponseTimeHistory.Sum(x => Math.Pow(x - average, 2));
        double variance = sumOfSquares / ResponseTimeHistory.Count;
        ResponseTimeStandardDeviation = Math.Sqrt(variance);
    }

    /// <summary>
    /// 統計情報をリセット
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            TotalConnections = 0;
            SuccessfulConnections = 0;
            TotalDisconnections = 0;
            TotalFramesSent = 0;
            TotalFramesReceived = 0;
            TotalResponsesReceived = 0;
            ErrorCount = 0;
            SendErrorCount = 0;
            ReceiveErrorCount = 0;
            ConnectionErrors = 0;
            TimeoutErrors = 0;
            RefusedErrors = 0;
            ValidationErrors = 0;
            NetworkErrors = 0;
            TotalRetries = 0;
            ResponseTimeHistory.Clear();
            RetryHistory.Clear();
            ResponseTimeStandardDeviation = 0;
        }
    }

    /// <summary>
    /// 統計情報のスナップショット（コピー）を作成
    /// TC122: 複数サイクルテストで初期値と最終値を比較するために使用
    /// </summary>
    public ConnectionStats Clone()
    {
        lock (_lock)
        {
            return new ConnectionStats
            {
                ConnectedAt = this.ConnectedAt,
                DisconnectedAt = this.DisconnectedAt,
                TotalConnectionTime = this.TotalConnectionTime,
                TotalFramesSent = this.TotalFramesSent,
                TotalFramesReceived = this.TotalFramesReceived,
                SendErrorCount = this.SendErrorCount,
                ReceiveErrorCount = this.ReceiveErrorCount,
                TotalConnections = this.TotalConnections,
                SuccessfulConnections = this.SuccessfulConnections,
                TotalDisconnections = this.TotalDisconnections,
                ResponseTimeHistory = new List<double>(this.ResponseTimeHistory),
                ErrorCount = this.ErrorCount,
                ConnectionErrors = this.ConnectionErrors,
                TimeoutErrors = this.TimeoutErrors,
                RefusedErrors = this.RefusedErrors,
                ValidationErrors = this.ValidationErrors,
                NetworkErrors = this.NetworkErrors,
                TotalRetries = this.TotalRetries,
                RetryHistory = new List<RetryRecord>(this.RetryHistory),
                TotalResponsesReceived = this.TotalResponsesReceived
            };
        }
    }
}

/// <summary>
/// リトライ記録
/// </summary>
public class RetryRecord
{
    /// <summary>
    /// リトライ理由
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 試行回数
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// リトライ発生時刻
    /// </summary>
    public DateTime RetryAt { get; set; }
}
