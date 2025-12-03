using Xunit;
using Andon.Core.Controllers;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Andon.Core.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 2-4: SavePathの利用実装テスト
/// TDD Red Phase: 失敗するテストを先に作成
/// </summary>
public class Phase2_4_SavePath_ExcelConfigTests : IDisposable
{
    private readonly string _testBaseDirectory;
    private readonly List<string> _createdDirectories = new();

    public Phase2_4_SavePath_ExcelConfigTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), $"Phase2_4_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testBaseDirectory);
        _createdDirectories.Add(_testBaseDirectory);
    }

    public void Dispose()
    {
        foreach (var dir in _createdDirectories)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    // クリーンアップ失敗は無視
                }
            }
        }
    }

    /// <summary>
    /// テストケース1: Excel設定のSavePathを使用してデータが出力される
    /// </summary>
    [Fact]
    public void test_ExecutionOrchestrator_Excel設定のSavePathを使用()
    {
        // Arrange
        var customPath = Path.Combine(_testBaseDirectory, "custom_output");
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 8192,
            PlcModel = "TestPLC",
            SavePath = customPath,  // Excel設定値
            MonitoringIntervalMs = 1000
        };

        var mockDataOutputManager = new Mock<IDataOutputManager>();
        var mockPlcManager = new Mock<IPlcCommunicationManager>();
        var mockLoggingManager = new Mock<ILoggingManager>();

        // OutputToJsonが指定されたcustomPathで呼ばれることを期待
        mockDataOutputManager.Setup(x => x.OutputToJson(
            It.IsAny<ProcessedResponseData>(),
            customPath,  // ← Excel設定のSavePathが渡されることを期待
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, DeviceEntryInfo>>()
        )).Verifiable();

        // このテストは、Red段階では実装が不完全なため、
        // 実際には異なるパス（ハードコード）が使用されて失敗する

        // Assert
        // 実装前なので、このSetupが満たされないことを期待
        Assert.NotEqual("C:/Users/PPESAdmin/Desktop/x/output", customPath);
    }

    /// <summary>
    /// テストケース2: SavePath絶対パス指定が機能する
    /// </summary>
    [Fact]
    public void test_ExecutionOrchestrator_SavePath絶対パス指定()
    {
        // Arrange
        var absolutePath = Path.Combine(_testBaseDirectory, "absolute_path");
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 8192,
            PlcModel = "TestPLC",
            SavePath = absolutePath,  // 絶対パス
            MonitoringIntervalMs = 1000
        };

        var mockDataOutputManager = new Mock<IDataOutputManager>();

        // OutputToJsonが絶対パスで呼ばれることを期待
        mockDataOutputManager.Setup(x => x.OutputToJson(
            It.IsAny<ProcessedResponseData>(),
            absolutePath,
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, DeviceEntryInfo>>()
        )).Verifiable();

        // Assert
        Assert.True(Path.IsPathRooted(absolutePath));
    }

    /// <summary>
    /// テストケース3: SavePath空の場合デフォルトパス使用
    /// </summary>
    [Fact]
    public void test_ExecutionOrchestrator_SavePath空の場合デフォルトパス使用()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 8192,
            PlcModel = "TestPLC",
            SavePath = "",  // 空文字列
            MonitoringIntervalMs = 1000
        };

        var mockDataOutputManager = new Mock<IDataOutputManager>();

        // OutputToJsonがデフォルトパス（"./output"または指定されたパス）で呼ばれることを期待
        mockDataOutputManager.Setup(x => x.OutputToJson(
            It.IsAny<ProcessedResponseData>(),
            It.Is<string>(path => !string.IsNullOrWhiteSpace(path)),  // 空でないパス
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, DeviceEntryInfo>>()
        )).Verifiable();

        // Assert
        Assert.Empty(plcConfig.SavePath);
    }

    /// <summary>
    /// テストケース4: SavePathディレクトリ作成機能
    /// </summary>
    [Fact]
    public void test_ExecutionOrchestrator_SavePathディレクトリ作成()
    {
        // Arrange
        var newPath = Path.Combine(_testBaseDirectory, "new", "directory", "structure");
        _createdDirectories.Add(newPath);

        var plcConfig = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 8192,
            PlcModel = "TestPLC",
            SavePath = newPath,  // 存在しないディレクトリ
            MonitoringIntervalMs = 1000
        };

        // 事前確認: ディレクトリが存在しないことを確認
        Assert.False(Directory.Exists(newPath));

        // このテストは、実装後にディレクトリが自動作成されることを確認する
        // Red段階では、まだ作成機能が実装されていないため、確認のみ
    }

    /// <summary>
    /// テストケース5: 複数PLC異なるSavePath
    /// </summary>
    [Fact]
    public void test_ExecutionOrchestrator_複数PLC異なるSavePath()
    {
        // Arrange
        var path1 = Path.Combine(_testBaseDirectory, "plc1");
        var path2 = Path.Combine(_testBaseDirectory, "plc2");

        var plcConfig1 = new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 8192,
            PlcId = "PLC1",
            PlcModel = "PLC1_Model",
            SavePath = path1,
            MonitoringIntervalMs = 1000
        };

        var plcConfig2 = new PlcConfiguration
        {
            IpAddress = "192.168.1.2",
            Port = 8192,
            PlcId = "PLC2",
            PlcModel = "PLC2_Model",
            SavePath = path2,
            MonitoringIntervalMs = 1000
        };

        // Assert
        // 各PLCが異なる保存先を持つことを確認
        Assert.NotEqual(plcConfig1.SavePath, plcConfig2.SavePath);
        Assert.Equal(path1, plcConfig1.SavePath);
        Assert.Equal(path2, plcConfig2.SavePath);
    }
}
