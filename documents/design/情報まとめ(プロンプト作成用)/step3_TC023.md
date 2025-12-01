# Step3 SendFrameAsync テスト実装用情報（TC023）

## ドキュメント概要

### 目的
このドキュメントは、TC023_SendFrameAsync_未接続状態テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `エラーハンドリング.md` - 例外処理・エラーメッセージ設計

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
  - 任意の有効なSLMPフレーム

#### Output
- Task (送信完了状態)
- 成功時: Taskが正常完了
- 失敗時: 例外スロー
  - **InvalidOperationException**: 未接続状態での送信試行時（TC023で検証）
  - TimeoutException: 送信タイムアウト時
  - SocketException: ソケットエラー時
  - ArgumentException: 不正なフレーム形式

#### 機能
- READコマンド(0104)送信
- タイムアウト制御（Socket.SendTimeout使用）
- ソケットレベルでのタイムアウト適用（ConnectAsync内で設定済み）
- **接続状態検証**: 未接続状態での送信試行を検出し、例外をスロー

#### データ取得元
- ConfigToFrameManager.BuildFrames()（送信フレーム）
- PlcCommunicationManager.ConnectAsync()（接続状態・Socketインスタンス）

---

## 2. テストケース仕様（TC023）

### TC023_SendFrameAsync_未接続状態
**目的**: 未接続状態でのSendFrameAsync呼び出しが適切に検出され、例外がスローされることをテスト

#### 前提条件
- **ConnectAsyncが未実行**（最重要）
- 接続状態: Disconnected または NotConnected
- Socketインスタンスがnull または 未接続状態

#### 入力データ
**任意の有効なSLMPフレーム（例）**:
- M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"
- D000-D999読み込みフレーム: "54001234000000010400A800000090E8030000"
- その他任意の形式上正しいSLMPフレーム

**重要**: フレーム形式自体は正しいが、接続状態が未接続であることがテストポイント

#### 期待出力（異常系）
- **InvalidOperationException がスローされる**
- 例外メッセージ: "PLC未接続状態です。先にConnectAsync()を実行してください。"
  - メッセージ取得元: Core/Constants/ErrorMessages.cs
- Task は正常完了しない（例外発生のため）
- Socket送信処理は実行されない

#### テスト検証項目
1. **例外タイプ**: InvalidOperationException であること
2. **例外メッセージ**: ErrorMessages.NotConnected 定数と一致すること
3. **接続状態確認**: Socket.Connected が false であることを確認
4. **送信未実行**: Socket.Send() または Socket.SendAsync() が呼ばれていないこと
5. **リソース状態**: 例外発生後もリソースリークが発生していないこと

---

## 3. 接続状態管理詳細

### ConnectionStatus（接続状態列挙型）
**定義**: Core/Models/ConnectionStatus.cs

```csharp
public enum ConnectionStatus
{
    NotConnected,    // 未接続状態（初期状態）
    Connecting,      // 接続中状態
    Connected,       // 接続済み状態（SendFrameAsync実行可能）
    Disconnecting,   // 切断中状態
    Disconnected,    // 切断済み状態（再接続必要）
    Failed           // 接続失敗状態
}
```

### SendFrameAsync実行可否判定

#### 実行可能状態
- **ConnectionStatus.Connected**: Socketが接続済み、送信処理実行可能

#### 実行不可能状態（TC023で検証）
- **ConnectionStatus.NotConnected**: 初期状態、ConnectAsync未実行
- **ConnectionStatus.Disconnected**: 切断済み、再接続必要
- **ConnectionStatus.Failed**: 接続失敗、エラー状態
- **ConnectionStatus.Connecting**: 接続処理中、まだ送信不可
- **ConnectionStatus.Disconnecting**: 切断処理中、もはや送信不可

### 接続状態チェックロジック

#### PlcCommunicationManager内部実装（想定）
```csharp
public async Task SendFrameAsync(string frame)
{
    // TC023で検証するロジック
    if (_connectionStatus != ConnectionStatus.Connected || _socket == null || !_socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnected);
    }

    // 実際の送信処理（接続済みの場合のみ実行）
    await SendFrameAsyncInternal(frame);
}
```

