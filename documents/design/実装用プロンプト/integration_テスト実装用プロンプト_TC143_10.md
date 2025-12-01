# TC143_10_Step3-6_M100～M107ビット読み出し4パターン統合テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC143_10_Step3-6_M100～M107ビット読み出し4パターン統合テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManagerのStep3-6完全サイクルを、M100～M107ビット読み出しの4パターン（3E/4E × バイナリ/ASCII）で統合テストします。
このテストは、オフライン環境（MockPlcServer使用）で、実際のPLC機器なしで実行可能です。

### 実装対象
- **テストファイル**: `Tests/Integration/PlcCommunicationManager_IntegrationTests_TC143_10.cs`
- **テスト名前空間**: `andon.Tests.Integration`
- **テストメソッド名**:
  - `TC143_10_1_Pattern1_3EBinary_M100to107BitRead`
  - `TC143_10_2_Pattern2_3EAscii_M100to107BitRead`
  - `TC143_10_3_Pattern3_4EBinary_M100to107BitRead`
  - `TC143_10_4_Pattern4_4EAscii_M100to107BitRead`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`（実装済み）
   - `Core/Interfaces/IPlcCommunicationManager.cs`
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Models/ConnectionConfig.cs`
   - `Core/Models/TimeoutConfig.cs`
   - `Core/Models/StructuredData.cs`
   - `Core/Models/ProcessedDeviceRequestInfo.cs`

2. **前提テスト完了確認**
   - TC017-TC027（Step3-5単体テスト）完了
   - TC029-TC037（Step6単体テスト）完了
   - TC121（FullCycle完全実行テスト）完了

3. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/MockPlcServer.cs`（バイナリ/ASCII、3E/4E対応）
   - `Tests/TestUtilities/Stubs/SlmpFrameStubs.cs`
   - `Tests/TestUtilities/Stubs/PlcResponseStubs.cs`

4. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

5. **情報まとめファイル参照**
   - `C:\Users\1010821\Desktop\python\andon\documents\design\情報まとめ(プロンプト作成用)\integration_TC143_10.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル作成
```
ファイル: Tests/Integration/PlcCommunicationManager_IntegrationTests_TC143_10.cs
名前空間: andon.Tests.Integration
```

#### Step 1-2: 共通テストセットアップ

