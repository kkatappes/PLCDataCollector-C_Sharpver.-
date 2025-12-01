# Step3〜6 PlcCommunicationManager メタプロンプトチェックリスト

---

## 🚨 **19時deadline対応 - 優先実施テスト一覧** 🚨

**完了期限**: 2025-11-06 19:00（日本時間）
**対象**: 18テスト（Phase 1: 10テスト + Phase 2: 8テスト）
**最小成功基準**: 7テスト（★マーク付き）

---

### ⭐ **Phase 1: 基本動作確認（10テスト）** - 推定100-150分

#### 1-1. 接続系（3テスト）
- [x] **★ TC017**: ConnectAsync_TCP接続成功 ✅ （10-15分）
- [x] **★ TC018**: ConnectAsync_UDP接続成功 ✅ （10-15分）
- [x] TC027(Disconnect): DisconnectAsync_正常切断 ✅ （8-12分）

#### 1-2. 送受信系（3テスト）
- [x] **★ TC021**: SendFrameAsync_正常送信 ✅ （10-15分）
- [x] TC022: SendFrameAsync_全機器データ取得 ✅ （12-18分）
- [x] **★ TC025**: ReceiveResponseAsync_正常受信 ✅ （10-15分）✅ 実装用プロンプト作成済み

#### 1-3. Step6データ処理系（4テスト）
- [x] **★ TC029**: ProcessReceivedRawData_基本後処理成功 ✅ （12-18分）
- [x] **★ TC032**: CombineDwordData_DWord結合処理成功 ✅ （12-18分）
- [x] **★ TC037**: ParseRawToStructuredData_3Eフレーム解析 ✅ （12-18分）
- [x] TC038: ParseRawToStructuredData_4Eフレーム解析 ✅ （12-18分）

---

### ⭐ **Phase 2: 連続動作確認（8テスト）** - 推定80-120分

#### 2-1. Step3-5連続動作（2テスト）
- [x] TC115: Step3to5_TCP完全サイクル正常動作 ✅ （12-18分）
- [x] TC116: Step3to5_UDP完全サイクル正常動作 ✅ （12-18分）✅ 実装用プロンプト作成済み

#### 2-2. Step6連続処理（2テスト）
- [x] TC118: Step6_ProcessToCombinetoParse連続処理 ✅ （12-18分）✅ 実装用プロンプト作成済み
- [x] TC119: Step6_各段階データ伝達整合性 ✅ （12-18分）✅ 実装用プロンプト作成済み

#### 2-3. Step3-6完全サイクル（4テスト）
- [x] **★★★ TC121**: FullCycle_接続から構造化まで完全実行 ⭐最重要⭐ ✅ （15-20分）
- [x] TC122: FullCycle_複数サイクル実行時統計累積 ✅ （12-18分）✅ 実装用プロンプト作成済み
- [x] TC123: FullCycle_エラー発生時の適切なスキップ ✅ （12-18分）✅ 実装用プロンプト作成済み
- [x] TC124: ErrorPropagation_Step3エラー時後続スキップ ✅ （10-15分）✅ 実装用プロンプト作成済み

---

### ✅ **最小成功基準（これだけは必須）** - ★マーク7テスト

- [x] **★ TC017**: TCP接続成功 ✅
- [x] **★ TC021**: 正常送信 ✅
- [x] **★ TC025**: 正常受信 ✅
- [x] **★ TC029**: 基本処理成功 ✅
- [x] **★ TC032**: DWord結合成功 ✅
- [x] **★ TC037**: 構造化成功 ✅
- [x] **★★★ TC121**: 完全サイクル実行成功 ⭐最重要⭐ ✅

**注意**: TC121成功で「通信→データ取得→処理完了」を実証可能

---

### 📅 推奨スケジュール

