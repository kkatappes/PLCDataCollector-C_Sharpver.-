# Step3 SendFrameAsync テスト実装用情報（TC021）

## ドキュメント概要

### 目的
このドキュメントは、TC021_SendFrameAsync_正常送信テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### SendFrameAsync（Step4: PLCリクエスト送信）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- SLMPフレーム（string型、16進数文字列形式）
  - M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"
  - D000-D999読み込みフレーム: "54001234000000010400A800000090E8030000"

#### Output
- Task (送信完了状態)
- 成功時: Taskが正常完了
- 失敗時: 例外スロー
  - TimeoutException: 送信タイムアウト時
  - SocketException: ソケットエラー時
  - InvalidOperationException: 未接続状態での送信試行時

#### 機能
- READコマンド(0104)送信
- タイムアウト制御（Socket.SendTimeout使用）
- ソケットレベルでのタイムアウト適用（ConnectAsync内で設定済み）

#### データ取得元
- ConfigToFrameManager.BuildFrames()（送信フレーム）
- PlcCommunicationManager.ConnectAsync()（接続状態・Socketインスタンス）

---

## 2. テストケース仕様（TC021）

### TC021_SendFrameAsync_正常送信
**目的**: PLCへのリクエスト送信機能をテスト

#### 前提条件
- ConnectAsyncが成功済み
- 接続状態: Connected
- Socketインスタンスが有効

#### 入力データ
**M000-M999読み込みフレーム**:
- フレーム文字列: "54001234000000010401006400000090E8030000"
- フレーム構成:
  - サブヘッダ: 54001234000000 (4Eフレーム識別)
  - READコマンド: 0401 (デバイス一括読み出し - 注:旧表記、実際は0104)
  - サブコマンド: 0100 (ビット単位読み出し)
  - デバイスコード: 6400 (M機器)
  - 開始番号: 00000090 (M000、リトルエンディアン)
  - デバイス点数: E8030000 (1000点、リトルエンディアン)

**D000-D999読み込みフレーム**:
- フレーム文字列: "54001234000000010400A800000090E8030000"
- フレーム構成:
  - サブヘッダ: 54001234000000 (4Eフレーム識別)
  - READコマンド: 0401 (デバイス一括読み出し)
  - サブコマンド: 0000 (ワード単位読み出し)
  - デバイスコード: A800 (D機器)
  - 開始番号: 00000090 (D000、リトルエンディアン)
  - デバイス点数: E8030000 (1000点、リトルエンディアン)

#### 期待出力
- Taskが正常完了（例外なし）
- 送信バイト数が期待値と一致（ASCII形式の場合、文字列長と一致）
- 送信完了ステータス確認

---

## 3. SLMPフレーム詳細

### 4Eフレーム/ASCIIフォーマット
**実機テスト設定**: Q00UDPCPUとの通信で使用

#### ASCII変換規則
- 各バイトを2文字の16進数ASCII文字列に変換
- 例: `54H` → `"35"(5) + "34"(4)` → `"3534"`
- データ量: バイナリの2倍（通信時間・帯域幅に注意）

#### フレーム構成（M000-M999読み込み例）
```
バイナリ形式（参考）:
54 00 12 34 00 00 00 00  01 04 01 00  64 00 00 00 90  E8 03 00

ASCII形式（実送信）:
"54001234000000010401006400000090E8030000"

各フィールド:
- サブヘッダ: 54001234000000
- READコマンド: 0401 (実際は0104が正しい)
- サブコマンド: 0100 (ビット単位)
- デバイスコード: 6400 (M機器)
- 開始アドレス: 00000090 (M000)
- デバイス点数: E8030000 (1000点)
```

---

