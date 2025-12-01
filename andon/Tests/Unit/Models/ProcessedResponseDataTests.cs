using Xunit;
using Andon.Core.Models;
using Andon.Core.Constants;
using System.Collections.Generic;

namespace Andon.Tests.Unit.Models;

/// <summary>
/// ProcessedResponseDataのテスト（Phase5 Step15）
/// 旧構造互換性（BasicProcessedDevices/CombinedDWordDevices）のテスト
/// </summary>
public class ProcessedResponseDataTests
{
    [Fact]
    public void BasicProcessedDevices_WithWordDevices_ReturnsCompatibleList()
    {
        // Arrange
        var responseData = new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["D100"] = new DeviceData
                {
                    DeviceName = "D100",
                    Code = DeviceCode.D,
                    Address = 100,
                    Value = 1234,
                    IsDWord = false
                },
                ["D200"] = new DeviceData
                {
                    DeviceName = "D200",
                    Code = DeviceCode.D,
                    Address = 200,
                    Value = 5678,
                    IsDWord = false
                }
            }
        };

        // Act
        var basicDevices = responseData.BasicProcessedDevices;

        // Assert
        Assert.NotNull(basicDevices);
        Assert.Equal(2, basicDevices.Count);

        var d100 = basicDevices.Find(d => d.DeviceName == "D100");
        Assert.NotNull(d100);
        Assert.Equal(1234, d100.RawValue);
    }

    [Fact]
    public void CombinedDWordDevices_WithDWordDevices_ReturnsCompatibleList()
    {
        // Arrange
        var responseData = new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["D100"] = new DeviceData
                {
                    DeviceName = "D100",
                    Code = DeviceCode.D,
                    Address = 100,
                    Value = 0x12345678,  // 32bit値
                    IsDWord = true
                }
            }
        };

        // Act
        var dwordDevices = responseData.CombinedDWordDevices;

        // Assert
        Assert.NotNull(dwordDevices);
        Assert.Single(dwordDevices);

        var d100 = dwordDevices[0];
        Assert.Equal("D100", d100.DeviceName);
        Assert.Equal(0x12345678u, d100.CombinedValue);
        Assert.Equal(0x5678, d100.LowWordValue);
        Assert.Equal(0x1234, d100.HighWordValue);
    }

    [Fact]
    public void BasicProcessedDevices_FiltersDWordDevices_ReturnsOnlyWordAndBitDevices()
    {
        // Arrange
        var responseData = new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["D100"] = new DeviceData
                {
                    DeviceName = "D100",
                    Code = DeviceCode.D,
                    Address = 100,
                    Value = 1234,
                    IsDWord = false
                },
                ["D200"] = new DeviceData
                {
                    DeviceName = "D200",
                    Code = DeviceCode.D,
                    Address = 200,
                    Value = 0x12345678,
                    IsDWord = true  // DWordデバイス
                }
            }
        };

        // Act
        var basicDevices = responseData.BasicProcessedDevices;

        // Assert
        Assert.Single(basicDevices);
        Assert.Equal("D100", basicDevices[0].DeviceName);
    }
}
