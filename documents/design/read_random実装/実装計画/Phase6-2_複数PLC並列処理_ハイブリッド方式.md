# Phase6-2: 複数PLC並列処理の実装（ハイブリッド方式）

## ステータス
⚠️ **未着手** - Phase6完了後に実施
🔄 **設計変更** - ハイブリッド方式採用 (2025-11-21)

## 概要
複数台のPLCを並列処理で効率的に監視・データ取得する機能を実装します。
ExecutionOrchestrator拡張 + 軽量なMultiPlcCoordinatorヘルパーによるハイブリッド方式を採用。

### 設計方針（ハイブリッド方式）

#### 採用理由
1. **最小限の機能実装**: 新規コード約100行、重複なし
2. **既存設計との整合性**: クラス設計.mdのExecutionOrchestrator中心構造を維持
3. **動作の安定性**: 既存のPlcCommunicationManagerを再利用

#### アーキテクチャ
```
ExecutionOrchestrator (既存拡張)
├── ExecuteSingleCycleAsync() (既存: 単一PLC用)
└── ExecuteMultiPlcCycleAsync() (新規: 複数PLC用)
    └── MultiPlcCoordinator.ExecuteParallelAsync()を呼び出し

MultiPlcCoordinator (新規ヘルパー、50行)
├── ExecuteParallelAsync() (Task.WhenAllラッパー)
└── ExecuteSequentialAsync() (順次処理)

PlcCommunicationManager (既存、変更なし)
└── ExecuteStep3to5CycleAsync() (既存メソッドを再利用)
```

#### 責務分担
| クラス | 責務 | 変更 |
|--------|------|------|
| **ExecutionOrchestrator** | サイクル実行制御、単一/複数の振り分け | +50行 |
| **MultiPlcCoordinator** | Task.WhenAllでの並列実行調整のみ | 新規50行 |
| **PlcCommunicationManager** | 単一PLC通信 | 変更なし |

## 前提条件
- ✅ Phase4完了: PlcCommunicationManager統合済み
- ✅ Phase5完了: ReadRandomレスポンスパース実装済み
- ✅ Phase6完了: 複数PLC設定対応済み

## 実装内容

### 1. 複数PLC設定モデル（既存利用）

**ファイル**: `andon/Core/Models/ConfigModels/MultiPlcConfig.cs` (既存)

```csharp
public class MultiPlcConfig
{
    public List<PlcConnectionConfig> PlcConnections { get; set; } = new();
    public ParallelProcessingConfig ParallelConfig { get; set; } = new();
}

public class ParallelProcessingConfig
{
    public bool EnableParallel { get; set; } = true;
    public int MaxDegreeOfParallelism { get; set; } = 0;
    public int OverallTimeoutMs { get; set; } = 30000;
}
```

### 2. MultiPlcCoordinator 実装（新規、軽量ヘルパー）

**ファイル**: `andon/Core/Managers/MultiPlcCoordinator.cs` (新規作成、約50行)

**責務**: 複数PLCの並列実行調整のみ

