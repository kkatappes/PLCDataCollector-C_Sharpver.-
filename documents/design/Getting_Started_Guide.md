# 実行可能な使用例とクイックスタートガイド

## 5分でスタート：SlmpClient導入ガイド

### NuGetパッケージからの導入

#### 1. パッケージインストール
```bash
# Package Manager Console
Install-Package SlmpClient.Core

# .NET CLI
dotnet add package SlmpClient.Core

# PackageReference (csproj)
<PackageReference Include="SlmpClient.Core" Version="1.0.0" />
```

#### 2. 基本的な使用方法

```csharp
using SlmpClient.Core;
using SlmpClient.Constants;

// 最も簡単な使用例
using var client = new SlmpClient("192.168.1.100");
await client.ConnectAsync();

// ビットデバイス読み取り (M100から8点)
var bitValues = await client.ReadBitDevicesAsync(DeviceCode.M, 100, 8);
Console.WriteLine($"M100-M107: {string.Join(", ", bitValues)}");

// ワードデバイス読み取り (D0から3点) 
var wordValues = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 3);
Console.WriteLine($"D0-D2: {string.Join(", ", wordValues)}");

// ワードデバイス書き込み (D10に値設定)
await client.WriteWordDevicesAsync(DeviceCode.D, 10, new ushort[] { 1234, 5678 });
```

#### 3. 製造業向け稼働第一継続機能の使用例

```csharp
using SlmpClient.Core;
using SlmpClient.Constants;

// 製造現場向け設定の適用
var settings = new SlmpConnectionSettings();
settings.ApplyManufacturingOperationFirstSettings();

using var client = new SlmpClient("192.168.1.100", settings);
await client.ConnectAsync();

// 製造ライン監視ループ（通信エラーが発生してもライン停止せず継続）
for (int cycle = 1; cycle <= 10; cycle++)
{
    Console.WriteLine($"製造サイクル {cycle}");
    
    // センサー状態読み取り（エラー時はデフォルト値で継続）
    var sensors = await client.ReadBitDevicesAsync(DeviceCode.M, 100, 8);
    Console.WriteLine($"センサー状態: [{string.Join(", ", sensors)}]");
    
    // 生産カウンター読み取り（エラー時はデフォルト値で継続）
    var counters = await client.ReadWordDevicesAsync(DeviceCode.D, 200, 4);
    Console.WriteLine($"生産カウンター: [{string.Join(", ", counters)}]");
    
    // 製造プロセス継続...
    await Task.Delay(2000); // 2秒間隔
}

// エラー統計の確認
var stats = client.ErrorStatistics.GetSummary();
Console.WriteLine($"総操作数: {stats.TotalOperations}");
Console.WriteLine($"エラー率: {stats.ErrorRate:F1}%");
Console.WriteLine($"継続率: {stats.ContinuityRate:F1}%");
```

### ソースからのビルド・導入

#### 1. リポジトリクローン
```bash
git clone https://github.com/your-org/SlmpClient.git
cd SlmpClient
```

#### 2. ビルドと実行
```bash
# ビルド
dotnet build SlmpClient.sln

# テスト実行
dotnet test

# サンプル実行
dotnet run --project samples/BasicExample
```

#### 3. プロジェクト参照
```xml
<ProjectReference Include="..\..\src\SlmpClient.Core\SlmpClient.Core.csproj" />
```

## 基本的な接続パターン

### パターン1: シンプル接続
```csharp
// デフォルト設定での接続
using var client = SlmpClient.Create("192.168.1.100");
await client.OpenAsync();

try
{
    var values = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 10);
    Console.WriteLine($"Read {values.Length} values");
}
finally
{
    await client.CloseAsync(); // usingで自動的に呼ばれる
}
```

### パターン2: 詳細設定での接続
```csharp
var settings = new SlmpConnectionSettings
{
    Port = 5000,
    IsBinary = true,
    Version = SlmpFrameVersion.Version4E,
    UseTcp = false, // UDP使用
    ReceiveTimeout = TimeSpan.FromSeconds(3),
    ConnectTimeout = TimeSpan.FromSeconds(10)
};

using var client = SlmpClient.Create("192.168.1.100", settings);
client.Target = new SlmpTarget
{
    Network = 0,
    Node = 255,
    DestinationProcessor = 1024,
    MultiDropStation = 0
};

await client.OpenAsync();
```

### パターン3: 依存性注入での使用
```csharp
// Startup.cs / Program.cs
services.AddSlmpClient("192.168.1.100", new SlmpConnectionSettings
{
    Port = 5000,
    IsBinary = true
});

// サービスクラス
public class PlcService
{
    private readonly SlmpClient _slmpClient;
    
    public PlcService(SlmpClient slmpClient)
    {
        _slmpClient = slmpClient;
    }
    
    public async Task<int[]> GetProductionCountAsync()
    {
        var counts = await _slmpClient.ReadWordDevicesAsync(DeviceCode.D, 1000, 10);
        return counts.Select(x => (int)x).ToArray();
    }
}
```

