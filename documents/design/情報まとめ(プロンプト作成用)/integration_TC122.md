# Integration FullCycle 複数サイクル テスト実装用情報（TC122）

## ドキュメント概要

### 目的
このドキュメントは、TC122_FullCycle_複数サイクル実行時統計累積テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `各ステップio.md` - 各ステップのInput/Output詳細

#### 既存テストケース実装情報
- `integration_TC121.md` - FullCycle基本実行テスト
- `step3_TC017.md` - TCP接続テスト（Step3）
- `step3_TC021.md` - 送信テスト（Step4）
- `step3_TC025.md` - 受信テスト（Step4）
- `step3_TC027.md` - 切断テスト（Step5）
- `step6_TC029.md` - 基本後処理テスト（Step6-1）
- `step6_TC032.md` - DWord結合テスト（Step6-2）
- `step6_TC037.md` - 3Eフレーム解析テスト（Step6-3）

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象機能仕様

### FullCycle複数サイクル実行（Step3-6複数回実行）
**統合テスト対象**: PlcCommunicationManager複数サイクル実行
**名前空間**: andon.Core.Managers

#### 複数サイクル実行パターン
```
サイクル1: Step3→Step4→Step5→Step6 (接続→送受信→切断→データ処理)
  ↓ (統計累積)
サイクル2: Step3→Step4→Step5→Step6 (接続→送受信→切断→データ処理)
  ↓ (統計累積)
サイクル3: Step3→Step4→Step5→Step6 (接続→送受信→切断→データ処理)
  ↓ (統計累積)
最終統計集計・検証
```

#### 統計累積Input（各サイクル共通）
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=true, IsBinary=false）
- TimeoutConfig（ConnectTimeoutMs=5000, SendTimeoutMs=3000, ReceiveTimeoutMs=3000）
- SLMPフレーム（2つ）:
  - M機器用: "54001234000000010401006400000090E8030000"
  - D機器用: "54001234000000010400A800000090E8030000"
- 実行サイクル数: 3回

#### 統計累積Output
- ConnectionStats（累積統計情報）:
  - TotalCycles（総実行サイクル数=3）
  - TotalResponseTime（合計応答時間=サイクル1+サイクル2+サイクル3）
  - AverageResponseTime（平均応答時間=TotalResponseTime/TotalCycles）
  - TotalConnectionTime（合計接続時間）
  - AverageConnectionTime（平均接続時間）
  - TotalProcessingTime（合計データ処理時間）
  - AverageProcessingTime（平均データ処理時間）
  - TotalDevicesProcessed（合計処理デバイス数=各サイクルデバイス数の累計）
  - TotalDataSize（合計データサイズ=各サイクルデータサイズの累計）
  - TotalErrors（合計エラー数=0（正常ケース））
  - TotalRetries（合計リトライ数=0（正常ケース））
  - FirstCycleSuccessRate（サイクル1の成功率=100%）
  - FinalSuccessRate（全サイクル通しての成功率=100%）

#### 機能
- 複数サイクルのStep3-6連続実行
- サイクル間の統計データ累積・継続
- 各サイクル個別統計の記録
- サイクル全体統計の集計・計算
- メモリリーク検証（各サイクル後のリソース解放確認）
- パフォーマンス推移監視（サイクル毎の処理時間推移）

#### データ取得元
- ConfigToFrameManager.LoadConfigAsync()（接続設定、デバイス設定）
- ConfigToFrameManager.BuildFrames()（SLMPフレーム）
- PlcCommunicationManager.ConnectionStats（累積統計情報）

---

## 2. テストケース仕様（TC122）

### TC122_FullCycle_複数サイクル実行時統計累積
**目的**: Step3-6の複数サイクル実行と統計累積機能を統合テスト

#### 前提条件
- ConfigToFrameManagerによる設定読み込み完了
- PLCが応答可能状態（実際の接続は不要、モック使用）
- 統計オブジェクトが初期化済み
- メモリ使用量が正常範囲内

