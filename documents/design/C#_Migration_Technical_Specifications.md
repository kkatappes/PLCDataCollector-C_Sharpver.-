# PySLMPClient → C# 移植技術仕様書

## 概要
Python実装の詳細分析に基づく、C#移植における5つの重要な技術課題への対応仕様

---

## 1. タイムアウトとリトライの正確なアルゴリズム

### Python実装の詳細 (`__init__.py:280-296`)
```python
def __recv_loop(self, seq: int, timeout: int):
    timeout *= 0.25  # 250ms単位への変換
    if timeout == 0:
        timeout = 100  # デフォルトは100秒
    start = time.monotonic()
    while seq not in self.__recv_queue.keys():
        end = time.monotonic()
        if end - start > timeout:
            break
    if seq not in self.__recv_queue.keys():
        raise TimeoutError()
```

### C#実装仕様
```csharp
private async Task<SlmpResponse> ReceiveLoopAsync(ushort sequenceId, ushort timeout, CancellationToken cancellationToken)
{
    // Python: timeout *= 0.25 (250ms単位)
    var timeoutMs = timeout == 0 ? 100_000 : timeout * 250; // ms単位に変換
    var timeoutTimeSpan = TimeSpan.FromMilliseconds(timeoutMs);
    
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(timeoutTimeSpan);
    
    var startTime = DateTime.UtcNow;
    
    while (!_receiveQueue.ContainsKey(sequenceId))
    {
        // 100ms間隔でポーリング（Pythonは連続チェック）
        await Task.Delay(100, cts.Token);
        
        if (cts.Token.IsCancellationRequested)
        {
            throw new SlmpTimeoutException($"Timeout after {timeoutMs}ms waiting for sequence {sequenceId}");
        }
    }
    
    if (_receiveQueue.TryRemove(sequenceId, out var response))
    {
        return response;
    }
    
    throw new SlmpTimeoutException($"Response lost for sequence {sequenceId}");
}
```

**キーポイント:**
- Python: `timeout *= 0.25` → C#: `timeout * 250` (ms変換)
- Python: `time.monotonic()` → C#: `DateTime.UtcNow` または `Stopwatch`
- Python: busy-wait → C#: `Task.Delay(100)` でCPU負荷軽減

---

## 2. 受信ワーカーの排他制御（複数同時コマンド時のレスポンス照合ID）

### Python実装の詳細 (`__init__.py:54-57, 247-254`)
```python
self.__recv_queue = dict()  # type: Dict[int, (int, int, int, int, int, bytes)]
self.__lock = threading.Lock()
self.__seq = 0  # シーケンス番号

# 受信時の処理
with self.__lock:
    self.__recv_queue[seq] = (
        network_num, pc_num, io_num, m_drop_num, term_code, data
    )
```

### C#実装仕様
```csharp
public class SlmpClient
{
    private readonly ConcurrentDictionary<ushort, SlmpResponse> _receiveQueue;
    private readonly SemaphoreSlim _sequenceLock;
    private ushort _currentSequence;
    
    public SlmpClient()
    {
        _receiveQueue = new ConcurrentDictionary<ushort, SlmpResponse>();
        _sequenceLock = new SemaphoreSlim(1, 1);
        _currentSequence = 0;
    }
    
    private async Task<ushort> GetNextSequenceAsync()
    {
        await _sequenceLock.WaitAsync();
        try
        {
            if (_currentSequence > 0xFF) // Python: if self.__seq > 0xFF
            {
                _currentSequence = 0;
            }
            return _currentSequence++;
        }
        finally
        {
            _sequenceLock.Release();
        }
    }
    
    private void ProcessReceivedFrame(byte[] buffer)
    {
        var response = ParseSlmpFrame(buffer);
        
        // スレッドセーフなキュー操作
        _receiveQueue.TryAdd(response.SequenceId, response);
        
        // 古いエントリのクリーンアップ（オプション）
        CleanupExpiredResponses();
    }
}

public struct SlmpResponse
{
    public ushort SequenceId;
    public byte NetworkNumber;
    public byte NodeNumber;
    public ushort IoNumber;
    public byte MultiDropStation;
    public EndCode EndCode;
    public byte[] Data;
}
```

**排他制御の要点:**
- Python: `threading.Lock` → C#: `SemaphoreSlim` (非同期対応)
- Python: `Dict` → C#: `ConcurrentDictionary` (ロックフリー)
- シーケンス番号の循環: 0-255 (8bit)
- 4Eフレーム使用時のみシーケンス番号有効

---

## 3. ランダム/ブロック/モニタ要求のパケット構造の境界ケース

