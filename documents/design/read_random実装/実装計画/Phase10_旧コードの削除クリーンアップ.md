# Phase10: 旧コードの削除・クリーンアップ

## ステータス
✅ **完了** - 選択肢C（レガシーテスト保持）方針でクリーンアップ完了（2025-12-03）

## 概要
Read(0x0401)専用コードの削除判断を行い、プロジェクト方針に基づいてクリーンアップします。

## 前提条件
- ✅ Phase1-9完了: 全機能実装・テスト・実機確認完了
- ✅ ReadRandom(0x0403)が実機環境で正常動作確認済み

---

## 現状分析（2025-12-03更新）

### ✅ 既に完了している削除項目

Phase1-9の実装過程で、Read(0x0401)専用コードは既に削除されています:

1. **SlmpFrameBuilder.BuildReadRequest()メソッド** → ✅ 削除済み
2. **Read(0x0401)用テストコード** → ✅ 削除済み
3. **旧形式設定ファイルサポート（ConvertOldFormatToNewFormat()）** → ✅ 存在せず
4. **旧形式データ出力（OutputReadDataToCsv()）** → ✅ 存在せず
5. **ProcessedResponseDataの旧形式プロパティ** → ✅ 削除済み
   - DeviceCode, StartDeviceNumber, Values, IsReadData等は存在しない
   - 現在はReadRandom専用プロパティのみ（ProcessedData, BitDeviceCount等）
6. **PlcCommunicationManager.cs** → ✅ 0x0401の痕跡完全削除済み

### ⚠️ 残存している0x0401の痕跡

実際の動作には影響しないが、以下の箇所にコメント・テストデータとして参照が残存:

1. **テストファイル内のコメント**
   - `Tests/Integration/PlcCommunicationManager_IntegrationTests_TC143_10.cs:110`
   - コメント: "コマンド: 0x0401 (2bytes, Little Endian) → 01 04"

2. **テストファイル内の0x0401フレーム例（16進数文字列）**
   - `Tests/Integration/Step3_6_IntegrationTests.cs` - 3箇所（60行目、265行目、708行目）
   - `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs` - 2箇所（57行目、163行目）
   - フレーム例: "54001234000000010401006400000090E8030000"

3. **バックアップファイル**
   - `Tests/Integration/Step3_6_IntegrationTests.cs.broken` - 3箇所
   - 削除候補

---

## 実装ステップ

### ステップ29: Read(0x0401)専用コードの削除判断

#### 判断結果: 選択肢C（レガシーテスト保持）を採用

**現状**:
- Read(0x0401)の実装コードは既に存在しない
- ReadRandom(0x0403)のみが実装されている
- ProcessedDeviceRequestInfoはPhase12で「テスト専用」として保持決定済み
- TC143_10テストはProcessedDeviceRequestInfoを使用し、現在も成功している

**結論**:
- 選択肢C（レガシーテスト保持）を採用
- TC143_10テスト等のRead(0x0401)関連テストを「歴史的資産」として保持
- コメントで「レガシーテスト」であることを明確化
- 残作業は「コメント明確化」と「バックアップファイル削除」のみ

#### ~~選択肢A: 完全削除（ReadRandom(0x0403)のみに統一）~~

**✅ 本番実装コードは既に完全削除済み**
- SlmpFrameBuilder.BuildReadRequest()メソッド削除済み
- PlcCommunicationManager内の0x0401処理削除済み

#### ~~選択肢B: 残す（互換性維持、設定で切り替え可能）~~

**注意**: 現状でこの選択肢を実施する場合、Read(0x0401)コードを新規に実装する必要があります。

#### ✅ 選択肢C: レガシーテスト保持（ProcessedDeviceRequestInfoと共に保持）

**採用理由**:
- Phase12でProcessedDeviceRequestInfoが「テスト専用」として保持決定済み
- TC143_10テストは現在も成功しており、通信層の低レベルテストとして価値がある
- テストコードはプロジェクトの歴史的資産として有用
- 動作に影響せず、混同リスクも低い

**実施内容**:
1. TC143_10テストファイルにXMLコメントで「Read(0x0401)レガシーテスト」と明記
2. 他のファイルの0x0401フレーム文字列にコメントで「レガシーフレーム」と明記
3. バックアップファイル `Step3_6_IntegrationTests.cs.broken` を削除
4. ProcessedDeviceRequestInfo使用箇所は保持（Phase12決定事項に準拠）

