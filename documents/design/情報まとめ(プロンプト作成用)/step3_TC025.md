# Step3 ReceiveResponseAsync 正常受信テスト実装用情報（TC025）

## ドキュメント概要

### 目的
このドキュメントは、TC025_ReceiveResponseAsync_正常受信テストの実装に必要な情報を集約したものです。
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

### ReceiveResponseAsync（Step4: PLCレスポンス受信）
**クラス**: PlcCommunicationManager
**名前空間**: andon.Core.Managers

#### Input
- タイムアウト設定（ReceiveTimeoutMs: 3000ms）
- 受信バッファサイズ（標準: 8192バイト）

#### Output
- 成功時: RawResponseData（生レスポンスデータオブジェクト）
  - ResponseData: byte[]型（受信した生バイトデータ）
  - DataLength: int型（受信データサイズ）
  - ReceivedAt: DateTime型（受信完了時刻）
  - ReceiveTime: TimeSpan型（受信処理時間）
  - ResponseHex: string型（16進数文字列表現）
  - FrameType: FrameType列挙型（4E/3E）
  - ErrorMessage: null（成功時はnull）
- 失敗時: 例外スロー
  - TimeoutException: 受信タイムアウト時
  - SocketException: ソケットエラー時
  - InvalidOperationException: 未接続状態での受信試行時

#### 機能
- PLCからのSLMPレスポンス受信
- 受信タイムアウト制御（Socket.ReceiveTimeout使用）
- 受信データのバイト配列変換
- 16進数文字列変換
- フレームタイプ判定（4E/3E）
- 受信時間計測

#### データ取得元
- PLC（実機またはモック）からのレスポンス
- Socket.ReceiveAsync()経由

---

## 2. テストケース仕様（TC025）

### TC025_ReceiveResponseAsync_正常受信
**目的**: PLCレスポンス受信機能が正常に動作することをテスト

#### 前提条件
- PLC（実機またはモック）が応答可能な状態
- PlcCommunicationManagerが接続済み状態（ConnectAsync完了）
- SendFrameAsync完了後（READコマンド送信済み）
- 有効なTimeoutConfig設定

#### 入力データ
**TimeoutConfig**:
- ReceiveTimeoutMs: 3000（受信タイムアウト: 3秒）

**期待受信データ（PLC Mock Response）**:
- M000-M999読み込み応答: 4Eフレーム形式
- ヘッダー部: "D4001234" (4Eフレーム識別)
- データ部: M000-M999の1000点分ビットデータ
- 終端: SLMP終端コード

#### 期待出力
- RawResponseData（受信成功オブジェクト）
  - ResponseData != null（受信バイトデータ配列）
  - ResponseData.Length > 0（受信データサイズが正の値）
  - DataLength == ResponseData.Length（データサイズ一致）
  - ReceivedAt != null（受信時刻記録済み）
  - ReceivedAt.Value <= DateTime.UtcNow（受信時刻が現在時刻以前）
  - ReceiveTime != null（受信処理時間記録済み）
  - ReceiveTime.Value.TotalMilliseconds > 0（受信処理時間が正の値）
  - ReceiveTime.Value.TotalMilliseconds < ReceiveTimeoutMs（タイムアウト以内）
  - ResponseHex != null && !string.IsNullOrEmpty（16進数文字列変換済み）
  - ResponseHex.StartsWith("D4001234")（4Eフレーム形式確認）
  - FrameType == FrameType.Frame4E（フレームタイプ判定正常）
  - ErrorMessage == null（成功時はエラーメッセージなし）

#### 動作フロー成功条件
1. **接続状態チェック**: 接続済み状態であることを確認
2. **受信バッファ準備**: byte[]配列初期化（8192バイト）
3. **受信実行**: Socket.ReceiveAsync実行、ReceiveTimeoutMs適用
4. **データ受信**: 0より大きいサイズのデータ受信完了
5. **時間計測**: 受信処理時間を正確に記録
6. **データ変換**: バイトデータを16進数文字列に変換
7. **フレーム判定**: 先頭バイトから4E/3Eフレーム判定
8. **受信時刻記録**: ReceivedAt設定
9. **戻り値生成**: RawResponseData生成・返却

