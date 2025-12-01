# ReadRandom ASCII形式実装・テスト結果

**作成日**: 2025-11-18
**最終更新**: 2025-11-18

## 概要

ReadRandom(0x0403)コマンドのASCII形式実装およびテスト結果の記録。
バイナリ形式に加えて、ASCII形式のSLMPフレーム送受信機能を実装し、全27個のテストケースが成功。

---

## 1. 実装内容

### 1.1 実装メソッド

| クラス | メソッド名 | 機能 | 行数 |
|--------|-----------|------|------|
| ConfigToFrameManager | `BuildReadRandomFrameFromConfigAscii()` | TargetDeviceConfigからASCIIフレーム構築 | 約30行 |
| SlmpFrameBuilder | `BuildReadRandomRequestAscii()` | ReadRandomコマンドのASCIIフレーム構築 | - |
| PlcCommunicationManager | `Parse3EFrameAscii()` | 3E ASCII応答の解析 | - |
| PlcCommunicationManager | `Parse4EFrameAscii()` | 4E ASCII応答の解析 | - |

### 1.2 主要機能

| 機能カテゴリ | 説明 | 対応状況 |
|-------------|------|----------|
| ASCII送信フレーム構築 | 3E/4Eフレームのバイナリ→ASCII変換 | ✅ 実装済み |
| ASCII応答解析 | ASCII形式応答の16進文字列解析 | ✅ 実装済み |
| エラーハンドリング | null/空/不正フレーム検証 | ✅ 実装済み |
| 構造化データ変換 | ASCII応答から構造化データへ変換 | ✅ 実装済み |

### 1.3 重要な実装判断

**ASCII/バイナリ統合アーキテクチャ**:
- バイナリ実装を基盤として、ASCII変換レイヤーを追加
- 理由: コードの重複を避け、バイナリ実装の信頼性を活用

**Convert.ToHexString使用**:
- .NET標準APIを使用してバイナリ→ASCII変換
- 理由: 高速・信頼性・保守性

**既存テスト構造の活用**:
- バイナリテストと同様の構造でASCIIテストを作成
- 理由: 統一されたテストパターンで保守性向上

---

## 2. テスト結果

### 2.1 全体サマリー

```
実行日時: 2025-11-18
VSTest: 17.14.1 (x64)
.NET: 9.0.8
xUnit.net: v2.8.2

結果: 成功 - 失敗: 0、合格: 27、スキップ: 0、合計: 27
実行時間: 約0.95秒
```

### 2.2 テストケース内訳

| テストカテゴリ | テスト数 | 成功 | 失敗 | 実行時間 |
|---------------|----------|------|------|----------|
| ConfigToFrameManager (ASCII) | 5 | 5 | 0 | 約25ms |
| SlmpFrameBuilder (ASCII) | 12 | 12 | 0 | 約30ms |
| PlcCommunicationManager (ASCII解析) | 8 | 8 | 0 | 約25ms |
| ParseRawToStructuredData (ASCII) | 2 | 2 | 0 | 約23ms |
| **合計** | **27** | **27** | **0** | **953ms** |

### 2.3 TDD実装フロー

**Red → Green → Refactorサイクル**:
1. ASCIIテストケースを先行実装（Red）
2. ASCII実装メソッドを追加してパス（Green）
3. コード品質確認（Refactor - 追加修正不要と判断）

---

## 3. テストケース詳細

### 3.1 ConfigToFrameManager ASCIIテスト (5テスト)

#### TC_Step12_ASCII_001: 正常系 - 4Eフレーム48デバイス
| 項目 | 内容 |
|------|------|
| **検証内容** | TargetDeviceConfigから4E ASCIIフレーム構築 |
| **入力** | FrameType="4E", Timeout=32, Devices=48デバイス(M0～M752) |
| **期待結果** | ASCII文字列長=426文字（213バイト×2） |
| **実行結果** | ✅ 成功 (9ms) |
| **検証ポイント** | バイナリフレームとの整合性確認 |

#### TC_Step12_ASCII_002: 正常系 - 3Eフレーム
| 項目 | 内容 |
|------|------|
| **検証内容** | TargetDeviceConfigから3E ASCIIフレーム構築 |
| **入力** | FrameType="3E", Timeout=16, Devices=3デバイス(D100-D102) |
| **期待結果** | ASCII文字列長=58文字（29バイト×2） |
| **実行結果** | ✅ 成功 (< 1ms) |

#### TC_Step12_ASCII_003: 異常系 - デバイスリスト空
| 項目 | 内容 |
|------|------|
| **検証内容** | 空デバイスリストでArgumentException発生 |
| **実行結果** | ✅ 成功 (8ms) |

#### TC_Step12_ASCII_004: 異常系 - Config null
| 項目 | 内容 |
|------|------|
| **検証内容** | nullでArgumentNullException発生 |
| **実行結果** | ✅ 成功 (6ms) |

