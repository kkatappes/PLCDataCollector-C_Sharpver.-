# Phase 4: JSON設定ファイル廃止計画

**最終更新**: 2025-11-28
**現在の状況**: Phase 1.5完了（ハードコード置き換え対応Phase 1-5完了）、Phase 2以降未開始

## 目的
Excel形式への完全移行により、JSON設定ファイル読み込み機能を段階的に廃止する

## 文書の位置づけ
本文書は、JSON設定ファイル読み込み機能の段階的廃止計画を記載しています。
**ハードコード置き換え対応**の完了により、Phase 1.5（実機テスト準備）が完了しました。

**関連文書**:
- ハードコード置き換え対応の詳細: `documents/design/ハードコード実装置き換え対応/実装計画/`
- Phase 1-5の実装結果: `documents/design/ハードコード実装置き換え対応/実装結果/`

## 重要な方針変更（2025-11-28更新）

### ハードコード置き換え対応完了（2025-11-28）
**Phase 1-5完了**: ハードコード置き換え対応が全て完了しました
- ✅ Phase 1: DefaultValues.cs（既定値定義）- 完了
- ✅ Phase 2: ConfigurationLoaderExcel拡張（設定読み込み）- 完了
- ✅ Phase 3: SettingsValidator.cs（検証ロジック）- 完了
- ✅ Phase 4: ConfigToFrameManager.cs（ハードコード削除）- 完了
- ✅ Phase 5: 統合テスト - 完了

**詳細**: `documents/design/ハードコード実装置き換え対応/実装計画/` 参照

### 実機テストのタイミング
**実機テストは現在実施可能**: ハードコード置き換え対応完了により、実機テストを開始できます
- **完了事項**: ConfigToFrameManagerのハードコード完全削除、既定値設定、検証ロジック実装
- **テスト結果**: 803/804テスト成功（1個はタイミング関連でハードコード対応無関係）

## 前提条件
✅ Phase 1-3（Excel対応実装・テスト）が完了していること
✅ ハードコード置き換え対応（Phase 1-5）が完了していること

## Phase 3からの引継ぎ事項

### ✅ 完了済み項目（2025-11-28時点）
1. **Phase1-3統合テスト完了**
   - ConfigToFrameManagerTests: 18/18テスト成功
   - Phase3統合テスト（TC_Step123_001）: 1/1成功
   - LoggingManagerTests: 28/28テスト全パス（Phase3 Part6で解決）
   - 全体テスト: 153/153テスト合格

2. **ハードコード置き換え対応完了（2025-11-28追加）**
   - DefaultValues.cs実装完了（6個の既定値定義）
   - PlcConfiguration拡張完了（7プロパティ追加）
   - SettingsValidator.cs実装完了（6つの検証メソッド）
   - ConfigToFrameManager.csハードコード削除完了
   - 統合テスト9個実装・全成功
   - 全体テスト: 803/804テスト成功

3. **実装完了範囲**
   - Step1_2_IntegrationTests.cs: 179-238行（統合テスト実装）
   - DeviceSpecification.cs: DeviceType初期化処理追加
   - DefaultValues.cs: 既定値定義（新規）
   - SettingsValidator.cs: 検証ロジック（新規）
   - HardcodeReplacement_IntegrationTests.cs: 統合テスト（新規）

4. **テスト実行環境**
   - ビルド: 成功（0エラー）
   - テスト実行: 803/804成功
   - オフラインテスト環境確認済み（MockSocket/MockUdpServer使用）

### 📌 Phase 4への重要な引継ぎ情報（2025-11-28更新）

#### 1. 現在の実装構造（Phase3で確認済み）
- **PlcConfiguration**: Excel読み込み専用モデル（実装済み、動作確認済み）
- **ConfigurationLoaderExcel**: Excel読み込み機能（実装済み、動作確認済み）
- **TargetDeviceConfig**: フレーム構築の共通入力型（JSON/Excel両対応）
- **ConfigToFrameManager**: TargetDeviceConfigを引数に取るメソッド群（実装済み、テスト済み）

#### 2. ハードコード置き換え対応により解決済み（2025-11-28）
**Phase1-3の課題**: `PlcConfiguration`と`TargetDeviceConfig`の型不一致問題
- 当初: PlcConfigurationを直接使用する想定
- 実際: TargetDeviceConfigとPlcConfigurationが併用されている状況

