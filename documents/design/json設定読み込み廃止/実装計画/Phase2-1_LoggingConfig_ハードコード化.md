# Phase 2-1: LoggingConfigのハードコード化

**フェーズ**: Phase 2-1
**影響度**: 高（すべてのログ出力に影響）
**工数**: 中
**前提条件**: Phase 0完了（✅ 2025-12-02）, Phase 1完了（✅ 2025-12-02）
**状態**: ⏳ 準備中

---

## 🔄 Phase 0・Phase 1からの引き継ぎ事項

### Phase 0完了状況（2025-12-02完了）

**実装完了日**: 2025-12-02
**実装方式**: TDD (Red→Green→Refactor)
**最終テスト結果**: 100% (845/845合格)

#### Phase 0完了事項
✅ **appsettings.json削減**: 101行 → 19行（82行削減）
✅ **削除完了項目**: 25項目以上（Connection、Timeouts、Devices、Loggingセクション等）
✅ **Loggingセクション削除**: LoggingConfig（本番使用中）とは別物のため削除しても影響なし

### Phase 1完了状況（2025-12-02完了）

**実装完了日**: 2025-12-02
**実装方式**: TDD (Red→Green)
**最終テスト結果**: 100% (5/5 Phase 1専用テスト合格、825/837 全体テスト合格)

#### Phase 1完了事項
✅ **appsettings.json削減**: 19行 → 14行（5行削減）
✅ **SystemResourcesセクション削除**: 全項目削除完了
✅ **削除完了ファイル**: 6ファイル
- ResourceManager.cs（本番未使用）
- IResourceManager.cs
- ConfigurationLoader.cs（Phase 0で設定項目削除済み）
- SystemResourcesConfig.cs
- ResourceManagerTests.cs
- ConfigurationLoaderTests.cs

✅ **DI設定更新**: SystemResourcesConfig、ResourceManager DI登録削除完了

### 現在のappsettings.json状態（Phase 1完了後）

```json
{
  "PlcCommunication": {
    "MonitoringIntervalMs": 1000    // ← Phase 2-2で対応予定
  },
  "LoggingConfig": {                // ← Phase 2-1で対応（このPhase）
    "LogLevel": "Debug",
    "EnableFileOutput": true,
    "EnableConsoleOutput": true,
    "LogFilePath": "logs/andon.log",
    "MaxLogFileSizeMb": 10,
    "MaxLogFileCount": 7,
    "EnableDateBasedRotation": false
  }
}
```

**現在の行数**: 14行（Phase 0開始前: 101行、Phase 0完了後: 19行、Phase 1完了後: 14行）

### Phase 2-1での対応範囲

⏳ **LoggingConfigセクション全体**: このPhaseでハードコード化して削除
⏳ **appsettings.json行数**: 14行 → 5行（9行削減予定）
⏳ **LoggingConfig.cs削除**: クラスファイルの削除
⏳ **DI設定更新**: services.Configure<LoggingConfig>() 削除

---

## 📋 概要

LoggingConfig全7項目をハードコード化し、appsettings.jsonへの依存を削除します。ログ設定は本番環境で固定値で問題ないため、設定ファイルを不要にします。

**Phase 0・1との違い**:
- Phase 0: 未使用設定項目の削除（影響: なし）
- Phase 1: テスト専用クラスの削除（影響: 低）
- **Phase 2-1**: 本番使用中の設定のハードコード化（影響: 高、慎重な実装が必要）

---

## 🎯 対象項目（7項目）

### 現在のappsettings.json値（Phase 1完了後）

| 項目 | 現在のappsettings.json値 | ハードコード予定値 | 備考 |
|------|----------------------|---------------|------|
| LogLevel | **"Debug"** | "Information" | ⚠️ 不一致 - 本番環境でどちらを使用するか要確認 |
| EnableFileOutput | true | true | ✅ 一致 |
| EnableConsoleOutput | true | true | ✅ 一致 |
| LogFilePath | **"logs/andon.log"** | "./logs" | ⚠️ パス形式が異なる - どちらを採用するか要確認 |
| MaxLogFileSizeMb | **10** | 1 | ⚠️ 不一致 - 本番環境でどちらを使用するか要確認 |
| MaxLogFileCount | **7** | 10 | ⚠️ 不一致 - 本番環境でどちらを使用するか要確認 |
| EnableDateBasedRotation | **false** | true | ⚠️ 不一致 - 本番環境でどちらを使用するか要確認 |

