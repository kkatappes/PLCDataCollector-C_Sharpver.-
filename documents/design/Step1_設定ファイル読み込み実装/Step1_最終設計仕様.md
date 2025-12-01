# Step1: 設定ファイル読み込み - 最終設計仕様書

## 文書概要

**目的**: Step1（設定ファイル読み込み機能）の実装に必要な全情報を集約
**対象読者**: 実装担当者
**最終更新**: 2025-11-26

---

## 1. 機能概要

### 1.1 主要機能

#### A. 単一設定ファイル読み込み（既存機能保持）
- データ取得元：外部設定ファイル（appsettings.xlsx, UTF-8形式）
- 実装クラス：ConfigToFrameManager.LoadConfigAsync()
- 後方互換性：既存の単一PLC用システムとの互換性維持

#### B. 複数設定ファイル管理機能（新規）
- データ取得元：外部設定ファイル群（*_settings.xlsx, UTF-8形式）
- アーキテクチャ：共有データ + 軽量インスタンス
- 実装クラス：MultiConfigManager

### 1.2 実行フロー

```
起動時（1回のみ実行）
    ↓
設定ファイル読み込み（LoadConfigAsync / LoadAllConfigsAsync）
    ↓
各種マネージャークラスのインスタンス作成
    ↓
初期化処理完了
    ↓
Step2以降の処理へ
```

---

## 2. クラス設計詳細

### 2.1 ConfigToFrameManager

**配置**: `andon/Core/Managers/ConfigToFrameManager.cs`

#### 実装メソッド1: LoadConfigAsync（単一設定ファイル読み込み）

**シグネチャ**:
```csharp
Task<InitializationResult> LoadConfigAsync(string configFileName = "appsettings.xlsx");
```

**Input**:
- configFileName（string型、デフォルト："appsettings.xlsx"）

**Output**: InitializationResult
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
- TimeoutConfig（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs, RetryTimeoutMs）
- TargetDeviceConfig（Devices: List<DeviceSpecification>, FrameType, Timeout）
- MonitoringIntervalMs（データ収集周期）
- SystemResourcesConfig（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, LowMemoryMode）
- DataProcessingConfig（TargetName, ContinuousDataMode, DataRetentionDays）
- LoggingConfig（ConsoleOutput, DetailedLog）
- DataTransferConfig（EnableTransfer, DestinationIpAddress, DestinationPort）
- ActualConfigPath（string型、実際に読み込んだファイルパス）

**処理フロー**:
1. パス解決（ResolveConfigPath）
2. ConfigurationLoader.LoadPlcConnectionConfig() 呼び出し
3. 設定検証（ValidateConfig）
4. 内部状態更新
5. InitializationResult生成・返却

**パス解決順序**:
1. ./config/[fileName]
2. ./[fileName]
3. 環境変数ANDON_CONFIG_PATH

**成功条件**:
- 設定ファイルに記載されている値を正確に取得できること
- 必要な値が設定されていない場合はエラーを返すこと

---

#### 実装メソッド2: LoadAllConfigsAsync（複数設定ファイル読み込み）

**シグネチャ**:
```csharp
Task<Dictionary<string, InitializationResult>> LoadAllConfigsAsync(
    string configDirectory = "./config/",
    string filePattern = "*_settings.xlsx");
```

**Input**:
- configDirectory（string型、デフォルト："./config/"）
- filePattern（string型、デフォルト："*_settings.xlsx"）

**Output**:
- Dictionary<string, InitializationResult>（ファイル名キーの設定データ辞書）

**処理フロー**:
1. ディレクトリ内ファイル探索（Directory.GetFiles）
2. 各ファイルに対してLoadConfigAsync実行
3. 結果をDictionaryに格納
4. 共有データ領域への保存（静的フィールド）

**データ取得元**: 指定ディレクトリ内の複数設定ファイル（*_settings.xlsx, UTF-8形式）

**処理方式**: 共有データ領域への一括読み込み

---

#### 実装メソッド3: GetConfig（設定内容取得）

**シグネチャ**:
```csharp
T GetConfig<T>(string configFileName = null) where T : class;
```

**機能**: 単一インスタンス用設定取得（ジェネリック型対応）

**Input**:
- 設定タイプ指定（ジェネリック型パラメータT）
- configFileName（string型、オプション：軽量インスタンス用）

**Output**: 指定設定オブジェクト
- ConnectionConfig
- TimeoutConfig
- TargetDeviceConfig
- MonitoringIntervalMs
- SystemResourcesConfig
- DataProcessingConfig
- LoggingConfig
- DataTransferConfig

**データ取得元**:
- 単一設定モード: ConfigToFrameManager内部状態（LoadConfigAsync後）
- 複数設定モード: SharedConfigData（静的共有領域）

**使用例**:
```csharp
var connectionConfig = configManager.GetConfig<ConnectionConfig>();
var timeoutConfig = configManager.GetConfig<TimeoutConfig>("PLC1_settings.xlsx");
```

---

#### ヘルパーメソッド1: ValidateConfig（設定検証）

**シグネチャ**:
```csharp
private ValidationResult ValidateConfig(
    ConnectionConfig connection,
    TimeoutConfig timeout,
    TargetDeviceConfig device);
```

**機能**: 読み込んだ設定値の妥当性検証

**検証項目**:
- IPアドレス形式チェック（ValidationHelper.IsValidIpAddress）
- ポート番号範囲チェック（1-65535、ValidationHelper.IsValidPortNumber）
- タイムアウト値の正当性チェック（> 0）
- デバイス範囲形式チェック（List<DeviceSpecification>型検証）
- 必須項目の存在チェック

**Output**: ValidationResult
- IsValid（bool型）
- ErrorMessages（List<string>型）

---

#### ヘルパーメソッド2: ResolveConfigPath（設定ファイルパス解決）

**シグネチャ**:
```csharp
private string ResolveConfigPath(string configFileName);
```

