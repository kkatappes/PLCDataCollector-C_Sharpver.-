# Step6: 受信データ解析

## 機能概要

受信データ解析（DWordの場合は事前に結合処理ステップを入れる）

## 詳細機能

### 生データ(16進数)to構造化データ変換機能（2段階処理）
- Step6-1 基本後処理：PlcCommunicationManager.ProcessReceivedRawData()（16進数パース・基本型変換、1回実行）
- Step6-2 構造化変換：PlcCommunicationManager.ParseRawToStructuredData()（SLMP構造化解析）
- データ取得元：PlcCommunicationManager.ReceiveResponseAsync()（受信生データ）
- デバイス要求情報：ConfigToFrameManager.PrepareDeviceRequestInfo()（デバイス要求情報）
- 変換対象：Random READレスポンス（ビット・ワード・ダブルワード混在データ）の段階的構造化
- **応答統合処理不要**：1回の受信で全デバイスデータ取得完了のため

### データ破損時の対応方針（継続実行モード）
- ParseRawToStructuredDataで壊れたデータを検出
- エラーログ出力後、次サイクル継続
- 正常データのみ処理、異常データは破棄

---

## クラス：PlcCommunicationManager

### ProcessReceivedRawData（Step6-1: 受信データ基本後処理）

**機能：** 受信生データの基本パース・型変換

**Input:**
- Step4で受信した生データ(16進数：PlcCommunicationManager.ReceiveResponseAsync()から取得)
- ProcessedDeviceRequestInfo（デバイス要求情報：ConfigToFrameManager.PrepareDeviceRequestInfo()から取得）

**Output:**
- ProcessedResponseData（基本後処理結果オブジェクト）
  - 基本結果: 元生データ、処理済みデータ（デバイス名キー構造）、処理時刻
  - エラー情報: エラーフラグ、エラー・警告メッセージリスト
  - 統計情報: 処理デバイス数（ビット・ワード・ダブルワード別）

**処理対象:**
- Random READコマンド(0403)レスポンスデータ（ビット・ワード・ダブルワード混在データ）

**責任範囲:**
- 生データの16進数パース
- デバイス別データ抽出（ビット・ワード・ダブルワード混在）
- 基本的な型変換（ビット/ワード/ダブルワード）
- エラー検証・記録

**処理内容:**
- Random READコマンド応答パース（全デバイスデータ、既にDWord形式）
- 1回の処理で全デバイス（M000-M999、D000-D999）のデータを抽出
- デバイス名キー構造の辞書に変換（例: {"M000": true, "D000": 0x1234, "D002": 0x56781234}）

**データ取得元:**
- PlcCommunicationManager.ReceiveResponseAsync()（受信生データ）
- ConfigToFrameManager.PrepareDeviceRequestInfo()（デバイス要求情報）

**旧MergeResponseDataの機能を統合:**
- 応答統合処理が不要なため、ProcessReceivedRawDataで完結

---

### ParseRawToStructuredData（Step6-2: 構造化データ変換）

**機能：** SLMP構造化解析・詳細情報付与

**Input:**
- 処理済み受信データ（ProcessedResponseData：PlcCommunicationManager.ProcessReceivedRawData()から取得）

**Output:**
- StructuredData（SLMP構造化解析結果オブジェクト）
  - 基本構造化データ: SLMPヘッダー（全標準情報）、終了コード、デバイスデータ、受信時刻、エラーフラグ
  - 解析詳細情報: 解析手順記録、解釈情報、処理時間、デバイス解釈、ステータス判定
  - エラー詳細情報: 詳細エラーコード、エラー説明、影響デバイス（エラー時のみ）

**データ取得元:**
- PlcCommunicationManager.ProcessReceivedRawData()（処理済みデータ）

**旧Step6-3から繰り上げ:**
- 応答統合処理（旧Step6-2）が不要なため

---

## クラス：SlmpDataParser（データ変換ユーティリティ）

### 機能概要

PlcCommunicationManagerの肥大化防止（現在2340行）、単一責務原則の遵守

**実装場所:**
- Andon.Utilities.SlmpDataParser（静的クラス）