#### 入力データ
**共通設定（全サイクル）**:
- ConnectionConfig:
  ```
  IpAddress: "192.168.1.10"
  Port: 5000
  UseTcp: true
  ConnectionType: "TCP"
  IsBinary: false
  FrameVersion: FrameVersion.Frame4E
  ```
- TimeoutConfig:
  ```
  ConnectTimeoutMs: 5000
  SendTimeoutMs: 3000
  ReceiveTimeoutMs: 3000
  RetryTimeoutMs: 1000
  ```

**SLMPフレーム（各サイクル送信）**:
- M機器用: "54001234000000010401006400000090E8030000"
- D機器用: "54001234000000010400A800000090E8030000"

**サイクル実行設定**:
- 実行回数: 3サイクル
- サイクル間間隔: 100ms
- 統計リセット: false（累積継続）

#### 期待出力
**各サイクル個別結果**:
- Cycle1Result（StructuredData）:
  - ProcessedDevices: M000-M999, D000-D999
  - ConnectionTime: 約50-200ms
  - ProcessingTime: 約10-50ms
  - DeviceCount: 2000
  - DataSize: 実際のバイト数
- Cycle2Result（同様の構造）
- Cycle3Result（同様の構造）

**累積統計結果（ConnectionStats）**:
- TotalCycles: 3
- TotalResponseTime: Cycle1+Cycle2+Cycle3の合計（約150-600ms）
- AverageResponseTime: TotalResponseTime/3（約50-200ms）
- TotalConnectionTime: 接続時間合計（約150-600ms）
- AverageConnectionTime: 接続時間平均（約50-200ms）
- TotalProcessingTime: データ処理時間合計（約30-150ms）
- AverageProcessingTime: データ処理時間平均（約10-50ms）
- TotalDevicesProcessed: 6000（2000×3サイクル）
- TotalDataSize: データサイズ合計
- TotalErrors: 0
- TotalRetries: 0
- SuccessRate: 100.0

#### 統計精度要件
- 時間計測精度: ±50ms以内
- 累積計算精度: 小数点以下2桁
- デバイス数計算: 完全一致
- 成功率計算: 小数点以下1桁（100.0%）

---

## 3. 複数サイクル実行フロー詳細

### サイクル実行ループ
```csharp
for (int cycle = 1; cycle <= 3; cycle++)
{
    // Step3: 接続
    var connectResult = await plcManager.ConnectAsync(connectionConfig, timeoutConfig);

    // Step4: 送受信
    await plcManager.SendFrameAsync(mDeviceFrame);
    var mResponse = await plcManager.ReceiveResponseAsync(timeoutConfig);

    await plcManager.SendFrameAsync(dDeviceFrame);
    var dResponse = await plcManager.ReceiveResponseAsync(timeoutConfig);

    // Step5: 切断
    await plcManager.DisconnectAsync();

    // Step6: データ処理
    var processedData = await plcManager.ProcessReceivedRawData(mResponse, dResponse);
    var combinedData = await plcManager.CombineDwordData(processedData);
    var structuredData = await plcManager.ParseRawToStructuredData(combinedData);

    // 統計記録
    RecordCycleStats(cycle, connectResult, structuredData);

    // サイクル間隔
    await Task.Delay(100);
}
```

### 統計累積メソッド
```csharp
private void RecordCycleStats(int cycle, ConnectionResponse connection, StructuredData structured)
{
    connectionStats.AddCycle(cycle);
    connectionStats.AddResponseTime(connection.ConnectionTime.Value);
    connectionStats.AddProcessingTime(structured.ProcessingTime);
    connectionStats.AddDeviceCount(structured.DeviceCount);
    connectionStats.AddDataSize(structured.TotalDataSize);
    connectionStats.UpdateSuccessRate();
}
```

---

## 4. 依存クラス・設定

### ConnectionStats（拡張統計情報）
**取得元**: PlcCommunicationManager内部統計

