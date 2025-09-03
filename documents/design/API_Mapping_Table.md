# PySLMPClient → C# API対応表

## メソッド対応表

### 接続管理

| Python | C# | 引数 | 戻り値 | 備考 |
|--------|----|----|-------|------|
| `__init__(addr, port=5000, binary=True, ver=4, tcp=False)` | `SlmpClient(string address, SlmpConnectionSettings settings = null)` | 設定クラス化 | - | 設定パラメータを専用クラスに分離 |
| `open()` | `OpenAsync(CancellationToken cancellationToken = default)` | キャンセルトークン追加 | `Task` | 非同期化 |
| `close()` | `CloseAsync()` | - | `Task` | 非同期化 |
| `__enter__()` | - | - | - | usingステートメントで代替 |
| `__exit__()` | `Dispose()` / `DisposeAsync()` | - | `void` / `ValueTask` | IDisposable実装 |

### デバイス読み書き

| Python | C# | 引数変更 | 戻り値変更 | 備考 |
|--------|----|----|-------|------|
| `read_bit_devices(device_code, start_num, count, timeout=0)` | `ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default)` | キャンセルトークン追加 | `Tuple[bool]` → `Task<bool[]>` | 非同期化、配列化 |
| `read_word_devices(device_code, start_num, count, timeout=0)` | `ReadWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default)` | キャンセルトークン追加 | `array` → `Task<ushort[]>` | 非同期化、型明確化 |
| `write_bit_devices(dc2, start_num, data, timeout=0)` | `WriteBitDevicesAsync(DeviceCode deviceCode, uint startAddress, bool[] data, ushort timeout = 0, CancellationToken cancellationToken = default)` | 引数名統一 | `void` → `Task` | 非同期化 |
| `write_word_devices(dc2, start_num, data, timeout=0)` | `WriteWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort[] data, ushort timeout = 0, CancellationToken cancellationToken = default)` | 引数名統一 | `void` → `Task` | 非同期化 |

### ランダムアクセス

| Python | C# | 引数変更 | 戻り値変更 | 備考 |
|--------|----|----|-------|------|
| `read_random_devices(word_list, dword_list, timeout=0)` | `ReadRandomDevicesAsync(IList<(DeviceCode deviceCode, uint address)> wordDevices, IList<(DeviceCode deviceCode, uint address)> dwordDevices, ushort timeout = 0, CancellationToken cancellationToken = default)` | ジェネリック型使用 | `(List[int], List[bytes])` → `Task<(ushort[] wordData, uint[] dwordData)>` | タプル記法、型明確化 |
| `write_random_bit_devices(device_list, timeout=0)` | `WriteRandomBitDevicesAsync(IList<(DeviceCode deviceCode, uint address, bool value)> devices, ushort timeout = 0, CancellationToken cancellationToken = default)` | タプル使用 | `void` → `Task` | 非同期化 |
| `write_random_word_devices(word_list, dword_list, timeout=0)` | `WriteRandomWordDevicesAsync(IList<(DeviceCode deviceCode, uint address, ushort value)> wordDevices, IList<(DeviceCode deviceCode, uint address, uint value)> dwordDevices, ushort timeout = 0, CancellationToken cancellationToken = default)` | 型安全性向上 | `void` → `Task` | 非同期化 |

### ブロック操作

| Python | C# | 引数変更 | 戻り値変更 | 備考 |
|--------|----|----|-------|------|
| `read_block(word_list, bit_list, timeout=0)` | `ReadBlockAsync(IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks, IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks, ushort timeout = 0, CancellationToken cancellationToken = default)` | タプル使用 | `(List[List[int]], List[List[bool]])` → `Task<(ushort[][] wordBlocks, bool[][] bitBlocks)>` | ジャグ配列使用 |
| `write_block(word_list, bit_list, timeout=0)` | `WriteBlockAsync(IList<(DeviceCode deviceCode, uint address, ushort count, ushort[] data)> wordBlocks, IList<(DeviceCode deviceCode, uint address, ushort count, bool[] data)> bitBlocks, ushort timeout = 0, CancellationToken cancellationToken = default)` | データ型明確化 | `void` → `Task` | 非同期化 |

### モニタ機能

