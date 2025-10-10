# SlmpRawDataAnalyzer 主要機能メインシステム統合計画

## 計画概要

SlmpRawDataAnalyzer の「上四つの項目」（16進ダンプ可視化、データ型別詳細解析、SLMPフレーム解析、エラーコード辞書）をメインシステムに統合し、レガシーコードの安全な削除を可能にする。

**作成日**: 2025-01-01
**対象システム**: C:\Users\1010821\Desktop\python\andon\andon

## 現状分析

### メインシステム vs SlmpRawDataAnalyzer 機能比較

#### 1. 16進ダンプ可視化

**現在のメインシステム (DeviceScanner.cs:600+行目)**
```csharp
private string GenerateHexDump(byte[] data)
{
    const int bytesPerLine = 16;
    var sb = new System.Text.StringBuilder();
    // 基本的な実装のみ
}
```

**SlmpRawDataAnalyzer (154-193行目, 378-417行目)**
```csharp
private void OutputHexDump(byte[] data, string prefix = "")
{
    const int bytesPerLine = 16;
    // プレフィックス対応 ("REQ", "RES")
    // 8バイトごとの区切り表示
    // より詳細なフォーマット
    sb.AppendFormat("   {0}{1:X8}: ", prefix.PadRight(4), i);
    if (j == 7) sb.Append(" "); // 8バイトごとに区切り
}
```

#### 2. データ型別詳細解析

**現在のメインシステム**
- **該当機能なし**

**SlmpRawDataAnalyzer (285-340行目)**
```csharp
private void AnalyzeWordDeviceData(byte[] dataBytes)
{
    for (int i = 0; i < dataBytes.Length; i += 2)
    {
        var value = BitConverter.ToUInt16(dataBytes, i);
        _logger.LogInformation("Word[{Index}]: 0x{Value:X4} ({Value}) = {Binary}",
            i / 2, value, value, Convert.ToString(value, 2).PadLeft(16, '0'));
    }
}

private void AnalyzeBitDeviceData(byte[] dataBytes)
{
    for (int i = 0; i < dataBytes.Length; i++)
    {
        var bits = Convert.ToString(dataBytes[i], 2).PadLeft(8, '0');
        _logger.LogInformation("Byte[{0}]: 0x{1:X2} = {2} (bits: {3})",
            i, dataBytes[i], dataBytes[i], bits);
    }
}
```

#### 3. SLMPフレーム解析

**現在のメインシステム (DeviceScanner.cs)**
```csharp
// ハードコードされた簡単な解析
rawDataAnalysis.FrameAnalysis = new FrameAnalysis
{
    SubHeader = "0x5400",
    SubHeaderDescription = "4Eフレーム",
    EndCode = "0x0000",
    EndCodeDescription = "正常終了"
};
```

**SlmpRawDataAnalyzer (198-249行目)**
```csharp
private void AnalyzeSlmpFrame(byte[] data, string operation)
{
    // サブヘッダー解析
    var subHeader = BitConverter.ToUInt16(data, 0);
    _logger.LogInformation("サブヘッダー: 0x{0:X4} ({1})", subHeader,
        subHeader == 0x5000 ? "3Eフレーム" : subHeader == 0x5400 ? "4Eフレーム" : "不明");

    // ネットワーク番号、PC番号、要求先ユニットI/O番号等の詳細解析
    // 終了コードと動的エラーメッセージ取得
}
```

#### 4. エラーコード辞書

**現在のメインシステム (Constants/EndCode.cs)**
- **39個のエラーコード** を完全網羅
- `GetJapaneseMessage()` 拡張メソッド提供
- エラー重要度、再試行可否判定機能

**SlmpRawDataAnalyzer (345-364行目)**
- **12個のエラーコード** のみ（基本的なもの）

## 統合計画

### Phase 1: 16進ダンプ可視化の強化

**対象ファイル**: `C:\Users\1010821\Desktop\python\andon\andon\Core\DeviceScanner.cs`

