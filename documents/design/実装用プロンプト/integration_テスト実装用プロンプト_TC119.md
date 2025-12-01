# TC119_Step6_各段階データ伝達整合性統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC119（Step6_各段階データ伝達整合性）の統合テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManagerのStep6データ処理における3段階のデータ伝達整合性を検証します。
このテストは、ProcessReceivedRawData → CombineDwordData → ParseRawToStructuredDataの各段階で
データが正しく受け渡され、変換・結合・解析処理が期待通りに連携することを確認します。

### 実装対象
- **テストファイル**: `Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs`
- **テスト名前空間**: `andon.Tests.Integration.Core.Managers`
- **テストメソッド名**: `TC119_Step6_各段階データ伝達整合性_M000M999`, `TC119_Step6_各段階データ伝達整合性_D000D999`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/BasicProcessedResponseData.cs`
   - `Core/Models/ProcessedResponseData.cs`
   - `Core/Models/StructuredData.cs`
   - `Core/Models/ProcessedDeviceRequestInfo.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/DataSources/SampleSLMPResponses.cs`
   - `Tests/TestUtilities/Assertions/DataIntegrityAssertions.cs`

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル作成・拡張
```
ファイル: Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs
名前空間: andon.Tests.Integration.Core.Managers
```

#### Step 1-2: テストケース実装（2つ）

**TC119-1: M000-M999データ段階伝達整合性テスト**

**Arrange（準備）**:
- 生レスポンスデータ（M000-M999用）を準備
  - サンプルデータ: `"D0000C0300000000005405000000..."`（125バイトのバイナリデータを16進文字列表現）
- ProcessedDeviceRequestInfoリストを準備
  - DeviceCode: "M", StartDeviceNumber: 0, DeviceCount: 1000
  - IsBit: true, IsDWordCombined: false
- PlcCommunicationManagerインスタンス作成
- 3段階の中間データ格納用変数を準備

**Act（実行）**:
```csharp
// Stage 1: ProcessReceivedRawData
var basicProcessedData = await manager.ProcessReceivedRawData(rawResponseData);

// Stage 2: CombineDwordData
var processedData = await manager.CombineDwordData(basicProcessedData, deviceRequestInfos);

// Stage 3: ParseRawToStructuredData
var structuredData = await manager.ParseRawToStructuredData(processedData);
```

**Assert（検証）**:

*Stage 1 → Stage 2 データ伝達整合性*:
- basicProcessedData.BasicProcessedDevices.Count = 1000
- processedData.ProcessedDevices.Count = 1000（結合不要のため同じ数）
- 各デバイス値が正しく伝達されていること
- エラー情報が適切に伝達されること

*Stage 2 → Stage 3 データ伝達整合性*:
- processedData.ProcessedDevices.Count = structuredData.StructuredDevices.Count
- DeviceNameパターンが維持されていること（M000, M001, ..., M999）
- DeviceValueが正しく伝達されていること
- TimestampParsedが設定されていること

*全段階通しての整合性*:
- 元の生データのビット情報がstructuredDataまで正しく伝達
- レスポンス時間の累積計算が正しいこと
- 各段階でエラーが発生していないこと

**TC119-2: D000-D999データ段階伝達整合性テスト（DWord結合あり）**

**Arrange（準備）**:
- 生レスポンスデータ（D000-D999用）を準備
- ProcessedDeviceRequestInfoリストを準備
  - DeviceCode: "D", StartDeviceNumber: 0, DeviceCount: 1000
  - IsBit: false, IsDWordCombined: true（D500-D999をDWord結合対象とする）

**Act（実行）**:
同様の3段階実行

**Assert（検証）**:

*Stage 1 → Stage 2 データ伝達整合性（DWord結合処理込み）*:
- basicProcessedData.BasicProcessedDevices.Count = 1000
- processedData.ProcessedDevices.Count = 750（D500-D999の500個が250個のDWordに結合）
- DWord結合済みデバイス（D500D501, D502D503, ...）が正しく作成されていること

*Stage 2 → Stage 3 データ伝達整合性*:
- 結合済みデバイス数が正しく伝達されていること
- DWordデバイス名のフォーマットが正しいこと（"D500D501"形式）
- 32ビット値が正しく計算・伝達されていること

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC119"
```

期待結果: テスト失敗（統合処理が未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: データ伝達メソッド統合実装

**ProcessReceivedRawData改修**:
- BasicProcessedResponseDataへの適切なデータ設定
- 次段階への引き渡し情報の完全性確保

**CombineDwordData改修**:
- BasicProcessedResponseDataからの正確なデータ受け取り
- DWord結合ロジックの完全実装
- ProcessedResponseDataへの適切なデータ設定

**ParseRawToStructuredData改修**:
- ProcessedResponseDataからの正確なデータ受け取り
- 構造化データへの変換完全実装
- StructuredDataへの適切なデータ設定

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC119"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- 各段階でのログ出力追加
  - データ伝達開始/完了ログ
  - 変換・結合・解析の詳細ログ
  - パフォーマンス計測ログ