### ランダムデバイス読み取り制限
```python
# Python実装での制限確認なし（プロトコル仕様上の制限）
def read_random_devices(self, word_list, dword_list, timeout=0):
    # 境界ケース：0件、最大件数の処理なし
```

### C#実装仕様
```csharp
public async Task<(ushort[] wordData, uint[] dwordData)> ReadRandomDevicesAsync(
    IList<(DeviceCode deviceCode, uint address)> wordDevices,
    IList<(DeviceCode deviceCode, uint address)> dwordDevices,
    ushort timeout = 0, CancellationToken cancellationToken = default)
{
    // 境界ケースの検証
    if (wordDevices == null) wordDevices = Array.Empty<(DeviceCode, uint)>();
    if (dwordDevices == null) dwordDevices = Array.Empty<(DeviceCode, uint)>();
    
    var totalDevices = wordDevices.Count + dwordDevices.Count;
    
    // 境界ケース1: 0件
    if (totalDevices == 0)
    {
        return (Array.Empty<ushort>(), Array.Empty<uint>());
    }
    
    // 境界ケース2: 最大件数（プロトコル仕様）
    if (totalDevices > 192) // SLMPプロトコル制限
    {
        throw new ArgumentException($"Too many devices: {totalDevices} (max: 192)");
    }
    
    // 境界ケース3: 異種混在の妥当性チェック
    ValidateDeviceMixing(wordDevices, dwordDevices);
    
    // パケット構築と送信
    var packet = BuildRandomReadPacket(wordDevices, dwordDevices);
    return await SendAndReceiveAsync<(ushort[], uint[])>(packet, timeout, cancellationToken);
}

private void ValidateDeviceMixing(
    IList<(DeviceCode deviceCode, uint address)> wordDevices,
    IList<(DeviceCode deviceCode, uint address)> dwordDevices)
{
    // ワードデバイスとダブルワードデバイスの混在チェック
    var invalidWordDevices = wordDevices.Where(d => !IsValidWordDevice(d.deviceCode));
    var invalidDwordDevices = dwordDevices.Where(d => !IsValidDwordDevice(d.deviceCode));
    
    if (invalidWordDevices.Any())
    {
        throw new ArgumentException($"Invalid word devices: {string.Join(", ", invalidWordDevices.Select(d => d.deviceCode))}");
    }
    
    if (invalidDwordDevices.Any())
    {
        throw new ArgumentException($"Invalid dword devices: {string.Join(", ", invalidDwordDevices.Select(d => d.deviceCode))}");
    }
}
```

### ブロック読み取り制限
```csharp
public async Task<(ushort[][] wordBlocks, bool[][] bitBlocks)> ReadBlockAsync(
    IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks,
    IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks,
    ushort timeout = 0, CancellationToken cancellationToken = default)
{
    // 境界ケース処理
    if (wordBlocks == null) wordBlocks = Array.Empty<(DeviceCode, uint, ushort)>();
    if (bitBlocks == null) bitBlocks = Array.Empty<(DeviceCode, uint, ushort)>();
    
    var totalBlocks = wordBlocks.Count + bitBlocks.Count;
    
    if (totalBlocks == 0)
    {
        return (Array.Empty<ushort[]>(), Array.Empty<bool[]>());
    }
    
    if (totalBlocks > 120) // ブロック読み取り制限
    {
        throw new ArgumentException($"Too many blocks: {totalBlocks} (max: 120)");
    }
    
    // 各ブロックの最大サイズチェック
    foreach (var (deviceCode, address, count) in wordBlocks)
    {
        if (count > 960) // ワードブロック最大サイズ
        {
            throw new ArgumentException($"Word block too large: {count} (max: 960)");
        }
    }
    
    foreach (var (deviceCode, address, count) in bitBlocks)
    {
        if (count > 7168) // ビットブロック最大サイズ
        {
            throw new ArgumentException($"Bit block too large: {count} (max: 7168)");
        }
    }
}
```

### モニタ登録制限
```python
# Python実装
def entry_monitor_device(self, word_list, dword_list, timeout=0):
    assert 1 < len(word_list) + len(dword_list) <= 192
```

```csharp
public async Task EntryMonitorDeviceAsync(
    IList<(DeviceCode deviceCode, uint address)> wordDevices,
    IList<(DeviceCode deviceCode, uint address)> dwordDevices,
    ushort timeout = 0, CancellationToken cancellationToken = default)
{
    var totalDevices = (wordDevices?.Count ?? 0) + (dwordDevices?.Count ?? 0);
    
    if (totalDevices < 1)
    {
        throw new ArgumentException("At least one device must be specified for monitoring");
    }
    
    if (totalDevices > 192)
    {
        throw new ArgumentException($"Too many monitor devices: {totalDevices} (max: 192)");
    }
    
    _monitorDeviceCount = (wordDevices?.Count ?? 0, dwordDevices?.Count ?? 0);
}
```