```csharp
public class PlcCommunicationManager_IntegrationTests_TC143_10 : IDisposable
{
    private readonly Mock<ILoggingManager> _mockLoggingManager;
    private readonly Mock<IErrorHandler> _mockErrorHandler;
    private readonly Mock<IResourceManager> _mockResourceManager;

    private readonly MockPlcServer _mockServer1; // 3E × Binary (Port 5001)
    private readonly MockPlcServer _mockServer2; // 3E × ASCII (Port 5002)
    private readonly MockPlcServer _mockServer3; // 4E × Binary (Port 5003)
    private readonly MockPlcServer _mockServer4; // 4E × ASCII (Port 5004)

    private readonly TimeoutConfig _timeoutConfig;
    private readonly ProcessedDeviceRequestInfo _deviceRequestInfo;

    public PlcCommunicationManager_IntegrationTests_TC143_10()
    {
        // モック初期化
        _mockLoggingManager = new Mock<ILoggingManager>();
        _mockErrorHandler = new Mock<IErrorHandler>();
        _mockResourceManager = new Mock<IResourceManager>();

        // タイムアウト設定
        _timeoutConfig = new TimeoutConfig
        {
            ConnectTimeoutMs = 5000,
            SendTimeoutMs = 3000,
            ReceiveTimeoutMs = 3000
        };

        // ProcessedDeviceRequestInfo準備
        _deviceRequestInfo = new ProcessedDeviceRequestInfo
        {
            DeviceConfiguration = new Dictionary<string, DeviceInfo>
            {
                { "M", new DeviceInfo
                  {
                      DeviceCode = "M",
                      Start = 100,
                      End = 107,
                      DataType = DataType.Bit,
                      DeviceCount = 8
                  }
                }
            },
            DWordSplitRanges = new List<DWordSplitRange>()
        };

        // MockPlcServer準備（4パターン）
        _mockServer1 = SetupMockServer_3E_Binary();
        _mockServer2 = SetupMockServer_3E_Ascii();
        _mockServer3 = SetupMockServer_4E_Binary();
        _mockServer4 = SetupMockServer_4E_Ascii();
    }

    private MockPlcServer SetupMockServer_3E_Binary()
    {
        var server = new MockPlcServer("127.0.0.1", 5001, isBinary: true, frameVersion: "3E");

        // リクエストフレーム（3Eバイナリ）
        byte[] requestFrame = new byte[]
        {
            0x50, 0x00,           // サブヘッダ
            0x00,                 // ネットワーク番号
            0xFF,                 // PC番号
            0xFF, 0x03,           // 要求先ユニットI/O番号
            0x00,                 // 要求先ユニット局番号
            0x01, 0x04,           // READコマンド
            0x01, 0x00,           // サブコマンド（ビット単位）
            0x64, 0x00, 0x00,     // 開始デバイス番号（M100）
            0x90,                 // デバイスコード（M機器）
            0x08, 0x00            // デバイス点数（8点）
        };

        // 応答フレーム（3Eバイナリ）
        byte[] responseFrame = new byte[]
        {
            0xD0, 0x00,           // 応答サブヘッダ
            0x02, 0x00, 0x00, 0x00, // データ長（2バイト）
            0x00, 0x00,           // 終了コード（正常）
            0xB5                  // データ（0xB5 = 10110101）
        };

        server.SetBinaryResponse(requestFrame, responseFrame);
        server.Start();

        return server;
    }

    private MockPlcServer SetupMockServer_3E_Ascii()
    {
        var server = new MockPlcServer("127.0.0.1", 5002, isBinary: false, frameVersion: "3E");

        // リクエストフレーム（3E ASCII）
        string requestFrame = "500000FF03FF000401000164000090080000";

        // 応答フレーム（3E ASCII）
        string responseFrame = "D0000200000000B5";

        server.SetAsciiResponse(requestFrame, responseFrame);
        server.Start();

        return server;
    }

    private MockPlcServer SetupMockServer_4E_Binary()
    {
        var server = new MockPlcServer("127.0.0.1", 5003, isBinary: true, frameVersion: "4E");

        // リクエストフレーム（4Eバイナリ）
        byte[] requestFrame = new byte[]
        {
            0x54, 0x00,                 // サブヘッダ
            0x34, 0x12, 0x00, 0x00,     // シリアル番号
            0x00,                       // ネットワーク番号
            0xFF,                       // PC番号
            0xFF, 0x03,                 // 要求先ユニットI/O番号
            0x00,                       // 要求先ユニット局番号
            0x01, 0x04,                 // READコマンド
            0x01, 0x00,                 // サブコマンド（ビット単位）
            0x64, 0x00, 0x00,           // 開始デバイス番号（M100）
            0x90,                       // デバイスコード（M機器）
            0x08, 0x00                  // デバイス点数（8点）
        };

        // 応答フレーム（4Eバイナリ）
        byte[] responseFrame = new byte[]
        {
            0xD4, 0x00,                 // 応答サブヘッダ
            0x34, 0x12, 0x00, 0x00,     // シリアル番号（リクエストと同一）
            0x02, 0x00, 0x00, 0x00,     // データ長（2バイト）
            0x00, 0x00,                 // 終了コード（正常）
            0xB5                        // データ（0xB5 = 10110101）
        };

        server.SetBinaryResponse(requestFrame, responseFrame);
        server.Start();

        return server;
    }

    private MockPlcServer SetupMockServer_4E_Ascii()
    {
        var server = new MockPlcServer("127.0.0.1", 5004, isBinary: false, frameVersion: "4E");

        // リクエストフレーム（4E ASCII）
        string requestFrame = "54001234000000FF03FF00010401006400009008000";

        // 応答フレーム（4E ASCII）
        string responseFrame = "D40012340000020000000B5";

        server.SetAsciiResponse(requestFrame, responseFrame);
        server.Start();

        return server;
    }

    public void Dispose()
    {
        _mockServer1?.Stop();
        _mockServer2?.Stop();
        _mockServer3?.Stop();
        _mockServer4?.Stop();
    }
}
```