## 同期/非同期の使い分け

### 非同期API（推奨）
```csharp
// ASP.NET Core Controllerでの使用
[ApiController]
[Route("api/[controller]")]
public class PlcController : ControllerBase
{
    private readonly SlmpClient _client;
    
    public PlcController(SlmpClient client)
    {
        _client = client;
    }
    
    [HttpGet("devices/{deviceCode}/{address}/{count}")]
    public async Task<ActionResult<ushort[]>> GetDeviceValues(
        string deviceCode, 
        uint address, 
        ushort count,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DeviceCode>(deviceCode, out var device))
            return BadRequest("Invalid device code");
            
        try
        {
            var values = await _client.ReadWordDevicesAsync(
                device, address, count, timeout: 0, cancellationToken);
            return Ok(values);
        }
        catch (SlmpTimeoutException)
        {
            return StatusCode(408, "PLC communication timeout");
        }
        catch (SlmpCommunicationException ex)
        {
            return StatusCode(502, $"PLC communication error: {ex.EndCode}");
        }
    }
}
```

### 同期API（レガシー対応）
```csharp
// 既存の同期コードから利用
public class LegacyPlcReader
{
    private readonly SlmpClient _client;
    
    public int[] ReadProductionCounters()
    {
        // 同期APIを使用（内部で非同期実装を同期的に実行）
        var values = _client.ReadWordDevices(DeviceCode.D, 2000, 5);
        return values.Select(x => (int)x).ToArray();
    }
    
    // 注意: 同期APIでもCancellationTokenを渡せない
    // 長時間実行される可能性がある処理では非同期APIを推奨
}
```

## 代表的なSLMPCommand使用例

### デバイス基本操作
```csharp
using var client = SlmpClient.Create("192.168.1.100");
await client.OpenAsync();

// 1. ビットデバイス操作
// M100-M107の読み取り
var bits = await client.ReadBitDevicesAsync(DeviceCode.M, 100, 8);
Console.WriteLine($"M100-M107: {string.Join("", bits.Select(b => b ? "1" : "0"))}");

// M200-M203への書き込み
await client.WriteBitDevicesAsync(DeviceCode.M, 200, new[] { true, false, true, false });

// 2. ワードデバイス操作
// D0-D9の読み取り
var words = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 10);
Console.WriteLine($"D0-D9: {string.Join(", ", words)}");

// D100-D102への書き込み
await client.WriteWordDevicesAsync(DeviceCode.D, 100, new ushort[] { 1000, 2000, 3000 });

// 3. ランダムアクセス（複数の不連続デバイス）
var (wordData, dwordData) = await client.ReadRandomDevicesAsync(
    wordDevices: new[] { (DeviceCode.D, 0u), (DeviceCode.D, 10u), (DeviceCode.TN, 0u) },
    dwordDevices: new[] { (DeviceCode.D, 1000u), (DeviceCode.CN, 0u) }
);

Console.WriteLine($"Word data: {string.Join(", ", wordData)}");
Console.WriteLine($"DWord data: {string.Join(", ", dwordData)}");

// 4. ブロック読み取り（複数のデバイス範囲）
var (wordBlocks, bitBlocks) = await client.ReadBlockAsync(
    wordBlocks: new[] 
    { 
        (DeviceCode.D, 0u, (ushort)10),    // D0-D9
        (DeviceCode.TN, 0u, (ushort)5)     // TN0-TN4
    },
    bitBlocks: new[] 
    { 
        (DeviceCode.M, 100u, (ushort)16), // M100-M115
        (DeviceCode.X, 0u, (ushort)32)    // X0-X31
    }
);

Console.WriteLine($"D0-D9: {string.Join(", ", wordBlocks[0])}");
Console.WriteLine($"M100-M115: {string.Join("", bitBlocks[0].Select(b => b ? "1" : "0"))}");
```

### モニタ機能（高頻度監視）
```csharp
// モニタ登録（一度設定すれば繰り返し高速読み取り可能）
await client.EntryMonitorDeviceAsync(
    wordDevices: new[] 
    { 
        (DeviceCode.D, 0u),     // D0
        (DeviceCode.D, 1u),     // D1
        (DeviceCode.TN, 0u)     // TN0
    },
    dwordDevices: new[] 
    { 
        (DeviceCode.D, 1000u),  // D1000-D1001 (32bit)
        (DeviceCode.CN, 0u)     // CN0-CN1 (32bit)
    }
);

// 高速繰り返し監視
for (int i = 0; i < 100; i++)
{
    var (wordData, dwordData) = await client.ExecuteMonitorAsync();
    Console.WriteLine($"Cycle {i}: D0={wordData[0]}, D1000-D1001={dwordData[0]}");
    
    await Task.Delay(100); // 100ms間隔
}
```

