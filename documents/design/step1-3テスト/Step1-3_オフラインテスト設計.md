# Step1-3 オフラインテスト設計書

## テスト対象範囲
- Step1: 設定ファイル読み込み (ConfigurationLoaderExcel)
- Step2: 通信フレーム構築 (ConfigToFrameManager)
- Step3: PLCへの送信準備・送信処理 (PlcCommunicationManager)

## テスト対象エクセルファイル
- ファイル名: 5JRS_N2.xlsx
- パス: C:\Users\1010821\Desktop\python\andon\5JRS_N2.xlsx
- タブ構成:
  - settingsタブ: PLC接続設定、収集周期等
  - データ収集デバイスタブ: 収集対象デバイス情報

## テスト環境制約
- オフラインテスト: 実機PLC接続不可
- モック/スタブを使用した動作検証
- TCP/UDP通信はMockSocket経由でシミュレート

---

## ⚠️ 実装不整合と修正要件

### 問題の詳細

**型の不一致により、現在の実装では動作しません。**

```
ConfigurationLoaderExcel → PlcConfiguration (List<DeviceSpecification>)
                                ↓ ❌ 型不一致
ConfigToFrameManager.BuildReadRandomFrameFromConfig(TargetDeviceConfig) (List<DeviceEntry>)
```

### 原因

実装が段階的に進められた結果、2つの設計パスが存在:
1. **JSONベースの設計** (古い): ConfigurationLoader → TargetDeviceConfig → DeviceEntry
2. **Excelベースの設計** (新しい): ConfigurationLoaderExcel → PlcConfiguration → DeviceSpecification

現在の`ConfigToFrameManager`は古いJSONベースの設計を前提としており、新しいExcelベースの設計には対応していない。

### 必要な修正

**ConfigToFrameManagerにPlcConfiguration用のオーバーロードメソッドを追加**

```csharp
/// <summary>
/// PlcConfigurationからReadRandomフレームを構築（Excel読み込み用）
/// </summary>
public byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    if (config.Devices == null || config.Devices.Count == 0)
        throw new ArgumentException("デバイスリストが空です", nameof(config));

    // PlcConfiguration.Devices は既に DeviceSpecification型のリスト
    // そのままSlmpFrameBuilderに渡せる
    byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
        config.Devices,
        frameType: "4E",  // 固定値
        timeout: 32       // 固定値
    );

    return frame;
}

/// <summary>
/// PlcConfigurationからReadRandomフレームを構築（ASCII形式、Excel読み込み用）
/// </summary>
public string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config)
{
    if (config == null)
        throw new ArgumentNullException(nameof(config));

    if (config.Devices == null || config.Devices.Count == 0)
        throw new ArgumentException("デバイスリストが空です", nameof(config));

    string asciiFrame = SlmpFrameBuilder.BuildReadRandomRequestAscii(
        config.Devices,
        frameType: "4E",
        timeout: 32
    );

    return asciiFrame;
}
```

### 修正後の動作フロー

```csharp
// ✅ 修正後：正常に動作
var loader = new ConfigurationLoaderExcel();
var plcConfig = loader.LoadAllPlcConnectionConfigs()[0]; // PlcConfiguration型

var frameManager = new ConfigToFrameManager();
var frame = frameManager.BuildReadRandomFrameFromConfig(plcConfig); // オーバーロード版を使用
```

### テストへの影響

**TC_Step3_04 統合テスト**の実装コードを以下のように修正する必要があります:

```csharp
// Step1: 設定読み込み（Excel）
var configLoader = new ConfigurationLoaderExcel();
var plcConfig = configLoader.LoadAllPlcConnectionConfigs()[0]; // PlcConfiguration

// Step2: フレーム構築（オーバーロード版を使用）
var frameManager = new ConfigToFrameManager();
var frameBytes = frameManager.BuildReadRandomFrameFromConfig(plcConfig); // ✅ 修正版

// Step3: 送信準備
// （PlcCommunicationManagerは接続情報のみ必要）
var connectionConfig = new ConnectionConfig
{
    IpAddress = plcConfig.IpAddress,
    Port = plcConfig.Port,
    UseTcp = false,  // UDP固定
    IsBinary = true,
    FrameVersion = FrameVersion.Frame4E
};

var mockSocket = new MockSocket(useTcp: false);
mockSocket.SetupConnected(true);
var socketFactory = new MockSocketFactory(mockSocket);

var plcManager = new PlcCommunicationManager(
    connectionConfig,
    timeoutConfig,
    socketFactory: socketFactory
);

// 実行
var connectResponse = await plcManager.ConnectAsync();
Assert.Equal(ConnectionStatus.Connected, connectResponse.Status);

await plcManager.SendFrameAsync(frameBytes);

var stats = plcManager.GetConnectionStats();
Assert.Equal(1, stats.TotalFramesSent);
Assert.Equal(frameBytes.Length, stats.TotalBytesSent);
```

---

## TDD実装順序（Red → Green → Refactor）

### Phase 1: Binary形式オーバーロードのTDD実装

#### Round 1: null検証（異常系）
1. **Red**: テストケース実装
   ```csharp
   [Fact]
   public void BuildReadRandomFrameFromConfig_PlcConfigurationがnull_例外をスローする()
   {
       // Arrange
       var frameManager = new ConfigToFrameManager();

       // Act & Assert
       Assert.Throws<ArgumentNullException>(() =>
           frameManager.BuildReadRandomFrameFromConfig((PlcConfiguration)null));
   }
   ```
2. **Green**: `ConfigToFrameManager`に最小限の実装を追加
   ```csharp
   public byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config)
   {
       if (config == null)
           throw new ArgumentNullException(nameof(config));

       return null; // まだ未実装
   }
   ```
3. **テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfig_PlcConfigurationがnull"`
4. **パス確認**: ✅ テストがパスすることを確認

#### Round 2: 空リスト検証（異常系）
1. **Red**: テストケース実装
   ```csharp
   [Fact]
   public void BuildReadRandomFrameFromConfig_デバイスリストが空_例外をスローする()
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
   ```
2. **Green**: Devicesリストの空チェック追加
   ```csharp
   public byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config)
   {
       if (config == null)
           throw new ArgumentNullException(nameof(config));

       if (config.Devices == null || config.Devices.Count == 0)
           throw new ArgumentException("デバイスリストが空です", nameof(config));

       return null; // まだ未実装
   }
   ```
3. **テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfig_デバイスリストが空"`
4. **パス確認**: ✅ テストがパスすることを確認

#### Round 3: フレーム構築（正常系）
1. **Red**: テストケース実装
   ```csharp
   [Fact]
   public void BuildReadRandomFrameFromConfig_PlcConfiguration_正常にフレームを構築する()
   {
       // Arrange
       var plcConfig = new PlcConfiguration
       {
           IpAddress = "172.30.40.40",
           Port = 8192,
           Devices = new List<DeviceSpecification>
           {
               new DeviceSpecification(DeviceCode.M, 33) { ItemName = "テスト1", Digits = 1, Unit = "bit" },
               new DeviceSpecification(DeviceCode.D, 100) { ItemName = "テスト2", Digits = 1, Unit = "word" }
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
   ```
2. **Green**: 完全な実装（SlmpFrameBuilderへの委譲）
   ```csharp
   public byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config)
   {
       if (config == null)
           throw new ArgumentNullException(nameof(config));

       if (config.Devices == null || config.Devices.Count == 0)
           throw new ArgumentException("デバイスリストが空です", nameof(config));

       byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
           config.Devices,
           frameType: "4E",
           timeout: 32
       );

       return frame;
   }
   ```
3. **テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfig_PlcConfiguration_正常に"`
4. **パス確認**: ✅ テストがパスすることを確認

#### Round 4: Binary形式全体テスト
1. **全テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfig"`
2. **パス確認**: ✅ Round 1-3の全てのテストがパスすることを確認
3. **Refactor**: 必要に応じてコード改善（今回は不要と判断）

---

### Phase 2: ASCII形式オーバーロードのTDD実装

