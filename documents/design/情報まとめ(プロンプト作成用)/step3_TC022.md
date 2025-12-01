# Step3 SendFrameAsync 全機器データ取得テスト実装用情報（TC022）

## ドキュメント概要

### 目的
このドキュメントは、TC022_SendFrameAsync_全機器データ取得テストの実装に必要な情報を集約したものです。
**コード作成時に必要となる技術情報のみ**を記載しており、学習資料や説明的な内容は含みません。

### 情報取得元
本ドキュメントの情報は以下のソースから抽出・統合されています：

#### 設計書（andon/documents/design/）
- `クラス・メソッドリスト.md` - クラス・メソッドの一覧と概要
- `クラス設計.md` - 詳細なクラス設計仕様
- `テスト内容.md` - テストケース仕様
- `プロジェクト構造設計.md` - フォルダ構造・プロジェクト構成
- `依存関係.md` - クラス間の依存関係

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
- SLMPフレーム配列（IEnumerable<string>型、複数の16進数文字列形式）
  - M001-M999読み込みフレーム: "54001234000000010401006400000090E8030000"
  - D001-D999読み込みフレーム: "54001234000000010400A800000090E8030000"
  - **注**: TC022では複数フレームの一括送信を検証

#### Output
- Task（全フレーム送信完了状態）
- 成功時: Taskが正常完了、全フレームの送信成功を確認
- 失敗時: 例外スロー
  - TimeoutException: 送信タイムアウト時
  - SocketException: ソケットエラー時
  - InvalidOperationException: 未接続状態での送信試行時
  - PartialFailureException: 一部フレーム送信失敗時（複数フレーム送信時のみ）

#### 機能
- READコマンド(0104)複数フレーム送信
- 全機器（M機器・D機器）からのデータ取得
- タイムアウト制御（Socket.SendTimeout使用）
- ソケットレベルでのタイムアウト適用（ConnectAsync内で設定済み）
- 複数フレーム送信時のエラーハンドリング

#### データ取得元
- ConfigToFrameManager.BuildFrames()（送信フレーム配列）
- PlcCommunicationManager.ConnectAsync()（接続状態・Socketインスタンス）

---

## 2. テストケース仕様（TC022）

### TC022_SendFrameAsync_全機器データ取得
**目的**: 一度の通信で全機器からデータを取得する機能をテスト（既定値 M001-M999, D001-D999）

#### 前提条件
- ConnectAsyncが成功済み
- 接続状態: Connected
- Socketインスタンスが有効
- BuildFrames()で複数フレーム構築済み

#### 入力データ

**M001-M999読み込みフレーム**:
- フレーム文字列: "54001234000000010401006400000090E8030000"
- フレーム構成:
  - サブヘッダ: 54001234000000 (4Eフレーム識別)
  - READコマンド: 0104 (デバイス一括読み出し)
  - サブコマンド: 0100 (ビット単位読み出し)
  - デバイスコード: 6400 (M機器、0x90)
  - 開始番号: 00000090 (M001、リトルエンディアン)
  - デバイス点数: E8030000 (1000点=0x03E8、リトルエンディアン)

**D001-D999読み込みフレーム**:
- フレーム文字列: "54001234000000010400A800000090E8030000"
- フレーム構成:
  - サブヘッダ: 54001234000000 (4Eフレーム識別)
  - READコマンド: 0104 (デバイス一括読み出し)
  - サブコマンド: 0000 (ワード単位読み出し)
  - デバイスコード: A800 (D機器、0xA8)
  - 開始番号: 00000090 (D001、リトルエンディアン)
  - デバイス点数: E8030000 (1000点=0x03E8、リトルエンディアン)

#### 期待出力
- Task<MultiFrameTransmissionResult>（全フレーム送信結果オブジェクト）
  - IsSuccess=true（全フレーム送信成功フラグ）
  - TotalFrameCount=2（送信対象フレーム数）
  - SuccessfulFrameCount=2（送信成功フレーム数）
  - FailedFrameCount=0（送信失敗フレーム数）
  - FrameResults（各フレームの送信結果詳細）
    - M機器フレーム: 送信バイト数、所要時間、成功状態
    - D機器フレーム: 送信バイト数、所要時間、成功状態
  - TotalTransmissionTime（全フレーム送信総時間）
  - TargetDeviceTypes=["M", "D"]（対象デバイス種別一覧）

