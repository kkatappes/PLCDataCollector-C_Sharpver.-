# Phase1: 準備と基礎クラス実装

## 概要

Step2フレーム構築実装の第1フェーズとして、以下を実施します：

1. ProcessedDeviceRequestInfo.cs の削除（DWord分割機能廃止）
2. SequenceNumberManager.cs の新規作成
3. SequenceNumberManagerTests.cs の作成とテスト実施

## 実装目標

- DWord分割機能の完全廃止（設計方針）
- 4Eフレーム用シーケンス番号自動管理機能の実装
- スレッドセーフなシーケンス番号管理

---

## 1. ProcessedDeviceRequestInfo.cs 削除作業

### 削除対象ファイル

- **ファイルパス**: `andon/Core/Models/ProcessedDeviceRequestInfo.cs`

### 削除理由

- DWord分割機能の完全廃止（設計方針）
- ReadRandom方式では `List<DeviceSpecification>` のみでデバイス指定が完結
- 実装複雑性の削減、保守性の向上

### 影響範囲確認

**削除前に以下を確認:**

```bash
# ProcessedDeviceRequestInfoの使用箇所を確認
grep -r "ProcessedDeviceRequestInfo" andon/Core/ andon/Tests/
```

**主な影響箇所:**

1. `andon/Core/Interfaces/IPlcCommunicationManager.cs`
   - ProcessReceivedRawData (49行目)
   - CombineDwordData (62行目)
   - ParseRawToStructuredData (75行目)

2. `andon/Core/Managers/PlcCommunicationManager.cs`
   - ProcessReceivedRawData (880行目)
   - CombineDwordData (1133行目)
   - ParseRawToStructuredData (2222行目)
   - ExecuteFullCycleAsync (2750行目)
   - privateメソッド群（ExtractDeviceValues, ExtractWordDevices等）

3. テストファイル群
   - `Tests/Integration/Step3_6_IntegrationTests.cs`
   - `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`

### 削除作業手順

**注意**: この削除作業は、PlcCommunicationManager の大規模リファクタリングを伴います。Phase1では削除のみを行い、代替実装はStep3-6実装時に行います。

**Phase1での対応:**

1. **保留判断**: ProcessedDeviceRequestInfo の削除は、PlcCommunicationManager の全体的なリファクタリングが必要なため、Phase1では実施しません。

2. **今後の対応**: Step3-6（PLC通信実装）の際に、以下の方針で対応:
   ```csharp
   // メソッドシグネチャを変更
   // Before
   public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
       byte[] rawData,
       ProcessedDeviceRequestInfo processedRequestInfo,  // 削除
       CancellationToken cancellationToken = default)

   // After
   public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
       byte[] rawData,
       List<DeviceSpecification> devices,  // シンプル化
       FrameType frameType,
       CancellationToken cancellationToken = default)
   ```

---

## 2. SequenceNumberManager.cs 新規作成

### ファイルパス

- `andon/Core/Managers/SequenceNumberManager.cs`

### 設計思想

- **PySLMPClientから採用**: 4Eフレーム用シーケンス番号自動管理
- **スレッドセーフ**: lockによる排他制御
- **自動ロールオーバー**: 0xFF超過時に0にリセット
- **3E/4E対応**: 3Eでは常に0、4Eでは自動インクリメント

### 実装コード

```csharp
namespace Andon.Core.Managers;

/// <summary>
/// シーケンス番号管理クラス
/// PySLMPClientから採用：4Eフレーム用シーケンス番号自動管理
/// </summary>
public class SequenceNumberManager
{
    private ushort _sequenceNumber = 0;
    private readonly object _lock = new object();

    /// <summary>
    /// 次のシーケンス番号を取得します。
    /// </summary>
    /// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
    /// <returns>シーケンス番号（3Eの場合は常に0、4Eの場合は自動インクリメント）</returns>
    public ushort GetNext(string frameType)
    {
        // 3Eフレームでは常に0を返す
        if (frameType == "3E")
        {
            return 0;
        }

        // 4Eフレームでは自動インクリメント
        lock (_lock)
        {
            // PySLMPClient方式：0xFF超過時ロールオーバー
            // シーケンス番号は1バイト（0～255）の範囲で管理
            if (_sequenceNumber > 0xFF)
            {
                _sequenceNumber = 0;
            }

            ushort current = _sequenceNumber;
            _sequenceNumber++;
            return current;
        }
    }

    /// <summary>
    /// シーケンス番号をリセットします。
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _sequenceNumber = 0;
        }
    }

    /// <summary>
    /// 現在のシーケンス番号を取得します（テスト用）。
    /// </summary>
    /// <returns>現在のシーケンス番号</returns>
    public ushort GetCurrent()
    {
        lock (_lock)
        {
            return _sequenceNumber;
        }
    }
}
```

