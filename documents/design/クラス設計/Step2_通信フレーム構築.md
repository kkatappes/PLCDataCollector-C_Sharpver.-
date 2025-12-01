# Step2: 通信フレーム構築

## 機能概要

通信フレームの構築（3E/4E、ASCII/Binary対応）
DWordの場合は事前に分割処理ステップを入れる

## 詳細機能

### Random READ用デバイスリスト生成機能
- 設定ファイルから各デバイスのビット/ワード数情報を取得
- 全デバイス（ビット・ワード・ダブルワード）を単一リストに統合

### 通信フレーム構築機能
- 対応形式：3E/4E、ASCII/Binary
- データ取得元：ConfigToFrameManager.LoadConfigAsync()（接続設定・デバイス設定）

---

## クラス：ConfigToFrameManager

### PrepareDeviceRequestInfo（Step2-1: デバイス要求情報準備）

**機能：** Random READ用デバイスリスト生成

**Input:**
- TargetDeviceConfig（MDeviceRange, DDeviceRange, DataType：ConfigToFrameManager.LoadConfigAsync()から取得）

**Output:**
- デバイス要求情報（Random READ用デバイス指定リスト）
- ProcessedDeviceRequestInfo（RandomReadDeviceList）

**処理内容：**

【全デバイスをRandom READ用リストに統合】
- 設定ファイルから各デバイスのビット/ワード数情報を取得
- 全デバイス（ビット・ワード・ダブルワード）を単一リストに統合
- Random READフレーム用のデバイス指定リストを作成

**処理例：**

M000-999（ビット機器）+ D000-999（ワード機器）→ RandomReadDeviceList:
```
[
  {DeviceCode: "90", DeviceNumber: 0, WordCount: 0},     // M000（ビット）
  {DeviceCode: "90", DeviceNumber: 1, WordCount: 0},     // M001（ビット）
  ...
  {DeviceCode: "90", DeviceNumber: 999, WordCount: 0},   // M999（ビット）
  {DeviceCode: "A8", DeviceNumber: 0, WordCount: 1},     // D000（1ワード）
  {DeviceCode: "A8", DeviceNumber: 1, WordCount: 1},     // D001（1ワード）
  {DeviceCode: "A8", DeviceNumber: 2, WordCount: 2},     // D002（2ワード=DWord）
  {DeviceCode: "A8", DeviceNumber: 4, WordCount: 1},     // D004（1ワード）
  ...
]
```
※ WordCount=0はビットデバイスを示す

**データ取得元：**
- ConfigToFrameManager.LoadConfigAsync()（デバイス設定）
- SLMP仕様書（Random READ仕様）

---

### BuildReadRandomFrameFromConfig（Step2-2: 通信フレーム構築 - Binary形式）

**機能：** Random READ専用の通信フレーム構築（Binary形式）

**別名メソッド:**
- BuildReadRandomFrameFromConfigAscii() - ASCII形式専用

**Input:**
- ConnectionConfig（IpAddress, Port, UseTcp, ConnectionType, IsBinary, FrameVersion：ConfigToFrameManager.LoadConfigAsync()から取得）
- ProcessedDeviceRequestInfo（デバイス要求情報：ConfigToFrameManager.PrepareDeviceRequestInfo()から取得）
  - RandomReadDeviceList（Random READ用デバイス指定リスト：全デバイス統合済み）

**Output:**
- 生成したSLMPフレーム（16進数文字列、1つのフレーム）

**構築コマンド：**
- Random READコマンド(0403)のみ: 全デバイス（ビット・ワード・ダブルワード）一括取得

**対象機器：**

全機器をRandom READで処理
- ビット機器（M, X, Y, L, F, V, B, TS, TC）: WordCount=0で指定
- ワード機器（D, W, SD, TN）: WordCount=1または2で指定

**最適化効果：**
- **DWord分割・結合処理完全削除**（Random READが自動処理）
- **応答データ統合処理不要**（1回の受信で全デバイス取得完了）
- 設定ファイルから直接Random READフレーム生成
- 通信回数最小化（1回のみ）
- 処理ステップ大幅簡素化