---

## 4. 依存クラス・設定

### ConnectionResponse（接続結果）
**取得元**: PlcCommunicationManager.ConnectAsync()

```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }  // TC023では NotConnected または Disconnected
    public Socket? Socket { get; set; }           // TC023では null
    public EndPoint? RemoteEndPoint { get; set; } // TC023では null
    public DateTime? ConnectedAt { get; set; }    // TC023では null
    public TimeSpan? ConnectionTime { get; set; } // TC023では null
    public string? ErrorMessage { get; set; }     // TC023では null（接続試行自体していない）
}
```

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

**重要**: TC023では設定は正常だが、ConnectAsync未実行が問題

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000
    public int SendTimeoutMs { get; set; }        // 例: 3000
    public int ReceiveTimeoutMs { get; set; }     // 例: 5000
    public int RetryTimeoutMs { get; set; }       // 例: 1000
}
```

**重要**: TC023ではタイムアウト設定は関係ない（送信処理自体が実行されないため）

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerTests.cs
- **配置先**: Tests/Unit/Core/Managers/
- **名前空間**: andon.Tests.Unit.Core.Managers

### テスト実装順序（TC023の位置づけ）
1. TC021_SendFrameAsync_正常送信（最優先）- 既に実装済み想定
2. **TC023_SendFrameAsync_未接続状態**（次優先・異常系基本）
3. TC024-TC030: その他異常系テスト

### モック・スタブ使用

#### TC023専用モック設計
- **MockSocket（未接続状態）**:
  - Socket.Connected = false
  - Socket インスタンス自体が null の場合も想定
- **MockConnectionResponse（未接続）**:
  - Status = ConnectionStatus.NotConnected
  - Socket = null
  - その他プロパティも null
- **MockPlcCommunicationManager**:
  - _connectionStatus = ConnectionStatus.NotConnected に設定
  - ConnectAsync() 未実行状態を維持

#### TC023でのモック・スタブ使用目的
- **接続未実施状態の再現**: ConnectAsync()を意図的に呼ばないシナリオ
- **例外発生確認**: InvalidOperationExceptionの正確な発生確認
- **リソース状態確認**: 例外発生後のリソースリーク無し確認

---

## 6. テストケース実装構造（TC023）

### Arrange（準備）
1. MockPlcCommunicationManagerの準備
   - 接続状態: NotConnected
   - Socket: null
   - ConnectAsync() **未実行**（最重要）
2. 有効なSLMPフレームの準備
   - M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"
   - または D000-D999読み込みフレーム: "54001234000000010400A800000090E8030000"
3. PlcCommunicationManagerインスタンス作成
   - 未接続状態を維持

### Act（実行）
1. **ConnectAsync実行をスキップ**（TC023の核心）
2. SendFrameAsync実行（例外発生を期待）
   - 入力: 有効なSLMPフレーム
   - 期待: InvalidOperationException スロー

### Assert（検証）
1. **InvalidOperationException が発生すること**
   ```csharp
   Assert.ThrowsAsync<InvalidOperationException>(async () =>
       await manager.SendFrameAsync(validFrame));
   ```
2. **例外メッセージが正しいこと**
   ```csharp
   var exception = Assert.ThrowsAsync<InvalidOperationException>(...);
   Assert.Equal(ErrorMessages.NotConnected, exception.Message);
   ```
3. **Socket送信メソッドが呼ばれていないこと**
   ```csharp
   mockSocket.Verify(s => s.Send(It.IsAny<byte[]>()), Times.Never);
   mockSocket.Verify(s => s.SendAsync(It.IsAny<byte[]>(), It.IsAny<SocketFlags>()), Times.Never);
   ```
4. **接続状態が未接続のままであること**
   ```csharp
   Assert.Equal(ConnectionStatus.NotConnected, manager.ConnectionStatus);
   ```

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

**重要**: TC023ではDI設定は直接関係しないが、統合テスト時に重要

---

## 8. エラーハンドリング

### SendFrameAsync スロー例外（TC023関連）

#### InvalidOperationException（TC023で検証）
- **発生条件**:
  - ConnectAsync未実行
  - ConnectionStatus != Connected
  - Socket == null または Socket.Connected == false
- **エラーメッセージ**: ErrorMessages.NotConnected
  - 内容: "PLC未接続状態です。先にConnectAsync()を実行してください。"
- **推奨対応**:
  - ConnectAsync()を先に実行
  - 接続状態を確認してからSendFrameAsync()を呼び出す

#### その他の例外（TC023では検証しない）
- **TimeoutException**: 送信タイムアウト（SendTimeoutMs超過）
- **SocketException**: ソケットエラー（接続切断等）
- **ArgumentException**: 不正なフレーム形式

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // TC023で使用
    public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";

    // その他関連メッセージ
    public const string SendTimeout = "フレーム送信がタイムアウトしました。";
    public const string InvalidFrame = "不正なSLMPフレーム形式です。";
    public const string SocketError = "ソケット通信エラーが発生しました。";
}
```

