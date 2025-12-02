# appsettings.json 利用状況調査結果

**調査日**: 2025-12-02
**最終更新**: 2025-12-02（起動フロー追跡調査完了、削除予定項目の明確化、Excel設定利用状況詳細調査追加、ハードコード置き換え対応完了状況追加）
**目的**: appsettings.json廃止に向けた現在の利用状況の把握

---

## ⚠️ 重要：ハードコード置き換え対応の完了状況（2025-11-28完了）

### Phase 1-5完了（実機テスト準備完了）
**詳細文書**: `documents/design/ハードコード実装置き換え対応/実装結果/`

#### ✅ 完了事項（Phase 1-5全体）

**Phase 1: DefaultValues.cs（既定値定義）**
- 6個の既定値定数を定義
- ConnectionMethod="UDP", FrameVersion="4E", TimeoutMs=1000, TimeoutSlmp=4, IsBinary=true, MonitoringIntervalMs=1000

**Phase 2: ConfigurationLoaderExcel拡張（設定読み込み）**
- PlcConfiguration拡張完了（7プロパティ追加）
  - FrameVersion（デフォルト: "4E"）
  - Timeout（デフォルト: 1000ms）
  - IsBinary（デフォルト: true）
  - ConnectionMethod（デフォルト: "UDP", Excel settingsシート B10セルから読み込み）
  - MonitoringIntervalMs（デフォルト: 1000ms, Excel settingsシート B11セルから読み込み）
  - PlcId（自動生成: "{IPAddress}_{Port}"）
  - PlcName（Excel settingsシート B15セルから読み込み、空の場合PlcIdを使用）
- ReadOptionalCell<T>()メソッド追加（空セル時に既定値を返す）

**Phase 3: SettingsValidator.cs（検証ロジック）**
- 6つの検証メソッド実装（IPAddress, Port, ConnectionMethod, FrameVersion, Timeout, MonitoringIntervalMs）
- 全40テストケース成功

**Phase 4: ConfigToFrameManagerのハードコード削除**
- frameType: "4E" → config.FrameVersion
- timeout: 32 → ConvertTimeoutMsToSlmpUnit(config.Timeout)
- タイムアウト変換ロジック実装（ミリ秒→SLMP単位）

**Phase 5: 統合テスト**
- 9個の統合テストケース全成功
- Phase 1-4の統合確認完了

**テスト結果**
- 全体テスト: 803/804成功（1個はタイミング関連でハードコード対応無関係）
- ビルド: 成功（0エラー）

#### 📌 本調査への影響
- **MonitoringIntervalMsは既にExcel読み込み完了**
  - ConfigurationLoaderExcel.cs:115で実装済み（Phase 2完了）
  - PlcConfiguration.MonitoringIntervalMsに格納済み（既定値: 1000ms）
  - ⚠️ ExecutionOrchestrator.csでの使用箇所のみ修正が必要（小規模修正）

- **実機テストは現在実施可能**
  - ハードコード完全削除により、Excel設定がそのまま使用される
  - appsettings.json廃止後も問題なし

---

## 調査概要

現在実装されているコードを解析し、appsettings.json内に記載されている項目のうち、実際に使用されている項目と使用されていない項目を特定しました。

### 調査方法
1. **静的解析**: コード内での参照箇所を検索
2. **起動フロー追跡**: Program.cs → AndonHostedService → ApplicationController → ExecutionOrchestratorの実行フローを追跡し、実際に値が読み込まれて使用されるかを確認

### 重要な発見
- **DIコンテナに登録されているが、実際には本番コードで使用されていないクラスが存在**
- ConfigurationLoader（appsettings.json読み込み）は本番環境で使用されず、テストコードでのみ使用
- 本番環境ではConfigurationLoaderExcel（Excel設定読み込み）を使用
- **ハードコード置き換え対応（Phase 1-5）完了により、MonitoringIntervalMsのExcel読み込みは既に実装済み**

---

## ⚠️ 重要な注意事項

### LoggingとLoggingConfigは完全に別物

appsettings.jsonには**2つの異なるログ関連セクション**が存在します：

1. **`LoggingConfig`セクション** - **本番環境で使用中**
   - DI登録あり（DependencyInjectionConfigurator.cs:32）
   - LoggingManager.csで全7項目を使用
   - 対応方針: ハードコード化へ移行

2. **`Logging`セクション** - **完全に未使用（削除予定）**
   - DIバインドなし
   - コード内で完全に未参照
   - Excel設定とも無関係
   - 対応方針: 即座に削除可能

**注意**: この2つは名前が似ていますが、設計段階で計画された別々のログシステムです。現在は`LoggingConfig`のみが実装されています。

---

## DI登録と本番利用の組み合わせによる分類

appsettings.jsonの各項目を「DI登録されているか」「本番環境で利用されているか」の2軸で分類しました。

### パターンA: DI登録 ○ & 本番利用 ○ → **本番環境で必須**

| セクション | 項目 | DIバインド箇所 | 本番利用箇所 | 対応方針 |
|-----------|------|---------------|-------------|---------|
| **LoggingConfig** | LogLevel | DependencyInjectionConfigurator.cs:32<br>`services.Configure<LoggingConfig>` | LoggingManager.cs:39,47<br>コンストラクタで受け取り、ログレベル制御に使用 | **ハードコード化** - "Debug"固定 |
| **LoggingConfig** | EnableFileOutput | 同上 | LoggingManager.cs:49,99<br>ファイル出力ON/OFF判定 | **ハードコード化** - true固定 |
| **LoggingConfig** | EnableConsoleOutput | 同上 | LoggingManager.cs:220,232,244,265,303,321,337,354<br>コンソール出力ON/OFF判定 | **ハードコード化** - true固定 |
| **LoggingConfig** | LogFilePath | 同上 | LoggingManager.cs:72,79,124,137,161,162,181,183,186,190,203<br>ファイル作成・ローテーション | **ハードコード化** - "./logs"固定 |
| **LoggingConfig** | MaxLogFileSizeMb | 同上 | LoggingManager.cs:138<br>ローテーション判定 | **ハードコード化** - 1MB固定 |
| **LoggingConfig** | MaxLogFileCount | 同上 | LoggingManager.cs:159,166<br>ローテーション処理 | **ハードコード化** - 10ファイル固定 |
| **LoggingConfig** | EnableDateBasedRotation | 同上 | LoggingManager.cs:130<br>ファイル名決定 | **ハードコード化** - true固定 |
| **PlcCommunication** | MonitoringIntervalMs | DependencyInjectionConfigurator.cs:30<br>`services.Configure<DataProcessingConfig>` | ExecutionOrchestrator.cs:74<br>GetMonitoringInterval()でタイマー間隔として使用 | **Excel設定へ移行** - 各PLC個別設定 |

**合計**: 8項目（LoggingConfig 7項目 + MonitoringIntervalMs 1項目）

---

### パターンB: DI登録 ○ & 本番利用 × → **削除検討対象**

| セクション | 項目 | DIバインド箇所 | 本番未使用の理由 | 対応方針 |
|-----------|------|---------------|-----------------|---------|
| **SystemResources** | MaxMemoryUsageMb | DependencyInjectionConfigurator.cs:31<br>`services.Configure<SystemResourcesConfig>` | ResourceManagerがDI登録されているが（L38）、ApplicationControllerやExecutionOrchestratorから呼ばれない。テストコードでのみ使用 | **ハードコード化** - 一接続当たり500KB未満に設定（本番利用するためコード修正） |
| **SystemResources** | MaxConcurrentConnections | 同上 | 設計段階で計画されたが未実装。接続数制限処理がどこにも実装されていない | **削除予定** - 未実装機能のため不要 |
| **SystemResources** | MaxLogFileSizeMb | 同上 | LoggingConfig.MaxLogFileSizeMbと機能が重複。SystemResources側は定義のみで未実装 | **即座に削除可** - LoggingConfigと機能重複 |

**合計**: 3項目

**注意**: SystemResourcesConfig自体がDI登録されているため、これらの項目もバインドされますが、実際には使用されていません。

---

### パターンC: DI登録 × & 本番利用 ○ → **論理的にあり得ない**

該当する項目はありません。

**理由**: DI登録されていない項目は、本番コードで使用することができません（IOptions<T>経由での取得ができないため）。

---

### パターンD: DI登録 × & 本番利用 × → **即座に削除可能**

| セクション | 項目 | 未使用の理由 | 対応方針 |
|-----------|------|-------------|---------|
| **PlcCommunication.Connection** | IpAddress | ConfigurationLoaderがDI登録されておらず、テストでのみ使用。本番はConfigurationLoaderExcel。Excel機能とは独立しており削除しても影響なし | **削除予定** |
| **PlcCommunication.Connection** | Port | 同上 | **削除予定** |
| **PlcCommunication.Connection** | UseTcp | 同上 | **削除予定** |
| **PlcCommunication.Connection** | IsBinary | 同上 | **削除予定** |
| **PlcCommunication.Connection** | FrameVersion | 同上（テストではConfigurationLoader経由で使用） | **削除予定** |
| **PlcCommunication.Timeouts** | ConnectTimeoutMs | コード内で完全に未参照。Excel設定にはタイムアウト項目が存在しないため削除しても影響なし | **削除予定** |
| **PlcCommunication.Timeouts** | SendTimeoutMs | 同上 | **削除予定** |
| **PlcCommunication.Timeouts** | ReceiveTimeoutMs | ConfigurationLoaderがテストでのみ使用。Excel設定にはタイムアウト項目が存在しないため削除しても影響なし | **削除予定** |
| **PlcCommunication.TargetDevices** | Devices | ConfigurationLoaderがテストでのみ使用。本番はConfigurationLoaderExcel（データ収集デバイスシート）。Excel機能とは独立しており削除しても影響なし | **削除予定** |
| **PlcCommunication.DataProcessing** | BitExpansion.* (全項目) | 実装されていない | **削除予定** |
| **SystemResources** | MemoryLimitKB | SystemResourcesConfigにプロパティ定義が存在しない（appsettings.jsonに値があっても読み込む先がない）。コード内で完全に未参照 | **削除予定** - 実装なし |
| **SystemResources** | MaxBufferSize | 同上 | **削除予定** |
| **SystemResources** | MemoryThresholdKB | 同上 | **削除予定** |
| **Logging** | ConsoleOutput.FilePath | コード内で完全に未参照。DIバインドなし。Excel設定とも無関係 | **削除予定** |
| **Logging** | ConsoleOutput.MaxFileSizeMB | 同上 | **削除予定** |
| **Logging** | ConsoleOutput.MaxFileCount | 同上 | **削除予定** |
| **Logging** | ConsoleOutput.FlushIntervalMs | 同上 | **削除予定** |
| **Logging** | DetailedLog.FilePath | 同上 | **削除予定** |
| **Logging** | DetailedLog.MaxFileSizeMB | 同上 | **削除予定** |
| **Logging** | DetailedLog.RetentionDays | 同上 | **削除予定** |

