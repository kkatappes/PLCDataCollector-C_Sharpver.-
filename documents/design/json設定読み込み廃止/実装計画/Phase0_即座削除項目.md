# Phase 0: 即座に削除可能な項目の削除

**フェーズ**: Phase 0
**影響度**: なし
**工数**: 小
**前提条件**: なし

---

## 📋 概要

appsettings.jsonに定義されているが、本番環境・テスト環境のいずれでも使用されていない項目を削除します。これらの項目はDIコンテナに登録されておらず、コード内でも完全に参照されていないため、削除しても影響はありません。

---

## 🎯 削除対象項目（25項目以上）

### 1. PlcCommunication.Connection セクション（5項目）

| 項目 | 理由 | 補足 |
|------|------|------|
| IpAddress | ConfigurationLoaderがテストでのみ使用。本番はConfigurationLoaderExcel。Excel機能とは独立しており削除しても影響なし | Excel設定（settingsシート B8セル）で代替済み |
| Port | 同上 | Excel設定（settingsシート B9セル）で代替済み |
| UseTcp | 同上 | Excel設定（settingsシート B10セル）で代替済み |
| IsBinary | 同上 | Phase 2完了でExcel読み込み実装済み |
| FrameVersion | 同上（テストではConfigurationLoader経由で使用） | Phase 2完了でExcel読み込み実装済み |

### 2. PlcCommunication.Timeouts セクション（3項目）

| 項目 | 理由 | 補足 |
|------|------|------|
| ConnectTimeoutMs | コード内で完全に未参照。Excel設定にはタイムアウト項目が存在しないため削除しても影響なし | 未実装機能 |
| SendTimeoutMs | 同上 | 未実装機能 |
| ReceiveTimeoutMs | ConfigurationLoaderがテストでのみ使用。Excel設定にはタイムアウト項目が存在しないため削除しても影響なし | Excel設定には存在しない |

### 3. PlcCommunication.TargetDevices セクション（1項目）

| 項目 | 理由 | 補足 |
|------|------|------|
| Devices | ConfigurationLoaderがテストでのみ使用。本番はConfigurationLoaderExcel（データ収集デバイスシート）。Excel機能とは独立しており削除しても影響なし | Excelのデータ収集デバイスタブで代替済み |

### 4. PlcCommunication.DataProcessing.BitExpansion セクション（全項目）

| 理由 |
|------|
| 実装されていない |

### 5. SystemResources セクション（3項目）

| 項目 | 理由 | 補足 |
|------|------|------|
| MemoryLimitKB | SystemResourcesConfigにプロパティ定義が存在しない（appsettings.jsonに値があっても読み込む先がない）。コード内で完全に未参照 | 実装なし |
| MaxBufferSize | 同上 | 実装なし |
| MemoryThresholdKB | 同上 | 実装なし |

### 6. Logging セクション全体（7項目）⚠️ LoggingConfigとは別物

| 項目 | 理由 | 補足 |
|------|------|------|
| ConsoleOutput.FilePath | コード内で完全に未参照。DIバインドなし。Excel設定とも無関係 | LoggingConfig（本番使用）とは別物 |
| ConsoleOutput.MaxFileSizeMB | 同上 | LoggingConfig（本番使用）とは別物 |
| ConsoleOutput.MaxFileCount | 同上 | LoggingConfig（本番使用）とは別物 |
| ConsoleOutput.FlushIntervalMs | 同上 | LoggingConfig（本番使用）とは別物 |
| DetailedLog.FilePath | 同上 | LoggingConfig（本番使用）とは別物 |
| DetailedLog.MaxFileSizeMB | 同上 | LoggingConfig（本番使用）とは別物 |
| DetailedLog.RetentionDays | 同上 | LoggingConfig（本番使用）とは別物 |

**⚠️ 重要**: `Logging`セクション全体を削除しても、`LoggingConfig`（本番使用中）には影響ありません。この2つは完全に別物です。

---

## 🔍 影響範囲の確認