#### TC_Step12_ASCII_005: 異常系 - 未対応フレームタイプ
| 項目 | 内容 |
|------|------|
| **検証内容** | "5E"で ArgumentException発生 |
| **実行結果** | ✅ 成功 (< 1ms) |

### 3.2 SlmpFrameBuilder ASCIIテスト (12テスト)

#### 正常系テスト (8テスト)
- ✅ BuildReadRandomRequestAscii_4EFrame_CorrectHeaderStructure (18ms)
  - 4Eフレームのサブヘッダ（"5400"）検証

- ✅ BuildReadRandomRequestAscii_3EFrame_CorrectHeaderStructure
  - 3Eフレームのサブヘッダ（"5000"）検証

- ✅ BuildReadRandomRequestAscii_1Device_MatchesBinaryConversion
  - 1デバイス指定時のバイナリ→ASCII変換正確性

- ✅ BuildReadRandomRequestAscii_48Devices_MatchesBinaryConversion
  - 48デバイス指定時の変換正確性

- ✅ BuildReadRandomRequestAscii_WordCount_IsCorrect
  - ワード点数フィールドの正確性

- ✅ BuildReadRandomRequestAscii_CommandCode_IsCorrect
  - コマンドコード（"0304"）の正確性

- ✅ BuildReadRandomRequestAscii_DataLength_IsCorrectlyEncoded
  - データ長フィールドの正確性

- ✅ BuildReadRandomRequestAscii_ConmoniTest_ExactMatch
  - Conmoniサンプルとの完全一致検証

#### デバイス指定テスト (2テスト)
- ✅ BuildReadRandomRequestAscii_DeviceD100_CorrectSpecification
  - D100デバイス指定の正確性

- ✅ BuildReadRandomRequestAscii_DeviceD61000_CorrectSpecification
  - D61000デバイス指定の正確性

#### 異常系テスト (2テスト)
- ✅ BuildReadRandomRequestAscii_EmptyDevices_ThrowsArgumentException
  - 空デバイスリスト検証

- ✅ BuildReadRandomRequestAscii_NullDevices_ThrowsArgumentException
  - nullデバイスリスト検証

### 3.3 PlcCommunicationManager ASCII解析テスト (8テスト)

#### 3E ASCII解析 (4テスト)
- ✅ TC037_3E_ASCII_ヘッダー解析成功
  - サブヘッダ、ネットワーク情報、終了コードの解析

- ✅ TC037_3E_ASCII_ビットデータ解析成功
  - ビットデバイス（M100など）の解析

- ✅ TC037_3E_ASCII_エラー終了コード処理 (18ms)
  - エラー終了コード検出と例外スロー

- ✅ TC037_3E_ASCII_不完全フレームエラー
  - 不完全なASCIIフレームの検出

#### 4E ASCII解析 (4テスト)
- ✅ TC038_4E_ASCII_基本解析成功
  - 4Eフレーム構造の基本解析

- ✅ TC038_4E_ASCII_ヘッダーサイズ検証
  - 4Eフレームのヘッダーサイズ（28文字）検証

- ✅ TC038_4E_ASCII_データ長整合性検証
  - データ長フィールドと実データの整合性

- ✅ TC038_4E_ASCII_不完全フレームエラー
  - 不完全な4E ASCIIフレームの検出

### 3.4 ParseRawToStructuredData ASCIIテスト (2テスト)

#### TC037_ParseRawToStructuredData_3E_ASCII_基本構造化成功
| 項目 | 内容 |
|------|------|
| **検証内容** | 3E ASCII応答から構造化データへ変換 |
| **入力** | 3E ASCIIフレーム（M100ビットデータ） |
| **期待結果** | StructuredData構築、フィールド値取得可能 |
| **実行結果** | ✅ 成功 (2ms) |
| **検証ポイント** | ビットデバイスの正確な解析 |

#### TC038_ParseRawToStructuredData_4E_ASCII_基本構造化成功
| 項目 | 内容 |
|------|------|
| **検証内容** | 4E ASCII応答から構造化データへ変換 |
| **入力** | 4E ASCIIフレーム（D8ワードデータ） |
| **期待結果** | StructuredData構築、UInt16値取得可能 |
| **実行結果** | ✅ 成功 (21ms) |
| **検証ポイント** | ワードデバイスの正確な解析 |

---

## 4. 実装の詳細

### 4.1 BuildReadRandomFrameFromConfigAscii メソッド

