# TC022_SendFrameAsync_全機器データ取得テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC022_SendFrameAsync_全機器データ取得テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
PlcCommunicationManager.SendFrameAsync()メソッドの複数フレーム送信機能テストケースTC022を実装します。
このテストは、全機器（M機器・D機器）からのデータを一括取得する機能が正常に動作することを検証します。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC022_SendFrameAsync_全機器データ取得_複数フレーム送信`

---

## 前提条件の確認

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs` (複数フレーム送信対応)
   - `Core/Interfaces/IPlcCommunicationManager.cs` (複数フレーム対応メソッドシグネチャ)
   - `Core/Models/ConnectionResponse.cs`
   - `Core/Models/ConnectionConfig.cs`
   - `Core/Models/TimeoutConfig.cs` (SendIntervalMs追加)
   - `Core/Models/MultiFrameTransmissionResult.cs` (TC022専用)
   - `Core/Models/FrameTransmissionResult.cs` (TC022専用)
   - `Core/Exceptions/PartialFailureException.cs` (TC022専用)

2. **テストユーティリティの確認**
   - `Tests/TestUtilities/Mocks/MockSocket.cs` (複数フレーム対応)
   - `Tests/TestUtilities/Stubs/PlcResponseStubs.cs` (複数フレーム対応)
   - `Tests/TestUtilities/TestData/SlmpFrameSamples/` (複数フレームサンプル)

3. **前提テストの確認**
   - TC021_SendFrameAsync_正常送信が実装済み・テストパス済みであること

4. **開発手法ドキュメント確認**
   - `C:\Users\1010821\Desktop\python\andon\documents\development_methodology\development-methodology.md`を参照

不足しているファイルがあれば報告してください。

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストファイル拡張
```
ファイル: Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs
名前空間: andon.Tests.Unit.Core.Managers
```

#### Step 1-2: テストケース実装

**TC022: 全機器データ取得テスト（複数フレーム送信）**

**Arrange（準備）**:
- MockSocketを作成（複数フレーム送信成功をシミュレート）
- ConnectionResponseを作成
  - Status = ConnectionStatus.Connected
  - Socket = MockSocket
  - ConnectedAt, ConnectionTime設定
- ConnectionConfigを作成
  - IpAddress = "192.168.3.250"
  - Port = 5007
  - UseTcp = false
  - ConnectionType = "UDP"
  - IsBinary = false
  - FrameVersion = FrameVersion.Frame4E
- TimeoutConfigを作成
  - SendTimeoutMs = 3000
  - SendIntervalMs = 100 (複数フレーム間隔制御)
- PlcCommunicationManagerインスタンス作成（モック注入）
- 複数フレーム配列準備:
  ```csharp
  var frames = new List<string>
  {
      "54001234000000010401006400000090E8030000", // M001-M999フレーム
      "54001234000000010400A800000090E8030000"   // D001-D999フレーム
  };
  ```

**Act（実行）**:
- `var result = await manager.SendMultipleFramesAsync(frames);`

**Assert（検証）**:
- MultiFrameTransmissionResultの検証:
  - `Assert.True(result.IsSuccess)`
  - `Assert.Equal(2, result.TotalFrameCount)`
  - `Assert.Equal(2, result.SuccessfulFrameCount)`
  - `Assert.Equal(0, result.FailedFrameCount)`
- 各フレーム送信結果の検証:
  - `Assert.True(result.FrameResults["M"].IsSuccess)`
  - `Assert.True(result.FrameResults["D"].IsSuccess)`
  - `Assert.Equal(38, result.FrameResults["M"].SentBytes)` (M機器フレーム送信バイト数)
  - `Assert.Equal(38, result.FrameResults["D"].SentBytes)` (D機器フレーム送信バイト数)
- 送信統計の検証:
  - `Assert.True(result.TotalTransmissionTime > TimeSpan.Zero)`
  - `Assert.Contains("M", result.TargetDeviceTypes)`
  - `Assert.Contains("D", result.TargetDeviceTypes)`
- MockSocketの送信メソッド検証:
  - 送信メソッドが2回呼ばれたこと
  - 送信データが各フレーム文字列のバイト配列と一致すること
  - フレーム間隔が適切に制御されたこと（SendIntervalMs設定）

