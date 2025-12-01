# Phase4: エラーハンドリング実装計画

## 概要
Phase4では、DataOutputManagerのエラーハンドリング機能を実装します。ファイル出力エラー、データ検証エラー、例外処理を網羅的に実装し、ログ出力を強化します。

---

## Phase3からの引き継ぎ事項（2025-11-27更新）

### Phase3完了事項
✅ **Phase3実装完了**: 2025-11-27
✅ **実装方式**: TDD (Red-Green-Refactor)
✅ **テスト結果**: 18/18テスト成功（Phase3: 7/7, 既存: 11/11）
✅ **実装時間**: 約3時間

### Phase3で実装された機能
Phase4実装時に利用可能な機能:

1. **ビットデバイス16ビット分割処理** (DataOutputManager.cs:53-80)
   - `IsBitDevice()`判定による分岐実装済み
   - MSB-first → LSB-first変換実装済み（`int bitIndex = 15 - i;`）
   - Phase4のエラーハンドリング時にもビット分割ロジックは正常動作

2. **ヘルパーメソッド群** (DataOutputManager.cs:96-180)
   - `AddBitDeviceItems()`: ビットデバイス16ビット分割処理
   - `AddWordDeviceItem()`: ワード/ダブルワードデバイス処理
   - `GetDeviceConfigInfo()`: デバイス設定情報取得（重複コード削減）
   - `ExtractBit()`: ビット抽出（単一責任原則）
   - Phase4でもこれらのメソッドをエラーハンドリング対象として利用

3. **定数化** (DataOutputManager.cs:17-18)
   - `BitsPerWord = 16`: 1ワード = 16ビット
   - `DefaultDigits = 1`: デフォルト桁数
   - Phase4のマジックナンバー排除パターンとして継続

4. **コメント充実化** (DataOutputManager.cs:53-80)
   - SLMP仕様（MSB-first）の明記
   - ビット順反転理由の説明
   - Phase4でもエラーハンドリング箇所に同様の詳細コメントを追加

### Phase4で解決すべき残課題

Phase3で未解決の最重要課題:

❌ **エラーハンドリング未実装**（Phase4実装予定）
- 現状: try-catchブロックなし
- Phase4実装: データ検証、ファイルI/Oエラーハンドリング、ログ強化
- 重要度: ★★★☆☆（重要）

❌ **ログ出力の不足**（Phase4実装予定）
- 現状: 出力開始・完了のINFOログのみ
- Phase4実装: エラーログ、デバッグログの追加
- 重要度: ★★★☆☆（重要）

❌ **plcModel値が固定値**（Phase5以降で検討）
- 現状: `plcModel = "Unknown"` 固定
- 将来対応: PlcConfigurationから動的取得

### Phase4実装の前提条件

Phase3完了により以下が利用可能:
- ✅ ビットデバイス16ビット分割処理が完全実装済み
- ✅ ヘルパーメソッド群が実装済み（エラーハンドリング対象として利用可能）
- ✅ 定数化によるマジックナンバー排除が完了
- ✅ コメント充実化パターンが確立
- ✅ TDD実装フローが確立（Red-Green-Refactorサイクル）
- ✅ 18/18テスト成功（Phase3: 7/7, 既存: 11/11）

### Phase4実装時の注意点

**Phase3で確立された実装パターンの継続**:
1. TDD手法を厳守（テスト先行実装）
2. Red-Green-Refactorサイクルを1つずつ完結
3. 実装完了後に実装記録を作成
4. 既存テストへの影響を確認

**Phase3実装記録の参照**:
- `documents/implementation_records/Phase3_DataOutputManager_BitDeviceSplitting_Implementation.txt`
- `documents/design/Step7_取得データ出力設計/実装結果/Phase3_BitDevice_16Bit_Split_Complete_Results.md`

**Phase3で解決した問題（Phase4で考慮すべき事項）**:
1. **テストデータ型ミスマッチ**: Phase3で明示的キャスト（`(ushort)0b...`）の重要性を確認
2. **ビット順反転処理**: Phase3でMSB-first → LSB-first変換の実装完了
3. **既存テスト期待値更新**: Phase3でビット分割によるエントリ数増加に対応