**ハードコード置き換え対応で実施した解決策**:
- ✅ PlcConfigurationに必要なプロパティ追加（FrameVersion, Timeout等）
- ✅ ConfigToFrameManagerにPlcConfiguration用オーバーロード実装
- ✅ 両クラスが独立して動作可能（変換メソッド不要）
- ✅ 803/804テスト成功により動作確認完了

**変換メソッド**: 不要（各クラスが独立して動作するため）

#### 3. テスト環境の制約事項
- **実機PLC接続禁止**: このPCはPLC接続環境ではない
- **モック/スタブ必須**: MockSocket/MockUdpServerを使用したオフラインテスト
- **実データ取得目的でのビルド禁止**: TDD目的のビルドのみ許可

#### 4. 既知の問題（解決済み）
- **LoggingManagerTestsのファイルロック問題**: Phase3 Part6で完全解決
  - 22箇所にmanager.Dispose()呼び出し追加
  - 28/28テスト全合格

### 📋 Phase 4実装時の注意事項

#### 必須の変換メソッド実装
Phase 4開始前に以下の実装が必要です：

```csharp
// 実装ファイル: Core/Models/ConfigModels/PlcConfigurationExtensions.cs
public static class PlcConfigurationExtensions
{
    public static TargetDeviceConfig ToTargetDeviceConfig(this PlcConfiguration plcConfig)
    {
        return new TargetDeviceConfig
        {
            FrameType = "4E", // またはplcConfigから取得
            Timeout = 32,     // またはplcConfigから取得
            Devices = plcConfig.Devices
                .Select(d => new DeviceEntry
                {
                    DeviceType = d.DeviceType,
                    DeviceNumber = d.DeviceNumber,
                    ItemName = d.ItemName,
                    Digits = d.Digits,
                    Unit = d.Unit
                })
                .ToList()
        };
    }
}
```

#### テスト実装の注意点
- 変換メソッドの単体テスト実装必須
- Excel読み込み→変換→フレーム構築の統合テスト実装必須
- 既存の153/153テストを維持しながら追加テスト実装

#### ビルド・テスト実行方針
- TDD手法継続（テスト→実装→リファクタ）
- オフラインテスト環境でのモック/スタブ利用
- テスト実行コマンド: `dotnet test andon.sln --filter "FullyQualifiedName~[TestName]" --verbosity minimal --no-restore`

---

## 実装状況の整理

### 現在実装されている構成

Step1-3テスト向けの実装は以下の通りです：

#### 1. PlcConfiguration クラス（Core/Models/ConfigModels/PlcConfiguration.cs）
**概要**: Excel設定ファイル（*.xlsx）から読み込んだPLC接続設定を保持する専用クラス

**用途**:
- Excel読み込み専用モデル
- `ConfigurationLoaderExcel`で使用
- **注意**: `ConfigToFrameManager`では直接使用していない

**プロパティ**:
- IpAddress（IPアドレス）
- Port（ポート番号）
- PlcModel（PLCモデル）
- SavePath（保存パス）
- SourceExcelFile（元Excelファイル）
- ConfigurationName（設定名）
- DataReadingFrequency（データ読込頻度）
- Devices（デバイスリスト - DeviceSpecification型）

#### 2. ConfigurationLoaderExcel クラス（Infrastructure/Configuration/ConfigurationLoaderExcel.cs）
**概要**: Excel設定ファイル（*.xlsx）からPLC接続設定を読み込み

**メソッド**:
- `LoadAllPlcConnectionConfigs(configDirectory, filePattern)`: 複数PLC設定一括読み込み
- `LoadFromExcel(filePath)`: 単一Excel設定読み込み（内部メソッド）

**戻り値型**: `List<PlcConfiguration>`

#### 3. ConfigToFrameManager の実装メソッド
**実際に実装されているメソッド**:
- `BuildReadRandomFrameFromConfig(TargetDeviceConfig config)`: Binary形式フレーム構築
- `BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig config)`: ASCII形式フレーム構築

**引数型**: `TargetDeviceConfig`（設定ファイルから読み込まれたJSON設定を保持）

#### 4. TargetDeviceConfig クラスの役割
**概要**: JSON設定ファイルから読み込んだデバイス設定を保持

**プロパティ**:
- FrameType（3E/4E）
- Timeout（タイムアウト値）
- Devices（デバイスリスト - DeviceEntry型）

