# PySLMPClient - Step1: 設定読み込み処理フロー

## 処理概要

**範囲**: インスタンス初期化時
**動作**: コンストラクタで接続情報とプロトコル設定を保持
**実装場所**: `pyslmpclient/__init__.py:22-77`

---

## 全体処理フロー

```
┌──────────────────────────────────────────────────────────────┐
│ 1. SLMPClientインスタンス生成                                 │
│    __init__.py:22-77                                         │
├──────────────────────────────────────────────────────────────┤
│ クライアント作成:                                             │
│    client = SLMPClient(addr, port, binary, ver, tcp)         │
│                                                               │
│ パラメータ:                                                   │
│    - addr: IPアドレス (例: "192.168.1.100")                  │
│    - port: ポート番号 (デフォルト: 5000)                     │
│    - binary: True=Binary形式, False=ASCII形式                │
│    - ver: 3=3Eフレーム, 4=4Eフレーム                         │
│    - tcp: True=TCP, False=UDP                                │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 2. 入力パラメータ検証                                         │
│    __init__.py:37-45                                         │
├──────────────────────────────────────────────────────────────┤
│ 検証項目:                                                     │
│    assert 0 < port, port                                     │
│    → ポート番号が正の値であることを確認                       │
│                                                               │
│    assert ver in (3, 4), ver                                 │
│    → フレームバージョンが3または4であることを確認              │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 3. 接続情報保持                                               │
│    __init__.py:38-48                                         │
├──────────────────────────────────────────────────────────────┤
│ 【アドレス情報】                                              │
│    self.__addr = (addr, port)                                │
│    - socket.connect()の引数として使用                        │
│    - 例: ("192.168.1.100", 5000)                             │
│                                                               │
│ 【プロトコル設定】                                            │
│    self.__protocol = (binary, ver, tcp)                      │
│    - binary: True/False (Binary/ASCII)                       │
│    - ver: 3/4 (3E/4Eフレーム)                                │
│    - tcp: True/False (TCP/UDP)                               │
│                                                               │
│ 使用例:                                                       │
│    if self.__protocol[0]:  # Binary判定                      │
│        make_frame = util.make_binary_frame                   │
│    else:                                                     │
│        make_frame = util.make_ascii_frame                    │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 4. 内部状態初期化                                             │
│    __init__.py:49-62                                         │
├──────────────────────────────────────────────────────────────┤
│ 【ソケット関連】                                              │
│    self.__socket = None                                      │
│    → 実際の接続は open() で実施                              │
│                                                               │
│ 【シーケンス番号管理】                                        │
│    self.__seq = 0                                            │
│    → 4Eフレーム用、自動インクリメント                         │
│    → 255を超えると0にロールオーバー                           │
│                                                               │
│ 【受信キュー】                                                │
│    self.__recv_queue = dict()                                │
│    → key: シーケンス番号                                     │
│    → value: (network, pc, io, m_drop, term_code, data)      │
│                                                               │
│ 【スレッドロック】                                            │
│    self.__lock = threading.Lock()                            │
│    → 送受信処理の排他制御                                     │
│                                                               │
│ 【受信バッファ】                                              │
│    self.__rest = b""                                         │
│    → 不完全フレーム一時保存用                                 │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 5. 通信対象設定                                               │
│    __init__.py:58                                            │
├──────────────────────────────────────────────────────────────┤
│ self.target = util.Target()                                  │
│                                                               │
│ デフォルト値:                                                 │
│    - network: 0x00 (ネットワーク番号)                        │
│    - node: 0xFF (PC番号、全局指定)                           │
│    - dst_proc: 0x03FF (I/O番号)                              │
│    - m_drop: 0x00 (マルチドロップ局番)                       │
│                                                               │
│ ユーザーが後から変更可能:                                     │
│    client.target.network = 1                                 │
│    client.target.node = 0x10                                 │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 6. ロガー設定                                                 │
│    __init__.py:63-68                                         │
├──────────────────────────────────────────────────────────────┤
│ self.logger = logging.getLogger(__name__)                    │
│                   .getChild(self.__class__.__name__)         │
│                                                               │
│ 用途:                                                         │
│    - デバッグログ出力                                         │
│    - エラー記録                                               │
│    - 通信トレース                                             │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 7. 受信スレッド準備                                           │
│    __init__.py:70-77                                         │
├──────────────────────────────────────────────────────────────┤
│ self.__recv_thread = threading.Thread(                       │
│     target=self.__worker,                                    │
│     daemon=True                                              │
│ )                                                             │
│                                                               │
│ ワーカー処理 (__worker):                                      │
│    while self.__socket:                                      │
│        try:                                                  │
│            self.__recv()  # 受信処理ループ                   │
│        except RuntimeError as e:                             │
│            self.logger.error(e)                              │
│                                                               │
│ ※スレッド起動は open() で実施                                │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 8. モニタデバイス数初期化                                     │
│    __init__.py:74-77                                         │
├──────────────────────────────────────────────────────────────┤
│ self.__monitor_device_num = (0, 0)                           │
│                                                               │
│ 構造: (ワードデバイス数, Dwordデバイス数)                      │
│                                                               │
│ 使用タイミング:                                               │
│    - entry_monitor_device() で設定                           │
│    - execute_monitor() で参照                                │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│ 9. 初期化完了                                                 │
├──────────────────────────────────────────────────────────────┤
│ インスタンス生成完了                                          │
│ ※実際の接続は open() で実施                                  │
└──────────────────────────────────────────────────────────────┘
```

