# Phase 5.0: 本番統合対応計画

**作成日**: 2025-12-05
**Phase**: Phase 5.0（本番統合対応）
**ステータス**: 計画策定完了、実装待ち

---

## 概要

Phase 1-4で実装した通信プロトコル自動切り替え機能が**テスト環境でのみ動作し、本番環境で完全に機能していない問題**を発見しました。実機検証（Phase 5.1）前に、本番環境への統合対応が必要です。

---

## 発見された問題

### 🔴 問題1: LoggingManager本番環境統合

**現状:**
- `ApplicationController.cs:96-98`でPlcCommunicationManager生成時にLoggingManagerパラメータが未指定
- `_loggingManager`フィールドが常にnullのため、Phase 3で実装した**全てのログ出力が本番で動作しない**

**影響範囲:**
```
PlcCommunicationManager.ConnectAsync():
├─ L93-95:  接続試行開始ログ（INFO）
├─ L172-173: 初期プロトコル失敗・再試行警告ログ（WARNING）
├─ L211-212: 代替プロトコル成功ログ（INFO）
└─ L299-300: 両プロトコル失敗エラーログ（ERROR）
```

**テストでは動作する理由:**
- テストコードではMockLoggingManagerを明示的に注入するため正常動作

---

### 🔴 問題2: ConnectionResponse新規プロパティの活用

**Phase 1で追加されたプロパティ（未活用）:**
- `UsedProtocol` (string?): 実際に使用されたプロトコル（"TCP"/"UDP"）
- `IsFallbackConnection` (bool): 代替プロトコルで接続したか
- `FallbackErrorDetails` (string?): 初期プロトコル失敗時のエラー詳細

**現状:**
- `ExecutionOrchestrator`でConnectResultを受け取るが、`Status`のみ参照（L2789, L3102）
- `DataOutputManager`のJSON出力に代替プロトコル情報が含まれない
- 代替プロトコルで接続成功した重要な情報が**完全に無視されている**

**影響:**
- 運用時に「どのプロトコルで接続できたか」が不明
- 代替プロトコル使用時の記録が残らない
- トラブルシューティングに必要な情報が欠落

---

## 動作状況まとめ

| 機能 | テスト環境 | 本番環境 | 問題 |
|-----|----------|---------|------|
| 代替プロトコル試行 | ✅ 動作 | ✅ 動作 | なし |
| ConnectionResponse新規プロパティ設定 | ✅ 動作 | ✅ 動作 | なし |
| ログ出力（Phase 3） | ✅ 動作 | ❌ 未動作 | LoggingManager未注入 |
| 代替プロトコル情報の活用 | ⚠️ テストのみ | ❌ 未使用 | 本番で参照されていない |
| 送受信処理連携 | ✅ 動作 | ✅ 動作 | なし |

---

## 対応計画

### 修正1: ApplicationControllerでLoggingManager注入

**ファイル**: `andon/Core/Controllers/ApplicationController.cs`

**修正箇所**: L96-98

**修正前:**
```csharp
var manager = new PlcCommunicationManager(
    connectionConfig,
    timeoutConfig);
```

**修正後:**
```csharp
var manager = new PlcCommunicationManager(
    connectionConfig,
    timeoutConfig,
    bitExpansionSettings: null,
    connectionResponse: null,
    socketFactory: null,
    loggingManager: _loggingManager);  // ← LoggingManager注入
```

**期待される効果:**
- Phase 3で実装した全てのログ出力が本番環境で動作開始
- 接続試行履歴が適切に記録される

---

### 修正2: ExecutionOrchestratorで代替プロトコル情報のログ出力

**ファイル**: `andon/Core/Controllers/ExecutionOrchestrator.cs`

**追加箇所**: `ExecuteMultiPlcCycleAsync_Internal`メソッド内、ExecuteFullCycleAsync呼び出し後

