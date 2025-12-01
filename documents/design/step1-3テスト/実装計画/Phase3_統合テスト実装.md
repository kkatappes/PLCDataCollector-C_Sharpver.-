# Phase 3: 統合テスト実装計画

## 目的
設定読み込み → フレーム構築 の統合テスト（PlcConfiguration型対応）

## 前提条件
✅ Phase 1（Binary形式オーバーロード - TargetDeviceConfig）が完了していること
✅ Phase 2（ASCII形式オーバーロード - TargetDeviceConfig）が完了していること
✅ Phase 1補完（Binary形式オーバーロード - PlcConfiguration）が完了していること

## 実装ファイル
- **統合テストクラス**: `Step1_2_IntegrationTests` (Tests/Integration/Step1_2_IntegrationTests.cs)

## 実装状況
**実装日**: 2025-11-28
**実装範囲**: Round 9（統合テスト実装）
**実装箇所**: Step1_2_IntegrationTests.cs: 179-238行

---

## Round 9: PlcConfiguration設定読み込み→フレーム構築 統合テスト

### Red: 統合テストケース実装

**テストケース**: `TC_Step123_001_PlcConfiguration設定からフレーム構築まで完全実行`
**実装日**: 2025-11-28
**実装箇所**: Step1_2_IntegrationTests.cs: 179-238行

**実装コード**:
```csharp
/// <summary>
/// TC_Step123_001: PlcConfiguration設定読み込みからフレーム構築までの統合テスト
/// Step1（PlcConfiguration構築）とStep2（フレーム構築）の統合テスト
/// Phase1補完で実装したPlcConfiguration用オーバーロードメソッドを使用
/// </summary>
[Fact]
public void TC_Step123_001_PlcConfiguration設定からフレーム構築まで完全実行()
{
    // Step1: PlcConfiguration構築（Excel読み込みを模擬）
    var plcConfig = new PlcConfiguration
    {
        IpAddress = "172.30.40.40",
        Port = 8192,
        DataReadingFrequency = 2000,
        PlcModel = "Q00JCPU",
        SavePath = "C:\\data\\output",
        SourceExcelFile = "5JRS_N2.xlsx",
        Devices = new List<DeviceSpecification>
        {
            new DeviceSpecification("M", 33),  // M33
            new DeviceSpecification("D", 100)  // D100
        }
    };

    // 設定検証
    Assert.Equal("172.30.40.40", plcConfig.IpAddress);
    Assert.Equal(8192, plcConfig.Port);
    Assert.Equal(2, plcConfig.Devices.Count);

    // 先頭デバイス検証（M33）
    var firstDevice = plcConfig.Devices[0];
    Assert.Equal("M", firstDevice.DeviceType);
    Assert.Equal(33, firstDevice.DeviceNumber);

    // Step2: フレーム構築（ConfigToFrameManager - PlcConfigurationオーバーロード使用）
    var frameManager = new ConfigToFrameManager();
    var frameBytes = frameManager.BuildReadRandomFrameFromConfig(plcConfig);

    // フレーム基本検証
    Assert.NotNull(frameBytes);
    Assert.True(frameBytes.Length > 0);

    // 4Eフレームヘッダ検証（PlcConfigurationオーバーロードは4E固定）
    Assert.Equal(0x54, frameBytes[0]); // サブヘッダ下位
    Assert.Equal(0x00, frameBytes[1]); // サブヘッダ上位

    // ReadRandomコマンド検証 (オフセット15-16)
    Assert.Equal(0x03, frameBytes[15]); // コマンド下位
    Assert.Equal(0x04, frameBytes[16]); // コマンド上位

    // ワード点数検証（2デバイス）
    Assert.Equal(2, frameBytes[19]); // ワード点数

    // Dword点数検証（0）
    Assert.Equal(0, frameBytes[20]); // Dword点数
}
```

### Green: Phase 1補完で既に実装済み
Phase 1補完で実装した`BuildReadRandomFrameFromConfig(PlcConfiguration)`メソッドを使用するため、新規実装は不要

### テスト実行
```bash
dotnet test --filter "TC_Step123_001"
```

### パス確認
**実行結果**: ✅ **成功**（2025-11-28）

```bash
# Phase3統合テスト実行
dotnet test andon.sln --filter "FullyQualifiedName~TC_Step123_001" --verbosity minimal --no-restore

# 結果
成功!   -失敗:     0、合格:     1、スキップ:     0、合計:     1、期間: 28 ms
```

**DeviceSpecification修正**:
- 初回実行でDeviceTypeプロパティが空文字列のエラーを発見
- コンストラクタにDeviceType初期化処理を追加（code.ToString()）
- 修正後にテスト成功

---

## Round 10: 全テスト実行

### 全テスト実行
```bash
# ConfigToFrameManagerTests全体実行
dotnet test --filter "FullyQualifiedName~ConfigToFrameManagerTests" --verbosity minimal --no-restore

# 結果
成功!   -失敗:     0、合格:    18、スキップ:     0、合計:    18、期間: 66 ms
```

