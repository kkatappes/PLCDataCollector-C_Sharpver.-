using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// PlcCommunicationManager.ParseRawToStructuredData メソッドテスト (TC037-TC040)
/// TDD Phase 1: Red - テスト先行実装
/// </summary>
public class PlcCommunicationManagerTests_ParseRawToStructuredData
{
    #region TC037: 3Eフレーム解析テスト

    /// <summary>
    /// TC037: ParseRawToStructuredData_3E_Binary_基本構造化成功
    ///
    /// 目的: 3E Binaryフレームの処理済みデータを構造化できることを確認
    /// 入力: BasicProcessedDevicesに基本デバイスデータ（M100-M107ビットデータ）
    /// 期待: StructuredDeviceが正しく生成され、フィールド値が設定される
    /// </summary>
    [Fact]
    public async Task TC037_ParseRawToStructuredData_3E_Binary_基本構造化成功()
    {
        // Arrange: テストデータ準備
        var processedData = new ProcessedResponseData
        {
            IsSuccess = true,
            BasicProcessedDevices = new List<ProcessedDevice>
            {
                new ProcessedDevice
                {
                    DeviceType = "M",
                    Address = 100,
                    Value = false, // M100 = OFF
                    DataType = "Bit",
                    DeviceName = "M100",
                    ProcessedAt = DateTime.UtcNow
                },
                new ProcessedDevice
                {
                    DeviceType = "M",
                    Address = 103,
                    Value = true, // M103 = ON
                    DataType = "Bit",
                    DeviceName = "M103",
                    ProcessedAt = DateTime.UtcNow
                },
                new ProcessedDevice
                {
                    DeviceType = "M",
                    Address = 106,
                    Value = true, // M106 = ON
                    DataType = "Bit",
                    DeviceName = "M106",
                    ProcessedAt = DateTime.UtcNow
                },
                new ProcessedDevice
                {
                    DeviceType = "M",
                    Address = 107,
                    Value = true, // M107 = ON
                    DataType = "Bit",
                    DeviceName = "M107",
                    ProcessedAt = DateTime.UtcNow
                }
            },
            ProcessedAt = DateTime.UtcNow,
            ProcessingTimeMs = 10,
            FrameType = FrameType.Frame3E
        };

        var structureDef = new StructureDefinition
        {
            Name = "TestBitData",
            Description = "M100-M107 ビットデータ構造",
            FrameType = SlmpConstants.Frame3E,
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    Name = "Bit_M100",
                    Address = "M100",
                    DataType = "Boolean",
                    Description = "M100ビット値"
                },
                new FieldDefinition
                {
                    Name = "Bit_M103",
                    Address = "M103",
                    DataType = "Boolean",
                    Description = "M103ビット値"
                },
                new FieldDefinition
                {
                    Name = "Bit_M106",
                    Address = "M106",
                    DataType = "Boolean",
                    Description = "M106ビット値"
                },
                new FieldDefinition
                {
                    Name = "Bit_M107",
                    Address = "M107",
                    DataType = "Boolean",
                    Description = "M107ビット値"
                }
            }
        };

        var parseConfig = new ParseConfiguration
        {
            FrameFormat = "3E",
            DataFormat = "Binary",
            StructureDefinitions = new List<StructureDefinition> { structureDef }
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "M",
            StartAddress = 100,
            Count = 8,
            FrameType = FrameType.Frame3E,
            RequestedAt = DateTime.UtcNow,
            ParseConfiguration = parseConfig
        };

        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "192.168.3.250",
            Port = 5007,
            UseTcp = false,
            IsBinary = true,
            FrameVersion = FrameVersion.Frame3E
        };

        var timeoutConfig = new TimeoutConfig();

        var manager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig
        );

        // Act: 構造化処理実行
        var result = await manager.ParseRawToStructuredData(
            processedData,
            requestInfo,
            CancellationToken.None
        );

        // Assert: 検証
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, "構造化処理が成功すること");
        Assert.Single(result.StructuredDevices); // 1つの構造化デバイスが生成される

        var structuredDevice = result.StructuredDevices[0];
        Assert.Equal("TestBitData", structuredDevice.DeviceName);
        Assert.Equal(4, structuredDevice.Fields.Count); // 4つのフィールド

        // フィールド値の検証
        Assert.False(structuredDevice.GetField<bool>("Bit_M100"), "M100はOFF");
        Assert.True(structuredDevice.GetField<bool>("Bit_M103"), "M103はON");
        Assert.True(structuredDevice.GetField<bool>("Bit_M106"), "M106はON");
        Assert.True(structuredDevice.GetField<bool>("Bit_M107"), "M107はON");

        // フレーム情報の検証
        Assert.NotNull(result.FrameInfo);
        Assert.Equal("3E", result.FrameInfo.FrameType);
    }

    /// <summary>
    /// TC037: ParseRawToStructuredData_3E_ASCII_基本構造化成功
    ///
    /// 目的: 3E ASCIIフレームの処理済みデータを構造化できることを確認
    /// </summary>
    [Fact]
    public async Task TC037_ParseRawToStructuredData_3E_ASCII_基本構造化成功()
    {
        // Arrange
        var processedData = new ProcessedResponseData
        {
            IsSuccess = true,
            BasicProcessedDevices = new List<ProcessedDevice>
            {
                new ProcessedDevice
                {
                    DeviceType = "M",
                    Address = 100,
                    Value = false,
                    DataType = "Bit",
                    DeviceName = "M100"
                }
            },
            FrameType = FrameType.Frame3E
        };

        var structureDef = new StructureDefinition
        {
            Name = "TestASCIIData",
            FrameType = SlmpConstants.Frame3E,
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    Name = "Bit_M100",
                    Address = "M100",
                    DataType = "Boolean"
                }
            }
        };

        var parseConfig = new ParseConfiguration
        {
            FrameFormat = "3E",
            DataFormat = "ASCII",
            StructureDefinitions = new List<StructureDefinition> { structureDef }
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "M",
            StartAddress = 100,
            Count = 1,
            FrameType = FrameType.Frame3E,
            ParseConfiguration = parseConfig
        };

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame3E
            },
            new TimeoutConfig()
        );

        // Act
        var result = await manager.ParseRawToStructuredData(
            processedData,
            requestInfo,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.StructuredDevices);
        Assert.Equal("3E", result.FrameInfo.FrameType);
    }

    #endregion

    #region TC038: 4Eフレーム解析テスト

    /// <summary>
    /// TC038: ParseRawToStructuredData_4E_Binary_実機データ構造化成功
    ///
    /// 目的: 4E Binaryフレームの実機データ（48ワード）を構造化できることを確認
    /// 入力: 実機から取得したD0-D47の48ワードデータ
    /// </summary>
    [Fact]
    public async Task TC038_ParseRawToStructuredData_4E_Binary_実機データ構造化成功()
    {
        // Arrange: 実機データの模擬
        var processedData = new ProcessedResponseData
        {
            IsSuccess = true,
            BasicProcessedDevices = new List<ProcessedDevice>
            {
                new ProcessedDevice
                {
                    DeviceType = "D",
                    Address = 8,
                    Value = (ushort)0x1907, // 6407
                    DataType = "Word",
                    DeviceName = "D8"
                },
                new ProcessedDevice
                {
                    DeviceType = "D",
                    Address = 23,
                    Value = (ushort)0x0020, // 32
                    DataType = "Word",
                    DeviceName = "D23"
                },
                new ProcessedDevice
                {
                    DeviceType = "D",
                    Address = 24,
                    Value = (ushort)0x0010, // 16
                    DataType = "Word",
                    DeviceName = "D24"
                },
                new ProcessedDevice
                {
                    DeviceType = "D",
                    Address = 25,
                    Value = (ushort)0x0008, // 8
                    DataType = "Word",
                    DeviceName = "D25"
                },
                new ProcessedDevice
                {
                    DeviceType = "D",
                    Address = 26,
                    Value = (ushort)0x0002, // 2
                    DataType = "Word",
                    DeviceName = "D26"
                }
            },
            FrameType = FrameType.Frame4E
        };

        var structureDef = new StructureDefinition
        {
            Name = "RealMachineData",
            Description = "実機D0-D47データ",
            FrameType = SlmpConstants.Frame4E,
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition { Name = "Word_D8", Address = "D8", DataType = "UInt16" },
                new FieldDefinition { Name = "Word_D23", Address = "D23", DataType = "UInt16" },
                new FieldDefinition { Name = "Word_D24", Address = "D24", DataType = "UInt16" },
                new FieldDefinition { Name = "Word_D25", Address = "D25", DataType = "UInt16" },
                new FieldDefinition { Name = "Word_D26", Address = "D26", DataType = "UInt16" }
            }
        };

        var parseConfig = new ParseConfiguration
        {
            FrameFormat = "4E",
            DataFormat = "Binary",
            HeaderSize = SlmpConstants.Frame4EHeaderSize,
            StructureDefinitions = new List<StructureDefinition> { structureDef }
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            StartAddress = 0,
            Count = 48,
            FrameType = FrameType.Frame4E,
            ParseConfiguration = parseConfig
        };

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = true,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act
        var result = await manager.ParseRawToStructuredData(
            processedData,
            requestInfo,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Is4EFrame, "4Eフレームであること");
        Assert.Single(result.StructuredDevices);

        var device = result.StructuredDevices[0];
        Assert.Equal((ushort)0x1907, device.GetField<ushort>("Word_D8"));
        Assert.Equal((ushort)0x0020, device.GetField<ushort>("Word_D23"));
        Assert.Equal((ushort)0x0010, device.GetField<ushort>("Word_D24"));
        Assert.Equal((ushort)0x0008, device.GetField<ushort>("Word_D25"));
        Assert.Equal((ushort)0x0002, device.GetField<ushort>("Word_D26"));

        // フレーム情報検証
        Assert.Equal("4E", result.FrameInfo.FrameType);
        Assert.Equal(SlmpConstants.Frame4EHeaderSize, result.FrameInfo.HeaderSize);
    }

    /// <summary>
    /// TC038: ParseRawToStructuredData_4E_ASCII_基本構造化成功
    /// </summary>
    [Fact]
    public async Task TC038_ParseRawToStructuredData_4E_ASCII_基本構造化成功()
    {
        // Arrange
        var processedData = new ProcessedResponseData
        {
            IsSuccess = true,
            BasicProcessedDevices = new List<ProcessedDevice>
            {
                new ProcessedDevice
                {
                    DeviceType = "D",
                    Address = 8,
                    Value = (ushort)0x1907,
                    DataType = "Word",
                    DeviceName = "D8"
                }
            },
            FrameType = FrameType.Frame4E
        };

        var structureDef = new StructureDefinition
        {
            Name = "Test4EASCII",
            FrameType = SlmpConstants.Frame4E,
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition { Name = "Word_D8", Address = "D8", DataType = "UInt16" }
            }
        };

        var parseConfig = new ParseConfiguration
        {
            FrameFormat = "4E",
            DataFormat = "ASCII",
            HeaderSize = SlmpConstants.Frame4EHeaderSize,
            StructureDefinitions = new List<StructureDefinition> { structureDef }
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "D",
            FrameType = FrameType.Frame4E,
            ParseConfiguration = parseConfig
        };

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                IsBinary = false,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act
        var result = await manager.ParseRawToStructuredData(
            processedData,
            requestInfo,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Is4EFrame);
        Assert.Equal((ushort)0x1907, result.StructuredDevices[0].GetField<ushort>("Word_D8"));
    }

    #endregion

    #region TC039: DWord結合済みデータ構造化テスト

    /// <summary>
    /// TC039: ParseRawToStructuredData_DWord結合済みデータ構造化成功
    ///
    /// 目的: DWord結合済みデバイスデータが正しく構造化されることを確認
    /// 入力: CombinedDWordDevicesに32bitデータ
    /// </summary>
    [Fact]
    public async Task TC039_ParseRawToStructuredData_DWord結合済みデータ構造化成功()
    {
        // Arrange
        var processedData = new ProcessedResponseData
        {
            IsSuccess = true,
            CombinedDWordDevices = new List<CombinedDWordDevice>
            {
                new CombinedDWordDevice
                {
                    DeviceName = "D100_32bit",
                    CombinedValue = 0x12345678,
                    LowWordAddress = 100,
                    HighWordAddress = 101,
                    LowWordValue = 0x5678,
                    HighWordValue = 0x1234,
                    DeviceType = "D"
                },
                new CombinedDWordDevice
                {
                    DeviceName = "D102_32bit",
                    CombinedValue = 0xABCDEF00,
                    LowWordAddress = 102,
                    HighWordAddress = 103,
                    LowWordValue = 0xEF00,
                    HighWordValue = 0xABCD,
                    DeviceType = "D"
                }
            },
            FrameType = FrameType.Frame4E
        };

        var structureDef = new StructureDefinition
        {
            Name = "DWordData",
            FrameType = SlmpConstants.Frame4E,
            Fields = new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    Name = "DWord_D100",
                    Address = "D100_32bit",
                    DataType = "UInt32"
                },
                new FieldDefinition
                {
                    Name = "DWord_D102",
                    Address = "D102_32bit",
                    DataType = "UInt32"
                }
            }
        };

        var parseConfig = new ParseConfiguration
        {
            FrameFormat = "4E",
            StructureDefinitions = new List<StructureDefinition> { structureDef }
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            FrameType = FrameType.Frame4E,
            ParseConfiguration = parseConfig
        };

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007,
                FrameVersion = FrameVersion.Frame4E
            },
            new TimeoutConfig()
        );

        // Act
        var result = await manager.ParseRawToStructuredData(
            processedData,
            requestInfo,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.StructuredDevices);

        var device = result.StructuredDevices[0];
        Assert.Equal((uint)0x12345678, device.GetField<uint>("DWord_D100"));
        Assert.Equal((uint)0xABCDEF00, device.GetField<uint>("DWord_D102"));
    }

    #endregion

    #region TC040: 例外系テスト

    /// <summary>
    /// TC040: ParseRawToStructuredData_処理失敗データ入力時例外
    ///
    /// 目的: ProcessedResponseData.IsSuccess = false の場合に例外が発生することを確認
    /// </summary>
    [Fact]
    public async Task TC040_ParseRawToStructuredData_処理失敗データ入力時例外()
    {
        // Arrange
        var processedData = new ProcessedResponseData
        {
            IsSuccess = false, // 失敗状態
            Errors = new List<string> { "前処理でエラーが発生しました" }
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            ParseConfiguration = new ParseConfiguration()
        };

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007
            },
            new TimeoutConfig()
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await manager.ParseRawToStructuredData(
                processedData,
                requestInfo,
                CancellationToken.None
            )
        );

        Assert.Contains("失敗状態", ex.Message);
    }

    /// <summary>
    /// TC040: ParseRawToStructuredData_null入力時例外
    /// </summary>
    [Fact]
    public async Task TC040_ParseRawToStructuredData_null入力時例外()
    {
        // Arrange
        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007
            },
            new TimeoutConfig()
        );

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            ParseConfiguration = new ParseConfiguration()
        };

        // Act & Assert: processedData が null
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await manager.ParseRawToStructuredData(
                null!,
                requestInfo,
                CancellationToken.None
            )
        );

        // Act & Assert: requestInfo が null
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await manager.ParseRawToStructuredData(
                new ProcessedResponseData { IsSuccess = true },
                null!,
                CancellationToken.None
            )
        );
    }

    /// <summary>
    /// TC040: ParseRawToStructuredData_構造定義なし時警告
    ///
    /// 目的: 構造定義がない場合でも例外は発生せず、警告が記録されることを確認
    /// </summary>
    [Fact]
    public async Task TC040_ParseRawToStructuredData_構造定義なし時警告()
    {
        // Arrange
        var processedData = new ProcessedResponseData
        {
            IsSuccess = true,
            BasicProcessedDevices = new List<ProcessedDevice>()
        };

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            ParseConfiguration = new ParseConfiguration
            {
                StructureDefinitions = new List<StructureDefinition>() // 空リスト
            }
        };

        var manager = new PlcCommunicationManager(
            new ConnectionConfig
            {
                IpAddress = "192.168.3.250",
                Port = 5007
            },
            new TimeoutConfig()
        );

        // Act
        var result = await manager.ParseRawToStructuredData(
            processedData,
            requestInfo,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess, "構造定義なしでも成功すること");
        Assert.Empty(result.StructuredDevices);
        Assert.NotEmpty(result.Warnings); // 警告が記録される
        Assert.Contains("構造定義が指定されていません", result.Warnings[0]);
    }

    #endregion
}