```
12:30-14:00 (90分)   Phase 1前半 - 接続・送受信（5テスト）
14:00-15:30 (90分)   Phase 1後半 - データ処理系（5テスト） + 休憩
15:30-17:00 (90分)   Phase 2前半 - Step3-5, Step6連続処理（4テスト）
17:00-18:30 (90分)   Phase 2後半 - 完全サイクル（4テスト）
18:30-19:00 (30分)   最終確認・バッファ
```

---

## 📋 メタプロンプト作成手順

**目的**
C:\Users\1010821\Desktop\python\andon\documents\design\テスト内容.mdの各項目のメタプロンプトを作成したいです
C:\Users\1010821\Desktop\python\andon\documents\design\実装用プロンプトを読み込んでこの内容の粒度の情報をまとめドットMDファイルを作成してください

**手順**
１．C:\Users\1010821\Desktop\python\andon\documents\design\実装用プロンプト　の中を確認し該当のテスト番号があれば、この文書のチェックリストにチェックを入れて終了
２．該当のテスト番号がなければ目的の内容を満たすファイルを作成
３．この文書該当部分のチェックリストにチェックを入れ次の項目に進む

**注意**
・作業が中途半端になると嫌なので目盛りのコンパクティングかかる前にユーザーに連絡してください
・C:\Users\1010821\Desktop\python\andon\documents\design\実装用プロンプト\★step3_テスト実装用プロンプト_TC021.mdの内容の粒度は最低限確保すること　**勝手に複数ファイルをまとめたり、中身を簡略化しないこと！**

---

## 2.1 ConnectAsync メソッドテスト

### 正常系
- [x] TC017_ConnectAsync_TCP接続成功
- [x] TC017_1_ConnectAsync_ソケットタイムアウト設定確認
- [x] TC018_ConnectAsync_UDP接続成功

### 異常系
- [x] TC019_ConnectAsync_接続タイムアウト
- [x] TC020_ConnectAsync_接続拒否
- [x] TC020_1_ConnectAsync_不正IPアドレス
- [x] TC020_2_ConnectAsync_不正ポート番号
- [x] TC020_3_ConnectAsync_null入力
- [x] TC020_4_ConnectAsync_既に接続済み状態での再接続
- [x] TC020_5_ConnectAsync_接続時間計測精度

---

## 2.2 SendFrameAsync メソッドテスト

### 正常系
- [x] TC021_SendFrameAsync_正常送信
- [x] TC022_SendFrameAsync_全機器データ取得

### 異常系
- [x] TC023_SendFrameAsync_未接続状態
- [x] TC024_SendFrameAsync_不正フレーム

---

## 2.3 ReceiveResponseAsync メソッドテスト

### 正常系
- [x] TC025_ReceiveResponseAsync_正常受信 ✅ 実装用プロンプト作成済み

### 異常系
- [x] TC026_ReceiveResponseAsync_受信タイムアウト

---

## 2.4 DisconnectAsync メソッドテスト

### 正常系
- [x] TC027_DisconnectAsync_正常切断

### 異常系
- [x] TC028_DisconnectAsync_未接続状態切断

---

## 2.5 ProcessReceivedRawData メソッドテスト（Step6-1）

### 正常系
- [x] TC029_ProcessReceivedRawData_基本後処理成功 ✅ 完了（step6_TC029.md）
- [ ] TC030_ProcessReceivedRawData_混合データ型基本処理

### 異常系
- [ ] TC031_ProcessReceivedRawData_不正生データ

---

## 2.6 CombineDwordData メソッドテスト（Step6-2）

### 正常系
- [x] TC032_CombineDwordData_DWord結合処理成功 ✅ 完了（step6_TC032.md）
- [ ] TC033_CombineDwordData_結合不要データ処理
- [ ] TC034_CombineDwordData_混合データ型結合処理

### 異常系
- [ ] TC035_CombineDwordData_基本後処理未実行
- [ ] TC036_CombineDwordData_DWord結合エラー

---

## 2.7 ParseRawToStructuredData メソッドテスト（Step6-3）