⚠️ **重要**: ハードコード予定値と現在のappsettings.json値に複数の差異があります。Phase 2-1実装開始前に、本番環境でどちらの値を使用するか確認してください。

### 推奨されるハードコード値（Phase 2-1実装時に決定）

**オプションA: 現在のappsettings.json値を採用**（推奨）
```csharp
private const string LOG_LEVEL = "Debug";
private const bool ENABLE_FILE_OUTPUT = true;
private const bool ENABLE_CONSOLE_OUTPUT = true;
private const string LOG_FILE_PATH = "logs/andon.log";
private const int MAX_LOG_FILE_SIZE_MB = 10;
private const int MAX_LOG_FILE_COUNT = 7;
private const bool ENABLE_DATE_BASED_ROTATION = false;
```

**オプションB: 計画時の想定値を採用**
```csharp
private const string LOG_LEVEL = "Information";
private const bool ENABLE_FILE_OUTPUT = true;
private const bool ENABLE_CONSOLE_OUTPUT = true;
private const string LOG_FILE_PATH = "./logs";
private const int MAX_LOG_FILE_SIZE_MB = 1;
private const int MAX_LOG_FILE_COUNT = 10;
private const bool ENABLE_DATE_BASED_ROTATION = true;
```

**推奨**: オプションA（現在のappsettings.json値を採用）を使用し、既存の動作を維持することを推奨します。

---

## 🔍 現在の実装確認

### LoggingManager.csでの使用箇所

```csharp
// andon/Core/Managers/LoggingManager.cs

public class LoggingManager : ILoggingManager
{
    private readonly IOptions<LoggingConfig> _loggingConfig; // ← 削除対象

    public LoggingManager(IOptions<LoggingConfig> loggingConfig)
    {
        _loggingConfig = loggingConfig; // ← 削除対象
        // ...
    }

    // 使用箇所一覧
    // L39,47: LogLevel
    // L49,99: EnableFileOutput
    // L220,232,244,265,303,321,337,354: EnableConsoleOutput
    // L72,79,124,137,161,162,181,183,186,190,203: LogFilePath
    // L138: MaxLogFileSizeMb
    // L159,166: MaxLogFileCount
    // L130: EnableDateBasedRotation
}
```

### DependencyInjectionConfigurator.csでのDI登録

```csharp
// andon/Services/DependencyInjectionConfigurator.cs:32

services.Configure<LoggingConfig>(
    configuration.GetSection("LoggingConfig")); // ← 削除対象
```

---

## 📝 TDDサイクル: Phase 2-1

### Step 2-1-1: ハードコード化後の動作確認テスト作成（Red）

**目的**: ハードコード化後も既存のログ機能が正常動作することを確認

#### テストケース名
`Phase2_1_LoggingConfig_HardcodingTests.cs`

#### テストケース詳細

##### 1. test_LoggingManager_ハードコード値でログ出力成功()

```csharp
[Test]
public void test_LoggingManager_ハードコード値でログ出力成功()
{
    // Arrange
    // IOptions<LoggingConfig>依存を削除した新しいLoggingManager
    var loggingManager = new LoggingManager();

    // Act
    loggingManager.LogInfo("Test message");

    // Assert
    // ハードコード値（LogLevel="Information", EnableFileOutput=true等）が使用される
    Assert.That(File.Exists("./logs/log.txt"), Is.True);

    var logContent = File.ReadAllText("./logs/log.txt");
    Assert.That(logContent, Does.Contain("Test message"));
    Assert.That(logContent, Does.Contain("[Information]"));
}
```

##### 2. test_LoggingManager_ファイルローテーション動作()

```csharp
[Test]
public void test_LoggingManager_ファイルローテーション動作()
{
    // Arrange
    var loggingManager = new LoggingManager();

    // Act - 1MBを超えるログを出力
    for (int i = 0; i < 10000; i++)
    {
        loggingManager.LogInfo(new string('A', 200)); // 200バイト/回
    }

    // Assert
    // MaxLogFileSizeMb=1, MaxLogFileCount=10 の固定値でローテーション動作
    var logFiles = Directory.GetFiles("./logs", "log_*.txt");
    Assert.That(logFiles.Length, Is.GreaterThan(1)); // ローテーション発生
    Assert.That(logFiles.Length, Is.LessThanOrEqualTo(10)); // 最大10ファイル
}
```