### 実装のポイント

1. **スレッドセーフ性**:
   - `lock (_lock)` によるクリティカルセクション保護
   - 並行アクセス時の競合防止

2. **ロールオーバー処理**:
   - シーケンス番号が0xFF（255）を超えたら0にリセット
   - 長時間稼働時のオーバーフロー防止

3. **3E/4E分岐**:
   - 3Eフレームでは常に0を返却（シーケンス番号不使用）
   - 4Eフレームでのみインクリメント処理

4. **テスト容易性**:
   - `GetCurrent()` メソッドで現在値を確認可能
   - `Reset()` メソッドでテスト前に初期化可能

---

## 3. SequenceNumberManagerTests.cs 作成

### ファイルパス

- `Tests/Unit/Core/Managers/SequenceNumberManagerTests.cs`

### テストケース設計

#### 基本動作テスト

**TC001: 初期値が0であること**
```csharp
[Fact]
public void GetCurrent_初期状態_ゼロを返す()
{
    // Arrange
    var manager = new SequenceNumberManager();

    // Act
    var current = manager.GetCurrent();

    // Assert
    Assert.Equal(0, current);
}
```

**TC002: 3Eフレームでは常に0を返すこと**
```csharp
[Fact]
public void GetNext_3Eフレーム_常にゼロを返す()
{
    // Arrange
    var manager = new SequenceNumberManager();

    // Act
    var seq1 = manager.GetNext("3E");
    var seq2 = manager.GetNext("3E");
    var seq3 = manager.GetNext("3E");

    // Assert
    Assert.Equal(0, seq1);
    Assert.Equal(0, seq2);
    Assert.Equal(0, seq3);
}
```

**TC003: 4Eフレームで呼び出すたびにインクリメントされること**
```csharp
[Fact]
public void GetNext_4Eフレーム_インクリメントされる()
{
    // Arrange
    var manager = new SequenceNumberManager();

    // Act
    var seq1 = manager.GetNext("4E");
    var seq2 = manager.GetNext("4E");
    var seq3 = manager.GetNext("4E");

    // Assert
    Assert.Equal(0, seq1);
    Assert.Equal(1, seq2);
    Assert.Equal(2, seq3);
}
```

#### ロールオーバーテスト

**TC004: 0xFF超過時に0にリセットされること**
```csharp
[Fact]
public void GetNext_4Eフレーム_0xFF超過でロールオーバー()
{
    // Arrange
    var manager = new SequenceNumberManager();

    // Act
    // 256回呼び出して0xFFを超える
    for (int i = 0; i < 256; i++)
    {
        manager.GetNext("4E");
    }

    var afterRollover = manager.GetNext("4E");

    // Assert
    Assert.Equal(0, afterRollover);
}
```

#### スレッドセーフテスト

**TC005: 並行呼び出しでも正しくインクリメントされること**
```csharp
[Fact]
public void GetNext_並行呼び出し_スレッドセーフ()
{
    // Arrange
    var manager = new SequenceNumberManager();
    var results = new ConcurrentBag<ushort>();
    const int threadCount = 10;
    const int callsPerThread = 100;

    // Act
    var tasks = Enumerable.Range(0, threadCount)
        .Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < callsPerThread; i++)
            {
                var seq = manager.GetNext("4E");
                results.Add(seq);
            }
        }))
        .ToArray();

    Task.WaitAll(tasks);

    // Assert
    Assert.Equal(threadCount * callsPerThread, results.Count);
    Assert.Equal(results.Count, results.Distinct().Count()); // 重複なし
}
```

#### リセットテスト

