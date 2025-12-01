# Integration FullCycle テスト実装用情報（TC121）

## ドキュメント概要

### 目的
このドキュメントは、TC121_FullCycle_接続から構造化まで完全実行テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `各ステップio.md` - 各ステップのInput/Output詳細

#### 既存テストケース実装情報
- `step3_TC017.md` - TCP接続テスト（Step3）
- `step3_TC021.md` - 送信テスト（Step4）
- `step3_TC025.md` - 受信テスト（Step4）
- `step3_TC027.md` - 切断テスト（Step5）
- `step6_TC029.md` - 基本後処理テスト（Step6-1）
- `step6_TC032.md` - DWord結合テスト（Step6-2）
- `step6_TC037.md` - 3Eフレーム解析テスト（Step6-3）
- `step6_TC038.md` - 4Eフレーム解析テスト（Step6-3）

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象機能仕様

### FullCycle（Step3-6完全サイクル実行）
**統合テスト対象**: PlcCommunicationManager全体
**名前空間**: andon.Core.Managers

#### 完全サイクル構成
```
Step3: PLC接続処理（ConnectAsync）
  ↓
Step4: PLCリクエスト送信（SendFrameAsync）
  ↓
Step4: PLCデータ受信（ReceiveResponseAsync）
  ↓
Step5: PLC切断処理（DisconnectAsync）
  ↓
Step6-1: 受信データ基本後処理（ProcessReceivedRawData）
  ↓
Step6-2: DWord結合処理（CombineDwordData）
  ↓
Step6-3: 構造化データ変換（ParseRawToStructuredData）
```

#### 統合Input
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=true, IsBinary=false, FrameVersion="4E"）
- TimeoutConfig（ConnectTimeoutMs=5000, SendTimeoutMs=3000, ReceiveTimeoutMs=3000）
- SLMPフレーム（2つ）:
  - M機器用: "54001234000000010401006400000090E8030000"
  - D機器用: "54001234000000010400A800000090E8030000"
- ProcessedDeviceRequestInfo（DWord分割情報、フレーム解析情報）

#### 統合Output
- StructuredData（最終構造化データ）:
  - StructuredDeviceData（Dictionary<string, StructuredDevice>）
  - SlmpFrameInfo（SLMPフレーム解析情報）
  - 統計情報（接続時間、送受信時間、処理時間、デバイス数等）
- ConnectionStats（接続統計情報）:
  - TotalResponseTime（合計応答時間）
  - TotalErrors（合計エラー数）
  - TotalRetries（合計リトライ数）
  - SuccessRate（成功率）

#### 機能
- Step3-6の連続実行
- 各ステップ間のデータ受け渡し
- 統計情報の累積
- エラーハンドリング・リトライ制御
- リソース管理（ソケット、メモリ）

#### データ取得元
- ConfigToFrameManager.LoadConfigAsync()（接続設定、デバイス設定）
- ConfigToFrameManager.BuildFrames()（SLMPフレーム）
- ConfigToFrameManager.SplitDwordToWord()（DWord分割情報）

---

## 2. テストケース仕様（TC121）

### TC121_FullCycle_接続から構造化まで完全実行
**目的**: Step3-6の完全サイクル実行機能を統合テスト

#### 前提条件
- ConfigToFrameManagerによる設定読み込み完了
- BuildFramesによるSLMPフレーム生成完了
- SplitDwordToWordによるDWord分割情報生成完了
- PLCシミュレータまたはモックサーバーが稼働中
- ネットワーク到達可能

#### 入力データ
**ConnectionConfig（TCP接続設定）**:
```csharp
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = true,
    ConnectionType = "TCP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame4E
};
```

**TimeoutConfig（タイムアウト設定）**:
```csharp
var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000
};
```

**SLMPフレーム（2つ）**:
```csharp
var slmpFrames = new List<string>
{
    "54001234000000010401006400000090E8030000",  // M機器用
    "54001234000000010400A800000090E8030000"   // D機器用
};
```