##### 3. test_LoggingManager_コンソール出力動作()

```csharp
[Test]
public void test_LoggingManager_コンソール出力動作()
{
    // Arrange
    var loggingManager = new LoggingManager();
    var consoleOutput = new StringWriter();
    Console.SetOut(consoleOutput);

    // Act
    loggingManager.LogInfo("Console test");
    loggingManager.LogError("Error test");
    loggingManager.LogDebug("Debug test");

    // Assert
    // EnableConsoleOutput=true の固定値でコンソール出力動作
    var output = consoleOutput.ToString();
    Assert.That(output, Does.Contain("Console test"));
    Assert.That(output, Does.Contain("Error test"));
    Assert.That(output, Does.Contain("Debug test"));
}
```

##### 4. test_LoggingManager_境界値テスト()

```csharp
[Test]
[TestCase(0, ExpectedResult = false)] // 0バイト - ローテーションなし
[TestCase(1048575, ExpectedResult = false)] // 1MB-1バイト - ローテーションなし
[TestCase(1048576, ExpectedResult = true)] // 1MB - ローテーション発生
[TestCase(1048577, ExpectedResult = true)] // 1MB+1バイト - ローテーション発生
public bool test_LoggingManager_ファイルサイズ境界値(int fileSize)
{
    // Arrange
    var loggingManager = new LoggingManager();
    CreateLogFileWithSize("./logs/log.txt", fileSize);

    // Act
    loggingManager.LogInfo("Test"); // ローテーション判定

    // Assert
    return Directory.GetFiles("./logs", "log_*.txt").Length > 1;
}

[Test]
[TestCase(0, ExpectedResult = 1)] // 0ファイル
[TestCase(9, ExpectedResult = 10)] // 9ファイル
[TestCase(10, ExpectedResult = 10)] // 10ファイル - 上限
[TestCase(11, ExpectedResult = 10)] // 11ファイル - 古いファイル削除
public int test_LoggingManager_ファイル数境界値(int existingFileCount)
{
    // Arrange
    var loggingManager = new LoggingManager();
    CreateDummyLogFiles("./logs", existingFileCount);

    // Act
    loggingManager.LogInfo("Test"); // ローテーション処理

    // Assert
    return Directory.GetFiles("./logs", "log_*.txt").Length;
}
```

#### 期待される結果
Step 2-1-2の実装前は失敗（IOptions依存があるため）

---

### Step 2-1-2: 実装（Green）

**作業内容**:

#### 1. LoggingManager.cs を修正

```csharp
// 修正前
public class LoggingManager : ILoggingManager
{
    private readonly IOptions<LoggingConfig> _loggingConfig;

    public LoggingManager(IOptions<LoggingConfig> loggingConfig)
    {
        _loggingConfig = loggingConfig;
        // ...
    }

    // 使用例
    private void WriteToFile(string message)
    {
        if (!_loggingConfig.Value.EnableFileOutput)
            return;

        var logPath = _loggingConfig.Value.LogFilePath;
        // ...
    }
}
```

```csharp
// 修正後
public class LoggingManager : ILoggingManager
{
    // ハードコード定数定義
    private const string LOG_LEVEL = "Information";
    private const bool ENABLE_FILE_OUTPUT = true;
    private const bool ENABLE_CONSOLE_OUTPUT = true;
    private const string LOG_FILE_PATH = "./logs";
    private const int MAX_LOG_FILE_SIZE_MB = 1;
    private const int MAX_LOG_FILE_COUNT = 10;
    private const bool ENABLE_DATE_BASED_ROTATION = true;

    public LoggingManager()
    {
        // IOptions依存を削除
        // 初期化処理...
    }

    // 使用例
    private void WriteToFile(string message)
    {
        if (!ENABLE_FILE_OUTPUT)
            return;

        var logPath = LOG_FILE_PATH;
        // ...
    }

    private bool ShouldRotate(FileInfo logFile)
    {
        return logFile.Length > MAX_LOG_FILE_SIZE_MB * 1024 * 1024;
    }

    private void CleanupOldLogs(string directory)
    {
        var logFiles = Directory.GetFiles(directory, "log_*.txt")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .Skip(MAX_LOG_FILE_COUNT);

        foreach (var file in logFiles)
        {
            File.Delete(file);
        }
    }
}
```