### 本番環境への影響
**なし** - これらの項目は本番コードで一切使用されていません。

### テスト環境への影響
**最小限** - ConfigurationLoaderを使用しているテストコードは存在しますが、Phase 1で整理します。Phase 0では影響ありません。

### Excel設定機能への影響
**なし** - これらの項目はExcel設定読み込み機能（ConfigurationLoaderExcel）と完全に独立しています。

### LoggingConfig（本番使用中）への影響
**なし** - `Logging`セクションと`LoggingConfig`セクションは完全に別物です。`Logging`セクションを削除しても`LoggingConfig`には影響ありません。

---

## 📝 TDDサイクル: Phase 0

### Step 0-1: 既存テストの確認（Red → Green確認）

**目的**: 削除前に既存機能が正常動作することを確認

#### テストケース

1. **ConfigurationLoaderExcelTests.cs** - Excel設定読み込みが正常動作することを確認
2. **LoggingManagerTests.cs** - LoggingConfig（本番使用中）が正常動作することを確認
3. **ExecutionOrchestratorTests.cs** - MonitoringIntervalMsが正常動作することを確認

#### 期待される結果
全テストがパス（Green状態）

#### 実施コマンド
```bash
dotnet test --filter "FullyQualifiedName~ConfigurationLoaderExcel"
dotnet test --filter "FullyQualifiedName~LoggingManager"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"
```

---

### Step 0-2: 削除後の動作確認テスト作成（Red）

**目的**: 削除後も既存機能が影響を受けないことを事前に確認するテスト

#### テストケース名
`Phase0_UnusedItemsDeletion_NoImpactTests.cs`

#### テストケース詳細

##### 1. test_Excel設定読み込み_appsettings削除後も動作()
- **検証内容**: ConfigurationLoaderExcelがExcel設定のみに依存していることを確認
- **期待結果**: appsettings.jsonの該当項目が存在しなくてもエラーにならないことを確認

```csharp
[Test]
public void test_Excel設定読み込み_appsettings削除後も動作()
{
    // Arrange
    var configLoader = new ConfigurationLoaderExcel();

    // Act
    var result = configLoader.LoadAllPlcConnectionConfigs("./test_settings.xlsx");

    // Assert
    Assert.That(result, Is.Not.Null);
    Assert.That(result.Count, Is.GreaterThan(0));
    // appsettings.jsonの該当項目（Connection, Timeouts, Devices等）が不在でもエラーにならない
}
```

##### 2. test_LoggingConfig_Loggingセクション削除後も動作()
- **検証内容**: LoggingConfigセクションとLoggingセクションが独立していることを確認
- **期待結果**: Loggingセクション削除後もLoggingManagerが正常動作することを確認

```csharp
[Test]
public void test_LoggingConfig_Loggingセクション削除後も動作()
{
    // Arrange
    // appsettings.jsonに「Logging」セクションが存在しない状態をシミュレート
    var loggingManager = CreateLoggingManagerWithoutLoggingSection();

    // Act
    loggingManager.LogInfo("Test message");

    // Assert
    // LoggingConfigセクションの値（LogLevel="Information"等）が正常に使用される
    Assert.That(loggingManager.IsEnabled, Is.True);
}
```

##### 3. test_本番フロー_未使用項目削除後も動作()
- **検証内容**: ApplicationController → ExecutionOrchestrator の実行フローが正常動作
- **期待結果**: 削除対象項目が本番フローで参照されていないことを確認

```csharp
[Test]
public async Task test_本番フロー_未使用項目削除後も動作()
{
    // Arrange
    var orchestrator = CreateOrchestratorWithoutUnusedItems();

    // Act
    var result = await orchestrator.ExecuteMultiPlcCycleAsync_Internal(plcConfigs);

    // Assert
    Assert.That(result.Success, Is.True);
    // PlcCommunication.Connection, Timeouts, Devices等が不在でも正常動作
}
```

#### 期待される結果
全テストが失敗（appsettings.jsonに項目が存在するため）→ 削除後にパス