**機能**: 設定ファイルの実パス解決

**解決順序**:
1. ./config/[fileName]
2. ./[fileName]
3. 環境変数ANDON_CONFIG_PATH

**Output**: 実際のファイルパス（string型）

**エラーハンドリング**:
- 全パスで見つからない場合：FileNotFoundException

---

### 2.2 MultiConfigManager

**配置**: `andon/Core/Managers/MultiConfigManager.cs`

#### 実装メソッド1: LoadAllFromDirectoryAsync（複数設定ファイル一括読み込み）

**シグネチャ**:
```csharp
Task<LoadResult> LoadAllFromDirectoryAsync(
    string configDirectory = "./config/",
    string filePattern = "*_settings.xlsx",
    bool allowPartialSuccess = true);
```

**Input**:
- configDirectory（string型、デフォルト："./config/"）
- filePattern（string型、デフォルト："*_settings.xlsx"）
- allowPartialSuccess（bool型、デフォルト：true）

**Output**: LoadResult
- LoadedFiles（string[]型）：成功したファイル名一覧
- FailedFiles（Dictionary<string, string>型）：失敗したファイルとエラーメッセージ
- TotalLoadTime（TimeSpan型）：総読み込み時間
- Managers（Dictionary<string, ConfigToFrameManager>型）：軽量インスタンス辞書

**処理フロー**:
1. ディレクトリ内ファイル探索
2. 各ファイルをConfigToFrameManager.LoadConfigAsyncで読み込み
3. 設定データを静的共有領域に保存
4. 各ファイル用の軽量インスタンス生成
5. LoadResult生成・返却

**データ取得元**: 指定ディレクトリ内の複数設定ファイル（*_settings.xlsx, UTF-8形式）

**処理方式**: 設定データを静的共有領域に保存、各ファイル用の軽量インスタンス生成

---

#### 実装メソッド2: CreateManagersAsync（軽量インスタンス生成）

**シグネチャ**:
```csharp
Task<ConfigToFrameManager[]> CreateManagersAsync(string[] configFileNames);
```

**Input**:
- configFileNames（string[]型：対象設定ファイル名一覧）

**Output**:
- ConfigToFrameManager[]（軽量インスタンス配列）

**データ取得元**: SharedConfigData（静的共有領域）

**動作**: 共有データを参照する軽量インスタンスを効率的に生成

---

#### 実装メソッド3: GetSharedConfigData（共有データアクセス）

**シグネチャ**:
```csharp
static ConfigDataSet GetSharedConfigData(string configFileName);
```

**Input**:
- configFileName（string型：対象設定ファイル名）

**Output**:
- ConfigDataSet（指定ファイルの設定データ）

**データ取得元**: SharedConfigData（静的共有領域）

**メモリ効率**: 設定データ実体の共有によりメモリ使用量最小化

---

#### 実装メソッド4: ReleaseSharedData（共有データ解放）

**シグネチャ**:
```csharp
static long ReleaseSharedData(string configFileName = null);
```

**Input**:
- configFileName（string型、オプション：特定ファイル or 全体）

**Output**:
- ReleasedMemoryKB（long型：解放されたメモリ量）

**動作**: 明示的なメモリ解放（長時間稼働時の最適化）

---

### 2.3 ConfigurationLoader

**配置**: `andon/Infrastructure/Configuration/ConfigurationLoader.cs`

#### 実装メソッド1: LoadPlcConnectionConfig

**シグネチャ**:
```csharp
Task<PlcConnectionConfig> LoadPlcConnectionConfig(string configFilePath);
```

**機能**: 設定ファイルの実際の読み込み処理

**処理フロー**:
1. ファイル存在チェック
2. ClosedXMLでxlsxファイル読み込み
3. 各セクションのパース
   - ConnectionConfig
   - TimeoutConfig
   - TargetDeviceConfig（DeviceEntryリスト → DeviceSpecification変換）
   - MonitoringIntervalMs
   - SystemResourcesConfig
   - DataProcessingConfig
   - LoggingConfig
   - DataTransferConfig
4. PlcConnectionConfig生成・返却

**DeviceEntry → DeviceSpecification変換**:
```csharp
// DeviceEntryからDeviceSpecificationへの変換
var deviceSpecs = deviceEntries.Select(entry => entry.ToDeviceSpecification()).ToList();
```

---

#### 実装メソッド2: ValidateConfig

**シグネチャ**:
```csharp
ValidationResult ValidateConfig(PlcConnectionConfig config);
```

**機能**: 設定内容の妥当性検証（ConfigToFrameManager.ValidateConfigと同等）

---

## 3. データモデル設計

### 3.1 設定関連モデル（ConfigModels）

**配置**: `andon/Core/Models/ConfigModels/`

#### ConnectionConfig（接続設定）

**プロパティ**:
```csharp
public string IpAddress { get; init; }
public int Port { get; init; }
public bool UseTcp { get; init; }
public string ConnectionType { get; init; }  // "TCP"/"UDP"、ログ出力用
public bool IsBinary { get; init; }
public string FrameVersion { get; init; }    // "3E"/"4E"
```

**データ取得元**: ConfigurationLoader.LoadPlcConnectionConfig()

---

#### TimeoutConfig（タイムアウト設定）

**プロパティ**:
```csharp
public int ConnectTimeoutMs { get; init; }
public int SendTimeoutMs { get; init; }
public int ReceiveTimeoutMs { get; init; }
public int RetryTimeoutMs { get; init; }
```

**データ取得元**: ConfigurationLoader.LoadPlcConnectionConfig()

---

#### TargetDeviceConfig（デバイス設定）

**プロパティ**:
```csharp
public List<DeviceSpecification> Devices { get; init; }
public string FrameType { get; init; }  // "3E"/"4E"
public ushort Timeout { get; init; }     // 監視タイマ（250ms単位、デフォルト32=8秒）
```