**既存メソッド**: `GenerateHexDump`（約600行目）

**変更内容**:
```csharp
// 変更前
private string GenerateHexDump(byte[] data)

// 変更後
private string GenerateHexDump(byte[] data, string prefix = "")
{
    const int bytesPerLine = 16;
    var sb = new StringBuilder();

    for (int i = 0; i < data.Length; i += bytesPerLine)
    {
        // プレフィックス対応追加
        sb.AppendFormat("   {0}{1:X8}: ", prefix.PadRight(4), i);

        // 16進数部分
        for (int j = 0; j < bytesPerLine; j++)
        {
            if (i + j < data.Length)
            {
                sb.AppendFormat("{0:X2} ", data[i + j]);
            }
            else
            {
                sb.Append("   ");
            }

            // 8バイトごとに区切り追加
            if (j == 7) sb.Append(" ");
        }

        sb.Append(" |");

        // ASCII部分（既存と同様）
        for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
        {
            byte b = data[i + j];
            sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
        }

        sb.AppendLine("|");
    }

    return sb.ToString();
}
```

**呼び出し箇所の変更**:
```csharp
// 送信データ用
rawDataAnalysis.RequestHexDump = slmpClient.LastSentFrame != null ?
    GenerateHexDump(slmpClient.LastSentFrame, "REQ") : "";

// 受信データ用
rawDataAnalysis.HexDump = slmpClient.LastReceivedFrame != null ?
    GenerateHexDump(slmpClient.LastReceivedFrame, "RES") : "";
```

### Phase 2: データ型別詳細解析の追加

**対象ファイル**: `C:\Users\1010821\Desktop\python\andon\andon\Core\DeviceScanner.cs`

**新規メソッド追加**:
```csharp
/// <summary>
/// データ型別詳細解析を実行
/// </summary>
private void AnalyzeDataByType(byte[] dataBytes, string operationType, ILogger logger)
{
    if (!_enableDetailedDataAnalysis) return;

    switch (operationType.ToLowerInvariant())
    {
        case "worddeviceread":
            AnalyzeWordDeviceData(dataBytes, logger);
            break;
        case "bitdeviceread":
            AnalyzeBitDeviceData(dataBytes, logger);
            break;
        case "mixeddeviceread":
            AnalyzeMixedDeviceData(dataBytes, logger);
            break;
        default:
            AnalyzeGenericData(dataBytes, logger);
            break;
    }
}

/// <summary>
/// ワードデバイスデータ解析
/// </summary>
private void AnalyzeWordDeviceData(byte[] dataBytes, ILogger logger)
{
    logger.LogInformation("     📊 ワードデバイスデータ:");
    for (int i = 0; i < dataBytes.Length; i += 2)
    {
        if (i + 1 < dataBytes.Length)
        {
            var value = BitConverter.ToUInt16(dataBytes, i);
            logger.LogInformation("       Word[{Index}]: 0x{Value:X4} ({Value}) = {Binary}",
                i / 2, value, value, Convert.ToString(value, 2).PadLeft(16, '0'));
        }
    }
}

/// <summary>
/// ビットデバイスデータ解析
/// </summary>
private void AnalyzeBitDeviceData(byte[] dataBytes, ILogger logger)
{
    logger.LogInformation("     🔢 ビットデバイスデータ:");
    for (int i = 0; i < dataBytes.Length; i++)
    {
        var bits = Convert.ToString(dataBytes[i], 2).PadLeft(8, '0');
        logger.LogInformation("       Byte[{0}]: 0x{1:X2} = {2} (bits: {3})",
            i, dataBytes[i], dataBytes[i], bits);
    }
}

/// <summary>
/// 混合デバイスデータ解析
/// </summary>
private void AnalyzeMixedDeviceData(byte[] dataBytes, ILogger logger)
{
    logger.LogInformation("     🔀 混合デバイスデータ (詳細解析には追加情報が必要):");
    AnalyzeGenericData(dataBytes, logger);
}

/// <summary>
/// 汎用データ解析
/// </summary>
private void AnalyzeGenericData(byte[] dataBytes, ILogger logger)
{
    var maxDisplay = Math.Min(dataBytes.Length, 32); // 最初の32バイトまで表示
    for (int i = 0; i < maxDisplay; i += 4)
    {
        var segment = dataBytes.Skip(i).Take(4).ToArray();
        var hex = string.Join(" ", segment.Select(b => $"{b:X2}"));
        var ascii = string.Join("", segment.Select(b => b >= 32 && b <= 126 ? (char)b : '.'));
        logger.LogInformation("       [{0:X4}]: {1,-11} |{2}|", i, hex, ascii);
    }

    if (dataBytes.Length > maxDisplay)
    {
        logger.LogInformation("       ... (残り{0}バイト)", dataBytes.Length - maxDisplay);
    }
}
```