### システム機能
```csharp
// PLC型名取得
var (typeName, typeCode) = await client.ReadTypeNameAsync();
Console.WriteLine($"PLC Type: {typeName} (Code: {typeCode})");

// 通信テスト
var testData = DateTime.Now.ToString("yyyyMMddHHmmss");
var isOk = await client.SelfTestAsync(testData);
Console.WriteLine($"Self test: {(isOk ? "OK" : "NG")}");

// エラークリア
await client.ClearErrorAsync();
Console.WriteLine("Error cleared");

// オンデマンドデータ確認
var onDemandData = client.CheckOnDemandData();
if (onDemandData != null)
{
    Console.WriteLine($"On-demand data received: {BitConverter.ToString(onDemandData)}");
}
```

### メモリ直接アクセス
```csharp
// 注意: ネットワーク=0, ノード=255 の場合のみ使用可能
client.Target = new SlmpTarget(network: 0, node: 255, destinationProcessor: 0, multiDropStation: 0);

// メモリ読み取り（アドレス0x1000から10ワード）
var memoryData = await client.MemoryReadAsync(0x1000, 10);
Console.WriteLine($"Memory data: {BitConverter.ToString(memoryData)}");

// メモリ書き込み
var writeData = new byte[] { 0x12, 0x34, 0x56, 0x78 };
await client.MemoryWriteAsync(0x2000, writeData);
```

## 典型的なエラーと対処法

### 1. 接続エラー
```csharp
try
{
    await client.OpenAsync();
}
catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
{
    Console.WriteLine("接続タイムアウト: PLCのIPアドレスとポート番号を確認してください");
}
catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
{
    Console.WriteLine("接続拒否: PLCのSLMP設定が有効か確認してください");
}
catch (SocketException ex)
{
    Console.WriteLine($"ネットワークエラー: {ex.Message}");
}
```

### 2. タイムアウトエラー
```csharp
try
{
    // タイムアウト値を明示的に指定（250ms単位）
    var values = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 100, timeout: 20); // 5秒
}
catch (SlmpTimeoutException ex)
{
    Console.WriteLine($"PLCからの応答がタイムアウトしました: {ex.Timeout}");
    
    // リトライ処理
    Console.WriteLine("リトライします...");
    await Task.Delay(1000);
    var values = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 100, timeout: 40); // 10秒
}
```

### 3. プロトコルエラー
```csharp
try
{
    var values = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 100);
}
catch (SlmpCommunicationException ex) when (ex.EndCode == EndCode.WrongCommand)
{
    Console.WriteLine("未対応コマンド: PLCがこのコマンドをサポートしていません");
}
catch (SlmpCommunicationException ex) when (ex.EndCode == EndCode.WrongFormat)
{
    Console.WriteLine("フォーマットエラー: フレーム形式を確認してください");
}
catch (SlmpCommunicationException ex) when (ex.EndCode == EndCode.Busy)
{
    Console.WriteLine("PLC処理中: しばらく待ってからリトライしてください");
    
    // 指数バックオフでリトライ
    for (int retry = 0; retry < 3; retry++)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, retry) * 1000));
        try
        {
            var values = await client.ReadWordDevicesAsync(DeviceCode.D, 0, 100);
            break; // 成功
        }
        catch (SlmpCommunicationException ex2) when (ex2.EndCode == EndCode.Busy)
        {
            if (retry == 2) throw; // 最後のリトライも失敗
        }
    }
}
```

### 4. デバイス範囲外エラー
```csharp
try
{
    // 存在しないデバイス番号を指定
    var values = await client.ReadWordDevicesAsync(DeviceCode.D, 999999, 10);
}
catch (SlmpCommunicationException ex) when (ex.EndCode == EndCode.ExceedReqLength)
{
    Console.WriteLine("要求データサイズが上限を超えています");
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"引数範囲外: {ex.ParamName} = {ex.ActualValue}");
}
```

### 5. 設定エラー
```csharp
try
{
    var client = SlmpClient.Create("invalid-ip-address");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"設定エラー: {ex.Message}");
}

try
{
    client.Target = new SlmpTarget(network: 256, node: 0, destinationProcessor: 0, multiDropStation: 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"ターゲット設定エラー: {ex.ParamName}は0-255の範囲で設定してください");
}
```

## 実践的な使用パターン