- エラーハンドリング強化
  - 段階間でのデータ不整合検出
  - 適切な例外スロー
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC119"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### データ段階遷移仕様

#### Stage 1: ProcessReceivedRawData
**入力**: 生レスポンスデータ（16進文字列）
**出力**: BasicProcessedResponseData
- BasicProcessedDevices: List<BasicProcessedDevice>
- ResponseTimeMs: 処理時間
- ErrorDetails: エラー情報
- WarningDetails: 警告情報

#### Stage 2: CombineDwordData
**入力**: BasicProcessedResponseData
**出力**: ProcessedResponseData
- ProcessedDevices: List<ProcessedDevice>（DWord結合反映済み）
- CombinedDWordDevices: List<ProcessedDevice>（結合されたDWordデバイスのみ）
- ResponseTimeMs: 累積処理時間
- ErrorDetails, WarningDetails: 累積情報

#### Stage 3: ParseRawToStructuredData
**入力**: ProcessedResponseData
**出力**: StructuredData
- StructuredDevices: List<StructuredDevice>
- ParseSteps: List<ParseStep>（解析手順の記録）
- ResponseTimeMs: 累積処理時間
- TimestampParsed: 解析完了時刻

### データ整合性チェック項目

**デバイス数整合性**:
- M機器（ビット型、結合なし）: 1000 → 1000 → 1000
- D機器（ワード型、部分結合）: 1000 → 750 → 750

**デバイス名整合性**:
- 元のデバイス名形式の維持: "M000", "M001", ...
- DWord結合デバイス名: "D500D501", "D502D503", ...

**値整合性**:
- ビット値の保持: 0/1 → false/true → "0"/"1"
- ワード値の保持: 16進 → 10進 → 文字列表現
- DWord値の計算: 2つの16ビット値 → 32ビット値

### エラーハンドリング

**スロー例外**:
- `DataIntegrityException`: 段階間データ不整合
- `InvalidDataTransformationException`: データ変換エラー
- `ProcessingSequenceException`: 処理順序エラー

**エラーメッセージ定数**:
```csharp
public const string DataIntegrityError = "段階間でのデータ整合性エラーが発生しました。";
public const string TransformationError = "データ変換処理でエラーが発生しました。";
public const string SequenceError = "処理段階の順序が正しくありません。";
```

---

## モック・テストデータ実装

### サンプルSLMPレスポンス（M000-M999）
```csharp
public static class SampleSLMPResponses
{
    public const string M000_M999_Response = "D0000C0300000000005405000000" +
        "1010101010101010..." + // 125バイトのビットデータ（1000ビット分）
        "0000"; // 終端コード
}
```

### サンプルSLMPレスポンス（D000-D999）
```csharp
public const string D000_D999_Response = "D0000C0300000000005407D0" +
    "00010002000300040005..." + // 2000バイトのワードデータ（1000ワード分）
    "0000"; // 終端コード
```

### データ整合性アサーション
```csharp
public static class DataIntegrityAssertions
{
    public static void AssertDataIntegrity(
        BasicProcessedResponseData basicData,
        ProcessedResponseData processedData,
        StructuredData structuredData)
    {
        // デバイス数整合性チェック
        // 値整合性チェック
        // タイムスタンプ整合性チェック
    }
}
```

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC119実装.md`
- 実装開始時刻
- 目標（TC119統合テスト実装完了）
- 実装方針（段階間データ伝達整合性の検証）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/Step6統合処理実装記録.md`
- 段階間データ伝達の設計判断
- DWord結合処理の統合実装方針
- パフォーマンス考慮事項
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC119_統合テスト結果.log`
- 各段階の個別実行結果
- 統合テスト結果（実行時間、メモリ使用量）
- データ整合性チェック結果
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC119-1（M000-M999段階伝達）テストがパス
- [ ] TC119-2（D000-D999段階伝達、DWord結合込み）テストがパス
- [ ] 3段階のデータ伝達処理統合実装完了
- [ ] データ整合性チェック機能実装完了
- [ ] リファクタリング完了（ログ出力、エラーハンドリング等）
- [ ] テスト再実行でGreen維持確認
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実装用プロンプト.mdの該当項目にチェック

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認

### データ整合性重視
- 各段階での中間データを詳細にチェック
- デバイス数の変化（特にDWord結合時）を正確に追跡
- 値の変換過程での精度維持を確認

### パフォーマンス考慮
- 3段階の処理時間を個別・累積で計測
- メモリ使用量の監視
- 大容量データ（1000デバイス）での安定性確認

### 記録の重要性
- 段階間でのデータ変化を詳細に記録
- テスト結果は数値データも含めて保存
- 統合処理の判断根拠を明確に記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`
- `documents/design/各ステップio.md`
- `documents/design/各クラス・メソッドio.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### 既存テスト参照
- `step6_TC029.md`（ProcessReceivedRawData単体テスト）
- `step6_TC032.md`（CombineDwordData単体テスト）
- `step6_TC037.md`（ParseRawToStructuredData単体テスト）
- `integration_TC121.md`（Step3-6完全サイクルテスト）

---

以上の指示に従って、TC119_Step6_各段階データ伝達整合性統合テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。