**合計**: 25項目以上（Loggingセクション詳細化により増加）

**重要**:
- `LoggingConfig`セクション（本番使用中）と`Logging`セクション（未使用）は**完全に別物**です
- `Logging`セクションはDIバインドなし、実装なし、Excel設定とも無関係で、**即座に削除可能**

---

## 分類サマリー

| パターン | DI登録 | 本番利用 | 項目数 | 対応方針 |
|---------|-------|---------|--------|---------|
| **A** | ○ | ○ | **8項目** | **移行必須** - 代替手段の実装が必要 |
| **B** | ○ | × | **3項目** | **削除検討** - DI登録の削除が必要 |
| **C** | × | ○ | **0項目** | 該当なし |
| **D** | × | × | **25項目以上** | **即座に削除可** - 影響なし（Excel設定とも無関係） |

### 重要な発見
1. **本番環境で実際に使用されているのは8項目のみ**
   - LoggingConfig 7項目（⚠️ Loggingセクションとは別物）
   - MonitoringIntervalMs 1項目

2. **appsettings.jsonの項目の大半（約75%以上）が未使用**
   - パターンD（即座に削除可）が25項目以上
     - うち**Loggingセクション全体（7項目）が削除予定**
   - パターンB（削除検討）が3項目
   - 合計28項目以上が本番で未使用

3. **DIに登録されていても本番で使用されていない項目が存在**
   - SystemResourcesConfigの一部項目
   - ResourceManager自体が本番未使用

---

## 移行方針の決定（2025-12-02 更新）

### 決定事項

#### LoggingConfig 全7項目 → **ハードコード化**

| 項目 | ハードコード値 | 理由 |
|------|-------------|------|
| LogLevel | "Information" | 本番環境で固定で問題ない |
| EnableFileOutput | true | ログファイル出力は常に有効 |
| EnableConsoleOutput | true | コンソール出力は常に有効 |
| LogFilePath | "./logs" | 実行ファイルと同じディレクトリ配下 |
| MaxLogFileSizeMb | 1 | 1MBでローテーション |
| MaxLogFileCount | 10 | 最大10ファイル保持 |
| EnableDateBasedRotation | true | 日付ベースローテーション有効 |

**実装方針**:
- `LoggingManager.cs` 内で直接定数として定義
- `IOptions<LoggingConfig>`依存を削除
- `LoggingConfig.cs` モデルを削除

**メリット**:
- 設定ファイル不要で管理が容易
- デプロイ時の設定漏れがない
- パフォーマンス向上（設定読み込みオーバーヘッドなし）

---

#### MonitoringIntervalMs → **Excel設定へ移行**（✅ 読み込み実装完了、使用箇所のみ修正）

| 項目 | 移行先 | 理由 |
|------|--------|------|
| MonitoringIntervalMs | Excel設定 settingsシート B11セル | ✅ ConfigurationLoaderExcel.cs:115で読み込み実装完了（Phase 2完了）、既定値1000ms設定済み、各PLC個別設定可能 |

**実装方針**:
- ✅ **Excel読み込み**: ConfigurationLoaderExcel.cs:115で実装完了（Phase 2完了）
- ✅ **PlcConfigurationへの格納**: PlcConfiguration.MonitoringIntervalMs（既定値: 1000ms）
- ❌ **使用箇所の修正**: ExecutionOrchestrator.cs:75のみ修正が必要
  - `_dataProcessingConfig.Value.MonitoringIntervalMs` → `plcConfig.MonitoringIntervalMs` に変更
- `IOptions<DataProcessingConfig>` 依存を削除
- `DataProcessingConfig.cs` モデルを削除

**メリット**:
- 各PLCごとに異なる監視間隔を設定可能（柔軟性向上）
- 既存のExcel設定管理と統一
- 追加の設定ファイル不要
- **実装工数が大幅削減**（読み込みは完了済み、使用箇所1箇所のみ修正）

---

## 本番環境での実際の使用状況（起動フロー調査結果）

### ✅ 実際に使用されている項目（本番環境）

| カテゴリ | 項目 | 使用箇所（実行フロー） | 移行方針 |
|---------|------|----------------------|---------|
| **ログ設定** | `LoggingConfig.*` 全7項目 | Program.cs → DI → LoggingManager | **ハードコード化** - 固定値で実装 |
| **監視制御** | `PlcCommunication.MonitoringIntervalMs` | ExecutionOrchestrator.GetMonitoringInterval() | **Excel設定へ移行** - settingsシート B11セル（Phase 2で実装済み、既定値: 1000ms） |

### ⚠️ DIに登録されているが本番環境で未使用の項目

| カテゴリ | 項目 | 理由 |
|---------|------|------|
| **リソース管理** | `SystemResources.MaxMemoryUsageMb` | ResourceManagerがDIに登録されているが、本番コードから呼ばれていない（テストのみ）。一接続当たり500KB未満に抑える設計意図あり |
| **PLC接続** | `PlcCommunication.Connection.*` | ConfigurationLoaderが本番環境で使用されない（テストのみ）。本番はConfigurationLoaderExcelを使用 |
| **PLC接続** | `PlcCommunication.Timeouts.*` | 同上 |
| **PLC接続** | `PlcCommunication.TargetDevices.Devices` | 同上 |

---

## 詳細分析: 項目別の使用状況

### 1. PlcCommunication セクション

#### ✅ 本番環境で使用されている項目

| 項目 | 使用箇所 | 詳細 |
|------|----------|------|
| `MonitoringIntervalMs` | ExecutionOrchestrator.cs:74 | IOptions<DataProcessingConfig>経由で読み込み。RunContinuousDataCycleAsync()でタイマー間隔として実際に使用 |

#### ⚠️ テストコードでのみ使用されている項目

| 項目 | 使用箇所 | 理由 |
|------|----------|------|
| `Connection.FrameVersion` | ConfigurationLoader.cs:29 | ConfigurationLoaderが本番環境で未使用 |
| `Timeouts.ReceiveTimeoutMs` | ConfigurationLoader.cs:33 | ConfigurationLoaderが本番環境で未使用 |
| `TargetDevices.Devices` | ConfigurationLoader.cs:37-41 | ConfigurationLoaderが本番環境で未使用 |

#### ❌ 完全に未使用の項目（**削除予定**）

| 項目 | 理由 |
|------|------|
| `Connection.IpAddress`, `Connection.Port`, `Connection.UseTcp`, `Connection.IsBinary` | ConfigurationLoaderがテストでのみ使用。本番はConfigurationLoaderExcel。**Excel機能とは独立しており削除しても影響なし** |
| `Timeouts.ConnectTimeoutMs`, `Timeouts.SendTimeoutMs` | コード内で完全に未参照。**Excel設定にはタイムアウト項目が存在しないため削除しても影響なし** |
| `DataProcessing.BitExpansion.*` | 実装されていない |

---

### 2. SystemResources セクション

#### ⚠️ DIに登録されているが本番環境で未使用

| 項目 | 使用箇所 | 詳細 |
|------|----------|------|
| `MaxMemoryUsageMb` | ResourceManager.cs:52 | IOptions<SystemResourcesConfig>経由で読み込まれるが、ResourceManager自体が本番コードで呼ばれない。一接続当たり500KB未満に抑える設計意図あり |

**起動フロー確認結果**:
- DependencyInjectionConfigurator.cs:38 でDIに登録
- しかし、ApplicationControllerやExecutionOrchestratorから呼ばれていない
- テストコード（ResourceManagerTests.cs）でのみ使用

#### ❌ 完全に未使用の項目

| 項目 | 理由 |
|------|------|
| `MemoryLimitKB` | SystemResourcesConfigで再定義されているが使用されていない |
| `MaxBufferSize` | コード内で参照されていない |
| `MemoryThresholdKB` | コード内で参照されていない |
| `MaxConcurrentConnections` | 設計段階で計画されたが未実装。PLC接続数制限処理がどこにも実装されていない（削除予定） |
| `MaxLogFileSizeMb` | LoggingConfig.MaxLogFileSizeMbと機能が重複。SystemResources側は定義のみで未実装 |

---

### 3. LoggingConfig セクション

#### ✅ 本番環境で使用されている項目（すべて使用中）

| 項目 | 使用箇所 | 詳細 |
|------|----------|------|
| `LogLevel` | LoggingManager.cs:39,47 | IOptions<LoggingConfig>経由。コンストラクタで読み込み、ログレベル制御に使用 |
| `EnableFileOutput` | LoggingManager.cs:49,99 | ファイル出力ON/OFF制御。実行時に判定 |
| `EnableConsoleOutput` | LoggingManager.cs:220,232,244,265,303,321,337,354 | コンソール出力ON/OFF制御。すべてのログ出力メソッドで使用 |
| `LogFilePath` | LoggingManager.cs:72,79,124,137,161,162,181,183,186,190,203 | ログファイルパス指定。ファイル作成・ローテーションで使用 |
| `MaxLogFileSizeMb` | LoggingManager.cs:138 | ログファイルサイズ上限。ローテーション判定に使用 |
| `MaxLogFileCount` | LoggingManager.cs:159,166 | ログファイル世代数。ローテーション処理で使用 |
| `EnableDateBasedRotation` | LoggingManager.cs:130 | 日付ベースローテーションON/OFF。ファイル名決定に使用 |

**起動フロー確認結果**:
- Program.cs → DependencyInjectionConfigurator.cs:32 でIOptions<LoggingConfig>をバインド
- AndonHostedService起動時にLoggingManagerがDIから取得される（Singleton）
- LoggingManagerのコンストラクタでIOptions<LoggingConfig>を受け取り、すべての項目を実際に使用
- 実行中のすべてのログ出力（LogInfo, LogError, LogDebug等）で設定値が参照される

**重要**: LoggingConfigセクションは本番環境で完全に使用されています。

---

### 4. Logging セクション

#### ❌ 完全に未使用の項目（すべて未使用・削除予定）

| 項目 | 理由 | 対応方針 |
|------|------|---------|
| `ConsoleOutput.FilePath` | DIバインドなし。コード内で完全に未参照。Excel設定とも無関係 | **即座に削除可** |
| `ConsoleOutput.MaxFileSizeMB` | 同上 | **即座に削除可** |
| `ConsoleOutput.MaxFileCount` | 同上 | **即座に削除可** |
| `ConsoleOutput.FlushIntervalMs` | 同上 | **即座に削除可** |
| `DetailedLog.FilePath` | 同上 | **即座に削除可** |
| `DetailedLog.MaxFileSizeMB` | 同上 | **即座に削除可** |
| `DetailedLog.RetentionDays` | 同上 | **即座に削除可** |

