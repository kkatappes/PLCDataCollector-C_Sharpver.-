# ReadRandom Phase8.5 実装・テスト結果

**作成日**: 2025-12-02
**最終更新**: 2025-12-02

## 概要

ReadRandom(0x0403)コマンド実装のPhase8.5（実機エラー暫定対策）で実装した`DeviceSpecifications`プロパティ再追加および`ExtractDeviceValuesFromReadRandom()`メソッドのテスト結果。実機テスト（PLC 172.30.40.15:8192）で発見された「サポートされていないデータ型です:」エラーの暫定対策を完了。Phase12根本対策への移行準備も完了。

---

## 1. 実装内容

### 1.1 実装クラス

| クラス名 | 機能 | ファイルパス |
|---------|------|------------|
| `ProcessedDeviceRequestInfo` | デバイス要求情報（DeviceSpecifications追加） | `Core/Models/ProcessedDeviceRequestInfo.cs` |
| `ExecutionOrchestrator` | DeviceSpecifications設定処理 | `Core/Controllers/ExecutionOrchestrator.cs` |
| `PlcCommunicationManager` | ReadRandomレスポンス処理 | `Core/Managers/PlcCommunicationManager.cs` |

### 1.2 実装メソッド

| メソッド名 | 機能 | 戻り値 |
|-----------|------|--------|
| `ExtractDeviceValues()` | ReadRandom/レガシー処理分岐 | `List<ProcessedDevice>` |
| `ExtractDeviceValuesFromReadRandom()` | ReadRandomレスポンス専用処理 | `List<ProcessedDevice>` |
| `ExecuteSingleCycleAsync()` | DeviceSpecifications設定 | `Task` |

### 1.3 重要な実装判断

**DeviceSpecificationsプロパティの再追加**:
- Phase3.5で削除されたプロパティをPhase8.5で復活
- 理由: ReadRandom(0x0403)の本質的な設計に合致（複数デバイス指定、不連続アドレスOK）
- トレードオフ: 設計の後退感あるが、Phase12での根本対策への移行が容易

**後方互換性の完全維持**:
- `DeviceSpecifications`がnullの場合は既存の処理を維持（DeviceType/StartAddress/Count使用）
- 理由: 既存テストコード資産を破壊しない、段階的移行を可能に

**ExtractDeviceValuesFromReadRandom()の独立メソッド化**:
- ReadRandom専用処理を独立したprivateメソッドとして実装
- 理由: テスト容易性、Phase12での再利用性、レガシー処理との明確な分離

---

## 2. テスト結果

### 2.1 全体サマリー

```
実行日時: 2025-12-02
VSTest: 17.14.1 (x64)
.NET: 9.0

結果: 成功 - 失敗: 0、合格: 19、スキップ: 0、合計: 19
実行時間: 5.9秒
```

### 2.2 テストケース内訳

| テストクラス | テスト数 | 成功 | 失敗 | 実行時間 |
|-------------|----------|------|------|----------|
| ProcessedDeviceRequestInfoTests | 2 | 2 | 0 | ~0.2秒 |
| ExecutionOrchestratorTests | 1 | 1 | 0 | ~0.5秒 |
| PlcCommunicationManagerTests | 2 | 2 | 0 | ~0.4秒 |
| Step3_6_IntegrationTests | 14 | 14 | 0 | ~5.0秒 |
| **合計** | **19** | **19** | **0** | **5.9秒** |

---

## 3. テストケース詳細

### 3.1 ProcessedDeviceRequestInfoTests (2テスト)

| テストカテゴリ | テスト数 | 検証内容 | 実行結果 |
|---------------|----------|---------|----------|
| DeviceSpecificationsプロパティ | 2 | nullable List型、後方互換性 | ✅ 全成功 |

**検証ポイント**:
- DeviceSpecifications: `List<DeviceSpecification>?` 型で追加
- nullableで後方互換性を維持（既存コードはnullのまま動作）
- 複数デバイス指定が可能（ReadRandom仕様に合致）