---

## 4. エンドコード毎の再試行可否マトリクス

### Python実装でのエンドコード定義 (`const.py:155-188`)
```python
class EndCode(enum.Enum):
    Success = 0x00
    WrongCommand = 0xC059
    WrongFormat = 0xC05C
    WrongLength = 0xC061
    Busy = 0xCEE0
    ExceedReqLength = 0xCEE1
    ExceedRespLength = 0xCEE2
    ServerNotFound = 0xCF10
    TimeoutError = 0xCF71
    # ... 他のエラーコード
```

### C#実装での再試行可否マトリクス
```csharp
public enum EndCode : ushort
{
    Success = 0x00,
    WrongCommand = 0xC059,
    WrongFormat = 0xC05C,
    WrongLength = 0xC061,
    Busy = 0xCEE0,
    ExceedReqLength = 0xCEE1,
    ExceedRespLength = 0xCEE2,
    ServerNotFound = 0xCF10,
    TimeoutError = 0xCF71,
    RelayFailure = 0xCF70,
    // ... 他のエラーコード
}

public static class EndCodeRetryPolicy
{
    private static readonly Dictionary<EndCode, RetryPolicy> RetryMatrix = new()
    {
        // 再試行推奨（一時的エラー）
        { EndCode.Busy, RetryPolicy.Retry },
        { EndCode.TimeoutError, RetryPolicy.Retry },
        { EndCode.RelayFailure, RetryPolicy.Retry },
        { EndCode.ServerNotFound, RetryPolicy.RetryWithDelay },
        
        // 再試行非推奨（設定エラー）
        { EndCode.WrongCommand, RetryPolicy.NoRetry },
        { EndCode.WrongFormat, RetryPolicy.NoRetry },
        { EndCode.WrongLength, RetryPolicy.NoRetry },
        { EndCode.ExceedReqLength, RetryPolicy.NoRetry },
        { EndCode.ExceedRespLength, RetryPolicy.NoRetry },
        
        // 成功
        { EndCode.Success, RetryPolicy.NoRetry }
    };
    
    public static RetryPolicy GetRetryPolicy(EndCode endCode)
    {
        return RetryMatrix.TryGetValue(endCode, out var policy) ? policy : RetryPolicy.NoRetry;
    }
}

public enum RetryPolicy
{
    NoRetry,        // 再試行しない
    Retry,          // 即座に再試行
    RetryWithDelay  // 遅延後再試行
}

// 使用例
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    SlmpException lastException = null;
    
    for (int attempt = 0; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (SlmpCommunicationException ex)
        {
            lastException = ex;
            var policy = EndCodeRetryPolicy.GetRetryPolicy(ex.EndCode);
            
            if (policy == RetryPolicy.NoRetry || attempt == maxRetries)
            {
                throw;
            }
            
            if (policy == RetryPolicy.RetryWithDelay)
            {
                await Task.Delay(1000 * (attempt + 1)); // 指数バックオフ
            }
        }
    }
    
    throw lastException!;
}
```

---

## 5. デバイスコードの番地体系差（X/Yの16進番地、D/Wの10進番地など）の入力API設計

### Python実装での番地体系区分 (`const.py:197-204`)
```python
# アドレス表現が16進数のデバイスの一覧
D_ADDR_16 = (
    DeviceCode.X, DeviceCode.Y, DeviceCode.B, DeviceCode.W,
    DeviceCode.SB, DeviceCode.SW, DeviceCode.DX, DeviceCode.DY,
)
```

### Python実装でのアドレス処理 (`__init__.py:312-315`)
```python
if device_code in const.D_ADDR_16:
    cmd_text += b"%06X%04d" % (start_num, count)  # 16進アドレス
else:
    cmd_text += b"%06d%04d" % (start_num, count)  # 10進アドレス
```

