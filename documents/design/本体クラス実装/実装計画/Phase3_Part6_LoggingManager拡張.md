# Phase 3 Part6: LoggingManager拡張実装計画

## 目標
LoggingManagerに以下の機能を追加する：
1. ファイル出力機能
2. ログレベル設定
3. ログファイルローテーション

---

## 現在の状況

### 既存実装
- **LoggingConfig.cs**: ログ設定モデル（Phase 3 Part4で実装済み）
  - LogLevel: string（デフォルト: "Information"）
  - EnableFileOutput: bool（デフォルト: true）
  - EnableConsoleOutput: bool（デフォルト: true）
  - LogFilePath: string（デフォルト: "logs/andon.log"）

- **LoggingManager.cs**: 基本実装済み
  - ILogger<LoggingManager>経由でコンソール出力のみ
  - LogInfo(), LogWarning(), LogError(), LogDebug()メソッド実装済み
  - LogDataAcquisition(), LogFrameSent(), LogResponseReceived()実装済み

### 未実装機能
- ファイル出力
- ログレベル設定によるフィルタリング
- ログファイルローテーション

---

## 実装方針

### A. ファイル出力機能

**使用技術**: `Microsoft.Extensions.Logging.File`パッケージを使用
- NuGetパッケージ: `Serilog.Extensions.Logging.File` または `NReco.Logging.File`
- StreamWriterを使った独自実装も検討

**実装内容**:
1. LoggingConfigからEnableFileOutput, LogFilePathを読み込み
2. ファイル出力が有効な場合、指定パスにログを書き込む
3. 非同期書き込みでパフォーマンス維持
4. 排他制御（複数スレッド対応）

### B. ログレベル設定

**実装内容**:
1. LoggingConfigのLogLevelプロパティを読み込み
2. 設定されたレベル以下のログのみ出力
3. レベル順序: Debug < Information < Warning < Error

**ログレベル定義**:
```csharp
public enum LogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3
}
```

### C. ログファイルローテーション

**実装内容**:
1. ファイルサイズベースのローテーション（デフォルト: 10MB）
2. 日付ベースのローテーション（オプション）
3. 古いログファイルの保持数制限（デフォルト: 7ファイル）

**ファイル命名規則**:
- andon.log（現在のログ）
- andon.log.1（1世代前）
- andon.log.2（2世代前）
- ...

---

## 実装手順（TDD準拠）

### Step 1: ファイル出力機能

#### 1-1. テスト作成（Red）
```csharp
[Fact]
public async Task LogInfo_EnableFileOutput_WritesToFile()
{
    // Arrange
    var tempFile = Path.GetTempFileName();
    var config = new LoggingConfig { EnableFileOutput = true, LogFilePath = tempFile };
    var manager = new LoggingManager(Mock.Of<ILogger<LoggingManager>>(), config);

    // Act
    await manager.LogInfo("Test message");
    await manager.CloseAndFlushAsync();

    // Assert
    var content = await File.ReadAllTextAsync(tempFile);
    Assert.Contains("Test message", content);
}
```

#### 1-2. 実装（Green）
- LoggingManagerコンストラクタでLoggingConfigを受け取る
- EnableFileOutputがtrueの場合、StreamWriterを初期化
- LogInfoなどのメソッドでファイルに書き込み

#### 1-3. リファクタリング（Refactor）
- 非同期書き込みの最適化
- 排他制御の追加

### Step 2: ログレベル設定

#### 2-1. テスト作成（Red）
```csharp
[Fact]
public async Task LogDebug_LogLevelInformation_NotWritten()
{
    // Arrange
    var config = new LoggingConfig { LogLevel = "Information" };
    var manager = new LoggingManager(Mock.Of<ILogger<LoggingManager>>(), config);

    // Act
    await manager.LogDebug("Debug message");

    // Assert
    // Debugレベルはフィルタされ、出力されない
}
```

#### 2-2. 実装（Green）
- LogLevelをenumに変換
- 各ログメソッドで現在のレベルと比較
- レベルが低い場合は出力をスキップ

#### 2-3. リファクタリング（Refactor）
- ログレベル判定ロジックの共通化

### Step 3: ログファイルローテーション