**実行結果例**:

```
✅ 成功 DeviceSpecifications_Should_BeNullableList [< 1 ms]
✅ 成功 DeviceSpecifications_Should_AllowNull_ForBackwardCompatibility [< 1 ms]
```

### 3.2 ExecutionOrchestratorTests (1テスト)

| テストカテゴリ | テスト数 | 検証内容 | 実行結果 |
|---------------|----------|---------|----------|
| DeviceSpecifications設定 | 1 | PlcConfigurationからの自動設定 | ✅ 全成功 |

**検証ポイント**:
- `config.Devices` → `deviceRequestInfo.DeviceSpecifications` への自動設定
- モックを使用してリクエスト情報のキャプチャ検証
- DeviceCode、DeviceNumberの正確性検証

**実行結果例**:

```
✅ 成功 Phase85_ExecuteSingleCycleAsync_Should_SetDeviceSpecifications_FromPlcConfiguration [< 1 ms]
```

**テストデータ例**:

```csharp
var config = new PlcConfiguration
{
    IpAddress = "172.30.40.15",
    Port = 8192,
    Devices = new List<DeviceSpecification>
    {
        new DeviceSpecification(DeviceCode.D, 100),
        new DeviceSpecification(DeviceCode.M, 200)
    }
};

// Assert
Assert.NotNull(capturedRequestInfo.DeviceSpecifications);
Assert.Equal(2, capturedRequestInfo.DeviceSpecifications.Count);
Assert.Equal(DeviceCode.D, capturedRequestInfo.DeviceSpecifications[0].Code);
Assert.Equal(100, capturedRequestInfo.DeviceSpecifications[0].DeviceNumber);
```

**実行結果**: ✅ 成功 (< 1ms)

### 3.3 PlcCommunicationManagerTests (2テスト)

| テストカテゴリ | テスト数 | 検証内容 | 実行結果 |
|---------------|----------|---------|----------|
| ReadRandomレスポンス処理 | 1 | 複数デバイス値抽出 | ✅ 全成功 |
| レガシーモードフォールバック | 1 | DeviceSpecifications=null時の動作 | ✅ 全成功 |

**検証ポイント**:
- ReadRandomレスポンス: 2バイト（1ワード）ずつ処理
- リトルエンディアン変換: `BitConverter.ToUInt16()`で正確に変換
- 後方互換性: `DeviceSpecifications=null`時は既存処理を使用

**実行結果例**:

```
✅ 成功 Phase85_ExtractDeviceValues_Should_ProcessReadRandomResponse_WithMultipleDevices [< 1 ms]
✅ 成功 Phase85_ExtractDeviceValues_Should_FallbackToLegacyMode_WhenDeviceSpecificationsIsNull [< 1 ms]
```

**テストデータ例 - ReadRandomレスポンス処理**:

```csharp
var responseData = new byte[]
{
    0x96, 0x00,  // D100 = 150 (LE)
    0x01, 0x00,  // M200 = 1 (word形式、下位バイトが1)
};

var requestInfo = new ProcessedDeviceRequestInfo
{
    DeviceSpecifications = new List<DeviceSpecification>
    {
        new DeviceSpecification(DeviceCode.D, 100),
        new DeviceSpecification(DeviceCode.M, 200)
    },
    FrameType = FrameType.Frame4E,
    RequestedAt = DateTime.UtcNow
};

// Act
var result = manager.ExtractDeviceValues(responseData, requestInfo, DateTime.UtcNow);

// Assert
Assert.Equal(2, result.Count);
Assert.Equal("D", result[0].DeviceType);
Assert.Equal(100, result[0].Address);
Assert.Equal(150, result[0].Value);

Assert.Equal("M", result[1].DeviceType);
Assert.Equal(200, result[1].Address);
Assert.Equal(1, result[1].Value);
```

**実行結果**: ✅ 成功 (< 1ms)

---

**テストデータ例 - レガシーモードフォールバック**:

```csharp
var responseData = new byte[]
{
    0x96, 0x00,  // D100 = 150
    0x97, 0x00   // D101 = 151
};

var requestInfo = new ProcessedDeviceRequestInfo
{
    DeviceSpecifications = null, // ← nullの場合
    DeviceType = "D",            // ← 既存プロパティを使用
    StartAddress = 100,
    Count = 2,
    FrameType = FrameType.Frame3E,
    RequestedAt = DateTime.UtcNow
};

// Act
var result = manager.ExtractDeviceValues(responseData, requestInfo, DateTime.UtcNow);

// Assert
Assert.Equal(2, result.Count);
Assert.Equal("D", result[0].DeviceType);
Assert.Equal(100, result[0].Address);
```

**実行結果**: ✅ 成功 (< 1ms)

### 3.4 Step3_6_IntegrationTests (14テスト)

| テストカテゴリ | テスト数 | 検証内容 | 実行結果 |
|---------------|----------|---------|----------|
| PLC通信統合テスト | 14 | エンドツーエンド動作確認 | ✅ 全成功 |

**検証ポイント**:
- ConnectAsync → SendRequestAsync → ReceiveResponseAsync → ExtractDeviceValues の統合動作
- 4Eフレーム・3Eフレーム両方の動作確認
- モックを使用したオフラインテスト（実機PLC接続なし）
- タイムアウト、エラーハンドリングの統合検証

**実行結果例**:

```
✅ 成功 ExecuteFullCycleAsync_ValidConfiguration_ReturnsSuccess [< 1 ms]
✅ 成功 ExecuteFullCycleAsync_4EFrame_Binary_Success [< 1 ms]
✅ 成功 ExecuteFullCycleAsync_3EFrame_Binary_Success [< 1 ms]
✅ 成功 ExecuteFullCycleAsync_ReadRandom_MultipleDevices_Success [< 1 ms]
... (計14テスト全て成功)
```

---

## 4. 実機エラー対策の検証

### 4.1 問題の発見

**エラー症状** (Phase9実機テスト):
```
サポートされていないデータ型です:
```

**発生箇所**:
- `PlcCommunicationManager.ExtractDeviceValues()` (line 1938)
- PLC: 172.30.40.15:8192
- フレーム: 4Eフレーム（Binary）
- プロトコル: UDP

### 4.2 根本原因

`ExecutionOrchestrator.cs`:199行目で空の`ProcessedDeviceRequestInfo`を作成:

```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo();
// ↑ すべてのプロパティがデフォルト値のまま
```

**未初期化プロパティ**:
- `DeviceType`: 空文字列 ("") ← `switch`文で該当せずエラー
- `StartAddress`: 0
- `Count`: 0
- `DeviceSpecifications`: Phase3.5で削除済み（致命的）

### 4.3 対策内容

**Phase8.5暫定対策**:
```csharp
// ExecutionOrchestrator.cs (line 199-205)
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    DeviceSpecifications = config.Devices?.ToList(), // ← PlcConfigurationから設定
    FrameType = config.FrameVersion == "4E" ? FrameType.Frame4E : FrameType.Frame3E,
    RequestedAt = DateTime.UtcNow
};
```

**ExtractDeviceValues()の修正**:
```csharp
// PlcCommunicationManager.cs (line 1921-1948)
private List<ProcessedDevice> ExtractDeviceValues(
    byte[] deviceData,
    ProcessedDeviceRequestInfo requestInfo,
    DateTime processedAt)
{
    var devices = new List<ProcessedDevice>();

    // Phase8.5暫定対策: DeviceSpecificationsが設定されている場合はReadRandom処理
    if (requestInfo.DeviceSpecifications != null && requestInfo.DeviceSpecifications.Any())
    {
        return ExtractDeviceValuesFromReadRandom(deviceData, requestInfo, processedAt);
    }

    // 後方互換性: 既存の処理を維持（DeviceType/StartAddress/Countを使用）
    switch (requestInfo.DeviceType.ToUpper())
    {
        case "D":
            devices.AddRange(ExtractWordDevices(deviceData, requestInfo, processedAt));
            break;

        case "M":
            devices.AddRange(ExtractBitDevices(deviceData, requestInfo, processedAt));
            break;

        default:
            throw new NotSupportedException(
                string.Format(ErrorMessages.UnsupportedDataType, requestInfo.DeviceType));
    }

    return devices;
}
```