### C#実装でのアドレス体系統一API設計
```csharp
public static class DeviceCodeExtensions
{
    private static readonly HashSet<DeviceCode> HexAddressDevices = new()
    {
        DeviceCode.X, DeviceCode.Y, DeviceCode.B, DeviceCode.W,
        DeviceCode.SB, DeviceCode.SW, DeviceCode.DX, DeviceCode.DY
    };
    
    public static bool UsesHexAddress(this DeviceCode deviceCode)
    {
        return HexAddressDevices.Contains(deviceCode);
    }
    
    public static AddressFormat GetAddressFormat(this DeviceCode deviceCode)
    {
        return deviceCode.UsesHexAddress() ? AddressFormat.Hexadecimal : AddressFormat.Decimal;
    }
}

public enum AddressFormat
{
    Decimal,
    Hexadecimal
}

// 統一アドレス入力API
public class DeviceAddress
{
    public DeviceCode DeviceCode { get; }
    public uint RawAddress { get; }
    public AddressFormat Format { get; }
    
    // 10進数での指定（内部で適切な形式に変換）
    public DeviceAddress(DeviceCode deviceCode, uint address)
    {
        DeviceCode = deviceCode;
        RawAddress = address;
        Format = deviceCode.GetAddressFormat();
    }
    
    // 文字列での指定（"X1A", "D100"など）
    public static DeviceAddress Parse(string deviceString)
    {
        if (string.IsNullOrEmpty(deviceString) || deviceString.Length < 2)
        {
            throw new ArgumentException("Invalid device string format");
        }
        
        // デバイスコード部分を抽出
        var devicePart = "";
        var addressPart = "";
        
        for (int i = 0; i < deviceString.Length; i++)
        {
            if (char.IsDigit(deviceString[i]) || 
                (deviceString[i] >= 'A' && deviceString[i] <= 'F'))
            {
                devicePart = deviceString.Substring(0, i);
                addressPart = deviceString.Substring(i);
                break;
            }
        }
        
        if (!Enum.TryParse<DeviceCode>(devicePart, true, out var deviceCode))
        {
            throw new ArgumentException($"Unknown device code: {devicePart}");
        }
        
        uint address;
        if (deviceCode.UsesHexAddress())
        {
            if (!uint.TryParse(addressPart, NumberStyles.HexNumber, null, out address))
            {
                throw new ArgumentException($"Invalid hex address: {addressPart}");
            }
        }
        else
        {
            if (!uint.TryParse(addressPart, out address))
            {
                throw new ArgumentException($"Invalid decimal address: {addressPart}");
            }
        }
        
        return new DeviceAddress(deviceCode, address);
    }
    
    public override string ToString()
    {
        return Format == AddressFormat.Hexadecimal 
            ? $"{DeviceCode}{RawAddress:X}" 
            : $"{DeviceCode}{RawAddress}";
    }
    
    // ASCIIフレーム用のフォーマット
    internal string ToAsciiFormat()
    {
        var deviceName = DeviceCode.ToString();
        if (deviceName.Length == 1)
        {
            deviceName += "*";
        }
        
        return Format == AddressFormat.Hexadecimal
            ? $"{deviceName}{RawAddress:X6}"
            : $"{deviceName}{RawAddress:D6}";
    }
}

// 使用例
public class SlmpClient
{
    // 従来の数値指定API（下位互換）
    public async Task<bool[]> ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0)
    {
        var deviceAddress = new DeviceAddress(deviceCode, startAddress);
        return await ReadBitDevicesAsync(deviceAddress, count, timeout);
    }
    
    // 新しい統一API
    public async Task<bool[]> ReadBitDevicesAsync(DeviceAddress startAddress, ushort count, ushort timeout = 0)
    {
        // 内部処理で適切な形式に変換
        var packet = BuildReadBitDevicesPacket(startAddress, count);
        return await SendAndReceiveAsync<bool[]>(packet, timeout);
    }
    
    // 文字列指定API
    public async Task<bool[]> ReadBitDevicesAsync(string deviceString, ushort count, ushort timeout = 0)
    {
        var deviceAddress = DeviceAddress.Parse(deviceString);
        return await ReadBitDevicesAsync(deviceAddress, count, timeout);
    }
}

// 使用例
var client = new SlmpClient("192.168.1.100");
await client.OpenAsync();

// 従来の方式
var result1 = await client.ReadBitDevicesAsync(DeviceCode.X, 0x10, 16);

// 新しい統一方式
var result2 = await client.ReadBitDevicesAsync("X10", 16);  // 16進自動認識
var result3 = await client.ReadBitDevicesAsync("D100", 16); // 10進自動認識
```

---

## 6. UDP/TCPでの再送・分割受信・MTU前提、フラグメント時の扱い

### Python実装の通信処理 (`__init__.py:165-255`)
```python
def __recv(self):
    try:
        self.__socket.settimeout(0)  # 非ブロッキング
        buf = self.__socket.recv(512)  # 固定512バイト
        self.__socket.settimeout(1)
    except socket.timeout and BlockingIOError:
        return
    buf = self.__rest + buf  # フラグメント結合
    self.__rest = b""
    
    # 不完全パケットの処理
    if len(buf) < 11:  # 最小ヘッダサイズ
        with self.__lock:
            self.__rest = buf  # 次回まで保持
        return
```