**ProcessedDeviceRequestInfo（DWord分割情報）**:
```csharp
var deviceRequestInfo = new ProcessedDeviceRequestInfo
{
    SlmpFrameFormat = "4E",
    ResponseFrameHeader = "54001234000000",
    DeviceConfiguration = new Dictionary<string, DeviceInfo>
    {
        { "M", new DeviceInfo { DeviceCode = "M", Start = 0, End = 999, DataType = DataType.Bit } },
        { "D", new DeviceInfo { DeviceCode = "D", Start = 0, End = 999, DataType = DataType.Word } }
    },
    DWordSplitRanges = new List<DWordSplitRange>
    {
        new DWordSplitRange { DeviceCode = "D", StartAddress = 0, EndAddress = 499, SplitCount = 500 }
    }
};
```

#### 期待出力
**StructuredData（最終構造化データ）**:
```csharp
// 構造化データ検証
Assert.NotNull(structuredData);
Assert.NotNull(structuredData.StructuredDeviceData);
Assert.True(structuredData.StructuredDeviceCount > 0);
Assert.NotNull(structuredData.SlmpFrameInfo);
Assert.Equal("4E", structuredData.SlmpFrameInfo.FrameVersion);
Assert.Equal(0x0000, structuredData.SlmpFrameInfo.EndCode);
Assert.True(structuredData.SlmpFrameInfo.IsSuccess);

// デバイスデータ検証（サンプル）
Assert.Contains("M000", structuredData.StructuredDeviceData.Keys);
Assert.Contains("D100", structuredData.StructuredDeviceData.Keys);
Assert.Contains("D000_DWord", structuredData.StructuredDeviceData.Keys);

// ビット型デバイス
Assert.Equal(DeviceDataType.Bit, structuredData.StructuredDeviceData["M000"].DataType);
Assert.IsType<bool>(structuredData.StructuredDeviceData["M000"].Value);

// ワード型デバイス
Assert.Equal(DeviceDataType.Word, structuredData.StructuredDeviceData["D100"].DataType);
Assert.IsType<ushort>(structuredData.StructuredDeviceData["D100"].Value);

// DWord型デバイス
Assert.Equal(DeviceDataType.DWord, structuredData.StructuredDeviceData["D000_DWord"].DataType);
Assert.IsType<uint>(structuredData.StructuredDeviceData["D000_DWord"].Value);
```

**ConnectionStats（接続統計情報）**:
```csharp
// 統計情報検証
Assert.NotNull(connectionStats);
Assert.True(connectionStats.TotalResponseTime > TimeSpan.Zero);
Assert.Equal(0, connectionStats.TotalErrors);
Assert.Equal(0, connectionStats.TotalRetries);
Assert.Equal(1.0, connectionStats.SuccessRate);
```

#### 動作フロー成功条件
1. **Step3（接続）成功**:
   - ConnectionResponse.Status == ConnectionStatus.Connected
   - Socket != null, Socket.Connected == true
   - RemoteEndPoint == "192.168.1.10:5000"
   - ConnectedAt != null
   - ConnectionTime > TimeSpan.Zero

2. **Step4（送信）成功**:
   - SendFrameAsync完了（例外なし）
   - 2つのフレーム（M機器用、D機器用）送信成功
   - 送信バイト数が期待値と一致

3. **Step4（受信）成功**:
   - ReceiveResponseAsync完了（例外なし）
   - 2つの応答データ受信成功
   - 受信データが正しいSLMP応答形式

4. **Step5（切断）成功**:
   - DisconnectAsync完了（例外なし）
   - Socket.Connected == false
   - 統計情報記録済み

5. **Step6-1（基本後処理）成功**:
   - BasicProcessedResponseData生成成功
   - BasicProcessedData（Dictionary）に全デバイスデータ格納
   - ProcessedDeviceCount > 0

6. **Step6-2（DWord結合）成功**:
   - ProcessedResponseData生成成功
   - CombinedDWordData（Dictionary）にDWord結合済みデータ格納
   - IsDwordCombined == true
   - DWordCombineCount > 0

7. **Step6-3（構造化変換）成功**:
   - StructuredData生成成功
   - StructuredDeviceData（Dictionary）に全構造化デバイスデータ格納
   - SlmpFrameInfo解析成功（EndCode == 0x0000）
   - StructuredDeviceCount > 0

