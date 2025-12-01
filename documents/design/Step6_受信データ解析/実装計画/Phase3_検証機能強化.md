# Phase 3: 検証機能強化 実装仕様書

作成日: 2025-01-17
優先度: 🟢 中優先
対象プロジェクト: andon (C#)
参照元: 受信データ解析_実装方針決定.md

---

## 1. Phase 3 概要

### 1.1 目標

データ整合性の多層検証、詳細なエラーコードマッピング、TCP分割受信対応、統計記録機能を実装し、堅牢で運用性の高いシステムを構築する。

**Phase 2完了に伴う追加目標**: Phase 2で実装したビット展開機能をPlcCommunicationManagerに統合し、実際の通信フローで使用可能にする。

### 1.2 実装範囲

- **3-1**: デバイス点数多層検証
- **3-2**: 詳細エラーコードマッピング
- **3-3**: データ残存管理（TCP対応）
- **3-4**: 統計記録機能
- **3-5**: **Phase 2ビット展開機能の統合**（Phase 2完了に伴う追加項目）

### 1.3 予想工数

**合計**: 14-19時間
- デバイス点数検証: 2-3時間
- エラーコードマッピング: 3-4時間
- データ残存管理（TCP対応）: 4-5時間
- 統計記録機能: 2-3時間
- **ビット展開機能統合**: 3-4時間（追加）

---

## 2. デバイス点数多層検証

### 2.1 目的

複数の方法でデバイス点数を検証し、不一致時は警告を出力しつつ、実データ長を優先して処理を継続する。

### 2.2 検証方法

#### 2.2.1 3つの検証方法

1. **ヘッダのデータ長フィールドから計算**
   - 3E: データ長（7-8バイト目）- 2（終了コード分）
   - 4E: データ長（11-12バイト目）- 2（終了コード分）
   - デバイス点数 = データ長 / 2

2. **実データ長から計算**
   - デバイスデータ開始位置を取得
   - 実データ長 = rawData.Length - オフセット
   - デバイス点数 = 実データ長 / 2

3. **要求値との照合**
   - 送信時に要求したデバイス点数と比較

#### 2.2.2 優先順位

実データ長 > ヘッダ値 > 要求値

### 2.3 実装仕様

```csharp
/// <summary>
/// デバイス点数の多層検証
/// </summary>
/// <param name="rawData">受信データ</param>
/// <param name="frameType">フレームタイプ</param>
/// <param name="expectedCountFromRequest">要求時のデバイス点数</param>
/// <returns>デバイス点数と検証警告リスト</returns>
private (int DeviceCount, List<string> ValidationWarnings) ValidateDeviceCount(
    byte[] rawData,
    FrameType frameType,
    int expectedCountFromRequest)
{
    var warnings = new List<string>();

    // 方法1: ヘッダのデータ長フィールドから計算
    int dataLengthFromHeader = ExtractDataLengthField(rawData, frameType);
    int deviceCountFromHeader = (dataLengthFromHeader - 2) / 2;

    // 方法2: 実データ長から計算
    int deviceDataOffset = GetDeviceDataOffset(frameType);
    int deviceDataLength = rawData.Length - deviceDataOffset;
    int deviceCountFromActualData = deviceDataLength / 2;

    _logger.LogDebug(
        $"Device count validation: " +
        $"FromHeader={deviceCountFromHeader}, " +
        $"FromActualData={deviceCountFromActualData}, " +
        $"FromRequest={expectedCountFromRequest}");

    // 検証1: ヘッダ値と実データの一致
    if (deviceCountFromHeader != deviceCountFromActualData)
    {
        string warning = $"[WARNING] Device count mismatch: " +
            $"FromHeader={deviceCountFromHeader}, " +
            $"FromActualData={deviceCountFromActualData}";
        warnings.Add(warning);
        _logger.LogWarning(warning);

        // 統計記録
        _communicationStatistics?.RecordDeviceCountMismatch();
    }

    // 検証2: 要求値との照合
    if (deviceCountFromActualData != expectedCountFromRequest &&
        expectedCountFromRequest > 0)
    {
        string info = $"[INFO] Device count differs from request: " +
            $"Actual={deviceCountFromActualData}, " +
            $"Expected={expectedCountFromRequest}";
        warnings.Add(info);
        _logger.LogInformation(info);
    }

    // 実データ長を最優先
    return (deviceCountFromActualData, warnings);
}

/// <summary>
/// フレームタイプに応じたデータ長フィールドを抽出
/// </summary>
private int ExtractDataLengthField(byte[] rawData, FrameType frameType)
{
    return frameType switch
    {
        // Binary形式: リトルエンディアン
        FrameType.Frame3E_Binary => rawData[7] | (rawData[8] << 8),
        FrameType.Frame4E_Binary => rawData[11] | (rawData[12] << 8),

        // ASCII形式: 16進文字列
        FrameType.Frame3E_ASCII => Convert.ToInt32(
            Encoding.ASCII.GetString(rawData, 12, 4), 16),
        FrameType.Frame4E_ASCII => Convert.ToInt32(
            Encoding.ASCII.GetString(rawData, 22, 4), 16),  // D4(2) + 予約1(2) + シーケンス(4) + 予約2(4) + ネットワーク(2) + PC(2) + I/O(4) + 局番(2) = 22

        _ => throw new NotSupportedException(
            $"Unsupported frame type: {frameType}")
    };
}
```

### 2.4 実装要件チェックリスト

- 🔲 ValidateDeviceCount()メソッド実装
- 🔲 ExtractDataLengthField()メソッド実装
- 🔲 ProcessReceivedRawData()に統合
- 🔲 不一致時の警告ログ出力
- 🔲 統計記録機能との連携
- 🔲 単体テストケース作成

---

## 3. 詳細エラーコードマッピング

### 3.1 目的

PySLMPClient互換の詳細なエラーコードマッピングを実装し、終了コードの意味と重大度を明確化する。

### 3.2 実装仕様

#### 3.2.1 エラーコードクラス

```csharp
/// <summary>
/// SLMP終了コード詳細マッピング（PySLMPClient互換）
/// </summary>
public static class SlmpErrorCodes
{
    /// <summary>エラー情報</summary>
    public record SlmpErrorInfo(
        string Code,
        string Description,
        ErrorSeverity Severity);

    /// <summary>エラー重大度</summary>
    public enum ErrorSeverity
    {
        None,       // 正常
        Warning,    // 警告（処理継続可能）
        Error,      // エラー（処理失敗）
        Critical    // 致命的（接続切断推奨）
    }

    /// <summary>エラーコード辞書</summary>
    public static readonly Dictionary<ushort, SlmpErrorInfo> ErrorCatalog = new()
    {
        // 正常系
        { 0x0000, new("Success", "正常終了", ErrorSeverity.None) },

        // コマンド関連エラー
        { 0xC050, new("AsciiConversionError", "ASCII変換エラー", ErrorSeverity.Critical) },
        { 0xC051, new("InvalidDeviceCode", "不正なデバイスコード", ErrorSeverity.Error) },
        { 0xC052, new("InvalidDeviceNumber", "不正なデバイス番号", ErrorSeverity.Error) },
        { 0xC053, new("InvalidCommandData", "不正なコマンドデータ", ErrorSeverity.Error) },
        { 0xC054, new("InvalidDataSize", "不正なデータサイズ", ErrorSeverity.Error) },
        { 0xC055, new("InvalidDataContent", "不正なデータ内容", ErrorSeverity.Error) },
        { 0xC056, new("DeviceRangeExceeded", "デバイス範囲超過", ErrorSeverity.Error) },
        { 0xC057, new("InvalidDataSpecification", "不正なデータ指定", ErrorSeverity.Error) },
        { 0xC058, new("InvalidMonitoringCondition", "不正な監視条件", ErrorSeverity.Error) },
        { 0xC059, new("DataLengthMismatch", "データ長不一致", ErrorSeverity.Error) },
        { 0xC05A, new("InvalidDeviceName", "不正なデバイス名称", ErrorSeverity.Error) },
        { 0xC05B, new("InvalidCommand", "不正なコマンド", ErrorSeverity.Error) },
        { 0xC05C, new("InvalidSubCommand", "不正なサブコマンド", ErrorSeverity.Error) },
        { 0xC05D, new("InvalidBlockNumber", "不正なブロック番号", ErrorSeverity.Error) },
        { 0xC05E, new("InvalidProgramNumber", "不正なプログラム番号", ErrorSeverity.Error) },
        { 0xC05F, new("InvalidRequestDataLength", "不正な要求データ長", ErrorSeverity.Error) },

        // PLC状態エラー
        { 0xC060, new("PlcPasswordIncorrect", "PLCパスワード不一致", ErrorSeverity.Critical) },
        { 0xC061, new("PlcPasswordNotSet", "PLCパスワード未設定", ErrorSeverity.Warning) },
        { 0xC070, new("DataProtected", "データ保護中", ErrorSeverity.Warning) },
        { 0xC0B5, new("KeySwitchStopPosition", "キースイッチがSTOP位置", ErrorSeverity.Critical) },

        // PLC動作モード
        { 0xC100, new("PlcRunMode", "PLC RUNモード中", ErrorSeverity.Warning) },
        { 0xC101, new("PlcStopMode", "PLC STOPモード中", ErrorSeverity.Warning) },

        // 通信エラー
        { 0xC200, new("CommunicationTimeout", "通信タイムアウト", ErrorSeverity.Critical) },
        { 0xC201, new("CommunicationError", "通信エラー", ErrorSeverity.Critical) },

        // CPU/ユニットエラー
        { 0xC0C0, new("CpuError", "CPU異常", ErrorSeverity.Critical) },
        { 0xC0C1, new("UnitNumberOutOfRange", "ユニット番号範囲外", ErrorSeverity.Error) },
        { 0xC0C2, new("UnitNotFound", "指定ユニット未実装", ErrorSeverity.Error) },
        { 0xC0C3, new("UnitBusyError", "ユニットビジー", ErrorSeverity.Warning) },
    };

    /// <summary>
    /// エラー情報を取得
    /// </summary>
    /// <param name="endCode">終了コード</param>
    /// <returns>エラー情報（未知の場合はUnknownError）</returns>
    public static SlmpErrorInfo GetErrorInfo(ushort endCode)
    {
        return ErrorCatalog.TryGetValue(endCode, out var info)
            ? info
            : new SlmpErrorInfo("UnknownError",
                $"不明なエラー (0x{endCode:X4})",
                ErrorSeverity.Critical);
    }

    /// <summary>
    /// エラーかどうかを判定
    /// </summary>
    public static bool IsError(ushort endCode) => endCode != 0x0000;

    /// <summary>
    /// 致命的なエラーかどうかを判定
    /// </summary>
    public static bool IsCritical(ushort endCode)
    {
        var info = GetErrorInfo(endCode);
        return info.Severity == ErrorSeverity.Critical;
    }
}
```

#### 3.2.2 エラーハンドリングの統合

ProcessReceivedRawData()での使用:

```csharp
// Step-5 終了コード確認（詳細エラーマッピング適用）
if (frameData.EndCode != 0x0000)
{
    var errorInfo = SlmpErrorCodes.GetErrorInfo(frameData.EndCode);

    _logger.LogError(
        $"PLC returned error: {errorInfo.Code} (0x{frameData.EndCode:X4}) - {errorInfo.Description}");

    // 統計記録
    _communicationStatistics?.RecordErrorCode(frameData.EndCode);

    // 重大度に応じた処理
    switch (errorInfo.Severity)
    {
        case SlmpErrorCodes.ErrorSeverity.Critical:
            throw new InvalidOperationException(
                $"Critical PLC error: {errorInfo.Code} (0x{frameData.EndCode:X4}) - {errorInfo.Description}");

        case SlmpErrorCodes.ErrorSeverity.Error:
            throw new InvalidOperationException(
                $"PLC error: {errorInfo.Code} (0x{frameData.EndCode:X4}) - {errorInfo.Description}");

        case SlmpErrorCodes.ErrorSeverity.Warning:
            _logger.LogWarning(
                $"PLC warning: {errorInfo.Code} (0x{frameData.EndCode:X4}) - {errorInfo.Description}");
            // 処理継続
            break;
    }
}
```

### 3.3 実装要件チェックリスト

- 🔲 SlmpErrorCodesクラス実装
- 🔲 エラーコード辞書作成（30種類以上）
- 🔲 GetErrorInfo()メソッド実装
- 🔲 重大度別エラーハンドリング
- 🔲 統計記録機能との連携
- 🔲 単体テストケース作成

---

## 4. データ残存管理（TCP対応）

### 4.1 目的

TCP通信での分割受信に対応し、フレームの完全性を保証する。

### 4.2 TCP vs UDP の違い

| 項目 | UDP | TCP |
|-----|-----|-----|
| **受信単位** | 1回で完全なフレーム | 分割される可能性あり |
| **データ残存** | 不要 | 必須 |
| **実装複雑度** | 低 | 高 |

**重要**: 現在の実装（UDP専用）では、TCP使用時にフレーム解析が失敗する可能性がある。

### 4.3 実装仕様

#### 4.3.1 TcpFrameBufferManagerクラス

PySLMPClientの`self.__rest`互換機能:

```csharp
/// <summary>
/// TCP通信用のデータ残存管理機能
/// PySLMPClientの self.__rest 互換
/// </summary>
public class TcpFrameBufferManager
{
    private byte[] _receiveBuffer = Array.Empty<byte>();
    private readonly object _bufferLock = new object();
    private readonly ILogger _logger;

    public TcpFrameBufferManager(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 受信データを前回の残存データと結合し、完全なフレームを抽出
    /// </summary>
    /// <param name="newData">新しく受信したデータ</param>
    /// <returns>完全なフレーム（未完成の場合はnull）とフレーム完成フラグ</returns>
    public (byte[]? CompleteFrame, bool IsComplete) ProcessReceivedData(byte[] newData)
    {
        lock (_bufferLock)
        {
            // 前回残データと今回受信データを連結
            byte[] combined = _receiveBuffer.Length > 0
                ? _receiveBuffer.Concat(newData).ToArray()
                : newData;

            _logger.LogDebug(
                $"TCP buffer processing: Previous={_receiveBuffer.Length} bytes, " +
                $"New={newData.Length} bytes, Combined={combined.Length} bytes");

            // フレーム完全性チェック
            if (!IsFrameComplete(combined, out int frameLength))
            {
                // フレーム未完成 - バッファに保存して次回待機
                _receiveBuffer = combined;
                _logger.LogDebug($"Frame incomplete, buffered {combined.Length} bytes");
                return (null, false);
            }

            // 完全なフレームを抽出
            byte[] completeFrame = new byte[frameLength];
            Array.Copy(combined, 0, completeFrame, 0, frameLength);

            // 残りデータを保存
            int remainingLength = combined.Length - frameLength;
            if (remainingLength > 0)
            {
                _receiveBuffer = new byte[remainingLength];
                Array.Copy(combined, frameLength, _receiveBuffer, 0, remainingLength);
                _logger.LogDebug($"Frame extracted ({frameLength} bytes), remaining {remainingLength} bytes");
            }
            else
            {
                _receiveBuffer = Array.Empty<byte>();
                _logger.LogDebug($"Frame extracted ({frameLength} bytes), buffer cleared");
            }

            return (completeFrame, true);
        }
    }

    /// <summary>
    /// フレームが完全かどうかを判定
    /// </summary>
    private bool IsFrameComplete(byte[] data, out int frameLength)
    {
        frameLength = 0;

        // 最小フレーム長チェック
        if (data.Length < 11) // 3Eフレームの最小長
        {
            return false;
        }

        try
        {
            // フレームタイプ判定
            FrameType frameType = DetectFrameType(data);

            // データ長フィールドの位置と値を取得
            int dataLengthOffset = frameType switch
            {
                FrameType.Frame3E_Binary => 7,
                FrameType.Frame4E_Binary => 11,
                FrameType.Frame3E_ASCII => 12,  // 文字位置
                FrameType.Frame4E_ASCII => 22,  // 文字位置（D4 + 予約1(2) + シーケンス(4) + 予約2(4) + ネットワーク(2) + PC(2) + I/O(4) + 局番(2) = 22）
                _ => throw new NotSupportedException()
            };

            // データ長抽出
            int dataLength;
            if (frameType == FrameType.Frame3E_ASCII || frameType == FrameType.Frame4E_ASCII)
            {
                // ASCII形式: 16進文字列
                if (data.Length < dataLengthOffset + 4)
                    return false;

                string hexLength = Encoding.ASCII.GetString(data, dataLengthOffset, 4);
                dataLength = Convert.ToInt32(hexLength, 16);

                // ASCII形式: 文字数計算
                int headerLength = frameType == FrameType.Frame3E_ASCII ? 20 : 30;
                frameLength = headerLength + dataLength * 2; // HEX文字列は2倍
            }
            else
            {
                // Binary形式
                if (data.Length < dataLengthOffset + 2)
                    return false;

                dataLength = data[dataLengthOffset] | (data[dataLengthOffset + 1] << 8);

                // Binary形式: バイト数計算
                int headerLength = frameType == FrameType.Frame3E_Binary ? 9 : 13;
                frameLength = headerLength + dataLength;
            }

            // 実際のデータ長がフレーム長以上か確認
            bool isComplete = data.Length >= frameLength;

            _logger.LogDebug(
                $"Frame completeness check: Type={frameType}, " +
                $"DataLength={dataLength}, FrameLength={frameLength}, " +
                $"ActualLength={data.Length}, IsComplete={isComplete}");

            return isComplete;
        }
        catch (Exception ex)
        {
            // フレーム判定エラー時は未完成とみなす
            _logger.LogWarning(ex, "Frame type detection failed, treating as incomplete");
            return false;
        }
    }

    /// <summary>
    /// フレームタイプを簡易判定（エラー処理簡素版）
    /// </summary>
    private FrameType DetectFrameType(byte[] data)
    {
        if (data.Length < 2)
            throw new ArgumentException("Data too short");

        if (data[0] == 0x44) // 'D'
        {
            return data[1] switch
            {
                0x30 => FrameType.Frame3E_ASCII,
                0x34 => FrameType.Frame4E_ASCII,
                _ => throw new FormatException()
            };
        }

        return (data[0], data[1]) switch
        {
            (0xD0, 0x00) => FrameType.Frame3E_Binary,
            (0xD4, 0x00) => FrameType.Frame4E_Binary,
            _ => throw new FormatException()
        };
    }

    /// <summary>
    /// バッファをクリア（接続リセット時に使用）
    /// </summary>
    public void ClearBuffer()
    {
        lock (_bufferLock)
        {
            _logger.LogInformation($"Clearing TCP buffer ({_receiveBuffer.Length} bytes)");
            _receiveBuffer = Array.Empty<byte>();
        }
    }

    /// <summary>
    /// 現在のバッファサイズを取得（デバッグ用）
    /// </summary>
    public int BufferSize
    {
        get
        {
            lock (_bufferLock)
            {
                return _receiveBuffer.Length;
            }
        }
    }
}
```

#### 4.3.2 PlcCommunicationManagerへの統合

```csharp
public class PlcCommunicationManager
{
    private TcpFrameBufferManager? _tcpBufferManager;

    /// <summary>
    /// 完全なフレームを受信（UDP/TCP自動切替）
    /// </summary>
    private async Task<byte[]> ReceiveCompleteFrameAsync(CancellationToken ct)
    {
        if (_connectionConfig.Protocol == ProtocolType.Tcp)
        {
            // TCP: データ残存管理を使用
            _tcpBufferManager ??= new TcpFrameBufferManager(_logger);

            while (!ct.IsCancellationRequested)
            {
                byte[] buffer = new byte[4096];
                int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, ct);

                if (bytesRead == 0)
                {
                    _logger.LogError("Connection closed by remote host");
                    throw new IOException("Connection closed by remote host");
                }

                byte[] receivedData = new byte[bytesRead];
                Array.Copy(buffer, 0, receivedData, 0, bytesRead);

                var (completeFrame, isComplete) = _tcpBufferManager.ProcessReceivedData(receivedData);

                if (isComplete && completeFrame != null)
                {
                    _logger.LogDebug(
                        $"Complete frame received: {completeFrame.Length} bytes, " +
                        $"Buffer remaining: {_tcpBufferManager.BufferSize} bytes");
                    return completeFrame;
                }

                _logger.LogDebug(
                    $"Incomplete frame, waiting for more data. " +
                    $"Current buffer: {_tcpBufferManager.BufferSize} bytes");
            }

            throw new OperationCanceledException();
        }
        else
        {
            // UDP: 従来通り1回の受信で完結
            byte[] buffer = new byte[4096];
            int bytesRead = await _socket.ReceiveAsync(buffer, ct);
            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, 0, receivedData, 0, bytesRead);

            _logger.LogDebug($"UDP frame received: {bytesRead} bytes");
            return receivedData;
        }
    }

    /// <summary>
    /// 接続切断時の処理
    /// </summary>
    public void Disconnect()
    {
        // TCP バッファクリア
        _tcpBufferManager?.ClearBuffer();
        _tcpBufferManager = null;

        // ソケットクローズ
        _socket?.Close();
        _networkStream?.Close();
    }
}
```

### 4.4 実装要件チェックリスト

- 🔲 TcpFrameBufferManagerクラス実装
- 🔲 IsFrameComplete()ロジック実装
- 🔲 UDP/TCP自動切替機能
- 🔲 接続リセット時のバッファクリア
- 🔲 ログ出力（バッファ状態、フレーム完成状況）
- 🔲 単体テストケース作成
  - 単一フレームの完全受信
  - 2分割受信（ヘッダ/データ）
  - 3分割以上の受信
  - 複数フレームの連続受信

---

## 5. 統計記録機能

### 5.1 目的

通信動作の統計情報を記録・分析し、システムの運用性とデバッグ性を向上させる。

### 5.2 実装仕様

```csharp
/// <summary>
/// 通信統計情報の記録と管理
/// </summary>
public class CommunicationStatistics
{
    // フレームタイプ使用統計
    private readonly Dictionary<FrameType, int> _frameTypeUsage = new();
    private readonly object _frameTypeLock = new();

    // エラーコード発生統計
    private readonly Dictionary<ushort, int> _errorCodeFrequency = new();
    private readonly object _errorCodeLock = new();

    // デバイス点数不一致統計
    private int _deviceCountMismatchCount = 0;

    // 処理時間統計
    private readonly List<double> _processingTimes = new();
    private readonly object _processingTimeLock = new();

    // TCP分割受信統計
    private int _fragmentedFrameCount = 0;
    private int _totalFragments = 0;

    /// <summary>フレームタイプ使用を記録</summary>
    public void RecordFrameType(FrameType frameType)
    {
        lock (_frameTypeLock)
        {
            if (!_frameTypeUsage.ContainsKey(frameType))
                _frameTypeUsage[frameType] = 0;
            _frameTypeUsage[frameType]++;
        }
    }

    /// <summary>エラーコード発生を記録</summary>
    public void RecordErrorCode(ushort errorCode)
    {
        lock (_errorCodeLock)
        {
            if (!_errorCodeFrequency.ContainsKey(errorCode))
                _errorCodeFrequency[errorCode] = 0;
            _errorCodeFrequency[errorCode]++;
        }
    }

    /// <summary>デバイス点数不一致を記録</summary>
    public void RecordDeviceCountMismatch()
    {
        Interlocked.Increment(ref _deviceCountMismatchCount);
    }

    /// <summary>処理時間を記録</summary>
    public void RecordProcessingTime(double milliseconds)
    {
        lock (_processingTimeLock)
        {
            _processingTimes.Add(milliseconds);

            // メモリ節約: 最新1000件のみ保持
            if (_processingTimes.Count > 1000)
                _processingTimes.RemoveAt(0);
        }
    }

    /// <summary>TCP分割受信を記録</summary>
    public void RecordFragmentedFrame(int fragmentCount)
    {
        Interlocked.Increment(ref _fragmentedFrameCount);
        Interlocked.Add(ref _totalFragments, fragmentCount);
    }

    /// <summary>統計レポートを生成</summary>
    public string GetStatisticsReport()
    {
        var report = new StringBuilder();
        report.AppendLine("=== PLC Communication Statistics ===");
        report.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        // フレームタイプ統計
        report.AppendLine("[Frame Type Usage]");
        lock (_frameTypeLock)
        {
            if (_frameTypeUsage.Any())
            {
                foreach (var (frameType, count) in _frameTypeUsage.OrderByDescending(x => x.Value))
                {
                    report.AppendLine($"  {frameType}: {count} times");
                }
            }
            else
            {
                report.AppendLine("  No data");
            }
        }
        report.AppendLine();

        // エラーコード統計
        report.AppendLine("[Error Code Frequency]");
        lock (_errorCodeLock)
        {
            if (_errorCodeFrequency.Any())
            {
                foreach (var (errorCode, count) in _errorCodeFrequency.OrderByDescending(x => x.Value).Take(10))
                {
                    var errorInfo = SlmpErrorCodes.GetErrorInfo(errorCode);
                    report.AppendLine($"  0x{errorCode:X4} ({errorInfo.Description}): {count} times");
                }
            }
            else
            {
                report.AppendLine("  No errors");
            }
        }
        report.AppendLine();

        // 処理時間統計
        report.AppendLine("[Processing Time Statistics]");
        lock (_processingTimeLock)
        {
            if (_processingTimes.Any())
            {
                report.AppendLine($"  Average: {_processingTimes.Average():F2} ms");
                report.AppendLine($"  Min: {_processingTimes.Min():F2} ms");
                report.AppendLine($"  Max: {_processingTimes.Max():F2} ms");
                report.AppendLine($"  Total samples: {_processingTimes.Count}");
            }
            else
            {
                report.AppendLine("  No data");
            }
        }
        report.AppendLine();

        // TCP分割受信統計
        if (_fragmentedFrameCount > 0)
        {
            report.AppendLine("[TCP Fragmentation Statistics]");
            report.AppendLine($"  Fragmented Frames: {_fragmentedFrameCount}");
            report.AppendLine($"  Total Fragments: {_totalFragments}");
            report.AppendLine($"  Avg Fragments per Frame: {(double)_totalFragments / _fragmentedFrameCount:F2}");
            report.AppendLine();
        }

        // その他統計
        report.AppendLine("[Other Statistics]");
        report.AppendLine($"  Device Count Mismatches: {_deviceCountMismatchCount}");

        return report.ToString();
    }

    /// <summary>CSV形式で出力</summary>
    public async Task ExportToCsvAsync(string outputPath)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Category,Item,Count,Value");

        lock (_frameTypeLock)
        {
            foreach (var (frameType, count) in _frameTypeUsage)
            {
                csv.AppendLine($"FrameType,{frameType},{count},");
            }
        }

        lock (_errorCodeLock)
        {
            foreach (var (errorCode, count) in _errorCodeFrequency)
            {
                var errorInfo = SlmpErrorCodes.GetErrorInfo(errorCode);
                csv.AppendLine($"ErrorCode,0x{errorCode:X4}_{errorInfo.Code},{count},{errorInfo.Description}");
            }
        }

        csv.AppendLine($"DeviceCountMismatch,Total,{_deviceCountMismatchCount},");

        lock (_processingTimeLock)
        {
            if (_processingTimes.Any())
            {
                csv.AppendLine($"ProcessingTime,Average,{_processingTimes.Count},{_processingTimes.Average():F2}");
                csv.AppendLine($"ProcessingTime,Min,{_processingTimes.Count},{_processingTimes.Min():F2}");
                csv.AppendLine($"ProcessingTime,Max,{_processingTimes.Count},{_processingTimes.Max():F2}");
            }
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString());
    }
}
```

### 5.3 実装要件チェックリスト

- 🔲 CommunicationStatisticsクラス実装
- 🔲 各種統計記録メソッド実装
- 🔲 GetStatisticsReport()実装
- 🔲 ExportToCsvAsync()実装
- 🔲 定期的な統計出力（タイマー機能）
- 🔲 スレッドセーフ処理
- 🔲 メモリ使用量制限（1000件保持）

---

## 5. Phase 2ビット展開機能の統合

### 5.1 目的

Phase 2で実装・テスト完了したビット展開機能（BitExpansionUtility, ProcessedDevice拡張, BitExpansionSettings）をPlcCommunicationManagerに統合し、実際のPLC通信フローの中で使用可能にする。

### 5.2 Phase 2完了時点の状態

#### 5.2.1 実装済み項目

✅ **BitExpansionUtilityクラス**（`andon/Utilities/BitExpansionUtility.cs`）
- ExpandWordToBits(ushort): ワード値を16ビット配列に展開（LSB first）
- ExpandWordToBits(int): int版オーバーロード
- ExpandMultipleWordsToBits(ushort[]): 複数ワード一括展開
- ExpandWithSelectionMask(...): 選択的ビット展開（ConMoni互換）

✅ **ProcessedDeviceクラス拡張**（`andon/Core/Models/ProcessedDevice.cs`）
- RawValue: 元のワード値
- ConvertedValue: 変換係数適用後の値
- ConversionFactor: 変換係数
- IsBitExpanded: ビット展開フラグ
- ExpandedBits: ビット配列（16要素）
- GetBit(int): ビット値の名前付き取得

✅ **BitExpansionSettingsクラス**（`andon/Core/Models/ConfigModels/BitExpansionSettings.cs`）
- Enabled: ビット展開機能の有効/無効
- SelectionMask: デバイスごとのビット展開フラグ配列
- ConversionFactors: 変換係数配列（ConMoniのdigitControl互換）
- Validate(): 設定の妥当性検証

✅ **テスト完了**
- 全22個のテストケース合格（100%成功率）
- ConMoni互換性確認済み
- LSB first順序の正確性検証済み

#### 5.2.2 未実装項目（Phase 3で実装）

⏳ **PlcCommunicationManagerへの統合**
- ProcessReceivedRawData()メソッドへの組み込み
- ApplyBitExpansion()プライベートメソッド追加
- BitExpansionSettings読み込み処理

⏳ **appsettings.json設定の追加**
- DataProcessing:BitExpansion設定セクションの追加
- SelectionMask, ConversionFactorsのサンプル設定

⏳ **統合テスト**
- 実際のPLCデータでの動作確認
- ConMoniとの出力比較

### 5.3 実装仕様

#### 5.3.1 appsettings.json設定の追加

**ファイル**: `appsettings.json`

```json
{
  "PlcCommunication": {
    "Connection": { ... },
    "Timeouts": { ... },
    "TargetDevices": { ... },
    "MonitoringIntervalMs": 1000,

    // ★追加セクション★
    "DataProcessing": {
      "BitExpansion": {
        // ★将来的にExcel設定ファイル(device_config.xlsx)から自動生成される想定★
        // 現在は手動で記載。Phase 4以降でExcel連携機能を実装予定。

        "_comment": "ビット展開機能の設定（ConMoni互換）",
        "Enabled": true,

        "_SelectionMask_comment": "デバイスごとのビット展開フラグ。true=16ビット展開、false=ワード値のまま。将来はExcelの「BitExpand」列から自動生成。",
        "SelectionMask": [
          false, false, false, false, false, false, false, false, false, false,
          true, true, true, true, true, true, true, true
        ],

        "_ConversionFactors_comment": "変換係数配列（ConMoniのdigitControl互換）。各デバイス値に乗算される係数。将来はExcelの「ConversionFactor」列から自動生成。",
        "ConversionFactors": [
          1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0,
          1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0
        ]

        // ★将来の拡張項目（Phase 4以降）★
        // "ExcelSettingFilePath": "settings/device_config.xlsx",
        // "UseExcelAsSource": false  // trueの場合、Excel設定を優先
      }
    }
  },
  "SystemResources": { ... },
  "Logging": { ... }
}
```

**設定例の説明**:
- デバイス0-9: センサー値などのワード値（SelectionMask=false）
- デバイス10-17: 状態ビット群をビット展開（SelectionMask=true）
- 変換係数: 全て1.0（変換なし）

**ConMoni互換設定の取得方法**:
ConMoniの設定ファイル（`settings/settingJson/*.json`）から以下の項目を取得:
- `accessBitDataLoc` → `SelectionMask`に変換（0→false, 1→true）
- `accessDeviceDigit` → `ConversionFactors`にコピー

#### 5.3.2 PlcCommunicationManagerへの統合

**ファイル**: `andon/Core/Managers/PlcCommunicationManager.cs`

**コンストラクタへの追加**:

```csharp
public class PlcCommunicationManager : IPlcCommunicationManager
{
    private readonly BitExpansionSettings _bitExpansionSettings;

    public PlcCommunicationManager(
        IConfiguration configuration,
        ILogger<PlcCommunicationManager> logger)
    {
        _logger = logger;

        // 既存の設定読み込み処理 ...

        // ★ビット展開設定の読み込み★
        _bitExpansionSettings = configuration
            .GetSection("PlcCommunication:DataProcessing:BitExpansion")
            .Get<BitExpansionSettings>() ?? new BitExpansionSettings();

        // ★将来の拡張: Excel設定ファイルからの読み込み（Phase 4以降）★
        // if (_bitExpansionSettings.UseExcelAsSource &&
        //     !string.IsNullOrEmpty(_bitExpansionSettings.ExcelSettingFilePath))
        // {
        //     try
        //     {
        //         _bitExpansionSettings.LoadFromExcel(_bitExpansionSettings.ExcelSettingFilePath);
        //         _logger.LogInformation($"Excel設定ファイルから読み込み成功: {_bitExpansionSettings.ExcelSettingFilePath}");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogWarning($"Excel設定ファイルの読み込み失敗: {ex.Message}。JSON設定を使用します。");
        //     }
        // }

        // 設定の妥当性検証
        try
        {
            _bitExpansionSettings.Validate();
            _logger.LogInformation($"BitExpansion設定読み込み完了: Enabled={_bitExpansionSettings.Enabled}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"BitExpansion設定の検証失敗: {ex.Message}。機能は無効化されます。");
            _bitExpansionSettings.Enabled = false;
        }
    }

    // ... 既存のメソッド
}
```

**ApplyBitExpansion()プライベートメソッドの追加**:

```csharp
/// <summary>
/// デバイス値にビット展開を適用（ConMoni互換）
/// Phase 2で実装したBitExpansionUtilityを使用
/// </summary>
/// <param name="devices">処理済みデバイスリスト</param>
/// <param name="settings">ビット展開設定</param>
/// <returns>ビット展開適用後のデバイスリスト</returns>
private List<ProcessedDevice> ApplyBitExpansion(
    List<ProcessedDevice> devices,
    BitExpansionSettings settings)
{
    // ビット展開が無効な場合はそのまま返却
    if (!settings.Enabled)
    {
        _logger.LogDebug("Bit expansion is disabled");
        return devices;
    }

    // 設定検証（念のため再確認）
    try
    {
        settings.Validate();
    }
    catch (Exception ex)
    {
        _logger.LogWarning($"Bit expansion validation failed: {ex.Message}. Skipping bit expansion.");
        return devices;
    }

    // デバイス数と設定の長さチェック
    if (devices.Count != settings.SelectionMask.Length)
    {
        _logger.LogWarning(
            $"Device count ({devices.Count}) does not match SelectionMask length ({settings.SelectionMask.Length}). " +
            $"Bit expansion will be skipped.");
        return devices;
    }

    _logger.LogDebug($"Applying bit expansion to {devices.Count} devices");

    for (int i = 0; i < devices.Count; i++)
    {
        var device = devices[i];

        // 変換係数適用
        if (settings.ConversionFactors.Length > 0 && i < settings.ConversionFactors.Length)
        {
            device.ConversionFactor = settings.ConversionFactors[i];
            device.ConvertedValue = device.RawValue * device.ConversionFactor;
        }
        else
        {
            device.ConversionFactor = 1.0;
            device.ConvertedValue = device.RawValue;
        }

        // ビット展開フラグ確認
        if (settings.SelectionMask[i])
        {
            // ビット展開モード
            device.IsBitExpanded = true;
            device.ExpandedBits = BitExpansionUtility.ExpandWordToBits(device.RawValue);
            device.DataType = "Bits";

            _logger.LogDebug(
                $"Device {device.DeviceName}: Expanded to bits (Raw=0x{device.RawValue:X4})");
        }
        else
        {
            // ワード値モード
            device.IsBitExpanded = false;
            device.ExpandedBits = null;
            device.DataType = "Word";

            _logger.LogDebug(
                $"Device {device.DeviceName}: Kept as word (Value={device.ConvertedValue}, Factor={device.ConversionFactor})");
        }
    }

    return devices;
}
```

**ProcessReceivedRawData()への統合**:

```csharp
public async Task<BasicProcessedResponseData> ProcessReceivedRawData(
    byte[] rawData,
    ProcessedDeviceRequestInfo processedRequestInfo,
    CancellationToken cancellationToken = default)
{
    // ... 既存の処理（Step-1 ～ Step-6）

    // Step-6 処理済みデバイスリストに追加
    foreach (var device in extractedDevices)
    {
        result.ProcessedDevices.Add(device);
        Console.WriteLine($"[DEBUG] デバイス値抽出: {device.DeviceName}={device.Value}({device.DataType})");
    }
    result.ProcessedDeviceCount = result.ProcessedDevices.Count;

    // ★★★ Step-7 ビット展開適用（Phase 2追加機能の統合）★★★
    if (_bitExpansionSettings.Enabled)
    {
        Console.WriteLine($"[INFO] ビット展開処理開始: デバイス数={result.ProcessedDevices.Count}");

        result.ProcessedDevices = ApplyBitExpansion(
            result.ProcessedDevices,
            _bitExpansionSettings);

        Console.WriteLine($"[INFO] ビット展開処理完了");
    }

    // Step-8 処理時間計算
    stopwatch.Stop();
    result.ProcessingTimeMs = Math.Max(stopwatch.ElapsedMilliseconds, 1);

    // ログ出力: 処理完了
    Console.WriteLine($"[INFO] ProcessReceivedRawData完了: 処理デバイス数={result.ProcessedDeviceCount}, 所要時間={result.ProcessingTimeMs}ms");

    return result;
}
```

#### 5.3.3 BitExpansionSettingsクラスへの将来拡張コメント追加

**ファイル**: `andon/Core/Models/ConfigModels/BitExpansionSettings.cs`

クラス冒頭のドキュメントコメントに以下を追加:

```csharp
/// <summary>
/// ビット展開設定（ConMoni互換）
///
/// ★Phase 2実装完了項目★
/// - Enabled, SelectionMask, ConversionFactors
/// - Validate()メソッド
///
/// ★将来の実装計画（Phase 4以降）★
/// - Excel設定ファイルからの読み込み機能
/// - Excel監視・自動リロード機能
/// - Excel → JSON変換ツール
///
/// Excelフォーマット想定:
/// | DeviceNo | DeviceName | DataType | BitExpand | ConversionFactor |
/// |----------|------------|----------|-----------|------------------|
/// | 0        | DATETIME   | Word     | FALSE     | 1.0              |
/// | 10       | シャッター  | Bit      | TRUE      | 1.0              |
///
/// TODO (Phase 4):
/// - EPPlus or ClosedXMLライブラリの導入
/// - LoadFromExcel()メソッドの実装
/// - Excel変更監視機能の実装
/// </summary>
public class BitExpansionSettings
{
    // ... 既存のプロパティ

    // ★将来の拡張項目（現在はコメントアウト）★
    // TODO (Phase 4): Excelファイル連携機能
    // /// <summary>Excel設定ファイルのパス</summary>
    // public string? ExcelSettingFilePath { get; set; }
    //
    // /// <summary>Excel設定を優先するか（trueの場合、Excelから読み込み）</summary>
    // public bool UseExcelAsSource { get; set; } = false;
    //
    // /// <summary>
    // /// Excelファイルから設定を読み込む
    // /// </summary>
    // /// <param name="excelFilePath">Excelファイルパス</param>
    // public void LoadFromExcel(string excelFilePath)
    // {
    //     // TODO: EPPlusやClosedXMLを使ってExcelを読み込み
    //     // SelectionMaskとConversionFactorsを自動生成
    // }
}
```

### 5.4 統合テスト計画

#### 5.4.1 単体統合テスト

**テストケース1: ビット展開無効時**
```csharp
// 設定: Enabled = false
// 期待結果: デバイスはワード値のまま、ビット展開されない
```

**テストケース2: 全ワードモード**
```csharp
// 設定: SelectionMask = [false, false, false]
// 期待結果: 全デバイスがワード値として処理される
```

**テストケース3: 選択的ビット展開**
```csharp
// 設定: SelectionMask = [false, true, false]
// 期待結果: デバイス1のみ16ビット展開、他はワード値
```

**テストケース4: 変換係数適用**
```csharp
// 設定: ConversionFactors = [1.0, 0.1, 10.0]
// 期待結果: 各デバイスに係数が適用される
```

**テストケース5: 設定長不一致**
```csharp
// 設定: デバイス数=5, SelectionMask長=3
// 期待結果: 警告ログ出力、ビット展開スキップ
```

#### 5.4.2 ConMoni互換性テスト

**目的**: ConMoniと同じ入力で同じ出力を得る

**準備**:
1. ConMoniの設定ファイル（`6-注液-CSK（N2BOX）-freq_1_setting.json`）を使用
2. `accessBitDataLoc` → `SelectionMask`に変換
3. `accessDeviceDigit` → `ConversionFactors`にコピー
4. 同じPLCデータで両方を実行

**検証**:
- ビット展開されたデバイスの順序が一致
- 各ビット値が一致（LSB first順序）
- ワード値デバイスの値が一致
- 変換係数適用後の値が一致

#### 5.4.3 実機データ統合テスト

**テストシナリオ**:
1. 実際のPLCに接続
2. デバイスD500-D517（18個）を読み取り
3. ビット展開設定を適用
4. ProcessedDeviceリストの内容を確認
5. ログ出力を確認

**検証項目**:
- [ ] 接続成功
- [ ] データ読み取り成功
- [ ] ビット展開処理の実行確認
- [ ] ProcessedDeviceの各フィールド（RawValue, ConvertedValue, IsBitExpanded, ExpandedBits）が正しく設定されている
- [ ] ログ出力が適切（デバッグログ、情報ログ）
- [ ] エラーなく処理完了

### 5.5 実装要件チェックリスト

- 🔲 appsettings.jsonにBitExpansion設定セクション追加
- 🔲 PlcCommunicationManagerコンストラクタで設定読み込み
- 🔲 ApplyBitExpansion()プライベートメソッド実装
- 🔲 ProcessReceivedRawData()にStep-7としてビット展開処理を追加
- 🔲 BitExpansionSettingsクラスに将来拡張コメント追加
- 🔲 単体統合テスト実施（5パターン）
- 🔲 ConMoni互換性テスト実施
- 🔲 実機データ統合テスト実施
- 🔲 実装記録ドキュメント作成
- 🔲 テスト結果レポート作成

### 5.6 実装時の注意点

#### 5.6.1 設定読み込みエラーのハンドリング

設定ファイルが不正な場合でも、システム全体が停止しないようにする:

```csharp
try
{
    _bitExpansionSettings.Validate();
}
catch (Exception ex)
{
    _logger.LogWarning($"BitExpansion設定の検証失敗: {ex.Message}。機能は無効化されます。");
    _bitExpansionSettings.Enabled = false;  // 自動的に無効化
}
```

#### 5.6.2 デバイス数不一致の処理

デバイス数と設定配列の長さが一致しない場合:
- エラーではなく警告として扱う
- ビット展開をスキップして処理を継続
- ログに詳細な情報を出力

#### 5.6.3 後方互換性の維持

ビット展開機能が無効（Enabled=false）の場合、既存の動作を完全に維持:
- ProcessedDeviceの既存フィールド（Value, DataType）は変更なし
- ログ出力の増加のみ

#### 5.6.4 将来のExcel連携への準備

コメントで将来の拡張を明示:
- appsettings.jsonにコメント記載
- BitExpansionSettingsクラスにコメントアウトされた拡張プロパティ
- PlcCommunicationManagerにコメントアウトされたExcel読み込みコード

これにより、Phase 4でExcel連携機能を追加する際に、設計意図が明確に伝わる。

---

## 6. 実装手順

### 6.1 推奨実装順序

**Phase 2ビット展開機能統合を最優先で実施** ← Phase 2完了に伴う変更

1. **Phase 2ビット展開機能統合**（3-4時間）
   - appsettings.jsonにBitExpansion設定追加
   - PlcCommunicationManagerコンストラクタで設定読み込み
   - ApplyBitExpansion()プライベートメソッド実装
   - ProcessReceivedRawData()にStep-7追加
   - BitExpansionSettingsクラスにコメント追加
   - 単体統合テスト（5パターン）
   - ConMoni互換性テスト

2. **デバイス点数検証**（2-3時間）
   - ValidateDeviceCount()実装
   - ExtractDataLengthField()実装
   - ProcessReceivedRawData()に統合

3. **エラーコードマッピング**（3-4時間）
   - SlmpErrorCodesクラス実装
   - エラーコード辞書作成
   - 重大度別エラーハンドリング

4. **統計記録機能**（2-3時間）
   - CommunicationStatisticsクラス実装
   - 各所での統計記録呼び出し
   - CSV出力機能

5. **TCP対応**（4-5時間）
   - TcpFrameBufferManagerクラス実装
   - IsFrameComplete()ロジック
   - UDP/TCP自動切替

6. **統合テスト**（2時間）
   - 全機能の動作確認

**合計**: 14-19時間

### 6.2 実装時の注意点

#### 6.2.1 スレッドセーフ性

統計記録は複数スレッドから呼ばれる可能性:
- Dictionaryはlockで保護
- int型カウンタはInterlocked.Increment使用

#### 6.2.2 メモリリーク対策

処理時間リストは無限に増加しないよう制限:
```csharp
if (_processingTimes.Count > 1000)
    _processingTimes.RemoveAt(0);
```

#### 6.2.3 TCP環境でのテスト

UDP環境では既存動作を維持:
```csharp
if (_connectionConfig.Protocol == ProtocolType.Tcp)
{
    // TCP専用処理
}
else
{
    // UDP従来処理
}
```

---

## 7. Phase 3 完了基準

### 7.1 機能要件

- ✅ **Phase 2ビット展開機能の統合**（追加項目）
  - appsettings.jsonにBitExpansion設定追加
  - PlcCommunicationManagerへの統合完了
  - ConMoni互換性確認
- ✅ デバイス点数の多層検証
- ✅ 30種類以上のエラーコードマッピング
- ✅ TCP分割受信対応
- ✅ 統計記録機能（テキスト・CSV出力）

### 7.2 品質要件

- ✅ 全単体テストがパス
- ✅ **ビット展開統合テスト成功**（5パターン + ConMoni互換性）（追加項目）
- ✅ TCP分割受信の連続1000フレームテスト成功
- ✅ メモリリークなし
- ✅ スレッドセーフ性確認

### 7.3 ドキュメント要件

- ✅ コード内コメント
- ✅ **将来のExcel連携のためのコメント記載**（追加項目）
- ✅ テスト結果レポート
- ✅ 統計サンプルレポート
- ✅ 実装記録の作成

---

## 8. Phase 3 後の次ステップ

Phase 3完了後:

1. **全Phase統合テスト** → Phase 1-3の全機能連携確認
2. **実機長時間テスト** → 24時間連続動作確認
3. **パフォーマンスチューニング** → ボトルネック分析・最適化
4. **運用マニュアル作成** → 統計レポートの見方、エラー対応手順

---

**文書作成者**: Claude Code
**参照元**: 受信データ解析_実装方針決定.md, PySLMPClient/__init__.py