### Phase4実装で活用すべきPhase3の成果

**エラーハンドリング実装時の参照ポイント**:
1. **ビット分割処理のエラー対象**: `AddBitDeviceItems()`メソッドでのnullチェック・範囲チェック
2. **ヘルパーメソッドのエラー対象**: `GetDeviceConfigInfo()`でのdeviceConfig辞書nullチェック
3. **ExtractBit()のエラー対象**: ビットインデックス範囲チェック（0-15）

---

## 実装対象

### 1. try-catchブロックの追加

#### 現在の実装状況
エラーハンドリングが**未実装**（例外が発生するとそのまま上位に伝播）

#### 追加するtry-catchブロック

```csharp
public void OutputToJson(
    ProcessedResponseData data,
    string outputDirectory,
    string ipAddress,
    int port,
    Dictionary<string, DeviceEntryInfo> deviceConfig)
{
    try
    {
        // データ検証
        ValidateInputData(data, outputDirectory, ipAddress, port, deviceConfig);

        // 既存のJSON出力処理
        // ...

        // 正常系ログ出力
        Console.WriteLine($"[INFO] JSON出力完了: ファイル={fileName}, デバイス数={data.ProcessedData.Count}");
    }
    catch (ArgumentNullException ex)
    {
        Console.WriteLine($"[ERROR] データがnullです: {ex.Message}");
        throw;  // 再スロー
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"[ERROR] データ検証エラー: {ex.Message}");
        throw;
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.WriteLine($"[ERROR] 書き込み権限エラー: {ex.Message}");
        throw;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"[ERROR] ファイルI/Oエラー: {ex.Message}");
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] 予期しないエラー: {ex.GetType().Name} - {ex.Message}");
        throw;
    }
}
```

---

### 2. データ検証処理の実装

#### 追加するプライベートメソッド

```csharp
/// <summary>
/// 入力データを検証します
/// </summary>
private void ValidateInputData(
    ProcessedResponseData data,
    string outputDirectory,
    string ipAddress,
    int port,
    Dictionary<string, DeviceEntryInfo> deviceConfig)
{
    // nullチェック
    if (data == null)
    {
        throw new ArgumentNullException(nameof(data), "ProcessedResponseDataがnullです");
    }

    if (string.IsNullOrWhiteSpace(outputDirectory))
    {
        throw new ArgumentException("出力ディレクトリが指定されていません", nameof(outputDirectory));
    }

    if (string.IsNullOrWhiteSpace(ipAddress))
    {
        throw new ArgumentException("IPアドレスが指定されていません", nameof(ipAddress));
    }

    if (port <= 0 || port > 65535)
    {
        throw new ArgumentException($"ポート番号が不正です: {port}", nameof(port));
    }

    if (deviceConfig == null)
    {
        throw new ArgumentNullException(nameof(deviceConfig), "デバイス設定がnullです");
    }

    // ProcessedDataチェック
    if (data.ProcessedData == null)
    {
        throw new ArgumentException("ProcessedDataがnullです", nameof(data));
    }

    if (data.ProcessedData.Count == 0)
    {
        throw new ArgumentException("ProcessedDataが空です", nameof(data));
    }

    // 処理成功フラグチェック
    if (!data.IsSuccess)
    {
        throw new InvalidOperationException("データ処理が失敗しています");
    }
}
```

---

### 3. ファイル出力エラー処理の詳細化

#### ディスク容量不足検出

```csharp
/// <summary>
/// ディスク容量を確認します
/// </summary>
private void CheckDiskSpace(string directoryPath, long requiredBytes)
{
    try
    {
        var driveInfo = new DriveInfo(Path.GetPathRoot(directoryPath));
        if (driveInfo.AvailableFreeSpace < requiredBytes)
        {
            throw new IOException(
                $"ディスク容量不足: 必要={requiredBytes / 1024}KB, 利用可能={driveInfo.AvailableFreeSpace / 1024}KB");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[WARN] ディスク容量確認失敗: {ex.Message}");
        // 警告のみ、処理は継続
    }
}
```

#### ファイル書き込み処理の改善

