using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Tests.TestUtilities.Mocks;

namespace Andon.Tests.Integration;

/// <summary>
/// TC143_10: Step3-6 M100～M107ビット読み出し4パターン統合テスト
/// 目的: 3E/4E × バイナリ/ASCIIの4パターンでM100～M107ビット読み出しの完全サイクルを統合検証
///
/// 【レガシーテスト】
/// このテストはRead(0x0401)コマンドを使用した歴史的テストです。
/// ProcessedDeviceRequestInfoと共に「テスト専用資産」として保持されています（Phase12決定事項）。
/// 本番実装ではReadRandom(0x0403)を使用してください。
/// </summary>
public class PlcCommunicationManager_IntegrationTests_TC143_10 : IDisposable
{
    // MockUdpServerインスタンス（2パターン用：バイナリのみ）
    private readonly MockUdpServer _mockServer1; // 3E × Binary (Port 5001)
    private readonly MockUdpServer _mockServer3; // 4E × Binary (Port 5003)

    private readonly TimeoutConfig _timeoutConfig;
    private readonly ProcessedDeviceRequestInfo _deviceRequestInfo;

    public PlcCommunicationManager_IntegrationTests_TC143_10()
    {
        // タイムアウト設定
        _timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // ProcessedDeviceRequestInfo準備（M100～M107: 8ビット）
        _deviceRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "M",
            StartAddress = 100,
            Count = 8,
            FrameType = FrameType.Unknown, // テスト時に3E/4Eを指定
            RequestedAt = DateTime.UtcNow,
            ParseConfiguration = null
        };

        // MockUdpServer初期化（2パターン：バイナリのみ）
        _mockServer1 = new MockUdpServer("127.0.0.1", 5001);
        _mockServer3 = new MockUdpServer("127.0.0.1", 5003);

        // 各サーバーにM100～M107ビット読み出し応答データを設定
        SetupMockServer_3E_Binary(_mockServer1);
        SetupMockServer_4E_Binary(_mockServer3);

