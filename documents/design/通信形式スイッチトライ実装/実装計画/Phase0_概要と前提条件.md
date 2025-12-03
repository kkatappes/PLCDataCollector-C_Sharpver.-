# Phase 0: 概要と前提条件

**最終更新**: 2025-12-03

## 概要

PLC接続時に、Excel設定ファイルで指定されたプロトコル（TCP/UDP）での接続が失敗した場合、自動的に代替プロトコルで再試行する機能を実装する。

**重要**: 本実装計画は**Excel設定ファイル形式**を前提としています。Phase4_JSON廃止計画に従い、JSON形式の設定ファイルは廃止されました。

## 目的

- PLC接続の信頼性向上
- ネットワーク環境に応じた柔軟な接続方式の選択
- オペレーターの手動設定変更作業を削減

## 実装対象

### 主要実装箇所

**1. PlcCommunicationManager.ConnectAsync()**
- 場所: `andon/Core/Managers/PlcCommunicationManager.cs`
- 役割: PLC接続処理の中核メソッド

### 関連する既存コンポーネント

**2. PlcConfiguration**
- 場所: `andon/Core/Models/ConfigModels/PlcConfiguration.cs`
- 役割: Excel設定ファイルから読み込まれた接続設定（IP、ポート、プロトコル、タイムアウト等）

**3. ConfigurationLoaderExcel**
- 場所: `andon/Infrastructure/Configuration/ConfigurationLoaderExcel.cs`
- 役割: Excel設定ファイル（*.xlsx）の読み込み

**4. ConnectionConfig**
- 場所: `andon/Core/Models/ConfigModels/ConnectionConfig.cs`
- 役割: PLC通信時に使用する接続設定
- プロパティ:
  - `IpAddress`: IPアドレス（IPAddress型）
  - `Port`: ポート番号（int型）
  - `UseTcp`: TCP使用フラグ（bool型、false=UDP）
  - `ConnectionType`: **string型計算プロパティ（読み取り専用）**、UseTcpから自動計算される接続プロトコル文字列表現（"TCP" または "UDP"）、ログ出力・可読性用
  - `IsBinary`: バイナリ形式フラグ（bool型）
  - `FrameVersion`: フレームバージョン（FrameVersion列挙型）

**5. TimeoutConfig**
- 場所: `andon/Core/Models/ConfigModels/TimeoutConfig.cs`
- 役割: タイムアウト設定
- プロパティ:
  - `ConnectTimeoutMs`: 接続タイムアウト（ミリ秒）
  - `SendTimeoutMs`: 送信タイムアウト（ミリ秒）
  - `ReceiveTimeoutMs`: 受信タイムアウト（ミリ秒）

**6. ConnectionResponse**
- 場所: `andon/Core/Models/ConnectionResponse.cs`
- 役割: ConnectAsync()の戻り値モデル

**7. ErrorHandler**
- 場所: `andon/Core/Managers/ErrorHandler.cs`
- 役割: エラー処理・ログ記録

**8. LoggingManager**
- 場所: `andon/Core/Managers/LoggingManager.cs`
- 役割: 接続試行履歴のログ出力

## 実装仕様

### 1. 接続試行フロー

```
開始
  ↓
Excel設定ファイルから初期プロトコルを取得
  ↓
初期プロトコルで接続試行（タイムアウト設定に従う）
  ↓
成功? ─YES→ 接続成功を返却 → 終了
  ↓NO
代替プロトコルで接続試行（タイムアウト設定に従う）
  ↓
成功? ─YES→ 接続成功を返却（代替プロトコル使用を記録） → 終了
  ↓NO
接続失敗を返却（両プロトコル失敗を記録） → 終了
```

### 2. 詳細仕様

#### 2.1 接続試行ロジック

**前提: 設定情報の変換**
- Excel設定ファイル → `PlcConfiguration` → **変換処理** → `ConnectionConfig`
- `PlcCommunicationManager`は`ConnectionConfig`を使用
- 変換処理で`PlcConfiguration.ConnectionMethod` ("TCP"/"UDP") → `ConnectionConfig.UseTcp` (bool)に変換