**実装確認結果**:
- `Logging`セクション全体がDIコンテナに登録されていない
- 対応するモデルクラスも存在しない
- コード検索で参照箇所0件
- Excel設定読み込み機能（ConfigurationLoaderExcel）とも完全に独立

**⚠️ 重要**:
- `LoggingConfig`セクション（使用中）と`Logging`セクション（未使用）は**完全に別物**です
- `Logging`セクション全体を削除しても、本番環境・テスト環境・Excel設定のいずれにも影響ありません

---

## 廃止を検討する際の推奨事項（起動フロー調査結果を反映）

### 1. ✅ 本番環境で必須の項目（移行が必須）

以下の項目は本番環境で実際に使用されているため、代替手段への移行が必要です。

| 優先度 | カテゴリ | 項目 | 移行先 | 影響度 |
|-------|---------|------|--------|--------|
| **最優先** | **ログ設定** | `LoggingConfig.LogLevel` | **ハードコード** - "Information"固定 | **高** - すべてのログ出力に影響 |
| **最優先** | **ログ設定** | `LoggingConfig.EnableFileOutput` | **ハードコード** - true固定 | **高** - すべてのログ出力に影響 |
| **最優先** | **ログ設定** | `LoggingConfig.EnableConsoleOutput` | **ハードコード** - true固定 | **高** - すべてのログ出力に影響 |
| **最優先** | **ログ設定** | `LoggingConfig.LogFilePath` | **ハードコード** - "./logs"固定（実行ファイルと同じディレクトリ） | **高** - すべてのログ出力に影響 |
| **最優先** | **ログ設定** | `LoggingConfig.MaxLogFileSizeMb` | **ハードコード** - 1MB固定 | **高** - ログローテーションに影響 |
| **最優先** | **ログ設定** | `LoggingConfig.MaxLogFileCount` | **ハードコード** - 10ファイル固定 | **高** - ログローテーションに影響 |
| **最優先** | **ログ設定** | `LoggingConfig.EnableDateBasedRotation` | **ハードコード** - true固定 | **高** - ログローテーションに影響 |
| **高** | **監視制御** | `PlcCommunication.MonitoringIntervalMs` | **Excel設定** - 各PLC個別設定（settingsシート B11セル、Phase 2で実装済み、既定値: 1000ms） | **中** - タイマー間隔に影響 |

---

### 2. ⚠️ DIに登録されているが本番環境で未使用の項目（削除可能だがテストコードへの影響を考慮）

以下の項目はDIコンテナに登録され、テストコードでは使用されていますが、本番コードでは使用されていません。

| カテゴリ | 項目 | 状況 | 対応方法 |
|---------|------|------|---------|
| **リソース管理** | `SystemResources.MaxMemoryUsageMb` | ResourceManagerがDI登録されているが本番未使用。一接続当たり500KB未満に抑える設計意図あり | ResourceManagerを本番で使用する予定があれば残す（一接続当たりメモリ管理機能）。なければDI登録とSystemResourcesConfigを削除 |
| **PLC接続** | `PlcCommunication.Connection.*` | ConfigurationLoaderがテストのみで使用 | ConfigurationLoaderをテスト専用にし、appsettings.jsonの該当項目を削除 |
| **PLC接続** | `PlcCommunication.Timeouts.*` (ReceiveTimeoutMs以外) | 同上 | 同上 |
| **PLC接続** | `PlcCommunication.TargetDevices.Devices` | 同上 | 同上 |

**重要**: これらの項目は削除しても本番環境に影響ありませんが、テストコードの修正が必要になる場合があります。

---

### 3. ❌ 完全に削除可能な項目（影響なし）

以下の項目は本番・テストいずれのコードでも使用されていないため、即座に削除可能です。

#### PlcCommunication セクション
- `Connection.IpAddress`, `Connection.Port`, `Connection.UseTcp`, `Connection.IsBinary` → Excel設定（ConfigurationLoaderExcel）で代替済み
- `Timeouts.ConnectTimeoutMs`, `Timeouts.SendTimeoutMs` → コード内で完全に未参照
- `DataProcessing.BitExpansion.*` → 実装されていない

#### SystemResources セクション
- `MemoryLimitKB`, `MaxBufferSize`, `MemoryThresholdKB` → コード内で完全に未参照
- `MaxConcurrentConnections` → 未実装機能（PLC接続数制限処理が実装されていない）のため削除予定

#### Logging セクション（⚠️ LoggingConfigとは別物）
- `ConsoleOutput.FilePath`, `ConsoleOutput.MaxFileSizeMB`, `ConsoleOutput.MaxFileCount`, `ConsoleOutput.FlushIntervalMs` → DIバインドなし、コード内で完全に未参照、Excel設定とも無関係
- `DetailedLog.FilePath`, `DetailedLog.MaxFileSizeMB`, `DetailedLog.RetentionDays` → 同上
- **⚠️ 注意**: `Logging`セクション全体を削除しても、`LoggingConfig`（本番使用中）には影響なし

---

## TDD準拠の移行計画（Red-Green-Refactorサイクル適用）

### TDD実装の基本原則
全てのフェーズで以下のサイクルを厳守します：
1. **Red**: 失敗するテストを先に書く
2. **Green**: テストを通すための最小限のコードを実装
3. **Refactor**: 動作を保ったままコードを改善

---

### フェーズ0: 即座に削除可能項目の削除（影響なし）

#### 対象項目
- `PlcCommunication.Connection` 全体（IpAddress, Port, UseTcp, IsBinary, FrameVersion）※Excel機能とは独立
- `PlcCommunication.Timeouts` 全体（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs）※Excel設定にはタイムアウト項目なし
- `PlcCommunication.TargetDevices.Devices` ※Excel機能（データ収集デバイスシート）とは独立
- `PlcCommunication.DataProcessing.BitExpansion` 全体
- `SystemResources` セクション全体（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, MaxConcurrentConnections）
- **`Logging` セクション全体（ConsoleOutput.*, DetailedLog.*）** ※⚠️ `LoggingConfig`とは完全に別物

#### TDDサイクル: フェーズ0

##### Step 0-1: 既存テストの確認（Red）
```
目的: 削除前に既存機能が正常動作することを確認

テストケース:
1. ConfigurationLoaderExcelTests.cs - Excel設定読み込みが正常動作することを確認
2. LoggingManagerTests.cs - LoggingConfig（本番使用中）が正常動作することを確認
3. ExecutionOrchestratorTests.cs - MonitoringIntervalMsが正常動作することを確認

期待される結果: 全テストがパス（Green状態）

実施コマンド:
dotnet test --filter "FullyQualifiedName~ConfigurationLoaderExcel"
dotnet test --filter "FullyQualifiedName~LoggingManager"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"
```

##### Step 0-2: 削除後の動作確認テスト作成（Red）
```
目的: 削除後も既存機能が影響を受けないことを事前に確認するテスト

テストケース名: Phase0_UnusedItemsDeletion_NoImpactTests.cs

1. test_Excel設定読み込み_appsettings削除後も動作()
   - ConfigurationLoaderExcelがExcel設定のみに依存していることを確認
   - appsettings.jsonの該当項目が存在しなくてもエラーにならないことを確認

2. test_LoggingConfig_Loggingセクション削除後も動作()
   - LoggingConfigセクションとLoggingセクションが独立していることを確認
   - Loggingセクション削除後もLoggingManagerが正常動作することを確認

3. test_本番フロー_未使用項目削除後も動作()
   - ApplicationController → ExecutionOrchestrator の実行フローが正常動作
   - 削除対象項目が本番フローで参照されていないことを確認

期待される結果: 全テストが失敗（appsettings.jsonに項目が存在するため）→ 削除後にパス
```

##### Step 0-3: 実装（Green）
```
作業内容:
1. appsettings.jsonから削除対象項目を削除
2. テスト再実行 → 全テストがパス

確認コマンド:
dotnet test --filter "FullyQualifiedName~Phase0"
```

##### Step 0-4: リファクタリング（Refactor）
```
作業内容:
1. ConfigurationLoader.cs のコメント更新（テスト専用クラスであることを明記）
2. 不要なusingディレクティブの削除
3. コード整形

確認: テスト再実行 → 全テストがパス
```

**影響**: 完全になし（本番・テスト・Excel機能すべてに影響なし）

---

### フェーズ1: テスト専用項目の整理

#### 判断が必要な項目
1. **ResourceManager** - 本番で使用する予定がある？（一接続当たり500KB未満に抑えるメモリ管理機能）
2. **ConfigurationLoader** - テストで引き続き使用する？

**推奨**: 両方とも本番未使用のため削除推奨

#### TDDサイクル: フェーズ1

##### Step 1-1: 削除影響範囲の特定テスト作成（Red）
```
目的: 削除対象クラスの依存関係を洗い出す

テストケース名: Phase1_TestOnlyClasses_DependencyTests.cs

1. test_ResourceManager_本番フローで未使用()
   - ApplicationController, ExecutionOrchestratorからResourceManagerが呼ばれていないことを確認
   - Mock注入テストでResourceManagerが本番フローに含まれないことを確認

2. test_ConfigurationLoader_本番フローで未使用()
   - 本番環境でConfigurationLoaderExcelのみが使用されることを確認
   - ConfigurationLoaderがテストコードでのみ使用されることを確認

期待される結果: 全テストがパス（本番未使用であることを確認）
```

##### Step 1-2: 削除後のテストコード修正（Green）
```
作業内容:
1. ResourceManagerTests.cs を削除 or インメモリ設定に変更
2. ConfigurationLoaderTests.cs を削除 or モック使用に変更
3. DependencyInjectionConfigurator.cs からResourceManagerのDI登録を削除
4. SystemResourcesConfig.cs を削除
5. テスト実行 → 全テストがパス

確認コマンド:
dotnet test --filter "FullyQualifiedName~Phase1"
```

##### Step 1-3: リファクタリング（Refactor）
```
作業内容:
1. 不要なusingディレクティブの削除
2. DI設定のコメント更新
3. テスト再実行 → 全テストがパス
```

**注意**:
- SystemResourcesConfig内の`MaxConcurrentConnections`は未実装機能のため削除対象
- SystemResourcesConfig内の`MaxLogFileSizeMb`は`LoggingConfig.MaxLogFileSizeMb`と機能重複のため削除対象

---

### フェーズ2: 本番環境で必須の項目の移行（最優先）

#### 2-1. LoggingConfig のハードコード化

**移行先**: ハードコード化（固定値）
**影響度**: 高 - すべてのログ出力に影響