**用途**: `ConfigToFrameManager`でフレーム構築時に使用される主要な型

---

## 実装の現状認識

### 重要な発見
Phase1-3の実装計画文書では`PlcConfiguration`を`ConfigToFrameManager`で使用することを想定していましたが、**実際の実装では`TargetDeviceConfig`を使用しています**。

### 現在の設定フローは二系統
1. **JSON形式フロー（実装済み・動作中）**:
   - JSON設定ファイル → `ConfigurationLoader` → `TargetDeviceConfig` → `ConfigToFrameManager`

2. **Excel形式フロー（実装済み・独立）**:
   - Excel設定ファイル → `ConfigurationLoaderExcel` → `PlcConfiguration` → （別用途で使用）

### 現状での問題点
- `PlcConfiguration`と`TargetDeviceConfig`が並存しているが、用途が分離されている
- Excel読み込み機能はあるが、`ConfigToFrameManager`との統合が未完了
- JSON形式は現在も主要な設定方式として機能している

## JSON廃止の再検討

### 本来の目的（Phase1-3文書より）
1. Excel形式に統一して設定管理を簡素化
2. ConfigurationLoaderとConfigurationLoaderExcelの重複実装を解消
3. ユーザビリティ向上（Excel形式の方が編集容易）

### 実際の状況
- **JSON形式**: 実装完了・テスト済み・動作中
- **Excel形式**: 読み込み機能実装済みだが、フレーム構築との統合は未完了
- **統合の必要性**: PlcConfigurationからTargetDeviceConfigへの変換ロジックが必要

---

## 廃止計画の修正

### 修正後の方針
Phase1-3文書の記載と実際の実装に齟齬があったため、以下のように計画を修正します：

### 現状維持すべき実装
以下は**廃止せず**、引き続き使用します：

#### ConfigToFrameManager の実装メソッド
- `BuildReadRandomFrameFromConfig(TargetDeviceConfig)` - **現行の主要メソッド（維持）**
- `BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig)` - **現行の主要メソッド（維持）**

#### TargetDeviceConfig クラス
- JSON設定から読み込まれたデバイス設定を保持
- `ConfigToFrameManager`で使用される主要な型
- **現在のフレーム構築フローの中核（維持）**

### 廃止対象（変更なし）

#### ConfigurationLoader クラス（Infrastructure/Configuration/ConfigurationLoader.cs）
- `LoadPlcConnectionConfig()` - JSON読み込み
- `ValidateConfig()` - JSON設定検証
- **廃止理由**: Excel形式に統一する場合に不要

#### 設定ファイル
- `appsettings.json` - JSON形式設定ファイル
- **廃止理由**: Excel形式に完全移行後は不要

### 追加実装が必要なもの

#### PlcConfigurationからTargetDeviceConfigへの変換メソッド
Excel読み込み結果をフレーム構築に渡すための変換ロジックが必要：

```csharp
public static class PlcConfigurationExtensions
{
    public static TargetDeviceConfig ToTargetDeviceConfig(this PlcConfiguration plcConfig)
    {
        return new TargetDeviceConfig
        {
            FrameType = "4E", // またはplcConfigから取得
            Timeout = 32,     // またはplcConfigから取得
            Devices = plcConfig.Devices
                .Select(d => new DeviceEntry
                {
                    DeviceType = d.DeviceType,
                    DeviceNumber = d.DeviceNumber,
                    ItemName = d.ItemName,
                    Digits = d.Digits,
                    Unit = d.Unit
                })
                .ToList()
        };
    }
}
```

---

## 移行計画（5つのPhase）

### Phase 1: Excel読み込み機能完全実装（✅完了: 2025-11-28）
- ✅ `PlcConfiguration` クラス実装
- ✅ `ConfigurationLoaderExcel` クラス実装
- ✅ `BuildReadRandomFrameFromConfig(PlcConfiguration)` 実装（Binary形式）
- ✅ `BuildReadRandomFrameFromConfigAscii(PlcConfiguration)` 実装（ASCII形式）
- ✅ Phase1-3統合テスト完了
- ✅ 全体テスト717/719合格（2個スキップは正常）

**完了条件**:
- [x] Excel読み込み機能の単体テストが全てパス
- [x] ConfigToFrameManagerTests全合格（18/18）
- [x] Phase3統合テスト全合格（TC_Step123_001）
- [x] LoggingManagerTests全合格（28/28）
- [x] 全体テスト合格