```csharp
public class ConnectionStats
{
    // 基本統計（既存）
    public int TotalCycles { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int TotalErrors { get; set; }
    public int TotalRetries { get; set; }
    public double SuccessRate { get; set; }

    // 複数サイクル専用統計（新規）
    public TimeSpan TotalConnectionTime { get; set; }
    public TimeSpan AverageConnectionTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public int TotalDevicesProcessed { get; set; }
    public long TotalDataSize { get; set; }

    // サイクル個別統計リスト
    public List<CycleStats> CycleStatistics { get; set; }
}
```

### CycleStats（サイクル個別統計）
```csharp
public class CycleStats
{
    public int CycleNumber { get; set; }
    public TimeSpan ConnectionTime { get; set; }
    public TimeSpan SendTime { get; set; }
    public TimeSpan ReceiveTime { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public TimeSpan TotalCycleTime { get; set; }
    public int DeviceCount { get; set; }
    public long DataSize { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### StructuredData（拡張統計情報付き）
```csharp
public class StructuredData
{
    // 既存プロパティ
    public Dictionary<string, StructuredDevice> StructuredDeviceData { get; set; }
    public SlmpFrameInfo FrameInfo { get; set; }

    // 統計専用プロパティ（新規）
    public TimeSpan ProcessingTime { get; set; }
    public int DeviceCount { get; set; }
    public long TotalDataSize { get; set; }
    public DateTime ProcessedAt { get; set; }
}
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerIntegrationTests.cs
- **配置先**: Tests/Integration/Core/Managers/
- **名前空間**: andon.Tests.Integration.Core.Managers

### テスト実装順序
1. **TC122_FullCycle_複数サイクル実行時統計累積**（最優先）
   - 3サイクル実行テスト
   - 統計累積検証テスト
2. パフォーマンステスト（次フェーズ）
   - メモリリーク検証
   - サイクル間パフォーマンス比較

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: 複数サイクル対応PlcCommunicationManager
- **MockConfigToFrameManager**: 設定読み込み・フレーム構築のモック
- **MockSocket**: TCP/UDP通信のモック（各サイクル独立）

#### 使用するスタブ
- **PlcMultiCycleResponseStubs**: 複数サイクル用PLC応答データ
- **ConfigurationStubs**: 設定データのスタブ
- **StatisticsStubs**: 統計計算検証用のスタブ

---

## 6. テストケース実装構造

### Arrange（準備）
1. MockSocketの準備（3サイクル分）
   - 各サイクルで異なる応答時間を設定
   - サイクル1: 100ms, サイクル2: 150ms, サイクル3: 120ms
2. ConnectionConfig・TimeoutConfigの準備
   - 複数サイクル実行に適した設定値
3. PlcCommunicationManagerインスタンス作成
   - 統計機能有効化
   - モックSocket注入
4. 期待統計値の準備
   - 予想される累積時間、平均時間等

### Act（実行）
1. 3サイクルのFullCycle実行
   ```csharp
   var results = new List<StructuredData>();
   for (int cycle = 1; cycle <= 3; cycle++)
   {
       var result = await ExecuteFullCycle(plcManager, cycle);
       results.Add(result);
   }
   ```
2. 統計情報取得
   ```csharp
   var finalStats = plcManager.GetConnectionStats();
   ```

### Assert（検証）
1. 各サイクル個別結果の検証
   - 各サイクルでStructuredDataが正常取得
   - デバイス数・データサイズの一致
2. 累積統計の検証
   - TotalCycles = 3
   - TotalResponseTime = サイクル時間の合計
   - AverageResponseTime = TotalResponseTime / 3
   - TotalDevicesProcessed = 6000
   - SuccessRate = 100.0