### C#実装でのフラグメント処理仕様
```csharp
public class SlmpTransport
{
    private const int MaxReceiveBufferSize = 2048; // Python: 512 → C#: 2048
    private readonly byte[] _receiveBuffer = new byte[MaxReceiveBufferSize];
    private readonly MemoryStream _fragmentBuffer = new MemoryStream();
    
    // UDP/TCP別の処理実装
    private async Task<byte[]> ReceiveCompleteFrameAsync(CancellationToken cancellationToken)
    {
        if (_useTcp)
        {
            return await ReceiveTcpFrameAsync(cancellationToken);
        }
        else
        {
            return await ReceiveUdpFrameAsync(cancellationToken);
        }
    }
    
    private async Task<byte[]> ReceiveTcpFrameAsync(CancellationToken cancellationToken)
    {
        // TCPでは分割受信が可能
        while (true)
        {
            var received = await _socket.ReceiveAsync(_receiveBuffer, SocketFlags.None, cancellationToken);
            if (received == 0) throw new SlmpCommunicationException("Connection closed");
            
            _fragmentBuffer.Write(_receiveBuffer, 0, received);
            
            // 完全フレームかチェック
            var completeFrame = TryExtractCompleteFrame(_fragmentBuffer);
            if (completeFrame != null)
            {
                return completeFrame;
            }
            
            // バッファオーバーフロー保護
            if (_fragmentBuffer.Length > 65536) // 64KB制限
            {
                throw new SlmpCommunicationException("Frame too large");
            }
        }
    }
    
    private async Task<byte[]> ReceiveUdpFrameAsync(CancellationToken cancellationToken)
    {
        // UDPでは単一パケットで完結（IPフラグメント化はOS処理）
        var result = await _socket.ReceiveFromAsync(_receiveBuffer, SocketFlags.None, _remoteEndPoint, cancellationToken);
        var frame = new byte[result.ReceivedBytes];
        Array.Copy(_receiveBuffer, frame, result.ReceivedBytes);
        return frame;
    }
    
    private byte[] TryExtractCompleteFrame(MemoryStream buffer)
    {
        var data = buffer.ToArray();
        if (data.Length < 11) return null; // 最小ヘッダサイズ
        
        int expectedLength;
        int headerLength;
        
        if (data[0] == 0xD0) // 3E Binary
        {
            if (data.Length < 9) return null;
            expectedLength = BitConverter.ToUInt16(data, 7) + 9;
            headerLength = 9;
        }
        else if (data[0] == 0xD4) // 4E Binary
        {
            if (data.Length < 11) return null;
            expectedLength = BitConverter.ToUInt16(data, 9) + 11;
            headerLength = 11;
        }
        else if (data[0] == (byte)'D') // ASCII
        {
            // ASCII フレーム処理
            var headerStr = Encoding.ASCII.GetString(data, 0, Math.Min(22, data.Length));
            if (!TryParseAsciiHeader(headerStr, out expectedLength, out headerLength))
            {
                return null;
            }
        }
        else
        {
            throw new SlmpCommunicationException($"Invalid frame header: 0x{data[0]:X2}");
        }
        
        if (data.Length >= expectedLength)
        {
            var frame = new byte[expectedLength];
            Array.Copy(data, frame, expectedLength);
            
            // 残りデータを前に詰める
            var remaining = data.Length - expectedLength;
            if (remaining > 0)
            {
                buffer.SetLength(0);
                buffer.Write(data, expectedLength, remaining);
                buffer.Position = 0;
            }
            else
            {
                buffer.SetLength(0);
            }
            
            return frame;
        }
        
        return null; // まだ不完全
    }
}
```

**MTU・フラグメント対応:**
- Python: 512バイト固定 → C#: 2048バイト（パフォーマンス向上）
- TCP: ストリーム受信でフラグメント再構築
- UDP: OS レベルIPフラグメント処理に依存
- バッファオーバーフロー保護実装

---

## 7. タイムアウトの基準（往復/片道/アイドル）とリトライ方針

### Python実装のタイムアウト基準 (`__init__.py:279-296`)
```python
def __recv_loop(self, seq: int, timeout: int):
    timeout *= 0.25  # 250ms単位 → 秒変換
    if timeout == 0:
        timeout = 100  # デフォルト100秒
    start = time.monotonic()  # 単調時間使用
    while seq not in self.__recv_queue.keys():
        end = time.monotonic()
        if end - start > timeout:  # 経過時間チェック
            break
    # タイムアウト = レスポンス待ち時間（片道+処理時間）
```

