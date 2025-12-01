using Xunit;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Tests.Unit.Core.Models;

/// <summary>
/// ProcessedDeviceRequestInfo のユニットテスト
/// Phase8.5: DeviceSpecificationsプロパティ追加のTDDテスト
/// </summary>
public class ProcessedDeviceRequestInfoTests
{
    /// <summary>
    /// Test Case 1-1: DeviceSpecificationsプロパティの追加
    /// DeviceSpecificationsがnullableなListとして動作することを確認
    /// </summary>
    [Fact]
    public void DeviceSpecifications_Should_BeNullableList()
    {
        // Arrange & Act
        var info = new ProcessedDeviceRequestInfo
        {
            DeviceSpecifications = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100),
                new DeviceSpecification(DeviceCode.M, 200)
            }
        };

        // Assert
        Assert.NotNull(info.DeviceSpecifications);
        Assert.Equal(2, info.DeviceSpecifications.Count);
        Assert.Equal(DeviceCode.D, info.DeviceSpecifications[0].Code);
    }

    /// <summary>
    /// Test Case 1-2: 後方互換性の確認
    /// DeviceSpecificationsがnullでも動作することを確認（後方互換性）
    /// </summary>
    [Fact]
    public void DeviceSpecifications_Should_AllowNull_ForBackwardCompatibility()
    {
        // Arrange & Act
        var info = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 100,
            Count = 10
        };

        // Assert
        Assert.Null(info.DeviceSpecifications);
        Assert.Equal("D", info.DeviceType); // 既存プロパティは残す
    }
}
