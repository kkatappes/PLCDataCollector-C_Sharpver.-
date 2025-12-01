# TC017_1_ConnectAsync_ソケットタイムアウト設定確認テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC017_1_ConnectAsync_ソケットタイムアウト設定確認テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
TC017で実装したConnectAsync()メソッドが、接続後のSocketに対して正しくタイムアウト設定を行うことを検証します。
このテストは、Step4（送受信処理）で個別にタイムアウト制御を実装する必要をなくし、保守性を向上させるための重要な検証です。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC017_1_ConnectAsync_ソケットタイムアウト設定確認`

---

## 前提条件

- TC017_ConnectAsync_TCP接続成功が実装済み・テストパス済みであること
- ConnectionResponse.Socketプロパティへのアクセスが可能であること

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### テストケース実装

**Arrange（準備）**:
- ConnectionConfigを作成
  - IpAddress = "192.168.1.10"
  - Port = 5000
  - UseTcp = true
- TimeoutConfigを作成
  - SendTimeoutMs = 3000
  - ReceiveTimeoutMs = 5000
  - ConnectTimeoutMs = 5000
- MockSocketFactoryを作成
- PlcCommunicationManagerインスタンス作成

**Act（実行）**:
- `var result = await manager.ConnectAsync(connectionConfig, timeoutConfig);`

**Assert（検証）**:
- `result != null`
- `result.Status == ConnectionStatus.Connected`
- `result.Socket != null`
- **重要**: `result.Socket.SendTimeout == timeoutConfig.SendTimeoutMs` (送信タイムアウト設定確認)
- **重要**: `result.Socket.ReceiveTimeout == timeoutConfig.ReceiveTimeoutMs` (受信タイムアウト設定確認)

#### テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC017_1"
```

期待結果: テスト失敗（ソケットタイムアウト設定が未実装の可能性）

---

### Phase 2: Green（最小実装）

TC017の実装で既にSocket.SendTimeout, Socket.ReceiveTimeoutを設定している場合は、このフェーズはスキップ可能です。

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`の`ConnectAsync()`メソッド内

**追加実装**:
```csharp
// ConnectAsync実行後、ConnectionResponse作成前に追加
socket.SendTimeout = timeout.SendTimeoutMs;
socket.ReceiveTimeout = timeout.ReceiveTimeoutMs;
```

#### テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC017_1"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

- ログ出力追加: ソケットタイムアウト設定値の記録
- ドキュメントコメント更新: タイムアウト設定の重要性を明記

---

## 技術仕様詳細

### ソケットタイムアウト設定の重要性

**Step4との関係**:
- Socketレベルでのタイムアウト設定により、SendFrameAsync/ReceiveResponseAsyncで個別にタイムアウト制御を実装する必要がなくなる
- 保守性の向上: タイムアウト制御ロジックが一箇所に集約される
- エラーハンドリングの簡潔化: SocketExceptionとして統一的に処理可能

**設定対象プロパティ**:
- `Socket.SendTimeout`: 送信タイムアウト（ミリ秒）
- `Socket.ReceiveTimeout`: 受信タイムアウト（ミリ秒）

---

## 完了条件

- [ ] TC017_1テストがパス
- [ ] Socket.SendTimeout設定確認
- [ ] Socket.ReceiveTimeout設定確認
- [ ] TC017テストも引き続きパス（回帰テスト）
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実施リスト.mdの該当項目にチェック

---

## 実装時の注意点

### TC017との統合
- TC017の実装に追加する形で実装
- TC017のテストが引き続きパスすることを確認

### Step4への影響
- この実装により、Step4でのタイムアウト制御が不要になる
- SendFrameAsync/ReceiveResponseAsyncの実装がシンプルになる

---

## 参考情報

### 設計書参照先
- `documents/design/テスト内容.md`（TC017_1詳細: 296-303行）

### 関連テストケース
- TC017: TCP接続成功（前提テスト）
- TC021: SendFrameAsync正常送信（タイムアウト設定の恩恵を受ける）
- TC019: ReceiveResponseAsync正常受信（タイムアウト設定の恩恵を受ける）

---

以上の指示に従って、TC017_1_ConnectAsync_ソケットタイムアウト設定確認テストの実装を開始してください。

不明点があれば、実装前に質問してください。
