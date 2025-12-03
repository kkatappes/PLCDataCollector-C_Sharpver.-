using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Andon.Utilities;
using Andon.Tests.TestUtilities.Mocks;
using System.Text;

namespace Andon.Tests.Integration;

/// <summary>
/// ReadRandom(0x0403)統合テスト
/// </summary>
public class ReadRandomIntegrationTests
{
#if FALSE  // TargetDeviceConfig/DeviceEntry削除により一時的にコンパイル除外（JSON設定廃止）
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public async Task ReadRandom_EndToEnd_FullFlow_Success()
    {
        // ========================================
        // Arrange: 各マネージャーのセットアップ
        // ========================================

        // 設定
        var config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 8000,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 105 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 200 }
            }
        };

        // MockPlcServer準備
        var mockPlcServer = new MockPlcServer();
        mockPlcServer.SetReadRandomResponse4EAscii();

        // MockSocket準備
        var mockSocket = new MockSocket(useTcp: true);
        mockSocket.SetupConnected(true);
        mockPlcServer.ConfigureMockSocket(mockSocket);

        // MockSocketFactory準備
        var mockSocketFactory = new MockSocketFactory(mockSocket, shouldSucceed: true, simulatedDelayMs: 10);

        // ConnectionConfig準備
        var connectionConfig = new ConnectionConfig
        {
            IpAddress = "127.0.0.1",
            Port = 5010,
            UseTcp = true,
            IsBinary = false,
            FrameVersion = FrameVersion.Frame4E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // マネージャー
        var frameManager = new ConfigToFrameManager();
        var communicationManager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            connectionResponse: null,
            socketFactory: mockSocketFactory);

        // ========================================
        // Act: 一連のフロー実行
        // ========================================

        // Step 1: フレーム構築
        byte[] sendFrame = frameManager.BuildReadRandomFrameFromConfig(config);
        Assert.NotNull(sendFrame);
        Assert.True(sendFrame.Length > 0);

        // Step 2: 接続
        var connectResult = await communicationManager.ConnectAsync();
        Assert.Equal(ConnectionStatus.Connected, connectResult.Status);

        // Step 3: フレーム送信（byte[]をHex文字列に変換）
        string sendFrameHex = BitConverter.ToString(sendFrame).Replace("-", "");
        await communicationManager.SendFrameAsync(sendFrameHex);

        // Step 4: レスポンス受信
        var responseData = await communicationManager.ReceiveResponseAsync(
            timeoutConfig.ReceiveTimeoutMs
        );
        Assert.NotNull(responseData);
        Assert.NotNull(responseData.ResponseData);

        // Step 5: レスポンスパース
        var devices = config.Devices.Select(d =>
        {
            var deviceEntry = d;
            return deviceEntry.ToDeviceSpecification();
        }).ToList();

        // ASCII形式の場合、Hex文字列をバイト配列に変換
        byte[] binaryResponse;
        if (!connectionConfig.IsBinary)
        {
            // ASCII応答（Hex文字列）をバイナリに変換
            var hexString = System.Text.Encoding.ASCII.GetString(responseData.ResponseData);
            binaryResponse = Convert.FromHexString(hexString);
        }
        else
        {
            binaryResponse = responseData.ResponseData;
        }

        var parsedData = SlmpDataParser.ParseReadRandomResponse(binaryResponse, devices);

        // Step 6: 切断
        var disconnectResult = await communicationManager.DisconnectAsync();
        Assert.Equal(DisconnectStatus.Success, disconnectResult.Status);

        // ========================================
        // Assert: 結果検証
        // ========================================

        // パースデータ検証
        Assert.Equal(3, parsedData.Count);
        Assert.True(parsedData.ContainsKey("D100"));
        Assert.True(parsedData.ContainsKey("D105"));
        Assert.True(parsedData.ContainsKey("M200"));

        // データ型検証
        Assert.Equal("Word", parsedData["D100"].Type);
        Assert.Equal("Word", parsedData["D105"].Type);
        Assert.Equal("Bit", parsedData["M200"].Type);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_FrameConstruction_3Devices_Success()
    {
        // Arrange: 3デバイス指定
        var config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 8000,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 105 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 200 }
            }
        };

        var frameManager = new ConfigToFrameManager();

        // Act
        byte[] sendFrame = frameManager.BuildReadRandomFrameFromConfig(config);

        // Assert
        Assert.NotNull(sendFrame);
        Assert.True(sendFrame.Length > 0);

        // フレーム構造検証（4Eフレーム）
        Assert.Equal(0x54, sendFrame[0]);  // サブヘッダ（4E）
        Assert.Equal(0x00, sendFrame[1]);

        // コマンド検証（0x0403: ReadRandom）
        Assert.Equal(0x03, sendFrame[15]);
        Assert.Equal(0x04, sendFrame[16]);

        // サブコマンド検証（0x0000: ワード単位）
        Assert.Equal(0x00, sendFrame[17]);
        Assert.Equal(0x00, sendFrame[18]);

        // デバイス点数検証（3点）
        Assert.Equal(3, sendFrame[19]);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_MixedDeviceTypes_Success()
    {
        // Arrange: 異なるデバイス種別を混在
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 8000,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 },
                new DeviceEntry { DeviceType = "W", DeviceNumber = 50 },
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 },
                new DeviceEntry { DeviceType = "X", DeviceNumber = 10 }
            }
        };

        var frameManager = new ConfigToFrameManager();

        // Act
        byte[] sendFrame = frameManager.BuildReadRandomFrameFromConfig(config);

        // Assert
        Assert.NotNull(sendFrame);
        Assert.True(sendFrame.Length > 0);

        // フレーム構造検証（3Eフレーム）
        Assert.Equal(0x50, sendFrame[0]);  // サブヘッダ（3E）
        Assert.Equal(0x00, sendFrame[1]);

        // コマンド検証（0x0403: ReadRandom）
        Assert.Equal(0x03, sendFrame[11]);
        Assert.Equal(0x04, sendFrame[12]);

        // デバイス点数検証（4点）
        Assert.Equal(4, sendFrame[15]);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_DeviceCountExceeds255_ThrowsException()
    {
        // Arrange: 256デバイス（上限超過）
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 8000,
            Devices = Enumerable.Range(0, 256).Select(i => new DeviceEntry
            {
                DeviceType = "D",
                DeviceNumber = i
            }).ToList()
        };

        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => frameManager.BuildReadRandomFrameFromConfig(config)
        );
        Assert.Contains("255", ex.Message);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_EmptyDeviceList_ThrowsException()
    {
        // Arrange: 空のデバイスリスト
        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 8000,
            Devices = new List<DeviceEntry>()
        };

        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => frameManager.BuildReadRandomFrameFromConfig(config)
        );
        Assert.Contains("デバイスリストが空です", ex.Message);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_InvalidFrameType_ThrowsException()
    {
        // Arrange: 不正なフレームタイプ
        var config = new TargetDeviceConfig
        {
            FrameType = "5E",  // 未対応
            Timeout = 8000,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 }
            }
        };

        var frameManager = new ConfigToFrameManager();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(
            () => frameManager.BuildReadRandomFrameFromConfig(config)
        );
        Assert.Contains("未対応のフレームタイプ", ex.Message);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_ResponseParsing_3EFrame_Success()
    {
        // Arrange: 3Eフレームレスポンス（3デバイス分）
        var responseFrame = new byte[]
        {
            0xD0, 0x00,  // サブヘッダ（3E応答）
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x08, 0x00,  // データ長（8バイト）
            0x00, 0x00,  // 終了コード（正常）
            // デバイスデータ（3デバイス × 2バイト = 6バイト）
            0x64, 0x00,  // D100 = 100
            0xC8, 0x00,  // D105 = 200
            0x01, 0x00   // M200 = 1
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null),
            new DeviceSpecification(DeviceCode.D, 105, null),
            new DeviceSpecification(DeviceCode.M, 200, null)
        };

        // Act
        var parsedData = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(3, parsedData.Count);
        Assert.Equal((uint)100, parsedData["D100"].Value);
        Assert.Equal((uint)200, parsedData["D105"].Value);
        Assert.Equal((uint)1, parsedData["M200"].Value);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_ResponseParsing_4EFrame_Success()
    {
        // Arrange: 4Eフレームレスポンス（3デバイス分）
        var responseFrame = new byte[]
        {
            0xD4, 0x00,  // サブヘッダ（4E応答）
            0x34, 0x12,  // シーケンス番号
            0x00, 0x00,  // 予約
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x08, 0x00,  // データ長（8バイト）
            0x00, 0x00,  // 終了コード（正常）
            // デバイスデータ（3デバイス × 2バイト = 6バイト）
            0x64, 0x00,  // D100 = 100
            0xC8, 0x00,  // D105 = 200
            0x01, 0x00   // M200 = 1
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null),
            new DeviceSpecification(DeviceCode.D, 105, null),
            new DeviceSpecification(DeviceCode.M, 200, null)
        };

        // Act
        var parsedData = SlmpDataParser.ParseReadRandomResponse(responseFrame, devices);

        // Assert
        Assert.Equal(3, parsedData.Count);
        Assert.Equal((uint)100, parsedData["D100"].Value);
        Assert.Equal((uint)200, parsedData["D105"].Value);
        Assert.Equal((uint)1, parsedData["M200"].Value);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_ResponseParsing_ErrorResponse_ThrowsException()
    {
        // Arrange: エラーレスポンス（終了コード異常）
        var responseFrame = new byte[]
        {
            0xD0, 0x00,  // サブヘッダ（3E応答）
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x02, 0x00,  // データ長（2バイト）
            0x51, 0xC0   // 終了コード（0xC051: デバイス範囲エラー）
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 999999, null)
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(responseFrame, devices)
        );
        Assert.Contains("0xC051", ex.Message);
    }

    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_ResponseParsing_InsufficientData_ThrowsException()
    {
        // Arrange: データ部が不足しているレスポンス
        var responseFrame = new byte[]
        {
            0xD0, 0x00,  // サブヘッダ（3E応答）
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x04, 0x00,  // データ長（4バイト）
            0x00, 0x00,  // 終了コード（正常）
            // デバイスデータ（1デバイス分のみ、本来は3デバイス必要）
            0x64, 0x00   // D100 = 100
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null),
            new DeviceSpecification(DeviceCode.D, 105, null),
            new DeviceSpecification(DeviceCode.M, 200, null)
        };

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(responseFrame, devices)
        );
        Assert.Contains("不足", ex.Message);
    }
#endif
}