8. **統計情報累積成功**:
   - 接続時間、送受信時間、処理時間が正確に記録
   - TotalResponseTime == Step3時間 + Step4送受信時間 + Step6処理時間
   - SuccessRate == 1.0（全ステップ成功）

---

## 3. Step別詳細仕様

### Step3: PLC接続処理（ConnectAsync）

#### 詳細情報
**参照**: step3_TC017.md（TCP接続成功テスト）

#### Input
- ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=true）
- TimeoutConfig（ConnectTimeoutMs=5000, SendTimeoutMs=3000, ReceiveTimeoutMs=3000）

#### Output
- ConnectionResponse:
  - Status = ConnectionStatus.Connected
  - Socket != null（TCP用ソケット）
  - RemoteEndPoint = "192.168.1.10:5000"
  - ConnectedAt != null
  - ConnectionTime > TimeSpan.Zero（接続処理時間）

#### 検証ポイント
- TCP接続成功
- ソケットタイムアウト設定済み（SendTimeout=3000ms, ReceiveTimeout=3000ms）
- 接続時間が ConnectTimeoutMs 以内
- エンドポイント情報正確に記録

---

### Step4: PLCリクエスト送信（SendFrameAsync）

#### 詳細情報
**参照**: step3_TC021.md（正常送信テスト）、step3_TC022.md（全機器データ取得テスト）

#### Input
- SLMPフレーム（2つ）:
  - M機器用: "54001234000000010401006400000090E8030000"
  - D機器用: "54001234000000010400A800000090E8030000"

#### Output
- Task（送信完了）
- 送信バイト数確認

#### 検証ポイント
- 2つのフレーム連続送信成功
- 送信完了（例外なし）
- 送信バイト数が期待値と一致

---

### Step4: PLCデータ受信（ReceiveResponseAsync）

#### 詳細情報
**参照**: step3_TC025.md（正常受信テスト）

#### Input
- TimeoutConfig（ReceiveTimeoutMs=3000）

#### Output
- PLCからの生データ（16進数文字列）
  - M機器応答: 125バイト（1000ビット ÷ 8）、ASCII形式250文字
  - D機器応答: 2000バイト（1000ワード × 2）、ASCII形式4000文字

#### 検証ポイント
- 2つの応答データ受信成功
- 受信データが正しいSLMP応答形式
- 受信時間が ReceiveTimeoutMs 以内

---

### Step5: PLC切断処理（DisconnectAsync）

#### 詳細情報
**参照**: step3_TC027.md（正常切断テスト）

#### Input
- 接続統計情報（ConnectionTime等）

#### Output
- 切断完了ステータス
- ConnectionStats（接続統計情報）
- リソース解放完了

#### 検証ポイント
- ソケット切断成功（Socket.Connected == false）
- 統計情報正確に記録
- リソース（ソケット、メモリ）正常解放

---

### Step6-1: 受信データ基本後処理（ProcessReceivedRawData）

#### 詳細情報
**参照**: step6_TC029.md（基本後処理成功テスト）

#### Input
- Step4で受信した生データ（2つ）
- ProcessedDeviceRequestInfo（送信時の前処理情報）

#### Output
- BasicProcessedResponseData:
  - BasicProcessedData（Dictionary<string, object>）:
    - "M000" → true, "M001" → false, ... （ビット型）
    - "D100" → 0x1234, "D101" → 0x5678, ... （ワード型）
  - RawDataHex（元生データ）
  - ProcessedAt（処理時刻）
  - ProcessedDeviceCount（処理デバイス数）

#### 検証ポイント
- 生データの16進数パース成功
- デバイス別データ抽出成功
- 基本的な型変換（ビット/ワード）成功

---

### Step6-2: DWord結合処理（CombineDwordData）

#### 詳細情報
**参照**: step6_TC032.md（DWord結合処理成功テスト）

#### Input
- BasicProcessedResponseData（Step6-1の出力）
- ProcessedDeviceRequestInfo（DWord分割情報）

