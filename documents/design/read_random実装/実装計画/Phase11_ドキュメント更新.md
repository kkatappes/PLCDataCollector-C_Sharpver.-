# Phase11: ドキュメント更新

## ステータス
⏳ **未着手** - Phase1-10完了後に実施予定

## 概要
ReadRandom(0x0403)実装に伴い、すべての設計書・実装記録・運用ガイドを最新状態に更新します。

## 前提条件
- ✅ Phase1-10完了: 全機能実装・テスト・実機確認・クリーンアップ完了

---

## 実装ステップ

### ステップ32: 設計書の更新

#### 更新対象ドキュメント

1. **`documents/design/クラス設計.md`**

```markdown
## SlmpFrameBuilder

**責務**: SLMPフレームの構築

**主要メソッド**:
- `BuildReadRandomRequest()`: ReadRandom(0x0403)フレーム構築 ✅ **実装済み**
  - 引数: `List<DeviceSpecification> devices, string frameType, ushort timeout`
  - 戻り値: `byte[]`（送信フレーム）
  - 機能: 不連続デバイスを一括読み出しするフレームを構築
  - 対応フレーム: 3E/4E
  - 最大デバイス数: 192点（サブコマンド0x0000）

- `BuildReadRequest()`: Read(0x0401)フレーム構築 ⚠️ **廃止予定** ※Phase10の方針による
  - 引数: `DeviceCode deviceCode, int startDeviceNumber, ushort readCount, string frameType, ushort timeout`
  - 戻り値: `byte[]`
  - 機能: 連続デバイスを読み出すフレームを構築（旧方式）

**関連クラス**:
- `DeviceSpecification`: デバイス指定情報
- `DeviceConstants`: デバイスコード定義
```

2. **`documents/design/フレーム構築方法.md`**

```markdown
# SLMPフレーム構築方法

## ReadRandom(0x0403)フレーム構築

### 概要
ReadRandom(0x0403)は、飛び飛びの（不連続な）デバイスアドレスのデータを一度に読み出すコマンドです。
異なるデバイス種別（D, M, W等）を混在して指定可能です。

### フレーム構造（3Eフレーム）

```
総バイト数: 2 + 9 + 6 + (4 × デバイス点数)

[サブヘッダ] 2バイト
  0x50, 0x00

[固定ヘッダ] 9バイト
  ネットワーク番号(1) + 局番(1) + I/O番号(2) + マルチドロップ(1) + データ長(2) + タイマ(2)

[コマンド部] 6バイト
  コマンド(2): 0x03 0x04
  サブコマンド(2): 0x00 0x00
  ワード点数(1)
  Dword点数(1): 0x00

[デバイス指定部] 4バイト × 点数
  各デバイス: デバイス番号(3バイト、LE) + デバイスコード(1バイト)
```

### 実装例

```csharp
var devices = new List<DeviceSpecification>
{
    new DeviceSpecification(DeviceCode.D, 100),
    new DeviceSpecification(DeviceCode.D, 105),
    new DeviceSpecification(DeviceCode.M, 200)
};

byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", timeout: 32);
```

### SLMP仕様書参照
- pdf2img/page_63.png: Entry Monitor Device（ReadRandomコマンド）
- pdf2img/page_64.png: ReadRandom制約事項
```

3. **`documents/design/各ステップio.md`**

```markdown
## Step1-2: 設定読み込み・フレーム構築

### 入力
- appsettings.json（設定ファイル）
  - FrameVersion: "3E" or "4E"
  - Timeout: タイムアウト時間（ms）
  - Devices: デバイスリスト
    - DeviceType: "D", "M", "W"等
    - DeviceNumber: デバイス番号（10進）
    - IsHexAddress: 16進アドレスかどうか

### 処理
1. ConfigurationLoader.LoadPlcConnectionConfig()
   - appsettings.jsonを読み込み
   - TargetDeviceConfigオブジェクトを構築

2. ConfigToFrameManager.BuildReadRandomFrameFromConfig()
   - デバイスリストをDeviceSpecificationのリストに変換
   - SlmpFrameBuilder.BuildReadRandomRequest()を呼び出し

3. SlmpFrameBuilder.BuildReadRandomRequest()
   - 3E/4Eフレームヘッダを構築
   - ReadRandomコマンド部を構築（0x0403）
   - デバイス指定部を構築（4バイト×点数）
   - データ長を動的計算

### 出力
- byte[]（送信フレーム）
  - 3Eフレーム: 2 + 9 + 6 + (4 × 点数) バイト
  - 4Eフレーム: 6 + 9 + 6 + (4 × 点数) バイト

### 実装クラス
- `ConfigurationLoader`
- `ConfigToFrameManager`
- `SlmpFrameBuilder`
- `DeviceSpecification`
```