### 正常系
- [x] TC037_ParseRawToStructuredData_3Eフレーム解析 ✅ 完了（step6_TC037.md）
- [x] TC038_ParseRawToStructuredData_4Eフレーム解析 ✅ 完了（step6_TC038.md）
- [ ] TC039_ParseRawToStructuredData_DWord結合済みデータ解析

### 異常系
- [ ] TC040_ParseRawToStructuredData_DWord結合未実行
- [ ] TC041_ParseRawToStructuredData_エラー終了コード

---

## 2.8 データ転送オブジェクトメソッドテスト（補助機能）

### ConnectionResponse プロパティ検証
- [ ] TC042_ConnectionResponse_成功時プロパティ検証
- [ ] TC043_ConnectionResponse_失敗時プロパティ検証

### ConnectionStats 統計メソッド
- [ ] TC044_ConnectionStats_AddResponseTime_統計更新
- [ ] TC045_ConnectionStats_IncrementError_エラー統計更新
- [ ] TC046_ConnectionStats_IncrementRetry_リトライ統計更新
- [ ] TC047_ConnectionStats_SuccessRate_成功率計算精度

### BasicProcessedResponseData メソッド
- [ ] TC048_BasicProcessedResponseData_AddBasicProcessedDevice
- [ ] TC049_BasicProcessedResponseData_GetDeviceValue
- [ ] TC050_BasicProcessedResponseData_AddError_AddWarning

### ProcessedResponseData メソッド
- [ ] TC051_ProcessedResponseData_MergeFromBasicData
- [ ] TC052_ProcessedResponseData_GetCombinedDWordDevices
- [ ] TC053_ProcessedResponseData_AddProcessedDevice

### StructuredData メソッド
- [ ] TC054_StructuredData_AddStructuredDevice
- [ ] TC055_StructuredData_AddParseStep
- [ ] TC056_StructuredData_SetErrorDetails

---

## 2.9 リソース管理テスト（補助機能）

### Dispose メソッド
- [ ] TC057_Dispose_初回呼び出し正常動作
- [ ] TC058_Dispose_重複呼び出し安全性
- [ ] TC059_Dispose_using文自動解放

### Disconnect メソッド（同期版）
- [ ] TC060_Disconnect_接続済み状態から同期切断
- [ ] TC061_Disconnect_未接続状態での安全性
- [ ] TC062_Disconnect_内部状態初期化確認

---

## 2.10 Step2連携テスト（インターフェース）

### ProcessedDeviceRequestInfo 連携
- [ ] TC063_Step2toStep6_ProcessedDeviceRequestInfo参照整合性
- [ ] TC064_Step2toStep6_DWord分割情報利用確認
- [ ] TC065_Step2toStep6_機器種別情報利用確認

---

## 2.11 非同期処理・例外ハンドリング連携テスト（補助機能）

### AsyncExceptionHandler 連携
- [ ] TC066_AsyncExceptionHandler_HandleCriticalOperation連携確認
- [ ] TC067_AsyncExceptionHandler_ConnectAsync例外ハンドリング
- [ ] TC068_AsyncExceptionHandler_SendReceive例外ハンドリング
- [ ] TC069_AsyncExceptionHandler_実行結果AsyncOperationResult確認

### CancellationToken 制御
- [ ] TC070_CancellationToken_ConnectAsync中断処理
- [ ] TC071_CancellationToken_SendReceive中断処理
- [ ] TC072_CancellationToken_DisconnectAsync中断処理
- [ ] TC073_CancellationToken_Step3to6連続中断時リソース解放確認

---

## 2.12 進捗報告・セマフォ制御テスト（補助機能）

### ProgressReporter 連携
- [ ] TC074_ProgressReporter_Step3接続進捗報告
- [ ] TC075_ProgressReporter_Step4送受信進捗報告
- [ ] TC076_ProgressReporter_Step6データ処理進捗報告
- [ ] TC077_ProgressReporter_連続サイクル進捗累積確認