---

## 3. SLMP受信処理詳細

### 4Eフレーム受信データ構造
**標準4Eレスポンスフレーム**:
1. フレーム識別: "D4001234"（8バイト、16進数）
2. データ長: レスポンスデータサイズ（4バイト）
3. 終了コード: "0000"（正常終了、4バイト）
4. データ部: 実際のPLCデータ（可変長）

### C# Socket受信実装
```csharp
// 受信バッファ準備
byte[] buffer = new byte[8192];
var startTime = DateTime.UtcNow;

// タイムアウト制御付き受信
var cts = new CancellationTokenSource(timeoutConfig.ReceiveTimeoutMs);

try
{
    int receivedBytes = await socket.ReceiveAsync(
        new ArraySegment<byte>(buffer),
        SocketFlags.None,
        cts.Token
    );

    var receiveTime = DateTime.UtcNow - startTime;

    // 受信データ処理
    byte[] responseData = new byte[receivedBytes];
    Array.Copy(buffer, responseData, receivedBytes);

    // 16進数文字列変換
    string responseHex = Convert.ToHexString(responseData);

    // フレームタイプ判定
    FrameType frameType = responseHex.StartsWith("D4001234")
        ? FrameType.Frame4E
        : FrameType.Frame3E;

    return new RawResponseData
    {
        ResponseData = responseData,
        DataLength = receivedBytes,
        ReceivedAt = DateTime.UtcNow,
        ReceiveTime = receiveTime,
        ResponseHex = responseHex,
        FrameType = frameType,
        ErrorMessage = null
    };
}
catch (OperationCanceledException)
{
    throw new TimeoutException($"受信タイムアウト（タイムアウト時間: {timeoutConfig.ReceiveTimeoutMs}ms）");
}
catch (SocketException ex)
{
    throw new SocketException($"受信エラー: {ex.Message}");
}
```

---

## 4. 依存クラス・設定

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000（接続タイムアウト）
    public int SendTimeoutMs { get; set; }        // 例: 3000（送信タイムアウト）
    public int ReceiveTimeoutMs { get; set; }     // 例: 3000（受信タイムアウト）
    public int RetryTimeoutMs { get; set; }       // 例: 1000（リトライ間隔）
}
```

### RawResponseData（受信結果）
**本メソッドの出力データ型**

```csharp
public class RawResponseData
{
    public byte[] ResponseData { get; set; }      // 受信した生バイトデータ
    public int DataLength { get; set; }           // 受信データサイズ
    public DateTime? ReceivedAt { get; set; }     // 受信完了時刻
    public TimeSpan? ReceiveTime { get; set; }    // 受信処理時間
    public string? ResponseHex { get; set; }      // 16進数文字列表現
    public FrameType FrameType { get; set; }      // フレームタイプ（4E/3E）
    public string? ErrorMessage { get; set; }     // エラーメッセージ（失敗時のみ）
}
```

### FrameType（フレーム種別列挙型）
**定義**: Core/Models/FrameType.cs

```csharp
public enum FrameType
{
    Frame3E,     // 3Eフレーム
    Frame4E,     // 4Eフレーム（TC025で期待される形式）
    Unknown      // 不明なフレーム
}
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
1. TC017_ConnectAsync_TCP接続成功（既実装前提）
2. TC021_SendFrameAsync_正常送信（既実装前提）
3. **TC025_ReceiveResponseAsync_正常受信**（本テスト）
4. TC026: 異常系テスト（受信タイムアウト）

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcServer**: PLC応答モックサーバー
  - 4Eフレーム形式の標準レスポンス返却
  - M000-M999読み込み応答データ生成
  - 指定時間内での応答（ReceiveTimeoutMs以内）
- **MockSocket**: 受信対応Socketモック
  - ReceiveAsync正常実行
  - 指定バイトデータ返却

#### 使用するスタブ
- **TimeoutConfigStubs**: タイムアウト設定スタブ
- **RawResponseDataValidator**: 期待出力検証用ヘルパー
- **SlmpResponseStubs**: SLMP応答データスタブ（4Eフレーム）

