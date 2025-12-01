using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// MultiPlcCoordinator のユニットテスト
/// </summary>
public class MultiPlcCoordinatorTests
{
    /// <summary>
    /// TC030: 並列実行の基本動作確認（3台のPLCを並列実行）
    /// </summary>
    [Fact]
    public async Task TC030_ExecuteParallelAsync_3台並列実行_全成功()
    {
        // Arrange
        var plcConfigs = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig
            {
                PlcId = "PLC_A",
                PlcName = "テストPLC_A",
                IPAddress = "192.168.1.10",
                Port = 8192,
                Priority = 8
            },
            new PlcConnectionConfig
            {
                PlcId = "PLC_B",
                PlcName = "テストPLC_B",
                IPAddress = "192.168.1.11",
                Port = 8192,
                Priority = 5
            },
            new PlcConnectionConfig
            {
                PlcId = "PLC_C",
                PlcName = "テストPLC_C",
                IPAddress = "192.168.1.12",
                Port = 8192,
                Priority = 3
            }
        };

        var parallelConfig = new ParallelProcessingConfig
        {
            EnableParallel = true,
            MaxDegreeOfParallelism = 3,
            OverallTimeoutMs = 10000
        };

        // モック実行関数（常に成功）
        Func<PlcConnectionConfig, CancellationToken, Task<PlcExecutionResult>> mockExecute =
            async (config, token) =>
            {
                await Task.Delay(100, token); // 通信をシミュレート
                return new PlcExecutionResult
                {
                    PlcId = config.PlcId,
                    PlcName = config.PlcName,
                    IsSuccess = true,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddMilliseconds(100),
                    Duration = TimeSpan.FromMilliseconds(100)
                };
            };

