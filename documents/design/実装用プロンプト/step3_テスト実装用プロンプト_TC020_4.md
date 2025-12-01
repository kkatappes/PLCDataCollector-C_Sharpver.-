# TC020_4_ConnectAsync_既接続状態での再接続テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC020_4_ConnectAsync_既に接続済み状態での再接続テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが、既に接続済み状態での再接続試行を適切に検出し、例外処理を行うことを検証します。
これは接続状態管理の安全性を確保する重要なテストです。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC020_4_ConnectAsync_既接続状態での再接続`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`（接続状態管理機能実装済み）
   - `Core/Constants/ErrorMessages.cs`

2. **前提テスト確認**
   - TC017～TC020_3が実装済み・テストパス済みであること

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC020_4: 既接続状態での再接続テスト**

**Arrange（準備）**:
- ConnectionConfigを作成（正常な値）
- TimeoutConfigを作成（正常な値）
- PlcCommunicationManagerインスタンス作成
- **1回目のConnectAsync実行（成功）**
  - Status = Connected確認
  - Socket != null確認

**Act & Assert（実行・検証）**:
```csharp
// 1回目のConnectAsync実行（成功を期待）
var firstResponse = await manager.ConnectAsync(config, timeout);
Assert.NotNull(firstResponse);
Assert.Equal(ConnectionStatus.Connected, firstResponse.Status);
Assert.NotNull(firstResponse.Socket);

// 2回目のConnectAsync実行（例外を期待）
var exception = await Assert.ThrowsAsync<InvalidOperationException>(
    async () => await manager.ConnectAsync(config, timeout));

// 例外メッセージ確認
Assert.Contains("既に接続済みです", exception.Message);
Assert.Contains("DisconnectAsync", exception.Message);
```

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync 接続済みチェック実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config, TimeoutConfig timeout)
{
    // 前処理（null検証、IP検証、ポート検証）...

    // 接続済みチェック（TC020_4）
    if (_socket != null && _socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.AlreadyConnected);
    }

    // 実際の接続処理...
    _socket = CreateSocket(config);
    await _socket.ConnectAsync(new IPEndPoint(ipAddress, config.Port));
    _status = ConnectionStatus.Connected;

    return new ConnectionResponse
    {
        Status = _status,
        Socket = _socket,
        RemoteEndPoint = _socket.RemoteEndPoint,
        ConnectedAt = DateTime.UtcNow,
        ConnectionTime = connectionTime,
        ErrorMessage = null
    };
}
```

**ErrorMessages定数追加**:
```csharp
public const string AlreadyConnected = "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください";
```

---

## 技術仕様詳細

### 接続状態管理

**内部状態**:
- `_socket`: Socketインスタンス（null または有効なSocket）
- `_status`: ConnectionStatus列挙型（Disconnected, Connected, Error）

**接続済み判定**:
```csharp
bool isConnected = _socket != null && _socket.Connected;
```

### 設計判断の重要性

**自動切断しない理由**:
- 意図しない接続切断を防ぐ
- データ送信中の接続切断を防止
- 明示的な切断要求（DisconnectAsync）を強制

**安全な再接続パターン**:
```csharp
// 既存接続がある場合は先に切断
if (plcCommManager.IsConnected)
{
    await plcCommManager.DisconnectAsync();
}

// 再接続
await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig);
```

### DisconnectAsync設計

```csharp
public async Task DisconnectAsync()
{
    if (_socket != null && _socket.Connected)
    {
        await _socket.DisconnectAsync(false);  // 再利用しない
        _socket.Dispose();
        _socket = null;
        _status = ConnectionStatus.Disconnected;
    }
}
```

---

## 完了条件

- [ ] TC020_4テストがパス
- [ ] 1回目のConnectAsync成功確認
- [ ] 2回目のConnectAsync例外発生確認
- [ ] InvalidOperationException適切にスロー
- [ ] 例外メッセージが正確
- [ ] 既存接続が維持される
- [ ] TC020_1～TC020_3も引き続きパス（回帰テスト）

---

## ログ出力

```
[Warning] ConnectAsync: 既に接続済み - 192.168.1.10:5000
  Exception: InvalidOperationException
  Message: 既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください
```

---

以上の指示に従って、TC020_4_ConnectAsync_既接続状態での再接続テストの実装を開始してください。