### パス確認
✅ **完了**: Phase1-3全テスト成功（2025-11-28）
- ConfigToFrameManagerTests: 18/18成功
- Phase3統合テスト: 1/1成功

### Refactor
Phase3統合テストコードは既に適切な構造
リファクタリング不要と判断

---

## Phase 3完了条件
- [x] Round 9: 統合テスト実装完了 ✅ (2025-11-28)
- [x] Round 10: Phase1-3全テストがパス ✅ (2025-11-28)
- [x] DeviceSpecification修正 ✅ (2025-11-28)

## 統合テストの検証項目

### Step1: PlcConfiguration構築
- ✅ PlcConfiguration設定検証
- ✅ IpAddress検証（"172.30.40.40"）
- ✅ Port検証（8192）
- ✅ デバイスリスト検証（2デバイス）
- ✅ 先頭デバイス検証（M33、DeviceType="M", DeviceNumber=33）

### Step2: フレーム構築
- ✅ フレームがnullでないこと
- ✅ フレーム長さが0より大きいこと
- ✅ 4Eフレームヘッダ検証（0x54, 0x00）
- ✅ ReadRandomコマンド検証（0x0403、オフセット15-16）
- ✅ ワード点数検証（2デバイス、オフセット19）
- ✅ Dword点数検証（0、オフセット20）

---

## 既知の問題

### LoggingManagerTestsのファイルロック問題 ✅ 解決済み

**問題内容**（発生時）:
- `LoggingManagerTests.cs`で12/28テストがファイルロックエラーで失敗
- ファイルアクセス競合エラー（IOException）
- テストコードでDispose()の明示的な呼び出しやファイルハンドル解放のタイミング問題

**影響範囲**:
- Phase3統合テストには影響なし（別ファイル）
- LoggingManagerTests: 16/28テスト成功、12/28テスト失敗（問題発生時）
- ConfigToFrameManagerTests: 18/18テスト成功（影響なし）

**解決状況** ✅:
- 2025-11-28にPhase3 Part6で完全解決
- テストコード22箇所にmanager.Dispose()呼び出し追加
- LoggingManagerTests: 28/28テスト全合格（100%成功率）
- Phase3 Part6完了: 153/153テスト合格

**現在の状態**:
- ✅ Phase3統合テストコード実装完了（Step1_2_IntegrationTests.cs: 179-238行）
- ✅ Phase3統合テスト実行成功（TC_Step123_001: 1/1成功）
- ✅ ビルド成功（0エラー、15警告）
- ✅ LoggingManagerTests完全解決（28/28テスト全パス）
- ✅ Phase3 Part6完了（153/153テスト全パス）

---

## 注意事項

### オフラインテスト制約
- 実機PLCへの接続は行わない
- MockSocket/MockUdpServerを必ず使用
- 実データ取得目的でのビルドは禁止

### テストデータ
- 設定データは`PlcConfiguration`モデルをコード内で直接構築
- Excel読み込み機能（`ConfigurationLoaderExcel`）の動作を模擬
- 5JRS_N2.xlsxの設定内容を参考にテストデータを作成

### 成功基準
- Step1-2の全工程がエラーなく完了すること
- 構築されたフレームが仕様通りの構造を持つこと
- PlcConfiguration型からのフレーム構築が正しく機能すること

---

## 実装完了サマリー

**実装日**: 2025-11-28
**実装範囲**: Round 9-10（統合テスト実装・実行）
**実装ステータス**: ✅ **完全実装・実行完了**
**テスト実行ステータス**: ✅ **全テスト成功**

**実装ファイル**:
- `Tests/Integration/Step1_2_IntegrationTests.cs`: 179-238行（60行追加）
- `Core/Models/DeviceSpecification.cs`: 60-68行（DeviceType初期化追加）

**テストケース**:
- `TC_Step123_001_PlcConfiguration設定からフレーム構築まで完全実行`

**テスト実行結果**:
- Phase3統合テスト: 1/1成功（28ms）
- ConfigToFrameManagerTests: 18/18成功（66ms）
- ビルド: 成功（0エラー、15警告）

**検証項目**:
- ✅ PlcConfiguration設定検証（IpAddress, Port, Devices）
- ✅ DeviceSpecification検証（DeviceType, DeviceNumber）
- ✅ 4Eフレーム構築検証（ヘッダ、コマンド、デバイス点数）

**修正内容**:
- DeviceSpecificationコンストラクタにDeviceType初期化処理追加

**詳細結果文書**:
- `documents/design/step1-3テスト/実装結果/Phase3_統合テスト実装_TestResults.md`

**次のステップ**:
1. ✅ Phase1-3実装完了（2025-11-28）
2. ✅ LoggingManagerTests修正完了（Phase3 Part6、2025-11-28）
3. ⏳ Step4-6実装: PLC通信・データ受信・パース処理
4. ⏳ Step7実装: データ出力処理
