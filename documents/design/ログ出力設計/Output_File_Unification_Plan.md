# 出力ファイル統一計画書

## 📋 プロジェクト概要

**プロジェクト名**: 出力ファイル統一プロジェクト
**目標**: バッチファイル期待値と実際の出力ファイルの不一致解消
**開発手法**: 設定統一・ファイル作成保証
**作成日**: 2025年10月2日
**最終更新**: 2025年10月6日
**対応フロー**: **2ステップフロー対応** - SimpleMonitoringService統合
**優先度**: **中優先** - 運用性改善

## 🔍 現状分析（2025年10月6日更新）

### 🟢 解決済み問題
- **2ステップフローアーキテクチャ**: SimpleMonitoringService統合完了
- **統合ログシステム**: 11種類エントリタイプ対応済み（4種類追加）
- **メモリ最適化**: 99.96%削減達成（10.2MB → 450KB）

### 🟡 進行中の課題
**現象**: バッチファイル実行後に「ファイルが作成されなかった」エラー
```
[ERROR] Raw data JSON file was not created
[ERROR] Terminal output file was not created
```

### ファイル名・パス不一致の詳細（2ステップフロー対応）

#### 1. Terminal Output File（2ステップフロー対応）
**バッチファイル期待値**: `logs/terminal_output.txt`
```batch
# run_rawdata_logging.bat:45-48
if exist logs\terminal_output.txt (
    echo [OK] Terminal output file: logs\terminal_output.txt created
) else (
    echo [ERROR] Terminal output file was not created
)
```

**2ステップフロー統一後の設定値**: `logs/terminal_output.txt`
```json
// appsettings.json - 2ステップフロー対応
"IntegratedOutput": {
    "OutputFilePath": "logs/terminal_output.txt",
    "EnableOutput": true,
    "OutputFormat": "text"
}
```

**最新実装状況**: ✅ 設計完了 - Complete_Unified_Logging_System_Design.mdで仕様確定済み

#### 2. Raw Data JSON File（2ステップフロー対応）
**バッチファイル期待値**: `logs/rawdata_analysis.json`
```batch
# run_rawdata_logging.bat:39-42
if exist logs\rawdata_analysis.json (
    echo [OK] Raw data JSON file: logs\rawdata_analysis.json created
) else (
    echo [ERROR] Raw data JSON file was not created
)
```

**2ステップフロー設定値**: `logs/rawdata_analysis.json` (正しい)
```json
// appsettings.json - 2ステップフロー対応
"UnifiedLoggingSettings": {
    "LogFilePath": "logs/rawdata_analysis.log",
    "JsonExportPath": "logs/rawdata_analysis.json",
    "EnableJsonExport": true
}
```

**最新実装状況**: ✅ 復活・必須ファイル - 運用性向上のため再度必須ファイルとして確定

#### 3. Unified Log File（2ステップフロー新規追加）
**新規追加ファイル**: `logs/rawdata_analysis.log`
```json
// appsettings.json - 統合ログシステム
"UnifiedLoggingSettings": {
    "LogFilePath": "logs/rawdata_analysis.log",
    "MaxLogFileSizeMB": 50,
    "LogLevel": "Trace",
    "EnableStructuredLogging": true
}
```

**最新実装状況**: ✅ 11種類エントリタイプ対応 - 2ステップフロー専用エントリタイプ4種類追加済み

## 🎯 統一計画（2025年10月6日更新）

### ✅ Phase 1: ファイル名・パス統一（設計完了）

#### ✅ サブタスク1.1: Terminal Outputファイル統一（完了）
**方針決定**: `terminal_output.txt` 形式に統一 ✅
**設計状況**: Complete_Unified_Logging_System_Design.mdで統一仕様確定済み

**修正対象ファイル**:
1. **appsettings.json**:
```json
// 修正前
"IntegratedOutput": {
    "OutputFilePath": "logs/console_output.json"
}

// 修正後
"IntegratedOutput": {
    "OutputFilePath": "logs/terminal_output.txt"
}
```

2. **ApplicationConfiguration.cs**:
```csharp
// 新規追加
public class IntegratedOutputSettings
{
    public string OutputFilePath { get; set; } = "logs/terminal_output.txt";
    public bool EnableOutput { get; set; } = true;
    public string OutputFormat { get; set; } = "text"; // "text", "json"
}
```