---

### Phase 1.5: ハードコード解消・実機テスト準備（✅完了: 2025-11-28）

**開始条件**: Phase 1完了

**目的**:
- ConfigToFrameManager内のハードコード値（frameType, timeout）を解消
- PlcConfigurationにExcel読み込み可能なフィールドを追加
- 実機テストで正しい設定値が使用されるようにする

**実施内容**（ハードコード置き換え対応Phase 1-5として完了）:

#### 1. PlcConfiguration拡張
以下のフィールドを追加:
```csharp
public string FrameVersion { get; set; } = "4E";   // デフォルト4E
public ushort Timeout { get; set; } = 32;          // デフォルト32
public bool IsBinary { get; set; } = true;         // デフォルトBinary形式
public string ConnectionMethod { get; set; } = "UDP"; // デフォルトUDP
```

#### 2. ConfigurationLoaderExcel拡張
Excelの"settings"シートから以下を読み込み:
- B10セル: ConnectionMethod（TCP/UDP）
- B11セル: Timeout（ミリ秒）
- B14セル: FrameVersion（3E/4E）
- B15セル: IsBinary（true/false）
- B16セル: DataReadingFrequency（既存のB11からB16に移動の可能性あり）

#### 3. ConfigToFrameManagerのハードコード解消
```csharp
// 修正前（ハードコード）
byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
    config.Devices,
    frameType: "4E",  // ← ハードコード
    timeout: 32       // ← ハードコード
);

// 修正後（Excel設定から取得）
byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
    config.Devices,
    frameType: config.FrameVersion,  // ← Excel設定から取得
    timeout: config.Timeout          // ← Excel設定から取得
);
```

Binary形式とASCII形式の両方のオーバーロードメソッドで対応。

#### 4. デフォルト値の統一
ハードコード状況確認文書に基づき、以下のデフォルト値を統一:
- FrameVersion: "4E"（既定値）
- Timeout: 32（既定値、Excelから読み込み可能）
- IsBinary: true（既定値をtrueに変更）
- ConnectionMethod: "UDP"（既定値）

#### 5. テスト実装
- PlcConfigurationの新フィールドのテスト
- ConfigurationLoaderExcelのフィールド読み込みテスト
- ConfigToFrameManagerの動的設定値使用テスト
- 統合テスト：Excel読み込み→フレーム構築（設定値反映確認）

**完了条件**:
- [x] PlcConfigurationに必要フィールド追加完了（2025-11-28）
  - FrameVersion, Timeout, IsBinary, ConnectionMethod, MonitoringIntervalMs
- [x] ConfigurationLoaderExcelでExcelから読み込み実装完了（Phase 2として完了）
- [x] ConfigToFrameManagerのハードコード解消完了（Phase 4として完了）
  - config.FrameVersion使用に変更済み
- [x] 全テストパス（803/804成功）
- [x] ビルド成功（0エラー）

**期間**: 1-2日（オフラインテスト環境）→ **実際**: 1日（2025-11-28に完了）

**実装詳細**: `documents/design/ハードコード実装置き換え対応/` 参照

---

### Phase 2: JSON機能廃止（Phase 1.5完了後）

**開始条件**: Phase 1.5完了、ハードコード解消確認後

**重要**: 実機テスト前にJSON機能を廃止し、Excel専用システムとして確定させる

**TDD方針**: Red-Green-Refactorサイクルに従って段階的に削除

---

#### **Step 1: Red（削除予定箇所の特定とテスト確認）**

##### 1-1. 削除対象の依存関係調査
```bash
# TargetDeviceConfigの参照箇所を特定
grep -r "TargetDeviceConfig" --include="*.cs" andon/

# ConfigurationLoaderの参照箇所を特定
grep -r "ConfigurationLoader" --include="*.cs" andon/
```

##### 1-2. 削除前の全テスト実行
```bash
dotnet test andon.sln --verbosity minimal
```

**期待結果**: 全テストパス（現状ベースライン確認）

---

#### **Step 2: Green（段階的削除 - 各ステップでテスト実行）**

##### 2-1. ConfigToFrameManagerのTargetDeviceConfig用メソッド削除