```csharp
// ファイル書き込み処理（既存）
try
{
    // ディスク容量確認（オプション）
    CheckDiskSpace(outputDirectory, jsonString.Length);

    // ファイル書き込み
    File.WriteAllText(filePath, jsonString);
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"[ERROR] 書き込み権限エラー: {filePath}");
    throw new UnauthorizedAccessException($"ファイル書き込み権限がありません: {filePath}", ex);
}
catch (DirectoryNotFoundException ex)
{
    Console.WriteLine($"[ERROR] ディレクトリが見つかりません: {outputDirectory}");
    throw new DirectoryNotFoundException($"出力ディレクトリが存在しません: {outputDirectory}", ex);
}
catch (IOException ex)
{
    Console.WriteLine($"[ERROR] ファイルI/Oエラー: {ex.Message}");
    throw;
}
```

---

### 4. ログ出力の強化

#### 追加するログレベル
1. **INFO**: 正常系の情報ログ
2. **WARN**: 警告（処理は継続）
3. **ERROR**: エラー（処理は中断）

#### 実装内容

```csharp
// 出力開始ログ
Console.WriteLine($"[INFO] JSON出力開始: IP={ipAddress}, Port={port}, デバイス数={data.ProcessedData.Count}");

// ディレクトリ作成ログ
if (!Directory.Exists(outputDirectory))
{
    Console.WriteLine($"[INFO] 出力ディレクトリを作成: {outputDirectory}");
    Directory.CreateDirectory(outputDirectory);
}

// ビットデバイス分割ログ（オプション）
if (deviceData.Code.IsBitDevice())
{
    Console.WriteLine($"[DEBUG] ビットデバイス分割: {deviceData.DeviceName} → 16ビット展開");
}

// 出力完了ログ
Console.WriteLine($"[INFO] JSON出力完了: ファイル={fileName}, デバイス数={data.ProcessedData.Count}, ファイルサイズ={new FileInfo(filePath).Length}バイト");

// エラーログ（各catch節で出力）
Console.WriteLine($"[ERROR] データ検証エラー: {ex.Message}");
Console.WriteLine($"[ERROR] ファイルI/Oエラー: {filePath} - {ex.Message}");
```

---

### 5. 戻り値型の追加（オプション）

#### OutputResultクラスの実装

**ファイル**: `andon/Core/Models/OutputResult.cs`（新規作成）

```csharp
namespace Andon.Core.Models
{
    /// <summary>
    /// データ出力結果
    /// </summary>
    public class OutputResult
    {
        /// <summary>
        /// 出力成功フラグ
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 出力ファイルパス
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 書き込んだバイト数
        /// </summary>
        public long BytesWritten { get; set; }

        /// <summary>
        /// 書き込んだレコード数（items配列の要素数）
        /// </summary>
        public int RecordsWritten { get; set; }

        /// <summary>
        /// 書き込み時間
        /// </summary>
        public TimeSpan WriteTime { get; set; }

        /// <summary>
        /// エラーメッセージ（失敗時）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 例外情報（失敗時）
        /// </summary>
        public Exception? Exception { get; set; }
    }
}
```

#### OutputToJsonの戻り値型変更

```csharp
// 現在のシグネチャ（void）
public void OutputToJson(...)

// Phase4での変更後シグネチャ（OutputResult）
public OutputResult OutputToJson(...)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        // 既存の処理
        // ...

        stopwatch.Stop();

        return new OutputResult
        {
            Success = true,
            OutputPath = filePath,
            BytesWritten = new FileInfo(filePath).Length,
            RecordsWritten = itemsList.Count,
            WriteTime = stopwatch.Elapsed
        };
    }
    catch (Exception ex)
    {
        stopwatch.Stop();

        return new OutputResult
        {
            Success = false,
            ErrorMessage = ex.Message,
            Exception = ex,
            WriteTime = stopwatch.Elapsed
        };
    }
}
```

**注意**: 戻り値型変更は**オプション**（Phase4では実装しなくてもOK）

---

## 実装手順

### Step 1: データ検証メソッドの実装
1. `ValidateInputData()`プライベートメソッドを追加
2. 各種nullチェック・範囲チェックを実装

### Step 2: try-catchブロックの追加
1. `OutputToJson()`メソッド全体をtryブロックで囲む
2. 各種例外に対応するcatchブロックを追加
3. エラーログ出力を追加

