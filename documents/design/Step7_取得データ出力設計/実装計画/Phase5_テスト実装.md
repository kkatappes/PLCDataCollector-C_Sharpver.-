# Phase5: 統合テスト・検証計画

## 概要
Phase5では、**Step1～Step7の統合テスト**を実装します。Phase2-4で各機能の単体テストは実装済みのため、Phase5では統合テスト、パフォーマンステスト、全体検証に専念します。

---

## Phase4からの引き継ぎ事項（2025-11-27更新）

### Phase4完了事項
✅ **Phase4実装完了**: 2025-11-27（12:25 - 12:35、約10分）
✅ **実装方式**: TDD (Red-Green-Refactor)
✅ **テスト結果**: 22/22テスト成功（既存18 + Phase4の4）
✅ **実装時間**: 約10分

### Phase4で実装された機能
Phase5実装時に利用可能な機能:

1. **ValidateInputData()メソッド** (DataOutputManager.cs:155-204)
   - nullチェック（data, deviceConfig）
   - パラメータ検証（outputDirectory, ipAddress, port）
   - ProcessedDataチェック（null、空）
   - IsSuccess警告（例外をスローしない）
   - Phase5のエラーケース統合テストで利用可能

2. **try-catchブロック** (DataOutputManager.cs:32-145)
   - 7種類の例外に対応（ArgumentNullException、ArgumentException、InvalidOperationException、UnauthorizedAccessException、DirectoryNotFoundException、IOException、Exception）
   - エラーログ出力
   - 再スロー処理
   - Phase5の統合テストでエラーハンドリングを検証可能

3. **ログ出力の強化** (DataOutputManager.cs)
   - 出力開始ログにデバイス数追加
   - 出力完了ログにファイルサイズ追加
   - ディレクトリ自動作成時のログ
   - Phase5の統合テストでログ出力を検証可能

4. **Phase4テスト実装** (DataOutputManagerTests.cs)
   - TC_P4_001: データ検証エラー（null）
   - TC_P4_002: データ検証エラー（空）
   - TC_P4_003: ポート番号不正
   - TC_P4_006: 正常系ログ出力
   - Phase5で再実行して動作確認

### Phase5で解決すべき残課題

Phase4で未解決の課題:

❌ **統合テストの不足**（Phase5実装予定）
- 現状: Phase2-4で単体テストのみ実装
- Phase5実装: Step1～Step7の統合テスト
- 重要度: ★★★★★（最重要）

❌ **5JRS_N2.xlsx実ファイルテスト**（Phase5実装予定）
- 現状: Step1で保留中
- Phase5実装: ConfigurationLoaderExcel_MultiPlcConfigManager_IntegrationTests実行
- 重要度: ★★★★☆（重要）

❌ **パフォーマンステスト**（Phase5実装予定、オプション）
- 現状: 未実装
- Phase5実装: 大量デバイス処理のパフォーマンス測定
- 重要度: ★★☆☆☆（推奨）

### Phase5実装の前提条件

Phase4完了により以下が利用可能:
- ✅ エラーハンドリング機能が完全実装済み
- ✅ データ検証機能が完全実装済み
- ✅ ログ出力機能が完全実装済み
- ✅ Phase2-4の単体テスト22件が成功
- ✅ TDD実装フローが確立（Red-Green-Refactorサイクル）

### Phase5実装時の注意点

**Phase4で確立された品質保証パターンの継続**:
1. テスト先行実装（TDD）
2. AAA（Arrange-Act-Assert）パターン
3. テスト後のクリーンアップ（IDisposableパターン）
4. 実装完了後に実装記録を作成

**Phase4実装記録の参照**:
- `documents/implementation_records/Phase4_DataOutputManager_ErrorHandling_Implementation.txt`
- `documents/design/Step7_取得データ出力設計/実装結果/Phase4_ErrorHandling_Complete_Results.txt`

**Phase4で確認された技術的注意点**:
1. **IsSuccessプロパティ**: 警告のみで例外をスローしない設計（既存テストとの互換性）
2. **エラーログの文字化け**: 現時点で発生していないが、将来的にLoggingManager使用を検討
3. **既存テストへの影響**: Phase5の統合テスト実装後も既存22テストがパスすることを確認