### Phase 3: SLMPフレーム解析の強化

**対象ファイル**: `C:\Users\1010821\Desktop\python\andon\andon\Core\DeviceScanner.cs`

**既存のハードコード部分を動的解析に変更**:
```csharp
// 変更前（約380行目）
rawDataAnalysis.FrameAnalysis = new FrameAnalysis
{
    SubHeader = "0x5400",
    SubHeaderDescription = "4Eフレーム",
    EndCode = "0x0000",
    EndCodeDescription = "正常終了"
};

// 変更後
rawDataAnalysis.FrameAnalysis = AnalyzeSlmpFrameStructure(slmpClient.LastReceivedFrame);
```

**新規メソッド追加**:
```csharp
/// <summary>
/// SLMPフレーム構造解析
/// </summary>
private FrameAnalysis AnalyzeSlmpFrameStructure(byte[]? frameData)
{
    if (frameData == null || frameData.Length < 11)
    {
        return new FrameAnalysis
        {
            SubHeader = "不明",
            SubHeaderDescription = "フレームデータ不足",
            EndCode = "不明",
            EndCodeDescription = "解析不可"
        };
    }

    try
    {
        // サブヘッダー解析
        var subHeader = BitConverter.ToUInt16(frameData, 0);
        var subHeaderDesc = subHeader switch
        {
            0x5000 => "3Eフレーム",
            0x5400 => "4Eフレーム",
            _ => "不明フレーム"
        };

        // 終了コード解析
        var endCode = BitConverter.ToUInt16(frameData, 9);
        var endCodeEnum = (EndCode)endCode;
        var endCodeDesc = endCodeEnum.GetJapaneseMessage();

        // 詳細ログ出力（設定により制御）
        if (_enableDetailedFrameAnalysis)
        {
            LogDetailedFrameAnalysis(frameData);
        }

        return new FrameAnalysis
        {
            SubHeader = $"0x{subHeader:X4}",
            SubHeaderDescription = subHeaderDesc,
            EndCode = $"0x{endCode:X4}",
            EndCodeDescription = endCodeDesc
        };
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "SLMPフレーム解析中にエラーが発生しました");
        return new FrameAnalysis
        {
            SubHeader = "エラー",
            SubHeaderDescription = "解析エラー",
            EndCode = "エラー",
            EndCodeDescription = ex.Message
        };
    }
}

/// <summary>
/// 詳細フレーム解析ログ出力
/// </summary>
private void LogDetailedFrameAnalysis(byte[] frameData)
{
    _logger.LogInformation("🔍 SLMPフレーム詳細解析:");

    // サブヘッダー
    var subHeader = BitConverter.ToUInt16(frameData, 0);
    _logger.LogInformation("   サブヘッダー: 0x{0:X4} ({1})", subHeader,
        subHeader == 0x5000 ? "3Eフレーム" : subHeader == 0x5400 ? "4Eフレーム" : "不明");

    // ネットワーク番号
    _logger.LogInformation("   ネットワーク番号: 0x{0:X2} ({0})", frameData[2]);

    // PC番号
    _logger.LogInformation("   PC番号: 0x{0:X2} ({0})", frameData[3]);

    // 要求先ユニットI/O番号
    var unitIO = BitConverter.ToUInt16(frameData, 4);
    _logger.LogInformation("   要求先ユニットI/O番号: 0x{0:X4} ({0})", unitIO);

    // 要求先ユニット局番号
    _logger.LogInformation("   要求先ユニット局番号: 0x{0:X2} ({0})", frameData[6]);

    // 応答データ長
    var dataLength = BitConverter.ToUInt16(frameData, 7);
    _logger.LogInformation("   応答データ長: 0x{0:X4} ({0} bytes)", dataLength);

    // 終了コード
    var endCode = BitConverter.ToUInt16(frameData, 9);
    var endCodeEnum = (EndCode)endCode;
    _logger.LogInformation("   終了コード: 0x{0:X4} ({1})", endCode, endCodeEnum.GetJapaneseMessage());

    // データ部の存在確認
    if (frameData.Length > 11)
    {
        var dataBytes = frameData.Skip(11).ToArray();
        _logger.LogInformation("   データ部: {0} bytes", dataBytes.Length);

        // データ型別解析の呼び出し
        if (_enableDetailedDataAnalysis)
        {
            AnalyzeDataByType(dataBytes, _currentOperationType ?? "unknown", _logger);
        }
    }
}
```