#### Step 1-3: テスト実行（Red確認）
```bash
dotnet test --filter "FullyQualifiedName~TC022"
```

期待結果: テスト失敗（SendMultipleFramesAsyncが未実装のため）

---

### Phase 2: Green（最小実装）

#### Step 2-1: SendMultipleFramesAsync最小実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

**最小実装要件**:
```csharp
public async Task<MultiFrameTransmissionResult> SendMultipleFramesAsync(
    IEnumerable<string> frames,
    CancellationToken cancellationToken = default)
{
    // 1. 未接続チェック
    if (_connectionResponse?.Status != ConnectionStatus.Connected || _socket == null)
    {
        throw new InvalidOperationException(ErrorMessages.NotConnected);
    }

    var result = new MultiFrameTransmissionResult
    {
        TotalFrameCount = frames.Count(),
        FrameResults = new Dictionary<string, FrameTransmissionResult>(),
        TargetDeviceTypes = new List<string>()
    };

    var startTime = DateTime.UtcNow;
    int successCount = 0;
    int failCount = 0;

    foreach (var frame in frames)
    {
        var frameStartTime = DateTime.UtcNow;

        try
        {
            // 2. フレーム文字列をバイト配列に変換
            byte[] frameBytes = ConvertHexStringToBytes(frame);

            // 3. ソケット送信
            int bytesSent = await _socket.SendAsync(
                new ArraySegment<byte>(frameBytes),
                SocketFlags.None,
                cancellationToken);

            // 4. 送信結果記録
            var deviceType = DetermineDeviceType(frame);
            var frameResult = new FrameTransmissionResult
            {
                IsSuccess = true,
                SentBytes = bytesSent,
                TransmissionTime = DateTime.UtcNow - frameStartTime,
                DeviceType = deviceType,
                DeviceRange = DetermineDeviceRange(frame)
            };

            result.FrameResults.Add(deviceType, frameResult);
            result.TargetDeviceTypes.Add(deviceType);
            successCount++;

            // 5. フレーム間隔制御
            if (_timeoutConfig.SendIntervalMs > 0)
            {
                await Task.Delay(_timeoutConfig.SendIntervalMs, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            var deviceType = DetermineDeviceType(frame);
            var frameResult = new FrameTransmissionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                DeviceType = deviceType,
                DeviceRange = DetermineDeviceRange(frame)
            };

            result.FrameResults.Add(deviceType, frameResult);
            failCount++;
        }
    }

    result.SuccessfulFrameCount = successCount;
    result.FailedFrameCount = failCount;
    result.IsSuccess = (failCount == 0);
    result.TotalTransmissionTime = DateTime.UtcNow - startTime;

    return result;
}

private byte[] ConvertHexStringToBytes(string hexString)
{
    byte[] bytes = new byte[hexString.Length / 2];
    for (int i = 0; i < bytes.Length; i++)
    {
        bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
    }
    return bytes;
}

private string DetermineDeviceType(string frame)
{
    // フレーム内のデバイスコードから判定
    // M機器: 6400 (0x90), D機器: A800 (0xA8)
    if (frame.Contains("6400"))
        return "M";
    else if (frame.Contains("A800"))
        return "D";
    else
        return "Unknown";
}

private string DetermineDeviceRange(string frame)
{
    var deviceType = DetermineDeviceType(frame);
    return deviceType switch
    {
        "M" => "M001-M999",
        "D" => "D001-D999",
        _ => "Unknown"
    };
}
```

**必要なフィールド追加**:
```csharp
private ConnectionResponse? _connectionResponse;
private Socket? _socket;
private TimeoutConfig _timeoutConfig;
```

#### Step 2-2: テスト再実行（Green確認）
```bash
dotnet test --filter "FullyQualifiedName~TC022"
```

期待結果: TC022テストがパス

---

### Phase 3: Refactor（リファクタリング）

#### Step 3-1: コード品質向上
- ログ出力追加（LoggingManager連携）
  - 複数フレーム送信開始ログ: フレーム総数、対象デバイス情報
  - 各フレーム送信完了ログ: フレーム番号、デバイス種別、送信バイト数、所要時間
  - 全フレーム送信完了ログ: 総送信バイト数、総所要時間、成功/失敗統計
  - エラーログ: 失敗フレーム詳細、例外詳細、スタックトレース