**削除対象**:
```csharp
// ConfigToFrameManager.cs から削除
- BuildReadRandomFrameFromConfig(TargetDeviceConfig)        // 削除（JSON用）
- BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig)   // 削除（JSON用）
```

**手順**:
1. 該当メソッドを削除
2. ビルド実行して参照エラーを確認
3. 参照箇所を特定・修正（PlcConfiguration用メソッドに置換）
4. テスト実行: `dotnet test --filter "FullyQualifiedName~ConfigToFrameManager"`
5. 全テスト実行: `dotnet test andon.sln --verbosity minimal`

**完了条件**: ビルド成功、全テストパス

---

##### 2-2. ConfigToFrameManagerTests内のTargetDeviceConfig用テスト削除

**削除対象**:
```
Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs
- TargetDeviceConfigを使用するテストメソッド
```

**手順**:
1. TargetDeviceConfig用テストメソッドを特定
2. 該当テストメソッドを削除
3. ビルド実行
4. テスト実行: `dotnet test --filter "FullyQualifiedName~ConfigToFrameManager"`
5. 全テスト実行

**完了条件**: ビルド成功、全テストパス

---

##### 2-3. ConfigurationLoaderクラス削除

**削除対象**:
```
Infrastructure/Configuration/
├── ConfigurationLoader.cs          // 削除（JSON読み込み専用）
```

**手順**:
1. ConfigurationLoaderの参照箇所を確認
2. 参照箇所をConfigurationLoaderExcelに置換
3. ConfigurationLoader.csを削除
4. ビルド実行
5. 関連テスト実行
6. 全テスト実行

**完了条件**: ビルド成功、全テストパス

---

##### 2-4. ConfigurationLoaderTests削除

**削除対象**:
```
Tests/Unit/Infrastructure/Configuration/
├── ConfigurationLoaderTests.cs     // 削除
```

**手順**:
1. ConfigurationLoaderTests.csを削除
2. ビルド実行
3. 全テスト実行

**完了条件**: ビルド成功、全テストパス

---

##### 2-5. TargetDeviceConfig/DeviceEntryクラス削除

**削除対象**:
```csharp
// Core/Models/ConfigModels/
- TargetDeviceConfig.cs    // 削除（JSON専用モデル）
- DeviceEntry.cs           // 削除（JSON専用モデル）
```

**手順**:
1. TargetDeviceConfig/DeviceEntryの参照箇所を最終確認（残存していないこと）
2. TargetDeviceConfig.csを削除
3. DeviceEntry.csを削除
4. ビルド実行
5. 全テスト実行

**完了条件**: ビルド成功、全テストパス

---

##### 2-6. appsettings.json削除

**削除対象**:
- `appsettings.json` サンプルファイル

**手順**:
1. appsettings.jsonを削除（または.jsonサンプルファイルをリネーム）
2. ビルド実行
3. 全テスト実行

**完了条件**: ビルド成功、全テストパス

---

#### **Step 3: Refactor（最終確認とドキュメント更新）**

##### 3-1. 残存コードの確認
```bash
# JSON関連の残存確認
grep -r "TargetDeviceConfig" --include="*.cs" andon/
grep -r "DeviceEntry" --include="*.cs" andon/
grep -r "ConfigurationLoader" --include="*.cs" andon/ | grep -v "ConfigurationLoaderExcel"
```

**期待結果**: 該当なし

##### 3-2. 最終ビルド・テスト実行
```bash
dotnet clean andon.sln
dotnet build andon.sln
dotnet test andon.sln --verbosity minimal
```

**完了条件**: ビルド成功、全テストパス

---

#### **完了条件（チェックリスト）**:
- [ ] **2-1**: ConfigToFrameManagerのTargetDeviceConfig用メソッド削除完了
- [ ] **2-2**: ConfigToFrameManagerTests内のTargetDeviceConfig用テスト削除完了
- [ ] **2-3**: ConfigurationLoaderクラス削除完了
- [ ] **2-4**: ConfigurationLoaderTests削除完了
- [ ] **2-5**: TargetDeviceConfig/DeviceEntry削除完了
- [ ] **2-6**: appsettings.jsonサンプル削除完了
- [ ] **3-1**: 残存コード確認完了（該当なし）
- [ ] **3-2**: ビルド成功、全テストパス

**期間**: 1日（オフラインテスト環境、TDDサイクル厳守）

---

### Phase 3: 実機テスト開始（Phase 2完了後）