#### Round 5: ASCII版null検証（異常系）
1. **Red**: テストケース実装
   ```csharp
   [Fact]
   public void BuildReadRandomFrameFromConfigAscii_PlcConfigurationがnull_例外をスローする()
   {
       // Arrange
       var frameManager = new ConfigToFrameManager();

       // Act & Assert
       Assert.Throws<ArgumentNullException>(() =>
           frameManager.BuildReadRandomFrameFromConfigAscii((PlcConfiguration)null));
   }
   ```
2. **Green**: 最小限の実装
   ```csharp
   public string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config)
   {
       if (config == null)
           throw new ArgumentNullException(nameof(config));

       return null; // まだ未実装
   }
   ```
3. **テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfigAscii_PlcConfigurationがnull"`
4. **パス確認**: ✅ テストがパスすることを確認

#### Round 6: ASCII版空リスト検証（異常系）
1. **Red**: テストケース実装
   ```csharp
   [Fact]
   public void BuildReadRandomFrameFromConfigAscii_デバイスリストが空_例外をスローする()
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
   ```
2. **Green**: Devicesリストの空チェック追加
   ```csharp
   public string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config)
   {
       if (config == null)
           throw new ArgumentNullException(nameof(config));

       if (config.Devices == null || config.Devices.Count == 0)
           throw new ArgumentException("デバイスリストが空です", nameof(config));

       return null; // まだ未実装
   }
   ```
3. **テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfigAscii_デバイスリストが空"`
4. **パス確認**: ✅ テストがパスすることを確認

#### Round 7: ASCII版フレーム構築（正常系）
1. **Red**: テストケース実装
   ```csharp
   [Fact]
   public void BuildReadRandomFrameFromConfigAscii_PlcConfiguration_正常にASCIIフレームを構築する()
   {
       // Arrange
       var plcConfig = new PlcConfiguration
       {
           IpAddress = "172.30.40.40",
           Port = 8192,
           Devices = new List<DeviceSpecification>
           {
               new DeviceSpecification(DeviceCode.M, 33) { ItemName = "テスト1", Digits = 1, Unit = "bit" }
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
       // オフセット26から監視タイマ、30からコマンド
       Assert.Contains("0403", asciiFrame.Substring(30, 4)); // コマンド 0x0403
   }
   ```
2. **Green**: 完全な実装
   ```csharp
   public string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config)
   {
       if (config == null)
           throw new ArgumentNullException(nameof(config));

       if (config.Devices == null || config.Devices.Count == 0)
           throw new ArgumentException("デバイスリストが空です", nameof(config));

       string asciiFrame = SlmpFrameBuilder.BuildReadRandomRequestAscii(
           config.Devices,
           frameType: "4E",
           timeout: 32
       );

       return asciiFrame;
   }
   ```
3. **テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfigAscii_PlcConfiguration_正常に"`
4. **パス確認**: ✅ テストがパスすることを確認

#### Round 8: ASCII形式全体テスト
1. **全テスト実行**: `dotnet test --filter "BuildReadRandomFrameFromConfigAscii"`
2. **パス確認**: ✅ Round 5-7の全てのテストがパスすることを確認
3. **Refactor**: 必要に応じてコード改善

---

### Phase 3: 統合テスト実装

#### Round 9: Excel読み込み→フレーム送信 統合テスト

**前提条件**: Phase 1（Round 1-4）のBinary形式テストが全てパスしていること