- 複数フレーム送信時のエラーハンドリング強化
  - PartialFailureException対応（一部フレーム失敗時）
- タイムアウト処理追加（全体タイムアウト、個別フレームタイムアウト）
- ドキュメントコメント追加

#### Step 3-2: テスト再実行（Green維持確認）
```bash
dotnet test --filter "FullyQualifiedName~TC022"
dotnet test --filter "FullyQualifiedName~TC021" # 回帰テスト
```

期待結果: すべてのテストがパス（リファクタリング後も）

---

## 技術仕様詳細

### SLMPフレーム構造（複数フレーム対応）

#### M001-M999読み込みフレーム
```
文字列: "54001234000000010401006400000090E8030000"
構成:
- サブヘッダ: 54001234000000 (4Eフレーム識別)
- READコマンド: 0104 (デバイス一括読み出し)
- サブコマンド: 0100 (ビット単位読み出し)
- デバイスコード: 6400 (M機器、0x90のリトルエンディアン表現)
- 開始番号: 00000090 (M001、リトルエンディアン)
- デバイス点数: E8030000 (1000点、0x03E8のリトルエンディアン表現)
```

#### D001-D999読み込みフレーム
```
文字列: "54001234000000010400A800000090E8030000"
構成:
- サブヘッダ: 54001234000000 (4Eフレーム識別)
- READコマンド: 0104 (デバイス一括読み出し)
- サブコマンド: 0000 (ワード単位読み出し)
- デバイスコード: A800 (D機器、0xA8のリトルエンディアン表現)
- 開始番号: 00000090 (D001、リトルエンディアン)
- デバイス点数: E8030000 (1000点、リトルエンディアン)
```

### 複数フレーム送信制御

#### フレーム間隔制御
- **SendIntervalMs**: フレーム間の待機時間（推奨: 100-200ms）
- **同期送信**: 各フレーム送信完了を確認後、次フレーム送信
- **シーケンス番号管理**: 応答とフレームの対応付け用（将来拡張）

#### 送信統計管理
- **各フレーム統計**: 送信バイト数、所要時間、成功/失敗状態
- **全体統計**: 総送信バイト数、総所要時間、成功/失敗フレーム数

### エラーハンドリング（複数フレーム対応）

**スロー例外**:
- `TimeoutException`: 送信タイムアウト（SendTimeoutMs超過、個別または全体）
- `SocketException`: ソケットエラー（接続切断等）
- `InvalidOperationException`: 未接続状態での送信試行
- `ArgumentException`: 不正なフレーム形式
- `PartialFailureException`: 一部フレーム送信失敗時（TC022専用）
  - SuccessfulFrames: 成功したフレーム情報
  - FailedFrames: 失敗したフレーム情報とエラー詳細

**エラーメッセージ定数**（Core/Constants/ErrorMessages.cs）:
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

### データモデル（TC022専用）

**MultiFrameTransmissionResult**:
```csharp
public class MultiFrameTransmissionResult
{
    public bool IsSuccess { get; set; }                           // 全フレーム送信成功フラグ
    public int TotalFrameCount { get; set; }                      // 送信対象フレーム数
    public int SuccessfulFrameCount { get; set; }                 // 送信成功フレーム数
    public int FailedFrameCount { get; set; }                     // 送信失敗フレーム数
    public Dictionary<string, FrameTransmissionResult> FrameResults { get; set; } // デバイス種別別結果
    public TimeSpan TotalTransmissionTime { get; set; }           // 全フレーム送信総時間
    public List<string> TargetDeviceTypes { get; set; }           // 対象デバイス種別一覧
    public string? ErrorMessage { get; set; }                     // エラーメッセージ（失敗時のみ）
}
```

