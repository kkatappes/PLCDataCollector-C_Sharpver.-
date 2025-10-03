# ランタイム診断ガイド

## 環境変数による診断スイッチ

### 診断レベル設定

#### 基本的な診断レベル
```bash
# 環境変数設定例
export SLMP_DIAGNOSTIC_LEVEL=DEBUG           # デバッグ情報を有効化
export SLMP_FRAME_LOGGING=true               # フレーム詳細ログを有効化
export SLMP_PERFORMANCE_COUNTERS=true        # パフォーマンスカウンターを有効化
export SLMP_MEMORY_DIAGNOSTICS=true          # メモリ診断を有効化

# Windows (PowerShell)
$env:SLMP_DIAGNOSTIC_LEVEL="DEBUG"
$env:SLMP_FRAME_LOGGING="true"
$env:SLMP_PERFORMANCE_COUNTERS="true"
$env:SLMP_MEMORY_DIAGNOSTICS="true"

# Windows (コマンドプロンプト)
set SLMP_DIAGNOSTIC_LEVEL=DEBUG
set SLMP_FRAME_LOGGING=true
set SLMP_PERFORMANCE_COUNTERS=true
set SLMP_MEMORY_DIAGNOSTICS=true
```

#### アプリケーション設定での有効化
```csharp
public class SlmpDiagnosticSettings
{
    public DiagnosticLevel Level { get; set; } = DiagnosticLevel.Information;
    public bool EnableFrameLogging { get; set; } = false;
    public bool EnablePerformanceCounters { get; set; } = false;
    public bool EnableMemoryDiagnostics { get; set; } = false;
    public bool EnableNetworkTracing { get; set; } = false;
    public string? LogDirectory { get; set; }
    public int MaxLogFileSizeMB { get; set; } = 100;
    public int MaxLogFiles { get; set; } = 10;
    
    public static SlmpDiagnosticSettings FromEnvironment()
    {
        var settings = new SlmpDiagnosticSettings();
        
        if (Enum.TryParse<DiagnosticLevel>(Environment.GetEnvironmentVariable("SLMP_DIAGNOSTIC_LEVEL"), out var level))
            settings.Level = level;
            
        if (bool.TryParse(Environment.GetEnvironmentVariable("SLMP_FRAME_LOGGING"), out var frameLogging))
            settings.EnableFrameLogging = frameLogging;
            
        if (bool.TryParse(Environment.GetEnvironmentVariable("SLMP_PERFORMANCE_COUNTERS"), out var perfCounters))
            settings.EnablePerformanceCounters = perfCounters;
            
        if (bool.TryParse(Environment.GetEnvironmentVariable("SLMP_MEMORY_DIAGNOSTICS"), out var memDiag))
            settings.EnableMemoryDiagnostics = memDiag;
            
        settings.LogDirectory = Environment.GetEnvironmentVariable("SLMP_LOG_DIRECTORY") ?? "logs";
        
        return settings;
    }
}

public enum DiagnosticLevel
{
    None,
    Error,
    Warning, 
    Information,
    Debug,
    Trace
}
```

### 診断機能の統合
```csharp
public class SlmpClient
{
    private readonly SlmpDiagnosticSettings _diagnostics;
    private readonly ILogger<SlmpClient> _logger;
    private readonly DiagnosticSource _diagnosticSource;
    private readonly SlmpPerformanceCounters? _performanceCounters;
    private readonly SlmpFrameLogger? _frameLogger;
    
    public SlmpClient(string address, SlmpConnectionSettings? settings = null, SlmpDiagnosticSettings? diagnostics = null)
    {
        _diagnostics = diagnostics ?? SlmpDiagnosticSettings.FromEnvironment();
        
        // 診断機能の初期化
        if (_diagnostics.EnablePerformanceCounters)
            _performanceCounters = new SlmpPerformanceCounters();
            
        if (_diagnostics.EnableFrameLogging)
            _frameLogger = new SlmpFrameLogger(_diagnostics);
            
        _diagnosticSource = new DiagnosticListener("SlmpClient");
    }
}
```

## Microsoft.Extensions.Logging統合