**初期プロトコル試行:**
- `ConnectionConfig.UseTcp`に基づいてプロトコル（TCP/UDP）を決定
- `TimeoutConfig.ConnectTimeoutMs`で指定されたタイムアウト時間内に接続を試行
- 接続失敗時は例外をキャッチし、次のステップへ

**代替プロトコル試行:**
- 初期プロトコルがTCPの場合 → UDPで再試行
- 初期プロトコルがUDPの場合 → TCPで再試行
- 同じタイムアウト設定を使用
- 接続失敗時は例外をキャッチし、最終的な失敗として処理

#### 2.2 戻り値仕様

**現在のConnectionResponseモデル（実装済み）:**
```csharp
public class ConnectionResponse
{
    public required ConnectionStatus Status { get; init; }  // 接続状態（Connected/Failed/Timeout）
    public Socket? Socket { get; init; }                    // ソケットインスタンス
    public DateTime? ConnectedAt { get; init; }             // 接続完了時刻
    public double? ConnectionTime { get; init; }            // 接続所要時間（ミリ秒）
    public string? ErrorMessage { get; init; }              // エラーメッセージ
}
```

**通信プロトコル自動切り替え機能実装時に追加済み（Phase1完了: 2025-12-03）:**
```csharp
// ✅ 以下のプロパティを追加完了（Phase1実装済み）
public string? UsedProtocol { get; init; }          // 実際に使用されたプロトコル（"TCP"/"UDP"、string型、null許容）
public bool IsFallbackConnection { get; init; }     // 代替プロトコルで接続したか（bool型、デフォルト：false）
public string? FallbackErrorDetails { get; init; }  // 初期プロトコル失敗時のエラー詳細（string型、null許容）
```

**接続ロジック実装完了（Phase2完了: 2025-12-03）:**
- ✅ PlcCommunicationManager.ConnectAsync()に代替プロトコル試行ロジックを実装
- ✅ 初期プロトコル失敗時に自動的に代替プロトコル（TCP↔UDP）で再試行
- ✅ ConnectionResponseの新規プロパティ（UsedProtocol, IsFallbackConnection, FallbackErrorDetails）を活用
- ✅ 例外オブジェクト保持によるエラータイプ正確判定（TimeoutException/SocketException）
- ✅ ConnectionStatus.Timeout vs Failed の適切な判定実装
- ✅ ErrorDetails.AdditionalInfoへの条件付きフィールド追加（TimeoutMs/SocketErrorCode）
- ✅ テスト結果: 799/801テスト成功（新規6テスト+既存799テスト）
- 📄 実装結果: [Phase2_接続ロジック実装_TestResults.md](../実装結果/Phase2_接続ロジック実装_TestResults.md)

**ログ出力実装完了（Phase3完了: 2025-12-03）:**
- ✅ PlcCommunicationManagerにLoggingManager統合（ILoggingManager?フィールド・パラメータ追加）
- ✅ Console.WriteLineからLoggingManagerへの置き換え（4箇所）
- ✅ ErrorMessages.csにログ出力用メソッド4件追加（詳細形式、IPアドレス/ポート含む）
- ✅ 適切なログレベル（INFO/WARNING/ERROR）での出力実装
- ✅ TDDサイクル（Red-Green-Refactor）完全実施
- ✅ null許容設計で既存テストへの影響なし
- ✅ テスト結果: 45/45テスト成功（新規3テスト+既存42テスト）
- 📄 実装結果: [Phase3_ログ出力実装_TestResults.md](../実装結果/Phase3_ログ出力実装_TestResults.md)

**成功パターン:**
1. 初期プロトコルで成功
   - `Status = ConnectionStatus.Connected`
   - `UsedProtocol = 設定で指定されたプロトコル（"TCP"または"UDP"）`
   - `IsFallbackConnection = false`
   - `Socket = 接続済みSocketインスタンス`
   - `ConnectedAt = 接続完了時刻`
   - `ConnectionTime = 接続所要時間（ミリ秒）`