#### 動作フロー成功条件
1. **複数フレーム送信制御**:
   - 各フレームが順次送信される
   - フレーム間の適切な間隔制御（SendIntervalMs設定）
   - 1つ目のフレーム送信成功を確認後、2つ目のフレーム送信
2. **全機器データ取得確認**:
   - M機器データ取得成功（M001-M999）
   - D機器データ取得成功（D001-D999）
   - データ欠損無し
3. **統計情報記録**:
   - 各フレームの送信バイト数記録
   - 各フレームの送信時間記録
   - 全体の送信総時間計算

---

## 3. SLMPフレーム詳細（複数フレーム対応）

### 4Eフレーム/ASCIIフォーマット（複数フレーム送信）
**実機テスト設定**: Q00UDPCPUとの通信で使用（複数デバイス対応）

#### 複数フレーム送信規則
- 各フレームは独立したSLMP要求として送信
- フレーム間の間隔制御（SendIntervalMs設定、デフォルト: 100ms）
- 各フレームのACK（応答待ち）制御
  - オプション1: 同期送信（各フレーム送信後に応答待ち）
  - オプション2: 非同期送信（全フレーム送信後に応答待ち）
  - **TC022では**: 同期送信（確実性優先）を使用

#### M001-M999フレーム（1つ目）
```
バイナリ形式（参考）:
54 00 12 34 00 00 00 00  01 04 01 00  64 00 01 00 90  E8 03 00

ASCII形式（実送信）:
"54001234000000010401006400000090E8030000"

各フィールド:
- サブヘッダ: 54001234000000
- READコマンド: 0104
- サブコマンド: 0100 (ビット単位)
- デバイスコード: 6400 (M機器=0x90)
- 開始アドレス: 00000090 (M001)
- デバイス点数: E8030000 (1000点=0x03E8)
```

#### D001-D999フレーム（2つ目）
```
バイナリ形式（参考）:
54 00 12 34 00 00 00 00  01 04 00 00  A8 00 01 00 90  E8 03 00

ASCII形式（実送信）:
"54001234000000010400A800000090E8030000"

各フィールド:
- サブヘッダ: 54001234000000
- READコマンド: 0104
- サブコマンド: 0000 (ワード単位)
- デバイスコード: A800 (D機器=0xA8)
- 開始アドレス: 00000090 (D001)
- デバイス点数: E8030000 (1000点=0x03E8)
```

---

## 4. 依存クラス・設定

### ConnectionConfig（接続設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class ConnectionConfig
{
    public string IpAddress { get; set; }        // 例: "192.168.3.250"
    public int Port { get; set; }                 // 例: 5007
    public bool UseTcp { get; set; }              // false (UDP使用)
    public string ConnectionType { get; set; }    // "UDP"
    public bool IsBinary { get; set; }            // false (ASCII形式)
    public FrameVersion FrameVersion { get; set; } // FrameVersion.Frame4E
}
```

### TimeoutConfig（タイムアウト設定）
**取得元**: ConfigToFrameManager.LoadConfigAsync()

```csharp
public class TimeoutConfig
{
    public int ConnectTimeoutMs { get; set; }     // 例: 5000
    public int SendTimeoutMs { get; set; }        // 例: 3000 ← SendFrameAsyncで使用
    public int ReceiveTimeoutMs { get; set; }     // 例: 5000
    public int RetryTimeoutMs { get; set; }       // 例: 1000
    public int SendIntervalMs { get; set; }       // 例: 100 ← TC022で使用（複数フレーム間隔）
}
```

### ConnectionResponse（接続結果）
**取得元**: PlcCommunicationManager.ConnectAsync()

```csharp
public class ConnectionResponse
{
    public ConnectionStatus Status { get; set; }  // Connected
    public Socket? Socket { get; set; }           // 実際の通信用ソケット
    public EndPoint? RemoteEndPoint { get; set; } // 接続先情報
    public DateTime? ConnectedAt { get; set; }    // 接続完了時刻
    public TimeSpan? ConnectionTime { get; set; } // 接続処理時間
    public string? ErrorMessage { get; set; }     // null (成功時)
}
```

### MultiFrameTransmissionResult（複数フレーム送信結果）
**TC022専用データ転送オブジェクト**

```csharp
public class MultiFrameTransmissionResult
{
    public bool IsSuccess { get; set; }                           // 全フレーム送信成功フラグ
    public int TotalFrameCount { get; set; }                      // 送信対象フレーム数
    public int SuccessfulFrameCount { get; set; }                 // 送信成功フレーム数
    public int FailedFrameCount { get; set; }                     // 送信失敗フレーム数
    public Dictionary<string, FrameTransmissionResult> FrameResults { get; set; } // デバイス種別別結果
    public TimeSpan TotalTransmissionTime { get; set; }           // 全フレーム送信総時間
    public List<string> TargetDeviceTypes { get; set; }           // 対象デバイス種別一覧（例: ["M", "D"]）
    public string? ErrorMessage { get; set; }                     // エラーメッセージ（失敗時のみ）
}