1. **Red**: 統合テストケース実装
   ```csharp
   [Fact]
   public async Task Excel読み込みからフレーム送信までの統合テスト()
   {
       // Step1: 設定読み込み（Excel）
       var configLoader = new ConfigurationLoaderExcel();
       var plcConfig = configLoader.LoadAllPlcConnectionConfigs()[0];

       // Excel読み込み検証
       Assert.Equal("172.30.40.40", plcConfig.IpAddress);
       Assert.Equal(8192, plcConfig.Port);
       Assert.Equal(225, plcConfig.Devices.Count); // 225デバイス

       // 先頭デバイス検証（M33）
       var firstDevice = plcConfig.Devices[0];
       Assert.Equal("M", firstDevice.DeviceType);
       Assert.Equal(33, firstDevice.DeviceNumber);
       Assert.Equal(DeviceCode.M, firstDevice.Code);

       // X機器検証（16進デバイス）
       var xDevice = plcConfig.Devices.First(d => d.DeviceType == "X");
       Assert.True(xDevice.IsHexAddress);
       Assert.Equal(192, xDevice.DeviceNumber); // 0xC0 = 192

       // Step2: フレーム構築（オーバーロード版）
       var frameManager = new ConfigToFrameManager();
       var frameBytes = frameManager.BuildReadRandomFrameFromConfig(plcConfig);

       // フレーム基本検証
       Assert.NotNull(frameBytes);
       Assert.True(frameBytes.Length > 0);

       // 4Eフレームヘッダ検証
       Assert.Equal(0x54, frameBytes[0]); // サブヘッダ
       Assert.Equal(0x00, frameBytes[1]);

       // ReadRandomコマンド検証 (オフセット15-16)
       Assert.Equal(0x03, frameBytes[15]); // コマンド下位
       Assert.Equal(0x04, frameBytes[16]); // コマンド上位

       // Step3: MockSocketで送信シミュレート
       var connectionConfig = new ConnectionConfig
       {
           IpAddress = plcConfig.IpAddress,
           Port = plcConfig.Port,
           UseTcp = false,
           IsBinary = true,
           FrameVersion = FrameVersion.Frame4E
       };

       var timeoutConfig = new TimeoutConfig
       {
           ConnectTimeoutMs = 5000,
           SendTimeoutMs = 3000,
           ReceiveTimeoutMs = 3000
       };

       var mockSocket = new MockSocket(useTcp: false);
       mockSocket.SetupConnected(true);
       var socketFactory = new MockSocketFactory(mockSocket);

       var plcManager = new PlcCommunicationManager(
           connectionConfig,
           timeoutConfig,
           socketFactory: socketFactory
       );

       // 接続検証
       var connectResponse = await plcManager.ConnectAsync();
       Assert.Equal(ConnectionStatus.Connected, connectResponse.Status);

       // フレーム送信検証
       await plcManager.SendFrameAsync(frameBytes);

       // 送信統計検証
       var stats = plcManager.GetConnectionStats();
       Assert.Equal(1, stats.TotalFramesSent);
       Assert.Equal(frameBytes.Length, stats.TotalBytesSent);

       // MockSocketに送信されたデータを検証
       var sentData = mockSocket.GetSentData();
       Assert.NotNull(sentData);
       Assert.Equal(frameBytes.Length, sentData.Length);
       Assert.Equal(frameBytes, sentData); // 送信データが元のフレームと一致
   }
   ```
2. **Green**: Phase 1で既に実装済みのため、テストがそのままパスするはず
3. **テスト実行**: `dotnet test --filter "Excel読み込みからフレーム送信までの統合テスト"`
4. **パス確認**: ✅ 統合テストがパスすることを確認

#### Round 10: 全テスト実行
1. **全テスト実行**: `dotnet test`（全てのテストを実行）
2. **パス確認**: ✅ Phase 1-3の全テストがパスすることを確認
3. **Refactor**: 必要に応じてコード改善

---

## テスト実装ファイル

**ファイル名**: `andon/Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs`

**実装するテスト（実装順序）**:

### Phase 1: Binary形式（Round 1-4）
1. `BuildReadRandomFrameFromConfig_PlcConfigurationがnull_例外をスローする()` (Round 1)
2. `BuildReadRandomFrameFromConfig_デバイスリストが空_例外をスローする()` (Round 2)
3. `BuildReadRandomFrameFromConfig_PlcConfiguration_正常にフレームを構築する()` (Round 3)
4. Binary形式全テスト実行 (Round 4)

### Phase 2: ASCII形式（Round 5-8）
5. `BuildReadRandomFrameFromConfigAscii_PlcConfigurationがnull_例外をスローする()` (Round 5)
6. `BuildReadRandomFrameFromConfigAscii_デバイスリストが空_例外をスローする()` (Round 6)
7. `BuildReadRandomFrameFromConfigAscii_PlcConfiguration_正常にASCIIフレームを構築する()` (Round 7)
8. ASCII形式全テスト実行 (Round 8)

### Phase 3: 統合テスト（Round 9-10）
9. `Excel読み込みからフレーム送信までの統合テスト()` (Round 9)
10. 全テスト実行 (Round 10)

---