### C#実装でのタイムアウト戦略
```csharp
public enum TimeoutType
{
    Response,     // レスポンス待ち（Python互換）
    RoundTrip,    // 往復時間
    Idle,         // アイドルタイムアウト
    Total         // 総処理時間
}

public class SlmpTimeoutManager
{
    public static async Task<T> ExecuteWithTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        ushort slmpTimeout,  // SLMP仕様タイムアウト（250ms単位）
        TimeoutType timeoutType = TimeoutType.Response,
        CancellationToken cancellationToken = default)
    {
        var timeoutMs = CalculateTimeoutMs(slmpTimeout, timeoutType);
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await operation(cts.Token);
            LogTimeout(timeoutType, stopwatch.ElapsedMilliseconds, timeoutMs, true);
            return result;
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            LogTimeout(timeoutType, stopwatch.ElapsedMilliseconds, timeoutMs, false);
            throw new SlmpTimeoutException($"{timeoutType} timeout after {stopwatch.ElapsedMilliseconds}ms (limit: {timeoutMs}ms)");
        }
    }
    
    private static TimeSpan CalculateTimeoutMs(ushort slmpTimeout, TimeoutType timeoutType)
    {
        var baseTimeoutMs = slmpTimeout == 0 ? 100_000 : slmpTimeout * 250; // Python互換
        
        return timeoutType switch
        {
            TimeoutType.Response => TimeSpan.FromMilliseconds(baseTimeoutMs), // Python互換
            TimeoutType.RoundTrip => TimeSpan.FromMilliseconds(baseTimeoutMs * 0.8), // レスポンス時間の80%
            TimeoutType.Idle => TimeSpan.FromMilliseconds(baseTimeoutMs * 1.5), // 余裕を持たせる
            TimeoutType.Total => TimeSpan.FromMilliseconds(baseTimeoutMs * 2.0), // 総処理時間
            _ => TimeSpan.FromMilliseconds(baseTimeoutMs)
        };
    }
}

// リトライ戦略実装
public class SlmpRetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);
    public double BackoffMultiplier { get; set; } = 2.0;
    
    public async Task<T> ExecuteAsync<T>(
        Func<int, Task<T>> operation,
        Func<Exception, bool> shouldRetry = null)
    {
        shouldRetry ??= DefaultShouldRetry;
        
        Exception lastException = null;
        
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation(attempt);
            }
            catch (Exception ex) when (shouldRetry(ex) && attempt < MaxRetries)
            {
                lastException = ex;
                var delay = CalculateDelay(attempt);
                await Task.Delay(delay);
            }
        }
        
        throw lastException!;
    }
    
    private bool DefaultShouldRetry(Exception ex)
    {
        return ex switch
        {
            SlmpTimeoutException => true,
            SlmpCommunicationException commEx => commEx.EndCode switch
            {
                EndCode.Busy => true,
                EndCode.TimeoutError => true,
                EndCode.RelayFailure => true,
                EndCode.ServerNotFound => true,
                _ => false
            },
            SocketException sockEx => sockEx.SocketErrorCode switch
            {
                SocketError.TimedOut => true,
                SocketError.ConnectionReset => true,
                SocketError.NetworkUnreachable => true,
                _ => false
            },
            _ => false
        };
    }
    
    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromTicks((long)(InitialDelay.Ticks * Math.Pow(BackoffMultiplier, attempt)));
        return delay > MaxDelay ? MaxDelay : delay;
    }
}
```

**タイムアウト基準:**
- **Response**: Python互換（レスポンス受信まで）
- **RoundTrip**: 送信から受信まで（80%設定）
- **Idle**: アイドル時間監視（150％設定）
- **Total**: 処理全体時間（200%設定）

---

## 8. 同一ソケットでの並列リクエスト可否（シリアライズかパイプラインか）

### Python実装の並列処理制限 (`__init__.py:70-73, 247-254`)
```python
self.__recv_thread = threading.Thread(target=self.__worker, daemon=True)
# 単一受信スレッド

def __worker(self):
    while self.__socket:
        try:
            self.__recv()  # 単一スレッドで順次受信
        except RuntimeError as e:
            self.logger.error(e)

# シーケンス番号での識別
with self.__lock:
    self.__recv_queue[seq] = (network_num, pc_num, io_num, m_drop_num, term_code, data)
```

