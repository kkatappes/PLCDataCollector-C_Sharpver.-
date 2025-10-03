# コーディング規約とアーキテクチャ決定記録（ADR）

## C# コーディング規約

### 命名規則

#### クラス・インターフェース・メソッド
```csharp
// ✅ Good
public class SlmpClient : IDisposable
public interface ISlmpTransport
public async Task<bool[]> ReadBitDevicesAsync(...)

// ❌ Bad
public class slmpClient
public interface slmpTransport
public async Task<bool[]> read_bit_devices(...)
```

#### フィールド・プロパティ・変数
```csharp
// ✅ Good - プロパティ
public DeviceCode DeviceCode { get; set; }

// ✅ Good - プライベートフィールド
private readonly ILogger<SlmpClient> _logger;
private Socket? _socket;

// ✅ Good - ローカル変数・引数
public void ProcessData(byte[] frameData)
{
    var responseLength = frameData.Length;
    var targetDevice = DeviceCode.D;
}

// ❌ Bad
public DeviceCode deviceCode { get; set; }
private readonly ILogger<SlmpClient> logger;
public void ProcessData(byte[] frame_data)
```

#### 定数・列挙型
```csharp
// ✅ Good
public const int MaxFrameSize = 8194;
public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

public enum SlmpCommand
{
    DeviceRead = 0x0401,
    DeviceWrite = 0x1401,
    DeviceReadRandom = 0x0403
}

// ❌ Bad
public const int MAX_FRAME_SIZE = 8194;
public enum SlmpCommand
{
    DEVICE_READ = 0x0401
}
```

### Null 許容参照型

#### 設定
```xml
<!-- プロジェクトファイル -->
<PropertyGroup>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS8618</WarningsNotAsErrors>
</PropertyGroup>
```

#### 使用方針
```csharp
// ✅ Good - 明示的null許容
public string? OptionalMessage { get; set; }
public byte[]? CheckOnDemandData() => _onDemandBuffer;

// ✅ Good - null非許容（初期化保証）
public string Address { get; }
public SlmpTarget Target { get; set; } = new();

// ✅ Good - null チェック
public void ProcessResponse(byte[]? responseData)
{
    if (responseData is null)
        throw new ArgumentNullException(nameof(responseData));
        
    // または
    ArgumentNullException.ThrowIfNull(responseData);
}

// ❌ Bad - 曖昧な null 許容性
public string Message { get; set; } // null許容性が不明
```

### async/await パターン

#### 基本方針
```csharp
// ✅ Good - ConfigureAwait(false) 使用
public async Task<bool[]> ReadBitDevicesAsync(
    DeviceCode deviceCode, 
    uint startAddress, 
    ushort count, 
    ushort timeout = 0,
    CancellationToken cancellationToken = default)
{
    var frame = _frameBuilder.BuildReadFrame(deviceCode, startAddress, count);
    var response = await _transport.SendAsync(frame, timeout, cancellationToken)
        .ConfigureAwait(false);
    return _responseParser.ParseBitResponse(response);
}

// ✅ Good - 同期版も提供（ライブラリAPI）
public bool[] ReadBitDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0)
{
    return ReadBitDevicesAsync(deviceCode, startAddress, count, timeout, CancellationToken.None)
        .ConfigureAwait(false)
        .GetAwaiter()
        .GetResult();
}

// ❌ Bad - ConfigureAwait(false) なし
var response = await _transport.SendAsync(frame, timeout, cancellationToken);

// ❌ Bad - async void
public async void ProcessData() { } // イベントハンドラ以外では禁止
```

#### CancellationToken の扱い
```csharp
// ✅ Good - 全非同期メソッドで受け入れ
public async Task<ushort[]> ReadWordDevicesAsync(
    DeviceCode deviceCode,
    uint startAddress, 
    ushort count,
    ushort timeout = 0,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(timeout * 250));
    
    return await InternalReadWordDevicesAsync(deviceCode, startAddress, count, timeoutCts.Token)
        .ConfigureAwait(false);
}
```

### Span<T>/Memory<T> の利用方針

#### 使用場面
```csharp
// ✅ Good - フレーム構築での使用
public ReadOnlySpan<byte> BuildBinaryFrame(
    byte sequence,
    SlmpTarget target,
    ushort timeout,
    SlmpCommand command,
    ushort subCommand,
    ReadOnlySpan<byte> data)
{
    Span<byte> frame = stackalloc byte[MaxFrameSize];
    var writer = new BinaryWriter(frame);
    
    // フレーム構築...
    return frame[..actualLength];
}

// ✅ Good - 大きなバッファの部分処理
public void ProcessResponse(ReadOnlySpan<byte> response)
{
    var header = response[..9];
    var payload = response[9..];
    // 処理...
}

// ❌ Bad - 不要なメモリ確保
public byte[] BuildFrame(...)
{
    var frame = new byte[8194]; // 常に最大サイズ確保
    // ...
    return frame; // 全体を返す
}
```

