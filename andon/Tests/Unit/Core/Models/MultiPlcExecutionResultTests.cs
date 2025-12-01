using Xunit;
using Andon.Core.Models;
using System;
using System.Collections.Generic;

namespace Andon.Tests.Unit.Core.Models;

/// <summary>
/// MultiPlcExecutionResult および PlcExecutionResult のユニットテスト
/// </summary>
public class MultiPlcExecutionResultTests
{
    [Fact]
    public void PlcExecutionResult_初期化_正常()
    {
        // Arrange & Act
        var result = new PlcExecutionResult
        {
            PlcId = "PLC_001",
            PlcName = "ライン1_設備A",
            IsSuccess = true,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMilliseconds(500)
        };
        result.Duration = result.EndTime - result.StartTime;

        // Assert
        Assert.Equal("PLC_001", result.PlcId);
        Assert.Equal("ライン1_設備A", result.PlcName);
        Assert.True(result.IsSuccess);
        Assert.True(result.Duration.TotalMilliseconds >= 500);
    }

    [Fact]
    public void PlcExecutionResult_エラー情報_正常設定()
    {
        // Arrange & Act
        var exception = new Exception("接続タイムアウト");
        var result = new PlcExecutionResult
        {
            PlcId = "PLC_002",
            IsSuccess = false,
            ErrorMessage = "接続失敗",
            Exception = exception
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("接続失敗", result.ErrorMessage);
        Assert.NotNull(result.Exception);
        Assert.Equal("接続タイムアウト", result.Exception.Message);
    }

    [Fact]
    public void MultiPlcExecutionResult_初期化_正常()
    {
        // Arrange & Act
        var result = new MultiPlcExecutionResult
        {
            IsSuccess = true,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(2),
            SuccessCount = 3,
            FailureCount = 0
        };
        result.TotalDuration = result.EndTime - result.StartTime;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.True(result.TotalDuration.TotalSeconds >= 2);
    }

    [Fact]
    public void MultiPlcExecutionResult_複数PLC結果_Dictionary格納()
    {
        // Arrange
        var plc1 = new PlcExecutionResult { PlcId = "PLC_A", IsSuccess = true };
        var plc2 = new PlcExecutionResult { PlcId = "PLC_B", IsSuccess = true };
        var plc3 = new PlcExecutionResult { PlcId = "PLC_C", IsSuccess = false };

        // Act
        var result = new MultiPlcExecutionResult
        {
            PlcResults = new Dictionary<string, PlcExecutionResult>
            {
                { "PLC_A", plc1 },
                { "PLC_B", plc2 },
                { "PLC_C", plc3 }
            },
            SuccessCount = 2,
            FailureCount = 1,
            IsSuccess = false // 1台でも失敗したら全体も失敗
        };

        // Assert
        Assert.Equal(3, result.PlcResults.Count);
        Assert.True(result.PlcResults["PLC_A"].IsSuccess);
        Assert.True(result.PlcResults["PLC_B"].IsSuccess);
        Assert.False(result.PlcResults["PLC_C"].IsSuccess);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(1, result.FailureCount);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void MultiPlcExecutionResult_全成功_IsSuccessTrue()
    {
        // Arrange
        var plc1 = new PlcExecutionResult { PlcId = "PLC_A", IsSuccess = true };
        var plc2 = new PlcExecutionResult { PlcId = "PLC_B", IsSuccess = true };

        // Act
        var result = new MultiPlcExecutionResult
        {
            PlcResults = new Dictionary<string, PlcExecutionResult>
            {
                { "PLC_A", plc1 },
                { "PLC_B", plc2 }
            },
            SuccessCount = 2,
            FailureCount = 0,
            IsSuccess = true
        };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
    }

    [Fact]
    public void MultiPlcExecutionResult_エラーメッセージ_正常設定()
    {
        // Arrange & Act
        var result = new MultiPlcExecutionResult
        {
            IsSuccess = false,
            ErrorMessage = "複数のPLCで接続エラーが発生しました",
            SuccessCount = 1,
            FailureCount = 2
        };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("複数のPLCで接続エラーが発生しました", result.ErrorMessage);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(2, result.FailureCount);
    }
}