### ResourceSemaphoreManager 連携
- [ ] TC078_ResourceSemaphore_ログファイル排他制御
- [ ] TC079_ResourceSemaphore_複数PLC並行実行時競合回避
- [ ] TC080_ResourceSemaphore_タイムアウト時適切解放

---

## 2.13 インターフェース分離・モック置換テスト（インターフェース）

### IPlcCommunicationManager インターフェース分離
- [ ] TC081_IPlcCommunicationManager_インターフェース契約準拠性
- [ ] TC082_IPlcCommunicationManager_モック完全置換可能性
- [ ] TC083_IPlcCommunicationManager_全メソッドシグネチャ互換性

### 他Managerインターフェース連携
- [ ] TC084_ILoggingManager_連携正常動作確認
- [ ] TC085_IErrorHandler_連携正常動作確認
- [ ] TC086_IResourceManager_連携正常動作確認
- [ ] TC087_IConfigToFrameManager_連携正常動作確認
- [ ] TC088_IDataOutputManager_連携正常動作確認

---

## 2.14 ソケット・リソース詳細管理テスト（補助機能）

### Socket リソース管理
- [ ] TC089_Socket_接続成功時適切設定確認
- [ ] TC090_Socket_接続失敗時適切解放確認
- [ ] TC091_Socket_複数回接続切断リソースリーク確認
- [ ] TC092_Socket_異常終了時リソース残存チェック

### IDisposable 実装詳細確認
- [ ] TC093_IDisposable_using文での自動解放確認
- [ ] TC094_IDisposable_明示的Dispose時の動作確認
- [ ] TC095_IDisposable_二重Dispose安全性確認

---

## 2.15 他Managerクラス連携詳細テスト（インターフェース）

### LoggingManager 連携（各ステップ）
- [ ] TC096_LoggingManager_Step3接続状態ログ出力確認
- [ ] TC097_LoggingManager_Step4通信データログ出力確認
- [ ] TC098_LoggingManager_Step5切断統計ログ出力確認
- [ ] TC099_LoggingManager_Step6データ解析ログ出力確認

### ErrorHandler 連携（エラー時）
- [ ] TC100_ErrorHandler_Step3エラー分類記録確認
- [ ] TC101_ErrorHandler_Step4エラー分類記録確認
- [ ] TC102_ErrorHandler_Step6エラー分類記録確認
- [ ] TC103_ErrorHandler_リトライ判定連携確認

### ResourceManager 連携
- [ ] TC104_ResourceManager_Step3to6メモリ使用量影響確認
- [ ] TC105_ResourceManager_連続実行時メモリ推移確認
- [ ] TC106_ResourceManager_警告閾値到達時動作確認

---

## 2.16 設定値動的反映テスト（補助機能）

### MonitoringIntervalMs 動的変更
- [ ] TC107_MonitoringIntervalMs_実行中変更反映確認
- [ ] TC108_MonitoringIntervalMs_TimerService連携確認

### タイムアウト値動的変更
- [ ] TC109_TimeoutConfig_ConnectTimeout変更反映確認
- [ ] TC110_TimeoutConfig_ReceiveTimeout変更反映確認
- [ ] TC111_TimeoutConfig_SendTimeout変更反映確認

---

## 2.17 Step7連携・最終出力確認テスト（インターフェース）

### DataOutputManager 連携
- [ ] TC112_DataOutputManager_StructuredData引き渡し整合性
- [ ] TC113_DataOutputManager_Step6to7データ完全性確認
- [ ] TC114_DataOutputManager_出力エラー時Step3to6影響確認

---

## 2.18 連続動作統合テスト

### Step3→4→5 連続実行
- [x] TC115_Step3to5_TCP完全サイクル正常動作 ✅ 完了（integration_TC115.md）
- [x] TC116_Step3to5_UDP完全サイクル正常動作 ✅ 完了（integration_テスト実装用プロンプト_TC116.md）
- [ ] TC117_Step3to5_統計情報累積確認