**用途**:
- Random READ(0x0403)コマンドでのデバイス一括取得
- 飛び飛びのデバイス指定に対応
- ビット・ワード・ダブルワード混在対応

**データ取得元**: ConfigurationLoader.LoadPlcConnectionConfig()

**注意**: Phase6(2025-11-21)でリスト形式に変更、旧形式のMDeviceRange/DDeviceRangeは廃止

---

#### DeviceEntry（設定読み込み用中間型）

**配置**: `andon/Core/Models/ConfigModels/DeviceEntry.cs`

**プロパティ**:
```csharp
public string DeviceType { get; init; }     // "D", "M", "W"等
public int DeviceNumber { get; init; }      // デバイス番号（10進表記）
public bool IsHexAddress { get; init; }     // 16進アドレス表記フラグ（デフォルト: false）
public string? Description { get; init; }   // デバイス説明（省略可）
```

**メソッド**:
```csharp
public DeviceSpecification ToDeviceSpecification();
```

**用途**:
- appsettings.jsonのDevicesリスト要素からの読み込み専用
- ConfigurationLoader内でDeviceSpecificationに変換
- アプリケーション内部では使用しない（中間型）

**実装日**: Phase6 (2025-11-21)

**変換例**:
```csharp
// DeviceEntry
{
    "DeviceType": "M",
    "DeviceNumber": 0,
    "IsHexAddress": false,
    "Description": "運転状態"
}

// ↓ ToDeviceSpecification()

// DeviceSpecification
{
    Code = DeviceCode.M,
    DeviceNumber = 0,
    IsHexAddress = false
}
```

---

#### DeviceSpecification（デバイス指定情報）

**配置**: `andon/Core/Models/DeviceSpecification.cs`

**プロパティ**:
```csharp
public DeviceCode Code { get; init; }
public int DeviceNumber { get; init; }
public bool IsHexAddress { get; init; }
```

**メソッド**:
```csharp
public static DeviceSpecification FromHexString(string hexString);
public byte[] ToDeviceNumberBytes();
public byte[] ToDeviceSpecificationBytes();
public bool ValidateForReadRandom();
```

**用途**:
- Random READ(0x0403)コマンドでのデバイス指定
- 不連続デバイスアドレスの指定に対応
- ビット・ワード・ダブルワードの区別なく統一的に扱う

**実装日**: Phase1 (2025-11-20)

**設計背景**:
- Random READ方式採用に伴う核心的なデータ構造
- DeviceEntryからの変換により設定ファイルとの橋渡し
- フレーム構築時の直接的なバイト列変換をサポート

---

#### SystemResourcesConfig（システムリソース設定）

**プロパティ**:
```csharp
public int MemoryLimitKB { get; init; }
public int MaxBufferSize { get; init; }
public int MemoryThresholdKB { get; init; }
public bool LowMemoryMode { get; init; }
```

---

#### DataProcessingConfig（データ処理設定）

**プロパティ**:
```csharp
public string TargetName { get; init; }
public bool ContinuousDataMode { get; init; }
public int DataRetentionDays { get; init; }
```

---

#### LoggingConfig（ログ設定）

**プロパティ**:
```csharp
public bool ConsoleOutput { get; init; }
public bool DetailedLog { get; init; }
```

---

#### DataTransferConfig（データ転送設定）

**プロパティ**:
```csharp
public bool EnableTransfer { get; init; }
public string DestinationIpAddress { get; init; }
public int DestinationPort { get; init; }
```

---

### 3.2 処理結果モデル

#### InitializationResult（初期化結果）

**配置**: `andon/Core/Models/InitializationResult.cs`

**プロパティ**:
```csharp
public bool Success { get; init; }
public string? ErrorMessage { get; init; }
public TimeSpan LoadTime { get; init; }
public string ConfigFilePath { get; init; }
public Dictionary<string, object> ConfigData { get; init; }
```

**データ取得元**: ConfigToFrameManager.LoadConfigAsync()

---

#### ValidationResult（検証結果）

**プロパティ**:
```csharp
public bool IsValid { get; init; }
public List<string> ErrorMessages { get; init; }
```

---

#### LoadResult（複数設定読み込み結果）

**プロパティ**:
```csharp
public string[] LoadedFiles { get; init; }
public Dictionary<string, string> FailedFiles { get; init; }
public TimeSpan TotalLoadTime { get; init; }
public Dictionary<string, ConfigToFrameManager> Managers { get; init; }
```

---

### 3.3 列挙型（Enums）

#### DeviceCode（デバイスコード）

**配置**: `andon/Core/Constants/DeviceConstants.cs`

**定義**:
```csharp
public enum DeviceCode : byte
{
    SM = 0x91,  // 特殊リレー
    X = 0x9C,   // 入力
    Y = 0x9D,   // 出力
    M = 0x90,   // 内部リレー
    D = 0xA8,   // データレジスタ
    W = 0xB4,   // リンクレジスタ
    // ... その他
}
```

**拡張メソッド**:
```csharp
public static class DeviceCodeExtensions
{
    public static bool IsHexAddress(this DeviceCode code);
    public static bool IsBitDevice(this DeviceCode code);
    public static bool IsReadRandomSupported(this DeviceCode code);
}
```

**実装日**: Phase1 (2025-11-20)

---

## 4. ユーティリティ・ヘルパーメソッド

### 4.1 ValidationHelper

**配置**: `andon/Utilities/ValidationHelper.cs`

#### IsValidIpAddress

**シグネチャ**:
```csharp
public static bool IsValidIpAddress(string ipAddress);
```

**検証内容**:
- IPv4形式チェック
- IPv6形式チェック（オプション）

---

#### IsValidPortNumber

**シグネチャ**:
```csharp
public static bool IsValidPortNumber(int port);
```

**検証内容**:
- 範囲チェック（1-65535）

---

#### IsValidDeviceCode