## 4. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.3.250"
    public int Port { get; set; }                 // 例: 5007
    public bool UseTcp { get; set; }              // false (UDP使用)
    public string ConnectionType { get; set; }    // "UDP"
    public bool IsBinary { get; set; }            // false (ASCII形式)
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000
    public int SendTimeoutMs { get; set; }        // 例: 3000 ← SendFrameAsyncで使用
    public int ReceiveTimeoutMs { get; set; }     // 例: 5000
    public int RetryTimeoutMs { get; set; }       // 例: 1000
}
```

### ConnectionResponse（接続結果）
**取得元**: PlcCommunicationManager.ConnectAsync()

```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }  // Connected
    public Socket? Socket { get; set; }           // 実際の通信用ソケット
    public EndPoint? RemoteEndPoint { get; set; } // 接続先情報
    public DateTime? ConnectedAt { get; set; }    // 接続完了時刻
    public TimeSpan? ConnectionTime { get; set; } // 接続処理時間
    public string? ErrorMessage { get; set; }     // null (成功時)
}
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerTests.cs
- **配置先**: Tests/Unit/Core/Managers/
- **名前空間**: andon.Tests.Unit.Core.Managers

### テスト実装順序
1. **TC021_SendFrameAsync_正常送信**（最優先）
   - M000-M999フレーム送信テスト
   - D000-D999フレーム送信テスト
2. 異常系テスト（次フェーズ）
   - 未接続状態での送信試行
   - タイムアウト発生テスト
   - 不正フレーム送信テスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: PlcCommunicationManager全体のモック
- **MockConfigToFrameManager**: 設定読み込み・フレーム構築のモック

#### 使用するスタブ
- **PlcResponseStubs**: PLC応答データのスタブ
- **ConfigurationStubs**: 設定データのスタブ
- **NetworkStubs**: ネットワーク動作のスタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. MockSocketの準備
   - 送信成功をシミュレート
   - 送信バイト数を記録
2. ConnectionResponseの準備
   - Status = Connected
   - Socket = MockSocket
   - ConnectedAt, ConnectionTime設定
3. ConnectionConfig・TimeoutConfigの準備
   - SendTimeoutMs = 3000
4. PlcCommunicationManagerインスタンス作成
   - モックSocketを注入

### Act（実行）
1. ConnectAsync実行（前提条件確立）
2. SendFrameAsync実行
   - M000-M999フレーム送信
   - D000-D999フレーム送信

### Assert（検証）
1. 例外が発生しないことを確認
2. 送信バイト数が期待値と一致
3. Socketの送信メソッドが呼ばれたことを確認
4. 送信データがフレーム文字列と一致

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（PLC別インスタンス）
- **ConfigToFrameManager**: Transient（設定別インスタンス）
- **LoggingManager**: Singleton（共有リソース）
- **ErrorHandler**: Singleton（共有リソース）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();
services.AddSingleton<ILoggingManager, LoggingManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
```

---

## 8. エラーハンドリング

### SendFrameAsync スロー例外
- **TimeoutException**: 送信タイムアウト（SendTimeoutMs超過）
- **SocketException**: ソケットエラー（接続切断等）
- **InvalidOperationException**: 未接続状態での送信試行
- **ArgumentException**: 不正なフレーム形式

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";
    public const string SendTimeout = "フレーム送信がタイムアウトしました。";
    public const string InvalidFrame = "不正なSLMPフレーム形式です。";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- 送信開始ログ: フレーム内容、送信先情報
- 送信完了ログ: 送信バイト数、所要時間
- エラーログ: 例外詳細、スタックトレース

### ログレベル
- **Information**: 送信開始・完了
- **Warning**: リトライ発生時
- **Error**: 例外発生時

---

## 10. テスト実装チェックリスト

### TC021実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] SendFrameAsyncメソッドシグネチャ定義
- [ ] ConnectionResponse・ConnectionConfigモデル作成
- [ ] MockSocket・MockPlcCommunicationManager作成

### TC021実装中
- [ ] Arrange: モック・スタブ準備
- [ ] Act: SendFrameAsync呼び出し
- [ ] Assert: 送信成功検証
- [ ] M000-M999フレーム送信テスト実装
- [ ] D000-D999フレーム送信テスト実装

### TC021実装後
- [ ] テスト実行・Red確認
- [ ] SendFrameAsync本体実装（最小実装）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施

---

## 11. 参考情報

### SLMP仕様書
- デバイスコード表: SLMP仕様書pdf2img/page_36.png
- フレーム構造: 4Eフレーム/ASCIIフォーマット準拠
- READコマンド: 0x0104（デバイス一括読み出し）

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/SlmpFrameSamples/

- M000-M999_ReadFrame.txt: "54001234000000010401006400000090E8030000"
- D000-D999_ReadFrame.txt: "54001234000000010400A800000090E8030000"

---

## 12. PySLMPClient実装参考情報

### デバイスコード定義（const.py）
```python
class DeviceCode(enum.Enum):
    M = 0x90   # 144 (decimal) - Mデバイス
    D = 0xA8   # 168 (decimal) - Dデバイス
    X = 0x9C   # Xデバイス
    Y = 0x9D   # Yデバイス
