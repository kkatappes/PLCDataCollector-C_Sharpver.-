# TC020_1_ConnectAsync_不正IPアドレステスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC020_1_ConnectAsync_不正IPアドレステストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが、不正なIPアドレス形式を適切に検出し、例外処理を行うことを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC020_1_ConnectAsync_不正IPアドレス`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (接続処理実装済み)
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Constants/ErrorMessages.cs`

2. **前提テスト確認**
   - TC017～TC020が実装済み・テストパス済みであること

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC020_1: 不正IPアドレステスト**

**Arrange（準備）**:
- ConnectionConfigを作成
  - **IpAddress = "999.999.999.999"（不正なIPアドレス形式、各オクテットが範囲外）**
  - Port = 5000
  - UseTcp = true
  - ConnectionType = "TCP"
- TimeoutConfigを作成
  - ConnectTimeoutMs = 5000
  - SendTimeoutMs = 3000
  - ReceiveTimeoutMs = 5000
- PlcCommunicationManagerインスタンス作成

**Act & Assert（実行・検証）**:
```csharp
// ArgumentException または FormatException がスローされることを確認
var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
    await manager.ConnectAsync(config, timeout));

// 例外メッセージ確認
Assert.Contains("不正なIPアドレス形式", exception.Message);
Assert.Contains("999.999.999.999", exception.Message);

// InnerException確認
Assert.NotNull(exception.InnerException);
Assert.IsType<FormatException>(exception.InnerException);
```

**追加検証項目**:
- 接続処理が実行されていないこと（IPアドレス検証でフェイルファスト）
- Status が Disconnected のままであること

#### Step 1-2: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC020_1"
```

期待結果: テスト失敗（IPアドレス検証が実装されていない、またはFormatExceptionがキャッチされていない）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync IPアドレス検証実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**IPアドレス検証実装**:
```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config, TimeoutConfig timeout)
{
    // null チェック（TC020_3で実装済み想定）
    if (config == null || timeout == null)
    {
        throw new ArgumentNullException(
            config == null ? nameof(config) : nameof(timeout),
            ErrorMessages.ConfigNull);
    }

    // IPアドレス検証（TC020_1）
    IPAddress ipAddress;
    try
    {
        ipAddress = IPAddress.Parse(config.IpAddress);
    }
    catch (FormatException ex)
    {
        throw new ArgumentException(
            string.Format(ErrorMessages.InvalidIpAddress, config.IpAddress),
            nameof(config.IpAddress),
            ex);
    }

    // ポート番号検証（TC020_2で実装予定）
    if (config.Port < 1 || config.Port > 65535)
    {
        throw new ArgumentOutOfRangeException(
            nameof(config.Port),
            config.Port,
            string.Format(ErrorMessages.InvalidPort, config.Port));
    }

    // 既存の接続処理...
}
```

**必要なErrorMessages定数追加**:
```csharp
// Core/Constants/ErrorMessages.cs
public const string InvalidIpAddress = "不正なIPアドレス形式: {0}";
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC020_1"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - エラーログ: 不正IPアドレス検出時、例外詳細、入力値
- エラーハンドリング強化
  - 様々な不正形式への対応確認
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC020_1"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### IPアドレス形式検証

**有効なIPv4アドレス形式**:
- 4つのオクテット（0-255）がドット（.）で区切られている
- 例: "192.168.1.10", "10.0.0.1", "255.255.255.255"

**不正なIPアドレス形式の例**:
| 入力値 | 理由 |
|--------|------|
| "999.999.999.999" | 各オクテットが範囲外（0-255） |
| "256.0.0.1" | 第1オクテットが範囲外 |
| "192.168.1" | オクテット不足 |
| "192.168.1.1.1" | オクテット過剰 |
| "abc.def.ghi.jkl" | 数値以外 |
| "" | 空文字列 |

### IPAddress.Parse動作

**成功時**:
- 正常なIPアドレス文字列 → IPAddressインスタンス生成

**失敗時**:
- FormatExceptionスロー
- 不正な形式を検出

### エラーハンドリングフロー

```
ConnectAsync実行
    ↓
IPAddress.Parse実行
    ↓
不正形式検出
    ↓
FormatException発生
    ↓
catch (FormatException)
    ↓
ArgumentException生成・スロー
    ↓
InnerExceptionにFormatException設定
    ↓