**シグネチャ**:
```csharp
public static bool IsValidDeviceCode(string deviceCode);
```

**検証内容**:
- サポートデバイスコードとの照合
- 対応表：M: 90, D: A8, X: 9C, Y: 9D, その他（仕様書準拠）

---

## 5. インターフェース設計

### 5.1 IConfigManager

**配置**: `andon/Core/Interfaces/IConfigManager.cs`

**メソッド定義**:
```csharp
public interface IConfigManager
{
    Task<InitializationResult> LoadConfigAsync(string configFileName = "appsettings.xlsx");
    T GetConfig<T>(string configFileName = null) where T : class;
}
```

**実装クラス**:
- ConfigToFrameManager
- MultiConfigManager

**目的**:
- 設定読み込み処理の抽象化
- テスト容易性の向上
- 将来的な設定ソース変更への対応（XML, JSON, データベース等）

---

## 6. エラーハンドリング設計

### 6.1 例外設計（バランス型）

#### 標準例外使用（Step1関連）

- **FileNotFoundException**: 設定ファイル不存在
- **IOException**: ファイル読み込みエラー
- **FormatException**: 設定値形式不正
- **UnauthorizedAccessException**: ファイルアクセス権限エラー

#### カスタム例外（複数設定専用）

**MultiConfigLoadException**

**配置**: `andon/Core/Exceptions/MultiConfigLoadException.cs`

**定義**:
```csharp
public class MultiConfigLoadException : Exception
{
    public string[] FailedFiles { get; init; }
    public Dictionary<string, string> FileErrors { get; init; }

    public MultiConfigLoadException(string message, Dictionary<string, string> fileErrors)
        : base(message)
    {
        FailedFiles = fileErrors.Keys.ToArray();
        FileErrors = fileErrors;
    }
}
```

**用途**: 複数設定ファイル読み込み時の部分失敗情報を集約

---

### 6.2 エラーメッセージ管理

**配置**: `andon/Core/Constants/ErrorMessages.cs`

**定義**:
```csharp
public static class ErrorMessages
{
    // 設定ファイル関連
    public const string ConfigFileNotFound = "設定ファイルが見つかりません: {0}";
    public const string ConfigFileInvalid = "設定ファイルの形式が不正です: {0}";
    public const string ConfigValueMissing = "必須設定項目が見つかりません: {0}";
    public const string ConfigValueInvalid = "設定値が不正です: {0} - {1}";

    // 複数設定ファイル関連
    public const string MultiConfigLoadFailed = "{0}個の設定ファイル読み込みに失敗しました";
    public const string MultiConfigPartialSuccess = "{0}個の設定ファイル読み込みに成功、{1}個が失敗しました";
}
```

---

## 7. TDD実装方針

### 7.1 テストPhase

#### Phase 1: ConfigToFrameManager単体テスト (TC001-TC016)

**テストファイル**: `Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs`

**テストケース**:
- TC001: LoadConfigAsync - 正常系（デフォルトファイル名）
- TC002: LoadConfigAsync - 正常系（カスタムファイル名）
- TC003: LoadConfigAsync - 異常系（ファイル不存在）
- TC004: LoadConfigAsync - 異常系（不正形式）
- TC005: GetConfig - 正常系（ConnectionConfig取得）
- TC006: GetConfig - 正常系（TimeoutConfig取得）
- TC007: GetConfig - 異常系（未初期化状態）
- TC008-TC011: 各種ヘルパーメソッドテスト

**モック・スタブ**:
- `Tests/TestUtilities/Mocks/MockConfigToFrameManager.cs`
- `Tests/TestUtilities/Stubs/ConfigurationStubs.cs`

---

#### Phase 1-2: MultiConfigManager単体テスト (TC016_1-TC016_12)

**テストファイル**: `Tests/Unit/Core/Managers/MultiConfigManagerTests.cs`

**テストケース**:
- TC016_1: LoadAllFromDirectoryAsync - 正常系（全ファイル成功）
- TC016_2: LoadAllFromDirectoryAsync - 部分成功
- TC016_3: LoadAllFromDirectoryAsync - 全失敗
- TC016_4-TC016_7: 共有データアクセステスト
- TC016_8-TC016_12: メモリ管理テスト

---

### 7.2 テスト実装順序

1. **ConfigurationLoader単体テスト** (TC001-TC004)
   - LoadPlcConnectionConfig
   - ValidateConfig

2. **ConfigToFrameManager単体テスト** (TC005-TC016)
   - LoadConfigAsync
   - GetConfig
   - ヘルパーメソッド

3. **MultiConfigManager単体テスト** (TC016_1-TC016_12)
   - LoadAllFromDirectoryAsync
   - 共有データ管理
   - メモリ最適化

4. **統合テスト** (TC047)
   - Step1-2統合テスト
   - 設定読み込み～フレーム構築統合

---

## 8. 設計方針・原則

### 8.1 アーキテクチャ原則

#### 共有データ + 軽量インスタンス
- 設定データは静的共有領域に保存
- インスタンスは共有データへの参照のみ保持
- メモリ効率の最大化

#### 後方互換性維持
- 単一設定ファイルモードの完全保持
- 既存コードへの影響最小化
- 段階的な移行サポート

---

### 8.2 拡張性

- 新しい設定項目の追加が容易
- 設定ファイル形式の柔軟な変更対応
- 将来的な設定変更監視機能の追加に備えた設計

---

### 8.3 保守性

- 設定読み込みロジックの一元管理
- 明確なエラーハンドリング
- 詳細なログ出力による問題追跡

---

### 8.4 テスタビリティ

- インターフェースを通じた依存関係の抽象化
- モック実装による単体テスト対応
- オフライン環境でのテスト実施

---

## 9. 実装チェックリスト

### 9.1 ConfigToFrameManager実装

- [ ] LoadConfigAsync実装
  - [ ] パス解決処理
  - [ ] ConfigurationLoader呼び出し
  - [ ] 設定検証
  - [ ] InitializationResult生成
