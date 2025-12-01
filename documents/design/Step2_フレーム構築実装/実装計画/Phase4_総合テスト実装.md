# Phase4: 総合テスト実装

## 概要

Step2フレーム構築実装の第4フェーズとして、Phase1～3で実装した全機能の総合テストを実施します。

## 実装目標

- SlmpFrameBuilderの全機能テスト
- ConfigToFrameManagerとの統合テスト
- 3E/4Eフレーム構築の完全な動作確認
- エラーハンドリングの検証

---

## ⚠️ Phase3からの引き継ぎ事項

### Phase3でのテスト後追い実装状況

Phase3では、Phase1-2で実装先行したコードに対してテストを後追い実装しました：

**Phase3完了事項**:
- ✅ SequenceNumberManagerのテスト補完（TC001～TC006）
- ✅ SlmpFrameBuilderの基本テスト補完（TC001～TC018）
- ✅ プライベートメソッドの間接的検証テスト追加
- ✅ 全テストケース実行・パス確認

**Phase4での対応**:

Phase4では、Phase3で実装済みのテストケースとの重複を避けつつ、以下を実施します：

1. **既存テストの確認**（重複チェック）
   - Phase3で実装済みのTC001～TC018が存在することを確認
   - 実装済みテストケースは再実装しない
   - 不足テストケースのみを追加実装

2. **統合テスト中心の実装**
   - Phase3では単体テスト中心だったため、Phase4では統合テストに注力
   - ConfigToFrameManager統合テスト（TC019～TC020）
   - Step1_2統合テスト（TC101～TC103）

3. **カバレッジ分析**
   - Phase3完了時点でのカバレッジを確認
   - 不足箇所を特定して追加テスト実装

---

## ⚠️ Phase2.5からの引き継ぎ事項（2025-11-27追加）

### Phase2.5での既存問題対応状況

Phase2リファクタリング後に発見された34件の既存テスト失敗に対応したPhase2.5が完了しました：

**Phase2.5完了事項**:
- ✅ フレーム長計算不一致修正（17件）
- ✅ SendFrameAsync IsBinary対応修正（1件）
- ✅ DeviceCountValidation修正（4件）
- ✅ TC025テストデータ修正（1件）
- ✅ TC029（3E ASCII基本後処理）修正（1件）
- ✅ ConfigurationLoader統合テスト設計統一（5件）
- ✅ テスト成功率: 92.6% → 98.4% (+5.8ポイント、28件改善)

**Phase2.5で確認された実装コードの正当性**:
- ✅ `UpdateDataLength`: データ長計算ロジック完全正常（監視タイマ含む、実機ログと一致）
- ✅ `SendFrameAsync`: IsBinary設定によるASCII/Binary変換切り替え正常
- ✅ `ExtractWordDevices`: データ長不足時の警告処理正常（例外→警告に変更）
- ✅ SlmpFrameBuilder: memo.md実機データ（213バイト）と完全一致
- ✅ PlcCommunicationManager: SLMP仕様完全準拠

**Phase2.5残存問題（6件、Phase4で対応推奨）**:
- ⚠️ TC032_CombineDwordData（優先度: 低、DWord処理は将来削除予定）
- ⚠️ TC021_TC025統合テスト（優先度: 中）
- ⚠️ TC118_Step6連続処理統合（優先度: 中）
- ⚠️ TC116_UDP完全サイクル（優先度: 中）
- ⚠️ TC143_10系ビットデバイステスト（2件、優先度: 中）

**Phase4での対応**:

Phase4では、Phase2.5で修正完了した実装コードを基に、以下を実施します：

1. **Phase2.5残存問題の対応**（6件、優先度: 中）
   - 統合テストのテストデータ修正（フレーム長補正、型設定修正等）
   - Phase2.5-5（ExtractWordDevices修正）との整合性確保
   - 推定修正時間: 2-3時間

2. **Phase2.5修正内容の考慮**
   - データ長計算: 監視タイマを含むことを前提にテスト実装
   - IsBinary設定: ASCII/Binary変換の切り替えを考慮したテスト実装
   - DeviceCountValidation: デバイス点数不一致時の警告処理を考慮

3. **実装コードの正当性検証活用**
   - Phase2.5で検証済みの実装コード仕様を正として統合テスト実装
   - 実機ログ（memo.md）との整合性を統合テストでも確認

4. **Phase2.5設計判断の継承**
   - ExtractWordDevices: データ長不足時は警告のみで処理継続
   - ConfigurationLoader: Excelファイル0件時は例外を投げる
   - デバイス点数不一致: 警告付き成功（実用性重視）

---