**データ取得元：**
- ConfigToFrameManager.LoadConfigAsync()（接続設定）
- ConfigToFrameManager.PrepareDeviceRequestInfo()（デバイス要求情報）
- SLMP仕様書
  - Random READ仕様：pdf2img/page_53-55
  - レスポンスフレーム構造：pdf2img/page_20-23

---

## 実機テスト設定フレーム例

### 4Eフレーム/ASCII形式（M000-M999/D000-D999一括取得）

**フレーム: 全デバイス一括読み出し（Random READ）**
- コマンド: Random READ（0403）
- サブコマンド: 0000（ビット・ワード・ダブルワード全対応）
- バイナリ形式（参考）: `54 00 12 34 00 00 00 [データ長] 04 03 00 00 [ビット点数] [ワード点数] [ダブルワード点数] [デバイス指定リスト]`

**デバイス指定例:**
```
ビットアクセス点数: 03E8（1000点 = M000-M999）
ワードアクセス点数: 0003（3点）
ダブルワードアクセス点数: 0001（1点）
ビットデバイス1: 90 00 00 00 00（M000）
ビットデバイス2: 90 00 01 00 00（M001）
... (M000-M999まで)
ワードデバイス1: A8 00 00 00 00（D000、1ワード）
ワードデバイス2: A8 00 01 00 00（D001、1ワード）
ワードデバイス3: A8 00 04 00 00（D004、1ワード）
ダブルワードデバイス1: A8 00 02 00 00（D002、2ワード=DWord）
```

**構成:**
サブヘッダ(4E) + Random READコマンド(0403) + サブコマンド(0000) + ビット点数 + ワード点数 + ダブルワード点数 + 各デバイス指定（デバイスコード + デバイス番号 + ワード数）

---

## ASCII変換規則（実機テスト設定）

- 各バイトを2文字の16進数ASCII文字列に変換
- 例: `54H` → `"35"(5) + "34"(4)` → `"3534"`
- データ量: バイナリの2倍（通信時間・帯域幅に注意）

---

## データ長計算方式の設計選択

### 送信時

**方式：** 事前設定方式（設定ファイルのデバイス範囲情報を使用）
- 設定ファイルから取得したデバイス点数を使用してフレームを構築
- 例: MDeviceRange="M000-M999" → 1000点として要求フレーム生成

### 受信時（推奨方式）

**方式：** フレーム解析方式（PySLMPClient準拠の真の動的計算）
- 受信フレームのヘッダーから`length`フィールドを動的に読み取る

#### Binary形式
9バイト目から2バイトのlengthフィールド（リトルエンディアン）
```
レスポンスフレーム構造:
[サブヘッダ(2B)] [シーケンス(2B)] [予約(2B)] [ネットワーク情報(5B)] [Length(2B)] [終了コード(2B)] [データ部(Length-2B)]
                                                                        ^^^^^^^^
                                                                  ここから長さを読み取る
```

#### ASCII形式
10-13文字目の4文字の16進数文字列
```
レスポンスフレーム構造（ASCII）:
"D400" [8文字] [18文字] [4文字(Length)] [4文字(終了コード)] [データ部]
                        ^^^^^^^^^^^^^^
                    ここから長さを読み取る（16進数4桁）
```

### 実装メリット

1. 設定不要: フレーム自体に含まれる長さ情報を使用
2. 可変長対応: PLCが返す実際のデータ長に自動対応
3. 堅牢性: 設定ミスによるデータ切り捨て/読み過ぎを防止
4. 汎用性: 様々なPLC機種・設定に柔軟に対応

### 参考: 他方式との比較

#### 事前設定方式（ConMoni等）
- 設定ファイルの点数情報を受信時にも使用
- 問題点: 設定と実際の応答が異なる場合にエラーが発生
- 柔軟性: 低（設定変更が必要）

#### 本実装採用方式
- フレームヘッダーのlengthフィールドを使用（PySLMPClient準拠）
- 利点: 堅牢性・保守性が高い、設定ミスの影響を受けにくい
- 柔軟性: 高（自動対応）

---

## 成功条件

- Step1で取得した値から正しい内容のフレームを生成できること
- DWordの情報を読み取る場合は分割→フレーム作成のステップが正しく動作すること（※現在はRandom READにより自動処理）