#### Step 1-3: パターン1テストケース実装（3E × バイナリ）

```csharp
[Fact]
public async Task TC143_10_1_Pattern1_3EBinary_M100to107BitRead()
{
    // Arrange
    var config = new ConnectionConfig
    {
        IpAddress = "127.0.0.1",
        Port = 5001,
        UseTcp = true,
        ConnectionType = "TCP",
        IsBinary = true,
        FrameVersion = FrameVersion.Frame3E
    };

    byte[] requestFrame = new byte[]
    {
        0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,
        0x01, 0x04, 0x01, 0x00, 0x64, 0x00, 0x00,
        0x90, 0x08, 0x00
    };

    var manager = new PlcCommunicationManager(
        _mockLoggingManager.Object,
        _mockErrorHandler.Object,
        _mockResourceManager.Object
    );

    // Act
    // Step3: 接続
    var connectionResponse = await manager.ConnectAsync(config, _timeoutConfig);

    // Step4: 送信
    await manager.SendFrameAsync(requestFrame);

    // Step4: 受信
    var response = await manager.ReceiveResponseAsync(_timeoutConfig);

    // Step5: 切断
    await manager.DisconnectAsync();

    // Step6-1: 基本後処理
    var basicProcessed = await manager.ProcessReceivedRawData(response, _deviceRequestInfo);

    // Step6-2: DWord結合（ビットデバイスのためスキップ）
    var processed = await manager.CombineDwordData(basicProcessed, _deviceRequestInfo);

    // Step6-3: 構造化変換
    var structured = await manager.ParseRawToStructuredData(processed, _deviceRequestInfo);

    // 統計情報取得
    var stats = manager.GetConnectionStats();

    // Assert
    AssertFullCycleSuccess(
        connectionResponse,
        basicProcessed,
        processed,
        structured,
        stats,
        expectedFrameVersion: "3E",
        expectedSubHeader: "5000",
        testPatternName: "パターン1（3E×バイナリ）"
    );
}
```

#### Step 1-4: パターン2-4テストケース実装

```csharp
[Fact]
public async Task TC143_10_2_Pattern2_3EAscii_M100to107BitRead()
{
    // （パターン1と同様の構造、3E ASCII用設定使用）
}

[Fact]
public async Task TC143_10_3_Pattern3_4EBinary_M100to107BitRead()
{
    // （パターン1と同様の構造、4E Binary用設定使用）
}

[Fact]
public async Task TC143_10_4_Pattern4_4EAscii_M100to107BitRead()
{
    // （パターン1と同様の構造、4E ASCII用設定使用）
}
```

#### Step 1-5: 共通検証ヘルパーメソッド実装