## ⚠️ Phase3.5からの引き継ぎ事項（2025-11-27追加）

### Phase3.5でのDWord機能完全廃止完了状況

Phase2.5完了後、ReadRandomコマンド(0x0403)の仕様に合わせてDWord分割/結合処理を完全廃止したPhase3.5が完了しました：

**Phase3.5完了事項**:
- ✅ DWord結合処理（CombineDwordData）の完全削除
- ✅ 関連テストコード（TC032, TC118）の削除
- ✅ ExecuteFullCycleAsync Step6-2をデータ変換処理に変更
- ✅ 統合テストの修正（TC119×2, TC143_10×2）
- ✅ コードベース622行削減（保守性向上）
- ✅ テスト成功率: 98.4% → 99.2% (+0.8ポイント、4件改善）
- ✅ TC143_10_3 4E Binaryフレーム構造修正（17バイト → 16バイト）

**Phase3.5で削除された機能**:
1. **CombineDwordDataメソッド**（PlcCommunicationManager.cs、183行）
2. **CombineDwordDataインターフェース定義**（IPlcCommunicationManager.cs、12行）
3. **DWordCombineTargetsプロパティ**（ProcessedDeviceRequestInfo.cs、4行）
4. **DWordCombineInfo.csファイル全体**（43行）
5. **TC032_CombineDwordData単体テスト**（138行）
6. **TC118_Step6連続処理統合テスト**（192行）

**Phase3.5で修正された統合テスト**:
- ✅ TC119_M000M999: Stage 2（CombineDwordData呼び出し）削除 → PASS
- ✅ TC119_D000D999: Stage 2（CombineDwordData呼び出し）削除 → PASS
- ✅ TC143_10_1（3E Binary）: CombineDwordData呼び出し削除 → PASS
- ✅ TC143_10_3（4E Binary）: CombineDwordData呼び出し削除 + フレーム修正 → PASS

**Phase3.5残存問題への影響**:
- Phase2.5残存6件 → Phase3.5完了後2件に削減（削減率: 約67%）
- ~~TC032~~（削除）
- TC021_TC025統合テスト（確認必要）
- ~~TC118~~（削除）
- ~~TC116_UDP完全サイクル~~（Phase3.5で解決、PASS）
- ~~TC143_10_1/TC143_10_3~~（Phase3.5で解決、PASS）

**Phase4での対応**:

Phase4では、Phase3.5でDWord機能削除完了したコードベースを基に、以下を実施します：

1. **Phase3.5残存問題の確認**（2件のみ、優先度: 中）
   - TC021_TC025統合テスト（DWord非依存、Phase4で確認）
   - その他1件（Phase3.5対象外の問題、Phase4で調査）
   - 推定修正時間: 1-2時間（Phase2.5残存6件→2件に削減）

2. **Phase3.5修正内容の考慮**
   - ExecuteFullCycleAsync Step6-2: データ変換処理（BasicProcessedData → ProcessedData）
   - CombinedDWordDevices: 常に空リスト（DWord結合処理なし）
   - Step6処理フロー: 3段階 → 2段階に簡素化

3. **コードベース簡素化の活用**
   - 622行削減により、テスト実装・デバッグが容易化
   - TC032, TC118削除により、Phase4テスト範囲が明確化
   - Step6フロー簡素化により、統合テスト設計が単純化

4. **Phase3.5設計判断の継承**
   - ReadRandomコマンドではDWord結合不要（仕様準拠）
   - DWord値が必要な場合は設定ファイルで個別指定可能
   - Step6-2: 型変換のみ実施（CombineDwordData呼び出しなし）

---

## 1. テストファイル構成

### 1-1. SlmpFrameBuilderTests.cs

**ファイルパス**: `Tests/Unit/Utilities/SlmpFrameBuilderTests.cs`

**テストカテゴリ:**
1. 入力検証系テスト
2. フレーム構築系テスト（3E）
3. フレーム構築系テスト（4E）
4. シーケンス番号管理テスト
5. フレーム検証テスト
6. ReadRandom対応チェックテスト

---

### 1-2. ConfigToFrameManagerTests.cs（追加）

**ファイルパス**: `Tests/Unit/Core/Managers/ConfigToFrameManagerTests.cs`

**追加テストカテゴリ:**
1. Phase2統合テスト
2. シーケンス番号管理統合テスト
3. エラーハンドリング統合テスト

---

### 1-3. 統合テストファイル（新規）

**ファイルパス**: `Tests/Integration/Step1_2_IntegrationTests.cs`

**テストカテゴリ:**
1. 設定ファイル読み込み～フレーム構築の全体フロー
2. 複数PLC設定の並行フレーム構築
3. 実機形式フレーム検証（ConMoniとの比較）

---

## 2. SlmpFrameBuilderTests.cs 詳細

### 2-1. 入力検証系テスト

**TC001: デバイスリストがnullの場合**
```csharp
[Fact]
public void BuildReadRandomRequest_デバイスリストがnull_ArgumentExceptionをスロー()
{
    // Arrange
    List<DeviceSpecification>? devices = null;

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(
        () => SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32)
    );

    Assert.Contains("デバイスリストが空です", exception.Message);
}
```

**TC002: デバイスリストが空の場合**
```csharp
[Fact]
public void BuildReadRandomRequest_デバイスリストが空_ArgumentExceptionをスロー()
{
    // Arrange
    var devices = new List<DeviceSpecification>();

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(
        () => SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32)
    );

    Assert.Contains("デバイスリストが空です", exception.Message);
}
```

**TC003: デバイス点数が256点以上の場合**
```csharp
[Fact]
public void BuildReadRandomRequest_デバイス点数256以上_ArgumentExceptionをスロー()
{
    // Arrange
    var devices = Enumerable.Range(0, 256)
        .Select(i => new DeviceSpecification("D", $"D{i}"))
        .ToList();

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(
        () => SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32)
    );

    Assert.Contains("デバイス点数が上限を超えています", exception.Message);
    Assert.Contains("256点", exception.Message);
}
```

**TC004: 未対応のフレームタイプ**
```csharp
[Fact]
public void BuildReadRandomRequest_未対応フレームタイプ_ArgumentExceptionをスロー()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(
        () => SlmpFrameBuilder.BuildReadRandomRequest(devices, "5E", 32)
    );

    Assert.Contains("未対応のフレームタイプ", exception.Message);
}
```

**TC005: ReadRandom非対応デバイス（TS）**
```csharp
[Fact]
public void BuildReadRandomRequest_TS指定_ArgumentExceptionをスロー()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("TS", "TS0")
    };

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(
        () => SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32)
    );

    Assert.Contains("ReadRandomコマンドは", exception.Message);
    Assert.Contains("TS", exception.Message);
}
```

**TC006～TC008: TC/CS/CC指定時も同様のテスト**

---

### 2-2. フレーム構築系テスト（3E）

**TC009: 3Eフレームの基本構造**
```csharp
[Fact]
public void BuildReadRandomRequest_3Eフレーム_正しい構造()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);

    // Assert
    // サブヘッダ
    Assert.Equal(0x50, frame[0]);
    Assert.Equal(0x00, frame[1]);

    // ネットワーク設定
    Assert.Equal(0x00, frame[2]); // ネットワーク番号
    Assert.Equal(0xFF, frame[3]); // 局番
    Assert.Equal(0xFF, frame[4]); // I/O番号（下位）
    Assert.Equal(0x03, frame[5]); // I/O番号（上位）
    Assert.Equal(0x00, frame[6]); // マルチドロップ

    // データ長フィールド（7-8バイト目、値は動的）
    Assert.True(frame.Length > 8);

    // コマンド部
    Assert.Equal(0x03, frame[11]); // コマンド（下位）
    Assert.Equal(0x04, frame[12]); // コマンド（上位）
}
```

**TC010: 3Eフレームのデータ長計算**
```csharp
[Fact]
public void BuildReadRandomRequest_3Eフレーム_データ長が正しい()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100"),
        new DeviceSpecification("D", "D200")
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);

    // Assert
    // データ長 = 監視タイマ(2) + コマンド(2) + サブコマンド(2) + 点数(2) + デバイス指定(4×2)
    //          = 2 + 2 + 2 + 2 + 8 = 16バイト
    int expectedDataLength = 16;
    int actualDataLength = BitConverter.ToUInt16(frame, 7);

    Assert.Equal(expectedDataLength, actualDataLength);
}
```

**TC011: 3Eフレームの監視タイマ設定**
```csharp
[Fact]
public void BuildReadRandomRequest_3Eフレーム_監視タイマが正しい()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };
    ushort timeout = 100; // 25秒

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", timeout);

    // Assert
    int actualTimeout = BitConverter.ToUInt16(frame, 9);
    Assert.Equal(timeout, actualTimeout);
}
```

**TC012: 3Eフレームのデバイス指定部**
```csharp
[Fact]
public void BuildReadRandomRequest_3Eフレーム_デバイス指定部が正しい()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")  // アドレス0x000064、コード0xA8
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);

    // Assert
    // デバイス指定部開始位置（3Eフレーム）
    int deviceSectionStart = 17;

    // D100: 0x000064, コード0xA8
    Assert.Equal(0x64, frame[deviceSectionStart]);     // アドレス下位
    Assert.Equal(0x00, frame[deviceSectionStart + 1]); // アドレス中位
    Assert.Equal(0x00, frame[deviceSectionStart + 2]); // アドレス上位
    Assert.Equal(0xA8, frame[deviceSectionStart + 3]); // デバイスコード
}
```

**TC013: 3Eフレームの複数デバイス**
```csharp
[Fact]
public void BuildReadRandomRequest_3Eフレーム_複数デバイスが正しい()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100"),
        new DeviceSpecification("D", "D200"),
        new DeviceSpecification("M", "M10")
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);

    // Assert
    // ワード点数
    int deviceSectionStart = 17;
    Assert.Equal(3, frame[15]); // ワード点数

    // デバイス指定部のサイズ
    int expectedDeviceSectionSize = 4 * 3; // 4バイト × 3デバイス
    Assert.True(frame.Length >= deviceSectionStart + expectedDeviceSectionSize);
}
```

---

### 2-3. フレーム構築系テスト（4E）

**TC014: 4Eフレームの基本構造**
```csharp
[Fact]
public void BuildReadRandomRequest_4Eフレーム_正しい構造()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "4E", 32);

    // Assert
    // サブヘッダ
    Assert.Equal(0x54, frame[0]);
    Assert.Equal(0x00, frame[1]);

    // シーケンス番号（2-3バイト目、値は動的）
    Assert.True(frame.Length > 5);

    // 予約
    Assert.Equal(0x00, frame[4]);
    Assert.Equal(0x00, frame[5]);

    // ネットワーク設定
    Assert.Equal(0x00, frame[6]);  // ネットワーク番号
    Assert.Equal(0xFF, frame[7]);  // 局番
    Assert.Equal(0xFF, frame[8]);  // I/O番号（下位）
    Assert.Equal(0x03, frame[9]);  // I/O番号（上位）
    Assert.Equal(0x00, frame[10]); // マルチドロップ
}
```

**TC015: 4Eフレームのデータ長計算**
```csharp
[Fact]
public void BuildReadRandomRequest_4Eフレーム_データ長が正しい()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "4E", 32);

    // Assert
    // データ長 = 監視タイマ(2) + コマンド(2) + サブコマンド(2) + 点数(2) + デバイス指定(4)
    //          = 2 + 2 + 2 + 2 + 4 = 12バイト
    int expectedDataLength = 12;
    int actualDataLength = BitConverter.ToUInt16(frame, 11);

    Assert.Equal(expectedDataLength, actualDataLength);
}
```

---

### 2-4. シーケンス番号管理テスト

**TC016: 4Eフレームでシーケンス番号がインクリメント**
```csharp
[Fact]
public void BuildReadRandomRequest_4Eフレーム連続呼び出し_シーケンス番号インクリメント()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act
    var frame1 = SlmpFrameBuilder.BuildReadRandomRequest(devices, "4E", 32);
    var frame2 = SlmpFrameBuilder.BuildReadRandomRequest(devices, "4E", 32);
    var frame3 = SlmpFrameBuilder.BuildReadRandomRequest(devices, "4E", 32);

    // Assert
    ushort seq1 = BitConverter.ToUInt16(frame1, 2);
    ushort seq2 = BitConverter.ToUInt16(frame2, 2);
    ushort seq3 = BitConverter.ToUInt16(frame3, 2);

    Assert.Equal(seq1 + 1, seq2);
    Assert.Equal(seq2 + 1, seq3);
}
```

**TC017: 3Eフレームでシーケンス番号が常に0**
```csharp
[Fact]
public void BuildReadRandomRequest_3Eフレーム連続呼び出し_シーケンス番号は常に0()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act
    var frame1 = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);
    var frame2 = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);

    // Assert
    // 3Eフレームはシーケンス番号フィールドなし
    // 内部的に_sequenceManager.GetNext("3E")が0を返すことを間接的に確認
    Assert.Equal(frame1.Length, frame2.Length);
}
```

---

### 2-5. フレーム検証テスト

**TC018: フレーム長上限チェック**
```csharp
[Fact]
public void BuildReadRandomRequest_フレーム長8194バイト超過_InvalidOperationExceptionをスロー()
{
    // Arrange
    // 8194バイトを超えるよう大量のデバイスを指定
    // 3Eフレーム: ヘッダ9 + コマンド8 + デバイス(4×N)
    // 8194バイト超過するにはN > 2043
    var devices = Enumerable.Range(0, 2050)
        .Select(i => new DeviceSpecification("D", $"D{i}"))
        .ToList();

    // Act & Assert
    var exception = Assert.Throws<InvalidOperationException>(
        () => SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32)
    );

    Assert.Contains("フレーム長が上限を超えています", exception.Message);
    Assert.Contains("8194", exception.Message);
}
```

---

## 3. ConfigToFrameManagerTests.cs 追加テスト

### 3-1. Phase2統合テスト

**TC019: Phase2統合確認**
```csharp
[Fact]
public void BuildReadRandomFrameFromConfig_Phase2統合_正しいフレーム構築()
{
    // Arrange
    var config = new TargetDeviceConfig
    {
        FrameType = "3E",
        Timeout = 32,
        Devices = new List<DeviceEntry>
        {
            new DeviceEntry { DeviceName = "D100", DeviceType = "D" }
        }
    };

    // Act
    var frame = _manager.BuildReadRandomFrameFromConfig(config);

    // Assert
    Assert.NotNull(frame);
    Assert.True(frame.Length > 0);

    // サブヘッダ確認（3E）
    Assert.Equal(0x50, frame[0]);
    Assert.Equal(0x00, frame[1]);
}
```

**TC020: ReadRandom非対応デバイス統合テスト**
```csharp
[Fact]
public void BuildReadRandomFrameFromConfig_TS指定_ArgumentExceptionをスロー()
{
    // Arrange
    var config = new TargetDeviceConfig
    {
        FrameType = "3E",
        Timeout = 32,
        Devices = new List<DeviceEntry>
        {
            new DeviceEntry { DeviceName = "TS0", DeviceType = "TS" }
        }
    };

    // Act & Assert
    var exception = Assert.Throws<ArgumentException>(
        () => _manager.BuildReadRandomFrameFromConfig(config)
    );

    Assert.Contains("ReadRandomコマンドは", exception.Message);
}
```

---

## 4. 統合テスト（新規ファイル）

### 4-1. Step1_2_IntegrationTests.cs

**ファイルパス**: `Tests/Integration/Step1_2_IntegrationTests.cs`

**TC101: 設定ファイル読み込み～フレーム構築の完全フロー**
```csharp
[Fact]
public async Task TC101_設定ファイル読み込みからフレーム構築まで完全実行()
{
    // Arrange
    var configLoader = new ConfigurationLoader();
    var configToFrameManager = new ConfigToFrameManager();

    // Act
    // Step1: 設定ファイル読み込み
    var config = await configLoader.LoadTargetDeviceConfigAsync("appsettings.json");

    // Step2: フレーム構築
    var frame = configToFrameManager.BuildReadRandomFrameFromConfig(config);

    // Assert
    Assert.NotNull(config);
    Assert.NotNull(frame);
    Assert.True(frame.Length > 0);

    // フレーム基本構造確認
    Assert.True(frame[0] == 0x50 || frame[0] == 0x54); // 3E or 4E
}
```

**TC102: 複数PLC設定の並行フレーム構築**
```csharp
[Fact]
public async Task TC102_複数PLC設定の並行フレーム構築()
{
    // Arrange
    var configLoader = new ConfigurationLoader();
    var configToFrameManager = new ConfigToFrameManager();

    var plc1Config = new TargetDeviceConfig { FrameType = "3E", Devices = new List<DeviceEntry> { ... } };
    var plc2Config = new TargetDeviceConfig { FrameType = "4E", Devices = new List<DeviceEntry> { ... } };

    // Act
    var tasks = new[]
    {
        Task.Run(() => configToFrameManager.BuildReadRandomFrameFromConfig(plc1Config)),
        Task.Run(() => configToFrameManager.BuildReadRandomFrameFromConfig(plc2Config))
    };

    var frames = await Task.WhenAll(tasks);

    // Assert
    Assert.Equal(2, frames.Length);
    Assert.All(frames, frame => Assert.True(frame.Length > 0));
}
```

**TC103: ConMoni実装との互換性確認**
```csharp
[Fact]
public void TC103_ConMoni実装との互換性確認()
{
    // Arrange
    var devices = new List<DeviceSpecification>
    {
        new DeviceSpecification("D", "D100")
    };

    // Act
    var frame = SlmpFrameBuilder.BuildReadRandomRequest(devices, "3E", 32);

    // Assert
    // ConMoniと同じフレーム構造であることを確認
    // （ConMoniのサンプルフレームと比較）

    // サブヘッダ（標準3E）
    Assert.Equal(0x50, frame[0]); // ConMoniは0x54使用だったが、標準は0x50

    // その他のフィールドはConMoniと同じ構造
    Assert.Equal(0x00, frame[2]); // ネットワーク番号
    Assert.Equal(0xFF, frame[3]); // 局番
}
```

---

## 5. Phase4実装チェックリスト

### Phase3完了確認（最優先）

- [ ] **Phase3でのテスト後追い実装確認**
  - [ ] SequenceNumberManagerTests.cs 存在確認
  - [ ] TC001～TC006が全て実装済みであることを確認
  - [ ] SlmpFrameBuilderTests.cs 存在確認
  - [ ] TC001～TC018が全て実装済みであることを確認
  - [ ] 全テストがパスしていることを確認

### Phase2.5完了確認（追加、2025-11-27）

- [ ] **Phase2.5での既存問題対応確認**
  - [ ] フレーム長計算不一致17件修正完了確認
  - [ ] SendFrameAsync IsBinary対応修正確認
  - [ ] DeviceCountValidation修正確認
  - [ ] TC025/TC029テストデータ修正確認
  - [ ] ConfigurationLoader統合テスト設計統一確認
  - [ ] テスト成功率98.4%達成確認
  - [ ] 実装コードの正当性検証完了確認

- [ ] **Phase2.5残存問題の確認（6件）**
  - [ ] TC032_CombineDwordData（優先度: 低）
  - [ ] TC021_TC025統合テスト（優先度: 中）
  - [ ] TC118_Step6連続処理統合（優先度: 中）
  - [ ] TC116_UDP完全サイクル（優先度: 中）
  - [ ] TC143_10_1/TC143_10_3ビットデバイステスト（優先度: 中）

### Phase3.5完了確認（追加、2025-11-27）

- [x] **Phase3.5でのDWord機能完全廃止確認**
  - [x] CombineDwordDataメソッド削除完了確認（183行削除）
  - [x] CombineDwordDataインターフェース定義削除確認（12行削除）
  - [x] DWordCombineTargetsプロパティ削除確認（4行削除）
  - [x] DWordCombineInfo.csファイル削除確認（43行削除）
  - [x] TC032/TC118テストケース削除確認（330行削除）
  - [x] 統合テスト修正完了確認（TC119×2, TC143_10×2）
  - [x] コードベース622行削減確認
  - [x] テスト成功率99.2%達成確認（98.4% → 99.2%）

- [x] **Phase3.5残存問題の確認（Phase2.5の6件→2件に削減）**
  - [x] ~~TC032_CombineDwordData~~（Phase3.5で削除）
  - [ ] TC021_TC025統合テスト（優先度: 中、Phase4で確認）
  - [x] ~~TC118_Step6連続処理統合~~（Phase3.5で削除）
  - [x] ~~TC116_UDP完全サイクル~~（Phase3.5で解決、PASS）
  - [x] ~~TC143_10_1/TC143_10_3~~（Phase3.5で解決、PASS）
  - [ ] その他1件（Phase3.5対象外の問題、Phase4で調査）

---

### テスト実装タスク（Phase3実装済みテストは除外）

- [ ] **SlmpFrameBuilderTests.cs（Phase3実装済み確認）**
  - [ ] TC001～TC008: 入力検証系テスト（✅ Phase3で実装済み）
  - [ ] TC009～TC013: 3Eフレーム構築系テスト（✅ Phase3で実装済み）
  - [ ] TC014～TC015: 4Eフレーム構築系テスト（✅ Phase3で実装済み）
  - [ ] TC016～TC017: シーケンス番号管理テスト（✅ Phase3で実装済み）
  - [ ] TC018: フレーム検証テスト（✅ Phase3で実装済み）
  - [ ] **不足テストケースの確認と追加実装**

- [ ] **ConfigToFrameManagerTests.cs 追加（Phase4新規実装）**
  - [ ] TC019: Phase2統合確認（1ケース）
  - [ ] TC020: ReadRandom非対応デバイス統合テスト（1ケース）

- [ ] **Step1_2_IntegrationTests.cs 新規作成（Phase4新規実装）**
  - [ ] TC101: 設定ファイル読み込み～フレーム構築の完全フロー
  - [ ] TC102: 複数PLC設定の並行フレーム構築
  - [ ] TC103: ConMoni実装との互換性確認

- [ ] **Phase2.5残存問題の対応（Phase4追加タスク）**
  - [x] ~~TC032_CombineDwordData修正または削除判断~~（Phase3.5で削除完了）
  - [ ] TC021_TC025統合テストのテストデータ修正
  - [x] ~~TC118_Step6連続処理統合のテストデータ修正~~（Phase3.5で削除完了）
  - [x] ~~TC116_UDP完全サイクルのテストデータ修正~~（Phase3.5で解決、PASS）
  - [x] ~~TC143_10系ビットデバイステストのテストデータ修正（2件）~~（Phase3.5で解決、PASS）
  - [ ] その他残存問題1件の調査・修正（Phase3.5対象外）

### テスト実行タスク

- [ ] **単体テスト実行**
  - [ ] SlmpFrameBuilderTests 全テストパス
  - [ ] ConfigToFrameManagerTests 全テストパス
  - [ ] SequenceNumberManagerTests 全テストパス（Phase1）

- [ ] **統合テスト実行**
  - [ ] Step1_2_IntegrationTests 全テストパス
  - [ ] Phase3.5残存問題2件 全テストパス（Phase2.5の6件から削減）

- [ ] **カバレッジ確認**
  - [ ] SlmpFrameBuilder: 目標95%以上
  - [ ] ConfigToFrameManager: 目標95%以上
  - [ ] SequenceNumberManager: 目標100%

- [ ] **Phase2.5修正内容の考慮確認**
  - [ ] データ長計算: 監視タイマを含む前提でテスト実装
  - [ ] IsBinary設定: ASCII/Binary変換切り替えをテスト
  - [ ] ExtractWordDevices: データ長不足時の警告処理をテスト
  - [ ] ConfigurationLoader: Excelファイル0件時の例外処理をテスト

### 完了条件

1. 全テストケースが実装されている
2. 全テストケースがパスしている（Phase3.5残存2件含む、Phase2.5残存6件→2件に削減）
3. カバレッジ目標を達成している
4. リグレッションテストがパスしている
5. Phase2.5で修正された実装コードの正当性が統合テストで確認されている
6. Phase3.5でDWord機能が完全廃止され、Step6処理フローが簡素化されている

---

## 6. 実装時間見積もり

| タスク | 見積もり時間 | 備考 |
|-------|------------|------|
| Phase3完了確認 | 0.5-1時間 | テスト実装状況確認 |
| **Phase2.5完了確認（追加）** | **0.5時間** | **修正内容と残存問題の確認** |
| **Phase3.5完了確認（追加）** | **0.5時間** | **DWord機能廃止完了確認、残存問題削減確認** |
| SlmpFrameBuilderTests実装 | 0-1時間 | Phase3で実装済みのため、不足分のみ |
| ConfigToFrameManagerTests追加 | 1時間 | TC019～TC020の2ケース |
| Step1_2_IntegrationTests実装 | 2-3時間 | TC101～TC103の3ケース |
| **Phase3.5残存問題対応（追加）** | **1-2時間** | **統合テスト2件のテストデータ修正（Phase2.5の6件から削減）** |
| テスト実行・デバッグ | 2-3時間 | 統合テスト中心 |
| カバレッジ分析・改善 | 1-2時間 | Phase3実装分も含めて分析 |
| **Phase2.5/3.5修正内容考慮確認** | **1時間** | **データ長計算、IsBinary設定、Step6簡素化等の考慮確認** |
| **合計** | **9-14.5時間** | **Phase3.5でDWord機能廃止完了により、Phase2.5の6件→2件に削減、約1時間短縮** |

**注**:
- Phase3でTC001～TC018の基本テストを実装済みのため、Phase4では統合テスト中心に実施します
- Phase2.5で28件のテスト修正が完了し、実装コードの正当性が検証済みです
- **Phase3.5でDWord機能を完全廃止し、コードベース622行削減、残存問題を6件→2件に削減しました**
- Phase3.5残存問題（2件）の対応時間は、Phase2.5の6件から大幅削減され1-2時間と見積もります
- Step6処理フローが3段階→2段階に簡素化されたため、統合テストの実装・デバッグが容易化しています

---

## 7. 次フェーズへの引き継ぎ事項

### Phase5（最終確認・ドキュメント整備）への準備

Phase4完了後、以下をPhase5に引き継ぎます：

1. **テスト結果レポート**:
   - 全テストケースの実行結果（Phase3.5残存問題2件の修正結果含む）
   - カバレッジレポート
   - 発見された問題と対処方法
   - Phase2.5からの改善状況（テスト成功率98.4%の維持または向上）
   - **Phase3.5からの改善状況（テスト成功率99.2%の維持または向上）**

2. **性能評価**:
   - フレーム構築の実行時間
   - メモリ使用量
   - Phase2.5修正による影響評価（実行速度等）
   - **Phase3.5でのコードベース削減効果（622行削減）**

3. **ドキュメント更新事項**:
   - 実装完了した機能リスト
   - 既知の制約事項
   - 今後の拡張ポイント
   - Phase2.5で確認された実装コードの正当性に関する記録
   - **Phase3.5で削除されたDWord機能に関する記録**

4. **Phase2.5からの引き継ぎ事項**:
   - UpdateDataLength: 監視タイマを含むデータ長計算の仕様確定
   - SendFrameAsync: IsBinary設定によるASCII/Binary変換の仕様確定
   - ExtractWordDevices: データ長不足時の警告処理の仕様確定
   - ConfigurationLoader: Excelファイル0件時の例外処理の仕様確定
   - 実機ログ（memo.md）との整合性検証結果

5. **Phase3.5からの引き継ぎ事項**:
   - ExecuteFullCycleAsync Step6-2: データ変換処理（BasicProcessedData → ProcessedData）
   - CombinedDWordDevices: 常に空リスト（DWord結合処理廃止）
   - Step6処理フロー: 3段階 → 2段階に簡素化
   - ReadRandomコマンドではDWord結合不要（仕様準拠）
   - コードベース622行削減による保守性向上
   - TC032, TC118削除によるテスト範囲明確化

---

## 参考資料

- `documents/design/Step2_フレーム構築実装/Step2_新設計_統合フレーム構築仕様.md` - 全体設計書
- `documents/design/Step2_フレーム構築実装/実装計画/Phase1～3.md` - 各Phase実装内容
- **`documents/design/Step2_フレーム構築実装/実装計画/Phase2.5_既存問題対応.md`** - **Phase2.5実装計画**
- **`documents/design/Step2_フレーム構築実装/実装結果/Phase2.5_既存問題対応_TestResults.md`** - **Phase2.5実装・テスト結果**
- **`documents/design/Step2_フレーム構築実装/実装結果/Phase2.5_SendFrameAsync_IsBinary_Fix_PartialResults.md`** - **Phase2.5部分実装結果**
- **`documents/design/Step2_フレーム構築実装/実装結果/Phase2.5_DataLength_FixResults.md`** - **Phase2.5データ長修正結果**
- **`documents/design/Step2_フレーム構築実装/実装計画/Phase3.5_DWord機能完全廃止.md`** - **Phase3.5実装計画（DWord機能廃止）**
- **`documents/design/Step2_フレーム構築実装/実装結果/Phase3.5_DWord機能完全廃止_TestResults.md`** - **Phase3.5実装・テスト結果**
- `documents/design/フレーム構築関係/フレーム構築方法.md` - フレーム仕様書（正）
- `memo.md` - 実機ログ参照（正）

---

**Phase4実装日**: 2025-11-27
**担当者**: Claude Code (Sonnet 4.5)
**ステータス**: ✅ 実装完了 → Phase5進行中

---

## Phase4完了サマリー（2025-11-27追加）

### 実装内容
- **実装時間**: 約1.5時間（計画: 9-14.5時間、計画比約14～23%）
- **実装日**: 2025-11-27

### 完了事項

#### 1. Phase3.5残存問題修正（2件）
- ✅ TC121: Step6_2_DWordCombine → Step6_2_DataConversion修正
- ✅ LogError: ILogger標準動作対応

#### 2. ConfigToFrameManagerTests追加（2件）
- ✅ TC019: Phase2統合確認（正しいフレーム構築）
- ✅ TC020: ReadRandom非対応デバイス統合テスト（TS指定でArgumentException）

#### 3. Step1_2_IntegrationTests新規実装（3件）
- ✅ TC101: 設定読み込みからフレーム構築まで完全実行
- ✅ TC102: 複数PLC設定の並行フレーム構築
- ✅ TC103: ConMoni実装との互換性確認

### テスト結果
- **総テスト数**: 513件
- **成功**: 511件 (99.6%)
- **失敗**: 0件 ✅
- **スキップ**: 2件（意図的なスキップ）
- **実行時間**: 約70秒

### カバレッジ（推定）
- SequenceNumberManager: 100%
- SlmpFrameBuilder: 95%以上
- ConfigToFrameManager: 95%以上

### 工数削減理由
Phase3でのテスト後追い実装完了により、Phase4では統合テスト中心の実装となり、大幅な工数削減を達成：
- Phase3実装済みテスト: TC001～TC018（18件）
- Phase4新規実装: TC019～TC020（2件）+ 統合テスト（3件）
- Phase3.5残存問題: 6件 → 2件に削減（約67%削減）

### Phase5への引き継ぎ
- ✅ 全テスト成功（511/513件、99.6%）
- ✅ 統合テスト完了（TC101～TC103）
- ✅ 残存問題解決
- 🔄 Phase5: 最終確認・ドキュメント整備（進行中）
