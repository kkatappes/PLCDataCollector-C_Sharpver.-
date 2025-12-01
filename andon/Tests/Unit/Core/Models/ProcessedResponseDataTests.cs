using Xunit;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Models;

/// <summary>
/// ProcessedResponseDataクラスのテスト（Phase5 Step15）
/// </summary>
public class ProcessedResponseDataTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        // Act
        var responseData = new ProcessedResponseData();

        // Assert
        Assert.NotNull(responseData.ProcessedData);
        Assert.Empty(responseData.ProcessedData);
        Assert.NotNull(responseData.Errors);
        Assert.Empty(responseData.Errors);
        Assert.NotNull(responseData.Warnings);
        Assert.Empty(responseData.Warnings);
        Assert.Equal(string.Empty, responseData.OriginalRawData);
    }

    [Fact]
    public void ProcessedData_AddDeviceData_UpdatesStatistics()
    {
        // Arrange
        var responseData = new ProcessedResponseData();
        var bitDevice = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.M, 0), 1);
        var wordDevice = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.D, 100), 1234);
        var dwordDevice = DeviceData.FromDWordDevice(new DeviceSpecification(DeviceCode.D, 200), 0x1234, 0x5678);

        // Act
        responseData.ProcessedData["M0"] = bitDevice;
        responseData.ProcessedData["D100"] = wordDevice;
        responseData.ProcessedData["D200"] = dwordDevice;

        // Assert
        Assert.Equal(3, responseData.TotalProcessedDevices);
        Assert.Equal(1, responseData.BitDeviceCount);
        Assert.Equal(1, responseData.WordDeviceCount);
        Assert.Equal(1, responseData.DWordDeviceCount);
    }

    [Fact]
    public void GetDeviceValue_ExistingDevice_ReturnsValue()
    {
        // Arrange
        var responseData = new ProcessedResponseData();
        var device = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.D, 100), 1234);
        responseData.ProcessedData["D100"] = device;

        // Act
        var value = responseData.GetDeviceValue("D100");

        // Assert
        Assert.NotNull(value);
        Assert.Equal(1234u, value);
    }

    [Fact]
    public void GetDeviceValue_NonExistingDevice_ReturnsNull()
    {
        // Arrange
        var responseData = new ProcessedResponseData();

        // Act
        var value = responseData.GetDeviceValue("D999");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetBitDevices_ReturnsBitDevicesOnly()
    {
        // Arrange
        var responseData = new ProcessedResponseData();
        responseData.ProcessedData["M0"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.M, 0), 1);
        responseData.ProcessedData["M16"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.M, 16), 0);
        responseData.ProcessedData["D100"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.D, 100), 1234);

        // Act
        var bitDevices = responseData.GetBitDevices();

        // Assert
        Assert.Equal(2, bitDevices.Count);
        Assert.Contains("M0", bitDevices);
        Assert.Contains("M16", bitDevices);
        Assert.DoesNotContain("D100", bitDevices);
    }

    [Fact]
    public void GetWordDevices_ReturnsWordDevicesOnly()
    {
        // Arrange
        var responseData = new ProcessedResponseData();
        responseData.ProcessedData["D100"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.D, 100), 1234);
        responseData.ProcessedData["D101"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.D, 101), 5678);
        responseData.ProcessedData["M0"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.M, 0), 1);
        responseData.ProcessedData["D200"] = DeviceData.FromDWordDevice(new DeviceSpecification(DeviceCode.D, 200), 0x1234, 0x5678);

        // Act
        var wordDevices = responseData.GetWordDevices();

        // Assert
        Assert.Equal(2, wordDevices.Count);
        Assert.Contains("D100", wordDevices);
        Assert.Contains("D101", wordDevices);
        Assert.DoesNotContain("M0", wordDevices);
        Assert.DoesNotContain("D200", wordDevices);
    }

    [Fact]
    public void GetDWordDevices_ReturnsDWordDevicesOnly()
    {
        // Arrange
        var responseData = new ProcessedResponseData();
        responseData.ProcessedData["D200"] = DeviceData.FromDWordDevice(new DeviceSpecification(DeviceCode.D, 200), 0x1234, 0x5678);
        responseData.ProcessedData["D202"] = DeviceData.FromDWordDevice(new DeviceSpecification(DeviceCode.D, 202), 0xABCD, 0xEF01);
        responseData.ProcessedData["D100"] = DeviceData.FromDeviceSpecification(new DeviceSpecification(DeviceCode.D, 100), 1234);

        // Act
        var dwordDevices = responseData.GetDWordDevices();

        // Assert
        Assert.Equal(2, dwordDevices.Count);
        Assert.Contains("D200", dwordDevices);
        Assert.Contains("D202", dwordDevices);
        Assert.DoesNotContain("D100", dwordDevices);
    }

    [Fact]
    public void IsSuccess_CanBeSetAndRetrieved()
    {
        // Arrange
        var responseData = new ProcessedResponseData
        {
            IsSuccess = true
        };

        // Assert
        Assert.True(responseData.IsSuccess);
    }

    [Fact]
    public void Errors_CanBeAddedAndRetrieved()
    {
        // Arrange
        var responseData = new ProcessedResponseData();

        // Act
        responseData.Errors.Add("Test error 1");
        responseData.Errors.Add("Test error 2");

        // Assert
        Assert.Equal(2, responseData.Errors.Count);
        Assert.Contains("Test error 1", responseData.Errors);
        Assert.Contains("Test error 2", responseData.Errors);
    }

    [Fact]
    public void FrameType_DefaultsTo3E()
    {
        // Arrange & Act
        var responseData = new ProcessedResponseData();

        // Assert
        Assert.Equal(FrameType.Frame3E, responseData.FrameType);
    }

    [Fact]
    public void FrameType_CanBeSetTo4E()
    {
        // Arrange
        var responseData = new ProcessedResponseData
        {
            FrameType = FrameType.Frame4E
        };

        // Assert
        Assert.Equal(FrameType.Frame4E, responseData.FrameType);
    }

    [Fact]
    public void ProcessedAt_CanBeSetAndRetrieved()
    {
        // Arrange
        var now = DateTime.Now;
        var responseData = new ProcessedResponseData
        {
            ProcessedAt = now
        };

        // Assert
        Assert.Equal(now, responseData.ProcessedAt);
    }

    [Fact]
    public void ProcessingTimeMs_CanBeSetAndRetrieved()
    {
        // Arrange
        var responseData = new ProcessedResponseData
        {
            ProcessingTimeMs = 123
        };

        // Assert
        Assert.Equal(123, responseData.ProcessingTimeMs);
    }
}