### 4.4 対策の効果

✅ **期待される結果**（実機テストで確認予定）:
- 「サポートされていないデータ型です:」エラーが発生しない
- ReadRandomコマンドでデバイス値が正しく取得できる
- 複数デバイス指定が正しく動作する

✅ **テストでの確認済み事項**:
- DeviceSpecifications設定処理が正常動作（1件テスト合格）
- ReadRandomレスポンス処理が正常動作（2件テスト合格）
- 統合テストで全14件合格

---

## 5. 追加修正（Phase8.5外）

### 5.1 DataOutputManager timestamp変数欠落の修正

**問題**: Phase7実装時に`timestamp`変数定義が欠落
**エラーメッセージ**: `現在のコンテキストに 'timestamp' という名前は存在しません`
**発生箇所**: `DataOutputManager.cs`:92行目

**修正内容**:
```csharp
// DataOutputManager.cs (line 51-52追加)
// Phase7: データ処理時刻を取得（JSON timestampフィールド用）
var timestamp = data.ProcessedAt;
```

**修正理由**: Phase7設計書（line 119）で定義されていたが実装漏れ

**テスト結果**: DataOutputManagerTests 22件全てパス ✅

### 5.2 DataOutputManagerテストコードの修正

**問題**: Phase7設計書とテストコードの不一致（日時付きファイル名 vs 日時なし）
**現在の正仕様**: 日時なしファイル名形式 `xxx-xxx-x-xx_zzzz.json`

**修正内容**:
- `OutputToJson_ReadRandomData_OutputsCorrectJson`: ファイル名Regex修正
- `OutputToJson_MultipleWrites_CreatesMultipleFiles`: 異なるIPアドレスを使用
- `OutputToJson_FileNameFormat_IsCorrect`: ファイル名Regex修正
- `TC_INT_005_FileNameFormat_Integration_Success`: ファイル名Regex修正

**テスト結果**: 修正後全てパス ✅

---

## 6. 実行環境

- **.NET SDK**: 9.0.304
- **xUnit.net**: v2.8.2+699d445a1a
- **VSTest**: 17.14.1 (x64)
- **プラットフォーム**: .NET 9.0 (64-bit)
- **OS**: Windows
- **ビルド構成**: Debug
- **テスト実行モード**: オフライン動作確認（実機PLC接続なし、モック使用）

---

## 7. 検証完了事項

### 7.1 機能要件

✅ **ProcessedDeviceRequestInfo**: DeviceSpecificationsプロパティ追加（nullable）
✅ **ExecutionOrchestrator**: PlcConfigurationからDeviceSpecifications自動設定
✅ **PlcCommunicationManager**: ReadRandomレスポンス専用処理実装
✅ **後方互換性**: DeviceSpecifications=null時は既存処理を維持
✅ **エラー対策**: 「サポートされていないデータ型です:」エラー対策完了

### 7.2 テストカバレッジ

- **Phase8.5関連テスト**: 100% (5/5テスト合格)
- **統合テスト**: 100% (14/14テスト合格)
- **リグレッション**: ゼロ（既存テストへの影響なし）
- **成功率**: 100% (19/19テスト合格)

---

## 8. Phase12への引き継ぎ事項

### 8.1 暫定対策の位置づけ

**Phase8.5（暫定対策）**:
```csharp
// ProcessedDeviceRequestInfo（暫定的に拡張）
public List<DeviceSpecification>? DeviceSpecifications { get; set; }
```