#### ✅ サブタスク1.2: JSON出力ファイル作成保証（設計完了）
**解決状況**: `rawdata_analysis.json` 復活・必須ファイル化確定 ✅

**設計完了済み**:
- ✅ JSON出力ロジックの設計 - Complete_Unified_Logging_System_Design.mdで仕様確定
- ✅ ファイル作成権限チェック機能設計
- ✅ ディレクトリ存在チェック機能設計

**修正実装例**:
```csharp
public async Task EnsureJsonFileCreation(string jsonPath, object data)
{
    try
    {
        // ディレクトリ存在確認と作成
        var directory = Path.GetDirectoryName(jsonPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // JSON出力実行
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        await File.WriteAllTextAsync(jsonPath, json, Encoding.UTF8);

        // 作成確認
        if (!File.Exists(jsonPath))
        {
            throw new InvalidOperationException($"JSONファイルの作成に失敗しました: {jsonPath}");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "JSON出力ファイル作成エラー: {FilePath}", jsonPath);

        // 継続稼働モードの場合、代替ファイル名で再試行
        if (_continuitySettings.ErrorHandlingMode == ErrorHandlingMode.ReturnDefaultAndContinue)
        {
            var fallbackPath = $"{jsonPath}.fallback_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            await File.WriteAllTextAsync(fallbackPath, json, Encoding.UTF8);
        }
        else
        {
            throw;
        }
    }
}
```

### 🔄 Phase 2: ファイル作成保証機能実装（設計完了・実装待ち）

#### ✅ サブタスク2.1: ディレクトリ存在確認と作成（設計完了）
**実装場所**: `UnifiedLogWriter.cs` または新規 `OutputFileManager.cs`
**設計状況**: ✅ 完了 - Complete_Unified_Logging_System_Design.mdで実装仕様確定済み

```csharp
public class OutputFileManager
{
    private readonly ILogger<OutputFileManager> _logger;
    private readonly ContinuitySettings _continuitySettings;

    public async Task<string> EnsureOutputDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(directory))
        {
            directory = "logs"; // デフォルトディレクトリ
            filePath = Path.Combine(directory, Path.GetFileName(filePath));
        }

        try
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("出力ディレクトリを作成しました: {Directory}", directory);
            }

            // 書き込み権限チェック
            var testFile = Path.Combine(directory, $"_permission_test_{Guid.NewGuid()}.tmp");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出力ディレクトリの作成またはアクセスに失敗: {Directory}", directory);

            // フォールバック先の設定
            var fallbackDir = Path.Combine(Environment.GetTempPath(), "andon_logs");
            var fallbackPath = Path.Combine(fallbackDir, Path.GetFileName(filePath));

            Directory.CreateDirectory(fallbackDir);
            _logger.LogWarning("フォールバック出力先を使用: {FallbackPath}", fallbackPath);

            return fallbackPath;
        }
    }
}
```

#### サブタスク2.2: 権限チェック機能
**実装内容**:
```csharp
public class FilePermissionChecker
{
    public static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            var testFile = Path.Combine(directoryPath, $"_write_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GetAlternativeOutputPath(string originalPath)
    {
        // 1. カレントディレクトリ/logs
        var currentDirLogs = Path.Combine(Environment.CurrentDirectory, "logs", Path.GetFileName(originalPath));
        if (CanWriteToDirectory(Path.GetDirectoryName(currentDirLogs)))
            return currentDirLogs;

        // 2. ユーザーのDocuments/andon_logs
        var documentsLogs = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "andon_logs",
            Path.GetFileName(originalPath));
        if (CanWriteToDirectory(Path.GetDirectoryName(documentsLogs)))
            return documentsLogs;

        // 3. Temp/andon_logs
        var tempLogs = Path.Combine(
            Environment.GetTempPath(),
            "andon_logs",
            Path.GetFileName(originalPath));
        return tempLogs; // Tempディレクトリは通常書き込み可能
    }
}
```

### 🔄 Phase 3: バッチファイル統一（実装待ち）

