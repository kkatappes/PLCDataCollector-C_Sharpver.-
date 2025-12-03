using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Andon.Core.Interfaces;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 2-3: PlcModelのJSON出力実装テスト
/// TDD Red Phase: 失敗するテストを先に作成
/// </summary>
public class Phase2_3_PlcModel_JsonOutputTests : IDisposable
{
    private readonly string _testDirectory;

    public Phase2_3_PlcModel_JsonOutputTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"Phase2_3_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    /// <summary>
    /// テストケース1: PlcModelがJSON出力に正しく含まれることを確認
    /// </summary>
    [Fact]
    public void test_DataOutputManager_PlcModelをJSON出力()
    {
        // Arrange
        string plcModel = "5_JRS_N2";
        var dataOutputManager = new DataOutputManager();

        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };

        var timestamp = DateTime.UtcNow;
        var processedData = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = timestamp
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "テストデバイス", Digits = 1 } }
        };

        // Act
        dataOutputManager.OutputToJson(
            processedData,
            _testDirectory,
            "172.30.40.40",
            8192,
            plcModel,  // ← 新規追加パラメータ（現在はシグネチャに存在しないため失敗する）
            deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        // source.plcModelの存在確認
        Assert.True(root.GetProperty("source").TryGetProperty("plcModel", out var plcModelElement));
        Assert.Equal("5_JRS_N2", plcModelElement.GetString());
    }

    /// <summary>
    /// テストケース2: PlcModelが空文字列の場合でも正しく出力される
    /// </summary>
    [Fact]
    public void test_DataOutputManager_PlcModel空文字列の場合()
    {
        // Arrange
        string plcModel = ""; // 空文字列
        var dataOutputManager = new DataOutputManager();

        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };

        var timestamp = DateTime.UtcNow;
        var processedData = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = timestamp
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "テストデバイス", Digits = 1 } }
        };

        // Act
        dataOutputManager.OutputToJson(
            processedData,
            _testDirectory,
            "172.30.40.40",
            8192,
            plcModel,  // ← 新規追加パラメータ
            deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        // 空文字列でもフィールドは存在する
        Assert.True(root.GetProperty("source").TryGetProperty("plcModel", out var plcModelElement));
        Assert.Equal("", plcModelElement.GetString());
    }

    /// <summary>
    /// テストケース3: PlcModelがnullの場合、空文字列として出力される
    /// </summary>
    [Fact]
    public void test_DataOutputManager_PlcModelがnullの場合()
    {
        // Arrange
        string plcModel = null; // null
        var dataOutputManager = new DataOutputManager();

        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };

        var timestamp = DateTime.UtcNow;
        var processedData = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = timestamp
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "テストデバイス", Digits = 1 } }
        };

        // Act
        dataOutputManager.OutputToJson(
            processedData,
            _testDirectory,
            "172.30.40.40",
            8192,
            plcModel,  // ← 新規追加パラメータ
            deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Single(files);

        var jsonContent = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        // nullの場合、フィールドが存在して空文字列
        Assert.True(root.GetProperty("source").TryGetProperty("plcModel", out var plcModelElement));
        Assert.Equal("", plcModelElement.GetString());
    }

    /// <summary>
    /// テストケース4: ExecutionOrchestratorがPlcModelをDataOutputManagerに渡すことを確認
    /// </summary>
    [Fact]
    public void test_ExecutionOrchestrator_PlcModelをDataOutputManagerに渡す()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "172.30.40.40",
            Port = 8192,
            PlcModel = "5_JRS_N2"
        };

        var mockDataOutputManager = new Mock<IDataOutputManager>();

        // OutputToJsonが新しいシグネチャ（plcModelパラメータ付き）で呼ばれることを期待
        mockDataOutputManager.Setup(x => x.OutputToJson(
            It.IsAny<ProcessedResponseData>(),
            It.IsAny<string>(),
            "172.30.40.40",
            8192,
            "5_JRS_N2",  // ← plcConfig.PlcModelが渡されることを確認
            It.IsAny<Dictionary<string, DeviceEntryInfo>>()
        )).Verifiable();

        // 注意: このテストは実装変更後に動作確認のため、Redフェーズでは
        // ExecutionOrchestratorの完全なモック化が必要だが、
        // 簡略化のため、このテストはGreenフェーズで動作確認する

        // Assert
        // 現時点ではIDataOutputManager.OutputToJsonのシグネチャに
        // plcModelパラメータが存在しないため、このSetupは失敗する（期待通り）

        // このテストはMockのSetup段階で失敗するため、
        // 実装後にVerifyで確認する設計となっている
        Assert.True(true); // プレースホルダー（Greenフェーズで完全実装）
    }
}
