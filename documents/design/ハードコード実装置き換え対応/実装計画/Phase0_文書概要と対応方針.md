# ハードコード置き換え実装計画 - Phase 0: 文書概要と対応方針

**作成日**: 2025-11-28
**対象**: andonプロジェクト

---

## 0. 文書の目的と構成

### 0.1 本文書の位置づけ

本文書は、**現在の実装状況を正確に反映**した完全版記録文書です。
- 当初計画していた実装方針と、実際の実装状況の差異を明確化
- 今後の実装に必要な全情報を集約（既定値、検証ルール、実装例など）
- 設計文書との整合性確認
- この文書だけで実装可能な完全な情報を提供

### 0.2 主要な差異サマリー

| 項目 | 当初計画 | 実装状況 |
|-----|---------|---------|
| **設定保持クラス** | PlcConfiguration単独 | PlcConfiguration + TargetDeviceConfig + PlcConnectionConfig の3クラス併用 |
| **FrameVersion/Timeout** | PlcConfigurationに実装予定 | TargetDeviceConfig（SLMP単位）とPlcConnectionConfig（ミリ秒単位）に実装済み |
| **Excel読み込み** | B10-B15セルから拡張項目取得 | B8, B9, B11-B13のみ読み込み中 |
| **ConfigToFrameManager** | PlcConfigurationのみ使用 | 2つのオーバーロード（TargetDeviceConfig版 / PlcConfiguration版）|
| **Phase 1-2** | 未実装 | 代替実装（TargetDeviceConfig使用） |
| **Phase 3** | 未実装 | TargetDeviceConfig版のみ完了 |
| **デフォルト値の違い** | 単一の既定値 | クラス毎に異なる設計意図（3E/4E、ミリ秒/SLMP単位）|

---

## 1. ハードコード対応方針（既定値・検証ルール）

### 1.1 対応対象項目一覧

現在のコード内で以下の値がハードコードされている可能性があります:

| 項目 | ハードコード値例 | 対応方針 |
|-----|---------------|---------|
| **IPAddress** | "127.0.0.1" | 既定値なし。ExcelファイルB8セルから取得された値のみ使用（必須項目） |
| **Port** | 8192 | 既定値なし。ExcelファイルB9セルから取得された値のみ使用（必須項目） |
| **ConnectionMethod** | "UDP" | 既定値: "UDP"。ExcelファイルB10セルから取得された値で更新（省略可） |
| **FrameVersion** | "3E" | 既定値: "4E"。ExcelファイルB11セルから取得された値で更新（省略可） |
| **Timeout** | 8000 | 既定値: 1000 (1秒)。ExcelファイルB12セルから取得された値で更新（省略可） |
| **IsBinary** | false | 既定値: true (Binary形式)。ExcelファイルB13セルから取得された値で更新（省略可） |
| **MonitoringIntervalMs** | 5000 | 既定値: 1000 (1秒)。ExcelファイルB14セルから取得された値で更新（省略可） |
| **PlcId** | "PLC_001" | 既定値なし。設定ファイルの「IP_ポート番号」を自動生成して使用 |
| **PlcName** | "ライン1_設備A" | 既定値なし（省略可）。ExcelファイルB15セルから取得、未設定時はPlcIdを使用 |
| **Priority** | 5 | **不要** (削除対象) |
| **ConnectTimeoutMs** | 5000 | 既定値: 3000 (3秒)。TimeoutConfig設定で更新可能 |
| **SendTimeoutMs** | 3000 | 既定値: 500 (0.5秒)。TimeoutConfig設定で更新可能 |
| **ReceiveTimeoutMs** | 5000 | 既定値: 500 (0.5秒)。TimeoutConfig設定で更新可能 |
| **SendIntervalMs** | 100 | 既定値: 100 (0.1秒)。TimeoutConfig設定で更新可能 |

**重要な補足説明**:

1. **IsBinary**: 現状のデフォルト値false（ASCII形式）を、true（Binary形式）に変更
   - 既定値変更により、設定ファイルで明示的に指定しない限りBinary形式を使用
   - 理由: Binary形式の方が高効率で一般的

2. **MonitoringIntervalMs** (データ取得間隔): 現状5000ms（5秒）のデフォルト値を1000ms（1秒）に変更
   - 現在は下記2箇所で重複定義されている:
     - DataProcessingConfig.cs:11 - プロパティのデフォルト値 `= 5000`
     - DependencyInjectionConfigurator.cs:27 - DI設定時の明示的設定 `= 5000`
   - 対応方針: 両方とも1000msに変更し、Excelファイルから取得された値で更新可能にする

3. **TimeoutConfig項目** (接続・送受信タイムアウト):
   - **ConnectTimeoutMs**: 既定値3000ms（3秒）
   - **SendTimeoutMs**: 既定値500ms（0.5秒）
   - **ReceiveTimeoutMs**: 既定値500ms（0.5秒）
   - **SendIntervalMs**: 既定値100ms（0.1秒）
   - これらの既定値は厳守すること。TimeoutConfig.csで定義され、設定で上書き可能