---

## 接続処理（open()メソッド）

初期化完了後、通信開始時に呼び出される処理:

```
┌──────────────────────────────────────────────────────────────┐
│ open() メソッド                                               │
│ __init__.py:86-107                                           │
├──────────────────────────────────────────────────────────────┤
│ 1. コンテキストカウンタ +1                                    │
│    with self.__lock:                                         │
│        self.__ctx_cnt += 1                                   │
│                                                               │
│ 2. ソケット作成                                               │
│    if self.__protocol[2]:  # TCP                             │
│        socket(AF_INET, SOCK_STREAM)                          │
│    else:  # UDP                                              │
│        socket(AF_INET, SOCK_DGRAM)                           │
│                                                               │
│ 3. PLC接続                                                    │
│    self.__socket.connect(self.__addr)                        │
│                                                               │
│ 4. タイムアウト設定                                           │
│    self.__socket.settimeout(1)  # 1秒                        │
│                                                               │
│ 5. 受信スレッド起動                                           │
│    self.__recv_thread.start()                                │
└──────────────────────────────────────────────────────────────┘
```

---

## 設定パラメータ詳細

### 接続情報

| パラメータ | 型 | デフォルト | 説明 |
|-----------|---|----------|------|
| addr | str | - | PLCのIPアドレス |
| port | int | 5000 | 接続先ポート番号 |

**検証**:
- `port > 0` (assert)

**例**:
```python
client = SLMPClient("192.168.1.100", port=8192)
```

### プロトコル設定

| パラメータ | 型 | デフォルト | 説明 |
|-----------|---|----------|------|
| binary | bool | True | True=Binary形式, False=ASCII形式 |
| ver | int | 4 | 3=3Eフレーム, 4=4Eフレーム |
| tcp | bool | False | True=TCP接続, False=UDP接続 |

**検証**:
- `ver in (3, 4)` (assert)

**例**:
```python
# 3E Binary + UDP
client = SLMPClient("192.168.1.100", binary=True, ver=3, tcp=False)

# 4E ASCII + TCP
client = SLMPClient("192.168.1.100", binary=False, ver=4, tcp=True)
```

### 通信対象設定（util.Target）