### Step 3: ファイル出力エラー処理の詳細化
1. `CheckDiskSpace()`メソッドを追加（オプション）
2. ファイル書き込み処理にtry-catchを追加
3. 各種IOExceptionに対応

### Step 4: ログ出力の強化
1. 出力開始ログを追加
2. 出力完了ログを追加（ファイルサイズ情報を含む）
3. エラーログを各catch節に追加

### Step 5: OutputResultクラスの実装（オプション）
1. `andon/Core/Models/OutputResult.cs`を作成
2. `OutputToJson()`の戻り値型を変更
3. インターフェース`IDataOutputManager`も更新

### Step 6: ビルド・動作確認
1. `dotnet build`を実行
2. コンパイルエラーがないことを確認

### Step 7: エラーケーステスト
1. 各種エラーケースをテスト
2. ログ出力が正常に行われることを確認

---

## テスト要件（TDD必須）

### Phase4の単体テスト（必須実装）

**重要**: Phase4はエラーハンドリングの網羅的実装です。TDD手法に従い、**エラーケースのテストを先に実装**してから機能実装を行います。

#### TDD実装サイクル（具体的手順）

Phase4ではエラーケースごとに以下のサイクルを実施します:

1. **ステップ1: データ検証エラーテスト（TC_P4_001, TC_P4_002）**
   - Red: null/空データのテスト実装 → 失敗確認
   - Green: ValidateInputData()メソッド実装 → パス確認
   - Refactor: 検証ロジック整理 → パス確認

2. **ステップ2: パラメータ検証エラーテスト（TC_P4_003）**
   - Red: ポート番号不正テスト実装 → 失敗確認
   - Green: ポート番号検証ロジック追加 → パス確認

3. **ステップ3: ファイルI/Oエラーテスト（TC_P4_004, TC_P4_005）**
   - Red: 権限エラー、パスエラーのテスト実装 → 失敗確認
   - Green: try-catchブロック実装 → パス確認

4. **ステップ4: 正常系ログ出力テスト（TC_P4_006）**
   - Red: ログ出力確認テスト実装 → 失敗確認
   - Green: Console.WriteLineによるログ出力実装 → パス確認

5. **ステップ5（オプション）: OutputResultテスト（TC_P4_007, TC_P4_008）**
   - Red: 戻り値検証テスト実装 → 失敗確認
   - Green: OutputResultクラス実装と戻り値型変更 → パス確認

**注意**: Phase4では**エラーケースのテストが失敗する = 正しくエラー処理されていない**ことを意味します。

#### テスト用モックデータとヘルパー

Phase4で使用するテストヘルパーの作成例:

```csharp
// nullデータのモック
private ProcessedResponseData CreateNullData()
{
    return null;
}

// 空データのモック
private ProcessedResponseData CreateEmptyData()
{
    return new ProcessedResponseData
    {
        ProcessedData = new Dictionary<string, DeviceData>(),
        ProcessedAt = DateTime.Now,
        IsSuccess = true
    };
}

// 読み取り専用ディレクトリの作成
private string CreateReadOnlyDirectory()
{
    var path = Path.Combine(Path.GetTempPath(), $"andon_readonly_{Guid.NewGuid()}");
    var dir = Directory.CreateDirectory(path);

    // Windowsでは読み取り専用属性を設定
    File.SetAttributes(path, FileAttributes.ReadOnly);

    return path;
}

// ログ出力のキャプチャ（Console.WriteLineのリダイレクト）
private StringWriter CaptureConsoleOutput()
{
    var stringWriter = new StringWriter();
    Console.SetOut(stringWriter);
    return stringWriter;
}
```

#### テスト失敗時の対応指針

エラーハンドリングのテストでは、**期待する例外がスローされること**を検証します:

```csharp
[Fact]
public void TC_P4_001_DataValidationError_Null()
{
    // Arrange
    var data = CreateNullData();

    // Act & Assert
    var exception = Assert.Throws<ArgumentNullException>(() =>
    {
        _manager.OutputToJson(data, "./output", "192.168.1.10", 5007, new Dictionary<string, DeviceEntryInfo>());
    });

    // エラーメッセージの検証
    Assert.Contains("ProcessedResponseData", exception.Message);
}
```