        // Act
        var results = await MultiPlcCoordinator.ExecuteParallelAsync(
            plcConfigs,
            parallelConfig,
            mockExecute
        );

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Contains(results, r => r.PlcId == "PLC_A");
        Assert.Contains(results, r => r.PlcId == "PLC_B");
        Assert.Contains(results, r => r.PlcId == "PLC_C");
    }

    /// <summary>
    /// TC031: 順次実行の基本動作確認（2台のPLCを順次実行）
    /// </summary>
    [Fact]
    public async Task TC031_ExecuteSequentialAsync_2台順次実行_全成功()
    {
        // Arrange
        var plcConfigs = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig
            {
                PlcId = "PLC_A",
                PlcName = "テストPLC_A",
                IPAddress = "192.168.1.10",
                Port = 8192
            },
            new PlcConnectionConfig
            {
                PlcId = "PLC_B",
                PlcName = "テストPLC_B",
                IPAddress = "192.168.1.11",
                Port = 8192
            }
        };

        var executionOrder = new List<string>();

        // モック実行関数（実行順序を記録）
        Func<PlcConnectionConfig, CancellationToken, Task<PlcExecutionResult>> mockExecute =
            async (config, token) =>
            {
                executionOrder.Add(config.PlcId);
                await Task.Delay(50, token);
                return new PlcExecutionResult
                {
                    PlcId = config.PlcId,
                    PlcName = config.PlcName,
                    IsSuccess = true,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddMilliseconds(50),
                    Duration = TimeSpan.FromMilliseconds(50)
                };
            };

        // Act
        var results = await MultiPlcCoordinator.ExecuteSequentialAsync(
            plcConfigs,
            mockExecute
        );

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.IsSuccess));

        // 順次実行の確認（実行順序が設定順と一致）
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal("PLC_A", executionOrder[0]);
        Assert.Equal("PLC_B", executionOrder[1]);
    }

    /// <summary>
    /// ParallelProcessingConfig のデフォルト値確認
    /// </summary>
    [Fact]
    public void ParallelProcessingConfig_デフォルト値_正常()
    {
        // Arrange & Act
        var config = new ParallelProcessingConfig();

        // Assert
        Assert.True(config.EnableParallel);
        Assert.Equal(0, config.MaxDegreeOfParallelism);
        Assert.Equal(30000, config.OverallTimeoutMs);
    }

    /// <summary>
    /// PlcConnectionConfig の Priority プロパティ確認
    /// </summary>
    [Fact]
    public void PlcConnectionConfig_Priority_正常設定()
    {
        // Arrange & Act
        var config = new PlcConnectionConfig
        {
            PlcId = "PLC_001",
            PlcName = "ライン1",
            Priority = 8
        };

        // Assert
        Assert.Equal("PLC_001", config.PlcId);
        Assert.Equal("ライン1", config.PlcName);
        Assert.Equal(8, config.Priority);
    }

    /// <summary>
    /// TC033: 並列実行で1台が失敗した場合
    /// </summary>
    [Fact]
    public async Task TC033_ExecuteParallelAsync_1台失敗_部分成功()
    {
        // Arrange
        var plcConfigs = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig { PlcId = "PLC_A", PlcName = "成功PLC" },
            new PlcConnectionConfig { PlcId = "PLC_B", PlcName = "失敗PLC" }
        };

        var parallelConfig = new ParallelProcessingConfig
        {
            EnableParallel = true,
            OverallTimeoutMs = 5000
        };

        // モック実行関数（PLC_Bだけ失敗）
        Func<PlcConnectionConfig, CancellationToken, Task<PlcExecutionResult>> mockExecute =
            async (config, token) =>
            {
                await Task.Delay(50, token);
                return new PlcExecutionResult
                {
                    PlcId = config.PlcId,
                    PlcName = config.PlcName,
                    IsSuccess = config.PlcId != "PLC_B",
                    ErrorMessage = config.PlcId == "PLC_B" ? "接続タイムアウト" : null,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddMilliseconds(50),
                    Duration = TimeSpan.FromMilliseconds(50)
                };
            };

        // Act
        var results = await MultiPlcCoordinator.ExecuteParallelAsync(
            plcConfigs,
            parallelConfig,
            mockExecute
        );

        // Assert
        Assert.Equal(2, results.Count);

        var successResult = results.First(r => r.PlcId == "PLC_A");
        Assert.True(successResult.IsSuccess);
        Assert.Null(successResult.ErrorMessage);

        var failureResult = results.First(r => r.PlcId == "PLC_B");
        Assert.False(failureResult.IsSuccess);
        Assert.Equal("接続タイムアウト", failureResult.ErrorMessage);
    }

    /// <summary>
    /// TC034: 優先度順に実行されることを確認
    /// </summary>
    [Fact]
    public async Task TC034_ExecuteParallelAsync_優先度順実行確認()
    {
        // Arrange
        var plcConfigs = new List<PlcConnectionConfig>
        {
            new PlcConnectionConfig { PlcId = "PLC_Low", Priority = 1 },
            new PlcConnectionConfig { PlcId = "PLC_High", Priority = 10 },
            new PlcConnectionConfig { PlcId = "PLC_Mid", Priority = 5 }
        };

        var parallelConfig = new ParallelProcessingConfig
        {
            EnableParallel = true,
            OverallTimeoutMs = 5000
        };

        var startOrder = new List<string>();
        var lockObj = new object();

        // モック実行関数（開始順序を記録）
        Func<PlcConnectionConfig, CancellationToken, Task<PlcExecutionResult>> mockExecute =
            async (config, token) =>
            {
                lock (lockObj)
                {
                    startOrder.Add(config.PlcId);
                }
                await Task.Delay(10, token);
                return new PlcExecutionResult
                {
                    PlcId = config.PlcId,
                    IsSuccess = true,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow,
                    Duration = TimeSpan.FromMilliseconds(10)
                };
            };

        // Act
        var results = await MultiPlcCoordinator.ExecuteParallelAsync(
            plcConfigs,
            parallelConfig,
            mockExecute
        );

        // Assert
        Assert.Equal(3, results.Count);

        // 優先度順（降順）に開始されることを確認
        Assert.Equal("PLC_High", startOrder[0]);  // Priority=10
        Assert.Equal("PLC_Mid", startOrder[1]);   // Priority=5
        Assert.Equal("PLC_Low", startOrder[2]);   // Priority=1
    }
}