| プロパティ | 型 | デフォルト | 範囲 | 説明 |
|----------|---|----------|-----|------|
| network | int | 0x00 | 0-0xFF | ネットワーク番号 |
| node | int | 0xFF | 0-0xFF | 要求先局番（0xFF=全局） |
| dst_proc | int | 0x03FF | 0-0xFFFF | 要求先I/O番号 |
| m_drop | int | 0x00 | 0-0xFF | マルチドロップ局番 |

**検証**:
- setter内で範囲チェック（ValueError）

**例**:
```python
client.target.network = 0x01
client.target.node = 0x10
client.target.dst_proc = 0x03FF
client.target.m_drop = 0x00
```

---

## andon設計との対応

### Step1の成功条件（andon設計との対応）

| andon設計要件 | PySLMPClient実装 | 対応状況 |
|--------------|-----------------|---------|
| 設定ファイルから値を正確に取得 | コンストラクタ引数で直接受け取り | ✅ 実装（方式違い） |
| 必要な値が設定されていない場合エラー | assert文で検証 | ✅ 実装 |
| 接続情報の保持 | `__addr`, `__protocol` に保存 | ✅ 実装 |
| プロトコル設定の保持 | `__protocol` タプル | ✅ 実装 |
| 通信対象設定 | `target` オブジェクト | ✅ 実装 |

### andonプロジェクトとの比較