---

### ステップ33: 実装記録の作成

#### 作成対象ドキュメント

1. **`documents/implementation_records/method_records/BuildReadRandomRequest_実装記録.md`**

```markdown
# BuildReadRandomRequest実装記録

## 実装日
2025-11-14

## 実装者
Claude Code

## 概要
SlmpFrameBuilder.BuildReadRandomRequest()メソッドの実装記録です。
ReadRandom(0x0403)コマンドのフレームを構築する機能を実装しました。

## 実装方針

### 選択した実装方法
1. **デバイス指定にDeviceSpecificationクラスを使用**
   - 理由: 型安全性、バイト変換ロジックのカプセル化
   - トレードオフ: オブジェクト生成のオーバーヘッド < 保守性向上

2. **3E/4Eフレーム両対応**
   - 理由: 実機環境の多様性に対応
   - トレードオフ: コード複雑化 < 汎用性向上

3. **データ長の動的計算**
   - 理由: デバイス点数が可変のため
   - トレードオフ: 計算処理 < ハードコード保守リスク

### 検討した他の方法
1. **ハードコードされたバイト配列**
   - メリット: シンプル、高速
   - デメリット: 保守性が低い、デバイス変更に弱い
   - 結論: 却下（conmoni_testのアンチパターン）

2. **文字列ベースのデバイス指定**
   - メリット: 設定ファイルとの親和性
   - デメリット: 型安全性がない、パース処理が必要
   - 結論: 却下（DeviceSpecificationクラスを採用）

## 発生した問題と解決過程

### 問題1: データ長計算の誤差
**問題**: 初期実装でデータ長が2バイト不足していた
**原因**: ワード点数・Dword点数（2バイト）を計算に含めていなかった
**解決**: コマンド部の6バイト（コマンド2+サブコマンド2+点数2）を含めるよう修正

### 問題2: conmoni_testとのバイト配列不一致
**問題**: 生成したフレームがconmoni_testと1バイトずれる
**原因**: 4Eフレームのシーケンス番号（2バイト）の位置ミス
**解決**: SLMP仕様書を再確認し、サブヘッダ直後にシーケンス番号を配置

## テスト結果
- 単体テスト: 21テスト全パス（100%成功率）
- conmoni_test互換性テスト: 213バイトフレーム完全一致 ✅
- データ長動的計算テスト: 1, 10, 48, 100デバイスで成功 ✅

## パフォーマンス
- 48デバイスフレーム構築: 平均0.5ms
- メモリ使用量: 約1KB/フレーム

## 今後の改善点
- Dwordアクセス対応（現在はワードアクセスのみ）
- ビットデバイスの16点単位パディング処理
- フレームバッファリングによる高速化
```

2. **`documents/implementation_records/method_records/DeviceSpecification_実装記録.md`**

```markdown
# DeviceSpecification実装記録

## 実装日
2025-11-14

## 実装者
Claude Code

## 概要
DeviceSpecificationクラスの実装記録です。
デバイス指定情報をカプセル化し、バイト変換ロジックを提供します。

## 実装方針

### 選択した実装方法
1. **イミュータブルなプロパティ設計**
   - 理由: データ整合性の保証
   - トレードオフ: 柔軟性 < 安全性

2. **Equals()とGetHashCode()のオーバーライド**
   - 理由: Dictionary<DeviceSpecification, ushort>のキーとして使用
   - トレードオフ: 実装量増加 < コレクション操作の正確性

3. **ValidateForReadRandom()による事前検証**
   - 理由: 実行時エラーを早期発見
   - トレードオフ: 検証処理 < エラー対応コスト

## テスト結果
- 単体テスト: 28テスト全パス（100%成功率）
- conmoni_test互換性テスト: D61000, W0x0118AAのバイト変換完全一致 ✅

## 今後の改善点
- デバイス番号の範囲検証強化（機種別上限値対応）
- ビットデバイスの点数単位検証（16点単位）
```