---

## 重要な前提
**Phase2-4で実装済みのテスト**:
- ✅ Phase2: device.number 3桁ゼロ埋めテスト（TC_P2_001～TC_P2_005）
- ✅ Phase3: ビットデバイス分割処理テスト（TC_P3_001～TC_P3_007）
- ✅ Phase4: エラーハンドリングテスト（TC_P4_001～TC_P4_006）

これらのテストは各Phase実装時にTDDで既に実装・合格済みです（合計22テスト）。Phase5ではこれらを再実行し、統合テストを追加します。

---

## 実装対象

### 1. Phase2-4テストの再実行・確認

#### 既存テストファイル
**ファイル**: `Tests/Unit/Core/Managers/DataOutputManagerTests.cs`（Phase2-4で実装済み）

#### 実施内容
- Phase2-4で実装済みのテストを再実行
- すべてのテストがパスすることを確認
- リグレッション（機能退行）がないことを確認

#### テストクラス構造

```csharp
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Andon.Tests.Unit.Core.Managers
{
    public class DataOutputManagerTests : IDisposable
    {
        private readonly DataOutputManager _manager;
        private readonly string _testOutputDirectory;

        public DataOutputManagerTests()
        {
            _manager = new DataOutputManager();
            _testOutputDirectory = Path.Combine(Path.GetTempPath(), "andon_test_output");
        }

        public void Dispose()
        {
            // テスト後のクリーンアップ
            if (Directory.Exists(_testOutputDirectory))
            {
                Directory.Delete(_testOutputDirectory, true);
            }
        }

        // テストケース実装（後述）
    }
}
```

---

### 2. 統合テストの新規実装

#### モックデータ作成メソッド

```csharp
// ビットデバイスのモックデータ
private ProcessedResponseData CreateMockProcessedResponseData_BitDevice()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["M0"] = new DeviceData
            {
                DeviceName = "M0",
                Code = DeviceCode.M,
                Address = 0,
                Value = 0b1010110011010101,  // 43605
                Type = "Bit"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// ワードデバイスのモックデータ
private ProcessedResponseData CreateMockProcessedResponseData_WordDevice()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["D100"] = new DeviceData
            {
                DeviceName = "D100",
                Code = DeviceCode.D,
                Address = 100,
                Value = 12345,
                Type = "Word"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// ダブルワードデバイスのモックデータ
private ProcessedResponseData CreateMockProcessedResponseData_DWordDevice()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["D200"] = new DeviceData
            {
                DeviceName = "D200",
                Code = DeviceCode.D,
                Address = 200,
                Value = 0x12345678,
                IsDWord = true,
                Type = "DWord"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// 混在デバイスのモックデータ
private ProcessedResponseData CreateMockProcessedResponseData_Mixed()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>
        {
            ["M0"] = new DeviceData
            {
                DeviceName = "M0",
                Code = DeviceCode.M,
                Address = 0,
                Value = 0b1010110011010101,
                Type = "Bit"
            },
            ["D100"] = new DeviceData
            {
                DeviceName = "D100",
                Code = DeviceCode.D,
                Address = 100,
                Value = 12345,
                Type = "Word"
            },
            ["D200"] = new DeviceData
            {
                DeviceName = "D200",
                Code = DeviceCode.D,
                Address = 200,
                Value = 0x12345678,
                IsDWord = true,
                Type = "DWord"
            }
        },
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}
```

#### deviceConfigのモック作成メソッド