```csharp
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
    // Step3検証
    Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status);
    Assert.NotNull(connectionResponse.Socket);
    Assert.True(connectionResponse.Socket.Connected);

    // Step6-1検証
    Assert.NotNull(basicProcessed);
    Assert.Equal(8, basicProcessed.ProcessedDeviceCount);
    Assert.False(basicProcessed.HasError);

    // Step6-2検証（ビットデバイスのためDWord結合なし）
    Assert.NotNull(processed);
    Assert.False(processed.IsDwordCombined);
    Assert.Equal(0, processed.DWordCombineCount);

    // Step6-3検証
    Assert.NotNull(structured);
    Assert.Equal(8, structured.StructuredDeviceCount);

    // SLMPフレーム情報検証
    Assert.Equal(expectedFrameVersion, structured.SlmpFrameInfo.FrameVersion);
    Assert.Equal(expectedSubHeader, structured.SlmpFrameInfo.SubHeader);
    Assert.Equal(0x0000, structured.SlmpFrameInfo.EndCode);
    Assert.True(structured.SlmpFrameInfo.IsSuccess);

    // デバイスデータ検証
    var expectedDevices = new[] { "M100", "M101", "M102", "M103", "M104", "M105", "M106", "M107" };
    foreach (var deviceName in expectedDevices)
    {
        Assert.Contains(deviceName, structured.StructuredDeviceData.Keys);

        var device = structured.StructuredDeviceData[deviceName];
        Assert.Equal(DeviceDataType.Bit, device.DataType);
        Assert.IsType<bool>(device.Value);
    }

    // ビット値検証（0xB5 = 10110101）
    Assert.True((bool)structured.StructuredDeviceData["M100"].Value);   // bit0 = 1
    Assert.False((bool)structured.StructuredDeviceData["M101"].Value);  // bit1 = 0
    Assert.True((bool)structured.StructuredDeviceData["M102"].Value);   // bit2 = 1
    Assert.False((bool)structured.StructuredDeviceData["M103"].Value);  // bit3 = 0
    Assert.True((bool)structured.StructuredDeviceData["M104"].Value);   // bit4 = 1
    Assert.True((bool)structured.StructuredDeviceData["M105"].Value);   // bit5 = 1
    Assert.False((bool)structured.StructuredDeviceData["M106"].Value);  // bit6 = 0
    Assert.True((bool)structured.StructuredDeviceData["M107"].Value);   // bit7 = 1

    // 統計情報検証
    Assert.NotNull(stats);
    Assert.True(stats.TotalResponseTime > TimeSpan.Zero);
    Assert.Equal(0, stats.TotalErrors);
    Assert.Equal(1.0, stats.SuccessRate);
}
```

#### Step 1-6: テスト実行（Red確認）

```bash
dotnet test --filter "FullyQualifiedName~TC143_10"
```

期待結果: テスト失敗（MockPlcServerまたはPlcCommunicationManagerのバイナリ/ASCII対応が未実装の可能性）

---

### Phase 2: Green（最小実装）

#### Step 2-1: PlcCommunicationManager バイナリ/ASCII対応実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**SendFrameAsync拡張（バイナリ対応）**:
```csharp
public async Task SendFrameAsync(byte[] frameBytes)
{
    if (_connectionResponse?.Status != ConnectionStatus.Connected || _socket == null)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnected);
    }

    try
    {
        int bytesSent = await _socket.SendAsync(frameBytes, SocketFlags.None);

        if (bytesSent != frameBytes.Length)
        {
            throw new InvalidOperationException($"送信バイト数不一致: 期待={frameBytes.Length}, 実際={bytesSent}");
        }

        _mockLoggingManager.LogInformation($"フレーム送信成功: {bytesSent}バイト");
    }
    catch (SocketException ex)
    {
        throw new SocketException($"フレーム送信失敗: {ex.Message}");
    }
}

// 既存のASCII版メソッドはそのまま維持
public async Task SendFrameAsync(string frameHexString)
{
    byte[] frameBytes = Encoding.ASCII.GetBytes(frameHexString);
    await SendFrameAsync(frameBytes);
}
```

**ReceiveResponseAsync拡張（バイナリ/ASCII自動判定）**:
```csharp
public async Task<byte[]> ReceiveResponseAsync(TimeoutConfig timeoutConfig)
{
    if (_socket == null)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnected);
    }

    byte[] buffer = new byte[8192];
    int bytesReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None);

    byte[] response = new byte[bytesReceived];
    Array.Copy(buffer, response, bytesReceived);

    _mockLoggingManager.LogInformation($"応答受信成功: {bytesReceived}バイト");

    return response;
}
```