```csharp
public string BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig config)
{
    // 1. null チェック
    if (config == null)
    {
        throw new ArgumentNullException(nameof(config));
    }

    // 2. デバイスリスト検証
    if (config.Devices == null || config.Devices.Count == 0)
    {
        throw new ArgumentException("デバイスリストが空です", nameof(config));
    }

    // 3. フレームタイプ検証
    if (config.FrameType != "3E" && config.FrameType != "4E")
    {
        throw new ArgumentException($"未対応のフレームタイプ: {config.FrameType}", nameof(config));
    }

    // 4. SlmpFrameBuilder.BuildReadRandomRequestAscii() を呼び出し
    string asciiFrame = SlmpFrameBuilder.BuildReadRandomRequestAscii(
        config.Devices,
        config.FrameType,
        config.Timeout
    );

    return asciiFrame;
}
```

### 4.2 実装の特徴

| 特徴 | 説明 |
|------|------|
| バイナリ実装との統合 | バイナリメソッドと同様の検証ロジック |
| ASCII変換の信頼性 | .NET標準APIを使用 |
| エラーハンドリング | バイナリと同等のエラー検証 |
| テスタビリティ | バイナリとの比較テストが容易 |

### 4.3 依存関係

**Phase2実装との連携**:
- SlmpFrameBuilder.BuildReadRandomRequest() - バイナリ版
- SlmpFrameBuilder.BuildReadRandomRequestAscii() - ASCII版
- 両者でコア機能を共有

**ASCII/バイナリ変換**:
- Convert.ToHexString() - バイナリ→ASCII
- Convert.FromHexString() - ASCII→バイナリ

---

## 5. 実行環境

- **.NET SDK**: 9.0.304
- **xUnit.net**: v2.8.2+699d445a1a
- **VSTest**: 17.14.1 (x64)
- **プラットフォーム**: .NET 9.0.8 (64-bit)
- **OS**: Windows
- **ビルド構成**: Debug
- **テスト実行モード**: オフライン動作確認（実機PLC接続なし）

---

## 6. 検証完了事項

### 6.1 機能要件

✅ **ASCII送信フレーム構築**: 3E/4EフレームのASCII形式構築
✅ **ASCII応答解析**: ASCII形式応答の正確な解析
✅ **エラーハンドリング**: null/空/不正入力の検証
✅ **構造化データ変換**: ASCII応答から構造化データへの変換
✅ **バイナリとの整合性**: ASCII/バイナリ間の相互変換正確性

### 6.2 テストカバレッジ

- **メソッドカバレッジ**: 100%（全パブリックメソッド）
- **TDDサイクル**: Red → Green → Refactor完了
- **成功率**: 100% (27/27テスト合格)
- **実行時間**: 953ms（高速）

### 6.3 統合確認

**バイナリ実装との統合**:
- バイナリ版と同等の機能を提供
- 相互変換の正確性確認済み
- エラーハンドリングの一貫性確保

---

## 7. 次ステップへの引き継ぎ事項

### 7.1 完了事項

✅ **ConfigToFrameManager ASCII実装**: BuildReadRandomFrameFromConfigAscii完成
✅ **SlmpFrameBuilder ASCII実装**: BuildReadRandomRequestAscii完成
✅ **PlcCommunicationManager ASCII解析**: Parse3EFrameAscii/Parse4EFrameAscii完成
✅ **構造化データ変換**: ParseRawToStructuredData ASCII対応完了

### 7.2 実機テストへの準備

⏳ **実機PLC接続テスト**
- ASCII形式でのPLC実機通信確認
- Phase4-ステップ13以降で実施予定

⏳ **パフォーマンス測定**
- ASCII/バイナリの性能比較
- 大量デバイス通信時の性能確認

---

## 8. Phase実装の進化

| フェーズ | 実装内容 | テスト数 | 状態 |
|---------|---------|---------|------|
| Phase1 | DeviceCode、DeviceSpecification | 78 | ✅ 完了 |
| Phase2 | BuildReadRandomRequest (Binary) | 21 | ✅ 完了 |
| Phase2拡張 | BuildReadRandomRequest (ASCII) | 12 | ✅ 完了 |
| Phase4-Step12 | BuildReadRandomFrameFromConfig | 5 | ✅ 完了 |
| Phase4-Step12拡張 | BuildReadRandomFrameFromConfigAscii | 5 | ✅ 完了 |
| Phase4-Step13 | ReadRandomCycle | - | ✅ 完了 |
| PlcCommunicationManager | ASCII解析・構造化 | 10 | ✅ 完了 |
| **累計** | **-** | **131+** | **100%成功** |

---

**実装完了率**: 100%
**テスト合格率**: 100% (27/27)
**実装方式**: TDD (Test-Driven Development)
**関連ドキュメント**:
- Phase1テスト結果: `Phase1_DeviceCode_DeviceSpecification_TestResults.md`
- Phase2テスト結果: `Phase2_SlmpFrameBuilder_TestResults.md`
- Phase4-Step12テスト結果: `Phase4_Step12_ConfigToFrameManager_TestResults.md`
- Phase4-Step13テスト結果: `Phase4_Step13_ReadRandomCycle_TestResults.md`
