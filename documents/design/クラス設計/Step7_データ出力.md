# Step7: データ出力

## 機能概要

解析後データの出力（ファイル/ネットワーク）

## 詳細機能

### CSV出力機能
- データ取得元：PlcCommunicationManager.ParseRawToStructuredData()（構造化データ）
- 出力形式：CSV（カンマ区切り、UTF-8 BOM付き）
- 出力先：ファイルシステム（指定パス）

### データ転送機能（オプション）
- データ取得元：PlcCommunicationManager.ParseRawToStructuredData()（構造化データ）
- 転送先：ネットワーク上の別システム
- 転送条件：DataTransferConfig.EnableTransfer有効時

---

## クラス：CsvFileWriter

### 機能概要

CSV形式でのデータ出力管理

**実装場所:**
- Andon.Utilities.CsvFileWriter

**設計目的:**
- データ永続化の責任分離
- ファイルI/Oの一元管理
- エラーハンドリングの集約

---

### WriteToFileAsync（Step7: CSV出力）

**機能：** 構造化データのCSVファイル出力

**Input:**
- StructuredData（構造化データ：PlcCommunicationManager.ParseRawToStructuredData()から取得）
- DataProcessingConfig（TargetName, ContinuousDataMode, DataRetentionDays：ConfigToFrameManager.LoadConfigAsync()から取得）
- 出力先パス（string型：設定ファイルまたはデフォルトパス）

**Output:**
- WriteResult（出力結果オブジェクト）
  - OutputPath（string型：実際の出力ファイルパス）
  - BytesWritten（long型：書き込んだバイト数）
  - RecordsWritten（int型：書き込んだレコード数）
  - WriteTime（TimeSpan型：書き込みにかかった時間）
  - Success（bool型：出力成功フラグ）
  - ErrorMessage（string型、null許容：エラーメッセージ）

**処理内容:**

1. **ファイル名生成**
   - ContinuousDataMode無効時：タイムスタンプベース（`TargetName_yyyyMMdd_HHmmss.csv`）
   - ContinuousDataMode有効時：固定ファイル名（`TargetName_continuous.csv`）

2. **CSV形式変換**
   - ヘッダー行：デバイス名リスト（カンマ区切り）
   - データ行：タイムスタンプ + デバイス値（カンマ区切り）
   - エンコーディング：UTF-8 BOM付き（Excel互換性）

3. **ファイル出力**
   - ContinuousDataMode無効時：新規ファイル作成（上書きモード）
   - ContinuousDataMode有効時：追記モード（既存データ保持）

4. **エラーハンドリング**
   - ディスクフル検出
   - アクセス権限エラー
   - パス不正エラー

**データ取得元:**
- PlcCommunicationManager.ParseRawToStructuredData()（構造化データ）
- ConfigToFrameManager.LoadConfigAsync()（出力設定）

---

### CleanupOldFilesAsync（古いファイル削除）

**機能：** 保持期間を過ぎたファイルの自動削除

**Input:**
- DataProcessingConfig（DataRetentionDays：ConfigToFrameManager.LoadConfigAsync()から取得）
- 対象ディレクトリパス（string型）

**Output:**
- CleanupResult（削除結果オブジェクト）
  - DeletedFiles（string[]型：削除されたファイルパス一覧）
  - DeletedCount（int型：削除されたファイル数）
  - FreedSpaceKB（long型：解放されたディスク容量）
  - Success（bool型：処理成功フラグ）
  - ErrorMessage（string型、null許容：エラーメッセージ）

**処理内容:**
- ファイル作成日時チェック
- DataRetentionDaysを超過したファイルを削除
- ディスク容量統計の更新

**実行タイミング:**
- アプリケーション起動時（オプション）
- 定期実行（バックグラウンドタスク）
- ディスク容量警告時

**データ取得元:**
- ConfigToFrameManager.LoadConfigAsync()（データ保持期間設定）

---

## クラス：DataTransferManager（オプション機能）

### 機能概要

構造化データのネットワーク転送管理

**実装場所:**
- Andon.Utilities.DataTransferManager

**設計目的:**
- ネットワーク転送の責任分離
- 転送プロトコルの抽象化
- リトライ・エラー処理の集約

---

### TransferDataAsync（データ転送）

**機能：** 構造化データの別システムへの転送

**Input:**
- StructuredData（構造化データ：PlcCommunicationManager.ParseRawToStructuredData()から取得）
- DataTransferConfig（EnableTransfer, DestinationIpAddress, DestinationPort：ConfigToFrameManager.LoadConfigAsync()から取得）

**Output:**
- TransferResult（転送結果オブジェクト）
  - Success（bool型：転送成功フラグ）
  - BytesTransferred（long型：転送されたバイト数）
  - TransferTime（TimeSpan型：転送にかかった時間）
  - Destination（string型：転送先アドレス）
  - ErrorMessage（string型、null許容：エラーメッセージ）

**処理内容:**
- データシリアライゼーション（JSON/バイナリ等）
- ネットワーク送信（TCP/UDP/HTTP等）
- 送信確認・リトライ処理
- タイムアウト管理

**実行条件:**
- DataTransferConfig.EnableTransfer = true

**エラーハンドリング:**
- ネットワーク接続エラー
- タイムアウトエラー
- データ形式エラー

**データ取得元:**
- PlcCommunicationManager.ParseRawToStructuredData()（構造化データ）
- ConfigToFrameManager.LoadConfigAsync()（転送設定）

---

## 成功条件

- 解析したデータを任意の場所/形式に出力/保存できる

---

## データ取得元

- PlcCommunicationManager.ParseRawToStructuredData()（構造化データ）
- ConfigToFrameManager.LoadConfigAsync()（出力設定）