**TC006: Reset()で0に戻ること**
```csharp
[Fact]
public void Reset_実行後_ゼロにリセットされる()
{
    // Arrange
    var manager = new SequenceNumberManager();
    manager.GetNext("4E");
    manager.GetNext("4E");
    manager.GetNext("4E");

    // Act
    manager.Reset();
    var afterReset = manager.GetCurrent();

    // Assert
    Assert.Equal(0, afterReset);
}
```

---

## 4. Phase1実装チェックリスト

### 実装タスク

- [ ] **SequenceNumberManager.cs 新規作成**
  - [ ] クラスファイル作成
  - [ ] GetNext() 実装
  - [ ] Reset() 実装
  - [ ] GetCurrent() 実装
  - [ ] スレッドセーフ処理実装

- [ ] **SequenceNumberManagerTests.cs 作成**
  - [ ] TC001: 初期値が0であること
  - [ ] TC002: 3Eフレームでは常に0を返すこと
  - [ ] TC003: 4Eフレームでインクリメントされること
  - [ ] TC004: 0xFF超過時にロールオーバー
  - [ ] TC005: 並行呼び出しでスレッドセーフ
  - [ ] TC006: Reset()で0に戻ること

- [ ] **テスト実行**
  - [ ] 全テストケースがパスすること
  - [ ] カバレッジ100%を確認

### 完了条件

1. SequenceNumberManager.cs が正しく実装されている
2. 全テストケースがパスしている
3. スレッドセーフ性が確認されている
4. ロールオーバー処理が正しく動作している

---

## 5. 次フェーズへの引き継ぎ事項

### Phase2への準備

Phase1完了後、以下をPhase2（SlmpFrameBuilder実装）に引き継ぎます：

1. **SequenceNumberManager の活用**:
   - SlmpFrameBuilder で `private static readonly SequenceNumberManager _sequenceManager = new();` として保持
   - 4Eフレーム構築時に `_sequenceManager.GetNext("4E")` を呼び出し

2. **ProcessedDeviceRequestInfo 削除の保留**:
   - Phase1では削除を保留（影響範囲が広いため）
   - Step3-6実装時に段階的に削減

---

## 実装時間見積もり

| タスク | 見積もり時間 |
|-------|------------|
| SequenceNumberManager.cs 実装 | 1時間 |
| SequenceNumberManagerTests.cs 実装 | 1-2時間 |
| テスト実行・デバッグ | 0.5時間 |
| **合計** | **2.5-3.5時間** |

---

## 参考資料

- `documents/design/Step2_フレーム構築実装/Step2_新設計_統合フレーム構築仕様.md` - 全体設計書
- PySLMPClient実装（シーケンス番号管理の参考）

---

## 7. 実装完了記録

**Phase1実装日**: 2025-11-27
**担当者**: Claude
**ステータス**: ✅ 実装完了

### 完了事項

- [x] SequenceNumberManager.cs 実装完了
- [x] SequenceNumberManagerTests.cs 実装完了（6テストケース）
- [x] 全テストケース合格（6/6件）
- [x] スレッドセーフ性確認完了
- [x] ロールオーバー処理動作確認完了
- [x] ProcessedDeviceRequestInfo削除判断記録完了（Phase1では保留）

### 実装判断事項

**ProcessedDeviceRequestInfo削除の保留**:
- **判断**: Phase1では削除を保留
- **理由**: PlcCommunicationManagerの大規模リファクタリングが必要、影響範囲が広い
- **対応時期**: Step3-6（PLC通信実装）時に段階的に実施

**並行呼び出しテストの調整**:
- **変更**: 呼び出し回数を1000回→200回に変更
- **理由**: 256超過時のロールオーバーにより重複が発生するため
- **結果**: スレッドセーフ性を確実に検証（200 < 256）

### Phase2への引き継ぎ

**SequenceNumberManagerの活用**:
- SlmpFrameBuilderで`private static readonly SequenceNumberManager _sequenceManager = new();`として保持
- 4Eフレーム構築時に`_sequenceManager.GetNext("4E")`を呼び出し

**実装結果詳細**: `documents/design/Step2_フレーム構築実装/実装結果/Phase1_SequenceNumberManager_TestResults.md`を参照

---

**Phase1実装日**: 2025-11-27
**担当者**: Claude
**ステータス**: ✅ 実装完了