**設計目的:**
- 通信管理とデータ変換の分離
- 他クラスでの再利用可能性確保
- 独立したテスト実施の容易性

**データ取得元:**
- Python PySLMPClientのutil.py相当機能

---

### メソッド一覧

#### DecodeBcd(byte[] data)
**機能：** 4bit BCD配列のデコード

**Input:** BCDエンコードされたバイト配列

**Output:** デコード後のバイト配列

**処理例:** [0x12, 0x34] → [1, 2, 3, 4]

---

#### UnpackBits(byte[] data)
**機能：** ビット配列の展開、LSBから順

**Input:** ビットパックされたバイト配列

**Output:** 展開後のbool配列

**処理例:** [0x85] → [true, false, true, false, false, false, false, true]

---

#### HexStringToBytes(string hexString)
**機能：** 16進数文字列からバイト列への変換

**Input:** 16進数文字列（偶数長）

**Output:** バイト配列

**処理例:** "1A2B" → [0x1A, 0x2B]

---

#### BytesToHexString(byte[] data)
**機能：** バイト配列から16進数文字列への変換

**Input:** バイト配列

**Output:** 16進数文字列（大文字）

**処理例:** [0x1A, 0x2B] → "1A2B"

---

#### ExtractWordDwordData(byte[] buffer, int splitPos)
**機能：** Word/Dwordデータの分離抽出

**Input:** 入力バイト配列、分割位置（Wordデータの終了位置）

**Output:** (Dwordデータリスト, Wordデータリスト)のタプル

**処理内容:** バイナリデータを2バイトデータ（Word）と4バイトデータ（Dword）に分離

---

#### ParseAsciiHex(string asciiData, int startIndex, int length)
**機能：** ASCII形式の応答データから数値を抽出

**Input:** ASCII形式データ文字列、開始インデックス、抽出する文字数

**Output:** 16進数として解釈した整数値

---

#### ParseAsciiBitData(string asciiData)
**機能：** ASCIIビットデータのパース

**Input:** ASCIIビットデータ文字列（'0'/'1'）

**Output:** bool配列

**処理例:** "0110" → [false, true, true, false]

---

#### ParseAsciiWordData(string asciiData)
**機能：** ASCIIワードデータのパース

**Input:** ASCIIワードデータ文字列（4文字単位）

**Output:** ushort配列

**処理例:** "12AB34CD" → [0x12AB, 0x34CD]

---

#### ParseBinaryBitData(byte[] binaryData, int count)
**機能：** Binaryビットデータのパース

**Input:** バイナリビットデータ、ビット数

**Output:** bool配列

**処理内容:** BCDデコード後、ビット値に変換（奇数個の場合、最後の余分なビットを削除）

---

#### ParseBinaryWordData(byte[] binaryData)
**機能：** Binaryワードデータのパース

**Input:** バイナリワードデータ

**Output:** ushort配列

**処理内容:** バイナリデータを直接ushort配列として解釈（リトルエンディアン）

---

### 使用箇所

- PlcCommunicationManager.ProcessReceivedRawData()（Step6-1: 受信データ基本後処理）
  - ASCII/Binary形式のパース処理
  - 3E/4Eフレーム対応
  - ビット/ワード/ダブルワード各データ型のパース処理

**実装参考:**
- C:\Users\1010821\Desktop\python\andon\documents\design\フレーム構築関係\受信データパース処理仕様.md

**フレーム構造参考:**
- C:\Users\1010821\Desktop\python\andon\documents\design\フレーム構築関係\フレーム構築方法.md
  - **重要**: 4Eフレーム応答には監視タイマフィールドが存在しない
  - 終了コード位置: オフセット13-14（監視タイマなし）
  - デバイスデータ開始位置: オフセット15

---

## 成功条件

- PLCから受信したデータ(16進数)から正しい内容の構造化データに変換できること
- DWordの情報を読み取る場合は受信データの結合→構造化データ変換のステップが正しく動作すること（※現在はRandom READにより自動処理）
- データ破損等の一時的な異常でもアプリケーション停止せず次サイクルで回復すること