```csharp
namespace Andon.Core.Managers;

/// <summary>
/// 複数PLC並列実行調整ヘルパー（軽量クラス）
/// </summary>
public class MultiPlcCoordinator
{
    /// <summary>
    /// 並列実行（Task.WhenAll）
    /// </summary>
    public static async Task<List<PlcExecutionResult>> ExecuteParallelAsync(
        List<PlcConnectionConfig> plcConfigs,
        ParallelProcessingConfig parallelConfig,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PlcExecutionResult>();
        var tasks = new List<Task<PlcExecutionResult>>();

        // タイムアウト設定
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(parallelConfig.OverallTimeoutMs);

        // 各PLC用のタスク生成
        foreach (var plcConfig in plcConfigs.OrderByDescending(p => p.Priority))
        {
            tasks.Add(ExecuteSinglePlcAsync(plcConfig, cts.Token));
        }

        // 並列実行
        var taskResults = await Task.WhenAll(tasks);
        results.AddRange(taskResults);

        return results;
    }

    /// <summary>
    /// 順次実行（ConMoni3互換）
    /// </summary>
    public static async Task<List<PlcExecutionResult>> ExecuteSequentialAsync(
        List<PlcConnectionConfig> plcConfigs,
        CancellationToken cancellationToken = default)
    {
        var results = new List<PlcExecutionResult>();

        foreach (var plcConfig in plcConfigs)
        {
            var result = await ExecuteSinglePlcAsync(plcConfig, cancellationToken);
            results.Add(result);
            await Task.Delay(10, cancellationToken); // スロットリング
        }

        return results;
    }

    /// <summary>
    /// 単一PLC処理（PlcCommunicationManagerを活用）
    /// </summary>
    private static async Task<PlcExecutionResult> ExecuteSinglePlcAsync(
        PlcConnectionConfig plcConfig,
        CancellationToken cancellationToken)
    {
        var result = new PlcExecutionResult
        {
            PlcId = plcConfig.PlcId,
            PlcName = plcConfig.PlcName,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // 既存のPlcCommunicationManagerを使用
            var manager = new PlcCommunicationManager();

            var connectionConfig = new ConnectionConfig
            {
                IpAddress = plcConfig.IPAddress,
                Port = plcConfig.Port,
                UseTcp = plcConfig.ConnectionMethod == "TCP"
            };

            var timeoutConfig = new TimeoutConfig
            {
                ConnectTimeoutMs = plcConfig.Timeout,
                SendTimeoutMs = plcConfig.Timeout,
                ReceiveTimeoutMs = plcConfig.Timeout
            };

            // フレーム構築（既存ユーティリティ使用）
            var devices = plcConfig.Devices.Select(d => d.ToDeviceSpecification()).ToList();
            var frame = SlmpFrameBuilder.BuildReadRandomRequest(
                devices,
                plcConfig.FrameVersion,
                (ushort)(plcConfig.Timeout / 250)
            );

            // 通信実行（既存メソッド活用）
            var cycleResult = await manager.ExecuteStep3to5CycleAsync(
                connectionConfig,
                timeoutConfig,
                frame,
                cancellationToken
            );

            result.IsSuccess = cycleResult.IsSuccess;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            result.DeviceData = cycleResult.ReceiveResult?.RawData;
            result.ErrorMessage = cycleResult.ErrorMessage;
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"エラー: {ex.Message}";
            result.Exception = ex;
        }

        return result;
    }
}
```

### 3. ExecutionOrchestrator 拡張

**ファイル**: `andon/Core/Controllers/ExecutionOrchestrator.cs` (既存拡張)

**追加メソッド**: `ExecuteMultiPlcCycleAsync()`

```csharp
public class ExecutionOrchestrator
{
    // 既存メソッド（変更なし）
    public async Task<CycleExecutionResult> ExecuteSingleCycleAsync(...) { ... }

    // ✅ 新規追加: 複数PLC並列実行
    /// <summary>
    /// 複数PLCサイクル実行（並列/順次）
    /// </summary>
    public async Task<MultiPlcExecutionResult> ExecuteMultiPlcCycleAsync(
        MultiPlcConfig config,
        CancellationToken cancellationToken = default)
    {
        var overallStartTime = DateTime.UtcNow;
        List<PlcExecutionResult> plcResults;

        // 並列 vs 順次処理の振り分け
        if (config.ParallelConfig.EnableParallel)
        {
            plcResults = await MultiPlcCoordinator.ExecuteParallelAsync(
                config.PlcConnections,
                config.ParallelConfig,
                cancellationToken
            );
        }
        else
        {
            plcResults = await MultiPlcCoordinator.ExecuteSequentialAsync(
                config.PlcConnections,
                cancellationToken
            );
        }

        // 結果集計
        var result = new MultiPlcExecutionResult
        {
            StartTime = overallStartTime,
            EndTime = DateTime.UtcNow,
            PlcResults = plcResults.ToDictionary(r => r.PlcId, r => r),
            SuccessCount = plcResults.Count(r => r.IsSuccess),
            FailureCount = plcResults.Count(r => !r.IsSuccess),
            IsSuccess = plcResults.All(r => r.IsSuccess)
        };

        result.TotalDuration = result.EndTime - result.StartTime;
        return result;
    }
}
```