**テスト失敗の原因**:
- 例外がスローされない → 検証ロジックが未実装
- 異なる例外がスローされる → 検証ロジックの実装ミス
- エラーメッセージが不適切 → メッセージの改善が必要

#### テストケース（必須）

#### TC_P4_001: データ検証エラー（null）
- **入力**: `data = null`
- **期待値**: `ArgumentNullException`がスローされる
- **ログ**: "[ERROR] データがnullです" が出力される

#### TC_P4_002: データ検証エラー（空）
- **入力**: `data.ProcessedData = new Dictionary()`
- **期待値**: `ArgumentException`がスローされる
- **ログ**: "[ERROR] データ検証エラー: ProcessedDataが空です" が出力される

#### TC_P4_003: ポート番号不正
- **入力**: `port = 0`
- **期待値**: `ArgumentException`がスローされる

#### TC_P4_004: ファイル書き込み権限エラー（モック）
- **入力**: 読み取り専用ディレクトリを指定
- **期待値**: `UnauthorizedAccessException`がスローされる
- **ログ**: "[ERROR] 書き込み権限エラー" が出力される

#### TC_P4_005: 存在しないディレクトリへの出力
- **入力**: `outputDirectory = "C:/invalid/path"`
- **期待値**: ディレクトリが自動作成されるか、`DirectoryNotFoundException`がスローされる

#### TC_P4_006: 正常系ログ出力
- **入力**: 正常なデータ
- **期待値**: "[INFO] JSON出力開始" と "[INFO] JSON出力完了" が出力される

#### TC_P4_007: OutputResult戻り値（成功）
- **入力**: 正常なデータ
- **期待値**: `Success = true`, `BytesWritten > 0`, `RecordsWritten > 0`

#### TC_P4_008: OutputResult戻り値（失敗）
- **入力**: 不正なデータ
- **期待値**: `Success = false`, `ErrorMessage != null`, `Exception != null`

---

## 完了条件

### 必須項目
- [ ] データ検証処理（ValidateInputData）が実装されている
- [ ] try-catchブロックが実装されている
- [ ] 各種例外に対応するcatch節が実装されている
- [ ] エラーログが各catch節で出力される
- [ ] 正常系ログ（出力開始・完了）が出力される
- [ ] `dotnet build`が成功する
- [ ] エラーケーステスト（TC_P4_001～TC_P4_006）がすべてパスする

### オプション項目
- [ ] `OutputResult`クラスが実装されている
- [ ] `OutputToJson()`の戻り値型が`OutputResult`に変更されている
- [ ] `CheckDiskSpace()`メソッドが実装されている
- [ ] TC_P4_007～TC_P4_008のテストが実装されている

---

## 実装後の確認事項

### 1. エラーログ出力確認
```
[ERROR] データ検証エラー: ProcessedDataが空です
[ERROR] 書き込み権限エラー: ./output/test.json
[ERROR] ファイルI/Oエラー: ディスク容量不足
```

### 2. 正常系ログ出力確認
```
[INFO] JSON出力開始: IP=192.168.1.10, Port=5007, デバイス数=17
[INFO] 出力ディレクトリを作成: ./output
[INFO] JSON出力完了: ファイル=20251127_123456789_192-168-1-10_5007.json, デバイス数=17, ファイルサイズ=1234バイト
```

### 3. 例外処理確認
- nullデータを渡した場合、ArgumentNullExceptionがスローされる
- 空のProcessedDataを渡した場合、ArgumentExceptionがスローされる
- 書き込み権限のないディレクトリを指定した場合、UnauthorizedAccessExceptionがスローされる

---

## エラーハンドリングのベストプラクティス

### 1. 例外の再スロー
キャッチした例外は基本的に**再スロー**する（`throw;`）
- 理由: 上位層でエラーハンドリングを行えるようにするため
- ログ出力後に再スロー

### 2. エラーメッセージの詳細化
例外メッセージには**具体的な情報**を含める
- 良い例: "ポート番号が不正です: 0"
- 悪い例: "エラーが発生しました"