### 1.2 項目分類詳細

#### 1.2.1 必須項目（既定値なし）

これらの項目は必ずExcelファイルから取得する必要があります:

##### 1. IPAddress
- **既定値**: なし
- **検証**: 有効なIPv4アドレス形式であること
- **検証詳細**:
  - IPv4形式: xxx.xxx.xxx.xxx
  - 各オクテット: 0～255
  - "0.0.0.0"は不許可
- **例**: "192.168.1.10", "172.30.40.15"

##### 2. Port
- **既定値**: なし
- **検証**: 1～65535の範囲内
- **一般的なPLCポート**: 5000, 8192
- **例**: 8192, 5000

##### 3. PlcId
- **既定値**: なし
- **自動生成ルール**: `{IPAddress}_{Port}`
- **例**: "192.168.1.10_8192"
- **用途**: ログ出力、データ識別子として使用
- **検証**: 自動生成のため検証不要

#### 1.2.2 オプション項目（既定値あり）

これらの項目はExcelファイルに値がない場合、既定値を使用します:

##### 1. ConnectionMethod
- **既定値**: "UDP"
- **許可値**: "TCP" または "UDP"
- **検証**: 大文字小文字を区別しない
- **エラー例**: "ConnectionMethodの値が不正です: 'HTTP' (許可値: TCP, UDP)"

##### 2. FrameVersion
- **既定値**: "4E"
- **許可値**: "3E" または "4E"
- **検証**: 大文字小文字を区別しない
- **エラー例**: "FrameVersionの値が不正です: '5E' (許可値: 3E, 4E)"

##### 3. Timeout
- **既定値**: 1000 (1秒)
- **単位**: ミリ秒
- **検証**: 正の整数であること
- **推奨範囲**: 100～30000 (0.1秒～30秒)
- **推奨値**: 1000～10000 (1秒～10秒)
- **SLMP変換**: ミリ秒を250ms単位に変換（例: 8000ms → 32単位）
- **エラー例**: "Timeoutの値が範囲外です: 50 (推奨範囲: 100～30000)"

##### 4. IsBinary
- **既定値**: true (Binary形式)
- **許可値**: true または false
- **検証**: ブール値であること
- **補足**: Excelファイルに値がない場合はBinary形式を使用
- **"1"/"0"形式のサポート**: "1"はtrue、"0"はfalseとして扱う
- **エラー例**: "IsBinaryの値が不正です: 'invalid' (許可値: true, false)"

##### 5. MonitoringIntervalMs (データ取得間隔)
- **既定値**: 1000 (1秒)
- **単位**: ミリ秒
- **検証**: 正の整数であること
- **推奨範囲**: 100～60000 (0.1秒～60秒)
- **補足**: Excelファイルから取得された値で更新可能
- **エラー例**: "MonitoringIntervalMsの値が範囲外です: 50 (推奨範囲: 100～60000)"

##### 6. ConnectTimeoutMs (接続タイムアウト)
- **既定値**: 3000 (3秒)
- **単位**: ミリ秒
- **検証**: 正の整数であること
- **推奨範囲**: 1000～30000 (1秒～30秒)
- **補足**: TimeoutConfig設定で更新可能
- **エラー例**: "ConnectTimeoutMsの値が範囲外です: 500 (推奨範囲: 1000～30000)"

##### 7. SendTimeoutMs (送信タイムアウト)
- **既定値**: 500 (0.5秒)
- **単位**: ミリ秒
- **検証**: 正の整数であること
- **推奨範囲**: 100～10000 (0.1秒～10秒)
- **補足**: TimeoutConfig設定で更新可能
- **エラー例**: "SendTimeoutMsの値が範囲外です: 50 (推奨範囲: 100～10000)"

##### 8. ReceiveTimeoutMs (受信タイムアウト)
- **既定値**: 500 (0.5秒)
- **単位**: ミリ秒
- **検証**: 正の整数であること
- **推奨範囲**: 100～10000 (0.1秒～10秒)
- **補足**: TimeoutConfig設定で更新可能
- **エラー例**: "ReceiveTimeoutMsの値が範囲外です: 50 (推奨範囲: 100～10000)"

##### 9. SendIntervalMs (送信間隔)
- **既定値**: 100 (0.1秒)
- **単位**: ミリ秒
- **検証**: 正の整数であること
- **推奨範囲**: 10～5000 (0.01秒～5秒)
- **補足**: TimeoutConfig設定で更新可能。PLC側の処理能力に応じて調整
- **エラー例**: "SendIntervalMsの値が範囲外です: 5 (推奨範囲: 10～5000)"

#### 1.2.3 オプション項目（既定値なし - Excelから取得のみ）

以下の項目はExcelファイルに値がある場合のみ使用します:

##### 1. PlcName ("ライン1_設備A")
- **既定値**: なし（省略可能）
- **用途**: ログ出力、表示用の識別名として使用
- **補足**: 未設定の場合はPlcIdを使用
- **最大長**: 100文字（推奨）

#### 1.2.4 削除対象項目

以下の項目はシステムで使用しないため削除します:

##### 1. Priority (5)
- **理由**: 優先度制御機能が未実装
- **将来的に必要になる場合は再検討**

### 1.3 Excelファイルのレイアウト

#### 推奨レイアウト

```
| 項目名              | 値                  | 備考                                    |
|--------------------|---------------------|-----------------------------------------|
| IPAddress          | 192.168.1.10        | (必須)                                   |
| Port               | 8192                | (必須)                                   |
| ConnectionMethod   | UDP                 | (省略時: UDP)                            |
| FrameVersion       | 4E                  | (省略時: 4E)                             |
| Timeout            | 5000                | (省略時: 1000) 単位: ミリ秒               |
| IsBinary           | true                | (省略時: true) Binary/ASCII形式切替       |
| MonitoringIntervalMs| 1000               | (省略時: 1000) 単位: ミリ秒 データ取得間隔 |
| PlcName            | ライン1_設備A        | (省略時: PlcIdを使用) 表示用識別名         |
```

#### 実際のセルマッピング（計画）

| セル | 項目名 | 備考 |
|------|-------|------|
| B8 | IPAddress | 必須 |
| B9 | Port | 必須 |
| B10 | ConnectionMethod | 省略可（既定値: UDP） |
| B11 | FrameVersion | 省略可（既定値: 4E） |
| B12 | Timeout | 省略可（既定値: 1000ms） |
| B13 | IsBinary | 省略可（既定値: true） |
| B14 | MonitoringIntervalMs | 省略可（既定値: 1000ms） |
| B15 | PlcName | 省略可（未設定時はPlcIdを使用） |

### 1.4 エラーメッセージ一覧

#### 必須項目の欠落
```
"必須項目 'IPAddress' が設定ファイルに存在しません。"
"必須項目 'Port' が設定ファイルに存在しません。"
```

#### 検証エラー
```
"IPAddressの形式が不正です: '999.999.999.999'"
"Portの値が範囲外です: 70000 (許可範囲: 1～65535)"
"ConnectionMethodの値が不正です: 'HTTP' (許可値: TCP, UDP)"
"FrameVersionの値が不正です: '5E' (許可値: 3E, 4E)"
"Timeoutの値が範囲外です: 50 (推奨範囲: 100～30000)"
"IsBinaryの値が不正です: 'invalid' (許可値: true, false)"
"MonitoringIntervalMsの値が範囲外です: 50 (推奨範囲: 100～60000)"
"ConnectTimeoutMsの値が範囲外です: 500 (推奨範囲: 1000～30000)"
"SendTimeoutMsの値が範囲外です: 50 (推奨範囲: 100～10000)"
"ReceiveTimeoutMsの値が範囲外です: 50 (推奨範囲: 100～10000)"
"SendIntervalMsの値が範囲外です: 5 (推奨範囲: 10～5000)"
```

---

---

## 更新履歴

| 日付 | 更新内容 |
|------|---------|
| 2025-11-28 | 初版作成 |
| 2025-11-28 | Phase 4実装完了を記録 |
| 2025-11-28 | Phase 5実装完了を記録 |

**Phase 4実装完了**（2025-11-28）:
- ConfigToFrameManagerのハードコード完全削除
- PlcConfigurationに既定値設定（FrameVersion="4E", Timeout=1000ms）
- タイムアウト変換ロジック実装（ConvertTimeoutMsToSlmpUnit）
- Phase 4専用テスト7個追加（全成功）
- 既存テスト785個保護（784個成功）
- TDD原則完全準拠（Red-Green-Refactor）
- 実装結果文書: `実装結果/Phase4_既存コード修正_TestResults.md`

**Phase 5実装完了**（2025-11-28）:
- 統合テスト9個実装（全成功）
- PlcConfigurationのデフォルト値設定完了（ConnectionMethod, IsBinary, MonitoringIntervalMs）
- Phase 1-4の統合確認完了
- マジックナンバー定数化（リファクタリング）
- 既存テスト794個保護（793個成功）
- TDD原則完全準拠（Red-Green-Refactor）
- 実装結果文書: `実装結果/Phase5_統合テスト_TestResults.md`

**ハードコード置き換え対応完了**（2025-11-28）:
- ✅ Phase 1: DefaultValues.cs（既定値定義）
- ✅ Phase 2: ConfigurationLoaderExcel拡張（設定読み込み）
- ✅ Phase 3: SettingsValidator.cs（検証ロジック）
- ✅ Phase 4: ConfigToFrameManager.cs（ハードコード削除）
- ✅ Phase 5: 統合テスト

---

**以上**