| 項目 | PySLMPClient | andon (C#) |
|-----|-------------|-----------|
| **設定ソース** | コンストラクタ引数 | appsettings.json |
| **設定読み込みタイミング** | インスタンス生成時 | 起動時 |
| **検証方法** | assert文 | Validatorパターン |
| **接続情報保持** | タプル `(addr, port)` | `ConnectionConfig` クラス |
| **プロトコル設定** | タプル `(binary, ver, tcp)` | 各種Config クラス |
| **通信対象** | `Target` オブジェクト | フレーム構築時に指定 |
| **エラーハンドリング** | assert + 例外 | 詳細なエラー分類 |

---

## PySLMPClient独自の特徴

### 1. スレッドセーフ設計

```python
self.__lock = threading.Lock()

# 使用例
with self.__lock:
    self.__seq += 1
    # クリティカルセクション
```

- 送受信処理の排他制御
- シーケンス番号の安全な更新
- 受信キューの同期アクセス

### 2. シーケンス番号自動管理

```python
self.__seq = 0  # 初期化

# __cmd_format() 内で自動インクリメント
with self.__lock:
    if self.__seq > 0xFF:
        self.__seq = 0  # ロールオーバー
```

- 4Eフレーム用
- 0-255の範囲で自動循環
- 要求-応答のマッチングに使用

### 3. 非同期受信処理

```python
self.__recv_thread = threading.Thread(
    target=self.__worker,
    daemon=True
)

# ワーカー処理
def __worker(self):
    while self.__socket:
        try:
            self.__recv()  # 受信ループ
        except RuntimeError as e:
            self.logger.error(e)
```

- 別スレッドで常時受信待機
- `__recv_queue` にシーケンス番号ごとに格納
- メインスレッドは `__recv_loop()` で応答待機

### 4. コンテキストマネージャ対応

```python
# with文で自動接続・切断
with SLMPClient("192.168.1.100") as client:
    data = client.read_random_devices([...], [])
# 自動的にclose()が呼ばれる
```

- `__enter__()` → `open()`
- `__exit__()` → `close()`

---

## andon実装への示唆

### ✅ 採用すべき点

1. **シーケンス番号自動管理**
   - 4Eフレームでの複数要求-応答マッチングに必須
   - ロールオーバー処理も実装済み
   - andon Phase2実装時の参考に

2. **スレッドセーフ設計**
   - `Lock`によるクリティカルセクション保護
   - 並行処理時の安全性確保

3. **通信対象の柔軟な設定**
   - `Target`オブジェクトで後から変更可能
   - ネットワーク番号、局番等の動的変更対応

4. **コンテキストマネージャ**
   - リソース管理の自動化
   - 切断漏れ防止

### ⚠️ 改善が必要な点（andonでは対策済み）

1. **入力検証が弱い**
   - assert文のみ（本番環境で無効化される可能性）
   - andonは厳格なバリデーション実装

2. **設定ファイル非対応**
   - コンストラクタ引数のみ
   - andonは設定ファイル+動的再読み込み対応

3. **エラー処理の簡素化**
   - カスタム例外が少ない
   - andonは多層的エラーハンドリング設計

4. **接続統計情報が不足**
   - 送受信バイト数、エラー回数等の統計なし
   - andonは `ConnectionStats` で詳細管理

---

## andon実装への適用推奨

### 1. シーケンス番号管理の実装

**推奨実装** (C#):

```csharp
public class SequenceNumberManager
{
    private ushort _sequence = 0;
    private readonly object _lock = new object();

    public ushort GetNext()
    {
        lock (_lock)
        {
            if (_sequence > 0xFF)
            {
                _sequence = 0;  // ロールオーバー
            }
            return _sequence++;
        }
    }
}
```

### 2. 通信対象設定クラスの実装

**推奨実装** (C#):

```csharp
public class SlmpTarget
{
    private byte _network = 0x00;
    private byte _node = 0xFF;
    private ushort _dstProc = 0x03FF;
    private byte _mDrop = 0x00;

    public byte Network
    {
        get => _network;
        set
        {
            if (value > 0xFF)
                throw new ArgumentOutOfRangeException(nameof(value));
            _network = value;
        }
    }

    public byte Node
    {
        get => _node;
        set
        {
            if (value > 0xFF)
                throw new ArgumentOutOfRangeException(nameof(value));
            _node = value;
        }
    }

    // dst_proc, m_drop も同様
}
```

### 3. 非同期受信処理の実装

**推奨実装** (C#):

```csharp
private readonly ConcurrentDictionary<ushort, byte[]> _recvQueue = new();

private async Task ReceiverWorkerAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            var response = await ReceiveAsync(cancellationToken);
            ushort seq = ExtractSequenceNumber(response);
            _recvQueue.TryAdd(seq, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "受信エラー");
        }
    }
}
```

---

## 実装優先度の提案

### andon Phase1実装時の参考箇所

| 実装内容 | PySLMPClient参考箇所 | andon対応クラス/メソッド |
|---------|---------------------|-------------------------|
| 設定保持 | `__init__()` (L22-77) | `ConfigurationLoader.LoadPlcConnectionConfig()` |
| 接続処理 | `open()` (L86-107) | `PlcCommunicationManager.ConnectAsync()` |
| 通信対象設定 | `util.Target` (L132-203) | フレーム構築時に指定 |
| シーケンス番号管理 | `__cmd_format()` (L126-163) | Phase2実装時 |

---

## まとめ

### PySLMPClient Step1の特徴

**✅ 実装されている機能**:
- コンストラクタで設定を直接受け取る
- プロトコル設定（Binary/ASCII、3E/4E、TCP/UDP）を柔軟に指定
- スレッドセーフな実装（ロック機構）
- シーケンス番号自動管理（4Eフレーム用）
- 非同期受信処理（別スレッド）
- コンテキストマネージャ対応

**⚠️ 改善の余地がある点**:
- 設定ファイル非対応
- 入力検証が弱い（assert文のみ）
- 接続統計情報が不足

### andon実装への活用

PySLMPClientのStep1処理は**技術的に洗練されており**、特に以下の点を参考にすべきです：

1. **シーケンス番号自動管理** - Phase2実装時の重要参考資料
2. **スレッドセーフ設計** - 並行処理時の安全性確保
3. **通信対象の柔軟な設定** - ネットワーク番号等の動的変更対応

ただし、andon独自の強み（厳格な入力検証、設定ファイル対応、詳細な統計管理）は維持すべきです。