#### Output
- ProcessedResponseData:
  - BasicProcessedData（継承）
  - CombinedDWordData（Dictionary<string, uint>）:
    - "D000_DWord" → 0x56781234 （32ビット）
    - "D001_DWord" → 0xDEF09ABC （32ビット）
  - IsDwordCombined = true
  - DWordCombineCount（DWord結合数）

#### 検証ポイント
- DWord分割要否判定成功
- Low/Highワードの結合処理成功（16bit×2→32bit）
- DWord結合統計の正確な計算

---

### Step6-3: 構造化データ変換（ParseRawToStructuredData）

#### 詳細情報
**参照**: step6_TC037.md（3Eフレーム解析）、step6_TC038.md（4Eフレーム解析）

#### Input
- ProcessedResponseData（Step6-2の出力）
- ProcessedDeviceRequestInfo（フレーム解析情報）

#### Output
- StructuredData:
  - StructuredDeviceData（Dictionary<string, StructuredDevice>）:
    - "M000" → { DeviceName="M000", DataType=Bit, Value=true, Metadata={...} }
    - "D100" → { DeviceName="D100", DataType=Word, Value=0x1234, Metadata={...} }
    - "D000_DWord" → { DeviceName="D000", DataType=DWord, Value=0x56781234, Metadata={...} }
  - SlmpFrameInfo:
    - FrameVersion = "4E"
    - SubHeader = "54001234000000"
    - EndCode = 0x0000
    - IsSuccess = true
  - ParsedAt（構造化処理時刻）
  - StructuredDeviceCount（構造化デバイス数）

#### 検証ポイント
- SLMP応答フレーム構造解析成功（4Eフレーム）
- デバイス別構造化データ生成成功
- 終了コード解析・エラー判定成功
- フレーム整合性検証成功

---

## 4. 統合テスト実装構造

### Arrange（準備）

#### 1. 設定準備
```csharp
// ConnectionConfig準備
var connectionConfig = new ConnectionConfig
{
    IpAddress = "192.168.1.10",
    Port = 5000,
    UseTcp = true,
    ConnectionType = "TCP",
    IsBinary = false,
    FrameVersion = FrameVersion.Frame4E
};

// TimeoutConfig準備
var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    SendTimeoutMs = 3000,
    ReceiveTimeoutMs = 3000
};

// ProcessedDeviceRequestInfo準備
var deviceRequestInfo = CreateDeviceRequestInfo();
```

#### 2. SLMPフレーム準備
```csharp
var slmpFrames = new List<string>
{
    "54001234000000010401006400000090E8030000",  // M機器用
    "54001234000000010400A800000090E8030000"   // D機器用
};
```

#### 3. PLCシミュレータ/モック準備
```csharp
// MockPlcServerまたはPLCシミュレータ起動
var mockPlcServer = new MockPlcServer("192.168.1.10", 5000);
mockPlcServer.Start();

// M機器応答データ設定（125バイト、ASCII形式250文字）
mockPlcServer.SetResponse(slmpFrames[0], CreateMDeviceResponse());

// D機器応答データ設定（2000バイト、ASCII形式4000文字）
mockPlcServer.SetResponse(slmpFrames[1], CreateDDeviceResponse());
```

#### 4. PlcCommunicationManager初期化
```csharp
var plcCommManager = new PlcCommunicationManager(
    loggingManager,
    errorHandler,
    resourceManager
);
```

---

### Act（実行）