        // 全サーバー起動
        _mockServer1.Start();
        _mockServer3.Start();
    }

    /// <summary>
    /// パターン1: 3Eバイナリフレーム用MockUdpServer設定
    /// </summary>
    private void SetupMockServer_3E_Binary(MockUdpServer server)
    {
        // リクエストフレーム（3Eバイナリ、M100～M107ビット読み出し）
        // サブヘッダ: 0x5000 (2bytes)
        // ネットワーク番号: 0x00 (1byte)
        // PC番号: 0xFF (1byte)
        // 要求先ユニットI/O番号: 0xFF03 (2bytes, Little Endian)
        // 要求先ユニット局番号: 0x00 (1byte)
        // 要求データ長: 0x0C00 (12bytes, Little Endian)
        // CPU監視タイマー: 0x0000 (2bytes)
        // コマンド: 0x0104 (2bytes, Little Endian) - デバイス一括読み出し
        // サブコマンド: 0x0100 (2bytes, Little Endian) - ビット単位
        // 開始デバイス番号: 0x000064 (3bytes, M100)
        // デバイスコード: 0x90 (1byte) - M機器
        // デバイス点数: 0x0800 (2bytes, Little Endian) - 8点
        string requestHex = "500000FFFF030000000C00000001040100640000900800";

        // 応答フレーム（3E Binary形式）
        // フレーム構築方法.md 43-59行目の仕様に基づく
        // [0-1] サブヘッダ: D0 00
        // [2] ネットワーク番号: 00
        // [3] PC番号: FF
        // [4-5] I/O番号: FF 03 (Little Endian)
        // [6] 局番: 00
        // [7-8] データ長: 03 00 (Little Endian: 終了コード2 + データ1 = 3)
        // [9-10] 終了コード: 00 00 (Little Endian)
        // [11] データ: B5 (M100-M107ビットパターン: 10110101 LSB first)
        string responseHex = "D00000FFFF030003000000B5";

        server.SetResponse(requestHex, responseHex);
    }


    /// <summary>
    /// パターン3: 4Eバイナリフレーム用MockUdpServer設定
    /// </summary>
    private void SetupMockServer_4E_Binary(MockUdpServer server)
    {
        // リクエストフレーム（4Eバイナリ）
        // サブヘッダ: 0x5400 (2bytes)
        // シリアル番号: 0x1234 (2bytes, Little Endian) → 34 12
        // 予約: 0x0000 (2bytes)
        // ネットワーク番号: 0x00 (1byte)
        // PC番号: 0xFF (1byte)
        // 要求先ユニットI/O番号: 0x03FF (2bytes, Little Endian) → FF 03
        // 要求先ユニット局番号: 0x00 (1byte)
        // 要求データ長: 0x000C (2bytes, Little Endian) → 0C 00
        // CPU監視タイマー: 0x0000 (2bytes)
        // コマンド: 0x0401 (2bytes, Little Endian) → 01 04
        // サブコマンド: 0x0001 (2bytes, Little Endian) → 01 00 (ビット単位)
        // 開始デバイス番号: 0x000064 (3bytes, Little Endian) → 64 00 00 (M100)
        // デバイスコード: 0x90 (1byte)
        // デバイス点数: 0x0008 (2bytes, Little Endian) → 08 00 (8点)
        string requestHex = "5400341200000000FFFF03000C00000001040100640000900800";

        // 応答フレーム（4E Binary形式）
        // フレーム構築方法.md 79-98行目、memo.md 48-58行目の仕様に基づく
        // [0-1] サブヘッダ: D4 00
        // [2-3] シーケンス番号: 34 12 (リクエストのエコーバック、Little Endian: 0x1234)
        // [4-5] 予約: 00 00
        // [6] ネットワーク番号: 00
        // [7] PC番号: FF
        // [8-9] I/O番号: FF 03 (Little Endian)
        // [10] 局番: 00
        // [11-12] データ長: 03 00 (Little Endian: 終了コード2 + データ1 = 3)
        // [13-14] 終了コード: 00 00 (Little Endian)
        // [15] データ: B5 (M100-M107ビットパターン: 10110101 LSB first)
        string responseHex = "D4003412000000FFFF030003000000B5";

        server.SetResponse(requestHex, responseHex);
    }


    /// <summary>
    /// TC143_10_1: パターン1（3E × バイナリ）M100～M107ビット読み出し
    /// </summary>
    [Fact]
    public async Task TC143_10_1_Pattern1_3EBinary_M100to107BitRead()
    {
        // Arrange
        var config = new ConnectionConfig
        {
            IpAddress = "127.0.0.1",
            Port = 5001,
            UseTcp = false, // UDP使用
            IsBinary = true,
            FrameVersion = FrameVersion.Frame3E
        };

        // 3Eバイナリリクエストフレーム（16進数文字列）
        string requestFrame = "500000FFFF030000000C00000001040100640000900800";

        var manager = new PlcCommunicationManager(config, _timeoutConfig);

        // サーバー起動待機
        await Task.Delay(100);

        // Act - Step3-6完全サイクル実行

        // Step3: 接続
        var connectionResponse = await manager.ConnectAsync();

        // Step4: 送信
        await manager.SendFrameAsync(requestFrame);

        // Step4: 受信
        var response = await manager.ReceiveResponseAsync(_timeoutConfig.ReceiveTimeoutMs);

        // Step5: 切断
        await manager.DisconnectAsync();

        // Step6-1: 基本後処理
        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "M",
            StartAddress = 100,
            Count = 8,
            FrameType = FrameType.Frame3E,
            RequestedAt = DateTime.UtcNow,
            ParseConfiguration = new ParseConfiguration
            {
                FrameFormat = "3E"
            }
        };
        var basicProcessed = await manager.ProcessReceivedRawData(response.ResponseData, requestInfo);

        // Step6-2: Phase3.5でDWord結合処理廃止（スキップ）
        // ReadRandomではDWord結合不要のため、basicProcessedを直接使用

        // Step6-3: 構造化変換
        // Phase3.5修正: processedData不要のため、BasicProcessedResponseDataから直接変換
        // ParseRawToStructuredDataがBasicProcessedResponseDataを受け入れない場合、
        // 簡易的にProcessedResponseDataを作成
        var processed = new ProcessedResponseData
        {
            IsSuccess = basicProcessed.IsSuccess,
            BasicProcessedDevices = basicProcessed.ProcessedDevices,
            CombinedDWordDevices = new List<CombinedDWordDevice>(),
            ProcessedAt = basicProcessed.ProcessedAt,
            ProcessingTimeMs = basicProcessed.ProcessingTimeMs,
            Errors = basicProcessed.Errors,
            Warnings = basicProcessed.Warnings
        };
        var structured = await manager.ParseRawToStructuredData(processed, requestInfo);

        // 統計情報取得
        var stats = manager.GetConnectionStats();

        // Assert - 完全サイクル検証
        AssertFullCycleSuccess(
            connectionResponse,
            basicProcessed,
            processed,
            structured,
            stats,
            expectedFrameVersion: "3E",
            expectedSubHeader: "D000",
            testPatternName: "パターン1（3E×バイナリ）"
        );
    }


    /// <summary>
    /// TC143_10_3: パターン3（4E × バイナリ）M100～M107ビット読み出し
    /// </summary>
    [Fact]
    public async Task TC143_10_3_Pattern3_4EBinary_M100to107BitRead()
    {
        // Arrange
        var config = new ConnectionConfig
        {
            IpAddress = "127.0.0.1",
            Port = 5003,
            UseTcp = false, // UDP使用
            IsBinary = true,
            FrameVersion = FrameVersion.Frame4E
        };

        string requestFrame = "5400341200000000FFFF03000C00000001040100640000900800";

        var manager = new PlcCommunicationManager(config, _timeoutConfig);
        await Task.Delay(100);

        // Act
        var connectionResponse = await manager.ConnectAsync();
        await manager.SendFrameAsync(requestFrame);
        var response = await manager.ReceiveResponseAsync(_timeoutConfig.ReceiveTimeoutMs);
        await manager.DisconnectAsync();

        var requestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceType = "M",
            StartAddress = 100,
            Count = 8,
            FrameType = FrameType.Frame4E,
            RequestedAt = DateTime.UtcNow,
            ParseConfiguration = new ParseConfiguration
            {
                FrameFormat = "4E"
            }
        };
        var basicProcessed = await manager.ProcessReceivedRawData(response.ResponseData, requestInfo);

        // Phase3.5修正: DWord結合処理廃止、簡易的にProcessedResponseDataを作成
        var processed = new ProcessedResponseData
        {
            IsSuccess = basicProcessed.IsSuccess,
            BasicProcessedDevices = basicProcessed.ProcessedDevices,
            CombinedDWordDevices = new List<CombinedDWordDevice>(),
            ProcessedAt = basicProcessed.ProcessedAt,
            ProcessingTimeMs = basicProcessed.ProcessingTimeMs,
            Errors = basicProcessed.Errors,
            Warnings = basicProcessed.Warnings
        };
        var structured = await manager.ParseRawToStructuredData(processed, requestInfo);
        var stats = manager.GetConnectionStats();

        // Assert
        AssertFullCycleSuccess(
            connectionResponse,
            basicProcessed,
            processed,
            structured,
            stats,
            expectedFrameVersion: "4E",
            expectedSubHeader: "D400",
            testPatternName: "パターン3（4E×バイナリ）"
        );
    }


    /// <summary>
    /// 完全サイクル成功検証（共通アサーションヘルパー）
    /// </summary>
    private void AssertFullCycleSuccess(
        ConnectionResponse connectionResponse,
        BasicProcessedResponseData basicProcessed,
        ProcessedResponseData processed,
        StructuredData structured,
        ConnectionStats stats,
        string expectedFrameVersion,
        string expectedSubHeader,
        string testPatternName)
    {
        // Step3検証: 接続成功
        Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status);
        Assert.NotNull(connectionResponse.Socket);
        // 注意: DisconnectAsync()後に呼ばれるため、Socket.Connectedの確認はスキップ

        // Step6-1検証: 基本後処理成功
        Assert.NotNull(basicProcessed);
        Assert.Equal(8, basicProcessed.ProcessedDeviceCount);
        Assert.True(basicProcessed.IsSuccess); // IsSuccessで成功を確認

        // Step6-2検証: DWord結合なし（ビットデバイスのため）
        Assert.NotNull(processed);
        Assert.True(processed.IsSuccess);
        Assert.Empty(processed.CombinedDWordDevices); // 結合デバイスが0件であることを確認

        // Step6-3検証: 構造化変換成功
        Assert.NotNull(structured);
        Assert.True(structured.IsSuccess);

        // SLMPフレーム情報検証
        Assert.Equal(expectedFrameVersion, structured.FrameInfo.FrameType);
        Assert.Equal(0x0000, structured.FrameInfo.EndCode);

        // 基本処理済みデバイスデータ検証（M100～M107のビット値）
        // 0xB5 = 10110101（ビットパターン）
        var expectedDevices = new[] { "M100", "M101", "M102", "M103", "M104", "M105", "M106", "M107" };
        var expectedBitValues = new[] { true, false, true, false, true, true, false, true };

        for (int i = 0; i < expectedDevices.Length; i++)
        {
            var deviceName = expectedDevices[i];
            var device = basicProcessed.ProcessedDevices.FirstOrDefault(d => d.DeviceName == deviceName);
            Assert.NotNull(device);
            Assert.Equal("M", device.DeviceType);
            Assert.Equal(100 + i, device.Address);

            // ビット値検証
            if (device.Value is bool boolValue)
            {
                Assert.Equal(expectedBitValues[i], boolValue);
            }
            else if (device.Value is int intValue)
            {
                Assert.Equal(expectedBitValues[i] ? 1 : 0, intValue);
            }
        }

        // 統計情報検証
        Assert.NotNull(stats);
        Assert.True(stats.TotalConnectionTime >= TimeSpan.Zero);
        Assert.Equal(0, stats.ErrorCount);
        Assert.Equal(100.0, stats.SuccessRate); // SuccessRateは0-100のスケール

        // パターン名ログ出力（テスト実行時の識別用）
        Console.WriteLine($"[TC143_10] {testPatternName} - 完全サイクル成功");
    }

    /// <summary>
    /// リソース解放
    /// </summary>
    public void Dispose()
    {
        _mockServer1?.Stop();
        _mockServer3?.Stop();
    }
}
