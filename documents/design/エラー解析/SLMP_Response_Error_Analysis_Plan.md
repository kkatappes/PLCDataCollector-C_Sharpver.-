# SLMP応答エラー詳細分析計画書

## 📋 プロジェクト概要

**プロジェクト名**: SLMP応答解析エラー修正プロジェクト
**目標**: 「返答にDが入るエラー」の根本原因解明と修正
**開発手法**: テストファースト開発（Red-Green-Refactor）
**作成日**: 2025年10月2日
**優先度**: **最高優先** - 通信停止リスク回避

## 🔍 エラー詳細分析

### エラー発生状況
```
無効な16進文字: バイト値=0xD0, 文字='D' (ASCII=208), 有効な16進文字は 0-9, A-F, a-f です
```

**発生箇所**: `SlmpResponseParser.cs:345` - `GetHexValue`メソッド
**呼び出し経路**: `ReadTypeNameAsync` → `ParseResponse` → `ParseAsciiResponse` → `ParseHexUshort` → `GetHexValue`
**影響範囲**: PLC型名取得処理、全SLMP通信に潜在的影響

### 根本原因の仮説

#### 1. バイナリ/ASCII応答形式判定の問題
- **現象**: バイナリ応答をASCII形式として誤解析
- **原因**: PLCがバイナリ形式で応答しているが、パーサーがASCII形式として処理
- **証拠**: バイト値0xD0は有効なバイナリデータだが、ASCII 16進文字として無効

#### 2. 擬似ダブルワード分割処理の間接的影響
- **Phase 4実装**: ReadMixedDevicesAsync API導入
- **エンディアン変換**: 0xDEADBEEF → 0xBEEF, 0xDEAD
- **関連性**: エラーバイト0xD0と"DEAD"の"D"部分の符合
- **推測**: 分割処理が応答形式に影響を与えている可能性

#### 3. 文字エンコーディングの問題
- **ASCII判定**: 有効範囲 0x30-0x39 (0-9), 0x41-0x46 (A-F), 0x61-0x66 (a-f)
- **エラー値**: 0xD0 = 208 (ASCII範囲外)
- **可能性**: 文字エンコーディング変換時の文字化け

## 🎯 修正計画

### Phase 1: 応答形式自動判定機能実装

#### サブタスク1.1: バイナリ/ASCII判定ロジック追加
**実装場所**: `SlmpResponseParser.cs`

**新規メソッド**:
```csharp
/// <summary>
/// 応答データがバイナリ形式かASCII形式かを判定
/// </summary>
/// <param name="responseFrame">応答フレーム</param>
/// <returns>true: バイナリ形式, false: ASCII形式</returns>
public static bool IsBinaryResponse(byte[] responseFrame)
{
    // ヘッダー解析による形式判定
    // ASCII形式の場合、最初の数バイトは印刷可能文字
    // バイナリ形式の場合、制御文字や高位バイトが含まれる
}
```

**判定基準**:
1. **ヘッダー文字判定**: ASCII印刷可能文字範囲チェック
2. **パターンマッチング**: 既知のバイナリ/ASCIIパターン識別
3. **統計的判定**: 非印刷可能文字の比率による判定

#### サブタスク1.2: フォールバック処理強化
**実装内容**:
```csharp
public static SlmpResponse ParseResponse(byte[] responseFrame, bool isBinary, SlmpFrameVersion version)
{
    try
    {
        // 指定された形式で解析を試行
        if (isBinary)
            return ParseBinaryResponse(responseFrame, version);
        else
            return ParseAsciiResponse(responseFrame, version);
    }
    catch (ArgumentException ex) when (ex.Message.Contains("無効な16進文字"))
    {
        // 形式判定が間違っていた場合、逆の形式で再試行
        try
        {
            return isBinary ?
                ParseAsciiResponse(responseFrame, version) :
                ParseBinaryResponse(responseFrame, version);
        }
        catch (Exception fallbackEx)
        {
            // 両方とも失敗した場合、詳細なエラー情報を提供
            throw new SlmpCommunicationException(
                "応答データの解析に失敗しました。バイナリ/ASCII両形式での解析を試行しましたが失敗しました。",
                ex);
        }
    }
}
```