呼び出し元に例外伝播
```

### フェイルファストの重要性

**早期検出の利点**:
- 無駄な接続処理を回避
- システムリソースの保護
- 明確なエラーメッセージ提供
- セキュリティリスク軽減

### エラーメッセージ詳細

**ArgumentException メッセージ形式**:
```
"不正なIPアドレス形式: 999.999.999.999"
```

**含むべき情報**:
- エラー種別（不正なIPアドレス形式）
- 入力された不正な値（999.999.999.999）
- InnerException（FormatException）

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC020_1実装.md`
- 実装開始時刻
- 目標（TC020_1テスト実装完了）
- 実装方針（IPアドレス検証）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/ConnectAsync_IPアドレス検証実装記録.md`
- 実装判断根拠
  - なぜIPAddress.Parseを使用したか
  - 他の検証方法との比較（正規表現等）
  - 技術選択の根拠とトレードオフ
- 発生した問題と解決過程
  - FormatException処理方法
  - エラーメッセージ設計

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC020_1_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- 不正IPアドレスパターンテスト結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC020_1テストがパス
- [ ] ArgumentException適切にスロー
- [ ] InnerExceptionがFormatException
- [ ] 例外メッセージが正確
- [ ] 接続処理が実行されていないことを確認
- [ ] TC017～TC020も引き続きパス（回帰テスト）
- [ ] リファクタリング完了
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### IPアドレス検証固有の注意点
- IPAddress.Parseを使用（.NET標準API）
- FormatExceptionを適切にキャッチ
- ArgumentExceptionに変換してスロー
- InnerExceptionを設定

### 記録の重要性
- IPアドレス検証方法の選択理由を記録
- 他の検証方法との比較を記録
- テスト結果は詳細データも含めて保存

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

### IPアドレス形式仕様
- **有効範囲**: 各オクテット 0-255
- **形式**: "xxx.xxx.xxx.xxx"（ドット区切り4オクテット）
- **検証方法**: IPAddress.Parse()（.NET標準API）

### .NET IPAddress.Parse動作
- **成功**: 正常なIPアドレス文字列 → IPAddressインスタンス
- **失敗**: 不正な形式 → FormatExceptionスロー
- **エラー例**:
  - "999.999.999.999" → FormatException（範囲外）
  - "192.168.1" → FormatException（オクテット不足）
  - "192.168.1.1.1" → FormatException（オクテット過剰）
  - "abc.def.ghi.jkl" → FormatException（数値以外）

### テストデータバリエーション（将来拡張用）
**配置先**: Tests/TestUtilities/TestData/InvalidIpAddresses/

- 範囲外: "999.999.999.999", "256.0.0.1"
- オクテット不足: "192.168.1"
- オクテット過剰: "192.168.1.1.1"
- 数値以外: "abc.def.ghi.jkl"
- 空文字列: ""
- null: null（TC020_3でカバー）

### 関連テストケース
- TC020: 接続拒否（前のテスト）
- TC020_2: 不正ポート番号（次のテスト）
- TC020_3: null入力（最優先異常系）

---

## エラーハンドリング設計

### ErrorMessages定数
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // TC020_1で使用
    public const string InvalidIpAddress = "不正なIPアドレス形式: {0}";

    // 関連メッセージ
    public const string InvalidPort = "ポート番号が範囲外です: {0}（有効範囲: 1-65535）";
    public const string ConfigNull = "ConnectionConfigまたはTimeoutConfigがnullです";
}
```

### エラー分類（ErrorHandler連携）
**TC020_1で発生する例外の分類**:
- **ErrorCategory**: ValidationError
- **Severity**: Error
- **ShouldRetry**: false（入力検証エラーはリトライ不要）
- **ErrorAction**: Abort（処理中断、入力修正が必要）

---

## ログ出力要件

### LoggingManager連携

**エラーログ**:
```
[Error] ConnectAsync: 不正なIPアドレス形式: 999.999.999.999
  Exception: ArgumentException
  InnerException: FormatException
  StackTrace: ...
```

### ログレベル
- **Error**: 不正IPアドレス検出時

### ログ実装例
```csharp
try
{
    ipAddress = IPAddress.Parse(config.IpAddress);
}
catch (FormatException ex)
{
    _logger.LogError(ex, "ConnectAsync: 不正なIPアドレス形式: {IpAddress}", config.IpAddress);
    throw new ArgumentException(
        string.Format(ErrorMessages.InvalidIpAddress, config.IpAddress),
        nameof(config.IpAddress),
        ex);
}
```

---

以上の指示に従って、TC020_1_ConnectAsync_不正IPアドレステストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
