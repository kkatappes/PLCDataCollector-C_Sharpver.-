# TC020_2_ConnectAsync_不正ポート番号テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC020_2_ConnectAsync_不正ポート番号テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが、不正なポート番号を適切に検出し、例外処理を行うことを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC020_2_ConnectAsync_不正ポート番号`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Constants/ErrorMessages.cs`

2. **前提テスト確認**
   - TC020_1_ConnectAsync_不正IPアドレスが実装済み・テストパス済みであること

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC020_2: 不正ポート番号テスト**

**Arrange（準備）**:
- ConnectionConfigを作成
  - IpAddress = "192.168.1.10"（正常な値）
  - **Port = 70000（範囲外のポート番号、有効範囲: 1-65535）**
  - UseTcp = true
- TimeoutConfigを作成
- PlcCommunicationManagerインスタンス作成

**Act & Assert（実行・検証）**:
```csharp
// ArgumentOutOfRangeException がスローされることを確認
var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
    await manager.ConnectAsync(config, timeout));

// 例外メッセージ確認
Assert.Contains("ポート番号が範囲外です", exception.Message);
Assert.Contains("70000", exception.Message);
Assert.Contains("有効範囲: 1-65535", exception.Message);

// パラメータ名確認
Assert.Contains("Port", exception.ParamName);
```

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync ポート番号検証実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

```csharp
// ポート番号検証（TC020_2）
if (config.Port < 1 || config.Port > 65535)
{
    throw new ArgumentOutOfRangeException(
        nameof(config.Port),
        config.Port,
        string.Format(ErrorMessages.InvalidPort, config.Port));
}
```

**ErrorMessages定数追加**:
```csharp
public const string InvalidPort = "ポート番号が範囲外です: {0}（有効範囲: 1-65535）";
```

---

## 技術仕様詳細

### ポート番号範囲
- **有効範囲**: 1-65535
- **Well-known ports**: 0-1023（予約済み）
- **Registered ports**: 1024-49151
- **Dynamic ports**: 49152-65535

### SLMP標準ポート
- TCP/UDP: 5007（Q00UDPCPUデフォルト）

### テストデータバリエーション
| 入力値 | 期待結果 |
|--------|----------|
| 70000 | 範囲外（上限超過） |
| 65536 | 範囲外（最大値+1） |
| 0 | 範囲外（最小値-1） |
| -1 | 範囲外（負数） |
| 1 | 正常（最小有効値） |
| 65535 | 正常（最大有効値） |

---

## 完了条件

- [ ] TC020_2テストがパス
- [ ] ArgumentOutOfRangeException適切にスロー
- [ ] 例外メッセージが正確
- [ ] パラメータ名が正確
- [ ] TC020_1も引き続きパス（回帰テスト）

---

## ログ出力

```
[Error] ConnectAsync: 不正なポート番号: 70000
  Exception: ArgumentOutOfRangeException
  有効範囲: 1-65535
```

---

以上の指示に従って、TC020_2_ConnectAsync_不正ポート番号テストの実装を開始してください。
