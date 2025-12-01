# Step1: 設定ファイル読み込み

## 機能概要

設定ファイルの読み込みと管理（複数設定ファイル対応）

## 詳細機能

### 複数設定ファイル管理機能
- データ取得元：外部設定ファイル群（*_settings.xlsx, UTF-8形式）
- アーキテクチャ：共有データ + 軽量インスタンス

### 単一設定ファイル読み込み機能（既存機能保持）
- データ取得元：外部設定ファイル（appsettings.xlsx, UTF-8形式）

### 設定値の読み込みと保持
- データ取得元：ConfigToFrameManager.LoadConfigAsync()（接続設定・デバイス設定）

---

## クラス：ConfigToFrameManager

### LoadConfigAsync（Step1: 単一設定ファイル読み込み）

**機能：** 単一設定ファイルの読み込み・既存機能保持

**Input:**
- configFileName（string型、デフォルト："appsettings.xlsx"）
- 設定ソース（appsettings.xlsx, UTF-8：外部設定ファイルから取得）

**Output:**
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
- TimeoutConfig（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs, RetryTimeoutMs）
- TargetDeviceConfig（MDeviceRange, DDeviceRange）
- MonitoringIntervalMs（データ収集周期）
- SystemResourcesConfig（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, LowMemoryMode）
- DataProcessingConfig（TargetName, ContinuousDataMode, DataRetentionDays）
- LoggingConfig（ConsoleOutput, DetailedLog）
- DataTransferConfig（EnableTransfer, DestinationIpAddress, DestinationPort）
- ActualConfigPath（string型、実際に読み込んだファイルパス）

**データ取得元：** 外部設定ファイル（configFileName指定, UTF-8形式）

**パス解決順序：** ./config/[fileName] → ./[fileName] → 環境変数ANDON_CONFIG_PATH

**成功条件:**
- 設定ファイルに記載されている値を正確に取得できること
- 必要な値が設定されていない場合はエラーを返すこと

---

### LoadAllConfigsAsync（複数設定ファイル読み込み）

**機能：** 複数設定ファイルの一括読み込み

**Input:**
- configDirectory（string型、デフォルト："./config/"）
- filePattern（string型、デフォルト："*_settings.xlsx"）

**Output:**
- Dictionary<string, ConfigData>（ファイル名キーの設定データ辞書）
- LoadedConfigPaths（string[]型、実際に読み込んだファイルパス一覧）

**データ取得元：** 指定ディレクトリ内の複数設定ファイル（*_settings.xlsx, UTF-8形式）

**処理方式：** 共有データ領域への一括読み込み

---

### GetConfig（設定内容取得）

**機能：** 単一インスタンス用設定取得

**Input:**
- 設定タイプ指定（ジェネリック型パラメータT、または設定種別指定）
- configFileName（string型、オプション：軽量インスタンス用）

**Output:**
指定設定オブジェクト（LoadConfigAsync/共有データから取得）
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion）
- TimeoutConfig（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs, RetryTimeoutMs）
- TargetDeviceConfig（MDeviceRange, DDeviceRange）
- MonitoringIntervalMs（データ収集周期）
- SystemResourcesConfig（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, LowMemoryMode）
- DataProcessingConfig（TargetName, ContinuousDataMode, DataRetentionDays）
- LoggingConfig（ConsoleOutput, DetailedLog）
- DataTransferConfig（EnableTransfer, DestinationIpAddress, DestinationPort）

**データ取得元：** ConfigToFrameManager.LoadConfigAsync()（単一）/ SharedConfigData（複数）

---

## クラス：MultiConfigManager

### 機能概要
複数設定ファイルの統合管理、共有データ + 軽量インスタンス アーキテクチャ実装

### LoadAllFromDirectoryAsync（複数設定ファイル一括読み込み）

**Input:**
- configDirectory（string型、デフォルト："./config/"）
- filePattern（string型、デフォルト："*_settings.xlsx"）
- allowPartialSuccess（bool型、デフォルト：true）

**Output:**
- Dictionary<string, ConfigToFrameManager>（ファイル名キーの軽量インスタンス辞書）
- LoadResult（LoadedFiles, FailedFiles, TotalLoadTime）

**データ取得元：** 指定ディレクトリ内の複数設定ファイル（*_settings.xlsx, UTF-8形式）

**処理方式：** 設定データを静的共有領域に保存、各ファイル用の軽量インスタンス生成

---

### CreateManagersAsync（軽量インスタンス生成）

**Input:**
- configFileNames（string[]型：対象設定ファイル名一覧）

**Output:**
- ConfigToFrameManager[]（軽量インスタンス配列）

**データ取得元：** SharedConfigData（静的共有領域）

**動作：** 共有データを参照する軽量インスタンスを効率的に生成

---

### GetSharedConfigData（共有データアクセス）

**Input:**
- configFileName（string型：対象設定ファイル名）

**Output:**
- ConfigDataSet（指定ファイルの設定データ）

**データ取得元：** SharedConfigData（静的共有領域）

**メモリ効率：** 設定データ実体の共有によりメモリ使用量最小化

---

### ReleaseSharedData（共有データ解放）

**Input:**
- configFileName（string型、オプション：特定ファイル or 全体）

**Output:**
- ReleasedMemoryKB（解放されたメモリ量）

**動作：** 明示的なメモリ解放（長時間稼働時の最適化）

---

## 設計方針

### アーキテクチャ
- 共有データ領域を活用した効率的なメモリ管理
- 軽量インスタンスによる複数PLC並行処理対応
- 単一設定ファイルモードとの完全互換性保持

### 拡張性
- 新しい設定項目の追加が容易
- 設定ファイル形式の柔軟な変更対応
- 将来的な設定変更監視機能の追加に備えた設計

### 保守性
- 設定読み込みロジックの一元管理
- 明確なエラーハンドリング
- 詳細なログ出力による問題追跡

---

## Step1の実行フロー

```
起動時（1回のみ実行）
    ↓
設定ファイル読み込み
    ↓
各種マネージャークラスのインスタンス作成
    ↓
初期化処理完了
    ↓
Step2以降の処理へ
```

## 成功条件

- 設定ファイルに記載されている値を正確に取得できること
- 必要な値が設定されていない場合はエラーを返すこと
- Step1で作成したインスタンスが各サイクル（Step2-7）で正常に動作すること