- [ ] GetConfig実装
  - [ ] ジェネリック型対応
  - [ ] 内部状態参照
  - [ ] null安全性確保
- [ ] ValidateConfig実装
  - [ ] IPアドレス検証
  - [ ] ポート番号検証
  - [ ] タイムアウト検証
  - [ ] デバイス設定検証
- [ ] ResolveConfigPath実装
  - [ ] 3段階パス解決
  - [ ] エラーハンドリング

---

### 9.2 MultiConfigManager実装

- [ ] LoadAllFromDirectoryAsync実装
  - [ ] ファイル探索
  - [ ] 並列読み込み
  - [ ] 共有データ保存
  - [ ] LoadResult生成
- [ ] CreateManagersAsync実装
  - [ ] 軽量インスタンス生成
  - [ ] 共有データ参照設定
- [ ] GetSharedConfigData実装
  - [ ] 静的フィールドアクセス
  - [ ] スレッドセーフ対応
- [ ] ReleaseSharedData実装
  - [ ] メモリ解放処理
  - [ ] 解放量計測

---

### 9.3 ConfigurationLoader実装

- [ ] LoadPlcConnectionConfig実装
  - [ ] ClosedXMLでxlsx読み込み
  - [ ] 各セクションパース
  - [ ] DeviceEntry → DeviceSpecification変換
  - [ ] PlcConnectionConfig生成
- [ ] ValidateConfig実装
  - [ ] 設定内容検証

---

### 9.4 データモデル実装

- [ ] ConnectionConfig実装
- [ ] TimeoutConfig実装
- [ ] TargetDeviceConfig実装
- [ ] DeviceEntry実装
  - [ ] ToDeviceSpecification()メソッド
- [ ] DeviceSpecification実装（Phase1実装済み）
- [ ] SystemResourcesConfig実装
- [ ] DataProcessingConfig実装
- [ ] LoggingConfig実装
- [ ] DataTransferConfig実装
- [ ] InitializationResult実装
- [ ] ValidationResult実装
- [ ] LoadResult実装

---

### 9.5 テスト実装

- [ ] ConfigToFrameManagerTests実装（TC001-TC016）
- [ ] MultiConfigManagerTests実装（TC016_1-TC016_12）
- [ ] ConfigurationLoaderTests実装
- [ ] Step1-2統合テスト実装（TC047）

---

## 10. 成功条件

### 10.1 機能要件

- [ ] 設定ファイルに記載されている値を正確に取得できること
- [ ] 必要な値が設定されていない場合はエラーを返すこと
- [ ] 複数設定ファイルを効率的に読み込めること
- [ ] Step1で作成したインスタンスが各サイクル（Step2-7）で正常に動作すること

---

### 10.2 非機能要件

- [ ] メモリ使用量が最小化されていること（共有データ方式）
- [ ] 後方互換性が維持されていること（単一設定ファイルモード）
- [ ] エラーメッセージが明確で問題追跡が容易であること
- [ ] テストカバレッジが十分であること（80%以上）

---

## 11. 参照文書

### 11.1 設計文書

- `documents/design/クラス設計/Step1_設定ファイル読み込み.md`
- `documents/design/クラス設計/インターフェース定義.md`
- `documents/design/クラス設計/ヘルパーメソッド・ユーティリティ.md`
- `documents/design/クラス設計/補助クラス・データモデル.md`
- `documents/design/プロジェクト構造設計.md`

---

### 11.2 実装関連

- `CLAUDE.md` - プロジェクト実装方針
- `documents/テスト内容.md` - テスト設計詳細

---

## 12. 変更履歴

| 日付 | バージョン | 変更内容 |
|------|-----------|---------|
| 2025-11-26 | 1.0 | 初版作成（各設計文書から情報集約） |

---

## 13. 実装時の注意事項

### 13.1 文字化け対策

#### ファイル作成時
1. .txt拡張子で作成
2. Readツールで内容確認
3. .cs拡張子にリネーム
4. 再度Readツールで正常表示を確認

---

### 13.2 コーディング標準

- **非同期メソッド**: 全I/O操作はAsync/await使用
- **例外処理**: 各レイヤーで適切な例外ハンドリング
- **null安全性**: nullable reference types使用
- **設定外部化**: appsettings.xlsx + 環境変数対応

---

### 13.3 TDD実装フロー

1. テストケース作成（Red）
2. 最小実装（Green）
3. リファクタリング（Refactor）
4. 単一ブロック毎にテストパス確認後、複合時の動作テストを実施

---

## 14. 実装開始前確認事項

- [ ] CLAUDE.mdの内容を理解している
- [ ] この最終設計仕様書の内容を理解している
- [ ] テスト駆動開発（TDD）の手法を理解している
- [ ] 文字化け対策を理解している
- [ ] モック/スタブを利用したオフラインテストの実施を理解している
- [ ] ユーザーへ実装開始の確認を取得した

---

## 15. 既存実装との関連・対応情報

### 15.1 現在の実装状況サマリ

#### 実装済みクラス・メソッド

**ConfigToFrameManager** (`andon/Core/Managers/ConfigToFrameManager.cs`)
- ✅ `BuildReadRandomFrameFromConfig(TargetDeviceConfig)` - Binary形式フレーム構築
- ✅ `BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig)` - ASCII形式フレーム構築
- ❌ `LoadConfigAsync()` - **未実装（Step1で実装予定）**
- ❌ `GetConfig<T>()` - **未実装（Step1で実装予定）**
- ❌ ヘルパーメソッド群 - **未実装（Step1で実装予定）**

**MultiConfigManager** (`andon/Core/Managers/MultiConfigManager.cs`)
- ❌ 全メソッド未実装 - **Step1で実装予定**