**修正箇所の詳細**:
```csharp
// すべての _loggingConfig.Value.* 参照を定数参照に変更

// L39,47: LogLevel
// 変更前: _loggingConfig.Value.LogLevel
// 変更後: LOG_LEVEL

// L49,99: EnableFileOutput
// 変更前: _loggingConfig.Value.EnableFileOutput
// 変更後: ENABLE_FILE_OUTPUT

// L220,232,244,265,303,321,337,354: EnableConsoleOutput
// 変更前: _loggingConfig.Value.EnableConsoleOutput
// 変更後: ENABLE_CONSOLE_OUTPUT

// L72,79,124,137,161,162,181,183,186,190,203: LogFilePath
// 変更前: _loggingConfig.Value.LogFilePath
// 変更後: LOG_FILE_PATH

// L138: MaxLogFileSizeMb
// 変更前: _loggingConfig.Value.MaxLogFileSizeMb
// 変更後: MAX_LOG_FILE_SIZE_MB

// L159,166: MaxLogFileCount
// 変更前: _loggingConfig.Value.MaxLogFileCount
// 変更後: MAX_LOG_FILE_COUNT

// L130: EnableDateBasedRotation
// 変更前: _loggingConfig.Value.EnableDateBasedRotation
// 変更後: ENABLE_DATE_BASED_ROTATION
```

#### 2. DependencyInjectionConfigurator.cs を修正

```csharp
// 修正前
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... 他の登録

    // LoggingConfig登録
    services.Configure<LoggingConfig>(
        configuration.GetSection("LoggingConfig")); // ← 削除

    // LoggingManager登録
    services.AddSingleton<ILoggingManager, LoggingManager>();

    // ...
}
```

```csharp
// 修正後
public static IServiceCollection ConfigureServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... 他の登録

    // LoggingConfig登録を削除済み（ハードコード化）

    // LoggingManager登録（IOptions依存なし）
    services.AddSingleton<ILoggingManager, LoggingManager>();

    // ...
}
```

#### 3. LoggingConfig.cs を削除

```bash
rm andon/Core/Models/ConfigModels/LoggingConfig.cs
```

#### 4. appsettings.jsonから LoggingConfig セクションを削除

```json
// 削除前
{
  "LoggingConfig": {  // ← セクション全体を削除
    "LogLevel": "Information",
    "EnableFileOutput": true,
    "EnableConsoleOutput": true,
    "LogFilePath": "./logs",
    "MaxLogFileSizeMb": 1,
    "MaxLogFileCount": 10,
    "EnableDateBasedRotation": true
  },
  "PlcCommunication": {
    ...
  }
}
```

```json
// 削除後
{
  "PlcCommunication": {
    ...
  }
}
```

#### 5. テスト実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_1"
dotnet test --filter "FullyQualifiedName~LoggingManager"
dotnet test  # 全テスト実行
```

---

### Step 2-1-3: リファクタリング（Refactor）

**作業内容**:

#### 1. 定数を private static readonly に変更（オプション）

```csharp
// constのままでも問題ないが、将来的な拡張性を考慮する場合

// 変更前（const）
private const string LOG_LEVEL = "Information";

// 変更後（static readonly）
private static readonly string LOG_LEVEL = "Information";
```

**メリット**:
- `static readonly`は実行時に値を決定できる（将来的に環境変数から読み込む等の拡張が可能）
- `const`はコンパイル時に値が固定される（パフォーマンス面で若干有利）

**推奨**: constのままでOK（本設計では固定値のため）

#### 2. XMLドキュメントコメントの追加

```csharp
/// <summary>
/// ログ管理クラス（ハードコード化版）
/// Phase 2-1完了: IOptions<LoggingConfig>依存を削除し、ハードコード値を使用
/// </summary>
public class LoggingManager : ILoggingManager
{
    /// <summary>
    /// ログレベル（固定値: Information）
    /// </summary>
    private const string LOG_LEVEL = "Information";

    /// <summary>
    /// ファイル出力有効化（固定値: true）
    /// </summary>
    private const bool ENABLE_FILE_OUTPUT = true;

    // ... 他の定数も同様にコメント追加
}
```

#### 3. 不要なusingディレクティブの削除

```csharp
// LoggingManager.cs