2. 代替プロトコルで成功
   - `Status = ConnectionStatus.Connected`
   - `UsedProtocol = 代替プロトコル（"TCP"または"UDP"）`
   - `IsFallbackConnection = true`
   - `FallbackErrorDetails = "初期プロトコル({初期})で接続失敗: {エラー詳細}"`
   - `Socket = 接続済みSocketインスタンス`
   - `ConnectedAt = 接続完了時刻`
   - `ConnectionTime = 接続所要時間（ミリ秒）`

**失敗パターン:**
- `Status = ConnectionStatus.Failed` または `ConnectionStatus.Timeout`
- `ErrorMessage = "TCP/UDP両プロトコルでの接続に失敗しました。\n- TCP接続エラー: {詳細}\n- UDP接続エラー: {詳細}"`
- `Socket = null`

#### 2.3 ログ出力仕様

**接続試行開始時:**
```
[INFO] PLC接続試行開始: {IP}:{Port}, プロトコル: {初期プロトコル}
```

**初期プロトコル失敗時:**
```
[WARN] {初期プロトコル}接続失敗: {エラー内容}. 代替プロトコル({代替})で再試行します。
```

**代替プロトコル成功時:**
```
[INFO] 代替プロトコル({代替})で接続成功: {IP}:{Port}
```

**両プロトコル失敗時:**
```
[ERROR] PLC接続失敗: {IP}:{Port}. TCP/UDP両プロトコルで接続に失敗しました。
  - TCP接続エラー: {TCPエラー詳細}
  - UDP接続エラー: {UDPエラー詳細}
```

#### 2.4 タイムアウト設定

- 各プロトコルの接続試行には独立したタイムアウトを適用
- `TimeoutConfig.ConnectTimeoutMs`を使用（単位: ミリ秒）
- 合計最大接続時間 = `ConnectTimeoutMs × 2`（初期 + 代替）

**注意**:
- Excel設定ファイルの`PlcConfiguration.Timeout`は`TimeoutConfig.ConnectTimeoutMs`に変換して使用
- `TimeoutConfig`には他に`SendTimeoutMs`、`ReceiveTimeoutMs`も存在するが、接続試行には`ConnectTimeoutMs`のみを使用

#### 2.5 エラーハンドリング

**対象とする例外:**
- `SocketException`: ネットワーク接続エラー
- `TimeoutException`: タイムアウトエラー
- `InvalidOperationException`: 無効な操作（ソケット状態エラー等）

**例外発生時の処理:**
1. エラー内容をログに記録
2. 初回試行の場合 → 代替プロトコルで再試行
3. 再試行後の場合 → ConnectionResponseに詳細を含めて失敗を返却

### 3. 設定ファイル仕様

既存のExcel設定ファイル（*.xlsx）の"settings"シートに変更なし。`ConnectionMethod`フィールドは初期接続試行プロトコルを指定する意味となる。

**Excel設定ファイル "settings"シート（抜粋）:**

| 項目名 | セル | 値の例 | 備考 |
|--------|------|--------|------|
| IPAddress | B8 | 192.168.1.100 | PLC IPアドレス（必須） |
| Port | B9 | 5000 | PLC ポート番号（必須） |
| ConnectionMethod | B10 | TCP | 初期試行プロトコル（TCP/UDP、既定値: UDP） |
| Timeout | B11 | 5000 | タイムアウト値（ミリ秒、既定値: 1000） |
| FrameVersion | B14 | 4E | SLMPフレームバージョン（3E/4E、既定値: 4E） |
| IsBinary | B15 | true | Binary/ASCII形式（既定値: true） |

**注意**: Phase4_JSON廃止計画に従い、JSON形式の設定ファイルは廃止されました。全ての設定はExcel形式（*.xlsx）から読み込まれます。

### 4. 既存機能への影響

**変更が必要な箇所:**
- `PlcCommunicationManager.ConnectAsync()`: 実装の追加・修正
- `ConnectionResponse`: 新規プロパティの追加