3. **`documents/implementation_records/progress_notes/2025-11-14_ReadRandom実装進捗.md`**

```markdown
# 2025-11-14: ReadRandom実装進捗

## 作業概要
Phase1（基礎定義）とPhase2（フレーム構築）を完了しました。

## Phase1完了: DeviceCode、DeviceSpecification実装
- DeviceCode列挙型: 15種類のデバイスコード定義
- DeviceCodeExtensions: IsHexAddress(), IsBitDevice(), IsReadRandomSupported()
- DeviceSpecification: バイト変換、検証、コレクション対応
- 単体テスト: 78テスト全パス（100%成功率）

## Phase2完了: SlmpFrameBuilder実装
- BuildReadRandomRequest(): 134行、3E/4E両対応
- データ長動的計算機能
- 単体テスト: 21テスト全パス（100%成功率）
- conmoni_test互換性: 213バイトフレーム完全一致

## 次のステップ
- Phase3: 設定読み込み統合（後回し）
- Phase4: 通信マネージャーの修正（優先）
- Phase5: レスポンス処理の修正

## 所感
- TDD手法が効果的（Red→Green→Refactorサイクル）
- conmoni_testとの互換性検証が重要
- SLMP仕様書の詳細確認が必須
```

---

### ステップ34: README・運用ガイドの更新

#### 更新対象ドキュメント

1. **`README.md`**

```markdown
# andon - PLC通信システム

## 概要
三菱電機PLCとのSLMP通信によるデータ収集システムです。
ReadRandom(0x0403)コマンドを使用し、飛び飛びのデバイスを効率的に読み出します。

## 特徴
- ✅ **ReadRandom(0x0403)対応**: 不連続デバイスを1回の通信で読み出し
- ✅ **複数デバイス種別対応**: D, M, W等を混在して指定可能
- ✅ **3E/4Eフレーム両対応**: 様々なPLC機種に対応
- ✅ **型安全な実装**: DeviceCode列挙型、DeviceSpecificationクラス
- ✅ **高パフォーマンス**: 通信回数を大幅削減
- ✅ **CSV出力**: タイムスタンプ付きデータ記録

## クイックスタート

### 1. 設定ファイル編集

`appsettings.json`を編集し、PLC接続情報とデバイスリストを設定します。

```json
{
  "PlcConnection": {
    "IpAddress": "192.168.1.10",
    "Port": 5010,
    "FrameVersion": "3E",
    "Timeout": 8000,
    "Devices": [
      {
        "DeviceType": "D",
        "DeviceNumber": 100,
        "Description": "生産数カウンタ"
      },
      {
        "DeviceType": "M",
        "DeviceNumber": 200,
        "Description": "運転状態フラグ"
      }
    ]
  }
}
```

### 2. ビルド・実行

```bash
dotnet build
dotnet run
```

### 3. 出力確認

- CSV出力: `output/data.csv`
- ログ出力: `logs/app.log`

## 設定ガイド

### デバイス指定方法

#### 10進デバイス（D, M, SD, SM等）
```json
{
  "DeviceType": "D",
  "DeviceNumber": 100
}
```

#### 16進デバイス（W, X, Y, B, ZR等）
```json
{
  "DeviceType": "W",
  "DeviceNumber": 4522,
  "IsHexAddress": true,
  "Description": "W0x11AA相当"
}
```

### フレームタイプ

- **3E**: 標準フレーム（推奨）
- **4E**: シーケンス番号付きフレーム

### タイムアウト設定

```json
{
  "Timeout": 8000  // ミリ秒単位（デフォルト: 8秒）
}
```

## トラブルシューティング

### エラー: "デバイス点数が上限を超えています"
- **原因**: 192点以上のデバイスを指定している
- **対策**: デバイス数を192点以下に減らす

### エラー: "PLCからエラー応答を受信しました: エンドコード=0xC051"
- **原因**: デバイス番号が範囲外
- **対策**: デバイス番号を確認（D0-D65535等）

### エラー: "ReadRandomでは指定できないデバイスです: TC"
- **原因**: ReadRandomで使用できないデバイスを指定
- **対策**: タイマコイル（TC）等は使用不可、代わりにタイマ現在値（TN）を使用

## アーキテクチャ

詳細は`documents/design/クラス設計.md`を参照してください。

```
ConfigurationLoader → ConfigToFrameManager → SlmpFrameBuilder
                                                      ↓
