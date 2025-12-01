# TC021_SendFrameAsync_正常送信テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC021_SendFrameAsync_正常送信テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManager.SendFrameAsync()メソッドのテストケースTC021を実装します。
このテストは、PLCへのSLMPフレーム送信機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC021_SendFrameAsync_正常送信_M000_M999フレーム`, `TC021_SendFrameAsync_正常送信_D000_D999フレーム`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (空実装可)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Models/ConnectionConfig.cs`
   - `Core/Models/TimeoutConfig.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/` 配下のモッククラス
   - `Tests/TestUtilities/Stubs/` 配下のスタブクラス

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル作成
```
ファイル: Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs
名前空間: andon.Tests.Unit.Core.Managers
```

#### Step 1-2: テストケース実装（2つ）

**TC021-1: M000-M999フレーム送信テスト**

**Arrange（準備）**:
- MockSocketを作成（送信成功をシミュレート）
- ConnectionResponseを作成
  - Status = ConnectionStatus.Connected
  - Socket = MockSocket
  - ConnectedAt, ConnectionTime設定
- ConnectionConfigを作成
  - IpAddress = "192.168.3.250"
  - Port = 5007
  - UseTcp = false
  - ConnectionType = "UDP"
  - IsBinary = false
  - FrameVersion = FrameVersion.Frame4E
- TimeoutConfigを作成
  - SendTimeoutMs = 3000
- PlcCommunicationManagerインスタンス作成（モック注入）

**Act（実行）**:
- フレーム文字列: `"54001234000000010401006400000090E8030000"`
- `await manager.SendFrameAsync(frameString);`

**Assert（検証）**:
- 例外が発生しないこと
- MockSocketの送信メソッドが1回呼ばれたこと
- 送信データがフレーム文字列のバイト配列と一致すること
- 送信バイト数が期待値（40バイト）と一致すること

**TC021-2: D000-D999フレーム送信テスト**

上記と同様の構成で、フレーム文字列のみ変更:
- フレーム文字列: `"54001234000000010400A800000090E8030000"`

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC021"
```

期待結果: テスト失敗（SendFrameAsyncが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: SendFrameAsync最小実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
```csharp
public async Task SendFrameAsync(string frameHexString)
{
    // 1. 未接続チェック
    if (_connectionResponse?.Status != ConnectionStatus.Connected || _socket == null)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnected);
    }

    // 2. フレーム文字列をバイト配列に変換（ASCII形式）
    byte[] frameBytes = Encoding.ASCII.GetBytes(frameHexString);

    // 3. ソケット送信
    try
    {
        int bytesSent = await _socket.SendAsync(frameBytes, SocketFlags.None);

        // 4. 送信バイト数検証
        if (bytesSent != frameBytes.Length)
        {
            throw new InvalidOperationException($"送信バイト数不一致: 期待={frameBytes.Length}, 実際={bytesSent}");
        }
    }
    catch (SocketException ex)
    {
        throw new SocketException($"フレーム送信失敗: {ex.Message}");
    }
}
```

**必要なフィールド**:
```csharp
private ConnectionResponse? _connectionResponse;
private Socket? _socket;
private TimeoutConfig _timeoutConfig;
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC021"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - 送信開始ログ: フレーム内容、送信先情報
  - 送信完了ログ: 送信バイト数、所要時間
  - エラーログ: 例外詳細、スタックトレース
- タイムアウト処理追加（Socket.SendTimeout設定）
- エラーハンドリング強化
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC021"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### SLMPフレーム構造

#### M000-M999読み込みフレーム
```
文字列: "54001234000000010401006400000090E8030000"
構成:
- サブヘッダ: 54001234000000 (4Eフレーム識別)
- READコマンド: 0401 (デバイス一括読み出し)
- サブコマンド: 0100 (ビット単位読み出し)
- デバイスコード: 6400 (M機器、0x90のリトルエンディアン表現)
- 開始番号: 00000090 (M000、リトルエンディアン)
- デバイス点数: E8030000 (1000点、0x03E8のリトルエンディアン表現)
```

#### D000-D999読み込みフレーム
```
文字列: "54001234000000010400A800000090E8030000"
構成:
- サブヘッダ: 54001234000000 (4Eフレーム識別)
- READコマンド: 0401 (デバイス一括読み出し)
- サブコマンド: 0000 (ワード単位読み出し)
- デバイスコード: A800 (D機器、0xA8のリトルエンディアン表現)
- 開始番号: 00000090 (D000、リトルエンディアン)
- デバイス点数: E8030000 (1000点、リトルエンディアン)
```

### エラーハンドリング

**スロー例外**:
- `TimeoutException`: 送信タイムアウト（SendTimeoutMs超過）
- `SocketException`: ソケットエラー（接続切断等）
- `InvalidOperationException`: 未接続状態での送信試行
- `ArgumentException`: 不正なフレーム形式

**エラーメッセージ定数**（Core/Constants/ErrorMessages.cs）:
```csharp
public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";
public const string SendTimeout = "フレーム送信がタイムアウトしました。";
public const string InvalidFrame = "不正なSLMPフレーム形式です。";
```

### モック・スタブ実装

**MockSocket**:
- SendAsyncメソッドをモック
- 送信バイト数を記録
- 送信データを記録（検証用）

**MockPlcCommunicationManager**（必要に応じて）:
- ConnectAsync結果をモック
- SendFrameAsync動作をモック

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC021実装.md`
- 実装開始時刻
- 目標（TC021テスト実装完了）
- 実装方針

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/SendFrameAsync実装記録.md`
- 実装判断根拠
  - なぜこの実装方法を選択したか
  - 検討した他の方法との比較
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC021_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC021-1（M000-M999）テストがパス
- [ ] TC021-2（D000-D999）テストがパス
- [ ] SendFrameAsync本体実装完了
- [ ] リファクタリング完了（ログ出力、エラーハンドリング等）
- [ ] テスト再実行でGreen維持確認
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実施リスト.mdの該当項目にチェック

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### 記録の重要性
- 実装判断の根拠を詳細に記録
- テスト結果は数値データも含めて保存

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`
- `documents/design/エラーハンドリング.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### PySLMPClient実装参照
- `PySLMPClient/pyslmpclient/const.py`（デバイスコード定義）
- `PySLMPClient/pyslmpclient/util.py`（フレーム作成ロジック）
- `PySLMPClient/tests/test_main.py`（テストケース実例）

---

以上の指示に従って、TC021_SendFrameAsync_正常送信テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
