# TC122_FullCycle_複数サイクル実行時統計累積統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC122（FullCycle_複数サイクル実行時統計累積）の統合テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManagerの完全サイクル（Step3→4→5→6）を複数回実行した際の
統計情報累積機能が正常に動作することを検証します。
このテストは、接続時間、応答時間、エラー率、リトライ回数などの統計データが
複数サイクルを通して正確に累積・更新されることを確認します。

### 実装対象
- **テストファイル**: `Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs`
- **テスト名前空間**: `andon.Tests.Integration.Core.Managers`
- **テストメソッド名**: `TC122_FullCycle_複数サイクル実行時統計累積_TCP`, `TC122_FullCycle_複数サイクル実行時統計累積_UDP`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`
   - `Core/Models/ConnectionStats.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Models/StructuredData.cs`
   - `Core/Models/ConnectionConfig.cs`

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/MockSocket.cs`
   - `Tests/TestUtilities/Assertions/StatisticsAssertions.cs`
   - `Tests/TestUtilities/DataSources/SampleSLMPResponses.cs`

3. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル拡張
```
ファイル: Tests/Integration/Core/Managers/PlcCommunicationManagerIntegrationTests.cs
名前空間: andon.Tests.Integration.Core.Managers
```

#### Step 1-2: テストケース実装（2つ）

**TC122-1: TCP複数サイクル統計累積テスト**

**Arrange（準備）**:
- MockSocketを作成（安定した接続・送受信をシミュレート）
- ConnectionConfigを作成（TCP設定）
  - IpAddress = "192.168.3.250"
  - Port = 5007
  - UseTcp = true
  - ConnectionType = "TCP"
- 複数サイクル実行用のデータセット準備
  - サイクル1: M000-M999データ（応答時間: 150ms想定）
  - サイクル2: D000-D999データ（応答時間: 200ms想定）
  - サイクル3: M000-M999データ（応答時間: 180ms想定）
  - サイクル4: D000-D999データ（応答時間: 220ms想定）
  - サイクル5: M000-M999データ（応答時間: 160ms想定）
- PlcCommunicationManagerインスタンス作成
- 初期統計状態を記録

**Act（実行）**:
```csharp
ConnectionStats initialStats = manager.GetConnectionStats();

// 5サイクル実行
for (int cycle = 1; cycle <= 5; cycle++)
{
    // Step3: Connect
    var connectResponse = await manager.ConnectAsync(connectionConfig);

    // Step4: SendFrame
    string frameString = GetFrameStringForCycle(cycle);
    await manager.SendFrameAsync(frameString);

    // Step5: ReceiveResponse
    var rawResponse = await manager.ReceiveResponseAsync();

    // Step6: データ処理完全実行
    var basicProcessed = await manager.ProcessReceivedRawData(rawResponse);
    var processed = await manager.CombineDwordData(basicProcessed, deviceRequestInfos);
    var structured = await manager.ParseRawToStructuredData(processed);

    // Step3-5: Disconnect
    await manager.DisconnectAsync();

    // 各サイクル後の統計確認
    var cycleStats = manager.GetConnectionStats();
    AssertCycleStatistics(cycle, cycleStats);
}

ConnectionStats finalStats = manager.GetConnectionStats();
```

**Assert（検証）**:

*累積統計検証*:
- TotalConnections = 5（5サイクル分）
- TotalDisconnections = 5
- SuccessfulConnections = 5（全て成功）
- TotalFramesSent = 5
- TotalResponsesReceived = 5
- AverageResponseTime = (150+200+180+220+160) / 5 = 182ms
- TotalUptime = 各サイクルの接続時間の合計
- SuccessRate = 100%

*個別サイクル統計検証*:
- 各サイクルで統計値が単調増加していること
- レスポンス時間の個別記録が正しいこと
- エラー・リトライ回数が0のまま維持されること

*統計精度検証*:
- ResponseTimeHistoryの要素数 = 5
- 最短・最長レスポンス時間の記録が正確であること
- 標準偏差計算が正しいこと

**TC122-2: UDP複数サイクル統計累積テスト（エラー混入）**

**Arrange（準備）**:
同様の設定で、以下の変更:
- ConnectionConfig.UseTcp = false（UDP設定）
- 意図的なエラーを3サイクル目、4サイクル目に混入
  - 3サイクル目: 接続タイムアウト（1回リトライして成功）
  - 4サイクル目: 受信タイムアウト（2回リトライして成功）