##### Step 2-1-1: ハードコード化後の動作確認テスト作成（Red）
```
目的: ハードコード化後も既存のログ機能が正常動作することを確認

テストケース名: Phase2_1_LoggingConfig_HardcodingTests.cs

1. test_LoggingManager_ハードコード値でログ出力成功()
   - IOptions<LoggingConfig>依存を削除した後のコンストラクタが正常動作
   - ハードコード値（LogLevel="Information", EnableFileOutput=true等）が使用される
   - 期待される結果: ログファイルが"./logs"に出力される

2. test_LoggingManager_ファイルローテーション動作()
   - MaxLogFileSizeMb=1, MaxLogFileCount=10 の固定値でローテーション動作
   - 期待される結果: 1MB超過時にローテーション、最大10ファイル保持

3. test_LoggingManager_コンソール出力動作()
   - EnableConsoleOutput=true の固定値でコンソール出力動作
   - 期待される結果: すべてのログレベルでコンソール出力される

4. test_LoggingManager_境界値テスト()
   - ログファイルサイズが0バイト、1MB-1バイト、1MB、1MB+1バイトの境界でのローテーション動作
   - ログファイル数が0、9、10、11ファイルの境界での動作

期待される結果: Step 2-1-2の実装前は失敗（IOptions依存があるため）
```

##### Step 2-1-2: 実装（Green）
```
作業内容:
1. LoggingManager.cs を修正
   - private readonly IOptions<LoggingConfig> _loggingConfig; を削除
   - 以下の定数を追加:
     private const string LOG_LEVEL = "Information";
     private const bool ENABLE_FILE_OUTPUT = true;
     private const bool ENABLE_CONSOLE_OUTPUT = true;
     private const string LOG_FILE_PATH = "./logs";
     private const int MAX_LOG_FILE_SIZE_MB = 1;
     private const int MAX_LOG_FILE_COUNT = 10;
     private const bool ENABLE_DATE_BASED_ROTATION = true;
   - 全ての _loggingConfig.Value.* 参照を定数参照に変更

2. DependencyInjectionConfigurator.cs を修正
   - services.Configure<LoggingConfig>(configuration.GetSection("LoggingConfig")); を削除

3. LoggingConfig.cs を削除

4. appsettings.jsonから LoggingConfig セクションを削除

5. テスト実行 → 全テストがパス

確認コマンド:
dotnet test --filter "FullyQualifiedName~Phase2_1"
dotnet test --filter "FullyQualifiedName~LoggingManager"
```

##### Step 2-1-3: リファクタリング（Refactor）
```
作業内容:
1. 定数を private static readonly に変更（必要に応じて）
2. XMLドキュメントコメントの追加
3. 不要なusingディレクティブの削除
4. テスト再実行 → 全テストがパス
```

---

#### 2-2. MonitoringIntervalMs のExcel設定への移行（✅ 読み込み完了、使用箇所のみ修正）

**移行先**: Excel設定（settingsシート B11セル）
**実装状況**: ✅ ConfigurationLoaderExcel.cs:115で読み込み実装完了（Phase 2完了）、既定値1000ms設定済み
**影響度**: 中 - タイマー間隔に影響
**工数**: **小**（使用箇所1箇所のみ修正、読み込みは完了済み）

##### Step 2-2-1: Excel設定値使用の動作確認テスト作成（Red）
```
目的: Excel設定のMonitoringIntervalMsを使用してタイマーが正常動作することを確認

テストケース名: Phase2_2_MonitoringInterval_ExcelMigrationTests.cs

1. test_ExecutionOrchestrator_Excel設定値を直接使用()
   - IOptions<DataProcessingConfig>依存を削除後のコンストラクタが正常動作
   - plcConfig.MonitoringIntervalMsからタイマー間隔を直接取得
   - 期待される結果: Excel設定の値（例: 5000ms）でタイマーが動作
   - 確認: _dataProcessingConfig.Value.MonitoringIntervalMsが使用されないこと

2. test_ExecutionOrchestrator_PLC毎に異なる監視間隔()
   - 複数PLCで異なるMonitoringIntervalMs（例: PLC1=5000ms, PLC2=10000ms）を設定
   - 各PLCが独立した間隔で動作することを確認
   - 期待される結果: PLC1は5秒間隔、PLC2は10秒間隔でデータ取得

3. test_MonitoringInterval_境界値テスト()
   - MonitoringIntervalMs = 1ms（最小値）
   - MonitoringIntervalMs = 3600000ms（最大値、1時間）
   - MonitoringIntervalMs = 0ms（異常値、エラーハンドリング確認）

4. test_GetMonitoringInterval_削除後の互換性()
   - GetMonitoringInterval()メソッドを削除しても既存テストがパスすることを確認
   - または、デフォルト値返却に変更した場合の動作確認

期待される結果: Step 2-2-2の実装前は失敗（IOptions依存があるため）
```

##### Step 2-2-2: 実装（Green）- 簡略化版
```
作業内容:
1. ExecutionOrchestrator.cs を修正（小規模修正）
   - private readonly IOptions<DataProcessingConfig> _dataProcessingConfig; を削除
   - 75行目の修正:
     * 変更前: var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;
     * 変更後: var intervalMs = plcConfig.MonitoringIntervalMs;
   - GetMonitoringInterval() を削除 or デフォルト値返却に変更

   ⚠️ 注意: plcConfigは既にメソッド引数で受け取っているため、追加の変更は不要

2. DependencyInjectionConfigurator.cs を修正
   - services.Configure<DataProcessingConfig>(configuration.GetSection("PlcCommunication")); を削除

3. DataProcessingConfig.cs を削除

4. appsettings.jsonから PlcCommunication.MonitoringIntervalMs を削除

5. テスト実行 → 全テストがパス

確認コマンド:
dotnet test --filter "FullyQualifiedName~Phase2_2"
dotnet test --filter "FullyQualifiedName~ExecutionOrchestrator"

⚠️ 重要: Excel読み込み（ConfigurationLoaderExcel.cs:115）は既に実装完了（Phase 2完了）、
        既定値1000ms設定済み、読み込み処理の追加実装は不要。使用箇所の修正のみで完了。
```

##### Step 2-2-3: リファクタリング（Refactor）
```
作業内容:
1. 各PLCごとの監視間隔処理のヘルパーメソッド抽出（必要に応じて）
2. XMLドキュメントコメントの追加
3. 不要なusingディレクティブの削除
4. テスト再実行 → 全テストがパス
```

---

### フェーズ3: appsettings.json完全廃止

**前提条件**: フェーズ0, 1, 2がすべて完了

##### Step 3-1: 完全廃止後の統合テスト作成（Red）
```
目的: appsettings.json無しで全機能が正常動作することを確認

テストケース名: Phase3_CompleteRemoval_IntegrationTests.cs

1. test_アプリケーション起動_appsettings無し()
   - appsettings.jsonが存在しない状態でアプリケーションが起動
   - 期待される結果: 正常起動、エラーログなし

2. test_PLC通信_appsettings無し()
   - Excel設定のみでPLC通信サイクルが正常動作
   - 期待される結果: Step3-6の全処理が正常実行

3. test_ログ出力_appsettings無し()
   - ハードコード値でログ出力が正常動作
   - 期待される結果: "./logs"にログファイルが出力される

4. test_複数PLC並列実行_appsettings無し()
   - Excel設定の各PLC個別設定で並列実行が正常動作
   - 期待される結果: 各PLCが独立した監視間隔で動作

期待される結果: Step 3-2の実装前は失敗（appsettings.json依存があるため）
```

##### Step 3-2: 実装（Green）
```
作業内容:
1. appsettings.json ファイルを削除（すべての環境から）
2. Program.cs の確認
   - Host.CreateDefaultBuilder(args) はappsettings.json不在でもエラーにならない
   - 不要なIConfiguration依存がないか確認
3. DI設定の最終確認
   - DependencyInjectionConfigurator.cs で不要なconfiguration参照を削除
4. テスト実行 → 全テストがパス

確認コマンド:
dotnet test --filter "FullyQualifiedName~Phase3"
dotnet test  # 全テスト実行
```

##### Step 3-3: リファクタリング（Refactor）
```
作業内容:
1. コメント・ドキュメントの更新
   - README.mdでappsettings.json不要であることを明記
   - 各クラスのXMLコメントで設定方法を更新
2. 不要なNuGetパッケージの削除確認
   - Microsoft.Extensions.Configuration.Json が不要か確認
3. テスト再実行 → 全テストがパス
```

**影響度**: 低 - すべての移行が完了しているため

---

### TDD実装の進め方まとめ

#### 各フェーズでの実装順序（厳守）
1. **テストファースト**: 実装前に必ず失敗するテストを書く
2. **最小実装**: テストを通すための必要最小限のコードのみ実装
3. **テスト確認**: 全テストがパスすることを確認
4. **リファクタリング**: テストがパスした状態でコード品質を改善
5. **テスト再確認**: リファクタリング後も全テストがパスすることを確認

#### 境界値テストの重要性
各フェーズで以下の境界値を必ずテスト:
- ログファイルサイズ: 0バイト, 1MB-1バイト, 1MB, 1MB+1バイト
- ログファイル数: 0, 9, 10, 11ファイル
- MonitoringIntervalMs: 1ms（最小）, 正常値（5000ms）, 3600000ms（最大）, 0ms（異常）

#### テスト可能な設計の原則
- **依存性注入**: IOptions依存の削除でテスト容易性向上
- **純粋関数**: ハードコード値使用で副作用を削減
- **単一責任**: LoggingManager、ExecutionOrchestratorが明確な責任を持つ