## 実装手順サマリー

### TDD厳守ルール
1. **1つのテストを書く → 実装 → パス確認**を繰り返す
2. **複数のテストを一度に実装しない**
3. **単体テストが全てパスしてから統合テストに進む**
4. **各Roundでテスト実行とパス確認を必ず行う**

### 実装フロー
```
Phase 1: Binary形式
├── Round 1: null検証テスト実装 → 実装 → パス確認 ✅
├── Round 2: 空リストテスト実装 → 実装 → パス確認 ✅
├── Round 3: 正常系テスト実装 → 実装 → パス確認 ✅
└── Round 4: Binary全テスト実行 → パス確認 ✅

Phase 2: ASCII形式
├── Round 5: ASCII null検証テスト実装 → 実装 → パス確認 ✅
├── Round 6: ASCII空リストテスト実装 → 実装 → パス確認 ✅
├── Round 7: ASCII正常系テスト実装 → 実装 → パス確認 ✅
└── Round 8: ASCII全テスト実行 → パス確認 ✅

Phase 3: 統合テスト
├── Round 9: 統合テスト実装 → パス確認 ✅（Phase 1の実装を利用）
└── Round 10: 全テスト実行 → パス確認 ✅
```

---

## 注意事項

1. **オフラインテスト制約**:
   - 実機PLCへの接続は行わない
   - MockSocket/MockUdpServerを必ず使用
   - 実データ取得目的でのビルドは禁止

2. **TDD推奨**:
   - 単一機能ごとにテスト→実装→パス確認
   - 複合機能テストは単一機能パス後に実施

3. **文字化け対策**:
   - ファイル作成時は.txt経由でリネーム
   - 日本語コンテンツは必ずReadツールで確認

4. **エクセルファイルアクセス**:
   - 5JRS_N2.xlsxは読み取り専用で使用
   - テストデータはSampleExcelConfigsクラスで模擬

---

## Excel対応とJSON廃止計画

### 実装追加内容

Step1-3テストに向けて以下のクラス・メソッドを追加実装しました：

#### 1. PlcConfiguration クラス（Core/Models/ConfigModels/PlcConfiguration.cs）
**概要**: Excel設定ファイル（*.xlsx）から読み込んだPLC接続設定を保持

**プロパティ**:
- ConnectionConfig（接続設定）
- TimeoutConfig（タイムアウト設定）
- TargetDeviceConfig（対象デバイス設定）
- MonitoringIntervalMs（監視間隔）
- SystemResourcesConfig（システムリソース設定）
- DataProcessingConfig（データ処理設定）
- LoggingConfig（ログ設定）
- DataTransferConfig（データ転送設定）
- ActualConfigPath（実際のファイルパス）

#### 2. ConfigurationLoaderExcel クラス（Infrastructure/Configuration/ConfigurationLoaderExcel.cs）
**概要**: Excel設定ファイル（*.xlsx）からPLC接続設定を読み込み

**メソッド**:
- `LoadAllPlcConnectionConfigs(configDirectory, filePattern)`: 複数PLC設定一括読み込み
- `LoadPlcConnectionConfig(configFileName)`: 単一PLC設定読み込み

#### 3. ConfigToFrameManager のオーバーロードメソッド
**Excel設定用のオーバーロード**:
- `BuildReadRandomFrameFromConfig(PlcConfiguration config)`: Binary形式フレーム構築
- `BuildReadRandomFrameFromConfigAscii(PlcConfiguration config)`: ASCII形式フレーム構築

### JSON設定ファイル読み込み機能 廃止計画

#### 廃止理由

1. **設定管理の一元化**
   - Excel形式（*.xlsx）に統一することで、設定管理を簡素化
   - JSON形式とExcel形式の二重管理を避ける

2. **保守性の向上**
   - ConfigurationLoader（JSON用）とConfigurationLoaderExcel（Excel用）の重複実装を解消
   - 設定ファイル形式が統一され、ドキュメント・サンプルファイルの管理が容易

3. **ユーザビリティの向上**
   - Excel形式の方が視覚的に分かりやすく、編集が容易
   - 複数PLC設定を1つのExcelファイルで管理可能

#### 廃止対象