**Act（実行）**:
同様の5サイクル実行（エラー・リトライ処理込み）

**Assert（検証）**:

*エラー混入時統計検証*:
- TotalConnections = 5 + 1（リトライ分）
- SuccessfulConnections = 5（最終的に全て成功）
- TotalRetries = 3（1回 + 2回）
- ErrorCount = 3（タイムアウトエラー3回）
- SuccessRate = 5/8 = 62.5%（リトライ込みでの成功率）

*リトライ統計検証*:
- RetryHistory記録が正確であること
- エラータイプ別カウントが正しいこと
- リトライ後の成功が正しく記録されること

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC122"
```

期待結果: テスト失敗（統計累積機能が未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectionStats統計累積機能実装

**ConnectionStats クラス拡張**:
```csharp
public class ConnectionStats
{
    // 基本統計
    public int TotalConnections { get; private set; }
    public int SuccessfulConnections { get; private set; }
    public int TotalDisconnections { get; private set; }
    public int TotalFramesSent { get; private set; }
    public int TotalResponsesReceived { get; private set; }

    // 時間統計
    public List<double> ResponseTimeHistory { get; private set; } = new();
    public double AverageResponseTime => ResponseTimeHistory.Count > 0 ? ResponseTimeHistory.Average() : 0;
    public double MinResponseTime => ResponseTimeHistory.Count > 0 ? ResponseTimeHistory.Min() : 0;
    public double MaxResponseTime => ResponseTimeHistory.Count > 0 ? ResponseTimeHistory.Max() : 0;
    public double ResponseTimeStandardDeviation { get; private set; }

    // エラー・リトライ統計
    public int ErrorCount { get; private set; }
    public int TotalRetries { get; private set; }
    public List<RetryRecord> RetryHistory { get; private set; } = new();
    public double SuccessRate => TotalConnections > 0 ? (double)SuccessfulConnections / TotalConnections * 100 : 0;

    // 累積メソッド
    public void AddConnection(bool successful)
    public void AddDisconnection()
    public void AddFrameSent()
    public void AddResponseReceived(double responseTimeMs)
    public void AddError(string errorType)
    public void AddRetry(string reason, int attemptNumber)

    // 統計計算メソッド
    private void RecalculateStatistics()
}
```

**PlcCommunicationManager統計連携実装**:
- 各メソッドでの統計更新呼び出し
- ConnectionStatsインスタンスの管理
- GetConnectionStats()メソッドの実装

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC122"
```

期待結果: すべてのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- 統計計算の最適化
  - ResponseTimeHistoryのメモリ効率化
  - 標準偏差計算の最適化
- ログ出力追加
  - サイクル開始/完了ログ
  - 統計更新ログ
  - パフォーマンス分析ログ
- スレッドセーフティ確保
  - 統計更新時のlockメカニズム
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC122"
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### 統計累積仕様

#### 基本カウンタ
- **TotalConnections**: ConnectAsync呼び出し回数（リトライ含む）
- **SuccessfulConnections**: 成功した接続数
- **TotalDisconnections**: DisconnectAsync呼び出し回数
- **TotalFramesSent**: SendFrameAsync呼び出し回数
- **TotalResponsesReceived**: ReceiveResponseAsync成功回数

#### 時間統計
- **ResponseTimeHistory**: 各レスポンス時間の履歴（List<double>）
- **AverageResponseTime**: 平均レスポンス時間（ミリ秒）
- **MinResponseTime**: 最短レスポンス時間
- **MaxResponseTime**: 最長レスポンス時間
- **ResponseTimeStandardDeviation**: レスポンス時間の標準偏差

#### エラー・リトライ統計
- **ErrorCount**: 発生したエラーの総数
- **TotalRetries**: 実行されたリトライの総数
- **RetryHistory**: リトライの詳細履歴
- **SuccessRate**: 成功率（SuccessfulConnections / TotalConnections × 100）

### 統計更新タイミング

#### Connect時
```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config)
{
    _stats.AddConnection(false); // 開始時
    try
    {
        // 接続処理
        _stats.AddConnection(true); // 成功時（最初のfalseを上書き）
        return response;
    }
    catch (Exception ex)
    {
        _stats.AddError(ex.GetType().Name);
        throw;
    }
}
```