### Phase 4: 設定ファイル拡張

**対象ファイル**: `C:\Users\1010821\Desktop\python\andon\andon\appsettings.json`

**追加設定**:
```json
{
  "DiagnosticSettings": {
    "EnableDetailedDiagnostic": true,
    "DiagnosticLevel": "Verbose",
    "ShowNetworkStats": true,
    "ShowDeviceDetails": true,
    "StatisticsInterval": 10,
    "EnableErrorAnalysis": true,

    // 新規追加
    "EnableDetailedFrameAnalysis": true,
    "EnableDetailedDataAnalysis": true,
    "EnableEnhancedHexDump": true,
    "HexDumpShowPrefix": true
  }
}
```

**DeviceScanner クラスへの設定フィールド追加**:
```csharp
private readonly bool _enableDetailedFrameAnalysis;
private readonly bool _enableDetailedDataAnalysis;
private readonly bool _enableEnhancedHexDump;
private string? _currentOperationType; // データ型解析用

// コンストラクタで設定読み込み
public DeviceScanner(/* 既存パラメータ */, IConfiguration configuration)
{
    // 既存の初期化...

    _enableDetailedFrameAnalysis = configuration.GetSection("DiagnosticSettings")
        .GetValue<bool>("EnableDetailedFrameAnalysis", false);
    _enableDetailedDataAnalysis = configuration.GetSection("DiagnosticSettings")
        .GetValue<bool>("EnableDetailedDataAnalysis", false);
    _enableEnhancedHexDump = configuration.GetSection("DiagnosticSettings")
        .GetValue<bool>("EnableEnhancedHexDump", true);
}
```

### Phase 5: UnifiedLogWriter 拡張

**対象ファイル**: `C:\Users\1010821\Desktop\python\andon\andon\Core\UnifiedLogWriter.cs`

**RawDataAnalysis クラス拡張**:
```csharp
public class RawDataAnalysis
{
    public string RequestFrameHex { get; set; } = string.Empty;
    public string ResponseFrameHex { get; set; } = string.Empty;
    public string HexDump { get; set; } = string.Empty;

    // 新規追加
    public string RequestHexDump { get; set; } = string.Empty;  // 送信データのHexDump
    public string DetailedDataAnalysis { get; set; } = string.Empty;  // データ型別解析結果
    public string DetailedFrameAnalysis { get; set; } = string.Empty; // 詳細フレーム解析結果

    public FrameAnalysis FrameAnalysis { get; set; } = new();
}
```

## 削除対象ファイル一覧

統合完了後に安全に削除可能なファイル：