3. 統計計算精度の検証
   - 時間計算の誤差範囲内（±50ms）
   - 平均値計算の正確性
   - 累積値の正確性

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（サイクル毎独立インスタンス）
- **ConfigToFrameManager**: Singleton（設定共有）
- **ConnectionStats**: Transient（サイクル統計別管理）
- **LoggingManager**: Singleton（共有リソース）
- **ErrorHandler**: Singleton（共有リソース）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddSingleton<IConfigToFrameManager, ConfigToFrameManager>();
services.AddTransient<IConnectionStats, ConnectionStats>();
services.AddSingleton<ILoggingManager, LoggingManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
```

---

## 8. エラーハンドリング

### 複数サイクル固有例外
- **CycleExecutionException**: サイクル実行時の例外
- **StatisticsCalculationException**: 統計計算エラー
- **MemoryLeakException**: メモリリーク検出時
- **CycleTimeoutException**: サイクル全体タイムアウト

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    public const string CycleExecutionFailed = "サイクル{0}の実行中にエラーが発生しました: {1}";
    public const string StatisticsCalculationFailed = "統計計算中にエラーが発生しました: {0}";
    public const string MemoryLeakDetected = "サイクル{0}実行後にメモリリークが検出されました";
    public const string CycleTimeout = "サイクル{0}の実行がタイムアウトしました（制限時間: {1}ms）";
}
```

---

## 9. ログ出力要件

### LoggingManager連携
- サイクル開始ログ: サイクル番号、開始時刻
- サイクル完了ログ: サイクル番号、所要時間、統計情報
- 統計累積ログ: 累積統計情報の更新内容
- 最終統計ログ: 全サイクル完了時の最終統計情報

### ログレベル
- **Information**: サイクル開始・完了、統計更新
- **Debug**: 個別統計詳細、計算過程
- **Warning**: パフォーマンス劣化検出時

---

## 10. テスト実装チェックリスト

### TC122実装前
- [ ] ConnectionStats拡張（複数サイクル統計プロパティ追加）
- [ ] CycleStatsクラス作成
- [ ] StructuredData統計プロパティ追加
- [ ] 複数サイクル実行メソッド定義
- [ ] MockSocket複数サイクル対応

### TC122実装中
- [ ] Arrange: 3サイクル分モック・スタブ準備
- [ ] Act: 複数サイクル実行ループ
- [ ] Assert: 個別サイクル結果検証
- [ ] Assert: 累積統計検証
- [ ] Assert: 統計計算精度検証

### TC122実装後
- [ ] テスト実行・Red確認
- [ ] 複数サイクル実行機能実装（最小実装）
- [ ] 統計累積機能実装
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施（パフォーマンス最適化）

---

## 11. パフォーマンス検証項目

### メモリ使用量監視
- サイクル開始時メモリ使用量記録
- サイクル終了時メモリ使用量記録
- メモリリーク検出（使用量増加トレンド監視）
- ガベージコレクション実行回数監視

### 処理時間監視
- サイクル毎の処理時間推移
- 処理時間の標準偏差計算
- パフォーマンス劣化検出（閾値: 前サイクル比+20%）

### リソース監視
- ソケット接続数（リーク検出）
- ファイルハンドル数
- スレッド数

---

## 12. 統計計算アルゴリズム

### 累積統計計算式
```csharp
// 合計時間累積
TotalResponseTime += currentCycleTime;

// 平均時間計算
AverageResponseTime = TotalResponseTime / TotalCycles;

// 成功率計算
SuccessRate = (double)SuccessfulCycles / TotalCycles * 100.0;

// データサイズ累積
TotalDataSize += currentCycleDataSize;

// デバイス数累積
TotalDevicesProcessed += currentCycleDeviceCount;
```

### 統計検証式
```csharp
// 時間計算精度検証（±50ms以内）
Assert.True(Math.Abs(expected.TotalMilliseconds - actual.TotalMilliseconds) <= 50);

// 平均値計算精度検証（小数点以下2桁）
Assert.Equal(expected, actual, 2);

// 成功率計算精度検証（小数点以下1桁）
Assert.Equal(expectedRate, actualRate, 1);
```

---

以上が TC122_FullCycle_複数サイクル実行時統計累積テスト実装に必要な情報です。