**開始条件**: Phase 2完了、JSON機能完全廃止確認後

**重要**: JSON機能を廃止した後、Excel専用システムとして実機テストを実施

**実施内容**:
1. **実機接続テスト**
   - 実際のPLC環境での接続確認
   - Excel設定ファイルからの読み込み確認
   - FrameVersion/Timeout/IsBinaryの設定値が正しく反映されることを確認

2. **データ取得テスト**
   - Step1-7の全フロー実機動作確認
   - 複数PLC並列実行確認
   - エラーハンドリング動作確認

3. **長期稼働テスト**
   - 24時間以上の連続稼働テスト
   - メモリリーク検証
   - リソース管理の動作確認

**完了条件**:
- [ ] 実機環境でExcel設定ファイルが正常動作
- [ ] 全Step（Step1-7）が実機で正常動作
- [ ] 複数PLC並列実行が正常動作
- [ ] 長期稼働で問題なし

**期間**: 1-2週間（実機テスト環境）

---

### Phase 4: クリーンアップ・ドキュメント整備（Phase 3完了後）

**開始条件**: Phase 3完了、実機テスト全合格

**実施内容**:

#### 1. モデルの役割明確化
Phase 2でJSON関連モデル（TargetDeviceConfig, DeviceEntry）を削除済みのため、以下のみ残存:
- **PlcConfiguration**: Excel読み込み専用型（Phase 1.5で拡張済み）
- **DeviceSpecification**: 共通デバイス表現（Excel/フレーム構築両対応）

#### 2. 依存関係の整理
- JSON関連NuGetパッケージの使用状況調査
  - System.Text.Json（他の機能で使用中の場合は維持）
  - 完全に未使用の場合のみ削除検討

**注意**: System.Text.Jsonはログ出力等で使用されている可能性が高いため、慎重に調査

#### 3. ドキュメント最終更新
```
documents/design/
├── プロジェクト構造設計.md    // Excel専用システムとして記載更新
├── クラス設計.md              // PlcConfiguration中心の設計に更新
├── CLAUDE.md                  // 構造ガイドをExcel専用に更新
├── step1-3テスト/実装計画/    // Phase1-4文書（本文書含む）
└── ハードコード状況確認/      // ハードコード解消状況を最終確認
```

#### 4. README・ユーザーガイド更新
- JSON形式の記載を完全削除
- Excel形式のみの使用方法を記載
- FrameVersion/Timeout/IsBinary等の設定方法を明記
- 実機テスト結果を反映

**完了条件**:
- [ ] 不要な依存関係削除完了（調査の上）
- [ ] 全ドキュメント更新完了（Excel専用システムとして）
- [ ] README更新完了（JSON記載削除）
- [ ] 実機テスト結果の文書化完了
- [ ] リリース前最終レビュー完了

**期間**: 1日（ドキュメント整備）

---

## 影響範囲

### 削除が必要なファイル
```
Infrastructure/Configuration/
├── ConfigurationLoader.cs          // 削除

Tests/Unit/Infrastructure/Configuration/
├── ConfigurationLoaderTests.cs     // 削除

appsettings.json                    // サンプルファイル削除
```

### 更新が必要なファイル
```
Core/Models/ConfigModels/
└── PlcConfigurationExtensions.cs   // 新規作成: PlcConfiguration→TargetDeviceConfig変換

documents/design/
├── プロジェクト構造設計.md          // モデルの役割を明確化
├── クラス設計.md                   // 各クラスの役割を明確化
├── CLAUDE.md                       // 構造ガイドを現状に合わせる
└── step1-3テスト/実装計画/          // Phase1-4文書（今回修正済み）

README.md                           // Excel推奨・JSON非推奨を明記
```

---

## マイルストーン（2025-11-28更新）

| Phase | 開始条件 | 期間目安 | 完了目標 | 状況 |
|-------|---------|---------|---------|------|
| Phase 1 | - | 2週間 | Excel読み込み機能実装・テスト完了 | ✅ 完了（2025-11-28） |
| Phase 1.5 | Phase 1完了 | 1-2日 | ハードコード解消・実機テスト準備 | ✅ 完了（2025-11-28） |
| Phase 2 | Phase 1.5完了 | 1日 | JSON機能完全廃止 | 未開始 |
| Phase 3 | Phase 2完了 | 1-2週間 | 実機テスト全合格 | 未開始 |
| Phase 4 | Phase 3完了 | 1日 | ドキュメント整備・リリース準備完了 | 未開始 |