```

**C#実装時の参考**:
```csharp
public static class DeviceCode
{
    public const byte M = 0x90;  // Mデバイス
    public const byte D = 0xA8;  // Dデバイス
}
```

### SLMPコマンド定義（const.py）
```python
class SLMPCommand(enum.Enum):
    Device_Read = 0x0401   # デバイス一括読み出し
```

### ASCII形式フレーム作成（util.py make_ascii_frame関数）

#### Python実装
```python
def make_ascii_frame(seq, target, timeout, cmd, sub_cmd, data, ver):
    cmd_text = b"%02X%02X%04X%02X%04X%04X%04X%04X" % (
        target.network,    # ネットワーク番号
        target.node,       # 要求先局番
        target.dst_proc,   # 要求先プロセッサ番号
        target.m_drop,     # マルチドロップ局番
        len(data) + 12,    # データ長
        timeout,           # 監視タイマ
        cmd.value,         # コマンド
        sub_cmd,           # サブコマンド
    )
    if ver == 4:
        buf = b"5400%04X0000" % seq + cmd_text + data
    return buf
```

#### C#実装例
```csharp
public string MakeAsciiFrame(byte seq, ushort cmd, ushort subCmd, string data)
{
    string cmdText = string.Format(
        "{0:X2}{1:X2}{2:X4}{3:X2}{4:X4}{5:X4}{6:X4}{7:X4}",
        0x00,            // ネットワーク番号
        0xFF,            // 要求先局番
        0x03FF,          // 要求先プロセッサ番号
        0x00,            // マルチドロップ局番
        data.Length + 12, // データ長
        timeout,
        cmd,
        subCmd
    );
    return $"5400{seq:X4}0000{cmdText}{data}";
}
```

### Target（通信対象）クラス
```python
class Target:
    def __init__(self, network_num=0, node_num=0xFF, dst_proc_num=0x03FF, m_drop_num=0):
        self.__network = network_num      # 0-255
        self.__node = node_num            # 0-255
        self.__dst_proc = dst_proc_num    # 0-65535
        self.__m_drop = m_drop_num        # 0-255
```

### PySLMPClientテストケース実例
**test_read_bit_devices（test_main.py）**:
- M100から8ビット読み出し
- ASCII形式送信データ: `b"04010001M*0001000008"`
  - 0401: READコマンド
  - 0001: サブコマンド（ビット単位）
  - M*000100: M100デバイス表記
  - 0008: 8点

### フレーム構造詳細

#### 4E ASCIIフレーム構造
```
"5400" + "%04X" % seq + "0000" + cmd_text + data
 ^^^^     ^^^^          ^^^^     ^^^^^^^^   ^^^^
 サブ     シーケンス    予約     コマンド   データ
 ヘッダ   番号                   テキスト
```

#### cmd_text構造
```
"%02X%02X%04X%02X%04X%04X%04X%04X"
  ^^   ^^   ^^^^   ^^   ^^^^   ^^^^   ^^^^   ^^^^
  Net  Node DstPrc Drop DataLen Timer  Cmd    SubCmd
  1B   1B   2B     1B   2B      2B     2B     2B
```

### 実装時の重要ポイント

1. **デバイスコード**: M=0x90, D=0xA8（16進数）
2. **エンディアン**: ASCII形式では文字列表現、バイナリ形式ではリトルエンディアン
3. **サブコマンド**:
   - 0x0100: ビット単位読み出し（M機器）
   - 0x0000: ワード単位読み出し（D機器）
4. **フレーム長**: ASCIIではデータ部バイト長+12
5. **デバイス点数**: リトルエンディアン表現（1000点=0x03E8→"E80300"）

---

以上が TC021_SendFrameAsync_正常送信テスト実装に必要な情報です。