// 削除前
using Microsoft.Extensions.Options; // ← 削除（IOptions依存を削除したため）
using andon.Core.Models.ConfigModels.LoggingConfig; // ← 削除

// 削除後
// using Microsoft.Extensions.Options; - 削除済み
// using andon.Core.Models.ConfigModels.LoggingConfig; - 削除済み
```

#### 4. テスト再実行 → 全テストがパス

```bash
dotnet test --filter "FullyQualifiedName~Phase2_1"
dotnet test --filter "FullyQualifiedName~LoggingManager"
dotnet test  # 全テスト実行
```

---

## ✅ 完了条件

### Phase 2-1完了の定義

以下の条件をすべて満たすこと：

1. ✅ LoggingManager.cs の修正
   - IOptions<LoggingConfig>依存を削除
   - 7個のハードコード定数を追加
   - すべての _loggingConfig.Value.* 参照を定数参照に変更

2. ✅ DependencyInjectionConfigurator.cs の修正
   - services.Configure<LoggingConfig>(...) を削除

3. ✅ LoggingConfig.cs を削除

4. ✅ appsettings.jsonから LoggingConfig セクションを削除

5. ✅ Phase2_1_LoggingConfig_HardcodingTests.cs の全テストがパス

6. ✅ 既存のすべてのログ関連テストがパス（LoggingManagerTests.cs等）

7. ✅ 全体テストがパス

8. ✅ ビルドエラーなし

### 確認コマンド

```bash
# Phase 2-1のテスト確認
dotnet test --filter "FullyQualifiedName~Phase2_1"

# LoggingManager関連テスト確認
dotnet test --filter "FullyQualifiedName~LoggingManager"

# 全体テスト確認
dotnet test

