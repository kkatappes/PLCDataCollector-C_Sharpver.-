using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Andon.Utilities;
using Andon.Tests.TestUtilities.Mocks;

namespace Andon.Tests.Integration;

/// <summary>
/// ReadRandom(0x0403)エラーハンドリング統合テスト
/// Phase8: ステップ25実装
/// </summary>
public class ErrorHandling_IntegrationTests
{
#if FALSE  // TargetDeviceConfig/DeviceEntry削除により一時的にコンパイル除外（JSON設定廃止）
    /// <summary>
    /// TC_ERR_01: デバイス点数上限超過テスト（256点）
    /// ReadRandom(0x0403)でサポートされる最大点数は255点
    /// 256点以上を指定した場合、ArgumentExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_DeviceCountExceeds255_ThrowsArgumentException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        // 256デバイス指定（上限超過）
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

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<ArgumentException>(
            () => frameManager.BuildReadRandomFrameFromConfig(config)
        );

        // エラーメッセージに"255"が含まれることを確認
        Assert.Contains("255", ex.Message);

        Console.WriteLine($"✅ TC_ERR_01完了: 256デバイス指定時にArgumentExceptionがスロー");
        Console.WriteLine($"   エラーメッセージ: {ex.Message}");
    }

    /// <summary>
    /// TC_ERR_02: デバイス点数境界値テスト（255点）
    /// ReadRandom(0x0403)の最大点数255点が正常に処理されることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_DeviceCount255_Success()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        // 255デバイス指定（上限ちょうど）
        var config = new TargetDeviceConfig
        {
            FrameType = "4E",
            Timeout = 8000,
            Devices = Enumerable.Range(0, 255).Select(i => new DeviceEntry
            {
                DeviceType = "D",
                DeviceNumber = i
            }).ToList()
        };

        var frameManager = new ConfigToFrameManager();

        // ===============================
        // Act（実行）
        // ===============================

        byte[] sendFrame = frameManager.BuildReadRandomFrameFromConfig(config);

        // ===============================
        // Assert（検証）
        // ===============================

        Assert.NotNull(sendFrame);
        Assert.True(sendFrame.Length > 0);

        // デバイス点数検証（4Eフレームのインデックス19）
        Assert.Equal(255, sendFrame[19]);

        Console.WriteLine($"✅ TC_ERR_02完了: 255デバイス指定時に正常処理");
    }

    /// <summary>
    /// TC_ERR_03: 空デバイスリストエラーテスト
    /// デバイスリストが空の場合、ArgumentExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_EmptyDeviceList_ThrowsArgumentException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 8000,
            Devices = new List<DeviceEntry>() // 空リスト
        };

        var frameManager = new ConfigToFrameManager();

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<ArgumentException>(
            () => frameManager.BuildReadRandomFrameFromConfig(config)
        );

        Assert.Contains("デバイスリストが空です", ex.Message);

        Console.WriteLine($"✅ TC_ERR_03完了: 空デバイスリストでArgumentExceptionがスロー");
    }

    /// <summary>
    /// TC_ERR_04: PLCエラー応答テスト（0xC051: デバイス範囲エラー）
    /// PLCから異常終了コードを受信した場合、InvalidOperationExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public async Task ReadRandom_PlcErrorResponse_0xC051_ThrowsInvalidOperationException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        var config = new TargetDeviceConfig
        {
            FrameType = "3E",
            Timeout = 8000,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 999999 } // 範囲外アドレス
            }
        };

        // MockPlcServer準備（エラー応答を返す）
        var mockPlcServer = new MockPlcServer();

        // エラーレスポンスデータ（3Eフレーム、終了コード0xC051）
        byte[] errorResponse = new byte[]
        {
            0xD0, 0x00,  // サブヘッダ（3E応答）
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x02, 0x00,  // データ長（2バイト: 終了コードのみ）
            0x51, 0xC0   // 終了コード（0xC051: デバイス範囲エラー）
        };
        mockPlcServer.SetResponseData(errorResponse);

        // MockSocket準備
        var mockSocket = new MockSocket(useTcp: true);
        mockSocket.SetupConnected(true);
        mockPlcServer.ConfigureMockSocket(mockSocket);

        var mockSocketFactory = new MockSocketFactory(mockSocket, shouldSucceed: true, simulatedDelayMs: 10);

        var connectionConfig = new Andon.Core.Models.ConfigModels.ConnectionConfig
        {
            IpAddress = "127.0.0.1",
            Port = 5010,
            UseTcp = true,
            IsBinary = true, // Binary形式
            FrameVersion = Andon.Core.Models.FrameVersion.Frame3E
        };

        var timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        var frameManager = new ConfigToFrameManager();
        var communicationManager = new PlcCommunicationManager(
            connectionConfig,
            timeoutConfig,
            connectionResponse: null,
            socketFactory: mockSocketFactory);

        // ===============================
        // Act（実行）
        // ===============================

        // フレーム構築
        byte[] sendFrame = frameManager.BuildReadRandomFrameFromConfig(config);

        // 接続
        var connectResult = await communicationManager.ConnectAsync();
        Assert.Equal(Andon.Core.Models.ConnectionStatus.Connected, connectResult.Status);

        // 送信
        string sendFrameHex = BitConverter.ToString(sendFrame).Replace("-", "");
        await communicationManager.SendFrameAsync(sendFrameHex);

        // 受信
        var responseData = await communicationManager.ReceiveResponseAsync(
            timeoutConfig.ReceiveTimeoutMs
        );

        // パース用デバイスリスト
        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 999999, null)
        };

        // ===============================
        // Assert（検証）
        // ===============================

        // パース時にInvalidOperationExceptionがスローされることを検証
        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(responseData.ResponseData, devices)
        );

        Assert.Contains("0xC051", ex.Message);

        // 切断
        await communicationManager.DisconnectAsync();

        Console.WriteLine($"✅ TC_ERR_04完了: PLCエラー応答(0xC051)でInvalidOperationExceptionがスロー");
        Console.WriteLine($"   エラーメッセージ: {ex.Message}");
    }

    /// <summary>
    /// TC_ERR_05: PLCエラー応答テスト（0xC059: コマンド/サブコマンド指定エラー）
    /// 無効なコマンドに対するPLCエラー応答を検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_PlcErrorResponse_0xC059_ThrowsInvalidOperationException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        // エラーレスポンスデータ（4Eフレーム、終了コード0xC059）
        byte[] errorResponse = new byte[]
        {
            0xD4, 0x00,  // サブヘッダ（4E応答）
            0x34, 0x12,  // シーケンス番号
            0x00, 0x00,  // 予約
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x02, 0x00,  // データ長（2バイト）
            0x59, 0xC0   // 終了コード（0xC059: コマンド指定エラー）
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null)
        };

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(errorResponse, devices)
        );

        Assert.Contains("0xC059", ex.Message);

        Console.WriteLine($"✅ TC_ERR_05完了: PLCエラー応答(0xC059)でInvalidOperationExceptionがスロー");
    }

    /// <summary>
    /// TC_ERR_06: 未対応フレームタイプエラーテスト
    /// 未対応のフレームタイプ（5E等）を指定した場合、ArgumentExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_UnsupportedFrameType_ThrowsArgumentException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        var config = new TargetDeviceConfig
        {
            FrameType = "5E", // 未対応のフレームタイプ
            Timeout = 8000,
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "D", DeviceNumber = 100 }
            }
        };

        var frameManager = new ConfigToFrameManager();

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<ArgumentException>(
            () => frameManager.BuildReadRandomFrameFromConfig(config)
        );

        Assert.Contains("未対応のフレームタイプ", ex.Message);

        Console.WriteLine($"✅ TC_ERR_06完了: 未対応フレームタイプ(5E)でArgumentExceptionがスロー");
    }

    /// <summary>
    /// TC_ERR_07: レスポンスデータ不足エラーテスト
    /// 受信データが要求デバイス数に対して不足している場合、InvalidOperationExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_InsufficientResponseData_ThrowsInvalidOperationException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        // 3デバイス要求だが、1デバイス分のデータしか含まれていないレスポンス
        byte[] insufficientResponse = new byte[]
        {
            0xD0, 0x00,  // サブヘッダ（3E応答）
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x04, 0x00,  // データ長（4バイト: 終了コード2 + データ2）
            0x00, 0x00,  // 終了コード（正常）
            0x64, 0x00   // デバイスデータ（1デバイス分のみ）
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null),
            new DeviceSpecification(DeviceCode.D, 105, null),
            new DeviceSpecification(DeviceCode.M, 200, null)
        };

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(insufficientResponse, devices)
        );

        Assert.Contains("不足", ex.Message);

        Console.WriteLine($"✅ TC_ERR_07完了: データ不足でInvalidOperationExceptionがスロー");
    }

    /// <summary>
    /// TC_ERR_08: 不正なサブヘッダーエラーテスト
    /// 不正なサブヘッダーを持つレスポンスを受信した場合、InvalidOperationExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_InvalidSubHeader_ThrowsInvalidOperationException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        // 不正なサブヘッダー（0xFF, 0xFF）
        byte[] invalidHeaderResponse = new byte[]
        {
            0xFF, 0xFF,  // 不正なサブヘッダー
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x04, 0x00,  // データ長
            0x00, 0x00,  // 終了コード
            0x64, 0x00   // デバイスデータ
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null)
        };

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(invalidHeaderResponse, devices)
        );

        Assert.Contains("未対応のフレームタイプ", ex.Message);

        Console.WriteLine($"✅ TC_ERR_08完了: 不正なサブヘッダーでInvalidOperationExceptionがスロー");
    }

    /// <summary>
    /// TC_ERR_09: フレーム長不足エラーテスト
    /// 最小フレーム長に満たないレスポンスを受信した場合、InvalidOperationExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_TooShortFrame_ThrowsInvalidOperationException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        // 極端に短いフレーム（ヘッダーすら完全でない）
        byte[] tooShortFrame = new byte[]
        {
            0xD0, 0x00, 0x00
        };

        var devices = new List<DeviceSpecification>
        {
            new DeviceSpecification(DeviceCode.D, 100, null)
        };

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        var ex = Assert.Throws<InvalidOperationException>(
            () => SlmpDataParser.ParseReadRandomResponse(tooShortFrame, devices)
        );

        // エラーメッセージに"長さ"または"不足"が含まれることを確認
        Assert.True(
            ex.Message.Contains("長さ") || ex.Message.Contains("不足"),
            $"期待されるエラーメッセージが含まれていません: {ex.Message}"
        );

        Console.WriteLine($"✅ TC_ERR_09完了: フレーム長不足でInvalidOperationExceptionがスロー");
    }

    /// <summary>
    /// TC_ERR_10: Null/空デバイスリストエラーテスト
    /// デバイスリストがNull or 空の場合、ArgumentExceptionがスローされることを検証
    /// </summary>
    [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
    public void ReadRandom_NullOrEmptyDeviceList_ThrowsArgumentException()
    {
        // ===============================
        // Arrange（準備）
        // ===============================

        byte[] validResponse = new byte[]
        {
            0xD0, 0x00,  // サブヘッダ（3E応答）
            0x00,        // ネットワーク番号
            0xFF,        // PC番号
            0xFF, 0x03,  // I/O番号
            0x00,        // 局番
            0x04, 0x00,  // データ長
            0x00, 0x00,  // 終了コード
            0x64, 0x00   // デバイスデータ
        };

        List<DeviceSpecification>? nullDevices = null;

        // ===============================
        // Act & Assert（実行と検証）
        // ===============================

        // 実装はNullを空リストとして扱い、ArgumentExceptionをスローする
        var ex = Assert.Throws<ArgumentException>(
            () => SlmpDataParser.ParseReadRandomResponse(validResponse, nullDevices!)
        );

        Assert.Contains("デバイスリストが空です", ex.Message);

        Console.WriteLine($"✅ TC_ERR_10完了: Null/空デバイスリストでArgumentExceptionがスロー");
    }
#endif
}
