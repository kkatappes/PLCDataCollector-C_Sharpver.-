using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;

namespace Andon.Tests.Integration;

/// <summary>
/// DataOutputManager 統合テスト（Step7データ出力機能）
/// Phase5: 統合テスト・検証
/// </summary>
public class DataOutputManager_IntegrationTests : IDisposable
{
    private readonly DataOutputManager _manager;
    private readonly string _testOutputDirectory;

    public DataOutputManager_IntegrationTests()
    {
        _manager = new DataOutputManager();
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"andon_integration_test_{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        // テスト後のクリーンアップ
        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, true);
        }
    }

    /// <summary>
    /// TC_INT_001: ビットデバイスのみの統合テスト
    /// </summary>
    [Fact]
    public void TC_INT_001_BitDeviceOnly_Integration_Success()
    {
        // Arrange
        var data = CreateMockProcessedResponseData_BitDevice();
        var deviceConfig = CreateMockDeviceConfig_BitDevice();

        // Act
        _manager.OutputToJson(data, _testOutputDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testOutputDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDocument = JsonDocument.Parse(jsonContent);

        // JSON構造検証
        Assert.True(jsonDocument.RootElement.TryGetProperty("source", out var source));
        Assert.Equal("192.168.1.10", source.GetProperty("ipAddress").GetString());
        Assert.Equal(5007, source.GetProperty("port").GetInt32());

        Assert.True(jsonDocument.RootElement.TryGetProperty("timestamp", out _));
        Assert.True(jsonDocument.RootElement.TryGetProperty("items", out var items));

        // ビットデバイスが16個に展開されていることを確認
        var itemsArray = items.EnumerateArray().ToList();
        Assert.Equal(16, itemsArray.Count);

        // device.numberが3桁ゼロ埋めされていることを確認
        var firstItem = itemsArray[0];
        var deviceNumber = firstItem.GetProperty("device").GetProperty("number").GetString();
        Assert.Equal("000", deviceNumber);

        // unitがbitであることを確認
        var unit = firstItem.GetProperty("unit").GetString();
        Assert.Equal("bit", unit);
    }

    /// <summary>
    /// TC_INT_002: ワードデバイスのみの統合テスト
    /// </summary>
    [Fact]
    public void TC_INT_002_WordDeviceOnly_Integration_Success()
    {
        // Arrange
        var data = CreateMockProcessedResponseData_WordDevice();
        var deviceConfig = CreateMockDeviceConfig_WordDevice();

        // Act
        _manager.OutputToJson(data, _testOutputDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testOutputDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDocument = JsonDocument.Parse(jsonContent);

        // items検証
        Assert.True(jsonDocument.RootElement.TryGetProperty("items", out var items));
        var itemsArray = items.EnumerateArray().ToList();
        Assert.Single(itemsArray);

        var item = itemsArray[0];
        Assert.Equal("生産台数", item.GetProperty("name").GetString());
        Assert.Equal("D", item.GetProperty("device").GetProperty("code").GetString());
        Assert.Equal("100", item.GetProperty("device").GetProperty("number").GetString());
        Assert.Equal("word", item.GetProperty("unit").GetString());
        Assert.Equal(12345, item.GetProperty("value").GetInt32());
    }

    /// <summary>
    /// TC_INT_003: ダブルワードデバイスのみの統合テスト
    /// </summary>
    [Fact]
    public void TC_INT_003_DWordDeviceOnly_Integration_Success()
    {
        // Arrange
        var data = CreateMockProcessedResponseData_DWordDevice();
        var deviceConfig = CreateMockDeviceConfig_DWordDevice();

        // Act
        _manager.OutputToJson(data, _testOutputDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testOutputDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDocument = JsonDocument.Parse(jsonContent);

        // items検証
        Assert.True(jsonDocument.RootElement.TryGetProperty("items", out var items));
        var itemsArray = items.EnumerateArray().ToList();
        Assert.Single(itemsArray);

        var item = itemsArray[0];
        Assert.Equal("累積生産台数", item.GetProperty("name").GetString());
        Assert.Equal("D", item.GetProperty("device").GetProperty("code").GetString());
        Assert.Equal("200", item.GetProperty("device").GetProperty("number").GetString());
        Assert.Equal("dword", item.GetProperty("unit").GetString());
        Assert.Equal(0x12345678, item.GetProperty("value").GetInt64());
    }

    /// <summary>
    /// TC_INT_004: 混在デバイスの統合テスト
    /// </summary>
    [Fact]
    public void TC_INT_004_MixedDevices_Integration_Success()
    {
        // Arrange
        var data = CreateMockProcessedResponseData_Mixed();
        var deviceConfig = CreateMockDeviceConfig_Mixed();

        // Act
        _manager.OutputToJson(data, _testOutputDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testOutputDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDocument = JsonDocument.Parse(jsonContent);

        // items検証（ビット16個 + ワード1個 + ダブルワード1個 = 18個）
        Assert.True(jsonDocument.RootElement.TryGetProperty("items", out var items));
        var itemsArray = items.EnumerateArray().ToList();
        Assert.Equal(18, itemsArray.Count);

        // ビットデバイスの検証（最初の16個）
        for (int i = 0; i < 16; i++)
        {
            var item = itemsArray[i];
            Assert.Equal($"ビットデバイスM{i}", item.GetProperty("name").GetString());
            Assert.Equal("bit", item.GetProperty("unit").GetString());
        }

        // ワードデバイスの検証
        var wordItem = itemsArray[16];
        Assert.Equal("生産台数", wordItem.GetProperty("name").GetString());
        Assert.Equal("word", wordItem.GetProperty("unit").GetString());

        // ダブルワードデバイスの検証
        var dwordItem = itemsArray[17];
        Assert.Equal("累積生産台数", dwordItem.GetProperty("name").GetString());
        Assert.Equal("dword", dwordItem.GetProperty("unit").GetString());
    }

    /// <summary>
    /// TC_INT_005: ファイル名形式の統合テスト
    /// </summary>
    [Fact]
    public void TC_INT_005_FileNameFormat_Integration_Success()
    {
        // Arrange
        var data = CreateMockProcessedResponseData_WordDevice();
        var deviceConfig = CreateMockDeviceConfig_WordDevice();

        // Act
        _manager.OutputToJson(data, _testOutputDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testOutputDirectory, "*.json");
        Assert.Single(files);

        var fileName = Path.GetFileName(files[0]);
        // ファイル名形式: yyyyMMdd_HHmmssfff_192-168-1-10_5007.json
        Assert.Matches(@"^\d{8}_\d{9}_192-168-1-10_5007\.json$", fileName);
    }


    // ========================================
    // モックデータ作成メソッド
    // ========================================

    /// <summary>
    /// ビットデバイスのモックデータ
    /// </summary>
    private ProcessedResponseData CreateMockProcessedResponseData_BitDevice()
    {
        return new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["M0"] = new DeviceData
                {
                    DeviceName = "M0",
                    Code = DeviceCode.M,
                    Address = 0,
                    Value = 0b1010110011010101,  // 43605
                    Type = "Bit"
                }
            },
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
    }

    /// <summary>
    /// ワードデバイスのモックデータ
    /// </summary>
    private ProcessedResponseData CreateMockProcessedResponseData_WordDevice()
    {
        return new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["D100"] = new DeviceData
                {
                    DeviceName = "D100",
                    Code = DeviceCode.D,
                    Address = 100,
                    Value = 12345,
                    Type = "Word"
                }
            },
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
    }

    /// <summary>
    /// ダブルワードデバイスのモックデータ
    /// </summary>
    private ProcessedResponseData CreateMockProcessedResponseData_DWordDevice()
    {
        return new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["D200"] = new DeviceData
                {
                    DeviceName = "D200",
                    Code = DeviceCode.D,
                    Address = 200,
                    Value = 0x12345678,
                    IsDWord = true,
                    Type = "DWord"
                }
            },
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
    }

    /// <summary>
    /// 混在デバイスのモックデータ
    /// </summary>
    private ProcessedResponseData CreateMockProcessedResponseData_Mixed()
    {
        return new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>
            {
                ["M0"] = new DeviceData
                {
                    DeviceName = "M0",
                    Code = DeviceCode.M,
                    Address = 0,
                    Value = 0b1010110011010101,
                    Type = "Bit"
                },
                ["D100"] = new DeviceData
                {
                    DeviceName = "D100",
                    Code = DeviceCode.D,
                    Address = 100,
                    Value = 12345,
                    Type = "Word"
                },
                ["D200"] = new DeviceData
                {
                    DeviceName = "D200",
                    Code = DeviceCode.D,
                    Address = 200,
                    Value = 0x12345678,
                    IsDWord = true,
                    Type = "DWord"
                }
            },
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
    }

    // ========================================
    // deviceConfigのモック作成メソッド
    // ========================================

    /// <summary>
    /// ビットデバイス用deviceConfig
    /// </summary>
    private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_BitDevice()
    {
        var config = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            config[$"M{i}"] = new DeviceEntryInfo
            {
                Name = $"ビットデバイスM{i}",
                Digits = 1
            };
        }
        return config;
    }

    /// <summary>
    /// ワードデバイス用deviceConfig
    /// </summary>
    private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_WordDevice()
    {
        return new Dictionary<string, DeviceEntryInfo>
        {
            ["D100"] = new DeviceEntryInfo { Name = "生産台数", Digits = 5 }
        };
    }

    /// <summary>
    /// ダブルワードデバイス用deviceConfig
    /// </summary>
    private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_DWordDevice()
    {
        return new Dictionary<string, DeviceEntryInfo>
        {
            ["D200"] = new DeviceEntryInfo { Name = "累積生産台数", Digits = 10 }
        };
    }

    /// <summary>
    /// 混在デバイス用deviceConfig
    /// </summary>
    private Dictionary<string, DeviceEntryInfo> CreateMockDeviceConfig_Mixed()
    {
        var config = CreateMockDeviceConfig_BitDevice();
        config["D100"] = new DeviceEntryInfo { Name = "生産台数", Digits = 5 };
        config["D200"] = new DeviceEntryInfo { Name = "累積生産台数", Digits = 10 };
        return config;
    }
}