**ConfigurationLoader** (`andon/Infrastructure/Configuration/ConfigurationLoader.cs`)
- ✅ `LoadPlcConnectionConfig()` - 実装済み（TargetDeviceConfig読み込み）
- ✅ `ValidateConfig(TargetDeviceConfig)` - 実装済み（設定検証）
- ⚠️ **注意**: 現在は`TargetDeviceConfig`のみ読み込み、他の設定は未対応

**IConfigToFrameManager** (`andon/Core/Interfaces/IConfigToFrameManager.cs`)
- ❌ メソッドシグネチャ未定義 - **Step1で定義予定**

---

#### 実装済みデータモデル

**完全実装済み**:
- ✅ `DeviceEntry` - 設定読み込み用中間型（`ToDeviceSpecification()`実装済み）
- ✅ `DeviceSpecification` - ReadRandom用デバイス指定情報（Phase1実装）
- ✅ `TargetDeviceConfig` - デバイス設定（List<DeviceEntry>形式）
- ✅ `ConnectionConfig` - PLC接続設定
- ✅ `TimeoutConfig` - タイムアウト設定
- ✅ `PlcConnectionConfig` - 個別PLC接続設定（PlcId, PlcName, Priority付き）

**未実装（スケルトンのみ）**:
- ❌ `InitializationResult` - **Step1で実装予定**
- ❌ `SystemResourcesConfig` - **Step1で実装予定**
- ❌ `DataProcessingConfig` - **Step1で実装予定**
- ❌ `LoggingConfig` - **Step1で実装予定**
- ❌ `DataTransferConfig` - **Step1で実装予定**

---

#### 実装済みユーティリティ

**SlmpFrameBuilder** (`andon/Utilities/SlmpFrameBuilder.cs`)
- ✅ `BuildReadRandomRequest(List<DeviceSpecification>, string, ushort)` - Binary形式（Phase2実装）
- ✅ `BuildReadRandomRequestAscii(List<DeviceSpecification>, string, ushort)` - ASCII形式（Phase2実装）

**DeviceConstants** (`andon/Core/Constants/DeviceConstants.cs`)
- ✅ `DeviceCode` enum - 完全実装済み（SM, X, Y, M, D, W, R, ZR, TC, TN, TS, CN, CS, CC, B, F, L）
- ✅ `DeviceCodeExtensions` - 拡張メソッド実装済み（`IsHexAddress()`, `IsBitDevice()`, `IsReadRandomSupported()`等）

**ErrorMessages** (`andon/Core/Constants/ErrorMessages.cs`)
- ✅ 既存エラーメッセージ定義（PLC接続、フレーム送受信、ソケット、データ処理関連）
- ⚠️ **Step1専用メッセージは未定義** - 追加が必要

---

### 15.2 既存実装との連携ポイント

#### A. ConfigToFrameManager実装時の連携

**既存メソッドの活用**:
```csharp
// Step1で実装するLoadConfigAsync()内での既存メソッド呼び出し例
public async Task<InitializationResult> LoadConfigAsync(string configFileName = "appsettings.json")
{
    // 1. ConfigurationLoaderで設定読み込み（既存実装を活用）
    var targetDeviceConfig = _configLoader.LoadPlcConnectionConfig();

    // 2. BuildReadRandomFrameFromConfig()で検証（既存実装を活用）
    // - このメソッドは既に実装済み
    // - 内部でSlmpFrameBuilder.BuildReadRandomRequest()を呼び出す
    byte[] testFrame = BuildReadRandomFrameFromConfig(targetDeviceConfig);

    // 3. InitializationResult生成（新規実装）
    return new InitializationResult
    {
        Success = true,
        // ... その他のプロパティ
    };
}
```

**活用する既存機能**:
1. `ConfigurationLoader.LoadPlcConnectionConfig()` - TargetDeviceConfig読み込み
2. `DeviceEntry.ToDeviceSpecification()` - デバイス仕様変換
3. `SlmpFrameBuilder.BuildReadRandomRequest()` - フレーム構築（検証用）
4. `DeviceSpecification.ValidateForReadRandom()` - デバイス検証
5. `DeviceSpecification.ValidateDeviceNumberRange()` - 番号範囲検証

---

#### B. ConfigurationLoader拡張時の注意点

**現在の制限事項**:
- ✅ 実装済み: `TargetDeviceConfig`の読み込みのみ
- ❌ 未実装: `ConnectionConfig`, `TimeoutConfig`, `SystemResourcesConfig`等

**Step1での拡張方針**:
```csharp
// ConfigurationLoader.cs に以下のメソッドを追加
public ConnectionConfig LoadConnectionConfig()
{
    var section = _configuration.GetSection("PlcCommunication:Connection");
    return new ConnectionConfig
    {
        IpAddress = section["IpAddress"] ?? "127.0.0.1",
        Port = int.Parse(section["Port"] ?? "8192"),
        UseTcp = bool.Parse(section["UseTcp"] ?? "false"),
        IsBinary = bool.Parse(section["IsBinary"] ?? "false"),
        FrameVersion = Enum.Parse<FrameVersion>(section["FrameVersion"] ?? "Frame4E")
    };
}

public TimeoutConfig LoadTimeoutConfig()
{
    var section = _configuration.GetSection("PlcCommunication:Timeouts");
    return new TimeoutConfig
    {
        ConnectTimeoutMs = int.Parse(section["ConnectTimeoutMs"] ?? "5000"),
        SendTimeoutMs = int.Parse(section["SendTimeoutMs"] ?? "3000"),
        ReceiveTimeoutMs = int.Parse(section["ReceiveTimeoutMs"] ?? "5000"),
        SendIntervalMs = int.Parse(section["SendIntervalMs"] ?? "100")
    };
}

// ... その他の設定読み込みメソッド
```

---

#### C. appsettings.json形式との対応