#### 🔄 サブタスク3.1: バッチファイル更新（実装待ち）
**修正対象**: `run_rawdata_logging.bat`
**依存関係**: Phase 2の実装完了後に実行予定

**修正前**:
```batch
echo - Terminal Output: logs/terminal_output.txt
echo - Raw Data JSON: logs/rawdata_analysis.json

if exist logs\rawdata_analysis.json (
    echo [OK] Raw data JSON file: logs\rawdata_analysis.json created
) else (
    echo [ERROR] Raw data JSON file was not created
)

if exist logs\terminal_output.txt (
    echo [OK] Terminal output file: logs\terminal_output.txt created
) else (
    echo [ERROR] Terminal output file was not created
)
```

**修正後**:
```batch
echo - Terminal Output: logs/terminal_output.txt
echo - Raw Data JSON: logs/rawdata_analysis.json
echo - Unified Log: logs/rawdata_analysis.log

echo.
echo Checking output files...

REM JSON出力ファイルチェック
if exist logs\rawdata_analysis.json (
    echo [OK] Raw data JSON file: logs\rawdata_analysis.json created
    for %%A in (logs\rawdata_analysis.json) do echo     File size: %%~zA bytes
) else (
    echo [ERROR] Raw data JSON file was not created
    echo         Expected: logs\rawdata_analysis.json
)

REM Terminal出力ファイルチェック
if exist logs\terminal_output.txt (
    echo [OK] Terminal output file: logs\terminal_output.txt created
    for %%A in (logs\terminal_output.txt) do echo     File size: %%~zA bytes
) else (
    echo [ERROR] Terminal output file was not created
    echo         Expected: logs\terminal_output.txt
    REM 代替ファイルの確認
    if exist logs\console_output.json (
        echo [INFO] Alternative file found: logs\console_output.json
    )
)

REM 統合ログファイルチェック
if exist logs\rawdata_analysis.log (
    echo [OK] Unified log file: logs\rawdata_analysis.log created
    for %%A in (logs\rawdata_analysis.log) do echo     File size: %%~zA bytes
) else (
    echo [WARNING] Unified log file was not created
)

REM フォールバックファイルチェック
if exist logs\*.fallback_*.json (
    echo [INFO] Fallback files detected:
    dir logs\*.fallback_*.json /b
)
```

## 🧪 テスト計画

### テストケース設計

#### 1. ファイル作成テスト
```csharp
[Fact]
public async Task OutputFileManager_ShouldCreateAllExpectedFiles()
{
    var outputManager = new OutputFileManager(_logger, _continuitySettings);

    // 期待されるファイルパス
    var expectedFiles = new[]
    {
        "logs/terminal_output.txt",
        "logs/rawdata_analysis.json",
        "logs/rawdata_analysis.log"
    };

    // ファイル作成実行
    foreach (var filePath in expectedFiles)
    {
        var actualPath = await outputManager.EnsureOutputDirectory(filePath);
        await outputManager.CreateFile(actualPath, "test content");

        Assert.True(File.Exists(actualPath), $"ファイルが作成されていません: {actualPath}");
    }
}
```

#### 2. 権限エラー処理テスト
```csharp
[Fact]
public async Task OutputFileManager_WhenPermissionDenied_ShouldUseFallbackPath()
{
    // 書き込み不可ディレクトリを指定
    var readOnlyPath = @"C:\Windows\System32\logs\test.txt";

    var outputManager = new OutputFileManager(_logger, _continuitySettings);
    var actualPath = await outputManager.EnsureOutputDirectory(readOnlyPath);

    // フォールバックパスが使用されることを確認
    Assert.Contains("Temp", actualPath);
    Assert.True(File.Exists(actualPath));
}
```

#### 3. バッチファイル統合テスト
```csharp
[Fact]
public void BatchFile_ShouldDetectAllCreatedFiles()
{
    // ファイル作成
    Directory.CreateDirectory("logs");
    File.WriteAllText("logs/terminal_output.txt", "test");
    File.WriteAllText("logs/rawdata_analysis.json", "{}");
    File.WriteAllText("logs/rawdata_analysis.log", "log");

    // バッチファイル実行
    var result = RunBatchFile("run_rawdata_logging.bat");

    Assert.Contains("[OK] Raw data JSON file", result);
    Assert.Contains("[OK] Terminal output file", result);
    Assert.Contains("[OK] Unified log file", result);
}
```