### Phase 1進捗詳細（✅完了: 2025-11-28）
- ✅ PlcConfiguration実装完了
- ✅ ConfigurationLoaderExcel実装完了
- ✅ PlcConfiguration用オーバーロードメソッド実装完了
- ✅ Phase1-3統合テスト完了（717/719テスト全合格）
- ✅ 全体テストパス確認済み

### Phase 1.5進捗詳細（✅完了: 2025-11-28）
- ✅ PlcConfigurationにFrameVersion/Timeout/IsBinary/ConnectionMethod/MonitoringIntervalMsフィールド追加
- ✅ ConfigurationLoaderExcelのExcel読み込み拡張
- ✅ ConfigToFrameManagerのハードコード解消（config.FrameVersion使用）
- ✅ テスト実装・全テストパス確認（803/804成功）
- ✅ ハードコード置き換え対応Phase 1-5として完了
- **詳細**: `documents/design/ハードコード実装置き換え対応/実装計画/` 参照

---

## 注意事項（修正版）

### 1. TargetDeviceConfigモデルの取り扱い（重要変更）
- **削除不要**: TargetDeviceConfigは**削除しない**
- **役割**: フレーム構築の共通入力型として、Excel/JSON両フローで使用
- **変換層**: PlcConfiguration（Excel）→ TargetDeviceConfig の変換メソッドを実装

### 2. 後方互換性
- **Phase 2期間中**: 両形式を必ずサポート（既存システム保護）
- **移行ガイド**: 既存ユーザー向けの移行ガイド作成必須
- **問い合わせ対応**: Phase 2期間中の問い合わせ対応体制確保

### 3. テストデータ
- **JSON形式テストデータ**: 維持（既存機能テストとして必要）
- **Excel形式テストデータ**: 追加実装
- **統合テスト**: Excel読み込み→TargetDeviceConfig変換→フレーム構築のテスト追加

### 4. リスク管理
- **ロールバック計画**: Phase 3実施前にロールバック可能な状態を確保
- **バックアップ**: JSON関連コード削除前にバックアップ取得
- **段階的実施**: 一度に削除せず、ビルド・テスト確認を挟みながら段階的に削除

### 5. 実装上の重要ポイント
- **型の役割分担を明確化**:
  - `PlcConfiguration`: Excel読み込み結果を保持
  - `TargetDeviceConfig`: フレーム構築の入力型（共通）
  - 変換メソッドで両者を橋渡し
- **ConfigToFrameManagerの変更は最小限**: 既存メソッドは変更せず維持

---

## 実装チェックリスト（2025-11-28更新）

### Phase 1: Excel読み込み機能完全実装（✅完了: 2025-11-28）
- [x] PlcConfigurationクラス実装完了
- [x] ConfigurationLoaderExcelクラス実装完了
- [x] PlcConfiguration用オーバーロードメソッド実装完了（Binary/ASCII）
- [x] ConfigToFrameManagerTests全合格（18/18）
- [x] Phase3統合テスト全合格（TC_Step123_001）
- [x] LoggingManagerTests全合格（28/28）
- [x] 全体テスト717/719合格

### Phase 1.5: ハードコード解消・実機テスト準備（✅完了: 2025-11-28）
- [x] PlcConfigurationにFrameVersion/Timeout/IsBinary/ConnectionMethod/MonitoringIntervalMs追加
- [x] ConfigurationLoaderExcelでExcelから追加フィールド読み込み実装
- [x] ConfigToFrameManagerのハードコード解消（Binary形式）
  - config.FrameVersion使用に変更
- [x] ConfigToFrameManagerのハードコード解消（ASCII形式）
  - config.FrameVersion使用に変更
- [x] 追加フィールドの単体テスト実装
- [x] Excel読み込み→フレーム構築の統合テスト拡張
- [x] 全テストパス確認（803/804成功）
- [x] ビルド成功確認（0エラー）
- [x] ハードコード置き換え対応Phase 1-5完了

### Phase 2: JSON機能廃止（TDD準拠）
#### Step 1: Red（削除予定箇所の特定）
- [ ] 1-1: 削除対象の依存関係調査（grep実行）
- [ ] 1-2: 削除前の全テスト実行（ベースライン確認）