#### SendFrame時
```csharp
public async Task SendFrameAsync(string frameString)
{
    var stopwatch = Stopwatch.StartNew();
    try
    {
        // 送信処理
        _stats.AddFrameSent();
    }
    finally
    {
        stopwatch.Stop();
        _stats.AddResponseReceived(stopwatch.ElapsedMilliseconds);
    }
}
```

### エラーハンドリング

**スロー例外**:
- `StatisticsOverflowException`: 統計データのオーバーフロー
- `ConcurrentStatisticsException`: 並行統計更新エラー

**エラーメッセージ定数**:
```csharp
public const string StatisticsOverflow = "統計データがオーバーフローしました。";
public const string ConcurrentUpdate = "統計の並行更新でエラーが発生しました。";
```

---

## モック・テストデータ実装

### MockSocket拡張（統計テスト対応）
```csharp
public class MockSocketForStatistics : MockSocket
{
    public List<TimeSpan> SimulatedResponseTimes { get; set; } = new();
    public List<Exception> SimulatedErrors { get; set; } = new();

    public override async Task<int> SendAsync(byte[] buffer, SocketFlags flags)
    {
        // 指定されたレスポンス時間をシミュレート
        if (SimulatedResponseTimes.Count > _callCount)
        {
            await Task.Delay(SimulatedResponseTimes[_callCount]);
        }

        // 指定されたエラーをシミュレート
        if (SimulatedErrors.Count > _callCount && SimulatedErrors[_callCount] != null)
        {
            throw SimulatedErrors[_callCount];
        }

        return base.SendAsync(buffer, flags);
    }
}
```

### 統計アサーション
```csharp
public static class StatisticsAssertions
{
    public static void AssertCumulativeStatistics(
        ConnectionStats initial,
        ConnectionStats final,
        int expectedCycles,
        double[] expectedResponseTimes)
    {
        Assert.Equal(expectedCycles, final.TotalConnections - initial.TotalConnections);
        Assert.Equal(expectedResponseTimes.Average(), final.AverageResponseTime, 1);
        Assert.Equal(expectedResponseTimes.Length, final.ResponseTimeHistory.Count - initial.ResponseTimeHistory.Count);
    }

    public static void AssertStatisticsProgression(
        List<ConnectionStats> statsHistory)
    {
        for (int i = 1; i < statsHistory.Count; i++)
        {
            Assert.True(statsHistory[i].TotalConnections >= statsHistory[i-1].TotalConnections);
            Assert.True(statsHistory[i].TotalFramesSent >= statsHistory[i-1].TotalFramesSent);
        }
    }
}
```

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC122実装.md`
- 実装開始時刻
- 目標（TC122複数サイクル統計累積テスト実装完了）
- 実装方針（統計累積アルゴリズムの設計）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/統計累積機能実装記録.md`
- 統計データ構造の設計判断
- メモリ効率とパフォーマンスのトレードオフ
- スレッドセーフティの実装方針
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC122_複数サイクルテスト結果.log`
- 各サイクルの統計データ推移
- メモリ使用量の変化
- パフォーマンス計測結果
- エラー・リトライ処理の検証結果

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC122-1（TCP複数サイクル統計累積）テストがパス
- [ ] TC122-2（UDP複数サイクル統計累積、エラー混入）テストがパス
- [ ] ConnectionStats統計累積機能実装完了
- [ ] PlcCommunicationManagerとの統計連携実装完了
- [ ] スレッドセーフティ確保完了
- [ ] リファクタリング完了（ログ出力、パフォーマンス最適化等）
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

### 統計データの正確性
- 累積計算の精度を維持
- オーバーフローやアンダーフローの対策
- 浮動小数点演算の誤差対応

### パフォーマンス考慮
- 大量の統計データ蓄積時のメモリ使用量
- 統計計算の計算量最適化
- ガベージコレクションの影響最小化

### 並行処理対応
- 複数スレッドからの統計更新
- lockメカニズムの適切な実装
- デッドロック回避

### 記録の重要性
- 統計アルゴリズムの選択根拠を記録
- パフォーマンステスト結果を数値で保存
- メモリ使用量の推移を詳細に記録

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

### 開発手法
- `documents/development_methodology/development-methodology.md`

### 既存テスト参照
- `integration_TC121.md`（Step3-6完全サイクルテスト）
- `integration_TC115.md`（Step3-5完全サイクルテスト）

### C#統計ライブラリ参照
- System.Linq（平均、最小、最大計算）
- Math クラス（標準偏差計算）

---

以上の指示に従って、TC122_FullCycle_複数サイクル実行時統計累積統合テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。