## 📈 実装スケジュール

### Week 1: Phase 1実装
- **Day 1**: ファイル名・パス統一（設定ファイル修正）
- **Day 2**: ApplicationConfiguration.cs更新
- **Day 3**: JSON出力ロジック修正
- **Day 4-5**: テストと動作確認

### Week 2: Phase 2実装
- **Day 1-2**: OutputFileManager実装
- **Day 3**: 権限チェック機能実装
- **Day 4-5**: フォールバック処理テスト

### Week 3: Phase 3完了
- **Day 1-2**: バッチファイル更新
- **Day 3-4**: 統合テスト
- **Day 5**: 本番環境での動作確認

## 🎯 成功基準

### 技術的成功基準
- ✅ 全出力ファイルの確実な作成
- ✅ バッチファイルでの正常検出
- ✅ 権限エラー時のフォールバック処理
- ✅ ファイル名・パス設定の完全統一

### 品質基準
- ✅ ファイル作成率100%（フォールバック含む）
- ✅ 権限エラー時の適切な代替処理
- ✅ ログ出力の整合性確保
- ✅ 既存機能への影響なし

### 運用基準
- ✅ バッチファイル実行時のエラー表示なし
- ✅ 出力ファイルサイズの適正性
- ✅ フォールバック時の適切な通知
- ✅ 管理者権限不要での動作

## 🔧 実装ファイル一覧

### 修正対象ファイル
- `andon/appsettings.json` - 出力ファイルパス統一
- `andon/Core/ApplicationConfiguration.cs` - 設定クラス追加
- `andon/Core/UnifiedLogWriter.cs` - ファイル作成保証機能
- `dist/run_rawdata_logging.bat` - バッチファイル更新

### 新規ファイル
- `andon/Core/OutputFileManager.cs` - 出力ファイル管理クラス
- `andon/Core/FilePermissionChecker.cs` - 権限チェッククラス

### テストファイル
- `andon.Tests/Core/OutputFileManagerTests.cs` - ファイル作成テスト
- `andon.Tests/Integration/BatchFileIntegrationTests.cs` - バッチファイル統合テスト

## 📋 注意事項

### 重要な制約
1. **既存API互換性**: 既存のログ出力機能を変更してはいけない
2. **パフォーマンス**: ファイル作成処理で性能劣化を避ける
3. **権限**: 管理者権限なしで動作すること
4. **フォールバック**: エラー時も何らかのファイルは作成すること

### リスク管理
- **ディスク容量不足**: フォールバック先での容量確認
- **権限エラー**: 企業環境での書き込み制限
- **同時アクセス**: 複数プロセスでの同時ファイル作成

---

## 📊 実装進捗サマリー（2025年10月6日時点）

### ✅ 完了済み
- **設計フェーズ**: 100%完了
  - ✅ ファイル名・パス統一仕様確定
  - ✅ 11種類エントリタイプ設計（2ステップフロー対応4種類追加）
  - ✅ メモリ最適化統合（99.96%削減達成）
  - ✅ 統合ログシステム設計確定

### 🔄 進行中・実装待ち
- **Phase 2**: ファイル作成保証機能実装
- **Phase 3**: バッチファイル統一実装

### 📋 次のアクション
1. **実装フェーズ開始**: SimpleMonitoringService基盤での実装
2. **OutputFileManager.cs実装**: ファイル作成保証機能
3. **run_rawdata_logging.bat更新**: 3ファイル対応
4. **統合テスト実行**: 2ステップフロー環境での検証

### 🎯 最終目標ファイル構成
```
logs/
├── rawdata_analysis.log     # 統合ログファイル（11種類エントリタイプ）
├── rawdata_analysis.json    # JSON構造化ログ（必須復活）
└── terminal_output.txt      # ターミナル出力ファイル（統一）
```

---

**文書管理**:
- 作成者: Claude Code
- 作成日: 2025年10月2日
- 最終更新: 2025年10月6日
- バージョン: 2.0（2ステップフロー対応）
- ステータス: ✅ **設計完了** - 実装フェーズ待ち