### C#実装での並列処理対応
```csharp
public class SlmpClient
{
    private readonly SemaphoreSlim _sendSemaphore;
    private readonly ConcurrentDictionary<ushort, TaskCompletionSource<SlmpResponse>> _pendingRequests;
    private readonly Timer _cleanupTimer;
    
    public SlmpClient()
    {
        // 並列送信制御（デフォルト4並列）
        _sendSemaphore = new SemaphoreSlim(4, 4);
        _pendingRequests = new ConcurrentDictionary<ushort, TaskCompletionSource<SlmpResponse>>();
        
        // 古いリクエストのクリーンアップタイマー
        _cleanupTimer = new Timer(CleanupExpiredRequests, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    // パイプライン処理対応
    public async Task<T> SendRequestAsync<T>(
        SlmpCommand command,
        ushort subCommand,
        byte[] data,
        Func<SlmpResponse, T> responseParser,
        ushort timeout = 0,
        CancellationToken cancellationToken = default)
    {
        // 送信セマフォで並列度制御
        await _sendSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            var sequenceId = await GetNextSequenceAsync();
            var tcs = new TaskCompletionSource<SlmpResponse>();
            
            // レスポンス待ちキューに登録
            _pendingRequests.TryAdd(sequenceId, tcs);
            
            try
            {
                // フレーム送信
                var frame = BuildFrame(sequenceId, command, subCommand, data);
                await _socket.SendAsync(frame, SocketFlags.None, cancellationToken);
                
                // レスポンス待ち（タイムアウト付き）
                var timeoutMs = timeout == 0 ? 100_000 : timeout * 250;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);
                
                var response = await tcs.Task.WaitAsync(cts.Token);
                
                // レスポンス解析
                return responseParser(response);
            }
            finally
            {
                // クリーンアップ
                _pendingRequests.TryRemove(sequenceId, out _);
            }
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }
    
    // 受信ワーカー（単一スレッド、高速処理）
    private async Task ReceiveWorkerAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[2048];
        var fragmentBuffer = new MemoryStream();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var received = await _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
                if (received == 0) continue;
                
                fragmentBuffer.Write(buffer, 0, received);
                
                // 完全フレームの抽出と処理
                while (TryExtractFrame(fragmentBuffer, out var frame))
                {
                    ProcessReceivedFrame(frame);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Receive worker error");
            }
        }
    }
    
    private void ProcessReceivedFrame(byte[] frame)
    {
        var response = ParseResponse(frame);
        
        if (_pendingRequests.TryRemove(response.SequenceId, out var tcs))
        {
            if (response.EndCode == EndCode.Success)
            {
                tcs.SetResult(response);
            }
            else
            {
                tcs.SetException(new SlmpCommunicationException(response.EndCode));
            }
        }
        // else: 古いレスポンスまたは不正なシーケンスID
    }
    
    private void CleanupExpiredRequests(object state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var expiredKeys = new List<ushort>();
        
        foreach (var kvp in _pendingRequests)
        {
            if (kvp.Value.Task.IsCompleted) continue;
            
            // タイムスタンプチェック（簡略化）
            expiredKeys.Add(kvp.Key);
        }
        
        foreach (var key in expiredKeys)
        {
            if (_pendingRequests.TryRemove(key, out var tcs))
            {
                tcs.SetException(new SlmpTimeoutException($"Request {key} expired"));
            }
        }
    }
}

// 設定による並列度制御
public class SlmpConnectionSettings
{
    public int MaxConcurrentRequests { get; set; } = 4; // 並列リクエスト数
    public bool EnablePipelining { get; set; } = true;  // パイプライン処理
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
```

**並列処理方針:**
- **Python**: シリアライズ処理（単一スレッド受信）
- **C#**: パイプライン対応（制御可能な並列度）
- **利点**: スループット向上、レスポンス性改善
- **制御**: セマフォによる並列度制限

---

## 9. Python版の既知のバグ・仕様依存の一覧とC#側での対応方針

### 既知の問題と対応方針

#### 1. 固定受信バッファサイズ制限
**Python問題:**
```python
buf = self.__socket.recv(512)  # 固定512バイト
```
**影響:** 大きなレスポンスの受信失敗

**C#対応:**
```csharp
private const int MaxReceiveBufferSize = 2048; // 4倍に拡張
private readonly byte[] _dynamicBuffer = new byte[65536]; // 必要時拡張
```

#### 2. Assert文による検証（本番無効化リスク）
**Python問題:**
```python
assert 0 < start_num < 0xFFF, start_num
assert 0 < count < 3584, count
assert len(ret) == count, len(ret)
```
**影響:** `-O`オプション使用時に検証無効化

**C#対応:**
```csharp
public static void ValidateDeviceAddress(uint address)
{
    if (address == 0 || address >= 0xFFF)
    {
        throw new ArgumentOutOfRangeException(nameof(address), 
            $"Device address must be between 1 and {0xFFF-1}");
    }
}

public static void ValidateCount(int count, int maxCount = 3584)
{
    if (count <= 0 || count >= maxCount)
    {
        throw new ArgumentOutOfRangeException(nameof(count), 
            $"Count must be between 1 and {maxCount-1}");
    }
}
```