#### 必須コマンド
```bash
# フェーズごとのテスト実行
dotnet test --filter "FullyQualifiedName~Phase0"
dotnet test --filter "FullyQualifiedName~Phase1"
dotnet test --filter "FullyQualifiedName~Phase2_1"
dotnet test --filter "FullyQualifiedName~Phase2_2"
dotnet test --filter "FullyQualifiedName~Phase3"

# 全テスト実行
dotnet test

# カバレッジ確認（オプション）
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

---

## 実装への影響評価（起動フロー調査に基づく）

### 影響度: 高（本番環境に直接影響）
| 項目 | 影響範囲 | 対応工数 |
|------|---------|---------|
| `LoggingConfig.*` 全7項目 | すべてのログ出力（LoggingManager.cs） | 中 - ハードコード化（IOptions依存削除のみ、設定ファイル不要） |

### 影響度: 中（本番環境に影響あり）
| 項目 | 影響範囲 | 対応工数 | Phase 2での完了状況 |
|------|---------|---------|---------------------|
| `MonitoringIntervalMs` | タイマー間隔（ExecutionOrchestrator.cs:75） | **小**（大幅削減） - ✅ Excel読み込み完了済み（ConfigurationLoaderExcel.cs:115）、使用箇所1箇所のみ修正 | ✅ Excel読み込み実装完了、PlcConfiguration.MonitoringIntervalMsに格納済み（既定値: 1000ms） |

### 影響度: 低（テストコードのみに影響）
| 項目 | 影響範囲 | 対応工数 |
|------|---------|---------|
| `SystemResources.MaxMemoryUsageMb` | ResourceManager（テストのみ）。一接続当たり500KB未満に抑えるメモリ管理用 | 小 - DI登録削除、テストコード修正 |
| `PlcCommunication.Connection.*`, `Timeouts.*`, `Devices` | ConfigurationLoader（テストのみ） | 小 - テストコード修正 or 削除 |

### 影響なし
| 項目 | 理由 |
|------|------|
| フェーズ0の削除対象すべて | 本番・テストいずれでも未使用 |

---

## Excel設定との重複チェック（2025-12-02 追加）

### 調査目的
Excel設定ファイル読み込み機能（ConfigurationLoaderExcel）で既に実装されている項目との重複を確認し、appsettings.json廃止時の削除対象を明確化する。

### 調査対象
- `C:\Users\1010821\Desktop\python\andon\documents\design\Step1_設定ファイル読み込み実装\実装計画` - Excel読み込み設計書
- `C:\Users\1010821\Desktop\python\andon\documents\design\Step1_設定ファイル読み込み実装\実装結果` - Excel読み込み実装結果
- `andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs` - Excel読み込み実装（Phase2～Phase5完了）
- `andon/Core/Models/ConfigModels/PlcConfiguration.cs` - Excel設定モデル

---

### 重複項目の結果

#### ✅ 重複している項目: **MonitoringIntervalMs のみ**

| 項目名 | appsettings.json | Excel設定 | 重複状況 |
|--------|-----------------|-----------|---------|
| **MonitoringIntervalMs** | PlcCommunication.MonitoringIntervalMs | settingsシート B11セル | ⚠️ **重複** |

#### ❌ 重複していない項目（Excel機能とは独立）

| カテゴリ | 項目 | Excel設定での対応状況 | 削除時の影響 |
|---------|------|---------------------|------------|
| **LoggingConfig.*** | 全7項目 | Excel設定には含まれない（ログ設定は別管理） | ⚠️ **移行必須** - ハードコード化が必要 |
| **PlcCommunication.Connection.*** | IpAddress, Port, UseTcp, IsBinary, FrameVersion | Excel設定で実装済み（settingsシート B8, B9, B10等）だが、appsettings.jsonの該当項目は本番未使用（テストのみ） | ✅ **削除予定** - 影響なし |
| **PlcCommunication.Timeouts.*** | ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs | Excel設定には含まれない | ✅ **削除予定** - 影響なし |
| **PlcCommunication.TargetDevices.Devices** | デバイスリスト | Excel設定で実装済み（データ収集デバイスシート）だが、appsettings.jsonの該当項目は本番未使用（テストのみ） | ✅ **削除予定** - 影響なし |
| **SystemResources.*** | 全項目 | Excel設定には含まれない | ✅ **削除予定** - 影響なし |

---

### MonitoringIntervalMsの重複詳細

#### 1. appsettings.json側の実装

**定義場所**: `PlcCommunication.MonitoringIntervalMs`

**モデル**:
```csharp
// andon/Core/Models/ConfigModels/DataProcessingConfig.cs
public class DataProcessingConfig
{
    public int MonitoringIntervalMs { get; set; } = 5000; // デフォルト5秒
}
```

**DI登録**:
- `DependencyInjectionConfigurator.cs:30` で `IOptions<DataProcessingConfig>` としてバインド

**使用箇所**:
- `ExecutionOrchestrator.cs:75` の `GetMonitoringInterval()` メソッド
  ```csharp
  var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;
  return TimeSpan.FromMilliseconds(intervalMs);
  ```

**用途**: タイマー間隔として使用（**全PLCで共通の監視間隔**）

---

#### 2. Excel設定側の実装

**定義場所**: `PlcConfiguration.MonitoringIntervalMs`

**モデル**:
```csharp
// andon/Core/Models/ConfigModels/PlcConfiguration.cs:56
public int MonitoringIntervalMs { get; set; }
```

**読み込み箇所**:
- `ConfigurationLoaderExcel.cs:115`
  ```csharp
  MonitoringIntervalMs = ReadCell<int>(settingsSheet, "B11", "データ取得周期(ms)")
  ```

**Excel位置**: settingsシート B11セル

**用途**: **各PLCごとに異なる監視間隔を設定可能**（Phase2～Phase5で実装完了）

---

### 現在の状況と問題点

#### 1. 現在の動作
- ExecutionOrchestratorは**appsettings.jsonの値**を使用（全PLC共通）
- Excel設定でも各PLCごとに値を読み込んでいるが、ExecutionOrchestratorでは**未使用**

#### 2. 設計上の矛盾
- Excel設定で各PLCごとに異なる監視間隔を設定できるのに、実際には使用されていない
- 将来的にはExcel設定の値（PLC個別の監視間隔）を使用するべき

#### 3. 削除時の影響
⚠️ **appsettings.jsonから削除すると、ExecutionOrchestrator.GetMonitoringInterval()が動作しなくなります**

- 現在本番環境で使用中なので、削除すると動作に問題が起きます
- 影響範囲: ExecutionOrchestrator.cs:75 (`GetMonitoringInterval()`メソッド)
- 依存DI: `IOptions<DataProcessingConfig>` (DependencyInjectionConfigurator.cs:30)

---

### appsettings.json廃止時の対応方針

#### MonitoringIntervalMsの移行手順

##### パターンA: Excel設定の値を使用（推奨）

**メリット**: 各PLCごとに異なる監視間隔を設定可能（柔軟性向上）

**実装手順**:
1. ExecutionOrchestrator.RunContinuousDataCycleAsync()を修正
   - 現在: 全PLC共通の監視間隔を使用
   - 変更後: 各PlcConfiguration.MonitoringIntervalMsを使用
2. ExecutionOrchestrator.GetMonitoringInterval()を削除 or デフォルト値返却に変更
3. DataProcessingConfigのDI登録を削除（DependencyInjectionConfigurator.cs:30）
4. appsettings.jsonから`PlcCommunication.MonitoringIntervalMs`を削除

**影響範囲**: ExecutionOrchestrator.cs（中規模修正）

---

##### パターンB: デフォルト値を使用

**メリット**: 最小限の修正で対応可能

**実装手順**:
1. DataProcessingConfig.cs:11のデフォルト値（5000ms）を維持
2. ExecutionOrchestratorでIOptions<DataProcessingConfig>への依存を削除
3. GetMonitoringInterval()をデフォルト値返却に変更
   ```csharp
   public TimeSpan GetMonitoringInterval()
   {
       return TimeSpan.FromMilliseconds(5000); // 固定値
   }
   ```
4. appsettings.jsonから削除

**影響範囲**: ExecutionOrchestrator.cs（小規模修正）

**デメリット**: Excel設定で各PLCごとに設定した値が使用されない

---

### 重複チェックのまとめ

| 項目 | Excel機能との重複 | 削除可否 | 対応方針 |
|------|-----------------|---------|---------|
| **MonitoringIntervalMs** | ⚠️ 重複あり | ❌ 移行必須 | パターンA（Excel設定使用）推奨 |
| **LoggingConfig.*** | 重複なし（Excel設定には含まれない） | ❌ 移行必須 | ハードコード化 |
| **PlcCommunication.Connection.*** | Excel機能とは独立（appsettings.json側は本番未使用） | ✅ **削除予定** | Excel読み込み機能には影響なし |
| **PlcCommunication.Timeouts.*** | Excel機能とは独立（Excel設定にタイムアウト項目なし） | ✅ **削除予定** | Excel読み込み機能には影響なし |
| **PlcCommunication.TargetDevices.Devices** | Excel機能とは独立（appsettings.json側は本番未使用） | ✅ **削除予定** | Excel読み込み機能には影響なし |
| **PlcCommunication.DataProcessing.BitExpansion*** | 実装されていない | ✅ **削除予定** | 影響なし |
| **SystemResources.*** | 重複なし（Excel設定には含まれない） | ✅ **削除予定** | 本番未使用のため影響なし |
| **Logging.*** | 重複なし（Excel設定には含まれない） | ✅ **削除予定** | 本番未使用のため影響なし |

---

## Excel設定利用状況詳細調査結果（2025-12-02 追加）

### 調査目的
ConfigurationLoaderExcelによってExcelファイルから設定が読み込まれた後、実際にアプリケーション内で利用されているかを確認し、未使用設定項目を特定する。

### 調査対象ファイル
- `documents/design/Step1_設定ファイル読み込み実装/設定ファイル内容.md` - Excel設定項目定義
- `andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs` - Excel読み込み実装
- `andon/Core/Models/ConfigModels/PlcConfiguration.cs` - 設定モデル
- `andon/Core/Controllers/ExecutionOrchestrator.cs` - 設定利用箇所
- `andon/Core/Controllers/ApplicationController.cs` - 設定利用箇所
- `andon/Core/Managers/PlcCommunicationManager.cs` - 設定利用箇所

---

### Excel設定項目の利用状況サマリー

#### ✅ 正常に利用されている項目（6項目）

| Excel項目 | セル位置 | PlcConfigurationプロパティ | 利用箇所 | 用途 |
|----------|---------|--------------------------|---------|------|
| **PLCのIPアドレス** | B8 | `IpAddress` | ApplicationController.cs:95, ExecutionOrchestrator.cs:189, PlcCommunicationManager.cs:86 | TCP/UDP接続先アドレス |
| **PLCのポート** | B9 | `Port` | ApplicationController.cs:96, ExecutionOrchestrator.cs:190, PlcCommunicationManager.cs:86 | TCP/UDP接続先ポート |
| **PLC-接続先通信方式** | B10 | `ConnectionMethod` | ApplicationController.cs:97, ExecutionOrchestrator.cs:191 | TCP/UDP切り替え（UseTcpに変換） |
| **PLC名称（装置略称）** | B15 | `PlcName` | ExecutionOrchestrator.cs:209, PlcCommunicationManager.cs（ログ出力） | ログ識別用PLC名 |
| **デバイスリスト** | データ収集デバイスタブ | `Devices` | ConfigToFrameManager.cs:158, ExecutionOrchestrator.cs:200 | SLMPフレーム構築・デバイス指定 |
| **オプション項目** | - | `FrameVersion`, `Timeout`, `IsBinary` | ConfigToFrameManager.cs:160-162, ExecutionOrchestrator.cs:194-196 | SLMP通信パラメータ（デフォルト値使用） |

---

#### ⚠️ 読み込まれているが未使用の項目（3項目）【重大な問題】

| Excel項目 | セル位置 | PlcConfigurationプロパティ | 問題の詳細 | 影響 | Phase 2完了状況 |
|----------|---------|--------------------------|-----------|------|---------------|
| **データ取得周期(sec)** | B11 | `MonitoringIntervalMs` | **ConfigurationLoaderExcel.cs:115で読み込み済み（既定値: 1000ms）**だが、ExecutionOrchestrator.cs:75で`_dataProcessingConfig.Value.MonitoringIntervalMs`（appsettings.json）を使用 | Excel設定が無視され、appsettings.jsonの値（デフォルト5000ms）が使用される | ✅ **Excel読み込み完了**、使用箇所の修正のみ必要 |
| **デバイス名（ターゲット名）** | B12 | `PlcModel` | **ConfigurationLoaderExcel.cs:116で読み込み済み**だが、DataOutputManager.OutputToJson()に渡されず、JSON出力に含まれない | 設計仕様（設定読み込み仕様.md:36）では`source.plcModel`に出力すべきだが未実装 | - |
| **データ保存先パス** | B13 | `SavePath` | **ConfigurationLoaderExcel.cs:117で読み込み済み**だが、ExecutionOrchestrator.cs:228でハードコード`"C:/Users/PPESAdmin/Desktop/x/output"`を使用 | Excel設定が無視され、固定パスが使用される（TODO: Phase 1-4 Refactorコメントあり） | - |

**重要**: これらの設定項目はConfigurationLoaderExcelで正常に読み込まれ、PlcConfigurationモデルに格納されているが、**実際の処理では使用されていない**。
**Phase 2完了により**: MonitoringIntervalMsはExcel読み込みと既定値設定が完了、使用箇所の修正のみが残されています。

---

#### ❌ 読み込まれていない項目（6項目）

| Excel項目 | セル位置 | 理由 |
|----------|---------|------|
| **工場** | B2 | ConfigurationLoaderExcel.csで読み込み処理が未実装 |
| **ライン** | B3 | 同上 |
| **工程** | B4 | 同上 |
| **装置番号** | B5 | 同上 |
| **装置名** | B6 | 同上 |
| **装置略称（旧仕様）** | B7 | 同上（B15のPLC名称を使用） |

**注意**: これらの項目は設計文書（設定ファイル内容.md）には記載されているが、ConfigurationLoaderExcelの実装では読み込み対象外。

---

### 問題点の詳細分析

#### 問題1: MonitoringIntervalMs（データ取得周期）の二重管理

**現状**:
```csharp
// ConfigurationLoaderExcel.cs:115 - Excel設定から読み込み
MonitoringIntervalMs = ReadCell<int>(settingsSheet, "B11", "データ取得周期(ms)")