PlcCommunicationManager ← ← ← ← ← ← ← ← ← ← ← (送信フレーム)
         ↓
   (PLC通信)
         ↓
SlmpDataParser → ProcessedResponseData → DataOutputManager
                                                      ↓
                                                  CSV出力
```

## ライセンス
MIT License

## 参考資料
- [SLMP通信仕様書](documents/SLMP仕様書/)
- [設計書](documents/design/)
- [実装記録](documents/implementation_records/)
```

2. **`TROUBLESHOOTING.md`**（新規作成）

```markdown
# トラブルシューティングガイド

## 目次
1. [設定エラー](#設定エラー)
2. [通信エラー](#通信エラー)
3. [パース処理エラー](#パース処理エラー)
4. [パフォーマンス問題](#パフォーマンス問題)

## 設定エラー

### エラー: "デバイスリストが空です"
**原因**: appsettings.jsonのDevicesリストが空または未定義

**対策**:
```json
{
  "PlcConnection": {
    "Devices": [
      {
        "DeviceType": "D",
        "DeviceNumber": 100
      }
    ]
  }
}
```

### エラー: "未対応のデバイス種別: ZZZ"
**原因**: 存在しないデバイス種別を指定

**対応デバイス種別**:
- D, M, W, X, Y, B, ZR, R, SD, SM, TN, CN

## 通信エラー

### エラー: "Connection refused"
**原因**: PLCに接続できない

**チェック項目**:
1. PLCの電源が入っているか
2. IPアドレスが正しいか
3. ポート番号が正しいか（通常5010）
4. ネットワーク接続が正常か
5. ファイアウォールでブロックされていないか

### エラー: "Timeout"
**原因**: PLCからの応答がタイムアウト

**対策**:
1. Timeout値を増やす（デフォルト8000ms → 15000ms等）
2. PLC負荷を確認
3. デバイス点数を減らす

## パース処理エラー

### エラー: "エンドコード異常: 0xC051"
**原因**: PLCがデバイス範囲エラーを返した

**対策**:
- デバイス番号が範囲内か確認
- D0-D65535、M0-M8191等

### エラー: "フレーム長が不正"
**原因**: 受信フレームのサイズが期待値と異なる

**対策**:
1. フレームタイプ（3E/4E）が正しいか確認
2. デバイス点数が設定と一致しているか確認
3. Wiresharkでフレーム内容を確認

## パフォーマンス問題

### 問題: "通信が遅い"
**対策**:
1. デバイス点数を適切に設定（最大192点）
2. タイムアウト値を最適化
3. ネットワーク帯域を確認

### 問題: "メモリ使用量が多い"
**対策**:
1. データ出力のバッファリング設定を見直し
2. ログレベルを下げる（Debug → Info）
```

---

## 完了条件
- ✅ documents/design/クラス設計.md更新完了
- ✅ documents/design/フレーム構築方法.md更新完了
- ✅ documents/design/各ステップio.md更新完了
- ✅ documents/implementation_records/method_records/作成完了
- ✅ documents/implementation_records/progress_notes/作成完了
- ✅ README.md更新完了
- ✅ TROUBLESHOOTING.md作成完了
- ✅ 全ドキュメントが最新状態

## 次のアクション
Phase11完了後、本プロジェクト（ReadRandom移行）は完了です。
実機での安定運用を継続してください。

---

**作成日**: 2025-11-18
**元ドキュメント**: read_to_readrandom_migration_plan.md