**Phase12（根本対策）**:
```csharp
// 新設計: ReadRandomRequestInfo（専用クラス）
public class ReadRandomRequestInfo
{
    public List<DeviceSpecification> Devices { get; set; }
    public FrameType FrameType { get; set; }
    public DateTime RequestedAt { get; set; }
}
```

### 8.2 Phase12で実施すべきこと

⏳ **専用クラスの設計**
- ReadRandomRequestInfo（ReadRandom専用）
- ReadRequestInfo（旧Read(0x0401)用）
- コマンド種別に応じた適切な型チェック

⏳ **インターフェース分離**
- ReadRandom専用の処理メソッド
- Read専用の処理メソッド
- 型安全性の向上

⏳ **不要なプロパティの削除**
- `ProcessedDeviceRequestInfo`からDeviceType/StartAddress/Count削除
- または`ProcessedDeviceRequestInfo`自体を廃止

### 8.3 今回準備できたこと

✅ **データ構造の整理**:
- DeviceSpecificationsベースの処理フロー確立
- ReadRandomレスポンス処理のロジック確立

✅ **テストコードの資産化**:
- ReadRandom専用のテストケース作成
- Phase12で再利用可能なテストパターン

✅ **アーキテクチャの知見**:
- ReadRandom(0x0403)とRead(0x0401)の設計の違いを明確化
- 専用クラス分離の必要性を実証

---

## 9. 残課題とリスク

### 9.1 リスク

⚠️ **設計の後退感**:
- Phase3.5で一度削除したプロパティの復活
- 後方互換性維持のため複雑度が増加

⚠️ **Phase12への依存**:
- 暫定対策のため、Phase12での抜本的な設計見直しが必須
- Phase12実施が遅れると技術的負債として残存

### 9.2 軽減策

✅ **暫定対策であることをコメントで明示**:
```csharp
/// <summary>
/// ReadRandomデバイス指定一覧（Phase8.5暫定対策）
/// ReadRandom(0x0403)コマンドで複数デバイスを指定する場合に使用
/// nullの場合は既存のDeviceType/StartAddress/Countを使用（後方互換性）
/// </summary>
public List<DeviceSpecification>? DeviceSpecifications { get; set; }
```

✅ **Phase12での抜本的な設計見直しを文書化**:
- Phase8_5_恒久対策計画.mdに移行計画を記載
- 専用クラス設計案を明記

✅ **既存テストの互換性を完全維持**:
- リグレッションゼロ達成
- 段階的移行が可能

---

## 10. 次のステップ

### 10.1 Phase9実機テスト再実行

**推奨事項**:
- PLC: 172.30.40.15:8192
- プロトコル: UDP、4Eフレーム（Binary）
- 確認項目: 「サポートされていないデータ型です:」エラーが解消されていること

**実行手順**:
```bash
# ビルド
dotnet build -c Release

# 実機テスト実行
cd publish
.\andon.exe --config=実機設定.xlsx
```

**成功基準**:
- ✅ エラーが発生しない
- ✅ デバイス値が正しく取得できる
- ✅ ログに正常なデバイス値が出力される

### 10.2 Phase12根本対策の準備

**次回実施事項**:
- ReadRandomRequestInfo専用クラスの設計
- ProcessedDeviceRequestInfoの廃止または役割の再定義
- インターフェース分離によるコマンド種別の型安全性向上

---

## 総括

**実装完了率**: 100%
**テスト合格率**: 100% (19/19)
**実装方式**: TDD (Test-Driven Development) - Red→Green→Refactor厳守

**Phase8.5達成事項**:
- 実機エラー「サポートされていないデータ型です:」の暫定対策完了
- DeviceSpecificationsプロパティ再追加、ReadRandom専用処理実装
- 後方互換性を完全維持、リグレッションゼロ達成
- 全19テストケース合格、エラーゼロ
- DataOutputManager timestamp変数欠落の修正完了（Phase8.5外）

**Phase12への準備完了**:
- DeviceSpecificationsベースの処理フロー確立
- ReadRandom専用テストケース資産化
- 専用クラス分離の必要性を実証