### Phase 2: 擬似ダブルワード処理との統合検証

#### サブタスク2.1: ReadMixedDevicesAsync影響調査
**調査対象**:
- `SlmpClient.cs:1342-1443` ReadMixedDevicesAsync実装
- DWord分割処理が応答形式に与える影響
- エンディアン変換処理の妥当性

**検証コード例**:
```csharp
[Fact]
public async Task ReadMixedDevicesAsync_ShouldNotAffectResponseParsing()
{
    // DWord分割を含む混合デバイス読み取りテスト
    var dwordDevices = new[] { (DeviceCode.D, 300u), (DeviceCode.D, 400u) };

    // 実際のPLC応答をモックして形式チェック
    var result = await client.ReadMixedDevicesAsync(
        wordDevices: Array.Empty<(DeviceCode, uint)>(),
        bitDevices: Array.Empty<(DeviceCode, uint)>(),
        dwordDevices: dwordDevices);

    // 応答解析が正常に完了することを確認
    Assert.NotNull(result.dwordData);
}
```

#### サブタスク2.2: エンディアン変換影響分析
**分析項目**:
1. **DWord分割時の応答データ形式変化**
2. **エンディアン変換が応答パーサーに与える影響**
3. **0xDEADBEEFパターンと実際のエラーバイト0xD0の関連性**

### Phase 3: エラーハンドリング強化

#### サブタスク3.1: GetHexValueメソッド改善
**現在のコード**:
```csharp
public static int GetHexValue(byte hexChar)
{
    return hexChar switch
    {
        >= (byte)'0' and <= (byte)'9' => hexChar - '0',
        >= (byte)'A' and <= (byte)'F' => hexChar - 'A' + 10,
        >= (byte)'a' and <= (byte)'f' => hexChar - 'a' + 10,
        _ => throw new ArgumentException($"無効な16進文字: バイト値=0x{hexChar:X2}, 文字='{(char)hexChar}' (ASCII={hexChar}), 有効な16進文字は 0-9, A-F, a-f です")
    };
}
```

**改善版**:
```csharp
public static int GetHexValue(byte hexChar, bool allowInvalidChars = false)
{
    return hexChar switch
    {
        >= (byte)'0' and <= (byte)'9' => hexChar - '0',
        >= (byte)'A' and <= (byte)'F' => hexChar - 'A' + 10,
        >= (byte)'a' and <= (byte)'f' => hexChar - 'a' + 10,
        _ when allowInvalidChars => 0, // デフォルト値を返す
        _ => throw new ArgumentException($"無効な16進文字: バイト値=0x{hexChar:X2}, 文字='{(char)hexChar}' (ASCII={hexChar}), 有効な16進文字は 0-9, A-F, a-f です。応答データがバイナリ形式の可能性があります。")
    };
}
```

#### サブタスク3.2: 継続稼働モード対応
**ContinuitySettings統合**:
```csharp
public static SlmpResponse ParseResponseWithContinuity(
    byte[] responseFrame,
    bool isBinary,
    SlmpFrameVersion version,
    ContinuitySettings continuitySettings)
{
    try
    {
        return ParseResponse(responseFrame, isBinary, version);
    }
    catch (Exception ex) when (continuitySettings?.ErrorHandlingMode == ErrorHandlingMode.ReturnDefaultAndContinue)
    {
        // 継続稼働モードの場合、デフォルト応答を返す
        return CreateDefaultResponse(ex);
    }
}
```

## 🧪 テスト計画

### テストケース設計

#### 1. バイナリ/ASCII判定テスト
```csharp
[Theory]
[InlineData(new byte[] { 0x35, 0x30, 0x30, 0x30 }, false)] // ASCII "5000"
[InlineData(new byte[] { 0xD0, 0x07, 0x00, 0x00 }, true)]  // Binary response
[InlineData(new byte[] { 0x46, 0x46, 0x46, 0x46 }, false)] // ASCII "FFFF"
public void IsBinaryResponse_ShouldDetectCorrectFormat(byte[] data, bool expectedIsBinary)
```