// PlcConfiguration.cs:56 - モデルに格納
public int MonitoringIntervalMs { get; set; }

// ⚠️ ExecutionOrchestrator.cs:75 - appsettings.jsonの値を使用
var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;
return TimeSpan.FromMilliseconds(intervalMs);
```

**問題の影響**:
- ユーザーがExcel設定でデータ取得周期を変更しても、アプリケーションは変更を反映しない
- appsettings.jsonのデフォルト値（5000ms = 5秒）が常に使用される
- 設定の二重管理により、混乱を招く

**推奨対応**: フェーズ2-2の修正計画を前倒しして対応すべき

---

#### 問題2: PlcModel（デバイス名）のJSON出力欠落

**現状**:
```csharp
// ConfigurationLoaderExcel.cs:116 - Excel設定から読み込み
PlcModel = ReadCell<string>(settingsSheet, "B12", "デバイス名")

// ⚠️ ExecutionOrchestrator.cs:227 - DataOutputManagerへの引数に含まれない
var outputResult = await _dataOutputManager.OutputToJson(
    plcConfig.IpAddress,
    plcConfig.Port,
    /* plcConfig.PlcModel が渡されていない */
    structuredData.Devices,
    outputDirectory
);
```

**設計仕様との不一致**:
- 設定読み込み仕様.md:36では`source.plcModel`への出力が必須と定義
- 実装ではPlcModelが完全に使用されていない

**推奨対応**: DataOutputManager.OutputToJson()のシグネチャを修正し、PlcModelを追加

---

#### 問題3: SavePath（データ保存先パス）のハードコード

**現状**:
```csharp
// ConfigurationLoaderExcel.cs:117 - Excel設定から読み込み
SavePath = ReadCell<string>(settingsSheet, "B13", "データ保存先パス")

// ⚠️ ExecutionOrchestrator.cs:228 - ハードコードされたパスを使用
// TODO: Phase 1-4 Refactor - outputDirectoryを設定から取得
var outputDirectory = "C:/Users/PPESAdmin/Desktop/x/output";
```

**問題の影響**:
- ユーザーがExcel設定でデータ保存先パスを変更しても、アプリケーションは変更を反映しない
- 開発環境固有のパスがハードコードされている
- TODOコメントが残っており、未実装であることが明示されている

**推奨対応**: TODOコメントの指示通り、`plcConfig.SavePath`を使用するよう修正

---

### appsettings.json廃止計画への影響

#### MonitoringIntervalMsの移行計画の修正（Phase 2完了により大幅簡略化）

**Phase 2での完了事項**:
- ✅ **ConfigurationLoaderExcel.cs:115でExcel読み込み実装完了**
- ✅ **PlcConfiguration.MonitoringIntervalMsに格納完了（既定値: 1000ms）**
- ✅ **ハードコード置き換え対応テスト（Phase 1-5完了、803/804成功）**

**従来の計画（フェーズ2-2）**:
- appsettings.jsonのMonitoringIntervalMsをExcel設定へ移行
- Excel読み込み実装
- ExecutionOrchestrator.csで`IOptions<DataProcessingConfig>`依存を削除
- `plcConfig.MonitoringIntervalMs`を直接使用

**修正後の計画（大幅簡略化）**:
- ✅ **Excel設定への読み込みは既に完了している（Phase 2完了）**
- ✅ **PlcConfigurationモデルへの格納は既に完了している（Phase 2完了、既定値: 1000ms）**
- ❌ **ExecutionOrchestratorでの使用箇所のみ修正が必要**
- 影響範囲: ExecutionOrchestrator.cs:75の1箇所のみ（**小規模修正**）
- **工数**: 大幅削減（中 → 小）

**実装手順の簡略化**:
```csharp
// 修正前（ExecutionOrchestrator.cs:75）
var intervalMs = _dataProcessingConfig.Value.MonitoringIntervalMs;