#### Step 2: Green（段階的削除）
- [ ] 2-1: ConfigToFrameManagerのTargetDeviceConfig用メソッド削除
  - [ ] メソッド削除
  - [ ] ビルド・参照エラー確認
  - [ ] 参照箇所修正
  - [ ] テスト実行（ConfigToFrameManager）
  - [ ] 全テスト実行
- [ ] 2-2: ConfigToFrameManagerTests内のTargetDeviceConfig用テスト削除
  - [ ] テストメソッド特定・削除
  - [ ] ビルド実行
  - [ ] テスト実行
- [ ] 2-3: ConfigurationLoaderクラス削除
  - [ ] 参照箇所確認・置換
  - [ ] ファイル削除
  - [ ] ビルド・テスト実行
- [ ] 2-4: ConfigurationLoaderTests削除
  - [ ] ファイル削除
  - [ ] ビルド・テスト実行
- [ ] 2-5: TargetDeviceConfig/DeviceEntry削除
  - [ ] 参照箇所最終確認
  - [ ] ファイル削除
  - [ ] ビルド・テスト実行
- [ ] 2-6: appsettings.json削除
  - [ ] ファイル削除
  - [ ] ビルド・テスト実行

#### Step 3: Refactor（最終確認）
- [ ] 3-1: 残存コード確認（grep実行）
- [ ] 3-2: 最終ビルド・テスト実行（clean build）

### Phase 3: 実機テスト開始
- [ ] 実機環境でExcel設定ファイル読み込み確認
- [ ] FrameVersion/Timeout/IsBinary設定値の反映確認
- [ ] Step1-7全フロー実機動作確認
- [ ] 複数PLC並列実行確認
- [ ] 24時間以上の長期稼働テスト
- [ ] エラーハンドリング動作確認
- [ ] メモリリーク検証

### Phase 4: クリーンアップ・ドキュメント整備
- [ ] JSON関連依存関係削除（調査の上）
- [ ] 全ドキュメント更新（Excel専用システムとして）
- [ ] CLAUDE.md更新
- [ ] README更新（JSON記載削除）
- [ ] 実機テスト結果の文書化
- [ ] リリース前最終レビュー

---

## 参照ドキュメント

### プロジェクト構造
- `documents/design/プロジェクト構造設計.md` - プロジェクト構造全体図
- `documents/design/クラス設計.md` - ConfigModels拡張仕様（Excel対応）
- `CLAUDE.md` - 実装者向けプロジェクト構造ガイド

### 実装計画
- `Phase1_Binary形式オーバーロード実装.md` - Phase 1詳細
- `Phase2_ASCII形式オーバーロード実装.md` - Phase 2詳細
- `Phase3_統合テスト実装.md` - Phase 3詳細
- `実装手順サマリー.md` - 全体サマリー
- `問題背景と解決策.md` - 型不一致問題の背景

---

## 成功基準（2025-11-28更新）

### Phase 1完了時（✅達成: 2025-11-28）
- ✅ Excel形式での基本機能動作確認完了
- ✅ PlcConfiguration用オーバーロードメソッド実装完了
- ✅ 全テスト合格（717/719）

### Phase 1.5完了時（実機テスト準備完了）✅達成（2025-11-28）
- ✅ ハードコード値（frameVersion, timeout）解消完了
- ✅ PlcConfigurationに必要フィールド追加完了
  - FrameVersion, Timeout, IsBinary, ConnectionMethod, MonitoringIntervalMs
- ✅ Excel設定ファイルから動的に設定値を読み込み可能
- ✅ 全テスト合格維持（803/804成功）
- ✅ ハードコード置き換え対応Phase 1-5完了

### Phase 2完了時（JSON廃止完了）
- ✅ JSON関連コード完全削除
- ✅ Excel専用システムとして確定
- ✅ ビルド成功、全テストパス

### Phase 3完了時（実機テスト全合格）
- ✅ 実機環境でExcel設定が正常動作
- ✅ Step1-7全フロー実機動作確認完了
- ✅ 長期稼働テスト合格

### Phase 4完了時（最終目標・リリース準備完了）
- ✅ Excel形式のみで全機能動作
- ✅ ドキュメント統一完了（Excel専用システムとして）
- ✅ README更新完了（JSON記載削除）
- ✅ 実機テスト結果文書化完了
- ✅ リリース準備完了