---

### Step 0-3: 実装（Green）

**作業内容**:

#### 1. appsettings.jsonから削除対象項目を削除

```json
// 削除前のappsettings.json（抜粋）
{
  "PlcCommunication": {
    "Connection": {
      "IpAddress": "172.30.40.40",  // ← 削除
      "Port": 8192,                   // ← 削除
      "UseTcp": false,                // ← 削除
      "IsBinary": true,               // ← 削除
      "FrameVersion": "4E"            // ← 削除
    },
    "Timeouts": {
      "ConnectTimeoutMs": 5000,       // ← 削除
      "SendTimeoutMs": 3000,          // ← 削除
      "ReceiveTimeoutMs": 3000        // ← 削除
    },
    "TargetDevices": {
      "Devices": [...]                // ← 削除
    },
    "DataProcessing": {
      "BitExpansion": {...}           // ← 削除（セクション全体）
    }
  },
  "SystemResources": {
    "MemoryLimitKB": 512,             // ← 削除
    "MaxBufferSize": 1024,            // ← 削除
    "MemoryThresholdKB": 256          // ← 削除
  },
  "Logging": {                        // ← 削除（セクション全体）
    "ConsoleOutput": {...},
    "DetailedLog": {...}
  }
}
```

```json
// 削除後のappsettings.json（抜粋）
{
  "PlcCommunication": {
    "MonitoringIntervalMs": 5000      // ← Phase 2-2で削除予定
  },
  "SystemResources": {
    "MaxMemoryUsageMb": 50,           // ← Phase 1で削除検討
    "MaxConcurrentConnections": 10,   // ← Phase 1で削除検討
    "MaxLogFileSizeMb": 10            // ← Phase 1で削除検討
  },
  "LoggingConfig": {                  // ← Phase 2-1でハードコード化予定
    "LogLevel": "Information",
    "EnableFileOutput": true,
    "EnableConsoleOutput": true,
    "LogFilePath": "./logs",
    "MaxLogFileSizeMb": 1,
    "MaxLogFileCount": 10,
    "EnableDateBasedRotation": true
  }
}
```

#### 2. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase0"
```

---

### Step 0-4: リファクタリング（Refactor）

**作業内容**:

#### 1. ConfigurationLoader.cs のコメント更新
```csharp
/// <summary>
/// appsettings.json読み込み用クラス（テスト専用）
/// 本番環境ではConfigurationLoaderExcelを使用
/// ⚠️ 注意: Phase 1で削除予定
/// </summary>
public class ConfigurationLoader
{
    // ...
}
```

#### 2. 不要なusingディレクティブの削除
```csharp
// 削除前
using System.Text.Json;           // ← 削除対象項目読み込みに使用していた場合
using Microsoft.Extensions.Configuration;

// 削除後
using Microsoft.Extensions.Configuration;
```

#### 3. コード整形
- 空行の削除
- コメントの整理

#### 確認
テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase0"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 0完了の定義

以下の条件をすべて満たすこと：

1. ✅ appsettings.jsonから削除対象25項目以上を削除
2. ✅ Phase0_UnusedItemsDeletion_NoImpactTests.cs の全テストがパス
3. ✅ 既存のすべてのテストがパス（Phase0削除の影響がないことを確認）
4. ✅ ビルドエラーなし
5. ✅ ConfigurationLoader.cs にコメント追加（テスト専用であることを明記）

### 確認コマンド

```bash
# Phase 0のテスト確認
dotnet test --filter "FullyQualifiedName~Phase0"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. LoggingとLoggingConfigの混同に注意

- ❌ **削除対象**: `Logging`セクション（ConsoleOutput, DetailedLog）
- ✅ **削除しない**: `LoggingConfig`セクション（本番環境で使用中）

**間違った削除例**:
```json
// ❌ 間違い: LoggingConfigを削除してしまった
{
  "Logging": {  // ← これは削除してOK（Phase 0）
    ...
  }
  // LoggingConfigが削除されている！
}
```