##### クラス・メソッド
- `ConfigurationLoader` クラス（Infrastructure/Configuration/ConfigurationLoader.cs）
  - `LoadPlcConnectionConfig()` - JSON読み込み
  - `ValidateConfig()` - JSON設定検証
- `ConfigToFrameManager` の以下メソッド（JSON用）
  - `BuildReadRandomFrameFromConfig(TargetDeviceConfig)` - JSON用Binary形式
  - `BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig)` - JSON用ASCII形式
  - `LoadConfigAsync()` - JSON設定ファイル読み込み（単一設定用）

##### 設定ファイル
- `appsettings.json` - JSON形式設定ファイル

##### 依存モデル（TargetDeviceConfig経由でのみ使用される場合）
- JSON専用の設定読み込みロジック
- JSON Schemaバリデーション関連

#### 移行計画

##### Phase 1: Excel読み込み機能完全実装（現在）
- ✅ `PlcConfiguration` クラス実装
- ✅ `ConfigurationLoaderExcel` クラス実装
- ✅ `BuildReadRandomFrameFromConfig(PlcConfiguration)` オーバーロード実装
- ✅ `BuildReadRandomFrameFromConfigAscii(PlcConfiguration)` オーバーロード実装
- 🔄 Step1-3オフラインテスト完了（Excel設定での動作確認）

##### Phase 2: 並行運用期間（移行猶予期間）
- JSON形式とExcel形式の両方をサポート
- 既存JSON設定ファイルをExcel形式に移行するツール提供（オプション）
- ドキュメント更新：Excel形式を推奨、JSON形式は非推奨（Deprecated）として明記

##### Phase 3: JSON機能廃止（Phase 2完了後）
- `ConfigurationLoader` クラス削除
- JSON用メソッドの削除：
  - `BuildReadRandomFrameFromConfig(TargetDeviceConfig)` 削除
  - `BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig)` 削除
  - `LoadConfigAsync()` 削除（JSON読み込み用）
- `appsettings.json` サンプルファイル削除
- 関連テストコードの削除・更新

##### Phase 4: クリーンアップ
- TargetDeviceConfigモデルの見直し（Excel設定に特化したプロパティ構成）
- 不要な依存関係の削除（JSON関連NuGetパッケージ）
- ドキュメント最終更新

#### 影響範囲

##### 削除が必要なファイル
```
Infrastructure/Configuration/
├── ConfigurationLoader.cs          // 削除
└── ConfigurationLoaderExcel.cs     // 存続（Excel専用）

Tests/Unit/Infrastructure/Configuration/
├── ConfigurationLoaderTests.cs     // 削除
└── ConfigurationLoaderExcelTests.cs // 存続
```

##### 更新が必要なファイル
```
Core/Managers/
└── ConfigToFrameManager.cs         // JSON用メソッド削除

documents/design/
├── プロジェクト構造設計.md          // JSON用記載削除
├── クラス設計.md                   // JSON用メソッド仕様削除
└── CLAUDE.md                       // JSON用記載削除
```

#### マイルストーン

- **Phase 1完了目標**: Step1-3オフラインテスト完了時点（現在進行中）
- **Phase 2開始**: 実機テスト開始時点
- **Phase 2期間**: 1-2ヶ月（移行猶予期間）
- **Phase 3実施**: 全システムExcel移行完了確認後
- **Phase 4完了**: リリース前最終クリーンアップ

#### 注意事項

1. **TargetDeviceConfigモデルの取り扱い**
   - 他の箇所で使用されている場合は残存させる
   - Excel設定専用に再設計する場合は影響範囲を十分に調査

2. **後方互換性**
   - Phase 2期間中は両形式をサポート
   - 既存ユーザー向けの移行ガイド作成

3. **テストデータ**
   - JSON形式のテストデータをExcel形式に移行
   - 単体テスト・統合テストの更新

---

## 参照情報

詳細なクラス仕様は以下のドキュメントを参照：
- `documents/design/クラス設計.md` - ConfigModels拡張仕様（Excel対応）セクション
- `documents/design/プロジェクト構造設計.md` - プロジェクト構造全体図
- `CLAUDE.md` - 実装者向けプロジェクト構造ガイド