### Step6 3段階処理連続実行
- [x] TC118_Step6_ProcessToCombinetoParse連続処理 ✅ 完了（integration_テスト実装用プロンプト_TC118.md）
- [x] TC119_Step6_各段階データ伝達整合性 ✅ 完了（integration_テスト実装用プロンプト_TC119.md）
- [ ] TC120_Step6_エラー情報伝播確認

### Step3→4→5→6 完全サイクル
- [x] TC121_FullCycle_接続から構造化まで完全実行 ✅ 完了（integration_TC121.md）⭐最重要⭐
- [x] TC122_FullCycle_複数サイクル実行時統計累積 ✅ 完了（integration_テスト実装用プロンプト_TC122.md）
- [x] TC123_FullCycle_エラー発生時の適切なスキップ ✅ 完了（integration_テスト実装用プロンプト_TC123.md）

---

## 2.19 エラー伝播・境界値テスト

### エラー伝播
- [x] TC124_ErrorPropagation_Step3エラー時後続スキップ ✅ 完了（integration_テスト実装用プロンプト_TC124.md）
- [ ] TC125_ErrorPropagation_Step6各段階エラー伝播
- [ ] TC126_ErrorPropagation_エラー情報完全性

### 境界値・特殊ケース
- [ ] TC127_BoundaryValue_最小データサイズ処理
- [ ] TC128_BoundaryValue_最大データサイズ処理
- [ ] TC129_BoundaryValue_空データ処理
- [ ] TC130_SpecialCase_並行接続試行

---

## 2.20 統合テスト（Step1-2, Step3-6, エラーハンドリング）

### Step1-2統合テスト
- [ ] TC142_統合_設定読み込み_フレーム構築

### Step3-6統合テスト（詳細）
- [ ] TC143_1_Step3-6基本連続実行テスト
- [ ] TC143_2_Step3-6データ継承精度テスト
- [ ] TC143_3_Step3-6段階的エラーハンドリングテスト
- [ ] TC143_4_Step3-6_DWord処理統合テスト
- [ ] TC143_5_Step3-6タイムアウト制御統合テスト
- [ ] TC143_6_Step3-6リソース管理統合テスト
- [ ] TC143_7_Step3-6パフォーマンス測定統合テスト
- [ ] TC143_8_Step3-6エラー回復・リトライ統合テスト
- [ ] TC143_9_Step3-6並行実行統合テスト（複数PLC対応）
- [ ] TC143_10_Step3-6_M100-M107ビット読み出し2パターン統合テスト（オフライン）

### エラーハンドリング統合テスト
- [ ] TC144_統合_エラーハンドリング_リトライ

---

## テスト進捗サマリー

### 基本メソッドテスト
- **ConnectAsyncメソッド**: 10/10 完了 ✅
- **SendFrameAsyncメソッド**: 4/4 完了 ✅
- **ReceiveResponseAsyncメソッド**: 2/2 完了 ✅
- **DisconnectAsyncメソッド**: 0/2 完了
- **ProcessReceivedRawDataメソッド**: 1/3 完了
- **CombineDwordDataメソッド**: 1/5 完了
- **ParseRawToStructuredDataメソッド**: 1/5 完了

### 補助機能・インターフェーステスト
- **データ転送オブジェクトメソッド**: 0/15 完了
- **リソース管理テスト**: 0/6 完了
- **Step2連携テスト**: 0/3 完了
- **非同期処理・例外ハンドリング連携**: 0/8 完了
- **進捗報告・セマフォ制御**: 0/7 完了
- **インターフェース分離・モック置換**: 0/8 完了
- **ソケット・リソース詳細管理**: 0/7 完了
- **他Managerクラス連携詳細**: 0/11 完了
- **設定値動的反映**: 0/5 完了
- **Step7連携・最終出力確認**: 0/3 完了