**変更不要な箇所:**
- `PlcConfiguration`: 既存のまま使用（Excel読み込み専用モデル）
- `ConfigurationLoaderExcel`: 既存のまま使用（Excel読み込み機能）
- `ConnectionConfig`: 既存のまま使用（PLC通信専用設定）
- `TimeoutConfig`: 既存のまま使用（タイムアウト設定）
- `ConnectionStatus`: 既存のまま使用（接続状態列挙型）
- その他のManagerクラス: 影響なし

**注意:**
- `ConnectionType`列挙型（Ethernet/Serial/USB）は本機能では使用しません
- プロトコル判定は`ConnectionConfig.UseTcp` (bool)と`ConnectionConfig.ConnectionType` (string計算プロパティ)を使用

## 参考情報

### 関連ドキュメント
- `documents/design/クラス設計.md`: PlcCommunicationManagerの詳細設計
- `documents/design/エラーハンドリング.md`: エラー処理方針
- `documents/design/ログ機能設計.md`: ログ出力仕様
- `documents/design/step1-3テスト/実装計画/Phase4_JSON廃止計画.md`: JSON廃止・Excel専用システム移行計画
- `documents/design/Step1_設定ファイル読み込み実装/実装計画/00_実装フェーズ概要.md`: Step1実装フェーズ概要
- `documents/design/ハードコード実装置き換え対応/ハードコード状況確認.md`: ハードコード値の調査結果

### 関連テストケース
- `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- `Tests/Integration/Step3_6_IntegrationTests.cs`

## 設定情報の変換について

**重要: PlcConfigurationとConnectionConfigの役割分担**

**PlcConfiguration** (Excel読み込み専用):
- Excel設定ファイルから読み込んだ全設定情報を保持
- プロパティ例:
  - `IpAddress` (string)
  - `Port` (int)
  - `ConnectionMethod` (string: "TCP"/"UDP")
  - `Timeout` (int: ミリ秒)
  - `FrameVersion` (string: "3E"/"4E")
  - `IsBinary` (bool)
  - `MonitoringIntervalMs` (int)
  - `Devices` (List<DeviceSpecification>)

**ConnectionConfig** (PLC通信専用):
- PLC通信時に使用する接続設定のみを保持
- プロパティ例:
  - `IpAddress` (string)
  - `Port` (int)
  - `UseTcp` (bool: TCP使用フラグ)
  - `ConnectionType` (string計算プロパティ: "TCP"/"UDP")
  - `IsBinary` (bool)
  - `FrameVersion` (FrameVersion列挙型: Frame3E/Frame4E)

**TimeoutConfig** (タイムアウト設定専用):
- プロパティ例:
  - `ConnectTimeoutMs` (int)
  - `SendTimeoutMs` (int)
  - `ReceiveTimeoutMs` (int)

**変換フロー:**
```
Excel設定ファイル (*.xlsx)
  ↓ ConfigurationLoaderExcel.LoadFromExcel()
PlcConfiguration
  ↓ ★変換処理（✅ 実装済み）
ConnectionConfig + TimeoutConfig
  ↓ PlcCommunicationManagerコンストラクタ
PlcCommunicationManager
```

→ **✅ PlcConfigurationからConnectionConfig/TimeoutConfigへの変換処理は実装済み**
   - 実装場所1: `ApplicationController.cs:92-110` (ExecuteStep1InitializationAsync内)
   - 実装場所2: `ExecutionOrchestrator.cs:186-199` (ExecuteMultiPlcCycleAsync_Internal内)

**変換処理の実装例:**
```csharp
var connectionConfig = new ConnectionConfig
{
    IpAddress = config.IpAddress,
    Port = config.Port,
    UseTcp = config.ConnectionMethod == "TCP",
    IsBinary = config.IsBinary
};

var timeoutConfig = new TimeoutConfig
{
    ConnectTimeoutMs = config.Timeout,
    SendTimeoutMs = config.Timeout,
    ReceiveTimeoutMs = config.Timeout
};
```