### エラー分類（ErrorHandler連携）
**TC023で発生する例外の分類**:
- **ErrorCategory**: ConfigurationError または OperationError
- **Severity**: Error（致命的ではないが処理続行不可）
- **ShouldRetry**: false（接続していないため、リトライ不要、ConnectAsync実行が必要）
- **ErrorAction**: Abort（処理中断、接続処理へ戻る）

---

## 9. ログ出力要件

### LoggingManager連携（TC023固有）

#### エラーログ
- **ログレベル**: Error
- **ログ内容**:
  - 未接続状態でのSendFrameAsync呼び出し試行
  - 例外詳細（InvalidOperationException）
  - スタックトレース
  - 現在の接続状態（NotConnected）
- **ログタイミング**: 例外発生直後

#### デバッグログ（詳細ログ有効時）
- **ログレベル**: Debug
- **ログ内容**:
  - SendFrameAsync呼び出し試行
  - 接続状態チェック結果
  - Socket状態確認結果

### ログ出力例

#### エラーログ出力例
```
[ERROR] [2025-11-06 10:30:45] SendFrameAsync failed: Not connected
  Exception: InvalidOperationException
  Message: PLC未接続状態です。先にConnectAsync()を実行してください。
  ConnectionStatus: NotConnected
  Socket: null
  StackTrace: ...
```

#### デバッグログ出力例
```
[DEBUG] [2025-11-06 10:30:45] SendFrameAsync called
  Frame: 54001234000000010401006400000090E8030000
  ConnectionStatus: NotConnected
  Socket.Connected: false (or null)
  Result: Exception thrown - InvalidOperationException
```

---

## 10. テスト実装チェックリスト

### TC023実装前
- [ ] PlcCommunicationManagerクラス作成（SendFrameAsync実装済み）
- [ ] IPlcCommunicationManagerインターフェース作成
- [ ] ConnectionStatus列挙型実装
- [ ] ErrorMessages.NotConnected定数定義
- [ ] MockPlcCommunicationManager作成（未接続状態対応）

### TC023実装中
- [ ] Arrange: 未接続状態のモック準備
- [ ] Arrange: 有効なSLMPフレーム準備
- [ ] Act: ConnectAsync未実行のままSendFrameAsync呼び出し
- [ ] Assert: InvalidOperationException発生確認
- [ ] Assert: 例外メッセージ確認（ErrorMessages.NotConnected）
- [ ] Assert: Socket送信メソッド未呼び出し確認
- [ ] Assert: 接続状態維持確認

### TC023実装後
- [ ] テスト実行・Red確認（最初は失敗するはず）
- [ ] SendFrameAsync内に接続状態チェック実装
- [ ] テスト実行・Green確認（チェック実装後は成功）
- [ ] リファクタリング実施
- [ ] 他の異常系テスト（TC024以降）への準備

---

## 11. 参考情報

### 実装設計書
- エラーハンドリング設計: documents/design/エラーハンドリング.md
- クラス設計: documents/design/クラス設計.md
- テスト内容: documents/design/テスト内容.md

