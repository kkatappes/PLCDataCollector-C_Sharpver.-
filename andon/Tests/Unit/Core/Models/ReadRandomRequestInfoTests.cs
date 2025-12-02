using Andon.Core.Models;
using Andon.Core.Constants;
using Xunit;

namespace Andon.Tests.Unit.Core.Models;

/// <summary>
/// ReadRandomRequestInfoクラスのテスト（Phase12恒久対策）
/// ReadRandom(0x0403)コマンド専用リクエスト情報
/// </summary>
public class ReadRandomRequestInfoTests
{
    /// <summary>
    /// TC12.1.1: デフォルトコンストラクタで正しく初期化される
    /// </summary>
    [Fact]
    public void Constructor_デフォルト値_正しく初期化される()
    {
        // Act
        var requestInfo = new ReadRandomRequestInfo();

        // Assert
        Assert.NotNull(requestInfo);
        Assert.NotNull(requestInfo.DeviceSpecifications);
        Assert.Empty(requestInfo.DeviceSpecifications);
        Assert.Equal(default(FrameType), requestInfo.FrameType);
        Assert.Equal(default(DateTime), requestInfo.RequestedAt);
    }

    /// <summary>
    /// TC12.1.2: DeviceSpecificationsに複数デバイスを設定可能
    /// </summary>
    [Fact]
    public void DeviceSpecifications_複数デバイス_設定可能()
    {
        // Arrange
        var requestInfo = new ReadRandomRequestInfo();
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.M, 200),
            new DeviceSpecification(DeviceCode.X, 0)
        };

        // Act
        requestInfo.DeviceSpecifications = devices;

        // Assert
        Assert.NotNull(requestInfo.DeviceSpecifications);
        Assert.Equal(3, requestInfo.DeviceSpecifications.Count);
        Assert.Equal(DeviceCode.D, requestInfo.DeviceSpecifications[0].Code);
        Assert.Equal(100, requestInfo.DeviceSpecifications[0].DeviceNumber);
        Assert.Equal(DeviceCode.M, requestInfo.DeviceSpecifications[1].Code);
        Assert.Equal(200, requestInfo.DeviceSpecifications[1].DeviceNumber);
        Assert.Equal(DeviceCode.X, requestInfo.DeviceSpecifications[2].Code);
        Assert.Equal(0, requestInfo.DeviceSpecifications[2].DeviceNumber);
    }

    /// <summary>
    /// TC12.1.3: DeviceSpecificationsは空リストでデフォルト初期化される
    /// </summary>
    [Fact]
    public void DeviceSpecifications_空リスト_デフォルト初期化()
    {
        // Act
        var requestInfo = new ReadRandomRequestInfo();

        // Assert
        Assert.NotNull(requestInfo.DeviceSpecifications);
        Assert.Empty(requestInfo.DeviceSpecifications);
        Assert.IsType<List<DeviceSpecification>>(requestInfo.DeviceSpecifications);
    }

    /// <summary>
    /// TC12.1.4: FrameTypeプロパティが設定・取得可能
    /// </summary>
    [Fact]
    public void FrameType_設定_取得可能()
    {
        // Arrange
        var requestInfo = new ReadRandomRequestInfo();

        // Act - 3Eフレーム設定
        requestInfo.FrameType = FrameType.Frame3E;

        // Assert
        Assert.Equal(FrameType.Frame3E, requestInfo.FrameType);

        // Act - 4Eフレーム設定
        requestInfo.FrameType = FrameType.Frame4E;

        // Assert
        Assert.Equal(FrameType.Frame4E, requestInfo.FrameType);
    }

    /// <summary>
    /// TC12.1.5: RequestedAtプロパティが設定・取得可能
    /// </summary>
    [Fact]
    public void RequestedAt_設定_取得可能()
    {
        // Arrange
        var requestInfo = new ReadRandomRequestInfo();
        var testDateTime = new DateTime(2025, 12, 2, 10, 30, 45, DateTimeKind.Utc);

        // Act
        requestInfo.RequestedAt = testDateTime;

        // Assert
        Assert.Equal(testDateTime, requestInfo.RequestedAt);
        Assert.Equal(DateTimeKind.Utc, requestInfo.RequestedAt.Kind);
    }

    /// <summary>
    /// TC12.1.6: 複数プロパティを同時に設定可能
    /// </summary>
    [Fact]
    public void 複数プロパティ_同時設定_正しく保持される()
    {
        // Arrange
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100),
            new DeviceSpecification(DeviceCode.M, 200)
        };
        var requestedAt = DateTime.UtcNow;

        // Act
        var requestInfo = new ReadRandomRequestInfo
        {
            DeviceSpecifications = devices,
            FrameType = FrameType.Frame4E,
            RequestedAt = requestedAt
        };

        // Assert
        Assert.Equal(2, requestInfo.DeviceSpecifications.Count);
        Assert.Equal(FrameType.Frame4E, requestInfo.FrameType);
        Assert.Equal(requestedAt, requestInfo.RequestedAt);
    }
}