#### 2. フォールバック処理テスト
```csharp
[Fact]
public void ParseResponse_WhenWrongFormatSpecified_ShouldFallbackToCorrectFormat()
{
    // バイナリデータをASCII指定で解析 → 自動でバイナリに切り替え
    var binaryData = new byte[] { 0xD0, 0x07, 0x00, 0x00 };
    var result = SlmpResponseParser.ParseResponse(binaryData, isBinary: false, SlmpFrameVersion.Version3E);

    Assert.NotNull(result);
}
```

#### 3. 擬似ダブルワード統合テスト
```csharp
[Fact]
public async Task ReadMixedDevicesWithDword_ShouldParseResponseCorrectly()
{
    // エンディアン変換を含むDWord読み取りテスト
    var dwordDevices = new[] { (DeviceCode.D, 300u) };
    var result = await client.ReadMixedDevicesAsync(
        Array.Empty<(DeviceCode, uint)>(),
        Array.Empty<(DeviceCode, uint)>(),
        dwordDevices);

    // 0xDEADBEEFのようなパターンでもエラーが発生しないことを確認
    Assert.NotNull(result.dwordData);
}
```

## 📈 実装スケジュール

### Week 1: Phase 1実装
- **Day 1-2**: バイナリ/ASCII判定ロジック実装
- **Day 3-4**: フォールバック処理実装とテスト
- **Day 5**: 統合テストと動作確認

### Week 2: Phase 2検証
- **Day 1-2**: ReadMixedDevicesAsync影響調査
- **Day 3-4**: エンディアン変換影響分析
- **Day 5**: 擬似ダブルワード統合テスト

### Week 3: Phase 3完了
- **Day 1-2**: エラーハンドリング強化実装
- **Day 3-4**: 継続稼働モード統合
- **Day 5**: 最終テストと本番適用準備

## 🎯 成功基準

### 技術的成功基準
- ✅ 「返答にDが入るエラー」の完全解消
- ✅ ReadTypeNameAsync処理の安定化
- ✅ バイナリ/ASCII両形式への対応
- ✅ 擬似ダブルワード機能との完全互換性

### 品質基準
- ✅ 全テストケース合格（カバレッジ95%以上）
- ✅ エラー処理の堅牢性確保
- ✅ 既存API互換性の完全保持
- ✅ パフォーマンス劣化なし（±5%以内）

### 運用基準
- ✅ PLC通信の安定稼働（24時間連続動作）
- ✅ フォールバック処理の正常動作
- ✅ ログ出力の適切な情報提供
- ✅ 継続稼働モードでの自動回復

## 🔧 実装ファイル一覧

### 修正対象ファイル
- `andon/Serialization/SlmpResponseParser.cs` - メイン修正箇所
- `andon/Core/SlmpClient.cs` - ReadTypeNameAsync改善
- `andon/Constants/TypeCode.cs` - 型定義追加（必要に応じて）

### 新規テストファイル
- `andon.Tests/Serialization/SlmpResponseParserAdvancedTests.cs` - 応答形式判定テスト
- `andon.Tests/Integration/PseudoDwordResponseParsingTests.cs` - 統合テスト

### 設定ファイル
- `andon/appsettings.json` - デバッグ設定追加

## 📋 注意事項

### 重要な制約
1. **既存API互換性**: 既存のSlmpClient APIを変更してはいけない
2. **パフォーマンス**: 応答解析処理の性能劣化を避ける
3. **安全性**: フォールバック処理で無限ループを避ける
4. **ログ出力**: デバッグ情報を適切に出力する

### リスク管理
- **通信断リスク**: 修正により通信が完全に停止するリスク
- **互換性リスク**: 既存コードへの影響
- **性能リスク**: 応答解析の処理時間増加

---

**文書管理**:
- 作成者: Claude Code
- 作成日: 2025年10月2日
- バージョン: 1.0
- ステータス: 🔄 **進行中** - Phase 1実装開始