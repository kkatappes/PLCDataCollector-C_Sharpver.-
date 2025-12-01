# Step3 SendFrameAsync テスト実装用情報（TC024）

## ドキュメント概要

### 目的
このドキュメントは、TC024_SendFrameAsync_不正フレームテストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係
- `エラーハンドリング.md` - 例外処理・エラーメッセージ設計

#### 実装参考（PySLMPClient）
- `PySLMPClient/pyslmpclient/const.py` - SLMP定数・列挙型定義
- `PySLMPClient/pyslmpclient/__init__.py` - SLMPクライアント実装
- `PySLMPClient/pyslmpclient/util.py` - フレーム作成ユーティリティ
- `PySLMPClient/tests/test_main.py` - テストケース実例

---

## 1. テスト対象メソッド仕様

### SendFrameAsync（Step4: PLCリクエスト送信）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- SLMPフレーム（string型、16進数文字列形式）
  - 正常なフレーム例: "54001234000000010401006400000090E8030000"
  - **不正なフレーム例（TC024で検証）**:
    - 空文字列: ""
    - null値: null
    - 不正な16進数: "ZZ0012340000"
    - 長さ不正: "5400" (短すぎる)
    - フォーマット異常: "54001234000000010401006400000090E803000G" (非16進数文字含む)

#### Output
- Task (送信完了状態)
- 成功時: Taskが正常完了
- 失敗時: 例外スロー
  - **ArgumentException**: 不正なフレーム形式（TC024で検証）
  - ArgumentNullException: null入力時
  - FormatException: 16進数変換失敗時
  - InvalidOperationException: 未接続状態時
  - TimeoutException: 送信タイムアウト時
  - SocketException: ソケットエラー時

#### 機能
- READコマンド(0104)送信
- SLMPフレーム形式検証
- ソケット経由でのPLC通信
- 送信前フレーム妥当性検証
- エラー時の適切な例外スロー

---

## 2. テストケース仕様（TC024）

### TC024_SendFrameAsync_不正フレーム
**目的**: 不正なSLMPフレーム入力時の例外ハンドリング機能をテスト

#### 前提条件
- ConnectAsyncが成功済み（接続状態: Connected）
- ソケットが正常に確立済み
- PLC（Q00UDPCPU）が通信可能状態

#### 入力データ（不正フレーム）
**不正パターン1: 空文字列**
```
入力: ""
期待例外: ArgumentException
期待メッセージ: "SLMPフレームが空です。"
```

**不正パターン2: null値**
```
入力: null
期待例外: ArgumentNullException
期待メッセージ: "SLMPフレームがnullです。"
```

**不正パターン3: 不正な16進数文字**
```
入力: "54001234000000010401006400000090E803000G"
期待例外: FormatException
期待メッセージ: "SLMPフレームに不正な16進数文字が含まれています: G"
```

**不正パターン4: 長さ不正（短すぎる）**
```
入力: "5400"
期待例外: ArgumentException
期待メッセージ: "SLMPフレーム長が不正です。最小長: 16文字、実際長: 4文字"
```

**不正パターン5: 奇数長（16進数文字列は偶数長である必要）**
```
入力: "54001234000000010401006400000090E80300001"
期待例外: ArgumentException
期待メッセージ: "SLMPフレーム長は偶数である必要があります。実際長: 41文字"
```

#### 期待動作
1. **フレーム検証**: 入力フレームの基本検証を実行
2. **例外スロー**: 不正検出時に適切な例外をスロー
3. **エラーメッセージ**: 具体的で分かりやすいエラーメッセージを提供
4. **リソース保護**: 不正入力による接続状態への悪影響を防止

#### 検証項目
1. **null・空文字列検証**:
   - null入力時のArgumentNullException
   - 空文字列入力時のArgumentException

2. **16進数形式検証**:
   - 非16進数文字検出（G, H, Z等）
   - FormatExceptionの適切なスロー

3. **長さ検証**:
   - 最小長（16文字）未満の検出
   - 奇数長の検出（16進数は偶数長必須）

4. **エラーメッセージ精度**:
   - 具体的な問題箇所の指摘
   - 修正のヒントを含むメッセージ

5. **接続状態保持**:
   - 不正フレーム送信試行後も接続状態維持
   - ソケットリソースへの悪影響なし

---

## 3. SLMP正常フレーム構造（参考）

### 4Eフレーム/ASCIIフォーマット要求構造
**実機テスト設定**: Q00UDPCPUとの通信で使用

#### M000-M999読み込み要求フレーム
```
正常フレーム例: "54001234000000010401006400000090E8030000"

フィールド構成:
- サブヘッダ: "5400" (4Eフレーム識別)
- ネットワーク番号: "12"
- PC番号: "34"
- 要求先ユニットIO番号: "0000"
- 予備: "00"
- 要求先プロセッサ番号: "01"
- コマンド: "0401" (バッチ読み出し)
- サブコマンド: "0000"
- データ長: "1400" (20バイト、リトルエンディアン)
- デバイスコード: "90" (M機器)
- 開始番号: "E80300" (M000、リトルエンディアン)
- デバイス点数: "0000" (1000点、リトルエンディアン)
```

#### フレーム長要件
- **最小長**: 32文字（16バイト×2）
- **実際の長さ**: コマンド種別により可変
- **偶数長必須**: 16進数文字列のため