### 例外設計

#### 例外階層
```csharp
// 基底例外
public class SlmpException : Exception
{
    public SlmpException() { }
    public SlmpException(string message) : base(message) { }
    public SlmpException(string message, Exception innerException) : base(message, innerException) { }
}

// 通信エラー
public class SlmpCommunicationException : SlmpException
{
    public EndCode EndCode { get; }
    
    public SlmpCommunicationException(EndCode endCode) 
        : base($"SLMP communication error: {endCode}")
    {
        EndCode = endCode;
    }
}

// タイムアウト
public class SlmpTimeoutException : SlmpException
{
    public TimeSpan Timeout { get; }
    
    public SlmpTimeoutException(TimeSpan timeout)
        : base($"Operation timed out after {timeout}")
    {
        Timeout = timeout;
    }
}
```

#### 例外メッセージの方針
```csharp
// ✅ Good - 構造化された情報、機密情報なし
throw new ArgumentOutOfRangeException(
    nameof(startAddress), 
    startAddress, 
    "Start address must be less than 0xFFFFFF");

throw new SlmpCommunicationException(EndCode.WrongCommand)
{
    Data = 
    {
        ["Command"] = command.ToString(),
        ["SubCommand"] = subCommand,
        ["Target"] = target.ToString()
    }
};

// ❌ Bad - 機密情報含む、非構造化
throw new Exception($"Error connecting to {ipAddress}:{port} with credentials {username}:{password}");
```

### ロギング方針

#### ILogger<T> 使用
```csharp
public class SlmpClient
{
    private readonly ILogger<SlmpClient> _logger;
    
    public SlmpClient(ILogger<SlmpClient>? logger = null)
    {
        _logger = logger ?? NullLogger<SlmpClient>.Instance;
    }
    
    public async Task<bool[]> ReadBitDevicesAsync(...)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = "ReadBitDevices",
            ["DeviceCode"] = deviceCode,
            ["StartAddress"] = startAddress,
            ["Count"] = count
        });
        
        _logger.LogInformation("Starting bit device read operation");
        
        try
        {
            var result = await InternalReadBitDevicesAsync(...);
            _logger.LogInformation("Bit device read completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bit device read failed");
            throw;
        }
    }
}
```

#### 構造化ログ
```csharp
// ✅ Good - 構造化ログ
_logger.LogInformation(
    "Frame sent: Command={Command}, Length={Length}, Sequence={Sequence}",
    command, frameLength, sequence);

// ❌ Bad - 文字列結合
_logger.LogInformation($"Frame sent: Command={command}, Length={frameLength}");
```

### DI（依存性注入）方針

#### DIコンテナ非依存設計
```csharp
// ✅ Good - DIコンテナに依存しない設計
public class SlmpClient : IDisposable
{
    private readonly ISlmpTransport _transport;
    private readonly ILogger<SlmpClient> _logger;
    
    // プライマリコンストラクタ（DI用）
    public SlmpClient(
        ISlmpTransport transport,
        ILogger<SlmpClient>? logger = null)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? NullLogger<SlmpClient>.Instance;
    }
    
    // ファクトリメソッド（シンプル利用用）
    public static SlmpClient Create(string address, SlmpConnectionSettings? settings = null)
    {
        settings ??= new SlmpConnectionSettings();
        var transport = new DefaultSlmpTransport(address, settings);
        return new SlmpClient(transport);
    }
}

// 拡張メソッドでDI統合
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSlmpClient(
        this IServiceCollection services,
        string address,
        SlmpConnectionSettings? settings = null)
    {
        services.AddSingleton<ISlmpTransport>(provider => 
            new DefaultSlmpTransport(address, settings ?? new()));
        services.AddScoped<SlmpClient>();
        return services;
    }
}
```

## パフォーマンス方針

### メモリ割り当て削減

#### ArrayPool 利用
```csharp
public class FrameBuilder
{
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;
    
    public async Task<byte[]> BuildFrameAsync(...)
    {
        var buffer = BytePool.Rent(MaxFrameSize);
        try
        {
            var length = BuildFrameInternal(buffer, ...);
            var result = new byte[length];
            buffer.AsSpan(0, length).CopyTo(result);
            return result;
        }
        finally
        {
            BytePool.Return(buffer);
        }
    }
}
```