```csharp
// ビットデバイス用deviceConfig
private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_BitDevice()
{
    var config = new Dictionary<string, DeviceEntryInfo>();
    for (int i = 0; i < 16; i++)
    {
        config[$"M{i}"] = new DeviceEntryInfo
        {
            Name = $"ビットデバイスM{i}",
            Digits = 1
        };
    }
    return config;
}

// ワードデバイス用deviceConfig
private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_WordDevice()
{
    return new Dictionary<string, DeviceEntryInfo>
    {
        ["D100"] = new DeviceEntryInfo { Name = "生産台数", Digits = 5 }
    };
}

// ダブルワードデバイス用deviceConfig
private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_DWordDevice()
{
    return new Dictionary<string, DeviceEntryInfo>
    {
        ["D200"] = new DeviceEntryInfo { Name = "累積生産台数", Digits = 10 }
    };
}

// 混在デバイス用deviceConfig
private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_Mixed()
{
    var config = CreateMockDeviceConfig_BitDevice();
    config["D100"] = new DeviceEntryInfo { Name = "生産台数", Digits = 5 };
    config["D200"] = new DeviceEntryInfo { Name = "累積生産台数", Digits = 10 };
    return config;
}
```

---

### 3. Step1で保留中のテスト実行

#### ビルドエラー修正後の実施事項
Phase5実装開始前に、**Step1で保留中だったテスト**を実行します。

**対象テスト**: `ConfigurationLoaderExcel_MultiPlcConfigManager_IntegrationTests`
- 実ファイル（5JRS_N2.xlsx）を使用した統合テスト
- 216件のデバイス情報読み込み確認
- MultiPlcConfigManagerとの統合確認

**実行タイミング**: Phase5開始時（ビルドエラー修正直後）

---

### 4. 統合テスト（新規実装）

#### テストファイル
**ファイル**: `Tests/Integration/Step3_6_IntegrationTests.cs`

#### 統合テストケース

```csharp
[Fact]
public async Task TC_Integration_Step3_to_Step7_Success()
{
    // Arrange
    var configToFrameManager = new ConfigToFrameManager(...);
    var plcCommunicationManager = new PlcCommunicationManager(...);
    var dataOutputManager = new DataOutputManager();

    // Step1-2: 設定読み込み・フレーム構築
    var config = await configToFrameManager.LoadConfigAsync("appsettings.json");

    // Step3-6: PLC通信・データ取得（モック）
    var processedData = CreateMockProcessedResponseData_Mixed();

    // Step7: データ出力
    dataOutputManager.OutputToJson(
        processedData,
        "./integration_test_output",
        config.Connection.IpAddress,
        config.Connection.Port,
        BuildDeviceConfigDictionary(config.TargetDevices)
    );

    // Assert
    var files = Directory.GetFiles("./integration_test_output", "*.json");
    Assert.Single(files);

    var jsonContent = File.ReadAllText(files[0]);
    var jsonDocument = JsonDocument.Parse(jsonContent);

    // JSON構造検証
    Assert.True(jsonDocument.RootElement.TryGetProperty("source", out _));
    Assert.True(jsonDocument.RootElement.TryGetProperty("timestamp", out _));
    Assert.True(jsonDocument.RootElement.TryGetProperty("items", out _));
}
```

---

### 6. パフォーマンステスト（オプション）

#### テストファイル
**ファイル**: `Tests/Performance/DataOutputPerformanceTests.cs`

#### パフォーマンステストケース

```csharp
[Fact]
public void Performance_OutputToJson_1000Devices_Within500ms()
{
    // Arrange
    var data = CreateMockProcessedResponseData_LargeScale(1000);
    var deviceConfig = CreateMockDeviceConfig_LargeScale(1000);
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    // Act
    _manager.OutputToJson(data, _testOutputDirectory, "192.168.1.10", 5007, deviceConfig);
    stopwatch.Stop();

    // Assert
    Assert.True(stopwatch.ElapsedMilliseconds < 500,
        $"処理時間が500msを超えました: {stopwatch.ElapsedMilliseconds}ms");
}

private ProcessedResponseData CreateMockProcessedResponseData_LargeScale(int deviceCount)
{
    var data = new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>(),
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };

    for (int i = 0; i < deviceCount; i++)
    {
        data.ProcessedData[$"D{i}"] = new DeviceData
        {
            DeviceName = $"D{i}",
            Code = DeviceCode.D,
            Address = i,
            Value = (uint)i,
            Type = "Word"
        };
    }

    return data;
}
```

---

## 実装手順