### SLMP仕様書
- デバイスコード表: SLMP仕様書pdf2img/page_36.png
- フレーム構造: 4Eフレーム/ASCIIフォーマット準拠
- READコマンド: 0x0104（デバイス一括読み出し）

**重要**: TC023では実機環境は不要（未接続状態のテストのため）

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/SlmpFrameSamples/

- M000-M999_ReadFrame.txt: "54001234000000010401006400000090E8030000"
- D000-D999_ReadFrame.txt: "54001234000000010400A800000090E8030000"

**重要**: TC023ではフレーム形式は正しいが、接続状態が問題

---

## 12. PySLMPClient実装参考情報

### 未接続状態チェック実装（Python実装例）

#### PySLMPClientでの接続状態管理
```python
class SLMPClient:
    def __init__(self):
        self.__socket = None
        self.__is_connected = False

    def send_frame(self, frame):
        # TC023相当のチェック
        if not self.__is_connected or self.__socket is None:
            raise RuntimeError("Not connected to PLC. Call connect() first.")

        # 実際の送信処理
        self.__socket.send(frame)
```

#### C#実装例（TC023対応）
```csharp
public class PlcCommunicationManager
{
    private Socket? _socket;
    private ConnectionStatus _connectionStatus = ConnectionStatus.NotConnected;

    public async Task SendFrameAsync(string frame)
    {
        // TC023で検証するロジック
        if (_connectionStatus != ConnectionStatus.Connected || _socket == null || !_socket.Connected)
        {
            throw new InvalidOperationException(ErrorMessages.NotConnected);
        }

        // 実際の送信処理
        byte[] frameBytes = ConvertHexStringToBytes(frame);
        await _socket.SendAsync(frameBytes, SocketFlags.None);
    }
}
```

### 実装時の重要ポイント（TC023関連）

1. **接続状態の厳密なチェック**:
   - ConnectionStatus列挙型による状態管理
   - Socket.Connected プロパティの確認
   - Socketインスタンスのnullチェック

2. **例外の適切な選択**:
   - InvalidOperationException（操作状態が不正）
   - RuntimeException（Python）との対応

3. **エラーメッセージの統一**:
   - ErrorMessages.cs定数クラスによる統一管理
   - 国際化対応（将来的に英語版も対応可能）

4. **ログ出力**:
   - 未接続状態での呼び出し試行をログ記録
   - デバッグ・トラブルシューティング支援

---

## 13. TC023と他のテストケースの関係

### TC021（正常系）との関係
- **TC021**: ConnectAsync成功後のSendFrameAsync正常実行
- **TC023**: ConnectAsync未実行でのSendFrameAsync例外発生
- **対比**: 接続状態の有無による動作の違いを検証

### TC024-TC030（他の異常系）との関係
- **TC024**: 送信タイムアウト（接続済みだが送信失敗）
- **TC025**: 不正フレーム形式（接続済みだがフレーム不正）
- **TC026-TC030**: その他異常系
- **TC023の位置づけ**: 最も基本的な異常系（前提条件エラー）

### テスト実行順序
1. **TC021**: 正常系（最優先）
2. **TC023**: 未接続状態異常系（基本異常系）
3. TC024-TC030: その他異常系

---

## 14. 統合テスト・結合テストでの考慮点

### Step1-2-3-4統合テスト
- **正常フロー**: LoadConfig → BuildFrames → ConnectAsync → SendFrameAsync
- **TC023異常フロー**: LoadConfig → BuildFrames → **ConnectAsyncスキップ** → SendFrameAsync（例外）

### エラー回復フロー
1. SendFrameAsync呼び出し（例外発生）
2. InvalidOperationException キャッチ
3. ConnectAsync実行
4. 再度SendFrameAsync実行（成功）

### 複数PLC並行実行時の考慮
- 各PLCインスタンス別の接続状態管理
- 一部PLC未接続時の他PLC処理継続
- エラーログの正確な記録

---

以上が TC023_SendFrameAsync_未接続状態テスト実装に必要な情報です。