---

### ステップ30: レガシーテストのコメント明確化（軽微な作業）

#### 対象ファイルと作業内容（選択肢C方針）

1. **TC143_10テストファイル - XMLコメント追加**
   - `Tests/Integration/PlcCommunicationManager_IntegrationTests_TC143_10.cs`
   - ファイル冒頭のXMLコメントに「Read(0x0401)レガシーテスト」と明記
   - 110行目のコメント「コマンド: 0x0401」はそのまま保持（歴史的正確性のため）
   - 115行目のフレーム文字列もそのまま保持（テスト実行中で動作確認済み）

2. **Step3_6_IntegrationTests.cs - コメント追加**
   - `Tests/Integration/Step3_6_IntegrationTests.cs` - 3箇所（60行目、265行目、708行目）
   - フレーム文字列の行末に `// Read(0x0401)レガシーフレーム` コメント追加
   - フレーム自体は変更しない（歴史的資産として保持）

3. **PlcCommunicationManagerTests.cs - コメント追加**
   - `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs` - 2箇所（57行目、163行目）
   - フレーム文字列の行末に `// Read(0x0401)レガシーフレーム` コメント追加
   - フレーム自体は変更しない（歴史的資産として保持）

4. **バックアップファイル削除**
   - `Tests/Integration/Step3_6_IntegrationTests.cs.broken` を削除

#### 実施方針

**選択肢C: レガシーテスト保持**
- 0x0401フレームを0x0403に変更しない（歴史的正確性維持）
- コメントで「レガシーテスト」であることを明示
- ProcessedDeviceRequestInfo保持方針（Phase12）と整合
- テストは引き続き実行可能（現在も成功している）

#### 実装手順（選択肢C版）

```bash
# 1. TC143_10テストファイルにXMLコメント追加
# - ファイル冒頭に「Read(0x0401)レガシーテスト」と明記

# 2. Step3_6_IntegrationTests.cs - コメント追加（3箇所）
# - 60行目, 265行目, 708行目のフレーム文字列に行末コメント追加

# 3. PlcCommunicationManagerTests.cs - コメント追加（2箇所）
# - 57行目, 163行目のフレーム文字列に行末コメント追加

# 4. バックアップファイル削除
rm Tests/Integration/Step3_6_IntegrationTests.cs.broken

# 5. ビルド確認
dotnet build

# 6. テスト実行（レガシーテスト含む全テスト）
dotnet test

# 7. コミット
git add .
git commit -m "Phase10: Read(0x0401)レガシーテストのコメント明確化完了"
```

#### クリーンアップ後のテスト

```bash
# 全単体テスト実行
dotnet test --filter "FullyQualifiedName~Unit"

# 全統合テスト実行
dotnet test --filter "FullyQualifiedName~Integration"

# ビルド確認
dotnet build andon.sln
```

---

### ~~ステップ31: 選択肢B（切り替え機能実装）の実装~~

**注意**: このステップは現状では実施不要です。選択肢Aが既に完了しているため。

選択肢Bを実施する場合は、Read(0x0401)コードの新規実装が必要になります。以下は参考情報として残します。

<details>
<summary>参考: 選択肢B実装案（クリックで展開）</summary>

#### 実装内容

1. **appsettings.jsonに切り替えフラグを追加**

```json
{
  "PlcConnection": {
    "FrameVersion": "3E",
    "Timeout": 8000,
    "UseReadRandom": true,  // ← 追加

    // ReadRandom用設定
    "Devices": [
      {
        "DeviceType": "D",
        "DeviceNumber": 100
      }
    ],

    // Read用設定（UseReadRandom=falseの場合）
    "StartDevice": "D100",
    "DeviceCount": 10
  }
}
```

2. **ConfigurationLoaderの拡張**

```csharp
public class ConfigurationLoader
{
    public TargetDeviceConfig LoadPlcConnectionConfig()
    {
        var config = new TargetDeviceConfig();

        // 基本設定
        config.FrameVersion = _configuration["PlcConnection:FrameVersion"] ?? "3E";
        config.Timeout = int.Parse(_configuration["PlcConnection:Timeout"] ?? "8000");
        config.UseReadRandom = bool.Parse(_configuration["PlcConnection:UseReadRandom"] ?? "true");

        if (config.UseReadRandom)
        {
            // ReadRandom用設定読み込み
            config.Devices = LoadDevicesList();
        }
        else
        {
            // Read用設定読み込み
            config.StartDevice = _configuration["PlcConnection:StartDevice"];
            config.DeviceCount = int.Parse(_configuration["PlcConnection:DeviceCount"] ?? "0");
        }

        return config;
    }
}
```

