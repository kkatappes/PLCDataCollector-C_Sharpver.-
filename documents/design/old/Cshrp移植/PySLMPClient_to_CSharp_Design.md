# PySLMPClient → C# 移植設計書

## 概要

PySLMPClientライブラリを機能の抜け漏れなくC#に完全移植するための詳細設計書です。

## 目次

1. [プロジェクト概要](#プロジェクト概要)
2. [現在のPython実装分析](#現在のpython実装分析)
3. [C#移植設計](#c移植設計)
4. [実装計画](#実装計画)
5. [テスト戦略](#テスト戦略)

---

## プロジェクト概要

### 目的
- PySLMPClientの全機能をC#に移植
- .NETエコシステムに適合した設計
- 性能向上と型安全性の確保

### スコープ
- SLMPプロトコル通信機能の完全移植
- 全デバイスタイプ対応
- 非同期処理対応
- エラーハンドリングの強化

---

## 現在のPython実装分析

### パッケージ構造
```
PySLMPClient/
├── pyslmpclient/
│   ├── __init__.py      # SLMPClientクラス (955行)
│   ├── const.py         # 定数・列挙型 (339行)
│   └── util.py          # ユーティリティ (345行)
└── tests/               # テストコード (1102行)
```

### 主要クラス

#### 1. SLMPClient クラス
**初期化パラメータ:**
- `addr`: IPアドレス (string)
- `port`: ポート番号 (int, default=5000)
- `binary`: バイナリモード (bool, default=True)
- `ver`: フレームバージョン (int, 3または4)
- `tcp`: TCP使用フラグ (bool, default=False)

**主要メソッド:**

| メソッド名 | 機能 | 引数 | 戻り値 |
|------------|------|------|--------|
| `open()` | 接続開始 | - | void |
| `close()` | 接続終了 | - | void |
| `read_bit_devices()` | ビットデバイス読み取り | device_code, start_num, count, timeout | Tuple[bool] |
| `read_word_devices()` | ワードデバイス読み取り | device_code, start_num, count, timeout | array |
| `write_bit_devices()` | ビットデバイス書き込み | dc2, start_num, data, timeout | void |
| `write_word_devices()` | ワードデバイス書き込み | dc2, start_num, data, timeout | void |
| `read_random_devices()` | ランダムデバイス読み取り | word_list, dword_list, timeout | (List[int], List[bytes]) |
| `write_random_bit_devices()` | ランダムビットデバイス書き込み | device_list, timeout | void |
| `write_random_word_devices()` | ランダムワードデバイス書き込み | word_list, dword_list, timeout | void |
| `entry_monitor_device()` | モニタデバイス登録 | word_list, dword_list, timeout | void |
| `execute_monitor()` | モニタ実行 | timeout | (List[int], List[bytes]) |
| `read_block()` | ブロック読み取り | word_list, bit_list, timeout | (List[List[int]], List[List[bool]]) |
| `write_block()` | ブロック書き込み | word_list, bit_list, timeout | void |
| `read_type_name()` | 型名読み取り | timeout | (str, TypeCode) |
| `self_test()` | セルフテスト | data, timeout | bool |
| `clear_error()` | エラークリア | timeout | void |
| `check_on_demand_data()` | オンデマンドデータ確認 | - | Optional[bytes] |
| `memory_read()` | メモリ読み取り | addr, length, timeout | List[bytes] |
| `memory_write()` | メモリ書き込み | addr, data, timeout | void |

#### 2. Target クラス
**プロパティ:**
- `network`: ネットワーク番号 (0-255)
- `node`: 要求先局番 (0-255)  
- `dst_proc`: 要求先プロセッサ番号 (0-65535)
- `m_drop`: 要求先マルチドロップ局番 (0-255)

#### 3. 定数・列挙型

**SLMPCommand enum (118項目):**
- Device操作: Device_Read, Device_Write, Device_ReadRandom, etc.
- Memory操作: Memory_Read, Memory_Write
- RemoteControl: RemoteControl_RemoteRun, RemoteControl_RemoteStop, etc.
- File操作: File_Read, File_Write, File_Delete, etc.

**DeviceCode enum (39項目):**
- SM, SD, X, Y, M, L, F, V, B, D, W, etc.
- 各デバイスの数値コード定義

**TypeCode enum (61項目):**
- Q00JCPU, Q00CPU, Q01CPU, etc.
- PLCタイプ識別用

**EndCode enum (39項目):**
- Success, WrongCommand, WrongFormat, etc.
- エラーコード定義

### 内部実装詳細

#### 通信方式
- **プロトコル**: TCP/UDP選択可能
- **コード**: Binary/ASCII選択可能
- **フレーム**: 3E/4E両対応
- **スレッド**: 受信専用ワーカースレッド
- **タイムアウト**: 250ms単位での監視タイマ

#### データ処理
- **BCD変換**: 4bit BCDエンコード/デコード
- **ビット操作**: LSBファーストのビットパッキング
- **エンディアン**: リトルエンディアン変換
- **フレーム構築**: バイナリ/ASCII両対応

---

## C#移植設計

### 名前空間構造
```
SlmpClient/
├── SlmpClient.Core/
│   ├── SlmpClient.cs           # メインクライアントクラス
│   ├── SlmpTarget.cs           # 通信対象設定
│   ├── SlmpConnectionSettings.cs # 接続設定
│   └── Constants/
│       ├── SlmpCommand.cs      # コマンド列挙型
│       ├── DeviceCode.cs       # デバイスコード列挙型
│       ├── TypeCode.cs         # タイプコード列挙型
│       └── EndCode.cs          # エンドコード列挙型
├── SlmpClient.Utils/
│   ├── BcdConverter.cs         # BCD変換ユーティリティ
│   ├── BitConverter.cs         # ビット変換ユーティリティ
│   ├── FrameBuilder.cs         # フレーム構築
│   └── DataProcessor.cs        # データ処理
├── SlmpClient.Exceptions/
│   ├── SlmpException.cs        # 基底例外クラス
│   ├── SlmpCommunicationException.cs
│   └── SlmpTimeoutException.cs
└── SlmpClient.Tests/           # テストプロジェクト
```

### 主要クラス設計

#### 1. SlmpClient クラス
```csharp
public class SlmpClient : IDisposable, IAsyncDisposable
{
    // コンストラクタ
    public SlmpClient(string address, SlmpConnectionSettings settings = null);
    
    // プロパティ
    public SlmpTarget Target { get; set; }
    public SlmpConnectionSettings Settings { get; }
    public bool IsConnected { get; }
    
    // 接続管理
    public async Task OpenAsync(CancellationToken cancellationToken = default);
    public async Task CloseAsync();
    
    // デバイス読み書き - 非同期版
    public async Task<bool[]> ReadBitDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task<ushort[]> ReadWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task WriteBitDevicesAsync(DeviceCode deviceCode, uint startAddress, bool[] data, ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task WriteWordDevicesAsync(DeviceCode deviceCode, uint startAddress, ushort[] data, ushort timeout = 0, CancellationToken cancellationToken = default);
    
    // ランダムアクセス
    public async Task<(ushort[] wordData, uint[] dwordData)> ReadRandomDevicesAsync(
        IList<(DeviceCode deviceCode, uint address)> wordDevices,
        IList<(DeviceCode deviceCode, uint address)> dwordDevices,
        ushort timeout = 0, CancellationToken cancellationToken = default);
        
    // ブロック操作
    public async Task<(ushort[][] wordBlocks, bool[][] bitBlocks)> ReadBlockAsync(
        IList<(DeviceCode deviceCode, uint address, ushort count)> wordBlocks,
        IList<(DeviceCode deviceCode, uint address, ushort count)> bitBlocks,
        ushort timeout = 0, CancellationToken cancellationToken = default);
        
    // モニタ機能
    public async Task EntryMonitorDeviceAsync(
        IList<(DeviceCode deviceCode, uint address)> wordDevices,
        IList<(DeviceCode deviceCode, uint address)> dwordDevices,
        ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task<(ushort[] wordData, uint[] dwordData)> ExecuteMonitorAsync(ushort timeout = 0, CancellationToken cancellationToken = default);
    
    // システム機能
    public async Task<(string typeName, TypeCode typeCode)> ReadTypeNameAsync(ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task<bool> SelfTestAsync(string data = null, ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task ClearErrorAsync(ushort timeout = 0, CancellationToken cancellationToken = default);
    
    // メモリアクセス
    public async Task<byte[]> MemoryReadAsync(uint address, ushort length, ushort timeout = 0, CancellationToken cancellationToken = default);
    public async Task MemoryWriteAsync(uint address, byte[] data, ushort timeout = 0, CancellationToken cancellationToken = default);
    
    // オンデマンド
    public byte[] CheckOnDemandData();
    
    // 同期版も提供
    public bool[] ReadBitDevices(DeviceCode deviceCode, uint startAddress, ushort count, ushort timeout = 0);
    // ... 他の同期メソッド
}
```

#### 2. SlmpConnectionSettings クラス
```csharp
public class SlmpConnectionSettings
{
    public int Port { get; set; } = 5000;
    public bool IsBinary { get; set; } = true;
    public SlmpFrameVersion Version { get; set; } = SlmpFrameVersion.Version4E;
    public bool UseTcp { get; set; } = false;
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public enum SlmpFrameVersion
{
    Version3E = 3,
    Version4E = 4
}
```

#### 3. SlmpTarget クラス
```csharp
public class SlmpTarget
{
    public byte Network { get; set; } = 0;
    public byte Node { get; set; } = 0;
    public ushort DestinationProcessor { get; set; } = 0;
    public byte MultiDropStation { get; set; } = 0;
    
    public override string ToString() => $"SlmpTarget({Network},{Node},{DestinationProcessor},{MultiDropStation})";
}
```

### 型マッピング表

| Python型 | C#型 | 備考 |
|----------|------|------|
| `str` | `string` | |
| `int` | `int`, `uint`, `ushort`, `byte` | 用途に応じて使い分け |
| `bool` | `bool` | |
| `bytes` | `byte[]` | |
| `List[int]` | `int[]`, `ushort[]` | サイズに応じて |
| `List[bool]` | `bool[]` | |
| `Tuple[...]` | `(...)` | タプル記法使用 |
| `Optional[T]` | `T?` | nullable参照型 |
| `Dict[K,V]` | `Dictionary<K,V>` | |
| `enum.Enum` | `enum` | |

### 命名規則変換

| Python | C# | 例 |
|--------|----|----|
| snake_case | PascalCase | `read_bit_devices` → `ReadBitDevices` |
| 引数名 | camelCase | `device_code` → `deviceCode` |
| プライベートフィールド | _camelCase | `__socket` → `_socket` |
| 定数 | PascalCase | `VERSION` → `Version` |

---

## 実装計画

### フェーズ1: 基盤クラス実装
1. **定数・列挙型の移植** (1-2日)
   - SlmpCommand, DeviceCode, TypeCode, EndCode
   - 型安全性の確保

2. **基本クラス構造** (2-3日)
   - SlmpClient基本構造
   - SlmpTarget, SlmpConnectionSettings
   - 例外クラス階層

3. **通信基盤** (3-4日)
   - TCP/UDP通信層
   - フレーム送受信機能
   - 非同期処理基盤

### フェーズ2: コア機能実装
1. **デバイス読み書き** (4-5日)
   - read_bit_devices, read_word_devices
   - write_bit_devices, write_word_devices
   - バイナリ/ASCII両対応

2. **ランダムアクセス** (2-3日)
   - read_random_devices
   - write_random_bit_devices, write_random_word_devices

3. **ブロック操作** (2-3日)
   - read_block, write_block
   - 複雑なデータ構造処理

### フェーズ3: 高度な機能
1. **モニタ機能** (2-3日)
   - entry_monitor_device
   - execute_monitor

2. **システム機能** (2-3日)
   - read_type_name, self_test
   - clear_error, オンデマンド機能

3. **メモリアクセス** (1-2日)
   - memory_read, memory_write

### フェーズ4: 品質向上
1. **エラーハンドリング強化** (2-3日)
   - 詳細な例外情報
   - リトライ機構

2. **パフォーマンス最適化** (2-3日)
   - メモリ使用量削減
   - 通信効率化

3. **ログ機能** (1-2日)
   - 構造化ログ出力
   - デバッグ支援

---

## テスト戦略

### 単体テスト
- **カバレッジ目標**: 90%以上
- **フレームワーク**: xUnit, FluentAssertions
- **モック**: Moq使用

### 統合テスト
- **実機テスト**: PLCシミュレータ使用
- **通信テスト**: 各プロトコル組み合わせ
- **負荷テスト**: 連続通信性能確認

### 互換性テスト
- **Python版との比較**: 同一条件での結果比較
- **バイナリレベル検証**: 送信フレーム完全一致確認

### テストデータ
- Python版テストケースの完全移植
- 追加のエッジケーステスト
- 異常系テストの充実

---

## 実装上の注意点

### 1. データ型の扱い
- エンディアンの統一（リトルエンディアン）
- 符号なし整数の適切な使用
- オーバーフロー検出

### 2. 非同期処理
- ConfigureAwait(false)の適切な使用
- CancellationTokenの一貫した処理
- デッドロック回避

### 3. メモリ管理
- IDisposableの適切な実装
- 大きなバッファの適切な解放
- WeakReferenceの活用検討

### 4. エラーハンドリング
- 階層化された例外設計
- 詳細なエラー情報の提供
- ログ出力の統一

### 5. スレッドセーフティ
- 並行アクセスの保護
- lock文の適切な使用
- 非同期処理での競合回避

---

## 成果物

### ライブラリ
- **SlmpClient.dll**: メインライブラリ
- **SlmpClient.xml**: XMLドキュメント
- **README.md**: 使用方法説明

### ドキュメント
- **API仕様書**: 全メソッドの詳細仕様  
- **移植対応表**: Python版との対応関係
- **サンプルコード**: 典型的な使用例

### ツール
- **互換性チェッカー**: Python版との結果比較
- **パフォーマンステスト**: 性能測定ツール
- **デバッグツール**: 通信ログ解析

---

この設計書に基づいて段階的に実装を進めることで、PySLMPClientの全機能を抜け漏れなくC#に移植できます。