#### 完全サイクル実行
```csharp
// Step3: 接続
var connectionResponse = await plcCommManager.ConnectAsync(
    connectionConfig,
    timeoutConfig
);

// Step4: 送信（M機器用フレーム）
await plcCommManager.SendFrameAsync(slmpFrames[0]);

// Step4: 受信（M機器応答）
var mDeviceResponse = await plcCommManager.ReceiveResponseAsync(timeoutConfig);

// Step4: 送信（D機器用フレーム）
await plcCommManager.SendFrameAsync(slmpFrames[1]);

// Step4: 受信（D機器応答）
var dDeviceResponse = await plcCommManager.ReceiveResponseAsync(timeoutConfig);

// Step5: 切断
var disconnectResponse = await plcCommManager.DisconnectAsync();

// Step6-1: 基本後処理（M機器）
var mBasicProcessed = await plcCommManager.ProcessReceivedRawData(
    mDeviceResponse,
    deviceRequestInfo
);

// Step6-1: 基本後処理（D機器）
var dBasicProcessed = await plcCommManager.ProcessReceivedRawData(
    dDeviceResponse,
    deviceRequestInfo
);

// Step6-1: 2つのBasicProcessedResponseDataを統合
var mergedBasicProcessed = MergeBasicProcessedData(mBasicProcessed, dBasicProcessed);

// Step6-2: DWord結合
var processedData = await plcCommManager.CombineDwordData(
    mergedBasicProcessed,
    deviceRequestInfo
);

// Step6-3: 構造化変換
var structuredData = await plcCommManager.ParseRawToStructuredData(
    processedData,
    deviceRequestInfo
);

// 統計情報取得
var connectionStats = plcCommManager.GetConnectionStats();
```

---

### Assert（検証）

#### 1. Step3（接続）検証
```csharp
// 接続成功検証
Assert.Equal(ConnectionStatus.Connected, connectionResponse.Status);
Assert.NotNull(connectionResponse.Socket);
Assert.True(connectionResponse.Socket.Connected);
Assert.Equal(ProtocolType.Tcp, connectionResponse.Socket.ProtocolType);

// エンドポイント検証
Assert.NotNull(connectionResponse.RemoteEndPoint);
Assert.Equal("192.168.1.10:5000", connectionResponse.RemoteEndPoint.ToString());

// 時刻・時間検証
Assert.NotNull(connectionResponse.ConnectedAt);
Assert.True(connectionResponse.ConnectedAt.Value <= DateTime.UtcNow);
Assert.NotNull(connectionResponse.ConnectionTime);
Assert.True(connectionResponse.ConnectionTime.Value > TimeSpan.Zero);
Assert.True(connectionResponse.ConnectionTime.Value.TotalMilliseconds < timeoutConfig.ConnectTimeoutMs);

// エラー情報検証
Assert.Null(connectionResponse.ErrorMessage);
```

#### 2. Step4（送受信）検証
```csharp
// 送信成功検証（例外なし）
// ReceiveResponseAsyncが正常完了していることで送信成功を間接的に検証

// 受信成功検証
Assert.NotNull(mDeviceResponse);
Assert.NotNull(dDeviceResponse);
Assert.NotEmpty(mDeviceResponse);
Assert.NotEmpty(dDeviceResponse);

// 受信データ長検証
Assert.Equal(250, mDeviceResponse.Length);  // 125バイト × 2（ASCII）
Assert.Equal(4000, dDeviceResponse.Length); // 2000バイト × 2（ASCII）
```

#### 3. Step5（切断）検証
```csharp
// 切断成功検証
Assert.False(connectionResponse.Socket.Connected);

// 統計情報検証
Assert.NotNull(disconnectResponse);
// （DisconnectAsyncの戻り値型に応じて検証項目追加）
```

#### 4. Step6-1（基本後処理）検証
```csharp
// 基本後処理成功検証
Assert.NotNull(mergedBasicProcessed);
Assert.NotNull(mergedBasicProcessed.BasicProcessedData);
Assert.True(mergedBasicProcessed.ProcessedDeviceCount > 0);

// デバイスデータ存在検証
Assert.Contains("M000", mergedBasicProcessed.BasicProcessedData.Keys);
Assert.Contains("D100", mergedBasicProcessed.BasicProcessedData.Keys);

// エラー情報検証
Assert.False(mergedBasicProcessed.HasError);
Assert.Empty(mergedBasicProcessed.Errors);
```

#### 5. Step6-2（DWord結合）検証
```csharp
// DWord結合成功検証
Assert.NotNull(processedData);
Assert.True(processedData.IsDwordCombined);
Assert.True(processedData.DWordCombineCount > 0);

// CombinedDWordData検証
Assert.NotNull(processedData.CombinedDWordData);
Assert.Contains("D000_DWord", processedData.CombinedDWordData.Keys);

// エラー情報検証
Assert.False(processedData.HasError);
Assert.Empty(processedData.Errors);
```

