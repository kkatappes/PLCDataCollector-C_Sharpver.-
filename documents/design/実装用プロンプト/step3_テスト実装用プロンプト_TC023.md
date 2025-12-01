# TC023_SendFrameAsync_未接続状態テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC023_SendFrameAsync_未接続状態テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
SendFrameAsync()メソッドが、未接続状態での送信試行を適切に検出し、例外処理を行うことを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC023_SendFrameAsync_未接続状態`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`（SendFrameAsync実装済み）
   - `Core/Constants/ErrorMessages.cs`

2. **前提テスト確認**
   - TC021_SendFrameAsync_正常送信が実装済み・テストパス済みであること

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC023: 未接続状態送信テスト**

**Arrange（準備）**:
- PlcCommunicationManagerインスタンス作成
- **ConnectAsync実行なし（未接続状態維持）**
- 有効なフレーム準備:
  - M000-M999読み込みフレーム: "54001234000000010401006400000090E8030000"

**Act & Assert（実行・検証）**:
```csharp
// InvalidOperationExceptionがスローされることを確認
var exception = await Assert.ThrowsAsync<InvalidOperationException>(
    async () => await manager.SendFrameAsync(validFrame));

// 例外メッセージ確認
Assert.Contains("PLC未接続状態です", exception.Message);
Assert.Contains("ConnectAsync", exception.Message);
```

**追加検証項目**:
- Socket送信メソッドが呼ばれていないこと
- 接続状態がNotConnectedまたはDisconnectedのまま

---

### Phase 2: Green（最小実装）

#### Step 2-1: SendFrameAsync 接続状態チェック実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

```csharp
public async Task SendFrameAsync(string frame)
{
    // 接続状態チェック（TC023で検証）
    if (_socket == null || !_socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnected);
    }

    // フレーム検証
    if (string.IsNullOrEmpty(frame))
    {
        throw new ArgumentException(ErrorMessages.InvalidFrame);
    }

    // 16進数文字列 → バイト配列変換
    byte[] frameBytes = ConvertHexStringToBytes(frame);

    // 送信実行
    await _socket.SendAsync(frameBytes, SocketFlags.None);
}
```

**ErrorMessages定数**:
```csharp
public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";
```

---

## 技術仕様詳細

### 接続状態検証

**実行可能状態**:
- `ConnectionStatus.Connected`
- `_socket != null && _socket.Connected == true`

**実行不可能状態（TC023で検証）**:
- `ConnectionStatus.NotConnected`: 初期状態
- `ConnectionStatus.Disconnected`: 切断済み
- `ConnectionStatus.Failed`: 接続失敗
- `_socket == null`: Socket未作成
- `_socket.Connected == false`: Socket切断済み

### エラー処理フロー

```
SendFrameAsync実行
    ↓
接続状態チェック
    ↓
_socket == null または !_socket.Connected
    ↓
InvalidOperationException発生
    ↓
エラーメッセージ: "PLC未接続状態です。先にConnectAsync()を実行してください。"
    ↓
Socket送信処理は実行されない
    ↓
呼び出し元に例外伝播
```

---

## 完了条件

- [ ] TC023テストがパス
- [ ] InvalidOperationException適切にスロー
- [ ] 例外メッセージが正確
- [ ] Socket送信メソッド未呼び出し確認
- [ ] 接続状態維持確認
- [ ] TC021, TC022も引き続きパス（回帰テスト）

---

## ログ出力

```
[Error] SendFrameAsync: PLC未接続状態です
  Exception: InvalidOperationException
  ConnectionStatus: NotConnected
  Socket: null
```

---

## フェイルファストの重要性

- **早期検出**: 送信処理前のチェック
- **明確なエラー**: ConnectAsync実行を促すメッセージ
- **安全設計**: 無効な操作からのシステム保護

---

以上の指示に従って、TC023_SendFrameAsync_未接続状態テストの実装を開始してください。
