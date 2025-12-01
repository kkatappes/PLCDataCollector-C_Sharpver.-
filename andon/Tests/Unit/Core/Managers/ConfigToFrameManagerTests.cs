using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// ConfigToFrameManager単体テスト
/// Phase4-ステップ12: ReadRandomフレーム構築テスト
/// </summary>
public class ConfigToFrameManagerTests
{
    /// <summary>
    /// TC_Step12_001: BuildReadRandomFrameFromConfig() - 正常系（4Eフレーム、48デバイス）
    /// </summary>
    [Fact]
    public void TC_Step12_001_BuildReadRandomFrameFromConfig_正常系_4Eフレーム_48デバイス()
    {
        // Arrange
        var config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 16 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 32 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 48 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 64 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 80 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 96 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 112 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 128 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 144 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 160 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 176 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 192 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 208 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 224 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 240 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 256 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 272 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 288 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 304 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 320 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 336 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 352 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 368 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 384 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 400 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 416 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 432 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 448 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 464 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 480 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 496 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 512 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 528 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 544 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 560 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 576 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 592 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 608 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 624 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 640 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 656 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 672 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 688 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 704 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 720 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 736 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 752 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act
        byte[] frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        Assert.NotNull(frame);
        Assert.Equal(213, frame.Length);  // 4Eフレーム: 48デバイス × 4バイト + ヘッダ = 213バイト

        // 4Eフレームサブヘッダ検証
        Assert.Equal(0x54, frame[0]);
        Assert.Equal(0x00, frame[1]);

        // コマンド検証（ReadRandom: 0x0403）
        int commandOffset = 15; // 4Eフレーム: サブヘッダ6 + ネットワーク情報5 + データ長2 + 監視タイマ2
        Assert.Equal(0x03, frame[commandOffset]);
        Assert.Equal(0x04, frame[commandOffset + 1]);

        // ワード点数検証
        int wordCountOffset = 19; // コマンド2 + サブコマンド2 = 4バイト後
        Assert.Equal(48, frame[wordCountOffset]);

        // Dword点数検証（0固定）
        Assert.Equal(0x00, frame[wordCountOffset + 1]);
    }

    /// <summary>
    /// TC_Step12_002: BuildReadRandomFrameFromConfig() - 3Eフレーム検証
    /// </summary>
    [Fact]
    public void TC_Step12_002_BuildReadRandomFrameFromConfig_正常系_3Eフレーム()
    {
        // Arrange
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 16,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 101 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 102 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act
        byte[] frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        Assert.NotNull(frame);
        // 3Eフレーム: 3デバイス × 4バイト + ヘッダ = 29バイト
        Assert.Equal(29, frame.Length);

        // 3Eフレームサブヘッダ検証
        Assert.Equal(0x50, frame[0]);
        Assert.Equal(0x00, frame[1]);

        // コマンド検証（ReadRandom: 0x0403）
        int commandOffset = 11; // 3Eフレーム: サブヘッダ2 + ネットワーク情報5 + データ長2 + 監視タイマ2
        Assert.Equal(0x03, frame[commandOffset]);
        Assert.Equal(0x04, frame[commandOffset + 1]);

        // ワード点数検証
        int wordCountOffset = 15; // コマンド2 + サブコマンド2 = 4バイト後
        Assert.Equal(3, frame[wordCountOffset]);
    }

    /// <summary>
    /// TC_Step12_003: BuildReadRandomFrameFromConfig() - 異常系（デバイスリストが空）
    /// </summary>
    [Fact]
    public void TC_Step12_003_BuildReadRandomFrameFromConfig_異常系_デバイスリスト空()
    {
        // Arrange
        var config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 32,
            Devices = new List<DeviceEntry>()  // 空リスト
        };