#### 6. Step6-3（構造化変換）検証
```csharp
// 構造化成功検証
Assert.NotNull(structuredData);
Assert.NotNull(structuredData.StructuredDeviceData);
Assert.True(structuredData.StructuredDeviceCount > 0);

// SLMPフレーム解析情報検証
Assert.NotNull(structuredData.SlmpFrameInfo);
Assert.Equal("4E", structuredData.SlmpFrameInfo.FrameVersion);
Assert.Equal("54001234000000", structuredData.SlmpFrameInfo.SubHeader);
Assert.Equal(0x0000, structuredData.SlmpFrameInfo.EndCode);
Assert.True(structuredData.SlmpFrameInfo.IsSuccess);
Assert.Null(structuredData.SlmpFrameInfo.ErrorDescription);

// 構造化デバイスデータ検証
Assert.Contains("M000", structuredData.StructuredDeviceData.Keys);
Assert.Contains("D100", structuredData.StructuredDeviceData.Keys);
Assert.Contains("D000_DWord", structuredData.StructuredDeviceData.Keys);

// ビット型デバイス検証
var m000Device = structuredData.StructuredDeviceData["M000"];
Assert.Equal("M000", m000Device.DeviceName);
Assert.Equal(DeviceDataType.Bit, m000Device.DataType);
Assert.IsType<bool>(m000Device.Value);
Assert.NotNull(m000Device.Metadata);

// ワード型デバイス検証
var d100Device = structuredData.StructuredDeviceData["D100"];
Assert.Equal("D100", d100Device.DeviceName);
Assert.Equal(DeviceDataType.Word, d100Device.DataType);
Assert.IsType<ushort>(d100Device.Value);
Assert.NotNull(d100Device.Metadata);

// DWord型デバイス検証
var d000DWordDevice = structuredData.StructuredDeviceData["D000_DWord"];
Assert.Equal("D000", d000DWordDevice.DeviceName);  // "_DWord"接尾辞なし
Assert.Equal(DeviceDataType.DWord, d000DWordDevice.DataType);
Assert.IsType<uint>(d000DWordDevice.Value);
Assert.NotNull(d000DWordDevice.Metadata);
Assert.True(d000DWordDevice.Metadata.ContainsKey("IsCombined"));
Assert.True((bool)d000DWordDevice.Metadata["IsCombined"]);

// エラー情報検証
Assert.Empty(structuredData.ParseErrors);
```

#### 7. 統計情報検証
```csharp
// 統計情報累積検証
Assert.NotNull(connectionStats);
Assert.True(connectionStats.TotalResponseTime > TimeSpan.Zero);
Assert.Equal(0, connectionStats.TotalErrors);
Assert.Equal(0, connectionStats.TotalRetries);
Assert.Equal(1.0, connectionStats.SuccessRate);

// 応答時間内訳検証
var expectedTotalTime = connectionResponse.ConnectionTime.Value +
                        /* 送受信時間 */ +
                        /* 処理時間 */;
Assert.True(connectionStats.TotalResponseTime >= expectedTotalTime);
```

#### 8. データ整合性検証
```csharp
// BasicProcessedData → ProcessedResponseData → StructuredData の整合性検証
foreach (var kvp in mergedBasicProcessed.BasicProcessedData)
{
    if (!kvp.Key.Contains("_DWord"))
    {
        // ワード・ビット型デバイスは直接継承
        Assert.Contains(kvp.Key, structuredData.StructuredDeviceData.Keys);
    }
}

foreach (var kvp in processedData.CombinedDWordData)
{
    // DWord型デバイスは構造化データに含まれる
    Assert.Contains(kvp.Key, structuredData.StructuredDeviceData.Keys);
}
```

---

## 5. エラーハンドリング

### 完全サイクルエラー処理（TC121では発生しない）

#### Step3エラー時
- ConnectionResponse.Status != Connected
- 以降のStep4-6はスキップ
- エラーログ出力
- ConnectionStats.TotalErrors++

#### Step4エラー時
- TimeoutException または SocketException
- 送受信失敗
- Step5（切断）実行後、Step6スキップ
- ConnectionStats.TotalErrors++