**追加コード:**
```csharp
// Step3-6: 完全サイクル実行
var result = await manager.ExecuteFullCycleAsync(...);

// Phase 5.0: 代替プロトコル情報のログ出力
if (result.IsSuccess && result.ConnectResult != null)
{
    if (result.ConnectResult.IsFallbackConnection)
    {
        await (_loggingManager?.LogInfo(
            $"[INFO] PLC #{i+1} は代替プロトコル({result.ConnectResult.UsedProtocol})で接続されました。" +
            $" 初期プロトコル失敗理由: {result.ConnectResult.FallbackErrorDetails}")
            ?? Task.CompletedTask);
    }
    else
    {
        await (_loggingManager?.LogDebug(
            $"[DEBUG] PLC #{i+1} は初期プロトコル({result.ConnectResult.UsedProtocol})で接続されました。")
            ?? Task.CompletedTask);
    }
}
```

**期待される効果:**
- 代替プロトコル使用時のサマリーログが出力される
- トラブルシューティング時に役立つ情報が記録される

---

### 修正3: ErrorMessages.csリファクタリング

**ファイル**: `andon/Core/Constants/ErrorMessages.cs`

**追加メソッド:**
```csharp
/// <summary>
/// 代替プロトコル接続成功のサマリーログメッセージ
/// </summary>
public static string FallbackConnectionSummary(
    int plcIndex, string protocol, string fallbackReason)
{
    return $"PLC #{plcIndex} は代替プロトコル({protocol})で接続されました。" +
           $" 初期プロトコル失敗理由: {fallbackReason}";
}
```

**期待される効果:**
- マジックストリング削減
- メッセージの一元管理

---

## TDDサイクル

### Step 5.0-Red: 本番統合不足の検証テスト作成

1. **ApplicationControllerテスト追加（TC_P5_0_001）**:
   - PlcCommunicationManager生成時にLoggingManagerが注入されることを確認

2. **ExecutionOrchestratorテスト追加（TC_P5_0_002）**:
   - 代替プロトコル使用時のログ出力を確認

**期待される結果:**
- テストが**失敗**する（現在は本番統合されていないため）

---

### Step 5.0-Green: 本番統合実装

上記の修正1-3を実装

**期待される結果:**
- 全テストが**成功**する
- 本番環境でログが正常に出力される

---

### Step 5.0-Refactor: コード品質向上

1. ErrorMessages.csリファクタリング
2. マジックストリング削減
3. 統合テスト追加

**期待される結果:**
- コード品質が向上
- 全テスト成功維持

---

## 完了判定

以下の全条件が満たされた場合、Phase 5.0完了とする:

1. ⏳ ApplicationControllerでLoggingManager注入実装完了
2. ⏳ ExecutionOrchestratorで代替プロトコル情報のログ出力実装完了
3. ⏳ 新規テスト（TC_P5_0_001, TC_P5_0_002）全て成功
4. ⏳ 既存テスト全て成功維持
5. ⏳ ErrorMessages.csリファクタリング完了

---

## 想定工数

| ステップ | 作業内容 | 想定工数 |
|---------|---------|---------|
| **Red** | 本番統合不足の検証テスト作成 | 0.5h |
| **Green** | 本番統合実装（3箇所修正） | 0.8h |
| **Refactor** | コード品質向上 | 0.5h |
| **合計** | | **1.8h** |

---

## 次のステップ

Phase 5.0完了後、**Phase 5.1（実機検証とドキュメント更新）**に進みます。

**Phase 5.1での実施内容:**
1. 実機PLC環境でのテストシナリオ作成（TC_P5_001～003）
2. 実機検証実施
3. README・運用ガイド・XMLコメント更新

---

## 参考情報

**関連ドキュメント:**
- `実装計画/Phase5_実機検証とドキュメント.md`: 詳細な対応計画
- `実装計画/現在の実装状況.md`: 実装状況の全体像
- `実装結果/Phase4_統合テスト_TestResults.md`: Phase 4完了の証跡

**関連ファイル:**
- `andon/Core/Controllers/ApplicationController.cs`
- `andon/Core/Controllers/ExecutionOrchestrator.cs`
- `andon/Core/Managers/DataOutputManager.cs`
- `andon/Core/Constants/ErrorMessages.cs`
- `andon/Core/Managers/PlcCommunicationManager.cs`