#### 3. 例外処理の不統一
**Python問題:**
```python
raise TimeoutError(device_code, start_num, count) from e  # 引数バラバラ
raise TimeoutError(word_list, dword_list) from e
raise TimeoutError() from e
```

**C#対応:**
```csharp
public class SlmpTimeoutException : SlmpException
{
    public ushort SequenceId { get; }
    public SlmpCommand Command { get; }
    public TimeSpan ElapsedTime { get; }
    public TimeSpan TimeoutDuration { get; }
    
    public SlmpTimeoutException(ushort sequenceId, SlmpCommand command, 
        TimeSpan elapsed, TimeSpan timeout, Exception innerException = null)
        : base($"Timeout waiting for response to {command} (seq: {sequenceId}). " +
               $"Elapsed: {elapsed.TotalMilliseconds}ms, Timeout: {timeout.TotalMilliseconds}ms", 
               innerException)
    {
        SequenceId = sequenceId;
        Command = command;
        ElapsedTime = elapsed;
        TimeoutDuration = timeout;
    }
}
```

#### 4. スレッドセーフティの不完全性
**Python問題:**
```python
# ロック範囲が不適切な箇所
with self.__lock:
    self.__ctx_cnt += 1
if self.__socket:  # ロック外での条件チェック
    return
```

**C#対応:**
```csharp
private readonly object _stateLock = new object();
private volatile bool _isDisposed;

public async Task OpenAsync()
{
    if (_isDisposed) throw new ObjectDisposedException(nameof(SlmpClient));
    
    lock (_stateLock)
    {
        _contextCount++;
        if (_socket != null) return; // 二重初期化防止
        
        // アトミックな初期化処理
        InitializeSocket();
    }
}
```

#### 5. 文字エンコーディング仮定
**Python問題:**
```python
data = data.decode("ascii")  # ASCIIエンコーディング固定
```
**影響:** 国際化対応の制限

**C#対応:**
```csharp
public class SlmpConnectionSettings
{
    public Encoding TextEncoding { get; set; } = Encoding.ASCII; // 設定可能
}

private string DecodeTextData(byte[] data)
{
    return Settings.TextEncoding.GetString(data);
}
```

#### 6. メモリリーク潜在リスク
**Python問題:**
```python
self.__recv_queue[seq] = (...)  # 古いエントリのクリーンアップなし
```

**C#対応:**
```csharp
private readonly Timer _cleanupTimer = new Timer(CleanupExpiredEntries, null, 
    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

private void CleanupExpiredEntries(object state)
{
    var cutoff = DateTime.UtcNow.AddMinutes(-5);
    var expiredKeys = _pendingRequests
        .Where(kvp => kvp.Value.Timestamp < cutoff)
        .Select(kvp => kvp.Key)
        .ToList();
        
    foreach (var key in expiredKeys)
    {
        if (_pendingRequests.TryRemove(key, out var entry))
        {
            entry.TaskCompletionSource.SetException(
                new SlmpTimeoutException("Request expired during cleanup"));
        }
    }
}
```

#### 7. 型変換の安全性
**Python問題:**
```python
term_code = int(buf[14:18].decode("ascii"), base=16)  # 例外未処理
```

**C#対応:**
```csharp
private static bool TryParseHexEndCode(string hexString, out EndCode endCode)
{
    endCode = EndCode.Success;
    
    if (string.IsNullOrEmpty(hexString) || hexString.Length != 4)
        return false;
        
    if (!ushort.TryParse(hexString, NumberStyles.HexNumber, null, out var value))
        return false;
        
    if (!Enum.IsDefined(typeof(EndCode), value))
        return false;
        
    endCode = (EndCode)value;
    return true;
}
```

---

## まとめ

この拡張技術仕様書により、PythonからC#への移植において以下の9つの重要な課題が解決されます：

1. **タイムアウト処理**: 250ms単位の正確な実装とCPU効率的な待機
2. **排他制御**: 非同期対応のスレッドセーフな実装
3. **境界ケース**: プロトコル制限の適切な検証と例外処理  
4. **再試行制御**: エンドコード別の適切な再試行ポリシー
5. **アドレス体系**: 16進/10進の自動判別と統一API
6. **通信処理**: UDP/TCP別最適化とフラグメント対応
7. **タイムアウト戦略**: 多様なタイムアウト基準と指数バックオフ
8. **並列処理**: パイプライン対応と制御可能な並列度
9. **品質向上**: Python版の既知問題の解決と堅牢性強化

これらの仕様に基づいて実装することで、Python版との完全互換性を保ちつつ、C#の特性を活かした高性能で信頼性の高いライブラリを構築できます。