#### 3-1. テスト作成（Red）
```csharp
[Fact]
public async Task LogInfo_ExceedsMaxFileSize_RotatesFile()
{
    // Arrange
    var tempFile = Path.GetTempFileName();
    var config = new LoggingConfig
    {
        EnableFileOutput = true,
        LogFilePath = tempFile,
        MaxLogFileSizeMb = 1 // 1MB
    };
    var manager = new LoggingManager(Mock.Of<ILogger<LoggingManager>>(), config);

    // Act
    // 1MBを超えるログを書き込み
    for (int i = 0; i < 100000; i++)
    {
        await manager.LogInfo($"Large message {i}");
    }

    // Assert
    Assert.True(File.Exists($"{tempFile}.1")); // ローテーションされたファイルが存在
}
```

#### 3-2. 実装（Green）
- 各書き込み時にファイルサイズをチェック
- 制限を超えた場合、ローテーション処理を実行
- 古いファイルをリネーム

#### 3-3. リファクタリング（Refactor）
- ローテーション処理の最適化
- 保持ファイル数の制限

---

## LoggingConfig拡張

以下のプロパティを追加する必要があります：

```csharp
public class LoggingConfig
{
    // 既存プロパティ
    public string LogLevel { get; init; } = "Information";
    public bool EnableFileOutput { get; init; } = true;
    public bool EnableConsoleOutput { get; init; } = true;
    public string LogFilePath { get; init; } = "logs/andon.log";

    // 追加プロパティ
    /// <summary>
    /// ログファイルの最大サイズ（MB）
    /// </summary>
    public int MaxLogFileSizeMb { get; init; } = 10;

    /// <summary>
    /// 保持するログファイルの最大数
    /// </summary>
    public int MaxLogFileCount { get; init; } = 7;

    /// <summary>
    /// 日付ベースのローテーションを有効化
    /// </summary>
    public bool EnableDateBasedRotation { get; init; } = false;
}
```

---

## テスト項目

### ファイル出力機能（6テスト）
1. ✅ LogInfo_EnableFileOutput_WritesToFile
2. ✅ LogWarning_EnableFileOutput_WritesToFile
3. ✅ LogError_EnableFileOutput_WritesToFile
4. ✅ LogDebug_EnableFileOutput_WritesToFile
5. ✅ LogInfo_DisableFileOutput_NotWritesToFile
6. ✅ CloseAndFlushAsync_FlushesBufferedLogs

### ログレベル設定（8テスト）
7. ✅ LogDebug_LogLevelInformation_NotWritten
8. ✅ LogInfo_LogLevelInformation_Written
9. ✅ LogWarning_LogLevelInformation_Written
10. ✅ LogError_LogLevelInformation_Written
11. ✅ LogDebug_LogLevelDebug_Written
12. ✅ LogInfo_LogLevelWarning_NotWritten
13. ✅ LogInfo_LogLevelError_NotWritten
14. ✅ InvalidLogLevel_ThrowsArgumentException

### ログファイルローテーション（8テスト）
15. ✅ LogInfo_ExceedsMaxFileSize_RotatesFile
16. ✅ LogInfo_MultipleRotations_KeepsMaxFileCount
17. ✅ LogInfo_DateBasedRotation_CreatesNewFileDaily
18. ✅ RotateFile_OldFilesExist_RenamesCorrectly
19. ✅ RotateFile_ExceedsMaxCount_DeletesOldestFile
20. ✅ LogInfo_RotationInProgress_ThreadSafe
21. ✅ LogInfo_DirectoryNotExists_CreatesDirectory
22. ✅ LogInfo_FileAccessError_HandlesGracefully

**合計**: 22テスト

---

## 完了条件

1. ✅ 全22テストが合格
2. ✅ ファイル出力機能が動作する
3. ✅ ログレベル設定によるフィルタリングが動作する
4. ✅ ログファイルローテーションが動作する
5. ✅ マルチスレッド環境で安全に動作する
6. ✅ エラーハンドリングが適切に実装される

---

## 実装日時
- **作成日**: 2025年11月28日
- **Phase**: 3 Part6
- **実装方式**: TDD（Red-Green-Refactor）
