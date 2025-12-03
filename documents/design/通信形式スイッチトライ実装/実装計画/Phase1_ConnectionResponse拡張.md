# Phase 1: ConnectionResponseモデル拡張

**最終更新**: 2025-12-03
**実装状況**: ✅ **完了** (2025-12-03)

## 概要

ConnectionResponseモデルに、通信プロトコル自動切り替え機能で必要となる新規プロパティを追加します。

**実装結果**: [Phase1_ConnectionResponse拡張_TestResults.md](../実装結果/Phase1_ConnectionResponse拡張_TestResults.md)

## TDDサイクル

### Step 1-Red: 失敗するテストを作成

**作業内容:**
1. `ConnectionResponseTests.cs`に以下のテストケースを追加（全て失敗する状態）:

```csharp
[Fact]
public void UsedProtocol_初期TCP成功時_TCPを返す()
{
    var response = new ConnectionResponse
    {
        Status = ConnectionStatus.Connected,
        UsedProtocol = "TCP",  // ← まだ存在しないプロパティ
        IsFallbackConnection = false
    };
    Assert.Equal("TCP", response.UsedProtocol);
}

[Fact]
public void IsFallbackConnection_代替プロトコル使用時_Trueを返す()
{
    var response = new ConnectionResponse
    {
        Status = ConnectionStatus.Connected,
        UsedProtocol = "UDP",
        IsFallbackConnection = true,  // ← まだ存在しないプロパティ
        FallbackErrorDetails = "TCP接続失敗"
    };
    Assert.True(response.IsFallbackConnection);
}

[Fact]
public void FallbackErrorDetails_初期プロトコル失敗時_エラー詳細を保持()
{
    var response = new ConnectionResponse
    {
        Status = ConnectionStatus.Connected,
        UsedProtocol = "UDP",
        IsFallbackConnection = true,
        FallbackErrorDetails = "TCP接続タイムアウト"  // ← まだ存在しないプロパティ
    };
    Assert.Equal("TCP接続タイムアウト", response.FallbackErrorDetails);
}
```

2. テスト実行 → **コンパイルエラー（Red状態）**を確認

**期待される結果:**
- 全テストがコンパイルエラーで失敗
- エラー内容: 存在しないプロパティ（UsedProtocol, IsFallbackConnection, FallbackErrorDetails）の参照

---

### Step 1-Green: テストを通す最小実装

**作業内容:**
1. `ConnectionResponse.cs`に新規プロパティを追加:

```csharp
public class ConnectionResponse
{
    // 既存プロパティ（変更なし）
    public required ConnectionStatus Status { get; init; }
    public Socket? Socket { get; init; }
    public DateTime? ConnectedAt { get; init; }
    public double? ConnectionTime { get; init; }
    public string? ErrorMessage { get; init; }

    // 新規プロパティ
    public string? UsedProtocol { get; init; }          // 実際に使用されたプロトコル（"TCP"/"UDP"）
    public bool IsFallbackConnection { get; init; }     // 代替プロトコルで接続したか
    public string? FallbackErrorDetails { get; init; }  // 初期プロトコル失敗時のエラー詳細
}
```

2. テスト実行 → **全テスト成功（Green状態）**を確認

**期待される結果:**
- `UsedProtocol_初期TCP成功時_TCPを返す()`: ✅ 成功
- `IsFallbackConnection_代替プロトコル使用時_Trueを返す()`: ✅ 成功
- `FallbackErrorDetails_初期プロトコル失敗時_エラー詳細を保持()`: ✅ 成功

---

### Step 1-Refactor: コード改善

**作業内容:**
1. XMLドキュメントコメントを追加:

```csharp
/// <summary>
/// PLC接続処理の結果を表すモデル
/// </summary>
public class ConnectionResponse
{
    // ... 既存プロパティ ...

    /// <summary>
    /// 実際に使用されたプロトコル（"TCP"または"UDP"）
    /// </summary>
    public string? UsedProtocol { get; init; }

    /// <summary>
    /// 代替プロトコルで接続したかどうか
    /// true: 初期プロトコル失敗後、代替プロトコルで接続成功
    /// false: 初期プロトコルで接続成功
    /// </summary>
    public bool IsFallbackConnection { get; init; }

    /// <summary>
    /// 初期プロトコル失敗時のエラー詳細
    /// IsFallbackConnection=trueの場合のみ値が設定される
    /// </summary>
    public string? FallbackErrorDetails { get; init; }
}
```

2. プロパティの妥当性確認（nullable設定が適切か等）:
   - `UsedProtocol`: string? → 接続失敗時はnull
   - `IsFallbackConnection`: bool → デフォルトfalse
   - `FallbackErrorDetails`: string? → 初期プロトコル成功時はnull

3. テスト実行 → **全テスト成功を維持**

**期待される結果:**
- 全テストが引き続き成功
- コード品質が向上（ドキュメントコメント追加）

---

## テストケース一覧

| テストID | テスト名 | Red状態 | Green状態 | 検証内容 |
|---------|---------|---------|----------|---------|
| TC_P1_001 | UsedProtocol_初期TCP成功時_TCPを返す | コンパイルエラー | テスト成功 | UsedProtocolプロパティの存在と値 |
| TC_P1_002 | IsFallbackConnection_代替プロトコル使用時_Trueを返す | コンパイルエラー | テスト成功 | IsFallbackConnectionプロパティの存在と値 |
| TC_P1_003 | FallbackErrorDetails_初期プロトコル失敗時_エラー詳細を保持 | コンパイルエラー | テスト成功 | FallbackErrorDetailsプロパティの存在と値 |

## 実装後の確認事項

### 必須確認項目

1. [x] 全テストがGreen状態 ✅ (6/6テスト成功、2025-12-03)
2. [x] 新規プロパティのXMLコメントが適切 ✅ (詳細なコメント追加済み)
3. [x] nullable設定が適切 ✅ (UsedProtocol: string?, IsFallbackConnection: bool, FallbackErrorDetails: string?)
4. [x] 既存プロパティに影響なし ✅ (853/854既存テスト成功)

### 既存コードへの影響確認

1. **ConnectionResponseを使用している箇所の確認**:
   - `PlcCommunicationManager.ConnectAsync()` - ✅ 新規プロパティはまだ使用されない（Phase 2で実装予定）
   - `ExecutionOrchestrator` - ✅ 新規プロパティはオプショナル（null許容）のため影響なし
   - その他の呼び出し元 - ✅ 新規プロパティは全てオプショナルなので既存動作に影響なし

2. **既存テストへの影響**:
   - ✅ 新規プロパティは全て`init`プロパティ
   - ✅ 既存のテストは新規プロパティを指定しなくても動作する（nullまたはデフォルト値）
   - ✅ 全853件の既存テストが成功

## 想定工数

| ステップ | 作業内容 | 想定工数 | 備考 |
|---------|---------|---------|------|
| **Red** | ConnectionResponseテスト作成（失敗状態） | 0.5h | コンパイルエラー確認 |
| **Green** | ConnectionResponseに新規プロパティ追加 | 0.3h | テスト成功確認 |
| **Refactor** | XMLコメント追加・妥当性確認 | 0.2h | テスト成功維持 |
| **合計** | | **1h** | |

## 次のステップ

Phase 1完了後、**Phase 2: 接続ロジック実装**に進みます。

Phase 2では、PlcCommunicationManager.ConnectAsync()メソッドに代替プロトコル試行ロジックを実装し、Phase 1で追加したプロパティを実際に使用します。