// 修正後（1行のみ変更）
// plcConfigは既にメソッド引数で受け取っており、MonitoringIntervalMsも格納済み（既定値: 1000ms）
var intervalMs = plcConfig.MonitoringIntervalMs;
```

**TDDテストケースの追加**:
```
test_ExecutionOrchestrator_Excel設定値を直接使用()
- 期待値: plcConfig.MonitoringIntervalMsの値でタイマーが動作
- 確認: _dataProcessingConfig.Value.MonitoringIntervalMsが使用されないこと
- 確認: Excel設定の値（例: 5000ms、10000ms等）が正しく反映されること
```

---

#### 新規追加対応項目

従来の計画では含まれていなかった、以下の項目を追加対応すべき：

##### 追加対応1: PlcModelのJSON出力実装

**対応フェーズ**: フェーズ2-3（新規追加）

**実装内容**:
1. DataOutputManager.OutputToJson()のシグネチャ変更
2. PlcModelをJSON出力に追加（source.plcModelフィールド）
3. テストケース追加

**TDDテストケース**:
```
test_DataOutputManager_PlcModelをJSON出力()
- 入力: plcModel = "5_JRS_N2"
- 期待値: JSON出力の`source.plcModel`に"5_JRS_N2"が含まれる
```

---

##### 追加対応2: SavePathの利用実装

**対応フェーズ**: フェーズ2-4（新規追加）

**実装内容**:
1. ExecutionOrchestrator.cs:228のハードコードを削除
2. plcConfig.SavePathを使用
3. TODOコメント削除
4. テストケース追加

**TDDテストケース**:
```
test_ExecutionOrchestrator_Excel設定のSavePathを使用()
- 入力: plcConfig.SavePath = "./aaa/bbb/ccc"
- 期待値: 指定されたパスにJSON出力される
- 確認: ハードコードされたパスが使用されないこと
```

---

### 更新後の移行計画フェーズ構成（Phase 1-5完了により修正）

#### フェーズ2の構成変更

従来のフェーズ2（本番環境で必須の項目の移行）に、以下のサブフェーズを追加：

| フェーズ | 対象項目 | 対応方針 | 影響度 | 工数 | Phase 2での完了状況 |
|---------|---------|---------|--------|------|--------------------|
| **フェーズ2-1** | LoggingConfig 全7項目 | ハードコード化 | 高 | 中 | - |
| **フェーズ2-2** | MonitoringIntervalMs | ✅ **Excel設定利用（読み込み完了済み、使用箇所のみ修正）** | 中 | **小**（大幅簡略化） | ✅ ConfigurationLoaderExcel.cs:115実装完了、PlcConfiguration.MonitoringIntervalMs格納完了（既定値: 1000ms） |
| **フェーズ2-3** | PlcModel | **新規追加** - JSON出力実装 | 中 | 小 | ✅ ConfigurationLoaderExcel.cs:116実装完了、PlcConfiguration.PlcModel格納完了（使用箇所への追加のみ） |
| **フェーズ2-4** | SavePath | **新規追加** - Excel設定利用実装 | 中 | 小 | ✅ ConfigurationLoaderExcel.cs:117実装完了、PlcConfiguration.SavePath格納完了（使用箇所への追加のみ） |

**重要**: Phase 2（ハードコード置き換え対応 - 設定読み込みロジックの実装）の完了により、フェーズ2-2、2-3、2-4の**Excel読み込み処理は既に実装完了**しています。残りは各使用箇所での修正のみです。

---

### TDDテストケースの追加（フェーズ2-2、2-3、2-4）

#### Phase2_2_MonitoringInterval_ExcelMigrationTests.cs（更新）

既存のテストケースに以下を追加：

```csharp
[Test]
public async Task test_ExecutionOrchestrator_Excel設定値を直接使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        MonitoringIntervalMs = 10000 // Excel設定値: 10秒
    };

    // Act
    var result = await _orchestrator.RunContinuousDataCycleAsync(plcConfig);

    // Assert
    // タイマー間隔がplcConfig.MonitoringIntervalMsの値（10000ms）であることを確認
    Assert.That(_timerService.LastInterval, Is.EqualTo(TimeSpan.FromMilliseconds(10000)));

    // _dataProcessingConfig.Value.MonitoringIntervalMsが使用されていないことを確認
    _mockDataProcessingConfig.Verify(x => x.Value.MonitoringIntervalMs, Times.Never);
}
```

---

#### Phase2_3_PlcModel_JsonOutputTests.cs（新規追加）

```csharp
[Test]
public async Task test_DataOutputManager_PlcModelをJSON出力()
{
    // Arrange
    string plcModel = "5_JRS_N2";
    var devices = CreateSampleDevices();

    // Act
    var result = await _dataOutputManager.OutputToJson(
        "172.30.40.40",
        8192,
        plcModel, // ← 新規追加パラメータ
        devices,
        "./output"
    );

    // Assert
    Assert.That(result.Success, Is.True);

    // JSON出力内容の確認
    var jsonContent = File.ReadAllText(result.OutputFilePath);
    var jsonObject = JsonSerializer.Deserialize<JsonDocument>(jsonContent);

    var sourcePlcModel = jsonObject.RootElement.GetProperty("source").GetProperty("plcModel").GetString();
    Assert.That(sourcePlcModel, Is.EqualTo("5_JRS_N2"));
}
```

---

#### Phase2_4_SavePath_ExcelConfigTests.cs（新規追加）

```csharp
[Test]
public async Task test_ExecutionOrchestrator_Excel設定のSavePathを使用()
{
    // Arrange
    var plcConfig = new PlcConfiguration
    {
        SavePath = "./test/custom/output" // Excel設定値
    };

    // Act
    var result = await _orchestrator.RunDataCycleAsync(plcConfig);

    // Assert
    Assert.That(result.Success, Is.True);

    // 指定されたパスにファイルが出力されていることを確認
    Assert.That(Directory.Exists("./test/custom/output"), Is.True);

    // ハードコードされたパスにファイルが存在しないことを確認
    Assert.That(Directory.Exists("C:/Users/PPESAdmin/Desktop/x/output"), Is.False);
}
```

---

### 推奨実装優先順位（Phase 2完了により更新）

**Phase 2での完了事項により、全ての項目でExcel読み込みは完了済みです。使用箇所の修正のみが必要です。**

1. **最優先（フェーズ2-2）**: MonitoringIntervalMsの修正
   - 影響度: 中
   - 工数: **小**（ExecutionOrchestrator.cs:75の1箇所のみ修正）
   - 理由: ユーザー設定が反映されない重大な問題
   - ✅ **Phase 2完了状況**: Excel読み込み完了、PlcConfiguration.MonitoringIntervalMs格納完了（既定値: 1000ms）

2. **高優先（フェーズ2-4）**: SavePathの修正
   - 影響度: 中
   - 工数: **小**（ExecutionOrchestrator.cs:228のハードコード削除のみ）
   - 理由: ハードコードされた開発環境パスが本番環境で動作しない
   - ✅ **Phase 2完了状況**: Excel読み込み完了、PlcConfiguration.SavePath格納完了

3. **中優先（フェーズ2-3）**: PlcModelのJSON出力実装
   - 影響度: 中
   - 工数: **小**（DataOutputManager.OutputToJson()のシグネチャ修正のみ）
   - 理由: 設計仕様との不一致、JSON出力の完全性
   - ✅ **Phase 2完了状況**: Excel読み込み完了、PlcConfiguration.PlcModel格納完了

**重要**: Phase 2完了により、全ての項目で**Excel読み込み処理の追加実装は不要**になりました。使用箇所の修正のみで完了します。

---

### 結論（Phase 1-5完了により更新）

**Excel設定読み込み機能（ConfigurationLoaderExcel）の実装状況**:

✅ **読み込み処理**: Phase2～Phase5で正常に実装済み
✅ **Phase 2完了**: MonitoringIntervalMs、PlcModel、SavePathのExcel読み込み実装完了（ConfigurationLoaderExcel.cs:115-117）
✅ **モデル格納**: PlcConfigurationへの格納完了（既定値設定含む）
⚠️ **利用処理**: 3項目（MonitoringIntervalMs、PlcModel、SavePath）が読み込み後に未使用（使用箇所の修正のみ必要）
❌ **未実装項目**: 6項目（工場、ライン、工程、装置番号、装置名、装置略称）が読み込み未実装

**appsettings.json廃止計画への影響**:

- **MonitoringIntervalMsの移行計画が大幅に簡略化**（✅ Excel読み込み完了済み（既定値: 1000ms）、使用箇所1箇所のみ修正）
- **PlcModelの移行計画が大幅に簡略化**（✅ Excel読み込み完了済み、DataOutputManager.OutputToJson()への追加のみ）
- **SavePathの移行計画が大幅に簡略化**（✅ Excel読み込み完了済み、ハードコード削除のみ）
- **フェーズ2の工数大幅削減**（MonitoringIntervalMs、PlcModel、SavePathすべて小規模修正化）

**Phase 1-5完了により実現したこと**:

1. ✅ Phase 1: DefaultValues.cs（6個の既定値定数定義）
2. ✅ Phase 2: ConfigurationLoaderExcel拡張（7プロパティ追加: ConnectionMethod, FrameVersion, Timeout, IsBinary, MonitoringIntervalMs, PlcId, PlcName）
3. ✅ Phase 3: SettingsValidator.cs（6つの検証メソッド実装）
4. ✅ Phase 4: ConfigToFrameManagerのハードコード削除完了
5. ✅ Phase 5: 統合テスト（9個のテストケース全成功）
6. ✅ 全体テスト803/804成功
7. ✅ 実機テスト準備完了

**次のアクション**:

1. フェーズ2-2の実装（MonitoringIntervalMsの使用箇所修正 - ExecutionOrchestrator.cs:75の1箇所）
2. フェーズ2-3の実装（PlcModelのJSON出力追加 - DataOutputManager.OutputToJson()への追加）
3. フェーズ2-4の実装（SavePathの利用 - ExecutionOrchestrator.cs:228のハードコード削除）
4. 全テストケースのパス確認

**重要**: Phase 1-5完了により、**Excel読み込み処理の追加実装は不要**になりました。各項目とも使用箇所の修正のみで完了します。

---

## 関連ファイル一覧（起動フロー順）

### 起動フロー
1. `andon/Program.cs` - エントリーポイント
2. `andon/Services/DependencyInjectionConfigurator.cs` (L30-32) - IOptions<T>バインド処理
3. `andon/Services/AndonHostedService.cs` - バックグラウンドサービス起動
4. `andon/Core/Controllers/ApplicationController.cs` - アプリケーション制御
5. `andon/Core/Controllers/ExecutionOrchestrator.cs` - 実行制御

### 本番環境で使用されている設定読み込み関連
- `andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs` - Excel設定読み込み（本番環境で使用）
- `andon/Core/Managers/LoggingManager.cs` - LoggingConfig全7項目を実際に使用
- `andon/Core/Controllers/ExecutionOrchestrator.cs` - MonitoringIntervalMsを実際に使用

### テストでのみ使用されている設定読み込み関連
- `andon/Infrastructure/Configuration/ConfigurationLoader.cs` - appsettings.json読み込み（テストのみ）
- `andon/Core/Managers/ResourceManager.cs` - MaxMemoryUsageMb読み込み（テストのみ）。一接続当たり500KB未満に抑えるメモリ管理用

### モデル定義
- `andon/Core/Models/ConfigModels/DataProcessingConfig.cs` - MonitoringIntervalMs定義
- `andon/Core/Models/ConfigModels/SystemResourcesConfig.cs` - MaxMemoryUsageMb定義（本番未使用）。一接続当たり500KB未満に抑えるメモリ管理用
- `andon/Core/Models/ConfigModels/LoggingConfig.cs` - ログ設定7項目定義（本番使用）

---

## 結論（起動フロー調査に基づく最終判断 - Phase 1-5完了により更新）

### appsettings.jsonの廃止は可能

**Phase 1-5（ハードコード置き換え対応）の完了により、対応工数が大幅に削減されました**

#### 必須対応（本番環境に影響）
1. **LoggingConfig（7項目）のハードコード化** - 最優先
   - 移行先: **ハードコード化**
     - LogLevel = "Information"
     - EnableFileOutput = true
     - EnableConsoleOutput = true
     - LogFilePath = "./logs"
     - MaxLogFileSizeMb = 1
     - MaxLogFileCount = 10
     - EnableDateBasedRotation = true
   - 影響度: 高 - すべてのログ出力に影響
   - 対応工数: 中（LoggingManager.csの修正のみ）

2. **MonitoringIntervalMsのExcel設定への移行**（✅ Phase 2で大幅簡略化）
   - 移行先: **Excel設定** (settingsシート B11セル)
   - ✅ **Phase 2完了状況**: Excel読み込み実装完了（ConfigurationLoaderExcel.cs:115、既定値: 1000ms）
   - 影響度: 中 - タイマー間隔に影響
   - 対応工数: **小**（ExecutionOrchestrator.cs:75の1箇所のみ修正 - Phase 2完了により大幅削減）

#### 推奨対応（コード整理）
3. **ResourceManagerの扱いを決定**
   - 本番で使用する予定がなければ削除
   - 影響度: 低 - テストコードのみに影響

4. **ConfigurationLoaderの扱いを決定**
   - テストではモックで十分なため削除推奨
   - 影響度: 低 - テストコードのみに影響

#### 即座に実行可能（影響なし）
5. **完全に未使用の項目を削除**（フェーズ0）（**削除予定**）
   - PlcCommunication.Connection.* 全体（IpAddress, Port, UseTcp, IsBinary, FrameVersion）※Excel機能とは独立
   - PlcCommunication.Timeouts.* 全体（ConnectTimeoutMs, SendTimeoutMs, ReceiveTimeoutMs）※Excel設定にはタイムアウト項目なし
   - PlcCommunication.TargetDevices.Devices ※Excel機能（データ収集デバイスシート）とは独立
   - PlcCommunication.DataProcessing.BitExpansion.* 全体
   - SystemResources.* 全体（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB, MaxConcurrentConnections）
   - **Logging.* 全体（ConsoleOutput.*, DetailedLog.*）** ※⚠️ LoggingConfig（本番使用中）とは完全に別物
   - 影響度: **完全になし**
     - 本番環境への影響なし
     - テスト環境への影響なし
     - Excel読み込み機能への影響なし
     - LoggingConfig（本番使用中）への影響なし

---

### 重要な発見（Phase 1-5完了により更新）
- **DIコンテナに登録されていても、実際には本番コードで使用されていないクラスが存在**
  - ResourceManager: DI登録されているが本番未使用
  - ConfigurationLoader: テストでのみ使用

- **appsettings.jsonで読み込まれているが、実際に本番環境で使用されているのは2項目のみ**
  - LoggingConfig.* 全7項目
  - PlcCommunication.MonitoringIntervalMs

- **Phase 1-5完了により、MonitoringIntervalMs、PlcModel、SavePathのExcel読み込みは完了済み**
  - ConfigurationLoaderExcel.cs:115-117で実装完了
  - PlcConfigurationへの格納完了（既定値設定含む）
  - 使用箇所の修正のみが必要（小規模修正）
  - Phase 2でConnectionMethod, FrameVersion, Timeout, IsBinary, PlcIdも追加実装完了

### 推奨アプローチ（Phase 1-5完了により更新）
段階的な廃止を推奨します。フェーズ0から順に実施することで、リスクを最小化できます。

1. **フェーズ0**: 未使用項目の即座削除（リスク: なし）
2. **フェーズ1**: テスト専用項目の整理（リスク: テストコードのみ）
3. **フェーズ2**: 本番環境で必須の項目の移行（リスク: 中 - **Phase 2完了により工数削減**）
   - フェーズ2-1: LoggingConfigのハードコード化（工数: 中）
   - フェーズ2-2: MonitoringIntervalMs使用箇所修正（工数: **小** - Excel読み込み完了済み、既定値: 1000ms）
   - フェーズ2-3: PlcModelのJSON出力追加（工数: **小** - Excel読み込み完了済み）
   - フェーズ2-4: SavePathの利用実装（工数: **小** - Excel読み込み完了済み）
4. **フェーズ3**: appsettings.json完全廃止（リスク: 低 - すべての移行完了後）

**Phase 1-5完了状況**:
- ✅ Phase 1: DefaultValues.cs（6個の既定値定数定義）
- ✅ Phase 2: ConfigurationLoaderExcel拡張（7プロパティ追加）
- ✅ Phase 3: SettingsValidator.cs（6つの検証メソッド実装）
- ✅ Phase 4: ConfigToFrameManagerのハードコード削除
- ✅ Phase 5: 統合テスト（9個のテストケース全成功）

---

## 🚨 追加発見事項：PlcConnectionConfig等のJSON設定用モデルについて

### 調査日: 2025-12-02

### 概要
PlcConfigurationとPlcConnectionConfigの2つの設定モデルが存在していることが判明しました。

### 2つのモデルの違い

#### 1. **PlcConfiguration** (Excel設定用モデル) - ✅ 継続使用
- **ファイル**: `andon\Core\Models\ConfigModels\PlcConfiguration.cs`
- **用途**: Excel設定ファイル（.xlsx）からの読み込み専用モデル
- **特徴**:
  - MonitoringIntervalMs, PlcModel, SavePath等を含む完全な設定
  - ConfigurationLoaderExcel.LoadAllPlcConnectionConfigs()で使用
  - ExecutionOrchestrator.ExecuteMultiPlcCycleAsync_Internal()で使用
  - 既存の運用で主に使用されている

#### 2. **PlcConnectionConfig** (JSON設定用モデル) - ❌ 廃止予定
- **ファイル**: `andon\Core\Models\ConfigModels\PlcConnectionConfig.cs`
- **用途**: appsettings.json等のJSON設定ファイル読み込み用モデル（Phase6新規追加）
- **特徴**:
  - 軽量な接続特化設定（MonitoringIntervalMs, PlcModel, SavePathを含まない）
  - Priority（並列実行優先度）プロパティあり
  - ExecutionOrchestrator.ExecuteSinglePlcAsync()で使用
  - MultiPlcCoordinator（並列実行）で使用
  - **現状では本格的な活用はこれからの段階**

#### 3. **DeviceEntry** (JSON設定読み込み用中間型) - ❌ 廃止予定
- **ファイル**: `andon\Core\Models\ConfigModels\DeviceEntry.cs`
- **用途**: appsettings.jsonから読み込むデバイスエントリ
- **特徴**:
  - DeviceType(string), DeviceNumber(int), Description(string)
  - ToDeviceSpecification()でDeviceSpecificationに変換可能
  - PlcConnectionConfig.Devicesで使用

#### 4. **MultiPlcConfig** (複数PLC設定のルート) - ❌ 廃止予定
- **ファイル**: `andon\Core\Models\ConfigModels\MultiPlcConfig.cs`
- **用途**: JSON設定での複数PLC管理
- **特徴**:
  - PlcConnections (List<PlcConnectionConfig>)
  - ParallelConfig (ParallelProcessingConfig)

---

### JSON設定読み込み完全廃止に伴う対応方針

#### ✅ 継続使用（Excel設定ベース）
| モデル | 理由 |
|--------|------|
| **PlcConfiguration** | Excel設定読み込みの主要モデル、既存運用で使用中 |
| **DeviceSpecification** | PLC通信で使用する本質的なモデル、ExcelとJSON両方で利用 |
| **ConfigurationLoaderExcel** | Excel設定読み込み実装、本番環境で使用中 |

#### ❌ 削除対象（JSON設定専用）
| モデル/クラス | ファイルパス | 削除理由 | 影響範囲 |
|------------|------------|---------|---------|
| **PlcConnectionConfig** | `andon\Core\Models\ConfigModels\PlcConnectionConfig.cs` | JSON設定専用モデル（Phase6追加）、本格活用前 | ExecutionOrchestrator.ExecuteSinglePlcAsync()<br>MultiPlcCoordinator<br>テストコード（MultiPlcConfigTests.cs等） |
| **DeviceEntry** | `andon\Core\Models\ConfigModels\DeviceEntry.cs` | JSON設定読み込み用中間型、PlcConnectionConfigでのみ使用 | PlcConnectionConfig.Devices<br>テストコード |
| **MultiPlcConfig** | `andon\Core\Models\ConfigModels\MultiPlcConfig.cs` | JSON設定での複数PLC管理用、PlcConnectionConfigのコンテナ | MultiPlcCoordinator<br>テストコード |
| **ConfigurationLoader** | `andon\Infrastructure\Configuration\ConfigurationLoader.cs` | appsettings.json読み込み実装、テストでのみ使用 | テストコード（Unit/Integration） |
| **MultiPlcCoordinator** | `andon\Core\Managers\MultiPlcCoordinator.cs` | PlcConnectionConfig専用の並列実行ヘルパー | ExecutionOrchestrator（使用箇所削除必要）<br>テストコード |

---

### 削除時の影響と対応方針

#### 影響度: 低～中（Phase6機能のため本格活用前）

| 削除対象 | 影響箇所 | 対応方針 |
|---------|---------|---------|
| **PlcConnectionConfig** | ExecutionOrchestrator.ExecuteSinglePlcAsync() | メソッド削除 or PlcConfiguration版に統合 |
| **MultiPlcCoordinator** | 並列実行機能 | 削除（Excel設定ベースの並列実行は別途実装検討） |
| **DeviceEntry** | PlcConnectionConfig.Devices | PlcConnectionConfigと同時削除 |
| **MultiPlcConfig** | - | PlcConnectionConfigと同時削除 |
| **ConfigurationLoader** | テストコード | テストコードをモック使用に変更 or 削除 |

---

### 実装手順（TDD準拠）

#### フェーズ0.5: JSON設定用モデルの削除（appsettings.json廃止前の準備）

##### Step 0.5-1: 削除影響範囲の特定テスト作成（Red）
```
目的: 削除対象クラスの依存関係を洗い出す