**現在のappsettings.json構造**:
```json
{
  "PlcCommunication": {
    "Connection": {
      "IpAddress": "172.30.40.15",
      "Port": 8192,
      "UseTcp": false,
      "IsBinary": false,
      "FrameVersion": "4E"
    },
    "Timeouts": {
      "ConnectTimeoutMs": 3000,
      "SendTimeoutMs": 500,
      "ReceiveTimeoutMs": 500
    },
    "TargetDevices": {
      "Devices": [
        {
          "DeviceType": "M",
          "DeviceNumber": 0,
          "Description": "運転状態フラグ開始"
        },
        // ... その他のデバイス
      ]
    },
    "MonitoringIntervalMs": 1000,
    "DataProcessing": {
      "BitExpansion": {
        "Enabled": true,
        "SelectionMask": [ /* ... */ ],
        "ConversionFactors": [ /* ... */ ]
      }
    }
  },
  "SystemResources": {
    "MemoryLimitKB": 450,
    "MaxBufferSize": 2048,
    "MemoryThresholdKB": 512
  },
  "Logging": {
    "ConsoleOutput": { /* ... */ },
    "DetailedLog": { /* ... */ }
  }
}
```

**設定セクションとモデルクラスのマッピング**:
| appsettings.jsonパス | モデルクラス | 実装状況 |
|---------------------|-------------|---------|
| `PlcCommunication:Connection` | `ConnectionConfig` | ❌ 未実装 |
| `PlcCommunication:Timeouts` | `TimeoutConfig` | ❌ 未実装 |
| `PlcCommunication:TargetDevices` | `TargetDeviceConfig` | ✅ 実装済み |
| `PlcCommunication:MonitoringIntervalMs` | `int` (単体プロパティ) | ❌ 未実装 |
| `PlcCommunication:DataProcessing` | `DataProcessingConfig` | ❌ 未実装 |
| `SystemResources` | `SystemResourcesConfig` | ❌ 未実装 |
| `Logging` | `LoggingConfig` | ❌ 未実装 |

---

### 15.3 Step1実装で必要な新規クラス・メソッド

#### 必須実装リスト

**ConfigToFrameManager拡張** (`andon/Core/Managers/ConfigToFrameManager.cs`):
```csharp
// 新規メソッド
public async Task<InitializationResult> LoadConfigAsync(string configFileName = "appsettings.json");
public T GetConfig<T>(string configFileName = null) where T : class;

// 新規ヘルパーメソッド
private ValidationResult ValidateConfig(ConnectionConfig, TimeoutConfig, TargetDeviceConfig);
private string ResolveConfigPath(string configFileName);

// 新規フィールド
private ConnectionConfig? _connectionConfig;
private TimeoutConfig? _timeoutConfig;
private TargetDeviceConfig? _targetDeviceConfig;
private SystemResourcesConfig? _systemResourcesConfig;
private DataProcessingConfig? _dataProcessingConfig;
private LoggingConfig? _loggingConfig;
private DataTransferConfig? _dataTransferConfig;
private int _monitoringIntervalMs;
private string? _actualConfigPath;
```

**MultiConfigManager実装** (`andon/Core/Managers/MultiConfigManager.cs`):
```csharp
// 新規実装
public class MultiConfigManager
{
    // 静的共有データ領域
    private static readonly Dictionary<string, ConfigDataSet> SharedConfigData = new();

    // メソッド
    public async Task<LoadResult> LoadAllFromDirectoryAsync(string configDirectory, string filePattern, bool allowPartialSuccess);
    public async Task<ConfigToFrameManager[]> CreateManagersAsync(string[] configFileNames);
    public static ConfigDataSet GetSharedConfigData(string configFileName);
    public static long ReleaseSharedData(string configFileName = null);
}
```

**ConfigurationLoader拡張** (`andon/Infrastructure/Configuration/ConfigurationLoader.cs`):
```csharp
// 新規メソッド
public ConnectionConfig LoadConnectionConfig();
public TimeoutConfig LoadTimeoutConfig();
public int LoadMonitoringIntervalMs();
public SystemResourcesConfig LoadSystemResourcesConfig();
public DataProcessingConfig LoadDataProcessingConfig();
public LoggingConfig LoadLoggingConfig();
public DataTransferConfig LoadDataTransferConfig();
```

**InitializationResult実装** (`andon/Core/Models/InitializationResult.cs`):
```csharp
public class InitializationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan LoadTime { get; init; }
    public string ConfigFilePath { get; init; } = string.Empty;
    public Dictionary<string, object> ConfigData { get; init; } = new();
}
```

**IConfigToFrameManager実装** (`andon/Core/Interfaces/IConfigToFrameManager.cs`):
```csharp
public interface IConfigToFrameManager
{
    Task<InitializationResult> LoadConfigAsync(string configFileName = "appsettings.json");
    T GetConfig<T>(string configFileName = null) where T : class;
    byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config);
    string BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig config);
}
```

**ErrorMessages拡張** (`andon/Core/Constants/ErrorMessages.cs`):
```csharp
// 設定ファイル関連（追加）
public const string ConfigFileNotFound = "設定ファイルが見つかりません: {0}";
public const string ConfigFileInvalid = "設定ファイルの形式が不正です: {0}";
public const string ConfigValueMissing = "必須設定項目が見つかりません: {0}";
public const string ConfigValueInvalid = "設定値が不正です: {0} - {1}";

// 複数設定ファイル関連（追加）
public const string MultiConfigLoadFailed = "{0}個の設定ファイル読み込みに失敗しました";
public const string MultiConfigPartialSuccess = "{0}個の設定ファイル読み込みに成功、{1}個が失敗しました";
```

---

### 15.4 既存実装を活用したStep1実装例

#### 実装例1: ConfigToFrameManager.LoadConfigAsync()