**ProcessReceivedRawData拡張（バイナリ/ASCII自動判定）**:
```csharp
public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
    byte[] rawData,
    ProcessedDeviceRequestInfo deviceRequestInfo)
{
    var result = new BasicProcessedResponseData
    {
        BasicProcessedData = new Dictionary<string, object>(),
        ProcessedAt = DateTime.UtcNow
    };

    // フレームバージョン判定（3E/4E）
    bool is4EFrame = (rawData[0] == 0xD4) || (rawData[0] == 0x54);

    // バイナリ/ASCII判定
    bool isBinary = !IsAsciiData(rawData);

    int dataOffset;
    if (is4EFrame)
    {
        // 4Eフレーム: サブヘッダ(2) + シリアル(4) + データ長(4) + 終了コード(2) = 12バイト
        dataOffset = 12;
    }
    else
    {
        // 3Eフレーム: サブヘッダ(2) + データ長(4) + 終了コード(2) = 8バイト
        dataOffset = 8;
    }

    // データ部抽出
    byte[] deviceData = new byte[rawData.Length - dataOffset];
    Array.Copy(rawData, dataOffset, deviceData, 0, deviceData.Length);

    // ビットデバイス処理（M100～M107）
    if (deviceRequestInfo.DeviceConfiguration.ContainsKey("M"))
    {
        var deviceInfo = deviceRequestInfo.DeviceConfiguration["M"];
        if (deviceInfo.DataType == DataType.Bit)
        {
            // 1バイトから8ビット抽出
            byte bitData = deviceData[0];
            for (int i = 0; i < 8; i++)
            {
                string deviceName = $"M{100 + i}";
                bool bitValue = ((bitData >> i) & 0x01) == 1;
                result.BasicProcessedData[deviceName] = bitValue;
            }
            result.ProcessedDeviceCount = 8;
        }
    }

    return result;
}

private bool IsAsciiData(byte[] data)
{
    // ASCII判定: すべてのバイトが印刷可能ASCII範囲（0x20-0x7E）
    return data.All(b => b >= 0x20 && b <= 0x7E);
}
```

#### Step 2-2: MockPlcServer実装確認

**必要な機能**:
- バイナリ/ASCII形式対応
- 3E/4Eフレーム対応
- 複数ポート同時起動機能
- リクエスト/レスポンスマッピング

#### Step 2-3: テスト再実行（Green確認）

```bash
dotnet test --filter "FullyQualifiedName~TC143_10"
```

期待結果: 全4パターンのテストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上

**ログ出力追加**:
- パターン別ログ出力（フレームバージョン、バイナリ/ASCII形式）
- ビット値詳細ログ（M100～M107の各ビット値）
- 統計情報ログ（処理時間、デバイス数）

**エラーハンドリング強化**:
- フレーム形式不一致検出
- バイナリ/ASCII判定エラー処理
- デバイスデータ不足エラー処理

**ドキュメントコメント追加**:
- バイナリ/ASCII対応説明
- 3E/4Eフレーム判定ロジック説明
- ビットデバイス処理ロジック説明

#### Step 3-2: テスト再実行（Green維持確認）

```bash
dotnet test --filter "FullyQualifiedName~TC143_10"
```

期待結果: リファクタリング後も全テストがパス

---

## 技術仕様詳細

### SLMP フレーム構造

#### 3Eバイナリフレーム

**リクエスト**:
```
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | 50 00 | 3Eフレーム識別
2         | ネットワーク番号 | 00 | ローカルネットワーク
3         | PC番号 | FF | 要求先PC
4-5       | 要求先ユニットI/O番号 | FF 03 | CPU直結
6         | 要求先ユニット局番号 | 00 | 局番号0
7-8       | コマンド | 01 04 | デバイス一括読み出し
9-10      | サブコマンド | 01 00 | ビット単位読み出し
11-13     | 開始デバイス番号 | 64 00 00 | M100（リトルエンディアン）
14        | デバイスコード | 90 | M機器
15-16     | デバイス点数 | 08 00 | 8点（リトルエンディアン）
```

**応答**:
```
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | D0 00 | 3E応答
2-5       | データ長 | 02 00 00 00 | 2バイト（終了コード）
6-7       | 終了コード | 00 00 | 正常終了
8         | データ | B5 | M100～M107のビット値（1バイト）
```

#### 3E ASCIIフレーム

**リクエスト**: `"500000FF03FF000401000164000090080000"`（34文字）

**応答**: `"D0000200000000B5"`（18文字）

#### 4Eバイナリフレーム

**リクエスト**:
```
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | 54 00 | 4Eフレーム識別
2-5       | シリアル番号 | 34 12 00 00 | 0x00001234
6-17      | （3Eと同様）
```

**応答**:
```
バイト位置 | 内容 | 値（16進） | 説明
----------|------|-----------|-----
0-1       | サブヘッダ | D4 00 | 4E応答
2-5       | シリアル番号 | 34 12 00 00 | リクエストと同一
6-12      | （3Eと同様）
```