---

## 6. テストケース実装構造

### Arrange（準備）
1. **TimeoutConfig準備**:
   - ReceiveTimeoutMs = 3000

2. **MockPlcServer準備**:
   - 4Eフレーム形式応答データ設定
   - M000-M999読み込み応答（1000点分）
   - 応答遅延設定（ReceiveTimeoutMs以内）

3. **PlcCommunicationManager接続済み状態**:
   - ConnectAsync完了状態
   - SendFrameAsync完了状態（READコマンド送信済み）

### Act（実行）
1. ReceiveResponseAsync実行:
   ```csharp
   var result = await plcCommManager.ReceiveResponseAsync(
       timeoutConfig.ReceiveTimeoutMs
   );
   ```

### Assert（検証）
1. **受信データ検証**:
   - `result.ResponseData != null`
   - `result.ResponseData.Length > 0`
   - `result.DataLength == result.ResponseData.Length`

2. **時刻・時間検証**:
   - `result.ReceivedAt != null`
   - `result.ReceivedAt.Value <= DateTime.UtcNow`
   - `result.ReceiveTime != null`
   - `result.ReceiveTime.Value.TotalMilliseconds > 0`
   - `result.ReceiveTime.Value.TotalMilliseconds < timeoutConfig.ReceiveTimeoutMs`

3. **16進数文字列検証**:
   - `result.ResponseHex != null && !string.IsNullOrEmpty(result.ResponseHex)`
   - `result.ResponseHex.StartsWith("D4001234")`（4Eフレーム確認）

4. **フレームタイプ検証**:
   - `result.FrameType == FrameType.Frame4E`

5. **エラー情報検証**:
   - `result.ErrorMessage == null`

---

## 7. エラーハンドリング

### ReceiveResponseAsync スロー例外（TC025では発生しない）
- **TimeoutException**: 受信タイムアウト（ReceiveTimeoutMs超過）
  - メッセージ: "受信タイムアウト（タイムアウト時間: {ReceiveTimeoutMs}ms）"
- **SocketException**: 受信エラー、ネットワークエラー
  - メッセージ: "受信エラー: {詳細}"
- **InvalidOperationException**: 未接続状態での受信試行
  - メッセージ: "未接続状態です。受信前にConnectAsync()を実行してください"

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // 受信関連
    public const string ReceiveTimeout = "受信タイムアウト（タイムアウト時間: {0}ms）";
    public const string ReceiveError = "受信エラー: {0}";
    public const string NotConnectedForReceive = "未接続状態です。受信前にConnectAsync()を実行してください";
}
```

---

## 8. ログ出力要件

### LoggingManager連携
- **受信開始ログ**: 受信タイムアウト時間
- **受信完了ログ**: 受信データサイズ、受信時間、フレームタイプ
- **エラーログ**: 例外詳細、スタックトレース（TC025では発生しない）

### ログレベル
- **Information**: 受信開始・完了
- **Debug**: 受信データ詳細、16進数文字列、フレーム判定
- **Error**: 例外発生時（TC025では発生しない）

### ログ出力例
```
[Information] PLCレスポンス受信開始: タイムアウト=3000ms
[Debug] 受信データサイズ: 248バイト, フレームタイプ: 4E
[Information] PLCレスポンス受信完了: 受信時間=45ms
```

---

## 9. テスト実装チェックリスト

### TC025実装前
- [ ] RawResponseDataモデル作成
- [ ] FrameTypeモデル作成
- [ ] ReceiveResponseAsyncメソッドシグネチャ定義
- [ ] MockPlcServer作成（4Eフレーム応答対応）
- [ ] SlmpResponseStubs作成

### TC025実装中
- [ ] Arrange: TimeoutConfig準備
- [ ] Arrange: MockPlcServer起動・応答設定
- [ ] Arrange: ConnectAsync, SendFrameAsync完了状態設定
- [ ] Act: ReceiveResponseAsync呼び出し
- [ ] Assert: ResponseData検証（非null、サイズ正常）
- [ ] Assert: DataLength検証（一致）
- [ ] Assert: ReceivedAt検証（時刻記録）
- [ ] Assert: ReceiveTime検証（処理時間記録）
- [ ] Assert: ResponseHex検証（16進数文字列変換）
- [ ] Assert: FrameType検証（4Eフレーム判定）
- [ ] Assert: ErrorMessage検証（null）

### TC025実装後
- [ ] テスト実行・Red確認
- [ ] ReceiveResponseAsync本体実装（受信処理）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施
- [ ] TC026（受信タイムアウト）への準備

---

## 10. 参考情報

### SLMP通信仕様
- プロトコル: SLMP（Seamless Message Protocol）
- フレーム形式: 4E/3E ASCII形式
- 応答タイムアウト: 通常3-5秒
- データ形式: 16進数ASCII文字列

**重要**: TC025ではMockPlcServerを使用、実機PLC不要

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/ResponseSamples/

- Slmp4EResponse.json: 4Eフレーム応答サンプル
- SlmpM000Response.json: M000-M999読み込み応答サンプル

---

## 11. PySLMPClient実装参考情報

### 受信処理実装（Python実装例）

#### PySLMPClientでの受信処理
```python
import socket
import time