| Python | C# | 引数変更 | 戻り値変更 | 備考 |
|--------|----|----|-------|------|
| `entry_monitor_device(word_list, dword_list, timeout=0)` | `EntryMonitorDeviceAsync(IList<(DeviceCode deviceCode, uint address)> wordDevices, IList<(DeviceCode deviceCode, uint address)> dwordDevices, ushort timeout = 0, CancellationToken cancellationToken = default)` | - | `void` → `Task` | 非同期化 |
| `execute_monitor(timeout=0)` | `ExecuteMonitorAsync(ushort timeout = 0, CancellationToken cancellationToken = default)` | - | `(List[int], List[bytes])` → `Task<(ushort[] wordData, uint[] dwordData)>` | 型明確化 |

### システム機能

| Python | C# | 引数変更 | 戻り値変更 | 備考 |
|--------|----|----|-------|------|
| `read_type_name(timeout=0)` | `ReadTypeNameAsync(ushort timeout = 0, CancellationToken cancellationToken = default)` | - | `(str, const.TypeCode)` → `Task<(string typeName, TypeCode typeCode)>` | 名前付きタプル |
| `self_test(data=None, timeout=0)` | `SelfTestAsync(string data = null, ushort timeout = 0, CancellationToken cancellationToken = default)` | - | `bool` → `Task<bool>` | 非同期化 |
| `clear_error(timeout=0)` | `ClearErrorAsync(ushort timeout = 0, CancellationToken cancellationToken = default)` | - | `void` → `Task` | 非同期化 |
| `check_on_demand_data()` | `CheckOnDemandData()` | - | `Optional[bytes]` → `byte[]?` | nullable型 |

### メモリアクセス

| Python | C# | 引数変更 | 戻り値変更 | 備考 |
|--------|----|----|-------|------|
| `memory_read(addr, length, timeout=0)` | `MemoryReadAsync(uint address, ushort length, ushort timeout = 0, CancellationToken cancellationToken = default)` | 引数名変更 | `List[bytes]` → `Task<byte[]>` | 単一配列化 |
| `memory_write(addr, data, timeout=0)` | `MemoryWriteAsync(uint address, byte[] data, ushort timeout = 0, CancellationToken cancellationToken = default)` | 引数名変更 | `void` → `Task` | 非同期化 |

## プロパティ対応表

### SLMPClient プロパティ

| Python | C# | 型変更 | 備考 |
|--------|----|----|------|
| `target` | `Target` | `util.Target` → `SlmpTarget` | クラス名変更 |
| `logger` | - | - | ILoggerに置き換え |

### Target プロパティ

| Python | C# | 型変更 | 備考 |
|--------|----|----|------|
| `network` | `Network` | `int` → `byte` | 範囲制限による型変更 |
| `node` | `Node` | `int` → `byte` | 範囲制限による型変更 |
| `dst_proc` | `DestinationProcessor` | `int` → `ushort` | 命名・型変更 |
| `m_drop` | `MultiDropStation` | `int` → `byte` | 命名・型変更 |

## 定数・列挙型対応表

### 列挙型

| Python | C# | 変更点 |
|--------|----|----|
| `const.SLMPCommand` | `SlmpCommand` | 名前空間統一 |
| `const.DeviceCode` | `DeviceCode` | 同上 |
| `const.TypeCode` | `TypeCode` | 同上 |
| `const.EndCode` | `EndCode` | 同上 |
| `const.PDU` | `Pdu` | 使用頻度低いため小文字化 |

### 定数配列

| Python | C# | 型変更 |
|--------|----|----|
| `const.D_ADDR_16` | `DeviceCodeExtensions.IsHexAddress(this DeviceCode)` | 拡張メソッド化 |
| `const.D_ADDR_4BYTE` | `DeviceCodeExtensions.Is4ByteAddress(this DeviceCode)` | 拡張メソッド化 |
| `const.D_STRANGE_NAME` | `DeviceCodeExtensions.HasStrangeName(this DeviceCode)` | 拡張メソッド化 |

## ユーティリティ関数対応表

### BCD変換

| Python | C# | 引数変更 | 戻り値変更 |
|--------|----|----|-------|
| `util.encode_bcd(data)` | `BcdConverter.Encode(int[] data)` | - | `List[int]` → `byte[]` |
| `util.decode_bcd(data)` | `BcdConverter.Decode(byte[] data)` | - | `List[int]` → `int[]` |

### ビット操作