### 構造化ログ設定
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SlmpClient": "Debug",
      "SlmpClient.Transport": "Trace",
      "SlmpClient.Protocol": "Debug"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss.fff ",
      "LogToStandardErrorThreshold": "Warning"
    },
    "File": {
      "Path": "logs/slmp-{Date}.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30,
      "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj} {Properties:j} {Scopes}{NewLine}{Exception}",
      "MinimumLevel": {
        "Default": "Debug",
        "Override": {
          "SlmpClient.Transport.TcpTransport": "Trace"
        }
      }
    },
    "Seq": {
      "ServerUrl": "http://localhost:5341",
      "ApiKey": "your-api-key-here",
      "MinimumLevel": "Information"
    }
  }
}
```

### 高度なログ実装
```csharp
public class SlmpEnhancedLogging
{
    private readonly ILogger _logger;
    private readonly DiagnosticSource _diagnosticSource;
    private readonly ActivitySource _activitySource;
    
    // 高性能ログメッセージ定義
    private static readonly Action<ILogger, string, DeviceCode, uint, ushort, long, Exception?> LogDeviceRead
        = LoggerMessage.Define<string, DeviceCode, uint, ushort, long>(
            LogLevel.Information,
            new EventId(1001, "DeviceRead"),
            "{Operation} completed: DeviceCode={DeviceCode}, StartAddress={StartAddress}, Count={Count}, Duration={Duration}ms");
    
    private static readonly Action<ILogger, string, EndCode, ushort?, SlmpCommand?, long, Exception?> LogCommunicationError
        = LoggerMessage.Define<string, EndCode, ushort?, SlmpCommand?, long>(
            LogLevel.Error,
            new EventId(2001, "CommunicationError"),
            "{Operation} failed: EndCode={EndCode}, Sequence={Sequence}, Command={Command}, Duration={Duration}ms");
    
