using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Andon.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace Andon.Tests.Integration;

/// <summary>
/// Step1-2統合テスト (設定～フレーム構築)
/// Phase4実装: 設定ファイル読み込みからフレーム構築までの完全フロー統合テスト
/// </summary>
public class Step1_2_IntegrationTests
{
#if FALSE  // TargetDeviceConfig/DeviceEntry削除により一時的にコンパイル除外（JSON設定廃止）
    #region Phase4統合テスト

    /// <summary>
    /// TC101: 設定ファイル読み込み～フレーム構築の完全フロー
    /// Step1（設定読み込み）とStep2（フレーム構築）の統合テスト
    /// ConfigurationLoaderの機能を使用せず、手動でTargetDeviceConfigを構築して統合フローを確認
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void TC101_設定読み込みからフレーム構築まで完全実行()
    {
        // Arrange
        // Step1相当: TargetDeviceConfig構築（設定ファイルから読み込んだ想定）
        var targetDeviceConfig = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 200 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 }
            }
        };

        var configToFrameManager = new ConfigToFrameManager();

        // Act
        // Step2: フレーム構築
        byte[] frame = configToFrameManager.BuildReadRandomFrameFromConfig(targetDeviceConfig);

        // Assert
        Assert.NotNull(targetDeviceConfig);
        Assert.NotNull(frame);
        Assert.True(frame.Length > 0);

        // フレーム基本構造確認
        // サブヘッダ: 3E Binary (0x50) または 4E Binary (0x54)
        bool is3E = frame[0] == 0x50;
        bool is4E = frame[0] == 0x54;
        Assert.True(is3E || is4E, "フレームは3Eまたは4Eである必要があります");

        // コマンド確認（ReadRandom: 0x0403）
        int commandOffset = is3E ? 11 : 15; // 3E: 11, 4E: 15
        Assert.Equal(0x03, frame[commandOffset]);
        Assert.Equal(0x04, frame[commandOffset + 1]);
    }

    /// <summary>
    /// TC102: 複数PLC設定の並行フレーム構築
    /// 複数のPLC設定に対して並行にフレーム構築が可能であることを確認
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void TC102_複数PLC設定の並行フレーム構築()
    {
        // Arrange
        var configToFrameManager = new ConfigToFrameManager();

        var plc1Config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 200 }
            }
        };

        var plc2Config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 64,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 16 }
            }
        };

        // Act
        // 並行でフレーム構築
        var tasks = new[]
        {
            Task.Run(() => configToFrameManager.BuildReadRandomFrameFromConfig(plc1Config)),
            Task.Run(() => configToFrameManager.BuildReadRandomFrameFromConfig(plc2Config))
        };

        byte[][] frames = Task.WhenAll(tasks).Result;

        // Assert
        Assert.Equal(2, frames.Length);
        Assert.All(frames, frame => Assert.True(frame.Length > 0));

        // PLC1: 3Eフレーム確認
        Assert.Equal(0x50, frames[0][0]); // 3E Binary
        Assert.Equal(0x00, frames[0][1]);

        // PLC2: 4Eフレーム確認
        Assert.Equal(0x54, frames[1][0]); // 4E Binary
        Assert.Equal(0x00, frames[1][1]);
    }

    /// <summary>
    /// TC103: ConMoni実装との互換性確認
    /// ConMoniの実機稼働実績のあるフレーム構造との互換性を確認
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void TC103_ConMoni実装との互換性確認()
    {
        // Arrange
        var configToFrameManager = new ConfigToFrameManager();

        // ConMoniと同じ設定: 3Eフレーム、D100デバイス
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 }
            }
        };

        // Act
        byte[] frame = configToFrameManager.BuildReadRandomFrameFromConfig(config);

        // Assert
        // ConMoniと同じフレーム構造であることを確認

        // サブヘッダ（標準3E Binary: 0x50）
        Assert.Equal(0x50, frame[0]);
        Assert.Equal(0x00, frame[1]);

        // ネットワーク設定（ConMoniと同じ）
        Assert.Equal(0x00, frame[2]); // ネットワーク番号
        Assert.Equal(0xFF, frame[3]); // 局番（全局）
        Assert.Equal(0xFF, frame[4]); // I/O番号下位
        Assert.Equal(0x03, frame[5]); // I/O番号上位
        Assert.Equal(0x00, frame[6]); // マルチドロップ番号

        // コマンド（ReadRandom: 0x0403）
        Assert.Equal(0x03, frame[11]); // コマンド下位
        Assert.Equal(0x04, frame[12]); // コマンド上位

        // サブコマンド（ワード単位: 0x0000）
        Assert.Equal(0x00, frame[13]);
        Assert.Equal(0x00, frame[14]);

        // ワード点数
        Assert.Equal(1, frame[15]);

        // Dword点数
        Assert.Equal(0, frame[16]);

        // デバイス指定部（D100）
        Assert.Equal(0x64, frame[17]); // アドレス下位（100 = 0x64）
        Assert.Equal(0x00, frame[18]); // アドレス中位
        Assert.Equal(0x00, frame[19]); // アドレス上位
        Assert.Equal(0xA8, frame[20]); // デバイスコード（D: 0xA8）
    }

    #endregion
#endif

    #region Phase3統合テスト: PlcConfiguration用オーバーロード

    /// <summary>
    /// TC_Step123_001: PlcConfiguration設定読み込みからフレーム構築までの統合テスト
    /// Step1（PlcConfiguration構築）とStep2（フレーム構築）の統合テスト
    /// Phase1補完で実装したPlcConfiguration用オーバーロードメソッドを使用
    /// </summary>
    [Fact]
    public void TC_Step123_001_PlcConfiguration設定からフレーム構築まで完全実行()
    {
        // Step1: PlcConfiguration構築（Excel読み込みを模擬）
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "172.30.40.40",
            Port = 8192,
            MonitoringIntervalMs = 2000,
            PlcModel = "Q00JCPU",
            SavePath = "C:\\data\\output",
            SourceExcelFile = "5JRS_N2.xlsx",
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.M, 33),  // M33
                new DeviceSpecification(DeviceCode.D, 100)  // D100
            }
        };

        // 設定検証
        Assert.Equal("172.30.40.40", plcConfig.IpAddress);
        Assert.Equal(8192, plcConfig.Port);
        Assert.Equal(2, plcConfig.Devices.Count);

        // 先頭デバイス検証（M33）
        var firstDevice = plcConfig.Devices[0];
        Assert.Equal("M", firstDevice.DeviceType);
        Assert.Equal(33, firstDevice.DeviceNumber);

        // Step2: フレーム構築（ConfigToFrameManager - PlcConfigurationオーバーロード使用）
        var frameManager = new ConfigToFrameManager();
        var frameBytes = frameManager.BuildReadRandomFrameFromConfig(plcConfig);

        // フレーム基本検証
        Assert.NotNull(frameBytes);
        Assert.True(frameBytes.Length > 0);

        // 4Eフレームヘッダ検証（PlcConfigurationオーバーロードは4E固定）
        Assert.Equal(0x54, frameBytes[0]); // サブヘッダ下位
        Assert.Equal(0x00, frameBytes[1]); // サブヘッダ上位

        // ReadRandomコマンド検証 (オフセット15-16)
        Assert.Equal(0x03, frameBytes[15]); // コマンド下位
        Assert.Equal(0x04, frameBytes[16]); // コマンド上位

        // ワード点数検証（2デバイス）
        Assert.Equal(2, frameBytes[19]); // ワード点数

        // Dword点数検証（0）
        Assert.Equal(0, frameBytes[20]); // Dword点数
    }

    #endregion
}