| Python | C# | 引数変更 | 戻り値変更 |
|--------|----|----|-------|
| `util.unpack_bits(data)` | `BitConverter.UnpackBits(byte[] data)` | - | `List[int]` → `bool[]` |
| `util.pack_bits(data)` | `BitConverter.PackBits(bool[] data)` | - | `List[int]` → `byte[]` |

### フレーム構築

| Python | C# | 引数変更 | 戻り値変更 |
|--------|----|----|-------|
| `util.make_binary_frame(seq, target, timeout, cmd, sub_cmd, data, ver)` | `FrameBuilder.BuildBinaryFrame(byte sequence, SlmpTarget target, ushort timeout, SlmpCommand command, ushort subCommand, byte[] data, SlmpFrameVersion version)` | 型明確化 | `bytes` → `byte[]` |
| `util.make_ascii_frame(seq, target, timeout, cmd, sub_cmd, data, ver)` | `FrameBuilder.BuildAsciiFrame(byte sequence, SlmpTarget target, ushort timeout, SlmpCommand command, ushort subCommand, byte[] data, SlmpFrameVersion version)` | 型明確化 | `bytes` → `byte[]` |

### データ処理

| Python | C# | 引数変更 | 戻り値変更 |
|--------|----|----|-------|
| `util.str2bytes_buf(data)` | `DataProcessor.StringToBytesBuffer(string data)` | - | `bytearray` → `byte[]` |
| `util.extracts_word_dword_data(buf, split_pos)` | `DataProcessor.ExtractWordDwordData(byte[] buffer, int splitPosition)` | - | `(List[bytes], List[bytes])` → `(byte[][] wordData, byte[][] dwordData)` |
| `util.device2ascii(device_type, address)` | `DataProcessor.DeviceToAscii(DeviceCode deviceType, uint address)` | - | `bytes` → `string` |

## 例外対応表

| Python | C# | 変更点 |
|--------|----|----|
| `util.SLMPError` | `SlmpException` | 基底例外クラス |
| `util.SLMPCommunicationError` | `SlmpCommunicationException` | 通信エラー専用 |
| `TimeoutError` | `SlmpTimeoutException` | タイムアウト専用 |
| `ValueError` | `ArgumentException` | .NET標準例外使用 |
| `RuntimeError` | `InvalidOperationException` | .NET標準例外使用 |
| `AssertionError` | `ArgumentOutOfRangeException` | .NET標準例外使用 |

## 型マッピング詳細

### 基本型

| Python | C# | 用途 |
|--------|----|----|
| `str` | `string` | 文字列 |
| `int` | `int` | 汎用整数 |
| `int` | `uint` | アドレス (非負整数) |
| `int` | `ushort` | カウント・タイムアウト |
| `int` | `byte` | ネットワーク番号等 |
| `bool` | `bool` | フラグ |
| `bytes` | `byte[]` | バイナリデータ |
| `bytearray` | `byte[]` | 可変バイナリデータ |

### コレクション型

| Python | C# | 備考 |
|--------|----|----|
| `List[T]` | `T[]` | 固定長配列 |
| `List[T]` | `IList<T>` | 可変長リスト |
| `Tuple[T1, T2]` | `(T1, T2)` | タプル記法 |
| `Dict[K, V]` | `Dictionary<K, V>` | 辞書 |
| `Optional[T]` | `T?` | nullable参照型 |
| `array.array('H')` | `ushort[]` | 数値配列 |

### 特殊型

| Python | C# | 備考 |
|--------|----|----|
| `threading.Lock` | `lock` / `SemaphoreSlim` | 排他制御 |
| `socket.socket` | `Socket` / `TcpClient` / `UdpClient` | ネットワーク通信 |
| `threading.Thread` | `Task` | 非同期処理 |
| `time.monotonic()` | `Stopwatch` | 経過時間測定 |

## 命名規則変換

### メソッド名
- `snake_case` → `PascalCase`
- `read_bit_devices` → `ReadBitDevices`
- `write_word_devices` → `WriteWordDevices`

### 引数名
- `snake_case` → `camelCase`
- `device_code` → `deviceCode`
- `start_num` → `startAddress`
- `timeout` → `timeout` (変更なし)

### プロパティ名
- `snake_case` → `PascalCase`
- `dst_proc` → `DestinationProcessor`
- `m_drop` → `MultiDropStation`

### 定数名
- `UPPER_SNAKE_CASE` → `PascalCase`
- `D_ADDR_16` → 拡張メソッド化

この対応表に従って実装することで、Python版の機能を完全にC#に移植できます。