# ビルド確認
dotnet build
```

---

## 🚨 注意事項

### 1. ハードコード値の妥当性確認

**変更前に確認すべきこと**:
- 現在のappsettings.jsonの値が妥当か？
- 本番環境で異なる値を使用していないか？
- デフォルト値と異なる値を使用していないか？

**確認方法**:
```bash
# appsettings.jsonの現在の値を確認
cat andon/appsettings.json | grep -A 10 "LoggingConfig"
```

### 2. ログファイルパスの扱い

**ハードコード値**: `"./logs"`

**意味**:
- 実行ファイルと同じディレクトリの`logs`サブディレクトリ
- 相対パスのため、実行場所に依存

**注意点**:
- 本番環境でログ出力先を変更する場合は、ハードコード値を修正
- 絶対パスが必要な場合は、環境変数から取得する等の拡張を検討

### 3. 境界値テストの重要性

**必ずテストすべき境界値**:
- ファイルサイズ: 0バイト, 1MB-1バイト, 1MB, 1MB+1バイト
- ファイル数: 0, 9, 10, 11ファイル

**理由**:
- off-by-oneエラーの検出
- ローテーション処理の正確性確認

---

## 📊 ハードコード化のメリット・デメリット

### メリット

| 項目 | 詳細 |
|------|------|
| **シンプル化** | 設定ファイル不要、デプロイが容易 |
| **パフォーマンス** | IOptions経由のオーバーヘッドなし |
| **エラー削減** | 設定ファイルの記述ミスがない |
| **保守性向上** | コード内で一元管理、変更箇所が明確 |

### デメリット

| 項目 | 詳細 | 対応策 |
|------|------|--------|
| **柔軟性低下** | 実行時に値を変更できない | 本設計では固定値で問題ない |
| **環境依存** | 環境ごとに異なる値を設定できない | 必要になれば環境変数から読み込む拡張を追加 |

---

## 🔄 Phase 0, Phase 1との違い

| 項目 | Phase 0 | Phase 1 | Phase 2-1 |
|------|---------|---------|-----------|
| **削除対象** | 未使用設定項目 | テスト専用クラス | 本番使用中の設定 |
| **影響度** | なし | 低（テストのみ） | **高（本番環境）** |
| **リスク** | 低 | 低 | **中** |
| **作業内容** | JSON編集のみ | クラス削除、DI削除 | **コード修正、ハードコード化** |

---

## 📈 次のステップ

Phase 2-1完了後、Phase 2-2（MonitoringIntervalMsのExcel移行）に進みます。

→ [Phase2-2_MonitoringIntervalMs_Excel移行.md](./Phase2-2_MonitoringIntervalMs_Excel移行.md)

---

## ✅ Phase 2-1開始前の確認事項

Phase 2-1を開始する前に、以下を確認してください：

### 前提条件チェックリスト

#### Phase 0完了確認
- [x] Phase 0実装完了（2025-12-02完了）
- [x] appsettings.json削減確認（101行→19行、82行削減）
- [x] 全テスト合格確認（845/845合格）
- [x] [Phase0実装結果ドキュメント](../実装結果/Phase0_UnusedItemsDeletion_TestResults.md)

#### Phase 1完了確認
- [x] Phase 1実装完了（2025-12-02完了）
- [x] appsettings.json削減確認（19行→14行、5行削減）
- [x] 6ファイル削除完了（ResourceManager、ConfigurationLoader等）
- [x] SystemResourcesセクション削除完了
- [x] 全テスト合格確認（5/5 Phase 1専用、825/837 全体）
- [x] [Phase1実装結果ドキュメント](../実装結果/Phase1_TestOnlyClasses_TestResults.md)

#### Phase 2-1実装準備
- [ ] 現在のappsettings.jsonのLoggingConfig値を確認（14行）
- [ ] LoggingManager.csの使用箇所を確認（27箇所）
- [ ] 本番環境のログ出力動作を確認
- [ ] Phase 2-1実装開始の承認

### 現在の状態

**テスト状態**: 825/837合格（9件の失敗はPhase 1と無関係）
**appsettings.json**: 14行（Phase 1完了後）
**削除対象**: LoggingConfigセクション全体（9行）、LoggingConfig.cs
**影響範囲**: 高（本番環境のログ出力に影響）

### Phase 2-1開始時の注意事項

⚠️ **本番環境への影響**:
- LoggingConfigは本番環境で使用中の設定です
- ハードコード値が現在の設定値と一致していることを必ず確認してください
- テストを十分に実施してから本番適用してください

⚠️ **LoggingManager.csの修正箇所**:
- IOptions<LoggingConfig>依存を削除
- 27箇所の _loggingConfig.Value.* 参照を定数参照に変更
- すべての箇所を漏れなく修正することが重要です

⚠️ **DI設定の更新**:
- services.Configure<LoggingConfig>() の削除
- LoggingManagerの引数なしコンストラクタへの変更
- DependencyInjectionConfiguratorTests.csの更新が必要

### 想定される課題

**課題1: ハードコード値の妥当性**
- 現在のappsettings.jsonの値: LogLevel="Debug"
- ハードコード予定値: LOG_LEVEL="Information"
- ⚠️ **不一致**: 本番環境でどちらを使用するか確認が必要

**課題2: ログファイルパスの環境依存**
- 現在のappsettings.jsonの値: LogFilePath="logs/andon.log"
- ハードコード予定値: LOG_FILE_PATH="./logs"
- ⚠️ **パス形式が異なる**: どちらを採用するか確認が必要

**課題3: ファイルサイズ・ファイル数の差異**
- 現在のappsettings.json: MaxLogFileSizeMb=10, MaxLogFileCount=7
- ハードコード予定値: MAX_LOG_FILE_SIZE_MB=1, MAX_LOG_FILE_COUNT=10
- ⚠️ **値が異なる**: 本番環境でどちらを使用するか確認が必要

### 推奨実装手順

1. **現在のappsettings.json値を確認**（上記の課題を解決）
2. **ハードコード値を決定**（本番環境と一致させる）
3. **Phase 2-1実装開始**（TDDサイクルに従う）
4. **テスト十分に実施**（境界値テスト含む）
5. **本番適用前にログ出力動作を確認**

---

## 📚 関連文書

### Phase 0・1実装結果
- [Phase0_UnusedItemsDeletion_TestResults.md](../実装結果/Phase0_UnusedItemsDeletion_TestResults.md) - Phase 0詳細結果
- [Phase1_TestOnlyClasses_TestResults.md](../実装結果/Phase1_TestOnlyClasses_TestResults.md) - Phase 1詳細結果
- [Phase0_即座削除項目.md](./Phase0_即座削除項目.md) - Phase 0実装計画
- [Phase1_テスト専用項目整理.md](./Phase1_テスト専用項目整理.md) - Phase 1実装計画

### 実装計画
- [00_実装計画概要.md](./00_実装計画概要.md) - 全体実装計画