        var manager = new ConfigToFrameManager();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            manager.BuildReadRandomFrameFromConfig(config));

        Assert.Contains("デバイスリストが空です", exception.Message);
    }

    /// <summary>
    /// TC_Step12_004: BuildReadRandomFrameFromConfig() - 異常系（config null）
    /// </summary>
    [Fact]
    public void TC_Step12_004_BuildReadRandomFrameFromConfig_異常系_ConfigNull()
    {
        // Arrange
        var manager = new ConfigToFrameManager();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            manager.BuildReadRandomFrameFromConfig((TargetDeviceConfig)null!));

        Assert.Equal("config", exception.ParamName);
    }

    /// <summary>
    /// TC_Step12_005: BuildReadRandomFrameFromConfig() - 異常系（未対応フレームタイプ）
    /// </summary>
    [Fact]
    public void TC_Step12_005_BuildReadRandomFrameFromConfig_異常系_未対応フレームタイプ()
    {
        // Arrange
        var config = new TargetDeviceConfig
        {
            FrameType = "5E",  // 未対応
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            manager.BuildReadRandomFrameFromConfig(config));

        Assert.Contains("未対応のフレームタイプ", exception.Message);
    }

    #region Phase2拡張: ASCII形式対応テスト

    /// <summary>
    /// TC_Step12_ASCII_001: BuildReadRandomFrameFromConfigAscii - 正常系 - 4Eフレーム48デバイス（ASCII形式）
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_001_BuildReadRandomFrameFromConfigAscii_正常系_4Eフレーム_48デバイス()
    {
        // Arrange: conmoni_testと同じ48デバイス設定
        var config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 32,
            Devices = new List<DeviceEntry>()
        };

        int[] deviceNumbers = { 61000, 61003, 61006, 61009, 61012, 61015, 61018, 61021, 61024, 61027, 61030, 61033, 61036, 61039, 61042, 61045, 61048, 61051, 61054, 61057, 61060, 61063, 61066, 61069, 61072, 61075, 61078, 61081, 61084, 61087, 61090, 61093, 61096, 61099, 61102, 61105, 61108, 61111, 61114, 61117, 61120, 61123, 61126, 61129, 61132, 61135, 61138, 61141 };

        foreach (var deviceNum in deviceNumbers)
        {
            config.Devices.Add(new DeviceEntry { DeviceType = "D", DeviceNumber = deviceNum });
        }

        var manager = new ConfigToFrameManager();

        // Act: ASCII形式フレーム構築
        var asciiFrame = manager.BuildReadRandomFrameFromConfigAscii(config);

        // Assert: ASCII文字列であることを確認
        Assert.NotNull(asciiFrame);
        Assert.True(asciiFrame.Length > 0);
        Assert.True(asciiFrame.Length % 2 == 0);  // hex文字列は偶数長

        // Assert: サブヘッダ（4Eフレーム: "5400"）
        Assert.StartsWith("5400", asciiFrame);

        // Assert: 213バイト = 426文字（hex）
        Assert.Equal(213 * 2, asciiFrame.Length);

        // Assert: Binary形式との構造一致確認（シーケンス番号は異なるため除外）
        var binaryFrame = manager.BuildReadRandomFrameFromConfig(config);
        var expectedAscii = Convert.ToHexString(binaryFrame);

        // 4Eフレーム: シーケンス番号（位置4-7の4文字）以外を比較
        Assert.Equal(expectedAscii.Substring(0, 4), asciiFrame.Substring(0, 4)); // サブヘッダ
        Assert.Equal(expectedAscii.Substring(8), asciiFrame.Substring(8)); // シーケンス番号以降
    }

    /// <summary>
    /// TC_Step12_ASCII_002: BuildReadRandomFrameFromConfigAscii - 正常系 - 3Eフレーム（ASCII形式）
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_002_BuildReadRandomFrameFromConfigAscii_正常系_3Eフレーム()
    {
        // Arrange: 3デバイス設定
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 200 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 300 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act: ASCII形式フレーム構築
        var asciiFrame = manager.BuildReadRandomFrameFromConfigAscii(config);

        // Assert: ASCII文字列であることを確認
        Assert.NotNull(asciiFrame);
        Assert.True(asciiFrame.Length > 0);

        // Assert: サブヘッダ（3Eフレーム: "5000"）
        Assert.StartsWith("5000", asciiFrame);

        // Assert: Binary形式との構造一致確認
        // 3Eフレームはシーケンス番号がないため、完全一致を確認
        var binaryFrame = manager.BuildReadRandomFrameFromConfig(config);
        var expectedAscii = Convert.ToHexString(binaryFrame);
        Assert.Equal(expectedAscii, asciiFrame);
    }

    /// <summary>
    /// TC_Step12_ASCII_003: BuildReadRandomFrameFromConfigAscii - 異常系 - デバイスリスト空
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_003_BuildReadRandomFrameFromConfigAscii_異常系_デバイスリスト空()
    {
        // Arrange: 空デバイスリスト
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>()
        };

        var manager = new ConfigToFrameManager();

        // Act & Assert: ArgumentException発生
        var exception = Assert.Throws<ArgumentException>(() =>
            manager.BuildReadRandomFrameFromConfigAscii(config));

        Assert.Contains("デバイスリストが空です", exception.Message);
    }

    /// <summary>
    /// TC_Step12_ASCII_004: BuildReadRandomFrameFromConfigAscii - 異常系 - Config null
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_004_BuildReadRandomFrameFromConfigAscii_異常系_ConfigNull()
    {
        // Arrange
        TargetDeviceConfig? config = null;
        var manager = new ConfigToFrameManager();

        // Act & Assert: ArgumentNullException発生
        Assert.Throws<ArgumentNullException>(() =>
            manager.BuildReadRandomFrameFromConfigAscii(config!));
    }

    /// <summary>
    /// TC_Step12_ASCII_005: BuildReadRandomFrameFromConfigAscii - 異常系 - 未対応フレームタイプ
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_005_BuildReadRandomFrameFromConfigAscii_異常系_未対応フレームタイプ()
    {
        // Arrange: 未対応フレームタイプ
        var config = new TargetDeviceConfig
        {
            FrameType = "5E",  // 未対応
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act & Assert: ArgumentException発生
        var exception = Assert.Throws<ArgumentException>(() =>
            manager.BuildReadRandomFrameFromConfigAscii(config));

        Assert.Contains("未対応のフレームタイプ", exception.Message);
    }

    #endregion

    #region Phase4追加テスト: ConfigToFrameManager統合テスト

    /// <summary>
    /// TC019: Phase2統合確認 - Phase2でリファクタリングしたSlmpFrameBuilderとの統合確認
    /// </summary>
    [Fact]
    public void TC019_BuildReadRandomFrameFromConfig_Phase2統合_正しいフレーム構築()
    {
        // Arrange
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act
        byte[] frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        Assert.NotNull(frame);
        Assert.True(frame.Length > 0);

        // サブヘッダ確認（3E Binary）
        Assert.Equal(0x50, frame[0]);
        Assert.Equal(0x00, frame[1]);

        // ネットワーク設定確認
        Assert.Equal(0x00, frame[2]); // ネットワーク番号
        Assert.Equal(0xFF, frame[3]); // 局番
        Assert.Equal(0xFF, frame[4]); // I/O番号（下位）
        Assert.Equal(0x03, frame[5]); // I/O番号（上位）
        Assert.Equal(0x00, frame[6]); // マルチドロップ

        // データ長確認（監視タイマ2 + コマンド2 + サブコマンド2 + ワード点数1 + Dword点数1 + デバイス指定4）
        ushort dataLength = BitConverter.ToUInt16(frame, 7);
        Assert.Equal(12, dataLength);

        // コマンド確認（ReadRandom: 0x0403）
        Assert.Equal(0x03, frame[11]); // コマンド下位
        Assert.Equal(0x04, frame[12]); // コマンド上位

        // サブコマンド確認（ワード単位: 0x0000）
        Assert.Equal(0x00, frame[13]);
        Assert.Equal(0x00, frame[14]);

        // ワード点数確認
        Assert.Equal(1, frame[15]);

        // Dword点数確認
        Assert.Equal(0, frame[16]);

        // デバイス指定部確認（D100: アドレス0x000064、コード0xA8）
        int deviceSectionStart = 17;
        Assert.Equal(0x64, frame[deviceSectionStart]);     // アドレス下位
        Assert.Equal(0x00, frame[deviceSectionStart + 1]); // アドレス中位
        Assert.Equal(0x00, frame[deviceSectionStart + 2]); // アドレス上位
        Assert.Equal(0xA8, frame[deviceSectionStart + 3]); // デバイスコード
    }

    /// <summary>
    /// TC020: ReadRandom非対応デバイス統合テスト - ConfigToFrameManagerでのエラーハンドリング確認
    /// </summary>
    [Fact]
    public void TC020_BuildReadRandomFrameFromConfig_TS指定_ArgumentExceptionをスロー()
    {
        // Arrange
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 32,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "TS", DeviceNumber = 0 }
            }
        };

        var manager = new ConfigToFrameManager();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => manager.BuildReadRandomFrameFromConfig(config)
        );

        Assert.Contains("ReadRandomコマンドは", exception.Message);
        Assert.Contains("TS", exception.Message);
    }

    #endregion

    #region Phase2: PlcConfiguration版オーバーロードテスト（ASCII形式）

    /// <summary>
    /// Round 5: ASCII版null検証（異常系）
    /// PlcConfigurationがnullの場合、ArgumentNullExceptionをスロー
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_004_PlcConfig_BuildReadRandomFrameFromConfigAscii_異常系_ConfigNull()
    {
        // Arrange
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            frameManager.BuildReadRandomFrameFromConfigAscii((PlcConfiguration)null));
    }

    /// <summary>
    /// Round 6: ASCII版空リスト検証（異常系）
    /// PlcConfigurationのDevicesが空の場合、ArgumentExceptionをスロー
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_003_PlcConfig_BuildReadRandomFrameFromConfigAscii_異常系_デバイスリスト空()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            Devices = new List<DeviceSpecification>()
        };
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            frameManager.BuildReadRandomFrameFromConfigAscii(plcConfig));
    }

    /// <summary>
    /// Round 7: ASCII版フレーム構築（正常系）
    /// PlcConfigurationから4EフレームのASCII形式を構築
    /// </summary>
    [Fact]
    public void TC_Step12_ASCII_001_PlcConfig_BuildReadRandomFrameFromConfigAscii_正常系_4Eフレーム()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "172.30.40.40",
            Port = 8192,
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.M, 33, false)
            }
        };

        var frameManager = new ConfigToFrameManager();

        // Act
        var asciiFrame = frameManager.BuildReadRandomFrameFromConfigAscii(plcConfig);

        // Assert
        Assert.NotNull(asciiFrame);
        Assert.True(asciiFrame.Length > 0);

        // 4EフレームASCIIヘッダ検証
        Assert.StartsWith("5400", asciiFrame); // サブヘッダ "54 00" の ASCII表現

        // ReadRandomコマンド検証 (ASCII形式では文字列オフセット30-33)
        // 4Eフレーム構造: サブヘッダ(2) + 予約1(2) + シーケンス(4) + 予約2(4) + ネットワーク(2) + PC(2) + I/O(4) + 局番(2) + データ長(4) + 監視タイマ(4) + コマンド(4)
        // オフセット30からコマンド
        // Note: リトルエンディアンのため、0x0403は"0304"として表現される
        Assert.Contains("0304", asciiFrame.Substring(30, 4)); // コマンド 0x0403 (LE)
    }


    #endregion

    #region Phase1: PlcConfiguration版オーバーロードテスト（Binary形式）

    /// <summary>
    /// Round 1: Binary版null検証（異常系）
    /// PlcConfigurationがnullの場合、ArgumentNullExceptionをスロー
    /// </summary>
    [Fact]
    public void TC_Step12_Binary_004_PlcConfig_BuildReadRandomFrameFromConfig_異常系_ConfigNull()
    {
        // Arrange
        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            frameManager.BuildReadRandomFrameFromConfig((PlcConfiguration)null));
    }

    /// <summary>
    /// Round 2: Binary版空リスト検証（異常系）
    /// PlcConfigurationのDevicesが空の場合、ArgumentExceptionをスロー
    /// </summary>
    [Fact]
    public void TC_Step12_Binary_003_PlcConfig_BuildReadRandomFrameFromConfig_異常系_デバイスリスト空()
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
    /// Round 3: Binary版フレーム構築（正常系）
    /// PlcConfigurationから4EフレームのBinary形式を構築
    /// </summary>
    [Fact]
    public void TC_Step12_Binary_001_PlcConfig_BuildReadRandomFrameFromConfig_正常系_4Eフレーム()
    {
        // Arrange
        var plcConfig = new PlcConfiguration
        {
            IpAddress = "172.30.40.40",
            Port = 8192,
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.M, 33, false),
                new DeviceSpecification(DeviceCode.D, 100, false)
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

    #endregion

    #region Phase4: ハードコード置き換えテスト（PlcConfiguration版）

    /// <summary>
    /// Phase4-Red-001: Binary版 - FrameVersion="3E"が正しく使用されることを確認
    /// </summary>
    [Fact]
    public void Phase4_Red_001_PlcConfig_BuildReadRandomFrame_ShouldUseFrameVersion3E()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "3E",
            Timeout = 1000,  // ミリ秒
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        // 3Eフレームの場合、サブヘッダは 0x50, 0x00
        Assert.Equal(0x50, frame[0]);
        Assert.Equal(0x00, frame[1]);
    }

    /// <summary>
    /// Phase4-Red-002: Binary版 - FrameVersion="4E"が正しく使用されることを確認
    /// </summary>
    [Fact]
    public void Phase4_Red_002_PlcConfig_BuildReadRandomFrame_ShouldUseFrameVersion4E()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "4E",
            Timeout = 1000,  // ミリ秒
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        // 4Eフレームの場合、サブヘッダは 0x54, 0x00
        Assert.Equal(0x54, frame[0]);
        Assert.Equal(0x00, frame[1]);
    }

    /// <summary>
    /// Phase4-Red-003: Binary版 - Timeoutが正しく使用されることを確認（4Eフレーム）
    /// </summary>
    [Fact]
    public void Phase4_Red_003_PlcConfig_BuildReadRandomFrame_ShouldUseTimeout_4E()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "4E",
            Timeout = 2000,  // 2000ms → SLMP単位: 8 (2000 / 250)
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        // 4Eフレームの場合、タイムアウトは13-14バイト目（リトルエンディアン）
        // 2000ms → 8単位 → 0x08, 0x00
        Assert.Equal(0x08, frame[13]);
        Assert.Equal(0x00, frame[14]);
    }

    /// <summary>
    /// Phase4-Red-004: Binary版 - Timeoutが正しく使用されることを確認（3Eフレーム）
    /// </summary>
    [Fact]
    public void Phase4_Red_004_PlcConfig_BuildReadRandomFrame_ShouldUseTimeout_3E()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "3E",
            Timeout = 3000,  // 3000ms → SLMP単位: 12 (3000 / 250)
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var frame = manager.BuildReadRandomFrameFromConfig(config);

        // Assert
        // 3Eフレームの場合、タイムアウトは9-10バイト目（リトルエンディアン）
        // 3000ms → 12単位 → 0x0C, 0x00
        Assert.Equal(0x0C, frame[9]);
        Assert.Equal(0x00, frame[10]);
    }

    /// <summary>
    /// Phase4-Red-005: ASCII版 - FrameVersion="3E"が正しく使用されることを確認
    /// </summary>
    [Fact]
    public void Phase4_Red_005_PlcConfig_BuildReadRandomFrameAscii_ShouldUseFrameVersion3E()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "3E",
            Timeout = 1000,  // ミリ秒
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var asciiFrame = manager.BuildReadRandomFrameFromConfigAscii(config);

        // Assert
        // 3EフレームASCII形式の場合、サブヘッダは "5000"
        Assert.StartsWith("5000", asciiFrame);
    }

    /// <summary>
    /// Phase4-Red-006: ASCII版 - FrameVersion="4E"が正しく使用されることを確認
    /// </summary>
    [Fact]
    public void Phase4_Red_006_PlcConfig_BuildReadRandomFrameAscii_ShouldUseFrameVersion4E()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "4E",
            Timeout = 1000,  // ミリ秒
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var asciiFrame = manager.BuildReadRandomFrameFromConfigAscii(config);

        // Assert
        // 4EフレームASCII形式の場合、サブヘッダは "5400"
        Assert.StartsWith("5400", asciiFrame);
    }

    /// <summary>
    /// Phase4-Red-007: ASCII版 - Timeoutが正しく使用されることを確認
    /// </summary>
    [Fact]
    public void Phase4_Red_007_PlcConfig_BuildReadRandomFrameAscii_ShouldUseTimeout()
    {
        // Arrange
        var config = new PlcConfiguration
        {
            FrameVersion = "4E",
            Timeout = 2000,  // 2000ms → SLMP単位: 8
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 0, false)
            }
        };
        var manager = new ConfigToFrameManager();

        // Act
        var asciiFrame = manager.BuildReadRandomFrameFromConfigAscii(config);

        // Assert
        // タイムアウト値が正しく設定されているか確認
        // 4EフレームASCII形式の場合、タイムアウトは26-29文字目（16進数4文字、リトルエンディアン）
        // 2000ms → 8単位 → "0800"
        Assert.Contains("0800", asciiFrame.Substring(26, 4));
    }

    #endregion
}
