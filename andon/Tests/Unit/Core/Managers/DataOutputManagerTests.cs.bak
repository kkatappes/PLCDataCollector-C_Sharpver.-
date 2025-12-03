using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// DataOutputManagerクラスのテスト
/// Phase7実装: データ出力処理（JSON形式）
/// TDD Red Phase: 失敗するテストを先に作成
/// </summary>
public class DataOutputManagerTests : IDisposable
{
    private readonly DataOutputManager _manager;
    private readonly string _testDirectory;

    public DataOutputManagerTests()
    {
        _manager = new DataOutputManager();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public void OutputToJson_ReadRandomData_OutputsCorrectJson()
    {
        // Arrange - Phase4仕様: Dictionary<string, DeviceData>を使用
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) },
            { "D105", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 105, false), 512) },
            { "M200", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 200, false), 1) }
        };

        var timestamp = new DateTime(2025, 11, 25, 10, 30, 45, 123);
        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = timestamp
        };

        // 設定ファイルから取得した情報を使用
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "生産数カウンタ", Digits = 1 } },
            { "D105", new DeviceEntryInfo { Name = "エラーカウンタ", Digits = 1 } },
            { "M200", new DeviceEntryInfo { Name = "運転状態フラグ", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(
            data,
            _testDirectory,
            "192.168.1.100",
            5000,
            deviceConfig);

        // Assert - ファイル名検証
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Single(files);

        var fileName = Path.GetFileName(files[0]);
        Assert.Matches(@"^192-168-1-100_5000\.json$", fileName);

        // Assert - JSON内容検証
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;

        // source検証
        Assert.Equal("Unknown", root.GetProperty("source").GetProperty("plcModel").GetString());
        Assert.Equal("192.168.1.100", root.GetProperty("source").GetProperty("ipAddress").GetString());
        Assert.Equal(5000, root.GetProperty("source").GetProperty("port").GetInt32());

        // timestamp検証
        var timestampStr = root.GetProperty("timestamp").GetProperty("local").GetString();
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}[+-]\d{2}:\d{2}$", timestampStr);

        // items検証 - Phase3: ビットデバイスは16ビットに分割される
        var items = root.GetProperty("items").EnumerateArray().ToList();
        // D100: 1, D105: 1, M200-M215: 16 = 合計18
        Assert.Equal(18, items.Count);

        // D100検証
        var d100Item = items.FirstOrDefault(i => i.GetProperty("device").GetProperty("code").GetString() == "D" &&
                                             i.GetProperty("device").GetProperty("number").GetString() == "100");
        Assert.NotEqual(default(JsonElement), d100Item);
        Assert.Equal("生産数カウンタ", d100Item.GetProperty("name").GetString());
        Assert.Equal(1, d100Item.GetProperty("digits").GetInt32());
        Assert.Equal("word", d100Item.GetProperty("unit").GetString());
        Assert.Equal(256u, d100Item.GetProperty("value").GetUInt32());
    }

    [Fact]
    public void OutputToJson_MultipleWrites_CreatesMultipleFiles()
    {
        // Arrange - Phase4仕様対応
        var deviceData1 = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };

        var deviceData2 = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 257) }
        };

        var data1 = new ProcessedResponseData
        {
            ProcessedData = deviceData1,
            ProcessedAt = DateTime.Now
        };

        var data2 = new ProcessedResponseData
        {
            ProcessedData = deviceData2,
            ProcessedAt = DateTime.Now.AddSeconds(1)
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "テストデバイス", Digits = 1 } }
        };

        // Act - 異なるIPアドレスで2回出力（日時なしファイル名では同じIP:ポートだと上書きされる）
        _manager.OutputToJson(data1, _testDirectory, "192.168.1.100", 5000, deviceConfig);
        _manager.OutputToJson(data2, _testDirectory, "192.168.1.101", 5000, deviceConfig);

        // Assert - 2ファイル作成されることを確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        Assert.Equal(2, files.Length);

        // 各ファイルの値を検証
        var jsonStrings = files.Select(f => File.ReadAllText(f)).ToList();
        var values = jsonStrings.Select(json =>
        {
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("items")[0].GetProperty("value").GetUInt32();
        }).OrderBy(v => v).ToList();

        Assert.Equal(256u, values[0]);
        Assert.Equal(257u, values[1]);
    }

    [Fact]
    public void OutputToJson_WithBitDevice_OutputsBitUnit()
    {
        // Arrange - ビットデバイスのテスト
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), 1) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "M0", new DeviceEntryInfo { Name = "運転状態フラグ開始", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "172.30.40.15", 8192, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal("bit", items[0].GetProperty("unit").GetString());
    }

    [Fact]
    public void OutputToJson_WithDWordDevice_OutputsDwordUnit()
    {
        // Arrange - DWordデバイスのテスト
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D200", DeviceData.FromDWordDevice(
                new DeviceSpecification(DeviceCode.D, 200, false), 0x1234, 0x5678) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D200", new DeviceEntryInfo { Name = "累積カウンタ（32ビット）", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal("dword", items[0].GetProperty("unit").GetString());
        Assert.Equal(0x56781234u, items[0].GetProperty("value").GetUInt32());
    }

    [Fact]
    public void OutputToJson_FileNameFormat_IsCorrect()
    {
        // Arrange
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 0, false), 100) }
        };

        var timestamp = new DateTime(2025, 11, 25, 10, 30, 45, 123);
        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = timestamp
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D0", new DeviceEntryInfo { Name = "テスト", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "172.30.40.15", 8192, deviceConfig);

        // Assert - ファイル名フォーマット検証
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var fileName = Path.GetFileName(files[0]);

        // xxx-xxx-x-xx_zzzz.json（日時なし）
        Assert.Matches(@"^172-30-40-15_8192\.json$", fileName);
    }

    [Fact]
    public void OutputToJson_WithoutDeviceConfig_UsesDeviceNameAsIs()
    {
        // Arrange - deviceConfigに該当デバイスがない場合
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D999", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 999, false), 42) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();  // 空の設定

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // name がデバイス名そのものになることを確認
        Assert.Equal("D999", items[0].GetProperty("name").GetString());
        Assert.Equal(1, items[0].GetProperty("digits").GetInt32());  // デフォルト値
    }

    // ========================================
    // Phase2: JSON生成機能改善テスト（TDD Red Phase）
    // ========================================

    [Fact]
    public void TC_P2_001_DeviceNumber_SingleDigit_FormattedAsThreeDigits()
    {
        // Arrange - device.number 1桁（Address=0）のテスト
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), 1) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "M0", new DeviceEntryInfo { Name = "テストビット", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert - device.numberが"000"（3桁ゼロ埋め）であることを確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        var deviceNumber = items[0].GetProperty("device").GetProperty("number").GetString();
        Assert.Equal("000", deviceNumber);  // 期待値: "000" (3桁ゼロ埋め)
    }

    [Fact]
    public void TC_P2_002_DeviceNumber_TwoDigits_FormattedAsThreeDigits()
    {
        // Arrange - device.number 2桁（Address=10）のテスト
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D10", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 10, false), 100) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D10", new DeviceEntryInfo { Name = "テストワード", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert - device.numberが"010"（3桁ゼロ埋め）であることを確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        var deviceNumber = items[0].GetProperty("device").GetProperty("number").GetString();
        Assert.Equal("010", deviceNumber);  // 期待値: "010" (3桁ゼロ埋め)
    }

    [Fact]
    public void TC_P2_003_DeviceNumber_ThreeDigits_FormattedAsThreeDigits()
    {
        // Arrange - device.number 3桁（Address=100）のテスト
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "テストワード100", Digits = 1 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);

        // Assert - device.numberが"100"（3桁そのまま）であることを確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        var deviceNumber = items[0].GetProperty("device").GetProperty("number").GetString();
        Assert.Equal("100", deviceNumber);  // 期待値: "100" (3桁そのまま)
    }

    [Fact]
    public void TC_P2_004_NonExistentDirectory_CreatesAutomatically()
    {
        // Arrange - 存在しないディレクトリパスを指定
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"andon_test_{Guid.NewGuid()}");
        
        // 事前条件: ディレクトリが存在しないことを確認
        Assert.False(Directory.Exists(nonExistentDir));

        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), 1) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "M0", new DeviceEntryInfo { Name = "テスト", Digits = 1 } }
        };

        try
        {
            // Act - 存在しないディレクトリに出力
            _manager.OutputToJson(data, nonExistentDir, "192.168.1.10", 5007, deviceConfig);

            // Assert - ディレクトリが自動作成され、ファイルが出力されることを確認
            Assert.True(Directory.Exists(nonExistentDir));
            var files = Directory.GetFiles(nonExistentDir, "*.json");
            Assert.Single(files);
        }
        finally
        {
            // クリーンアップ
            if (Directory.Exists(nonExistentDir))
            {
                Directory.Delete(nonExistentDir, true);
            }
        }
    }

    [Fact]
    public void TC_P2_005_LogOutput_OutputsStartAndCompletionLogs()
    {
        // Arrange
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), 1) },
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "M0", new DeviceEntryInfo { Name = "テストビット", Digits = 1 } },
            { "D100", new DeviceEntryInfo { Name = "テストワード", Digits = 1 } }
        };

        // コンソール出力をキャプチャ
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);

            // Assert - ログ出力を確認
            var output = stringWriter.ToString();
            
            // 期待されるログ出力:
            // [INFO] JSON出力開始: IP=192.168.1.10, Port=5007
            // [INFO] JSON出力完了: ファイル=<filename>, デバイス数=2
            Assert.Contains("[INFO] JSON出力開始: IP=192.168.1.10, Port=5007", output);
            Assert.Contains("[INFO] JSON出力完了:", output);
            Assert.Contains("デバイス数=2", output);
        }
        finally
        {
            // コンソール出力を元に戻す
            Console.SetOut(originalOut);
        }
    }

    // ========================================
    // Phase3: ビットデバイス16ビット分割処理テスト（TDD Red Phase）
    // ========================================

    /// <summary>
    /// TC_P3_001: ビットデバイス16ビット分割（基本）
    /// </summary>
    [Fact]
    public void TC_P3_001_BitDevice_SplitsInto16Bits()
    {
        // Arrange - M0デバイス（ビットデバイス）1ワード = 16ビット
        // Value = 0b1010110011010101 (43605) = ビット列: 1,0,1,0,1,1,0,0,1,1,0,1,0,1,0,1
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), (ushort)0b1010110011010101) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // deviceConfigには16ビット分のエントリを事前に登録
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"ビットM{i}", Digits = 1 };
        }

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);

        // DEBUG: JSON内容を出力
        Console.WriteLine($"[DEBUG] JSON Content:\n{jsonString}");

        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // DEBUG: アイテム数を出力
        Console.WriteLine($"[DEBUG] Items Count: {items.Count}");

        // 期待値: 16個のビットに分割される
        Assert.Equal(16, items.Count);

        // 各ビットの値を確認（ビット0から順に）
        // 0b1010110011010101 = ビット0:1, ビット1:0, ビット2:1, ビット3:0, ビット4:1, ビット5:1, ビット6:0, ビット7:0,
        //                      ビット8:1, ビット9:1, ビット10:0, ビット11:1, ビット12:0, ビット13:1, ビット14:0, ビット15:1
        int[] expectedBits = { 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 0, 1, 0, 1 };

        for (int i = 0; i < 16; i++)
        {
            var item = items[i];

            // device.code = "M"
            Assert.Equal("M", item.GetProperty("device").GetProperty("code").GetString());

            // device.number = "000", "001", ..., "015"（3桁ゼロ埋め）
            Assert.Equal(i.ToString("D3"), item.GetProperty("device").GetProperty("number").GetString());

            // name = "ビットM0", "ビットM1", ...
            Assert.Equal($"ビットM{i}", item.GetProperty("name").GetString());

            // unit = "bit"
            Assert.Equal("bit", item.GetProperty("unit").GetString());

            // value = 期待値（0 or 1）
            Assert.Equal(expectedBits[i], item.GetProperty("value").GetInt32());
        }
    }

    /// <summary>
    /// TC_P3_002: ビットデバイス16ビット分割（すべて0）
    /// </summary>
    [Fact]
    public void TC_P3_002_BitDevice_AllZeros()
    {
        // Arrange - M0デバイス（ビットデバイス）Value = 0（すべてのビットが0）
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), (ushort)0) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // deviceConfigには16ビット分のエントリを事前に登録
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"ビットM{i}", Digits = 1 };
        }

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // 期待値: 16個のビットに分割される
        Assert.Equal(16, items.Count);

        // すべてのビットが0であることを確認
        for (int i = 0; i < 16; i++)
        {
            var item = items[i];

            // device.code = "M"
            Assert.Equal("M", item.GetProperty("device").GetProperty("code").GetString());

            // device.number = "000", "001", ..., "015"（3桁ゼロ埋め）
            Assert.Equal(i.ToString("D3"), item.GetProperty("device").GetProperty("number").GetString());

            // name = "ビットM0", "ビットM1", ...
            Assert.Equal($"ビットM{i}", item.GetProperty("name").GetString());

            // unit = "bit"
            Assert.Equal("bit", item.GetProperty("unit").GetString());

            // value = 0（すべてのビットが0）
            Assert.Equal(0, item.GetProperty("value").GetInt32());
        }
    }

    /// <summary>
    /// TC_P3_003: ビットデバイス16ビット分割（すべて1）
    /// </summary>
    [Fact]
    public void TC_P3_003_BitDevice_AllOnes()
    {
        // Arrange - M0デバイス（ビットデバイス）Value = 65535（0xFFFF、すべてのビットが1）
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), (ushort)0xFFFF) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // deviceConfigには16ビット分のエントリを事前に登録
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"ビットM{i}", Digits = 1 };
        }

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // 期待値: 16個のビットに分割される
        Assert.Equal(16, items.Count);

        // すべてのビットが1であることを確認
        for (int i = 0; i < 16; i++)
        {
            var item = items[i];

            // device.code = "M"
            Assert.Equal("M", item.GetProperty("device").GetProperty("code").GetString());

            // device.number = "000", "001", ..., "015"（3桁ゼロ埋め）
            Assert.Equal(i.ToString("D3"), item.GetProperty("device").GetProperty("number").GetString());

            // name = "ビットM0", "ビットM1", ...
            Assert.Equal($"ビットM{i}", item.GetProperty("name").GetString());

            // unit = "bit"
            Assert.Equal("bit", item.GetProperty("unit").GetString());

            // value = 1（すべてのビットが1）
            Assert.Equal(1, item.GetProperty("value").GetInt32());
        }
    }

    /// <summary>
    /// TC_P3_004: ワード/ダブルワードデバイスは分割されない
    /// </summary>
    [Fact]
    public void TC_P3_004_WordDevice_NotSplit()
    {
        // Arrange - D100（ワードデバイス）
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 12345) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "生産カウンタ", Digits = 5 } }
        };

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // 期待値: 1個のエントリ（分割されない）
        Assert.Single(items);

        var item = items[0];
        Assert.Equal("D", item.GetProperty("device").GetProperty("code").GetString());
        Assert.Equal("100", item.GetProperty("device").GetProperty("number").GetString());
        Assert.Equal("生産カウンタ", item.GetProperty("name").GetString());
        Assert.Equal("word", item.GetProperty("unit").GetString());
        Assert.Equal(12345u, item.GetProperty("value").GetUInt32());
    }

    /// <summary>
    /// TC_P3_005: ビット+ワード混在
    /// </summary>
    [Fact]
    public void TC_P3_005_MixedDevices_BitAndWord()
    {
        // Arrange - M0（ビット） + D100（ワード）
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), (ushort)0b1010110011010101) },
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 12345) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // deviceConfigには16ビット分のエントリ + D100
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"ビットM{i}", Digits = 1 };
        }
        deviceConfig["D100"] = new DeviceEntryInfo { Name = "生産カウンタ", Digits = 5 };

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // 期待値: M000～M015の16エントリ + D100の1エントリ = 計17エントリ
        Assert.Equal(17, items.Count);

        // M0-M15の検証（最初の16個）
        for (int i = 0; i < 16; i++)
        {
            var item = items[i];
            Assert.Equal("M", item.GetProperty("device").GetProperty("code").GetString());
            Assert.Equal(i.ToString("D3"), item.GetProperty("device").GetProperty("number").GetString());
            Assert.Equal("bit", item.GetProperty("unit").GetString());
        }

        // D100の検証（最後の1個）
        var d100 = items[16];
        Assert.Equal("D", d100.GetProperty("device").GetProperty("code").GetString());
        Assert.Equal("100", d100.GetProperty("device").GetProperty("number").GetString());
        Assert.Equal("生産カウンタ", d100.GetProperty("name").GetString());
        Assert.Equal("word", d100.GetProperty("unit").GetString());
        Assert.Equal(12345u, d100.GetProperty("value").GetUInt32());
    }

    /// <summary>
    /// TC_P3_006: デバイス名マッピング
    /// </summary>
    [Fact]
    public void TC_P3_006_DeviceNameMapping()
    {
        // Arrange - M0（ビットデバイス）
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), (ushort)1) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // deviceConfigには16ビット分のエントリ（異なる名前を設定）
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"運転状態_{i}", Digits = 1 };
        }

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // 期待値: M000～M015の16エントリ
        Assert.Equal(16, items.Count);

        // 各ビットのデバイス名が正しくマッピングされていることを確認
        for (int i = 0; i < 16; i++)
        {
            var item = items[i];
            Assert.Equal($"運転状態_{i}", item.GetProperty("name").GetString());
        }
    }

    /// <summary>
    /// TC_P3_007: device.number 3桁ゼロ埋め（ビットデバイス）
    /// </summary>
    [Fact]
    public void TC_P3_007_BitDevice_ThreeDigitZeroPadding()
    {
        // Arrange - M0（ビットデバイス）
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "M0", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.M, 0, false), (ushort)0xFFFF) }
        };

        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now
        };

        // deviceConfigには16ビット分のエントリ
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();
        for (int i = 0; i < 16; i++)
        {
            deviceConfig[$"M{i}"] = new DeviceEntryInfo { Name = $"M{i}", Digits = 1 };
        }

        // Act
        _manager.OutputToJson(data, _testDirectory, "192.168.1.100", 5000, deviceConfig);

        // Assert - JSON出力確認
        var files = Directory.GetFiles(_testDirectory, "*.json");
        var jsonString = File.ReadAllText(files[0]);
        var jsonDoc = JsonDocument.Parse(jsonString);
        var items = jsonDoc.RootElement.GetProperty("items").EnumerateArray().ToList();

        // 期待値: device.numberが3桁ゼロ埋め（"000", "001", ..., "015"）
        for (int i = 0; i < 16; i++)
        {
            var item = items[i];
            string expectedNumber = i.ToString("D3");
            Assert.Equal(expectedNumber, item.GetProperty("device").GetProperty("number").GetString());
        }
    }

    // ========================================
    // Phase4: エラーハンドリング実装テスト（TDD Red Phase）
    // ========================================

    /// <summary>
    /// TC_P4_001: データ検証エラー（null）
    /// 入力: data = null
    /// 期待値: ArgumentNullExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC_P4_001_DataValidationError_Null()
    {
        // Arrange
        ProcessedResponseData data = null;
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);
        });

        // エラーメッセージの検証
        Assert.Contains("data", exception.ParamName);
    }

    /// <summary>
    /// TC_P4_002: データ検証エラー（空）
    /// 入力: data.ProcessedData = new Dictionary()（空）
    /// 期待値: ArgumentExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC_P4_002_DataValidationError_Empty()
    {
        // Arrange
        var data = new ProcessedResponseData
        {
            ProcessedData = new Dictionary<string, DeviceData>(),  // 空のディクショナリ
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);
        });

        // エラーメッセージの検証
        Assert.Contains("ProcessedData", exception.Message);
        Assert.Contains("空", exception.Message);
    }

    /// <summary>
    /// TC_P4_003: ポート番号不正
    /// 入力: port = 0（無効なポート番号）
    /// 期待値: ArgumentExceptionがスローされる
    /// </summary>
    [Fact]
    public void TC_P4_003_InvalidPort_ThrowsArgumentException()
    {
        // Arrange
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };
        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>();

        // Act & Assert - ポート番号0
        var exception1 = Assert.Throws<ArgumentException>(() =>
        {
            _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 0, deviceConfig);
        });
        Assert.Contains("ポート番号", exception1.Message);

        // Act & Assert - ポート番号65536（範囲外）
        var exception2 = Assert.Throws<ArgumentException>(() =>
        {
            _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 65536, deviceConfig);
        });
        Assert.Contains("ポート番号", exception2.Message);
    }

    /// <summary>
    /// TC_P4_006: 正常系ログ出力
    /// 入力: 正常なデータ
    /// 期待値: "[INFO] JSON出力開始" と "[INFO] JSON出力完了" が出力される
    /// </summary>
    [Fact]
    public void TC_P4_006_NormalOperation_OutputsLogs()
    {
        // Arrange
        var deviceData = new Dictionary<string, DeviceData>
        {
            { "D100", DeviceData.FromDeviceSpecification(
                new DeviceSpecification(DeviceCode.D, 100, false), 256) }
        };
        var data = new ProcessedResponseData
        {
            ProcessedData = deviceData,
            ProcessedAt = DateTime.Now,
            IsSuccess = true
        };
        var deviceConfig = new Dictionary<string, DeviceEntryInfo>
        {
            { "D100", new DeviceEntryInfo { Name = "温度", Digits = 1 } }
        };

        // Console出力をキャプチャ
        var output = new System.IO.StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(output);

        try
        {
            // Act
            _manager.OutputToJson(data, _testDirectory, "192.168.1.10", 5007, deviceConfig);

            // Assert
            var logOutput = output.ToString();
            Assert.Contains("[INFO] JSON出力開始", logOutput);
            Assert.Contains("[INFO] JSON出力完了", logOutput);
            Assert.Contains("IP=192.168.1.10", logOutput);
            Assert.Contains("Port=5007", logOutput);
        }
        finally
        {
            // Console出力を元に戻す
            Console.SetOut(originalOutput);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}