### Step 1: Phase2-4テストの再実行
1. Phase2-4で実装済みのテストを再実行
   ```bash
   dotnet test --filter "FullyQualifiedName~DataOutputManagerTests"
   ```
2. すべてのテストがパスすることを確認
3. リグレッションがないことを確認

### Step 2: Step1保留テストの実行
1. ビルドエラーが修正されていることを確認
2. Step1統合テストを実行
   ```bash
   dotnet test --filter "FullyQualifiedName~ConfigurationLoaderExcel_MultiPlcConfigManager_IntegrationTests"
   ```
3. 5JRS_N2.xlsx読み込みテストが合格することを確認

### Step 3: 統合テスト実装
1. `Tests/Integration/Step1_to_Step7_IntegrationTests.cs`を作成
2. Step1設定読み込み→Step2フレーム構築→Step3-6通信→Step7出力の一貫処理を実装
3. 統合テスト実行・パス確認

### Step 4: パフォーマンステスト実装（オプション）
1. `Tests/Performance/DataOutputPerformanceTests.cs`を作成
2. 大量デバイス処理のパフォーマンスを測定

### Step 5: 全テスト実行・確認
1. `dotnet test`を実行
2. すべてのテストがパスすることを確認
3. テストカバレッジを確認（80%以上）

---

## テスト実行方法

### 単体テストのみ実行
```bash
dotnet test --filter "FullyQualifiedName~DataOutputManagerTests"
```

### 統合テストのみ実行
```bash
dotnet test --filter "FullyQualifiedName~Step3_6_IntegrationTests"
```

### すべてのテスト実行
```bash
dotnet test
```

### カバレッジ測定（オプション）
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## 完了条件

### 必須項目
- [ ] **Phase2-4のテストがすべて再実行され、パスする**
- [ ] **Step1保留テスト（5JRS_N2.xlsx統合テスト）が実行され、パスする**
- [ ] **統合テスト（Step1～Step7）が実装され、パスする**
- [ ] すべてのテスト（単体・統合）がパスする
- [ ] テストカバレッジが80%以上
- [ ] リグレッション（機能退行）が発生していない

### 推奨項目
- [ ] パフォーマンステストが実装されている
- [ ] テストカバレッジが90%以上
- [ ] 実ファイル（5JRS_N2.xlsx）を使用したEnd-to-Endテストが成功する

---

## テスト実装のベストプラクティス

### 1. AAA（Arrange-Act-Assert）パターン
```csharp
[Fact]
public void TestMethod()
{
    // Arrange: テストデータ準備
    var data = CreateMockData();

    // Act: テスト対象メソッド実行
    var result = _manager.OutputToJson(...);

    // Assert: 結果検証
    Assert.True(result.Success);
}
```

### 2. テストの独立性
- 各テストは独立して実行可能にする
- テスト間でファイルやデータを共有しない
- テスト後のクリーンアップを忘れない（IDisposableパターン）

### 3. テスト名の命名規則
- `TC番号_メソッド名_テストケース_期待結果`
- 例: `TC042_OutputToJson_BitDeviceOnly_Success`

### 4. モックデータの再利用
- 共通のモックデータは専用メソッドで生成
- テストケース間でモックデータを再利用

---

## 既存実装との差異

### Phase5で解決される問題
- ✅ 包括的なテストの不足
- ✅ モック・スタブの不足
- ✅ 統合テストの不足

### Phase5完了後の状態
- ✅ すべての機能が十分にテストされている
- ✅ エラーケースも網羅的にテストされている
- ✅ パフォーマンスも検証されている

---

## 次のステップ

Phase5完了後、**Step7データ出力機能の実装は完了**です。

次は以下の作業に進んでください:
1. Step1～Step7の統合テスト
2. 実機テスト（PLC接続環境）
3. ドキュメントの最終更新

---

## 参照文書
- `実装ガイド.md`: テスト要件（セクション7）
- `実装時対応関係.md`: モックデータの作成方法（セクション11）

## 作成日時
- **作成日**: 2025年11月27日
- **最終更新**: 2025年11月27日（Phase4引き継ぎ事項追記）
- **対象Phase**: Phase5（テスト実装）