テストケース名: Phase0_5_JsonConfigModels_DependencyTests.cs

1. test_PlcConnectionConfig_本番フローで限定的使用()
   - ExecuteSinglePlcAsync()でのみ使用されることを確認
   - ExecuteMultiPlcCycleAsync_Internal()では使用されていないことを確認

2. test_MultiPlcCoordinator_本番フローで使用()
   - ExecutionOrchestratorから呼ばれているか確認
   - Excel設定ベースの代替実装が可能か確認

3. test_DeviceEntry_PlcConnectionConfigでのみ使用()
   - DeviceEntryがPlcConnectionConfig以外で使用されていないことを確認

期待される結果: 影響範囲の特定
```

##### Step 0.5-2: 削除実装（Green）
```
作業内容:
1. ExecutionOrchestrator.ExecuteSinglePlcAsync() を削除
2. MultiPlcCoordinator.cs を削除
3. PlcConnectionConfig.cs を削除
4. DeviceEntry.cs を削除
5. MultiPlcConfig.cs を削除
6. 関連テストコードを削除 or 修正
   - MultiPlcConfigTests.cs
   - MultiPlcCoordinatorTests.cs
   - ExecutionOrchestratorTests.cs（該当テストケースのみ）

確認コマンド:
dotnet build  # ビルドエラーがないことを確認
dotnet test --filter "FullyQualifiedName~Phase0_5"
```

##### Step 0.5-3: リファクタリング（Refactor）
```
作業内容:
1. 不要なusingディレクティブの削除
2. コメント更新（PlcConfiguration中心の設計であることを明記）
3. ドキュメント更新
```

---

### 削除タイミング

**推奨**: フェーズ0（未使用項目の即座削除）と同時 or フェーズ1（テスト専用項目の整理）で実施

**理由**:
- Phase6で追加されたばかりで本格活用前
- JSON設定読み込み自体を廃止するため、JSON専用モデルも不要
- Excel設定ベース（PlcConfiguration）に統一することで、設計がシンプル化

---

### 設計上の教訓

#### 問題点
- 2つの設定管理方法（ExcelとJSON）が並存していた
- JSON設定ベースのモデル（PlcConnectionConfig）は本格活用前だったため、早期に廃止判断が可能

#### 改善方針
- **Excel設定ベースに統一** - PlcConfigurationのみを使用
- 将来的な拡張もExcel設定の範囲内で実施
- JSON設定は完全に廃止し、設定管理方法を単一化

---

### まとめ

| 項目 | 結論 |
|------|------|
| **PlcConfiguration** | ✅ **継続使用** - Excel設定の主要モデル |
| **PlcConnectionConfig** | ❌ **削除予定** - JSON設定専用、本格活用前 |
| **DeviceEntry** | ❌ **削除予定** - PlcConnectionConfig専用 |
| **MultiPlcConfig** | ❌ **削除予定** - JSON設定専用 |
| **MultiPlcCoordinator** | ❌ **削除予定** - PlcConnectionConfig専用 |
| **ConfigurationLoader** | ❌ **削除予定** - appsettings.json読み込み専用 |

**重要**: JSON設定読み込みの完全廃止により、PlcConnectionConfig関連のモデル・クラスも併せて削除することで、設計がExcel設定ベースに統一され、保守性が大幅に向上します。
