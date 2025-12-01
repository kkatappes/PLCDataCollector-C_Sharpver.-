# TC020_3_ConnectAsync_null入力テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC020_3_ConnectAsync_null入力テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが、null入力を適切に検出し、例外処理を行うことを検証します。
これは最も基本的な入力検証（防御的プログラミング）です。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC020_3_ConnectAsync_null入力`（3バリエーション）

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Constants/ErrorMessages.cs`

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装（3バリエーション）

**バリエーション1: ConnectionConfig = null**

```csharp
[Fact]
public async Task TC020_3_1_ConnectAsync_ConnectionConfig_null()
{
    // Arrange
    ConnectionConfig config = null;  // null
    var timeout = new TimeoutConfig { /* 正常値 */ };
    var manager = new PlcCommunicationManager();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await manager.ConnectAsync(config, timeout));

    Assert.Equal("connectionConfig", exception.ParamName);
}
```

**バリエーション2: TimeoutConfig = null**

```csharp
[Fact]
public async Task TC020_3_2_ConnectAsync_TimeoutConfig_null()
{
    // Arrange
    var config = new ConnectionConfig { /* 正常値 */ };
    TimeoutConfig timeout = null;  // null
    var manager = new PlcCommunicationManager();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await manager.ConnectAsync(config, timeout));

    Assert.Equal("timeoutConfig", exception.ParamName);
}
```

**バリエーション3: 両方null**

```csharp
[Fact]
public async Task TC020_3_3_ConnectAsync_両方_null()
{
    // Arrange
    ConnectionConfig config = null;
    TimeoutConfig timeout = null;
    var manager = new PlcCommunicationManager();

    // Act & Assert
    var exception = await Assert.ThrowsAsync<ArgumentNullException>(
        async () => await manager.ConnectAsync(config, timeout));

    // どちらか一方のパラメータ名
    Assert.True(
        exception.ParamName == "connectionConfig" ||
        exception.ParamName == "timeoutConfig");
}
```

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync null検証実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**推奨実装パターン（個別チェック）**:
```csharp
public async Task<ConnectionResponse> ConnectAsync(
    ConnectionConfig config,
    TimeoutConfig timeout)
{
    // ConnectionConfig null チェック
    if (config == null)
    {
        throw new ArgumentNullException(
            nameof(config),
            ErrorMessages.ConfigNull);
    }

    // TimeoutConfig null チェック
    if (timeout == null)
    {
        throw new ArgumentNullException(
            nameof(timeout),
            ErrorMessages.ConfigNull);
    }

    // 以降の処理...
}
```

**ErrorMessages定数追加**:
```csharp
public const string ConfigNull = "ConnectionConfigまたはTimeoutConfigがnullです";
```

---

## 技術仕様詳細

### null検証の重要性
- **セキュリティ**: NullReferenceException回避
- **早期検出**: 接続処理前の最優先チェック（フェイルファスト）
- **診断性**: 明確なエラーメッセージ
- **防御的プログラミング**: 最も基本的な入力検証

### ArgumentNullException仕様
- **.NET標準例外**: System.ArgumentNullException
- **用途**: null不可パラメータにnullが渡された時
- **コンストラクタ**: ArgumentNullException(string paramName, string message)

### C# 10.0以降の代替実装
```csharp
ArgumentNullException.ThrowIfNull(config);      // C# 10.0+
ArgumentNullException.ThrowIfNull(timeout);     // C# 10.0+
```

---

## 完了条件

- [ ] TC020_3_1テストがパス（ConnectionConfig = null）
- [ ] TC020_3_2テストがパス（TimeoutConfig = null）
- [ ] TC020_3_3テストがパス（両方null）
- [ ] ArgumentNullException適切にスロー
- [ ] パラメータ名が正確

---

## ログ出力（オプション）

```
[Error] ConnectAsync: ConnectionConfigがnullです
  Exception: ArgumentNullException
  ParamName: connectionConfig
```

**注意**: null検証はログ無しで即座にスローが推奨（パフォーマンス優先）

---

以上の指示に従って、TC020_3_ConnectAsync_null入力テストの実装を開始してください。