```csharp
public async Task<InitializationResult> LoadConfigAsync(string configFileName = "appsettings.json")
{
    var startTime = DateTime.UtcNow;

    try
    {
        // 1. パス解決
        var configPath = ResolveConfigPath(configFileName);
        _actualConfigPath = configPath;

        // 2. IConfiguration構築
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();

        // 3. ConfigurationLoaderインスタンス生成
        var loader = new ConfigurationLoader(configuration);

        // 4. 各設定セクション読み込み（既存実装 + 新規実装を活用）
        _targetDeviceConfig = loader.LoadPlcConnectionConfig(); // ✅ 既存実装
        _connectionConfig = loader.LoadConnectionConfig();       // ❌ 新規実装
        _timeoutConfig = loader.LoadTimeoutConfig();             // ❌ 新規実装
        _monitoringIntervalMs = loader.LoadMonitoringIntervalMs(); // ❌ 新規実装
        // ... その他の設定

        // 5. 設定検証
        var validationResult = ValidateConfig(_connectionConfig, _timeoutConfig, _targetDeviceConfig);
        if (!validationResult.IsValid)
        {
            return new InitializationResult
            {
                Success = false,
                ErrorMessage = string.Join(", ", validationResult.ErrorMessages),
                LoadTime = DateTime.UtcNow - startTime,
                ConfigFilePath = configPath
            };
        }

        // 6. フレーム構築テスト（既存実装を活用）
        var testFrame = BuildReadRandomFrameFromConfig(_targetDeviceConfig); // ✅ 既存実装

        // 7. InitializationResult生成
        return new InitializationResult
        {
            Success = true,
            LoadTime = DateTime.UtcNow - startTime,
            ConfigFilePath = configPath,
            ConfigData = new Dictionary<string, object>
            {
                { "ConnectionConfig", _connectionConfig },
                { "TimeoutConfig", _timeoutConfig },
                { "TargetDeviceConfig", _targetDeviceConfig },
                // ... その他の設定
            }
        };
    }
    catch (Exception ex)
    {
        return new InitializationResult
        {
            Success = false,
            ErrorMessage = ex.Message,
            LoadTime = DateTime.UtcNow - startTime,
            ConfigFilePath = configFileName
        };
    }
}
```

---

#### 実装例2: ConfigurationLoader.LoadConnectionConfig()（新規実装）

```csharp
public ConnectionConfig LoadConnectionConfig()
{
    var section = _configuration.GetSection("PlcCommunication:Connection");

    var ipAddress = section["IpAddress"] ?? throw new InvalidOperationException(
        string.Format(ErrorMessages.ConfigValueMissing, "PlcCommunication:Connection:IpAddress"));

    var portStr = section["Port"] ?? throw new InvalidOperationException(
        string.Format(ErrorMessages.ConfigValueMissing, "PlcCommunication:Connection:Port"));

    if (!int.TryParse(portStr, out var port))
    {
        throw new InvalidOperationException(
            string.Format(ErrorMessages.ConfigValueInvalid, "Port", portStr));
    }

    var useTcp = bool.Parse(section["UseTcp"] ?? "false");
    var isBinary = bool.Parse(section["IsBinary"] ?? "false");
    var frameVersionStr = section["FrameVersion"] ?? "4E";

    if (!Enum.TryParse<FrameVersion>(frameVersionStr, out var frameVersion))
    {
        throw new InvalidOperationException(
            string.Format(ErrorMessages.ConfigValueInvalid, "FrameVersion", frameVersionStr));
    }

    return new ConnectionConfig
    {
        IpAddress = ipAddress,
        Port = port,
        UseTcp = useTcp,
        IsBinary = isBinary,
        FrameVersion = frameVersion
    };
}
```

---

### 15.5 実装時の注意事項

#### A. 既存実装との整合性

1. **DeviceEntry → DeviceSpecification変換**
   - ✅ `DeviceEntry.ToDeviceSpecification()`は既に実装済み
   - ConfigToFrameManagerでの使用例は既に実装済み（`BuildReadRandomFrameFromConfig()`内）

2. **FrameVersion型の使用**
   - ⚠️ `ConnectionConfig.FrameVersion`は`FrameVersion` enum型
   - ⚠️ `TargetDeviceConfig.FrameType`は`string`型（"3E" or "4E"）
   - 変換が必要: `FrameVersion.Frame3E` ↔ `"3E"`

3. **appsettings.json vs appsettings.xlsx**
   - 設計書では`.xlsx`だが、現在の実装では`.json`を使用
   - Step1実装では`.json`形式をサポート（`.xlsx`は将来拡張）

---

#### B. 依存関係の追加

**必要なNuGetパッケージ**:
```xml
<PackageReference Include="Microsoft.Extensions.Configuration" Version="7.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="7.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="7.0.0" />
```

---

#### C. テスト実装時の注意

**既存実装のモック化**:
```csharp
// ConfigurationLoaderのモック作成例
public class MockConfigurationLoader
{
    public TargetDeviceConfig LoadPlcConnectionConfig()
    {
        // ✅ 既存実装と同じ戻り値構造を返す
        return new TargetDeviceConfig
        {
            Devices = new List<DeviceEntry>
            {
                new DeviceEntry { DeviceType = "M", DeviceNumber = 0 },
                new DeviceEntry { DeviceType = "D", DeviceNumber = 0 }
            },
            FrameType = "4E",
            Timeout = 32
        };
    }
}
```

---

### 15.6 実装完了後の統合確認

#### 統合テストチェックリスト

- [ ] ConfigToFrameManager.LoadConfigAsync()が既存の`BuildReadRandomFrameFromConfig()`を正常に呼び出せる
- [ ] ConfigurationLoader拡張メソッドがappsettings.jsonから全設定を読み込める
- [ ] DeviceEntry → DeviceSpecification変換が既存実装と整合性がある
- [ ] FrameVersion型とstring型の変換が正常に動作する
- [ ] 既存のSlmpFrameBuilderメソッドが新規実装から正常に呼び出せる
- [ ] エラーメッセージが既存のErrorMessagesクラスと統一されている

---

以上
