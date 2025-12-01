using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;

namespace Tests.Unit.Core.Managers;

/// <summary>
/// ConfigToFrameManager のユニットテスト
/// Phase1: Binary形式オーバーロード実装のTDDテスト
/// </summary>
public class ConfigToFrameManagerTests
{
    /// <summary>
    /// Round 1: null検証（異常系）
    /// TC_Step12_004: config引数がnullの場合、ArgumentNullExceptionがスローされることを確認
    /// </summary>
    [Fact]
    public void TC_Step12_004_BuildReadRandomFrameFromConfig_異常系_ConfigNull()
    {
        // Arrange
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            frameManager.BuildReadRandomFrameFromConfig((TargetDeviceConfig)null));
    }

    /// <summary>
    /// Round 2: 空リスト検証（異常系）
    /// TC_Step12_003: Devicesリストが空の場合、ArgumentExceptionがスローされることを確認
    /// </summary>
    [Fact]
    public void TC_Step12_003_BuildReadRandomFrameFromConfig_異常系_デバイスリスト空()
    {
        // Arrange
        var targetConfig = new TargetDeviceConfig
        {
            Devices = new List<DeviceEntry>()
        };
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            frameManager.BuildReadRandomFrameFromConfig(targetConfig));
    }

    /// <summary>
    /// Round 3: フレーム構築（正常系）
    /// TC_Step12_001: 4Eフレーム形式で複数デバイスのフレームが正しく構築されることを確認
    /// </summary>
    [Fact]
    public void TC_Step12_001_BuildReadRandomFrameFromConfig_正常系_4Eフレーム_複数デバイス()
    {
        // Arrange
        var targetConfig = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry
                {
                    DeviceType = "M",
                    DeviceNumber = 33,
                    Description = "テスト1"
                },
                new DeviceEntry
                {
                    DeviceType = "D",
                    DeviceNumber = 100,
                    Description = "テスト2"
                }
            }
        };

        var frameManager = new ConfigToFrameManager();

        // Act
        var frame = frameManager.BuildReadRandomFrameFromConfig(targetConfig);

        // Assert
        Assert.NotNull(frame);
        Assert.True(frame.Length > 0);

        // 4Eフレームヘッダ検証
        Assert.Equal(0x54, frame[0]); // サブヘッダ下位
        Assert.Equal(0x00, frame[1]); // サブヘッダ上位

        // コマンド検証 (4Eフレームはオフセット15-16)
        Assert.Equal(0x03, frame[15]); // コマンド下位 (ReadRandom)
        Assert.Equal(0x04, frame[16]); // コマンド上位
    }

    // ===================================================
    // Phase1補完: PlcConfiguration用Binary形式オーバーロード
    // ===================================================

    /// <summary>
    /// Round 1: PlcConfiguration null検証（異常系）
    /// TC_Step12_Binary_004_PlcConfig: config引数がnullの場合、ArgumentNullExceptionがスローされることを確認
    /// </summary>
    [Fact]
    public void TC_Step12_Binary_004_PlcConfig()
    {
        // Arrange
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            frameManager.BuildReadRandomFrameFromConfig((PlcConfiguration)null));
    }

    /// <summary>
    /// Round 2: PlcConfiguration 空リスト検証（異常系）
    /// TC_Step12_Binary_003_PlcConfig: Devicesリストが空の場合、ArgumentExceptionがスローされることを確認
    /// </summary>
    [Fact]
    public void TC_Step12_Binary_003_PlcConfig()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            Devices = new List<DeviceSpecification>()
        };
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            frameManager.BuildReadRandomFrameFromConfig(plcConfig));
    }

    /// <summary>
    /// Round 3: PlcConfiguration 正常系（4Eフレーム構築）
    /// TC_Step12_Binary_001_PlcConfig: 4Eフレーム形式で複数デバイスのフレームが正しく構築されることを確認
    /// </summary>
    [Fact]
    public void TC_Step12_Binary_001_PlcConfig()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification("M", 33),
                new DeviceSpecification("D", 100)
            }
        };

        var frameManager = new ConfigToFrameManager();

        // Act
        var frame = frameManager.BuildReadRandomFrameFromConfig(plcConfig);

        // Assert
        Assert.NotNull(frame);
        Assert.True(frame.Length > 0);

        // 4Eフレームヘッダ検証
        Assert.Equal(0x54, frame[0]); // サブヘッダ下位
        Assert.Equal(0x00, frame[1]); // サブヘッダ上位

        // コマンド検証 (4Eフレームはオフセット15-16)
        Assert.Equal(0x03, frame[15]); // コマンド下位 (ReadRandom)
        Assert.Equal(0x04, frame[16]); // コマンド上位
    }
}