    public async Task<T> LogOperationAsync<T>(string operationName, Func<Task<T>> operation, object? parameters = null)
    {
        using var activity = _activitySource.StartActivity(operationName);
        activity?.SetTag("operation.name", operationName);
        
        if (parameters != null)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                activity?.SetTag($"parameter.{prop.Name.ToLowerInvariant()}", prop.GetValue(parameters)?.ToString());
            }
        }
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["OperationName"] = operationName,
            ["CorrelationId"] = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            ["Parameters"] = parameters ?? new { }
        });
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Starting operation {OperationName}", operationName);
            
            var result = await operation();
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            LogDeviceRead(_logger, operationName, default, 0, 0, stopwatch.ElapsedMilliseconds, null);
            
            return result;
        }
        catch (SlmpCommunicationException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", "SlmpCommunicationException");
            activity?.SetTag("error.endcode", ex.EndCode.ToString());
            
            LogCommunicationError(_logger, operationName, ex.EndCode, ex.Sequence, ex.Command, stopwatch.ElapsedMilliseconds, ex);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            _logger.LogError(ex, "Operation {OperationName} failed after {Duration}ms", operationName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### EventSource統合
```csharp
[EventSource(Name = "SlmpClient")]
public sealed class SlmpEventSource : EventSource
{
    public static readonly SlmpEventSource Log = new();
    
    [Event(1, Level = EventLevel.Informational, Message = "Connection established to {0}:{1} in {2}ms")]
    public void ConnectionEstablished(string address, int port, long durationMs)
    {
        WriteEvent(1, address, port, durationMs);
    }
    
    [Event(2, Level = EventLevel.Warning, Message = "Connection attempt failed to {0}:{1}, retry {2}/{3}")]
    public void ConnectionRetry(string address, int port, int attempt, int maxAttempts)
    {
        WriteEvent(2, address, port, attempt, maxAttempts);
    }
    
    [Event(3, Level = EventLevel.Error, Message = "SLMP communication error: EndCode={0}, Command={1}")]
    public void CommunicationError(ushort endCode, ushort command)
    {
        WriteEvent(3, endCode, command);
    }
    
    [Event(4, Level = EventLevel.Verbose, Message = "Frame sent: Length={0}, Command={1}, Sequence={2}")]
    public void FrameSent(int length, ushort command, ushort sequence)
    {
        WriteEvent(4, length, command, sequence);
    }
    
    [Event(5, Level = EventLevel.Verbose, Message = "Frame received: Length={0}, EndCode={1}, Sequence={2}")]
    public void FrameReceived(int length, ushort endCode, ushort sequence)
    {
        WriteEvent(5, length, endCode, sequence);
    }
    
    [Event(6, Level = EventLevel.Informational, Message = "Performance: Operation={0}, Duration={1}ms, Throughput={2} ops/sec")]
    public void Performance(string operation, double durationMs, double throughput)
    {
        WriteEvent(6, operation, durationMs, throughput);
    }
}
```

## パフォーマンス計測とメトリクス

### OpenTelemetry統合
```csharp
public class SlmpTelemetry
{
    private readonly Meter _meter;
    private readonly Counter<long> _operationCounter;
    private readonly Histogram<double> _operationDuration; 
    private readonly Counter<long> _errorCounter;
    private readonly Gauge<int> _activeConnections;
    
    public SlmpTelemetry()
    {
        _meter = new Meter("SlmpClient", "1.0.0");
        
        _operationCounter = _meter.CreateCounter<long>(
            "slmp_operations_total",
            "count",
            "Total number of SLMP operations");
            
        _operationDuration = _meter.CreateHistogram<double>(
            "slmp_operation_duration",
            "ms", 
            "Duration of SLMP operations");
            
        _errorCounter = _meter.CreateCounter<long>(
            "slmp_errors_total",
            "count",
            "Total number of SLMP errors");
            
        _activeConnections = _meter.CreateGauge<int>(
            "slmp_active_connections",
            "count",
            "Number of active SLMP connections");
    }
    
    public void RecordOperation(string operationName, double durationMs, bool success, string? errorType = null)
    {
        var tags = new TagList
        {
            ["operation"] = operationName,
            ["success"] = success.ToString().ToLower()
        };
        
        _operationCounter.Add(1, tags);
        _operationDuration.Record(durationMs, tags);
        
        if (!success && errorType != null)
        {
            _errorCounter.Add(1, new TagList 
            { 
                ["operation"] = operationName,
                ["error_type"] = errorType 
            });
        }
    }
    
    public void UpdateActiveConnections(int count)
    {
        _activeConnections.Record(count);
    }
}
```

### パフォーマンスカウンター実装
```csharp
public class SlmpPerformanceCounters : IDisposable
{
    private readonly PerformanceCounter _operationsPerSecond;
    private readonly PerformanceCounter _averageResponseTime;
    private readonly PerformanceCounter _errorRate;
    private readonly PerformanceCounter _activeConnections;
    private readonly Timer _updateTimer;
    
    public SlmpPerformanceCounters()
    {
        var categoryName = "SlmpClient";
        
        // カテゴリが存在しない場合は作成
        if (!PerformanceCounterCategory.Exists(categoryName))
        {
            CreatePerformanceCounters(categoryName);
        }
        
        _operationsPerSecond = new PerformanceCounter(categoryName, "Operations/sec", false);
        _averageResponseTime = new PerformanceCounter(categoryName, "Average Response Time", false);
        _errorRate = new PerformanceCounter(categoryName, "Error Rate", false);
        _activeConnections = new PerformanceCounter(categoryName, "Active Connections", false);
        
        _updateTimer = new Timer(UpdateCounters, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }
    
    private static void CreatePerformanceCounters(string categoryName)
    {
        var counters = new CounterCreationDataCollection
        {
            new("Operations/sec", "Number of operations per second", PerformanceCounterType.RateOfCountsPerSecond32),
            new("Average Response Time", "Average response time in milliseconds", PerformanceCounterType.NumberOfItems32),
            new("Error Rate", "Percentage of failed operations", PerformanceCounterType.RawFraction),
            new("Active Connections", "Number of active connections", PerformanceCounterType.NumberOfItems32)
        };
        
        PerformanceCounterCategory.Create(categoryName, "SlmpClient Performance Counters", PerformanceCounterCategoryType.SingleInstance, counters);
    }
    
    private void UpdateCounters(object? state)
    {
        // カウンター値の更新処理
        _operationsPerSecond.NextValue();
        _averageResponseTime.RawValue = (long)_averageResponseTimeMs;
        _errorRate.RawValue = (long)(_errorRate * 100);
        _activeConnections.RawValue = _activeConnectionCount;
    }
    
    public void Dispose()
    {
        _updateTimer?.Dispose();
        _operationsPerSecond?.Dispose();
        _averageResponseTime?.Dispose();
        _errorRate?.Dispose();
        _activeConnections?.Dispose();
    }
}
```

## ネットワーク診断

### Wireshark パケットキャプチャ自動化
```csharp
public class NetworkDiagnostics
{
    public static async Task<string> CapturePacketsAsync(string interfaceName, string filterExpression, int durationSeconds)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var captureFile = $"slmp_capture_{timestamp}.pcap";
        
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "tshark",
            Arguments = $"-i {interfaceName} -f \"{filterExpression}\" -a duration:{durationSeconds} -w {captureFile}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(processStartInfo);
        if (process == null) throw new InvalidOperationException("Failed to start tshark");
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"tshark failed: {error}");
        }
        
        return captureFile;
    }
    
    public static async Task<NetworkStatistics> AnalyzeCaptureAsync(string captureFile)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "tshark", 
            Arguments = $"-r {captureFile} -T fields -e frame.time_relative -e ip.src -e ip.dst -e tcp.srcport -e tcp.dstport -e frame.len",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(processStartInfo);
        if (process == null) throw new InvalidOperationException("Failed to start tshark");
        
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return ParseNetworkStatistics(output);
    }
    
    private static NetworkStatistics ParseNetworkStatistics(string tsharkOutput)
    {
        var lines = tsharkOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var packets = new List<PacketInfo>();
        
        foreach (var line in lines)
        {
            var fields = line.Split('\t');
            if (fields.Length >= 6)
            {
                packets.Add(new PacketInfo
                {
                    Timestamp = double.Parse(fields[0]),
                    SourceIP = fields[1],
                    DestIP = fields[2], 
                    SourcePort = int.Parse(fields[3]),
                    DestPort = int.Parse(fields[4]),
                    Length = int.Parse(fields[5])
                });
            }
        }
        
        return new NetworkStatistics
        {
            TotalPackets = packets.Count,
            TotalBytes = packets.Sum(p => p.Length),
            AveragePacketSize = packets.Any() ? packets.Average(p => p.Length) : 0,
            Duration = packets.Any() ? packets.Max(p => p.Timestamp) - packets.Min(p => p.Timestamp) : 0,
            PacketsPerSecond = packets.Any() ? packets.Count / (packets.Max(p => p.Timestamp) - packets.Min(p => p.Timestamp)) : 0
        };
    }
}

public record PacketInfo
{
    public double Timestamp { get; init; }
    public string SourceIP { get; init; } = "";
    public string DestIP { get; init; } = "";
    public int SourcePort { get; init; }
    public int DestPort { get; init; }
    public int Length { get; init; }
}

public record NetworkStatistics
{
    public int TotalPackets { get; init; }
    public long TotalBytes { get; init; }
    public double AveragePacketSize { get; init; }
    public double Duration { get; init; }
    public double PacketsPerSecond { get; init; }
}
```

### 自動診断レポート生成
```csharp
public class DiagnosticReportGenerator
{
    public async Task<DiagnosticReport> GenerateReportAsync(string connectionString, TimeSpan duration)
    {
        var report = new DiagnosticReport
        {
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            ConnectionString = connectionString
        };
        
        // 接続テスト
        report.ConnectionTest = await TestConnectionAsync(connectionString);
        
        // 性能テスト
        report.PerformanceTest = await RunPerformanceTestAsync(connectionString);
        
        // ネットワーク分析
        report.NetworkAnalysis = await AnalyzeNetworkAsync(connectionString, duration);
        
        // メモリ使用量分析
        report.MemoryAnalysis = AnalyzeMemoryUsage();
        
        return report;
    }
    
    private async Task<ConnectionTestResult> TestConnectionAsync(string connectionString)
    {
        var result = new ConnectionTestResult();
        
        try
        {
            using var client = SlmpClient.Create(connectionString);
            
            var stopwatch = Stopwatch.StartNew();
            await client.OpenAsync();
            result.ConnectionTime = stopwatch.Elapsed;
            result.ConnectionSuccessful = true;
            
            // 基本機能テスト
            stopwatch.Restart();
            var (typeName, typeCode) = await client.ReadTypeNameAsync();
            result.TypeNameReadTime = stopwatch.Elapsed;
            result.PlcType = typeName;
            result.TypeCode = typeCode;
            
            stopwatch.Restart();
            var testData = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 1);
            result.BasicReadTime = stopwatch.Elapsed;
            result.BasicReadSuccessful = true;
            
        }
        catch (Exception ex)
        {
            result.ConnectionSuccessful = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private async Task<PerformanceTestResult> RunPerformanceTestAsync(string connectionString)
    {
        const int iterationCount = 100;
        var durations = new List<TimeSpan>();
        
        using var client = SlmpClient.Create(connectionString);
        await client.OpenAsync();
        
        for (int i = 0; i < iterationCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await client.ReadWordDevicesAsync(DeviceCode.D, 0, 10);
            durations.Add(stopwatch.Elapsed);
        }
        
        return new PerformanceTestResult
        {
            IterationCount = iterationCount,
            AverageResponseTime = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds)),
            MinResponseTime = durations.Min(),
            MaxResponseTime = durations.Max(),
            P95ResponseTime = durations.OrderBy(d => d).Skip((int)(iterationCount * 0.95)).First(),
            ThroughputPerSecond = iterationCount / durations.Sum(d => d.TotalSeconds)
        };
    }
}

public record DiagnosticReport
{
    public DateTime Timestamp { get; init; }
    public TimeSpan Duration { get; init; }
    public string ConnectionString { get; init; } = "";
    public ConnectionTestResult ConnectionTest { get; init; } = new();
    public PerformanceTestResult PerformanceTest { get; init; } = new();
    public NetworkAnalysisResult NetworkAnalysis { get; init; } = new();
    public MemoryAnalysisResult MemoryAnalysis { get; init; } = new();
}
```

## トラブルシューティング自動化

### 症状別診断フロー
```csharp
public class AutoDiagnostics
{
    public async Task<DiagnosisResult> DiagnoseAsync(Exception exception, SlmpClient client)
    {
        var diagnosis = new DiagnosisResult
        {
            Timestamp = DateTime.UtcNow,
            ExceptionType = exception.GetType().Name,
            ExceptionMessage = exception.Message
        };
        
        switch (exception)
        {
            case SlmpTimeoutException timeoutEx:
                diagnosis = await DiagnoseTimeoutAsync(timeoutEx, client);
                break;
                
            case SlmpCommunicationException commEx when commEx.EndCode == EndCode.Busy:
                diagnosis = await DiagnoseBusyAsync(commEx, client);
                break;
                
            case SocketException socketEx:
                diagnosis = await DiagnoseSocketErrorAsync(socketEx, client);
                break;
                
            case SlmpProtocolException protocolEx:
                diagnosis = await DiagnoseProtocolErrorAsync(protocolEx, client);
                break;
        }
        
        return diagnosis;
    }
    
    private async Task<DiagnosisResult> DiagnoseTimeoutAsync(SlmpTimeoutException ex, SlmpClient client)
    {
        var diagnosis = new DiagnosisResult
        {
            Symptom = "Timeout",
            ProbableCause = "Network latency or PLC overload"
        };
        
        // ネットワーク疎通確認
        var pingResult = await PingHostAsync(client.Address);
        diagnosis.Tests.Add("Ping", pingResult ? "Success" : "Failed");
        
        if (!pingResult)
        {
            diagnosis.ProbableCause = "Network connectivity issue";
            diagnosis.Recommendations.Add("Check network connection");
            diagnosis.Recommendations.Add("Verify PLC IP address");
            return diagnosis;
        }
        
        // ポート接続確認
        var portResult = await TestPortAsync(client.Address, client.Port);
        diagnosis.Tests.Add("Port Connection", portResult ? "Success" : "Failed");
        
        if (!portResult)
        {
            diagnosis.ProbableCause = "SLMP service not running or blocked";
            diagnosis.Recommendations.Add("Check PLC SLMP settings");
            diagnosis.Recommendations.Add("Verify firewall settings");
            return diagnosis;
        }
        
        // 負荷テスト
        var loadTestResult = await TestPlcLoadAsync(client);
        diagnosis.Tests.Add("PLC Load", $"Average response: {loadTestResult.AverageMs:F1}ms");
        
        if (loadTestResult.AverageMs > 1000)
        {
            diagnosis.ProbableCause = "PLC overload or slow processing";
            diagnosis.Recommendations.Add("Reduce request frequency");
            diagnosis.Recommendations.Add("Optimize PLC program");
            diagnosis.Recommendations.Add("Increase timeout values");
        }
        
        return diagnosis;
    }
    
    private async Task<bool> PingHostAsync(string address)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(address, 5000);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
    
    private async Task<bool> TestPortAsync(string address, int port)
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(address, port);
            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }
}

public record DiagnosisResult
{
    public DateTime Timestamp { get; init; }
    public string ExceptionType { get; init; } = "";
    public string ExceptionMessage { get; init; } = "";
    public string Symptom { get; init; } = "";
    public string ProbableCause { get; init; } = "";
    public Dictionary<string, string> Tests { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}
```

### 診断レポート出力
```csharp
public class DiagnosticReporter
{
    public void GenerateReport(DiagnosticReport report, string outputPath)
    {
        var html = GenerateHtmlReport(report);
        File.WriteAllText(Path.Combine(outputPath, $"diagnostic_report_{report.Timestamp:yyyyMMdd_HHmmss}.html"), html);
        
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(outputPath, $"diagnostic_report_{report.Timestamp:yyyyMMdd_HHmmss}.json"), json);
    }
    
    private string GenerateHtmlReport(DiagnosticReport report)
    {
        return $"""
<!DOCTYPE html>
<html>
<head>
    <title>SLMP Diagnostic Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background-color: #f0f0f0; padding: 10px; border-radius: 5px; }}
        .section {{ margin: 20px 0; }}
        .success {{ color: green; }}
        .warning {{ color: orange; }}
        .error {{ color: red; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #f2f2f2; }}
    </style>
</head>
<body>
    <div class="header">
        <h1>SLMP Diagnostic Report</h1>
        <p>Generated: {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</p>
        <p>Connection: {report.ConnectionString}</p>
        <p>Duration: {report.Duration}</p>
    </div>
    
    <div class="section">
        <h2>Connection Test</h2>
        <table>
            <tr><th>Test</th><th>Result</th><th>Time</th></tr>
            <tr>
                <td>Connection</td>
                <td class="{(report.ConnectionTest.ConnectionSuccessful ? "success" : "error")}">
                    {(report.ConnectionTest.ConnectionSuccessful ? "Success" : "Failed")}
                </td>
                <td>{report.ConnectionTest.ConnectionTime.TotalMilliseconds:F1} ms</td>
            </tr>
            <tr>
                <td>Type Name Read</td>
                <td class="success">{report.ConnectionTest.PlcType}</td>
                <td>{report.ConnectionTest.TypeNameReadTime.TotalMilliseconds:F1} ms</td>
            </tr>
        </table>
    </div>
    
    <div class="section">
        <h2>Performance Test</h2>
        <table>
            <tr><th>Metric</th><th>Value</th></tr>
            <tr><td>Iterations</td><td>{report.PerformanceTest.IterationCount}</td></tr>
            <tr><td>Average Response Time</td><td>{report.PerformanceTest.AverageResponseTime.TotalMilliseconds:F1} ms</td></tr>
            <tr><td>P95 Response Time</td><td>{report.PerformanceTest.P95ResponseTime.TotalMilliseconds:F1} ms</td></tr>
            <tr><td>Throughput</td><td>{report.PerformanceTest.ThroughputPerSecond:F1} ops/sec</td></tr>
        </table>
    </div>
</body>
</html>
""";
    }
}
```

このランタイム診断ガイドにより、SlmpClientアプリケーションの運用時の問題を迅速に特定・解決できます。