---

## 4. 例外ハンドリング詳細

### ArgumentException
**発生条件**:
- 空文字列入力
- 長さ不正（短すぎる、奇数長）
- フレーム構造異常

**メッセージ例**:
```csharp
// 空文字列
"SLMPフレームが空です。"

// 長さ不正
"SLMPフレーム長が不正です。最小長: {0}文字、実際長: {1}文字"

// 奇数長
"SLMPフレーム長は偶数である必要があります。実際長: {0}文字"
```

### ArgumentNullException
**発生条件**:
- null入力

**メッセージ例**:
```csharp
"SLMPフレームがnullです。"
```

### FormatException
**発生条件**:
- 非16進数文字含有
- 16進数変換失敗

**メッセージ例**:
```csharp
"SLMPフレームに不正な16進数文字が含まれています: {0}"
```

---

## 5. テスト実装方針（TDD）

### 開発手法
- C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.mdに記載のTDD手法を使用

### テストファイル配置
- **ファイル名**: PlcCommunicationManagerTests.cs
- **配置先**: Tests/Unit/Core/Managers/
- **名前空間**: andon.Tests.Unit.Core.Managers

### テスト実装順序
1. **TC021_SendFrameAsync_正常送信**（前提テスト）
2. **TC022_SendFrameAsync_全機器データ取得**（前提テスト）
3. **TC023_SendFrameAsync_未接続状態**（前提テスト）
4. **TC024_SendFrameAsync_不正フレーム**（本テスト、最優先）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockSocket**: Socket通信のモック（接続済み状態）
- **MockPlcCommunicationManager**: 部分モック

#### 使用するスタブ
- **InvalidSlmpFrameStubs**: 不正フレームパターンのスタブ
- **ExceptionMessageValidator**: 例外メッセージ検証用ヘルパー

---

## 6. テストケース実装構造

### Arrange（準備）
1. **接続済みPlcCommunicationManager作成**:
   - MockSocket注入（Connected状態）
   - 必要な依存関係設定

2. **不正フレームパターン準備**:
   - 空文字列: ""
   - null値: null
   - 不正16進数: "54001234000000010401006400000090E803000G"
   - 短すぎる: "5400"
   - 奇数長: "54001234000000010401006400000090E80300001"

3. **期待例外・メッセージ準備**:
   - 各パターンに対応する例外タイプ
   - 期待されるエラーメッセージ

### Act & Assert（実行・検証）
各不正パターンに対して:

```csharp
[Test]
public async Task SendFrameAsync_不正フレーム_ArgumentException()
{
    // Arrange
    var invalidFrame = "";

    // Act & Assert
    var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
        plcCommManager.SendFrameAsync(invalidFrame)
    );

    Assert.That(ex.Message, Is.EqualTo("SLMPフレームが空です。"));
}
```

### 検証項目詳細
1. **例外タイプ**: 期待される例外が確実にスローされる
2. **例外メッセージ**: 具体的で有用なメッセージが含まれる
3. **接続状態**: 例外後も接続状態が維持される
4. **リソース**: ソケット等のリソースが正常状態を保つ

---

## 7. エラーメッセージ統一

### 定数定義
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // SendFrameAsync関連
    public const string SlmpFrameNull = "SLMPフレームがnullです。";
    public const string SlmpFrameEmpty = "SLMPフレームが空です。";
    public const string SlmpFrameLengthInvalid = "SLMPフレーム長が不正です。最小長: {0}文字、実際長: {1}文字";
    public const string SlmpFrameLengthOdd = "SLMPフレーム長は偶数である必要があります。実際長: {0}文字";
    public const string SlmpFrameInvalidHex = "SLMPフレームに不正な16進数文字が含まれています: {0}";

    // 接続関連
    public const string NotConnected = "PLC接続が確立されていません。";
}
```

---

## 8. ログ出力要件

### LoggingManager連携
- エラー発生時のフレーム内容記録
- 例外詳細のログ出力
- デバッグ時の検証結果記録

### ログレベル
- **Error**: 例外発生時
- **Warning**: フレーム自動修正時（将来拡張）
- **Debug**: フレーム検証詳細

---

## 9. テスト実装チェックリスト

### TC024実装前
- [ ] PlcCommunicationManagerクラス作成（空実装）
- [ ] SendFrameAsyncメソッドシグネチャ定義
- [ ] 例外クラス準備（ArgumentException等）
- [ ] ErrorMessagesクラス作成
- [ ] MockSocket作成

### TC024実装中
- [ ] Arrange: 不正フレームパターン準備
- [ ] Act & Assert: 空文字列パターン
- [ ] Act & Assert: null値パターン
- [ ] Act & Assert: 不正16進数パターン
- [ ] Act & Assert: 長さ不正パターン
- [ ] Act & Assert: 奇数長パターン

### TC024実装後
- [ ] テスト実行・Red確認
- [ ] SendFrameAsync本体実装（フレーム検証ロジック）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC021-TC023との整合性確認

---

## 10. 参考情報

### SLMP仕様書
- フレーム構造: 4Eフレーム/ASCIIフォーマット準拠
- コマンド体系: バッチ読み出し(0401)、書き込み(1401)等
- エラーコード: 応答時のエラー判定用

---

以上が TC024_SendFrameAsync_不正フレームテスト実装に必要な情報です。