---

## Phase5実装開始前のチェックリスト

Phase5実装を開始する前に、以下を確認してください:

### 前提条件確認
- [x] Phase4実装完了を確認（Phase4完了日: 2025-11-27）
- [x] Phase4テスト結果を確認（22/22テスト成功）
- [x] Phase4実装記録を読了
- [x] エラーハンドリング機能が正常動作することを確認
- [x] ValidateInputData()メソッドの理解
- [x] try-catchブロックの実装パターンの理解

### 実装準備
- [ ] Phase5実装計画を理解
- [ ] 統合テストの実装方針を理解
- [ ] モックデータ作成方法を理解
- [ ] AAA（Arrange-Act-Assert）パターンを理解

### 開発環境確認
- [ ] `dotnet build`が成功することを確認
- [ ] 既存テスト22/22が成功することを確認
- [ ] 開発環境がクリーンな状態であることを確認

**Phase5実装準備完了**: すべてのチェックボックスが✅になったら実装開始可能

---

## Phase4実装で得た知見の活用

### 1. TDD手法の効果（Phase4実績）
Phase4でのTDD実装結果:
- **実装時間**: 約10分（Phase3の3時間と比較して短時間）
- **テスト成功率**: 100%（22/22）
- **リファクタリングの安全性**: IsSuccessチェック変更時もテストで安全に確認

Phase5でも同様のTDD手法を継続:
- Red-Green-Refactorサイクルを厳守
- テスト失敗理由を明確化
- リファクタリング時の安全性確保

### 2. エラーハンドリングのパターン（Phase4で確立）
Phase4で確立したパターン:
- 例外の再スロー（ログ出力後に`throw;`）
- 例外の詳細な分類
- エラーメッセージへの具体的情報含有

Phase5の統合テストでも同様のパターンを検証

### 3. IsSuccessプロパティの扱い（Phase4の教訓）
Phase4での対応:
- IsSuccessチェックは警告のみ（例外をスローしない）
- 既存テストとの互換性を優先

Phase5での対応:
- 統合テストでは`IsSuccess=true`を明示的に設定
- エラーケース統合テストでは`IsSuccess=false`の挙動も検証

### 4. ログ出力の検証方法（Phase4で実装）
Phase4で実装したログ検証パターン:
```csharp
var output = new System.IO.StringWriter();
Console.SetOut(output);
try
{
    // テスト実行
    var logOutput = output.ToString();
    Assert.Contains("[INFO] JSON出力開始", logOutput);
}
finally
{
    Console.SetOut(Console.Out);
}
```

Phase5の統合テストでも同様のパターンを使用

---

## Phase4完了時の実装状況（参考情報）

### DataOutputManagerの現在の行数
- 全体: 約280行（Phase4完了時点）
- OutputToJson(): 約115行（try-catchブロック含む）
- ValidateInputData(): 約50行
- ヘルパーメソッド群: 約80行（Phase3で実装）
- Phase5実装後: 行数変化なし見込み（テストファイルのみ増加）

### Phase4で追加されたコード量
- 実装コード: 約55行（ValidateInputData + try-catch）
- テストコード: 約140行（TC_P4_001～006）
- コメント・ドキュメント: 約20行

### Phase4で確立されたテストパターン
1. **AAA（Arrange-Act-Assert）パターン**: 明確な3段階構成
2. **例外検証**: Assert.Throws<TException>の使用
3. **ログ検証**: Console出力のキャプチャと検証
4. **テストクリーンアップ**: IDisposableパターンの使用

---

## Phase4実装時間の内訳（Phase5見積もりの参考）

### Phase4実装時間: 約10分
- Red Phase（テスト実装）: 約3分
- Green Phase（機能実装）: 約5分
- Refactor Phase（IsSuccess変更）: 約2分

### Phase5予想実装時間
- 統合テスト実装: 約30分～1時間（モックデータ作成含む）
- パフォーマンステスト実装（オプション）: 約20分
- 全体検証: 約10分
- 実装記録作成: 約10分
- **合計**: 約1～2時間見込み