### 4. 実行結果モデル（既存利用 + 拡張）

**ファイル**: `andon/Core/Models/MultiPlcExecutionResult.cs` (新規作成)

```csharp
namespace Andon.Core.Models;

/// <summary>
/// 複数PLC実行結果
/// </summary>
public class MultiPlcExecutionResult
{
    public bool IsSuccess { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, PlcExecutionResult> PlcResults { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 単一PLC実行結果
/// </summary>
public class PlcExecutionResult
{
    public string PlcId { get; set; } = string.Empty;
    public string PlcName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public byte[]? DeviceData { get; set; }
    public Dictionary<string, DeviceData>? ParsedDeviceData { get; set; }
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}
```

## テスト実装

### TC030: MultiPlcCoordinator並列処理テスト

**ファイル**: `andon/Tests/Unit/Core/Managers/MultiPlcCoordinatorTests.cs`

```csharp
public class MultiPlcCoordinatorTests
{
    [Fact]
    public async Task TC030_ExecuteParallelAsync_3台並列_全成功()
    {
        // Arrange
        var plcConfigs = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig { PlcId = "PLC_A", IPAddress = "127.0.0.1", Port = 8192 },
            new PlcConnectionConfig { PlcId = "PLC_B", IPAddress = "127.0.0.1", Port = 8193 },
            new PlcConnectionConfig { PlcId = "PLC_C", IPAddress = "127.0.0.1", Port = 8194 }
        };
        var parallelConfig = new ParallelProcessingConfig
        {
            EnableParallel = true,
            MaxDegreeOfParallelism = 3
        };

        // Act
        var results = await MultiPlcCoordinator.ExecuteParallelAsync(
            plcConfigs,
            parallelConfig
        );

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));
    }

    [Fact]
    public async Task TC031_ExecuteSequentialAsync_順次処理_全成功()
    {
        // Arrange
        var plcConfigs = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig { PlcId = "PLC_A" },
            new PlcConnectionConfig { PlcId = "PLC_B" }
        };

        // Act
        var results = await MultiPlcCoordinator.ExecuteSequentialAsync(plcConfigs);

        // Assert
        Assert.Equal(2, results.Count);
    }
}
```

### TC032: ExecutionOrchestrator複数PLC実行テスト

**ファイル**: `andon/Tests/Unit/Core/Controllers/ExecutionOrchestratorTests.cs`

```csharp
[Fact]
public async Task TC032_ExecuteMultiPlcCycleAsync_並列実行()
{
    // Arrange
    var config = new MultiPlcConfig
    {
        ParallelConfig = new ParallelProcessingConfig { EnableParallel = true },
        PlcConnections = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig { PlcId = "PLC_A" },
            new PlcConnectionConfig { PlcId = "PLC_B" }
        }
    };
    var orchestrator = new ExecutionOrchestrator();

    // Act
    var result = await orchestrator.ExecuteMultiPlcCycleAsync(config);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.SuccessCount);
}
```

## 完了条件

- [x] MultiPlcConfig モデル（既存確認）
- [ ] MultiPlcCoordinator 実装（新規50行）
- [ ] ExecutionOrchestrator 拡張（+50行）
- [ ] MultiPlcExecutionResult モデル実装
- [ ] TC030/TC031/TC032 テスト実装・実行（全PASSED）
- [ ] appsettings.json 複数PLC設定追加
- [ ] パフォーマンステスト（10台並列処理）

## 実装規模

| 項目 | 内容 |
|-----|------|
| **新規クラス** | MultiPlcCoordinator (50行) |
| **拡張クラス** | ExecutionOrchestrator (+50行) |
| **新規モデル** | MultiPlcExecutionResult, PlcExecutionResult |
| **テスト** | 3テスト + パフォーマンステスト |
| **合計新規コード** | 約100行 |

## 変更履歴

| 日付 | 変更内容 |
|------|---------|
| 2025-11-21 | ハイブリッド方式採用（ExecutionOrchestrator拡張 + MultiPlcCoordinator） |

---

**作成日**: 2025-11-21
**参考**: Phase6-2_複数PLC並列処理.md（当初案）、クラス設計.md