#### Step6エラー時
- DataProcessingException
- 構造化データ不完全
- エラー情報を含むStructuredDataを返却
- ConnectionStats.TotalErrors++

### リトライ制御（TC121では実施しない）
- TC123（エラー発生時の適切なスキップ）で詳細検証
- ErrorHandler.ApplyRetryPolicy()による判定
- リトライ可能エラー: 通信タイムアウト、一時的なネットワークエラー
- リトライ不可エラー: 設定エラー、認証エラー、致命的なプロトコルエラー

---

## 6. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManager_IntegrationTests.cs
- **配置先**: Tests/Integration/
- **名前空間**: andon.Tests.Integration

### テスト実装順序
1. **TC017-TC027**: Step3-5の単体テスト（完了済み）
2. **TC029-TC037**: Step6の単体テスト（完了済み）
3. **TC115-TC116**: Step3-5の連続実行テスト
4. **TC118-TC119**: Step6の連続処理テスト
5. **TC121**: FullCycle完全実行テスト（本テスト、最優先）
6. TC122-TC123: FullCycleの複数サイクル・エラーハンドリングテスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcServer**: TCP/UDP両対応PLCシミュレータ
  - SLMPフレーム受信→応答返却機能
  - 4Eフレーム対応
  - 遅延・タイムアウトシミュレーション機能
- **MockLoggingManager**: ログ出力モック
- **MockErrorHandler**: エラーハンドリングモック
- **MockResourceManager**: リソース管理モック

#### 使用するスタブ
- **ConnectionConfigStubs**: 接続設定スタブ（TCP/UDP）
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **SlmpFrameStubs**: SLMPフレームスタブ（M機器用、D機器用）
- **PlcResponseStubs**: PLC応答データスタブ（M機器、D機器）
- **StructuredDataValidator**: 構造化データ検証ヘルパー

---

## 7. 依存クラス・設定

