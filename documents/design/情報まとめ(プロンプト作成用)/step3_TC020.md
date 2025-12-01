# Step3 ConnectAsync 接続拒否テスト実装用情報（TC020）

## ドキュメント概要

### 目的
このドキュメントは、TC020_ConnectAsync_接続拒否テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

---

## 1. テスト対象メソッド仕様

### ConnectAsync（Step3: PLC接続処理）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- ConnectionConfig（IpAddress, Port, UseTcp）
  - IpAddress: "192.168.1.10"
  - **Port: 9999（接続拒否するポート）**
  - UseTcp: true
- TimeoutConfig（ConnectTimeoutMs: 5000）

#### Output
- 成功時: ConnectionResponse
- **失敗時（TC020で期待）**: SocketException スロー
  - 例外メッセージ: "接続拒否: 192.168.1.10:9999"

---

## 2. テストケース仕様（TC020）

### TC020_ConnectAsync_接続拒否
**目的**: 接続拒否が正しく検出され、SocketExceptionがスローされることをテスト

#### 前提条件
- 有効なConnectionConfig設定（接続拒否するポート指定）
- **接続先ポートが閉じている（リスニングしていない）**

#### 入力データ
**ConnectionConfig**:
- IpAddress: "192.168.1.10"
- **Port: 9999（接続拒否するポート）**
- UseTcp: true

**TimeoutConfig**:
- ConnectTimeoutMs: 5000

#### 期待動作（異常系）
- **SocketException がスローされる**
- **例外メッセージ**: "接続拒否: 192.168.1.10:9999"
- ConnectionResponse は返却されない

#### 検証項目
1. **SocketException発生確認**:
   ```csharp
   await Assert.ThrowsAsync<SocketException>(async () =>
       await plcCommManager.ConnectAsync(connectionConfig, timeoutConfig));
   ```

2. **例外メッセージ検証**:
   ```csharp
   var exception = await Assert.ThrowsAsync<SocketException>(...);
   Assert.Contains("接続拒否", exception.Message);
   Assert.Contains("192.168.1.10:9999", exception.Message);
   ```

---

## 3. エラーハンドリング

### ConnectAsync スロー例外（TC020で検証）
**SocketException**:
- **発生条件**: 接続拒否（ポートが閉じている）
- **メッセージ形式**: "接続拒否: {IpAddress}:{Port}"
- **ErrorMessages定数**: ErrorMessages.ConnectionRefused

### エラーメッセージ統一
```csharp
public static class ErrorMessages
{
    public const string ConnectionRefused = "接続拒否: {0}:{1}";
}
```

---

## 4. テスト実装構造

### Arrange（準備）
1. ConnectionConfig準備（接続拒否ポート設定）
2. TimeoutConfig準備
3. PlcCommunicationManagerインスタンス作成

### Act（実行）
1. ConnectAsync実行（例外発生を期待）

### Assert（検証）
1. SocketException発生確認
2. 例外メッセージ検証

---

## 5. テスト実装チェックリスト

### TC020実装前
- [ ] TC017, TC019完了済み
- [ ] ErrorMessages.ConnectionRefused定義済み

### TC020実装中
- [ ] Arrange: ConnectionConfig準備（接続拒否ポート）
- [ ] Act: ConnectAsync呼び出し（Assert.ThrowsAsync内）
- [ ] Assert: SocketException発生確認
- [ ] Assert: 例外メッセージ検証

### TC020実装後
- [ ] テスト実行・Green確認
- [ ] TC020_1（不正IPアドレス）への準備

---

以上が TC020_ConnectAsync_接続拒否テスト実装に必要な情報です。