3. **PlcCommunicationManagerの拡張**

```csharp
public class PlcCommunicationManager
{
    private readonly TargetDeviceConfig _config;

    public async Task<ProcessedResponseData> ReadDevicesAsync()
    {
        if (_config.UseReadRandom)
        {
            // ReadRandom(0x0403)実行
            return await ReadDevicesWithReadRandomAsync();
        }
        else
        {
            // Read(0x0401)実行（旧方式）
            return await ReadDevicesWithReadAsync();
        }
    }

    private async Task<ProcessedResponseData> ReadDevicesWithReadRandomAsync()
    {
        var sendFrame = _frameManager.BuildReadRandomFrameFromConfig(_config);
        await SendFrameAsync(sendFrame);
        var responseFrame = await ReceiveResponseAsync();
        var parsedData = SlmpDataParser.ParseReadRandomResponse(responseFrame, _config.Devices);
        return new ProcessedResponseData(parsedData);
    }

    private async Task<ProcessedResponseData> ReadDevicesWithReadAsync()
    {
        var sendFrame = _frameManager.BuildReadRequestFromConfig(_config);
        await SendFrameAsync(sendFrame);
        var responseFrame = await ReceiveResponseAsync();
        var parsedData = SlmpDataParser.ParseReadResponse(responseFrame);
        return new ProcessedResponseData(parsedData);
    }
}
```

4. **TargetDeviceConfigの拡張**

```csharp
public class TargetDeviceConfig
{
    public string FrameVersion { get; set; } = "3E";
    public int Timeout { get; set; } = 8000;

    /// <summary>
    /// ReadRandom(0x0403)を使用するか（デフォルト: true）
    /// </summary>
    public bool UseReadRandom { get; set; } = true;

    // ReadRandom用設定
    public List<DeviceEntry> Devices { get; set; } = new();

    // Read用設定（UseReadRandom=falseの場合）
    public string? StartDevice { get; set; }
    public int? DeviceCount { get; set; }
}
```

</details>

---

## 完了条件

### 選択肢C（レガシーテスト保持）の完了条件
- ✅ Read(0x0401)本番実装コード完全削除（Phase1-9で実施済み）
- ✅ PlcCommunicationManager.cs内の0x0401処理削除済み
- ✅ ReadRandom(0x0403)のみが本番実装として稼働
- ✅ ProcessedDeviceRequestInfo「テスト専用」として保持（Phase12決定事項）
- ✅ レガシーテストのコメント明確化（2025-12-03完了）
  - [x] TC143_10テストファイルにXMLコメント追加
  - [x] Step3_6_IntegrationTests.cs - 行末コメント追加（3箇所）
  - [x] PlcCommunicationManagerTests.cs - 行末コメント追加（2箇所）
  - [x] バックアップファイル削除（Step3_6_IntegrationTests.cs.broken）
- ✅ Phase10関連テスト全てパス（16/16合格）
- ✅ ビルド成功（0 errors, 0 warnings）

### ~~選択肢A（完全削除）~~
本番実装コードは既に削除済み。テストコードは選択肢Cにより保持。

### ~~選択肢B（切り替え機能）~~
実施不要（選択肢Cを採用）

## 次フェーズへの依存関係
- Phase11（ドキュメント更新）で、削除・変更内容をドキュメントに反映します

## リスク管理
| リスク | 影響 | 対策 |
|--------|------|------|
| **コメント・テスト更新時のミス** | 低 | ・動作に影響しない箇所のみ<br>・テスト実行で確認 |
| ~~**誤削除**~~ | なし | 削除対象コードは既に存在しない |
| ~~**互換性喪失（選択肢A）**~~ | なし | 既に実施済み |

---

**作成日**: 2025-11-18
**更新日**: 2025-12-03（選択肢C方針に修正、Phase12との整合性確保）
**元ドキュメント**: read_to_readrandom_migration_plan.md