public class FrameTransmissionResult
{
    public bool IsSuccess { get; set; }           // 個別フレーム送信成功フラグ
    public int SentBytes { get; set; }            // 送信バイト数
    public TimeSpan TransmissionTime { get; set; }// 送信所要時間
    public string DeviceType { get; set; }        // デバイス種別（"M", "D"）
    public string DeviceRange { get; set; }       // デバイス範囲（"M001-M999", "D001-D999"）
    public string? ErrorMessage { get; set; }     // エラーメッセージ（失敗時のみ）
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
1. **TC021_SendFrameAsync_正常送信**（単一フレーム：既存）
2. **TC022_SendFrameAsync_全機器データ取得**（複数フレーム：今回実装）
   - M機器フレーム送信テスト
   - D機器フレーム送信テスト
   - 全機器データ取得統合テスト
3. 異常系テスト（次フェーズ）
   - 複数フレーム送信時の部分失敗テスト
   - フレーム間隔制御異常テスト

### モック・スタブ使用
**テストユーティリティ配置**: Tests/TestUtilities/

#### 使用するモック
- **MockPlcCommunicationManager**: PlcCommunicationManager全体のモック
- **MockConfigToFrameManager**: 設定読み込み・フレーム構築のモック
- **MockSocket**: 複数フレーム送信対応Socket

#### 使用するスタブ
- **PlcResponseStubs**: PLC応答データのスタブ（複数フレーム対応）
- **ConfigurationStubs**: 設定データのスタブ（SendIntervalMs含む）
- **NetworkStubs**: ネットワーク動作のスタブ（複数フレーム送信対応）

---

## 6. テストケース実装構造

### Arrange（準備）
1. MockSocketの準備（複数フレーム対応）
   - 送信成功をシミュレート（複数回）
   - 各フレームの送信バイト数を記録
   - フレーム間隔制御のシミュレート
2. ConnectionResponseの準備
   - Status = Connected
   - Socket = MockSocket
   - ConnectedAt, ConnectionTime設定
3. ConnectionConfig・TimeoutConfigの準備
   - SendTimeoutMs = 3000
   - SendIntervalMs = 100（複数フレーム間隔）
4. BuildFramesスタブの準備
   - M機器フレーム: "54001234000000010401006400000090E8030000"
   - D機器フレーム: "54001234000000010400A800000090E8030000"
   - フレーム配列: List<string> { M機器, D機器 }
5. PlcCommunicationManagerインスタンス作成
   - モックSocketを注入
   - 複数フレーム送信対応

### Act（実行）
1. ConnectAsync実行（前提条件確立）
2. SendFrameAsync実行（複数フレーム）
   - 入力: List<string> { M機器フレーム, D機器フレーム }
   - 期待: MultiFrameTransmissionResult取得

### Assert（検証）
1. MultiFrameTransmissionResultの検証
   - IsSuccess=trueの確認
   - TotalFrameCount=2の確認
   - SuccessfulFrameCount=2の確認
   - FailedFrameCount=0の確認
2. 各フレーム送信結果の検証
   - FrameResults["M"]の送信成功確認
   - FrameResults["D"]の送信成功確認
   - 各フレームの送信バイト数が期待値と一致
3. 送信統計の検証
   - TotalTransmissionTime > 0
   - TotalTransmissionTime >= 各フレーム送信時間の合計
4. Socketの送信メソッド検証
   - 送信メソッドが2回呼ばれたことを確認
   - 送信データが各フレーム文字列と一致
   - フレーム間隔が適切（SendIntervalMs設定）

---

## 7. DIコンテナ設定

### サービスライフタイム
- **PlcCommunicationManager**: Transient（PLC別インスタンス）
- **ConfigToFrameManager**: Transient（設定別インスタンス）
- **LoggingManager**: Singleton（共有リソース）
- **ErrorHandler**: Singleton（共有リソース）

### インターフェース登録
```csharp
services.AddTransient<IPlcCommunicationManager, PlcCommunicationManager>();
services.AddTransient<IConfigToFrameManager, ConfigToFrameManager>();
services.AddSingleton<ILoggingManager, LoggingManager>();
services.AddSingleton<IErrorHandler, ErrorHandler>();
```

---

## 8. エラーハンドリング（複数フレーム対応）

### SendFrameAsync スロー例外（TC022追加）
- **TimeoutException**: 送信タイムアウト（SendTimeoutMs超過、個別フレームまたは全体）
- **SocketException**: ソケットエラー（接続切断等）
- **InvalidOperationException**: 未接続状態での送信試行
- **ArgumentException**: 不正なフレーム形式
- **PartialFailureException**: 一部フレーム送信失敗（TC022専用、複数フレーム送信時のみ）
  - 例: M機器フレーム成功、D機器フレーム失敗
  - SuccessfulFrames: 成功したフレーム情報
  - FailedFrames: 失敗したフレーム情報とエラー詳細

### エラーメッセージ統一
**ファイル**: Core/Constants/ErrorMessages.cs

```csharp
public static class ErrorMessages
{
    // 既存メッセージ
    public const string NotConnected = "PLC未接続状態です。先にConnectAsync()を実行してください。";
    public const string SendTimeout = "フレーム送信がタイムアウトしました。";
    public const string InvalidFrame = "不正なSLMPフレーム形式です。";

    // TC022追加メッセージ
    public const string PartialFrameFailure = "{0}個中{1}個のフレーム送信に失敗しました。";
    public const string AllFramesSuccess = "全{0}個のフレーム送信が成功しました。";
    public const string MultiFrameTimeout = "複数フレーム送信がタイムアウトしました。{0}/{1}フレーム完了。";
}
```

---

## 9. ログ出力要件（複数フレーム対応）

### LoggingManager連携
- **送信開始ログ**: フレーム総数、各フレームの対象デバイス情報
- **各フレーム送信完了ログ**: フレーム番号、デバイス種別、送信バイト数、所要時間
- **全フレーム送信完了ログ**: 総送信バイト数、総所要時間、成功/失敗統計
- **エラーログ**: 失敗フレーム詳細、例外詳細、スタックトレース

### ログレベル
- **Information**: 送信開始・各フレーム完了・全フレーム完了
- **Warning**: 個別フレーム送信リトライ発生時、部分失敗時
- **Error**: 例外発生時、複数フレーム全体失敗時

### ログ出力例
```
[Information] 複数フレーム送信開始: 対象デバイス=M機器, D機器, フレーム数=2
[Information] フレーム1/2送信完了: デバイス=M機器, 範囲=M001-M999, 送信バイト数=38, 所要時間=150ms
[Information] フレーム2/2送信完了: デバイス=D機器, 範囲=D001-D999, 送信バイト数=38, 所要時間=145ms
[Information] 全フレーム送信完了: 総送信バイト数=76, 総所要時間=395ms（間隔100ms含む）, 成功=2/2
```

---

## 10. テスト実装チェックリスト

### TC022実装前
- [ ] PlcCommunicationManagerクラス（複数フレーム送信対応）
- [ ] IPlcCommunicationManagerインターフェース更新（複数フレーム対応）
- [ ] SendFrameAsyncメソッドシグネチャ更新（IEnumerable<string>対応）
- [ ] MultiFrameTransmissionResult・FrameTransmissionResultモデル作成
- [ ] MockSocket（複数フレーム対応）作成
- [ ] TimeoutConfig（SendIntervalMs追加）更新

### TC022実装中
- [ ] Arrange: 複数フレーム対応モック・スタブ準備
- [ ] Act: SendFrameAsync呼び出し（複数フレーム）
- [ ] Assert: MultiFrameTransmissionResult検証
- [ ] Assert: 各FrameTransmissionResult検証
- [ ] Assert: 送信統計検証
- [ ] Assert: Socket呼び出し回数・間隔検証

### TC022実装後
- [ ] テスト実行・Red確認
- [ ] SendFrameAsync本体実装（複数フレーム対応）
- [ ] フレーム間隔制御実装（SendIntervalMs）
- [ ] エラーハンドリング実装（PartialFailureException）
- [ ] ログ出力実装（複数フレーム対応）
- [ ] テスト実行・Green確認
- [ ] リファクタリング実施

---

## 11. 参考情報

### SLMP仕様書
- デバイスコード表: SLMP仕様書pdf2img/page_36.png
- フレーム構造: 4Eフレーム/ASCIIフォーマット準拠
- READコマンド: 0x0104（デバイス一括読み出し）
- 複数デバイス読み出し: 別フレームでの送信が標準

### テストデータサンプル
**配置先**: Tests/TestUtilities/TestData/SlmpFrameSamples/

- M001-M999_ReadFrame.txt: "54001234000000010401006400000090E8030000"
- D001-D999_ReadFrame.txt: "54001234000000010400A800000090E8030000"
- MultiFrame_ReadAll.txt: M機器フレーム + D機器フレームの配列データ

---

## 12. PySLMPClient実装参考情報（複数フレーム対応）

### デバイスコード定義（const.py）
```python
class DeviceCode(enum.Enum):
    M = 0x90   # 144 (decimal) - Mデバイス
    D = 0xA8   # 168 (decimal) - Dデバイス
    X = 0x9C   # Xデバイス
    Y = 0x9D   # Yデバイス
```

**C#実装時の参考**:
```csharp
public static class DeviceCode
{
    public const byte M = 0x90;  // Mデバイス
    public const byte D = 0xA8;  // Dデバイス
}
```

### SLMPコマンド定義（const.py）
```python
class SLMPCommand(enum.Enum):
    Device_Read = 0x0104   # デバイス一括読み出し（注：0x0401から修正）
```

### ASCII形式フレーム作成（util.py make_ascii_frame関数）

#### Python実装
```python
def make_ascii_frame(seq, target, timeout, cmd, sub_cmd, data, ver):
    cmd_text = b"%02X%02X%04X%02X%04X%04X%04X%04X" % (
        target.network,    # ネットワーク番号
        target.node,       # 要求先局番
        target.dst_proc,   # 要求先プロセッサ番号
        target.m_drop,     # マルチドロップ局番
        len(data) + 12,    # データ長
        timeout,           # 監視タイマ
        cmd.value,         # コマンド
        sub_cmd,           # サブコマンド
    )
    if ver == 4:
        buf = b"5400%04X0000" % seq + cmd_text + data
    return buf
```

#### C#実装例（複数フレーム対応）
```csharp
public List<string> MakeMultiAsciiFrames(List<DeviceReadRequest> requests, byte startSeq, ushort timeout)
{
    var frames = new List<string>();
    byte seq = startSeq;

    foreach (var request in requests)
    {
        string cmdText = string.Format(
            "{0:X2}{1:X2}{2:X4}{3:X2}{4:X4}{5:X4}{6:X4}{7:X4}",
            0x00,                   // ネットワーク番号
            0xFF,                   // 要求先局番
            0x03FF,                 // 要求先プロセッサ番号
            0x00,                   // マルチドロップ局番
            request.Data.Length + 12, // データ長
            timeout,                // 監視タイマ
            0x0104,                 // READコマンド
            request.SubCommand      // サブコマンド（ビット/ワード）
        );

        string frame = $"5400{seq:X4}0000{cmdText}{request.Data}";
        frames.Add(frame);
        seq++;
    }

    return frames;
}
```

### 複数フレーム送信実装例

#### C#実装（TC022対応）
```csharp
public async Task<MultiFrameTransmissionResult> SendMultipleFramesAsync(
    IEnumerable<string> frames,
    Socket socket,
    TimeoutConfig timeout,
    CancellationToken cancellationToken = default)
{
    var result = new MultiFrameTransmissionResult
    {
        TotalFrameCount = frames.Count(),
        FrameResults = new Dictionary<string, FrameTransmissionResult>()
    };

    var startTime = DateTime.UtcNow;
    int successCount = 0;
    int failCount = 0;

    foreach (var frame in frames)
    {
        var frameStartTime = DateTime.UtcNow;

        try
        {
            // 個別フレーム送信
            byte[] frameBytes = ConvertHexStringToBytes(frame);
            int sentBytes = await socket.SendAsync(
                new ArraySegment<byte>(frameBytes),
                SocketFlags.None,
                cancellationToken);

            var frameResult = new FrameTransmissionResult
            {
                IsSuccess = true,
                SentBytes = sentBytes,
                TransmissionTime = DateTime.UtcNow - frameStartTime,
                DeviceType = DetermineDeviceType(frame),
                DeviceRange = DetermineDeviceRange(frame)
            };

            result.FrameResults.Add(frameResult.DeviceType, frameResult);
            successCount++;

            // フレーム間隔制御
            if (timeout.SendIntervalMs > 0)
            {
                await Task.Delay(timeout.SendIntervalMs, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var frameResult = new FrameTransmissionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                DeviceType = DetermineDeviceType(frame),
                DeviceRange = DetermineDeviceRange(frame)
            };

            result.FrameResults.Add(frameResult.DeviceType, frameResult);
            failCount++;
        }
    }

    result.SuccessfulFrameCount = successCount;
    result.FailedFrameCount = failCount;
    result.IsSuccess = (failCount == 0);
    result.TotalTransmissionTime = DateTime.UtcNow - startTime;
    result.TargetDeviceTypes = result.FrameResults.Keys.ToList();

    return result;
}
```

### フレーム構造詳細

#### 4E ASCIIフレーム構造（複数フレーム）
```
"5400" + "%04X" % seq + "0000" + cmd_text + data
 ^^^^     ^^^^          ^^^^     ^^^^^^^^   ^^^^
 サブ     シーケンス    予約     コマンド   データ
 ヘッダ   番号（連番）          テキスト
```

**複数フレーム送信時のシーケンス番号管理**:
- 1つ目フレーム（M機器）: seq=0x1234
- 2つ目フレーム（D機器）: seq=0x1235
- 連番管理により、応答とフレームの対応付けが可能

#### cmd_text構造
```
"%02X%02X%04X%02X%04X%04X%04X%04X"
  ^^   ^^   ^^^^   ^^   ^^^^   ^^^^   ^^^^   ^^^^
  Net  Node DstPrc Drop DataLen Timer  Cmd    SubCmd
  1B   1B   2B     1B   2B      2B     2B     2B
```

### 実装時の重要ポイント（TC022追加）

1. **デバイスコード**: M=0x90, D=0xA8（16進数）
2. **エンディアン**: ASCII形式では文字列表現、バイナリ形式ではリトルエンディアン
3. **サブコマンド**:
   - 0x0100: ビット単位読み出し（M機器）
   - 0x0000: ワード単位読み出し（D機器）
4. **フレーム長**: ASCIIではデータ部バイト長+12
5. **デバイス点数**: リトルエンディアン表現（1000点=0x03E8→"E80300"）
6. **複数フレーム送信制御**:
   - フレーム間隔制御（SendIntervalMs、推奨: 100-200ms）
   - シーケンス番号連番管理（応答対応付け用）
   - 各フレーム送信成功確認後の次フレーム送信
   - 部分失敗時のエラーハンドリング（PartialFailureException）

---

## 13. TC021との主要な違い

### TC021（正常送信テスト）
- **対象**: 単一フレームの送信機能
- **入力**: 1つのSLMPフレーム文字列
- **検証項目**: 送信成功、送信バイト数、例外なし
- **期待出力**: Task（単純な完了状態）

### TC022（全機器データ取得テスト）
- **対象**: 複数フレームの一括送信機能、全機器データ取得
- **入力**: 複数のSLMPフレーム文字列（M機器・D機器）
- **検証項目**:
  - 全フレーム送信成功
  - 各フレーム送信統計
  - フレーム間隔制御
  - 全機器データ取得完了
- **期待出力**: MultiFrameTransmissionResult（詳細統計情報）

### 実装時の追加考慮事項
1. **データ構造**: MultiFrameTransmissionResult, FrameTransmissionResultの追加
2. **エラーハンドリング**: PartialFailureExceptionの追加
3. **ログ出力**: 複数フレーム対応の詳細ログ
4. **統計管理**: フレーム別・全体の送信統計
5. **間隔制御**: SendIntervalMsによるフレーム間隔管理

---

以上が TC022_SendFrameAsync_全機器データ取得テスト実装に必要な情報です。