#### 4E ASCIIフレーム

**リクエスト**: `"54001234000000FF03FF00010401006400009008000"`（42文字）

**応答**: `"D40012340000020000000B5"`（26文字）

### ビットデバイスデータ構造

**テストデータ（0xB5 = 10110101）**:
```
バイト値: 0xB5
2進数: 10110101
ビット配置（LSBファースト）:
  bit0 (M100): 1 → true
  bit1 (M101): 0 → false
  bit2 (M102): 1 → true
  bit3 (M103): 0 → false
  bit4 (M104): 1 → true
  bit5 (M105): 1 → true
  bit6 (M106): 0 → false
  bit7 (M107): 1 → true
```

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-11_TC143_10実装.md`
- 実装開始時刻
- 目標（TC143_10テスト実装完了、4パターン全て）
- 実装方針

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/TC143_10_統合テスト実装記録.md`
- 実装判断根拠
  - なぜバイナリ/ASCII対応を実装したか
  - 3E/4Eフレーム判定ロジックの選択理由
  - MockPlcServerの拡張方法の選択理由
- 発生した問題と解決過程
  - バイナリ/ASCII判定の問題
  - フレーム構造解析の問題
  - ビット抽出ロジックの問題

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC143_10_テスト結果.log`
- 4パターン別テスト結果（成功/失敗、実行時間）
- Red-Green-Refactorの各フェーズ結果
- パターン別統計情報（応答時間、デバイス数）
- エラーログとデバッグ情報

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC143_10_1（パターン1: 3E×バイナリ）テストがパス
- [ ] TC143_10_2（パターン2: 3E×ASCII）テストがパス
- [ ] TC143_10_3（パターン3: 4E×バイナリ）テストがパス
- [ ] TC143_10_4（パターン4: 4E×ASCII）テストがパス
- [ ] PlcCommunicationManager バイナリ/ASCII対応実装完了
- [ ] MockPlcServer 4パターン対応完了
- [ ] リファクタリング完了（ログ出力、エラーハンドリング等）
- [ ] テスト再実行でGreen維持確認（4パターン全て）
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] テスト結果ログ保存完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test情報まとめ.mdの該当項目にチェック

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認（4パターン全て）

### バイナリ/ASCII対応
- SendFrameAsyncのオーバーロード実装（byte[]版、string版）
- ReceiveResponseAsyncのバイナリ応答対応
- ProcessReceivedRawDataのバイナリ/ASCII自動判定
- フレーム形式不一致エラーの適切な処理

### 3E/4Eフレーム対応
- サブヘッダによるフレームバージョン判定（0x5000/0x5400、0xD000/0xD400）
- フレーム構造の違いを考慮したデータオフセット計算
- シリアル番号の有無による処理分岐

### ビットデバイス処理
- LSBファースト（bit0が最下位ビット）
- 1バイトから8ビット抽出
- ビット境界の適切な処理

### 記録の重要性
- 実装判断の根拠を詳細に記録
- テスト結果は数値データも含めて保存（4パターン全て）
- パターン別の処理時間、統計情報を記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md`
- `documents/design/テスト内容.md`
- `documents/design/エラーハンドリング.md`
- `documents/design/情報まとめ(プロンプト作成用)/integration_TC143_10.md`

### 開発手法
- `documents/development_methodology/development-methodology.md`

### 前提テスト参照
- `integration_TC121.md` - FullCycle完全実行テスト
- `step3_TC017.md` - TCP接続テスト
- `step6_TC037.md` - 3Eフレーム解析テスト
- `step6_TC038.md` - 4Eフレーム解析テスト

### PySLMPClient実装参照
- `PySLMPClient/pyslmpclient/const.py`（デバイスコード定義）
- `PySLMPClient/pyslmpclient/util.py`（フレーム作成ロジック）
- `PySLMPClient/tests/test_main.py`（テストケース実例）

---

以上の指示に従って、TC143_10_Step3-6_M100～M107ビット読み出し4パターン統合テストの実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。