### 統合・連続動作テスト
- **連続動作統合テスト**: 0/9 完了
- **エラー伝播・境界値テスト**: 0/7 完了
- **統合テスト（Step1-2, Step3-6詳細, エラーハンドリング）**: 0/12 完了（TC142, TC143_1〜TC143_10, TC144）

**合計進捗**: 21/136 (15.4%)

---

## 作成済みファイル一覧

### 2.1 ConnectAsync (10個)
- `step3_テスト実装用プロンプト_TC017.md`
- `step3_テスト実装用プロンプト_TC017_1.md`
- `step3_テスト実装用プロンプト_TC018.md`
- `step3_テスト実装用プロンプト_TC019.md`
- `step3_テスト実装用プロンプト_TC020.md`
- `step3_テスト実装用プロンプト_TC020_1to5.md` (TC020_1〜5を統合)

### 2.2 SendFrameAsync (4個)
- `step3_テスト実装用プロンプト_TC021.md` (既存)
- `step3_テスト実装用プロンプト_TC022to023.md` (TC022, TC023, TC018_SendFrameAsyncを統合)

### 2.3 ReceiveResponseAsync (2個)
- `step3_テスト実装用プロンプト_TC019_ReceiveResponse.md` (TC025→TC019に修正済み)

### 2.4 DisconnectAsync (0個) ❌未作成
- TC027_028_Disconnect.md（要作成）

### 2.5 ProcessReceivedRawData (1個) ✅作成済み
- `step6_テスト実装用プロンプト_TC029.md` (★重要テスト)

### 2.6 CombineDwordData (1個) ✅作成済み
- `step6_テスト実装用プロンプト_TC032.md` (★重要テスト)

### 2.7 ParseRawToStructuredData (1個) ✅作成済み
- `step6_テスト実装用プロンプト_TC037.md` (★重要テスト)

### 2.18 完全サイクル (1個) ✅作成済み
- TC121_FullCycle.md（完了済み・最重要）

---

## 作業完了サマリー

### ✅ 完了済み作業（2025-11-06更新）
1. **実装用プロンプトフォルダ調査**: 存在ファイルを確認
2. **チェックリスト精査**: 実際の存在状況に基づいてチェック更新
3. **進捗状況修正**: 作成済みファイルのチェック状況を正確に反映
4. **未作成ファイル特定**: TC027_028（DisconnectAsync）とTC066（完全サイクル）が未作成と確認

### 🎯 19時Deadline対応状況
**最小成功基準（★マーク7テスト）のプロンプトファイル準備状況**:
- [x] TC017: TCP接続成功（既存）
- [x] TC021: 正常送信（既存）
- [x] TC025: 正常受信（既存）
- [x] TC029: 基本処理成功（既存）
- [x] TC032: DWord結合成功（既存）
- [x] TC037: 構造化成功（既存）
- [x] TC121: 完全サイクル実行成功（完了済み）← 最重要

**結果**: 最小成功基準のテスト実装プロンプトファイル **7/7完備** ✅完了

### 📋 次回作業（必要に応じて）

#### 🔥 優先度：最高（19時deadline対応）
- **TC121**: FullCycle_接続から構造化まで完全実行 (✅完了・★★★最重要)
- **TC027_028**: DisconnectAsync_正常切断・未接続状態切断

#### 残りの基本テストファイル作成
- TC038: ParseRawToStructuredData_4Eフレーム解析
- TC030: ProcessReceivedRawData_混合データ型基本処理
- TC033: CombineDwordData_結合不要データ処理

#### Phase2連続動作テストファイル作成
- TC115: Step3to5_TCP完全サイクル正常動作 (✅完了)
- TC116: Step3to5_UDP完全サイクル正常動作
- TC118: Step6_ProcessToCombinetoParse連続処理
- TC119: Step6_各段階データ伝達整合性