### パターン1: 工場監視システム
```csharp
public class ProductionMonitor
{
    private readonly SlmpClient _client;
    private readonly ILogger<ProductionMonitor> _logger;
    
    public ProductionMonitor(SlmpClient client, ILogger<ProductionMonitor> logger)
    {
        _client = client;
        _logger = logger;
    }
    
    public async Task<ProductionStatus> GetProductionStatusAsync()
    {
        try
        {
            // 生産状況を複数デバイスから一括読み取り
            var (wordData, _) = await _client.ReadRandomDevicesAsync(
                wordDevices: new[]
                {
                    (DeviceCode.D, 1000u), // 生産数
                    (DeviceCode.D, 1001u), // 不良数  
                    (DeviceCode.D, 1002u), // 稼働時間
                    (DeviceCode.D, 1003u), // 停止時間
                },
                dwordDevices: Array.Empty<(DeviceCode, uint)>()
            );
            
            // 機械状態をビットデバイスから読み取り
            var statusBits = await _client.ReadBitDevicesAsync(DeviceCode.M, 2000, 16);
            
            return new ProductionStatus
            {
                ProductionCount = wordData[0],
                DefectCount = wordData[1], 
                OperatingMinutes = wordData[2],
                StoppedMinutes = wordData[3],
                IsRunning = statusBits[0],
                IsAlarming = statusBits[1],
                IsAutoMode = statusBits[2]
            };
        }
        catch (SlmpException ex)
        {
            _logger.LogError(ex, "Failed to read production status");
            throw;
        }
    }
}

public record ProductionStatus
{
    public ushort ProductionCount { get; init; }
    public ushort DefectCount { get; init; }
    public ushort OperatingMinutes { get; init; }  
    public ushort StoppedMinutes { get; init; }
    public bool IsRunning { get; init; }
    public bool IsAlarming { get; init; }
    public bool IsAutoMode { get; init; }
}
```

### パターン2: バッチ処理システム
```csharp
public class BatchController
{
    private readonly SlmpClient _client;
    
    public async Task ExecuteBatchAsync(BatchRecipe recipe)
    {
        // バッチ開始
        await _client.WriteBitDevicesAsync(DeviceCode.M, 3000, new[] { true }); // スタート信号
        
        // レシピデータ書き込み
        await _client.WriteWordDevicesAsync(DeviceCode.D, 5000, new ushort[]
        {
            recipe.Temperature,
            recipe.Pressure, 
            recipe.Duration,
            recipe.MixingSpeed
        });
        
        // 完了待ち（ポーリング）
        while (true)
        {
            var status = await _client.ReadBitDevicesAsync(DeviceCode.M, 3001, 3);
            
            if (status[0]) // 完了
            {
                Console.WriteLine("Batch completed successfully");
                break;
            }
            else if (status[1]) // エラー
            {
                var errorCode = await _client.ReadWordDevicesAsync(DeviceCode.D, 6000, 1);
                throw new BatchException($"Batch failed with error code: {errorCode[0]}");
            }
            else if (status[2]) // 停止要求
            {
                await _client.WriteBitDevicesAsync(DeviceCode.M, 3002, new[] { true });
                throw new OperationCanceledException("Batch was cancelled");
            }
            
            await Task.Delay(1000); // 1秒待機
        }
    }
}
```

### パターン3: 高頻度データ収集
```csharp
public class HighSpeedDataCollector
{
    private readonly SlmpClient _client;
    private readonly Channel<SensorData> _dataChannel;
    
    public async Task StartCollectionAsync(CancellationToken cancellationToken)
    {
        // モニタ登録（センサー値12点）
        await _client.EntryMonitorDeviceAsync(
            wordDevices: Enumerable.Range(0, 12)
                .Select(i => (DeviceCode.D, (uint)(2000 + i)))
                .ToArray(),
            dwordDevices: Array.Empty<(DeviceCode, uint)>()
        );
        
        // 高速データ収集ループ
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var (sensorValues, _) = await _client.ExecuteMonitorAsync();
                
                var sensorData = new SensorData
                {
                    Timestamp = DateTime.UtcNow,
                    Temperature = sensorValues[0] * 0.1, // スケーリング
                    Pressure = sensorValues[1] * 0.01,
                    FlowRate = sensorValues[2] * 0.1,
                    Values = sensorValues
                };
                
                await _dataChannel.Writer.WriteAsync(sensorData, cancellationToken);
            }
            catch (SlmpException ex) when (ex is SlmpTimeoutException or SlmpCommunicationException)
            {
                // 通信エラーは記録して継続
                Console.WriteLine($"Communication error: {ex.Message}");
            }
            
            await Task.Delay(50, cancellationToken); // 20Hz収集
        }
    }
}
```

このガイドに従うことで、SlmpClientライブラリを5分で導入し、実践的なPLC通信アプリケーションを構築できます。