### Core ディレクトリ
```
C:\Users\1010821\Desktop\python\andon\andon\Core\SlmpRawDataAnalyzer.cs
C:\Users\1010821\Desktop\python\andon\andon\Core\SlmpClientWithTestLogging.cs
C:\Users\1010821\Desktop\python\andon\andon\Core\RealMachineTestLogger.cs
```

### Examples ディレクトリ（全体）
```
C:\Users\1010821\Desktop\python\andon\andon\Examples\
├── ContinuityExample.cs (削除済み)
├── EnhancedConnectionDemo.cs
├── IntelligentMonitoringExample.cs
├── RawDataOutputExample.cs
├── RealMachineTestExample.cs
├── TestRealMachineLogging.cs
├── TestTypeCodeMapping.cs
└── TypeCodeMappingTest.cs
```

### Tests ディレクトリ（Phase開発テスト）
```
C:\Users\1010821\Desktop\python\andon\andon.Tests\Phase4_MixedDeviceTests.cs (削除済み)
C:\Users\1010821\Desktop\python\andon\andon.Tests\Phase4_MixedDeviceTests.cs.bak
```

## 実装手順

### Step 1: 設定ファイル更新
1. `appsettings.json` に新しい設定項目を追加
2. `DeviceScanner` クラスに設定読み込み処理を追加

### Step 2: 16進ダンプ機能強化
1. `DeviceScanner.GenerateHexDump` メソッドを拡張
2. プレフィックス対応、8バイト区切り機能を追加
3. 呼び出し箇所を更新

### Step 3: データ型別解析機能追加
1. `AnalyzeDataByType` および関連メソッドを `DeviceScanner` に追加
2. `UnifiedLogWriter` の `RawDataAnalysis` クラスを拡張
3. 呼び出し処理を統合

### Step 4: SLMPフレーム解析強化
1. `AnalyzeSlmpFrameStructure` および `LogDetailedFrameAnalysis` メソッドを追加
2. 既存のハードコード部分を動的解析に変更
3. `EndCode.GetJapaneseMessage()` を活用

### Step 5: 統合テスト
1. 各機能が正常に動作することを確認
2. 既存機能に影響がないことを確認
3. ログ出力形式の確認

### Step 6: レガシーファイル削除
1. `SlmpRawDataAnalyzer.cs` を削除
2. `Examples` ディレクトリを削除
3. その他のレガシーファイルを削除
4. 使用していないusing文の削除

## テスト計画

### 機能テスト
- [ ] 16進ダンプのプレフィックス表示確認
- [ ] ワードデバイスデータの詳細解析確認
- [ ] ビットデバイスデータの詳細解析確認
- [ ] SLMPフレームの動的解析確認
- [ ] エラーコードの日本語メッセージ表示確認

### 設定テスト
- [ ] 詳細解析有効/無効の動作確認
- [ ] パフォーマンスへの影響確認
- [ ] ログファイルサイズの確認

### 回帰テスト
- [ ] 既存のStep4動作に影響がないことを確認
- [ ] 統合ログ出力形式に問題がないことを確認
- [ ] パフォーマンス劣化がないことを確認

## 期待効果

1. **機能統合**: SlmpRawDataAnalyzer の主要機能をメインシステムで利用可能
2. **コード整理**: 35+個のレガシーファイルを安全に削除
3. **保守性向上**: 統一されたアーキテクチャによるメンテナンス効率化
4. **分析精度向上**: より詳細な生データ解析とエラー診断
5. **設定の柔軟性**: 詳細解析機能の有効/無効制御

## 注意事項

- 既存の動作に影響を与えないよう、すべての新機能はオプション設定として実装
- パフォーマンスへの影響を最小限に抑えるため、詳細解析は必要時のみ実行
- 後方互換性を維持し、既存のログ形式を変更しない
- 段階的な実装により、各ステップで動作確認を実施

---
**作成者**: Claude Code
**レビュー**: 要レビュー
**承認**: 未承認
**実装予定**: TBD