**正しい削除例**:
```json
// ✅ 正しい: Loggingセクションのみ削除、LoggingConfigは残す
{
  // Loggingセクションは削除済み
  "LoggingConfig": {  // ← これは残す（Phase 2-1でハードコード化予定）
    "LogLevel": "Information",
    ...
  }
}
```

### 2. Excel設定機能への影響確認

削除対象項目はExcel設定とは独立していますが、以下を確認：

```bash
# Excel設定読み込みが正常動作することを確認
dotnet test --filter "FullyQualifiedName~ConfigurationLoaderExcel"
```

### 3. ConfigurationLoaderの扱い

Phase 0ではConfigurationLoader自体は削除しません（Phase 1で対応）。コメント追加のみ実施します。

---

## 📊 削除の影響評価

| 影響範囲 | 影響度 | 詳細 |
|---------|--------|------|
| **本番環境** | なし | 削除対象項目は本番コードで一切使用されていない |
| **テスト環境** | 最小限 | ConfigurationLoaderを使用しているテストは存在するが、Phase 1で整理 |
| **Excel設定機能** | なし | 完全に独立している |
| **LoggingConfig（本番使用）** | なし | `Logging`セクションと`LoggingConfig`は別物 |
| **ビルド** | なし | ビルドエラーなし |

---

## 📈 次のステップ

Phase 0完了後、Phase 1（テスト専用項目の整理）に進みます。

→ [Phase1_テスト専用項目整理.md](./Phase1_テスト専用項目整理.md)

---

## ✅ Phase 0 実装結果（2025-12-02完了）

### 実施サマリー

**実装完了日**: 2025-12-02
**実装方式**: TDD (Red→Green→Refactor)
**テスト結果**: 100% (845/845合格)

### 削除実績

✅ **25項目以上削除完了**:
- PlcCommunication.Connection（5項目）
- PlcCommunication.Timeouts（3項目）
- PlcCommunication.TargetDevices（全体）
- PlcCommunication.DataProcessing.BitExpansion（全体）
- SystemResources未使用項目（3項目）
- Loggingセクション（全体、7項目）

### TDDサイクル実施結果

| ステップ | 状態 | テスト結果 | 備考 |
|---------|------|----------|------|
| Step 0-1 (Green確認) | ✅ 完了 | 830/830合格 | 既存21件の失敗を修正 |
| Step 0-2 (Red) | ✅ 完了 | 5/9失敗 | 期待通りのRed状態 |
| Step 0-3 (Green) | ✅ 完了 | 845/845合格 | Phase 0テスト9件追加 |
| Step 0-4 (Refactor) | ✅ 完了 | 845/845合格 | ConfigurationLoaderコメント追加 |

### 影響評価結果

| 評価項目 | 結果 | 詳細 |
|---------|------|------|
| 本番環境 | 影響なし ✅ | ConfigurationLoaderExcelと完全独立 |
| テスト環境 | 影響なし ✅ | ConfigurationLoader使用テスト正常動作 |
| Excel設定機能 | 影響なし ✅ | 完全独立確認 |
| LoggingConfig | 影響なし ✅ | Loggingセクション削除の影響なし |
| ビルド | 成功 ✅ | ビルドエラーなし |

### 成果物

- ✅ appsettings.json更新（101行→19行、82行削減）
- ✅ Phase0_UnusedItemsDeletion_NoImpactTests.cs作成（9テスト）
- ✅ ConfigurationLoader.csコメント更新
- ✅ [実装結果詳細ドキュメント](../実装結果/Phase0_UnusedItemsDeletion_TestResults.md)

### Phase 1への引き継ぎ

✅ **ConfigurationLoader削除**: Phase 1で完了（2025-12-02）
✅ **SystemResources整理**: Phase 1で完了（SystemResourcesConfig、ResourceManager削除完了）
⏳ **MonitoringIntervalMs整理**: Phase 2-2で実施予定
⏳ **LoggingConfig整理**: Phase 2-1でハードコード化予定