### ConnectionResponse（接続結果）
```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }
    public Socket? Socket { get; set; }
    public EndPoint? RemoteEndPoint { get; set; }
    public DateTime? ConnectedAt { get; set; }
    public TimeSpan? ConnectionTime { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### BasicProcessedResponseData（基本後処理結果）
```csharp
public class BasicProcessedResponseData
{
    public Dictionary<string, object> BasicProcessedData { get; set; }
    public string RawDataHex { get; set; }
    public DateTime ProcessedAt { get; set; }
    public int ProcessedDeviceCount { get; set; }
    public bool HasError { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

### ProcessedResponseData（DWord結合後処理結果）
```csharp
public class ProcessedResponseData : BasicProcessedResponseData
{
    public Dictionary<string, uint> CombinedDWordData { get; set; }
    public bool IsDwordCombined { get; set; }
    public int DWordCombineCount { get; set; }
}
```

### StructuredData（最終構造化データ）
```csharp
public class StructuredData : ProcessedResponseData
{
    public Dictionary<string, StructuredDevice> StructuredDeviceData { get; set; }
    public DateTime ParsedAt { get; set; }
    public int StructuredDeviceCount { get; set; }
    public SlmpFrameAnalysis SlmpFrameInfo { get; set; }
    public List<string> ParseErrors { get; set; }
}
```

### ConnectionStats（接続統計情報）
```csharp
public class ConnectionStats
{
    public TimeSpan TotalResponseTime { get; set; }
    public int TotalErrors { get; set; }
    public int TotalRetries { get; set; }
    public double SuccessRate { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public DateTime FirstRequestAt { get; set; }
    public DateTime LastRequestAt { get; set; }
}
```

---

## 8. ログ出力要件

### LoggingManager連携
- **完全サイクル開始ログ**: 設定情報、デバイス情報
- **各ステップ開始/完了ログ**: Step3-6の各ステップ
- **完全サイクル完了ログ**: 統計情報、処理時間、デバイス数
- **エラーログ**: 各ステップでのエラー詳細（TC121では発生しない）

### ログレベル
- **Information**: 完全サイクル開始・完了、各ステップ完了
- **Debug**: 各ステップ詳細、データ内容
- **Error**: エラー発生時（TC121では発生しない）

### ログ出力例
```
[Information] FullCycle開始: PLC=192.168.1.10:5000, デバイス=M000-M999,D000-D999
[Debug] Step3開始: TCP接続処理
[Information] Step3完了: 接続時間=120ms
[Debug] Step4開始: M機器フレーム送信
[Information] Step4完了: M機器送信成功
[Debug] Step4開始: M機器応答受信
[Information] Step4完了: M機器受信成功, データ長=250文字
[Debug] Step4開始: D機器フレーム送信
[Information] Step4完了: D機器送信成功
[Debug] Step4開始: D機器応答受信
[Information] Step4完了: D機器受信成功, データ長=4000文字
[Debug] Step5開始: 切断処理
[Information] Step5完了: 切断時間=50ms
[Debug] Step6-1開始: M機器基本後処理
[Information] Step6-1完了: M機器処理デバイス数=1000
[Debug] Step6-1開始: D機器基本後処理
[Information] Step6-1完了: D機器処理デバイス数=1000
[Debug] Step6-2開始: DWord結合処理
[Information] Step6-2完了: DWord結合数=500
[Debug] Step6-3開始: 構造化変換処理
[Information] Step6-3完了: 構造化デバイス数=2000
[Information] FullCycle完了: 合計時間=350ms, 成功率=100%, デバイス数=2000
```

---

## 9. テスト実装チェックリスト

### TC121実装前
- [ ] 単体テスト完了確認（TC017-TC027, TC029-TC037）
- [ ] MockPlcServer作成（4Eフレーム対応）
- [ ] PlcResponseStubs作成（M機器、D機器）
- [ ] StructuredDataValidator作成

### TC121実装中
- [ ] Arrange: ConnectionConfig準備（TCP設定）
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: SLMPフレーム準備（2つ）
- [ ] Arrange: ProcessedDeviceRequestInfo準備
- [ ] Arrange: MockPlcServer起動・応答データ設定
- [ ] Act: Step3（ConnectAsync）実行
- [ ] Act: Step4（SendFrameAsync, ReceiveResponseAsync）実行（2回）
- [ ] Act: Step5（DisconnectAsync）実行
- [ ] Act: Step6-1（ProcessReceivedRawData）実行（2回）
- [ ] Act: Step6-1データ統合
- [ ] Act: Step6-2（CombineDwordData）実行
- [ ] Act: Step6-3（ParseRawToStructuredData）実行
- [ ] Assert: Step3検証（接続成功）
- [ ] Assert: Step4検証（送受信成功）
- [ ] Assert: Step5検証（切断成功）
- [ ] Assert: Step6-1検証（基本後処理成功）
- [ ] Assert: Step6-2検証（DWord結合成功）
- [ ] Assert: Step6-3検証（構造化変換成功）
- [ ] Assert: 統計情報検証（応答時間、成功率）
- [ ] Assert: データ整合性検証

### TC121実装後
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC122（複数サイクル統計累積）への準備
- [ ] TC123（エラー発生時スキップ）への準備

---

## 10. 参考情報

### 完全サイクル処理時間（目安）
- Step3（接続）: 50-150ms
- Step4（送信）: 10-50ms × 2回 = 20-100ms
- Step4（受信）: 50-200ms × 2回 = 100-400ms
- Step5（切断）: 10-50ms
- Step6-1（基本後処理）: 10-50ms × 2回 = 20-100ms
- Step6-2（DWord結合）: 5-20ms
- Step6-3（構造化変換）: 10-50ms
- **合計**: 215-870ms（通常300-500ms）

**重要**: TC121ではモックPLCサーバーを使用、実機PLC不要

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/

- FullCycleConfig.json: 完全サイクル設定サンプル
- M_DeviceResponse_4E.hex: M機器4Eフレーム応答サンプル
- D_DeviceResponse_4E.hex: D機器4Eフレーム応答サンプル
- FullCycle_StructuredData_Expected.json: 完全サイクル期待出力サンプル

---

以上が TC121_FullCycle_接続から構造化まで完全実行テスト実装に必要な情報です。