class SLMPClient:
    def receive_response(self, timeout_sec):
        self.__socket.settimeout(timeout_sec)

        try:
            start_time = time.time()
            response_data = self.__socket.recv(8192)
            receive_time = time.time() - start_time

            # 16進数文字列変換
            response_hex = response_data.hex().upper()

            # フレームタイプ判定
            frame_type = "4E" if response_hex.startswith("D4001234") else "3E"

            return {
                'response_data': response_data,
                'data_length': len(response_data),
                'received_at': time.time(),
                'receive_time': receive_time,
                'response_hex': response_hex,
                'frame_type': frame_type
            }

        except socket.timeout:
            raise TimeoutError(f"Receive timeout: {timeout_sec}s")
        except socket.error as e:
            raise RuntimeError(f"Receive error: {e}")
```

#### C#実装例（TC025対応）
```csharp
public async Task<RawResponseData> ReceiveResponseAsync(int receiveTimeoutMs)
{
    // 接続状態チェック
    if (_socket == null || !_socket.Connected)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnectedForReceive);
    }

    // 受信バッファ準備
    byte[] buffer = new byte[8192];
    var startTime = DateTime.UtcNow;

    // タイムアウト制御付き受信
    var cts = new CancellationTokenSource(receiveTimeoutMs);

    try
    {
        int receivedBytes = await _socket.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            SocketFlags.None,
            cts.Token
        );

        var receiveTime = DateTime.UtcNow - startTime;

        // 受信データ処理
        byte[] responseData = new byte[receivedBytes];
        Array.Copy(buffer, responseData, receivedBytes);

        // 16進数文字列変換
        string responseHex = Convert.ToHexString(responseData);

        // フレームタイプ判定
        FrameType frameType = responseHex.StartsWith("D4001234")
            ? FrameType.Frame4E
            : FrameType.Frame3E;

        return new RawResponseData
        {
            ResponseData = responseData,
            DataLength = receivedBytes,
            ReceivedAt = DateTime.UtcNow,
            ReceiveTime = receiveTime,
            ResponseHex = responseHex,
            FrameType = frameType,
            ErrorMessage = null
        };
    }
    catch (OperationCanceledException)
    {
        throw new TimeoutException(
            string.Format(ErrorMessages.ReceiveTimeout, receiveTimeoutMs));
    }
    catch (SocketException ex)
    {
        throw new SocketException(
            string.Format(ErrorMessages.ReceiveError, ex.Message));
    }
}
```

### 実装時の重要ポイント

1. **受信バッファサイズ**: 8192バイト（SLMP標準最大サイズ対応）
2. **受信時間計測**: DateTime.UtcNowを使用した正確な時間計測
3. **16進数変換**: Convert.ToHexStringを使用した効率的な変換
4. **フレーム判定**: 先頭8バイト（"D4001234"）による4E/3E判定
5. **エラーハンドリング**: OperationCanceledException（タイムアウト）とSocketException（受信失敗）の区別

---

以上が TC025_ReceiveResponseAsync_正常受信テスト実装に必要な情報です。