#### BinaryPrimitives 利用
```csharp
// ✅ Good - BinaryPrimitives使用
public void WriteUInt16(Span<byte> buffer, int offset, ushort value)
{
    BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], value);
}

public ushort ReadUInt16(ReadOnlySpan<byte> buffer, int offset)
{
    return BinaryPrimitives.ReadUInt16LittleEndian(buffer[offset..]);
}

// ❌ Bad - BitConverter使用（アロケーション発生）
public void WriteUInt16(byte[] buffer, int offset, ushort value)
{
    var bytes = BitConverter.GetBytes(value);
    Array.Copy(bytes, 0, buffer, offset, 2);
}
```

#### 構造体の適切な使用
```csharp
// ✅ Good - 小さな値型として構造体使用
public readonly struct SlmpTarget : IEquatable<SlmpTarget>
{
    public byte Network { get; }
    public byte Node { get; }
    public ushort DestinationProcessor { get; }
    public byte MultiDropStation { get; }
    
    public SlmpTarget(byte network, byte node, ushort destinationProcessor, byte multiDropStation)
    {
        Network = network;
        Node = node;
        DestinationProcessor = destinationProcessor;
        MultiDropStation = multiDropStation;
    }
    
    public bool Equals(SlmpTarget other) => 
        Network == other.Network && 
        Node == other.Node && 
        DestinationProcessor == other.DestinationProcessor && 
        MultiDropStation == other.MultiDropStation;
        
    public override bool Equals(object? obj) => obj is SlmpTarget other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Network, Node, DestinationProcessor, MultiDropStation);
}
```

### セキュリティ方針

#### ソケットタイムアウト
```csharp
// ✅ Good - 適切なタイムアウト設定
public class TcpSlmpTransport : IDisposable
{
    private TcpClient? _tcpClient;
    
    public async Task ConnectAsync(string address, int port, TimeSpan timeout)
    {
        _tcpClient = new TcpClient();
        
        // 接続タイムアウト
        using var cts = new CancellationTokenSource(timeout);
        await _tcpClient.ConnectAsync(address, port, cts.Token);
        
        // 送受信タイムアウト
        _tcpClient.ReceiveTimeout = (int)timeout.TotalMilliseconds;
        _tcpClient.SendTimeout = (int)timeout.TotalMilliseconds;
        
        // バッファサイズ制限
        _tcpClient.ReceiveBufferSize = 8192;
        _tcpClient.SendBufferSize = 8192;
    }
}
```

#### 例外メッセージの情報漏えい防止
```csharp
// ✅ Good - 機密情報を含まない
public void ValidateConnection()
{
    if (!IsConnected)
        throw new InvalidOperationException("Client is not connected");
        
    if (_target.Network > 255)
        throw new ArgumentOutOfRangeException("Network number must be 0-255");
}

// ❌ Bad - 内部情報漏えい
throw new Exception($"Failed to connect to {_internalServerAddress} using key {_secretKey}");
```

## アーキテクチャ決定記録（ADR）

### ADR-001: 同期/非同期API提供方針

**決定日**: 2024年12月
**ステータス**: 承認済み

#### 決定内容
- 非同期APIを主要APIとして提供
- 同期APIは非同期APIのラッパーとして提供
- 同期APIは`GetAwaiter().GetResult()`を使用してデッドロック回避

#### 理由
- .NET環境では非同期処理が標準
- I/O集約的な通信処理には非同期が適している
- 既存の同期的コードからの移行を容易にするため同期版も提供

#### 実装例
```csharp
// 非同期版（主要API）
public async Task<bool[]> ReadBitDevicesAsync(
    DeviceCode deviceCode,
    uint startAddress,
    ushort count,
    ushort timeout = 0,
    CancellationToken cancellationToken = default)
{
    // 実装...
}

// 同期版（ラッパー）
public bool[] ReadBitDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0)
{
    return ReadBitDevicesAsync(deviceCode, startAddress, count, timeout, CancellationToken.None)
        .ConfigureAwait(false)
        .GetAwaiter()
        .GetResult();
}
```

### ADR-002: エンディアンの扱い

**決定日**: 2024年12月
**ステータス**: 承認済み

#### 決定内容
- SLMPプロトコルのリトルエンディアンに準拠
- `BinaryPrimitives`を使用してエンディアン変換を明示的に実行
- プラットフォーム依存性を排除

#### 理由
- SLMPプロトコル仕様でリトルエンディアンが規定されている
- クロスプラットフォーム対応のためプラットフォーム依存を避ける
- パフォーマンスと安全性を両立