### 3. ログレベルの適切な使用
- **ERROR**: 処理が中断される重大なエラー
- **WARN**: 処理は継続するが注意が必要
- **INFO**: 正常な動作の情報
- **DEBUG**: デバッグ用の詳細情報（本番では出力しない）

---

## 既存実装との差異

### Phase4で解決される問題
- ✅ エラーハンドリングの不足
- ✅ ログ出力の不足
- ✅ データ検証の欠如

### Phase4で未解決の問題（Phase5以降で対応）
- ❌ 包括的なテストの不足
- ❌ パフォーマンステストの未実施

---

## 次のPhaseへの準備

Phase5では、**包括的なテスト**を実装します。
単体テスト、統合テスト、パフォーマンステストを網羅的に実装します。

Phase4完了後、`Phase5_テスト実装.md`を参照してください。

---

## 参照文書
- `実装ガイド.md`: エラーハンドリング設計（セクション5, 6）
- `実装時対応関係.md`: エラーハンドリングの実装方針（セクション9）

## 作成日時
- **作成日**: 2025年11月27日
- **最終更新**: 2025年11月27日（Phase3引き継ぎ事項追記）
- **対象Phase**: Phase4（エラーハンドリング実装）

---

## Phase4実装開始前のチェックリスト

Phase4実装を開始する前に、以下を確認してください:

### 前提条件確認
- [ ] Phase3実装完了を確認（Phase3完了日: 2025-11-27）
- [ ] Phase3テスト結果を確認（18/18テスト成功）
- [ ] Phase3実装記録を読了
- [ ] ビットデバイス16ビット分割処理が正常動作することを確認
- [ ] ヘルパーメソッド群（AddBitDeviceItems, AddWordDeviceItem, GetDeviceConfigInfo, ExtractBit）の理解

### 実装準備
- [ ] TDD実装フローを理解（Red-Green-Refactor）
- [ ] テストケース（TC_P4_001～008）を理解
- [ ] エラーハンドリングのベストプラクティスを理解
- [ ] ValidateInputData()メソッドの実装方針を理解

### 開発環境確認
- [ ] `dotnet build`が成功することを確認
- [ ] 既存テスト18/18が成功することを確認
- [ ] 開発環境がクリーンな状態であることを確認

**Phase4実装準備完了**: すべてのチェックボックスが✅になったら実装開始可能

---

## Phase3実装で得た知見の活用

### 1. SLMPプロトコルのビット順（MSB-first）
Phase3でConMoni分析により発見した重要な仕様:
- SLMPプロトコルはMSB-first（ビット15が最初）
- C#実装ではMSB-first → LSB-first変換が必要（`int bitIndex = 15 - i;`）
- Phase4のエラーハンドリングでもこの仕様を考慮

### 2. テストデータ型の明示的キャスト
Phase3で発見した問題:
- `int`リテラルが暗黙的に`ushort`変換されオーバーフロー
- Phase4のテストデータ作成時も明示的キャスト（`(ushort)0b...`）を使用

### 3. 既存テスト期待値の更新
Phase3で学んだ教訓:
- ビットデバイス分割によりエントリ数が増加（1 → 16）
- Phase4のエラーハンドリング実装後も既存テストへの影響を確認

### 4. ヘルパーメソッドによるコード分離
Phase3で確立したパターン:
- 単一責任原則に基づくヘルパーメソッド作成
- Phase4でもValidateInputData()などのヘルパーメソッドを積極的に活用

---

## Phase3完了時の実装状況（参考情報）

### DataOutputManagerの現在の行数
- 全体: 約180行（Phase3完了時点）
- OutputToJson(): 約50行
- ヘルパーメソッド群: 約80行
- Phase4実装後: 約250行見込み（+70行）

### Phase3で追加されたコード量
- 実装コード: 約80行（ヘルパーメソッド含む）
- テストコード: 約200行（TC_P3_001～007）
- コメント・ドキュメント: 約30行

### Phase3で確立されたコーディングパターン
1. **定数化**: マジックナンバーを定数に置き換え
2. **ヘルパーメソッド**: 単一責任原則に基づく分離
3. **コメント**: SLMP仕様など重要な情報を明記
4. **テストデータ**: 明示的キャストによる型安全性確保