**FrameTransmissionResult**:
```csharp
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

### モック・スタブ実装（複数フレーム対応）

**MockSocket（複数フレーム対応）**:
- SendAsyncメソッドをモック（複数回呼び出し対応）
- 送信バイト数を記録（フレーム別）
- 送信データを記録（検証用、フレーム別）
- フレーム間隔制御のシミュレート

**PlcResponseStubs（複数フレーム対応）**:
- M機器フレーム応答データ
- D機器フレーム応答データ
- 複数フレーム応答の組み合わせ

---

## 実装記録・ドキュメント作成要件

### 必須作業項目

#### 1. 進捗記録開始
**ファイル**: `documents/implementation_records/progress_notes/2025-11-06_TC022実装.md`
- 実装開始時刻
- 目標（TC022複数フレーム送信テスト実装完了）
- 実装方針（TC021との違い、複数フレーム対応の追加要素）

#### 2. 実装記録作成
**ファイル**: `documents/implementation_records/method_records/SendMultipleFramesAsync実装記録.md`
- 実装判断根拠
  - 複数フレーム送信の実装方法選択理由
  - MultiFrameTransmissionResultデータ構造の設計根拠
  - フレーム間隔制御の実装方法
  - エラーハンドリング（PartialFailureException）の設計根拠
- 検討した他の方法との比較
  - 単一メソッドでの複数フレーム送信 vs. 複数回の単一フレーム送信
  - 同期送信 vs. 非同期送信
- 技術選択の根拠とトレードオフ
- 発生した問題と解決過程

#### 3. テスト結果保存
**ファイル**: `documents/implementation_records/execution_logs/TC022_テスト結果.log`
- 単体テスト結果（成功/失敗、実行時間、カバレッジ）
- Red-Green-Refactorの各フェーズ結果
- 複数フレーム送信のパフォーマンステスト結果
- エラーログとデバッグ情報
- TC021回帰テスト結果

---

## 完了条件

以下すべてが満たされた時点で実装完了とする：

- [ ] TC022複数フレーム送信テストがパス
- [ ] MultiFrameTransmissionResult正常取得確認
- [ ] 各FrameTransmissionResult詳細情報確認
- [ ] フレーム間隔制御動作確認
- [ ] PartialFailureException動作確認（異常系実装時）
- [ ] SendMultipleFramesAsync本体実装完了
- [ ] リファクタリング完了（ログ出力、エラーハンドリング等）
- [ ] TC021回帰テストでGreen維持確認
- [ ] 進捗記録作成完了
- [ ] 実装記録作成完了
- [ ] C:\Users\1010821\Desktop\python\andon\documents\design\チェックリスト\step3to6_test実施リスト.mdの該当項目にチェック

---

## 実装時の注意点

### TDD手法厳守
- 必ずテストを先に書く（Red）
- 最小実装でテストをパスさせる（Green）
- リファクタリングで品質向上（Refactor）
- 各フェーズでテスト実行を確認
- TC022特有の複数フレーム送信機能も同様のTDDサイクルで実装

### 複数フレーム送信の実装ポイント
- フレーム間隔制御の確実な実装（SendIntervalMs使用）
- 各フレーム送信結果の詳細記録
- 部分失敗時のエラーハンドリング
- 送信統計の正確な計算
- ログ出力での詳細な進捗報告

### 記録の重要性
- 実装判断の根拠を詳細に記録
- テスト結果は数値データも含めて保存
- 複数フレーム送信の性能データも記録
- TC021との違いや改善点を明確に記録

### 文字化け対策
- 日本語ファイル名の新規作成時は`.txt`経由で作成
- 作成後は必ずReadツールで確認
- 文字化け発見時は早期に対処

---

## 参考情報

### 設計書参照先
- `documents/design/クラス設計.md` - PlcCommunicationManager複数フレーム対応
- `documents/design/テスト内容.md` - TC022詳細仕様
- `documents/design/エラーハンドリング.md` - PartialFailureException設計
- `documents/design/メタプロンプト作成用情報/step3_TC022.md` - 実装技術詳細

### 開発手法
- `documents/development_methodology/development-methodology.md`

### PySLMPClient実装参照（複数フレーム対応）
- `PySLMPClient/pyslmpclient/const.py`（デバイスコード定義、複数デバイス対応）
- `PySLMPClient/pyslmpclient/util.py`（複数フレーム作成ロジック）
- `PySLMPClient/tests/test_main.py`（複数フレーム送信テストケース実例）

### SLMP仕様書
- 4Eフレーム/ASCIIフォーマット仕様
- 複数デバイス読み出し方式
- フレーム間隔制御推奨値

---

以上の指示に従って、TC022_SendFrameAsync_全機器データ取得テスト（複数フレーム送信対応）の実装を開始してください。

不明点や不足情報があれば、実装前に質問してください。特に複数フレーム送信機能に関する技術的な詳細については、メタプロンプト情報を参照してください。