#### 実装例
```csharp
public void WriteUInt16(Span<byte> buffer, int offset, ushort value)
{
    BinaryPrimitives.WriteUInt16LittleEndian(buffer[offset..], value);
}

public ushort ReadUInt16(ReadOnlySpan<byte> buffer, int offset)
{
    return BinaryPrimitives.ReadUInt16LittleEndian(buffer[offset..]);
}
```

### ADR-003: エラーハンドリングの方針

**決定日**: 2024年12月
**ステータス**: 承認済み

#### 決定内容
- SLMP固有エラーは`SlmpException`階層で表現
- システムエラーは標準.NET例外を使用
- エラーコード情報は例外のプロパティで提供
- リトライ可能性を例外プロパティで示す

#### 理由
- エラー種別による適切な処理を可能にする
- 既存.NETエコシステムとの一貫性
- 運用時のトラブルシューティングを容易にする

#### 実装例
```csharp
public class SlmpCommunicationException : SlmpException
{
    public EndCode EndCode { get; }
    public bool IsRetryable { get; }
    
    public SlmpCommunicationException(EndCode endCode) : base(GetMessage(endCode))
    {
        EndCode = endCode;
        IsRetryable = DetermineRetryability(endCode);
    }
    
    private static bool DetermineRetryability(EndCode endCode) => endCode switch
    {
        EndCode.Busy => true,
        EndCode.TimeoutError => true,
        EndCode.WrongCommand => false,
        EndCode.WrongFormat => false,
        _ => false
    };
}
```

### ADR-004: DeviceCodeの表現形式

**決定日**: 2024年12月
**ステータス**: 承認済み

#### 決定内容
- `enum`として型安全に定義
- 拡張メソッドで特性（16進アドレス、4バイトアドレス等）を提供
- 数値コードとの相互変換機能を提供

#### 理由
- 型安全性とIntellSenseによる開発効率向上
- Python版の定数配列を型安全な形で再現
- 可読性と保守性の向上

#### 実装例
```csharp
public enum DeviceCode : byte
{
    SM = 0x91,
    SD = 0xA9,
    X = 0x9C,
    Y = 0x9D,
    M = 0x90,
    // ...
}

public static class DeviceCodeExtensions
{
    private static readonly HashSet<DeviceCode> HexAddressDevices = new()
    {
        DeviceCode.X, DeviceCode.Y, DeviceCode.B, DeviceCode.W,
        DeviceCode.SB, DeviceCode.SW, DeviceCode.DX, DeviceCode.DY,
        DeviceCode.ZR, DeviceCode.W
    };
    
    public static bool IsHexAddress(this DeviceCode deviceCode) => 
        HexAddressDevices.Contains(deviceCode);
        
    public static bool Is4ByteAddress(this DeviceCode deviceCode) => deviceCode switch
    {
        DeviceCode.LTS or DeviceCode.LTC or DeviceCode.LTN or
        DeviceCode.LSTS or DeviceCode.LSTC or DeviceCode.LSTN or
        DeviceCode.LCS or DeviceCode.LCC or DeviceCode.LCN or
        DeviceCode.LZ or DeviceCode.RD => true,
        _ => false
    };
}
```

### ADR-005: ログ出力設計

**決定日**: 2024年12月
**ステータス**: 承認済み

#### 決定内容
- `Microsoft.Extensions.Logging`を使用
- 構造化ログでコンテキスト情報を記録
- 機密情報（IPアドレス、認証情報等）は記録しない
- パフォーマンス計測は`DiagnosticSource`で実装

#### 理由
- .NETエコシステムの標準
- 構造化ログによる検索・分析の容易さ
- セキュリティとプライバシーの保護

#### 実装例
```csharp
public async Task<ushort[]> ReadWordDevicesAsync(...)
{
    using var activity = DiagnosticSource.StartActivity("SlmpClient.ReadWordDevices", new
    {
        DeviceCode = deviceCode.ToString(),
        StartAddress = startAddress,
        Count = count
    });
    
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["Operation"] = "ReadWordDevices",
        ["DeviceCode"] = deviceCode,
        ["Count"] = count
    });
    
    _logger.LogDebug("Starting word device read operation");
    
    var stopwatch = Stopwatch.StartNew();
    try
    {
        var result = await InternalReadWordDevicesAsync(...);
        
        _logger.LogInformation(
            "Word device read completed: Duration={Duration}ms, ResultCount={ResultCount}",
            stopwatch.ElapsedMilliseconds,
            result.Length);
            
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, 
            "Word device read failed: Duration={Duration}ms",
            stopwatch.ElapsedMilliseconds);
        throw;
    }
}
```

これらの規約とADRに従うことで、一貫性があり保守しやすく、パフォーマンスの良いC#実装を実現できます。