# ProcessedResponseData修正レポート

**修正日**: 2025年11月27日
**修正対象**: ProcessedResponseData.BasicProcessedDevicesセッター
**修正理由**: ParseRawToStructuredData関連テストの失敗（7件）を解決

---

## 問題の概要

### 発生していた問題
- TC037_ParseRawToStructuredData_3Eフレーム解析: 失敗
- TC038_ParseRawToStructuredData_4Eフレーム解析: 失敗
- TC038_ParseRawToStructuredData_4E_Binary_実機データ構造化成功: 失敗
- TC_DeviceCountValidation_001～004: 4件失敗

### 問題の根本原因
`ProcessedResponseData.BasicProcessedDevices`のsetterが`device.RawValue`プロパティのみを参照していたため、テストデータで`Value`プロパティのみを設定した場合に値が0になっていました。

```csharp
// 修正前（問題のあるコード）
Value = device.RawValue,  // RawValueが0の場合、値が失われる
```

### エラーログの例
```
[WARN] 基本デバイス アドレス '100' が見つかりません。フォールバック値を使用します。
Assert.Equal() Failure: Values differ
Expected: 7117
Actual:   0
```

---

## 実施した修正

### 修正ファイル
`andon/Core/Models/ProcessedResponseData.cs`

### 修正箇所
`BasicProcessedDevices`プロパティのsetter（行138-155）

### 修正内容
`Value`プロパティ（object型）から値を取得し、`RawValue`がゼロの場合のフォールバックを実装しました。

```csharp
// 修正後のコード
// Value プロパティ（object型）から値を取得、RawValueがゼロの場合のフォールバック
uint deviceValue;
if (device.RawValue != 0)
{
    deviceValue = device.RawValue;
}
else if (device.Value is ushort u16)
{
    deviceValue = u16;
}
else if (device.Value is uint u32)
{
    deviceValue = u32;
}
else if (device.Value is int i32)
{
    deviceValue = (uint)i32;
}
else if (device.Value is short i16)
{
    deviceValue = (ushort)i16;
}
else
{
    deviceValue = Convert.ToUInt32(device.Value);
}

var deviceData = new DeviceData
{
    DeviceName = device.DeviceName,
    Code = Enum.Parse<DeviceCode>(device.DeviceType),
    Address = device.Address,
    Value = deviceValue,  // 正しく値が設定される
    IsDWord = false,
    IsHexAddress = false
};
```

### 修正のポイント
1. **型安全な変換**: パターンマッチングを使用して、様々な数値型から安全に変換
2. **フォールバック戦略**: RawValueが設定されている場合はそれを優先、なければValueから取得
3. **互換性維持**: 既存のコードとの互換性を保ちながら、テストデータの柔軟性を向上

---

## 修正結果

### テスト実行結果

#### ParseRawToStructuredData関連（3件）
- ✅ TC037_ParseRawToStructuredData_3Eフレーム解析: **成功**
- ✅ TC038_ParseRawToStructuredData_4Eフレーム解析: **成功**
- ✅ TC038_ParseRawToStructuredData_4E_Binary_実機データ構造化成功: **成功**

#### DeviceCountValidation関連（4件）
- ✅ TC_DeviceCountValidation_001_全て一致_正常ケース: **成功**
- ✅ TC_DeviceCountValidation_002_要求値不一致_警告発生_処理継続: **成功**
- ✅ TC_DeviceCountValidation_003_4Eフレーム_ヘッダ位置検証: **成功**
- ✅ TC_DeviceCountValidation_004_ヘッダ実データ不一致_実データ優先: **成功**

### 全体テスト結果比較

| 項目 | 修正前 | 修正後 | 改善 |
|------|--------|--------|------|
| 総テスト数 | 492 | 502 | +10 |
| 失敗数 | 30 | 17 | **-13** |
| 成功数 | 462 | 485 | +23 |
| 成功率 | 93.9% | 96.6% | **+2.7%** |

---

## 残存課題

修正後も残っている失敗テスト（17件）:

### 1. PlcCommunicationManager統合テスト（8件）
- TC029_ProcessReceivedRawData_基本後処理成功
- TC032_CombineDwordData_DWord結合処理成功
- TC_Step13_001～004: ReadRandom完全サイクル統合テスト（4件）
- TC021_TC025統合_ReadRandom送受信_正常動作
- TC118_Step6_ProcessToCombinetoParse連続処理統合

**原因**: テストデータが簡略化されすぎており、実際のSLMPフレーム構造に準拠していない
**対応**: テストデータを正しいSLMPフレーム形式に修正する必要がある

### 2. ConfigurationLoader関連（5件）
- LoadAllPlcConnectionConfigs関連の統合テスト

**原因**: Excelファイル読み込み処理またはDI統合の問題
**対応**: ConfigurationLoaderとMultiPlcConfigManagerの調査が必要

### 3. 実機通信統合テスト（3件）
- TC116_Step3to5_UDP完全サイクル正常動作
- TC143_10_1, TC143_10_3: ビットデバイス読み取りテスト

**原因**: 上記PlcCommunicationManagerの問題の波及と推測
**対応**: PlcCommunicationManagerの統合テスト修正後に再評価

### 4. DataOutputManager統合テスト（1件）
- TC_INT_006_MultipleOutputs_Integration_Success

**原因**: 新規発見の問題、詳細調査が必要
**対応**: 個別に調査

---

## 技術的な学び

### ProcessedDeviceのValue vs RawValue
- `Value`: object型、汎用的なプロパティ
- `RawValue`: ushort型、ワード値専用

テストコードでは`Value`を設定することが多いため、setterは両方に対応すべき。

### 型変換の重要性
Obsoleteなプロパティのsetterでも、様々な型からの変換に対応することで、テストの柔軟性が向上します。

### フォールバック戦略
複数の型をパターンマッチングで処理し、最終的に`Convert.ToUInt32`で汎用変換を行うことで、堅牢性を確保。

---

## まとめ

**今回の修正で7件のテスト失敗を解決しました。**

- ✅ ParseRawToStructuredData関連: 3件修正完了
- ✅ DeviceCountValidation関連: 4件修正完了
- ✅ 全体成功率: 93.9% → 96.6%（+2.7ポイント）

**次のステップ**:
1. PlcCommunicationManager統合テストのテストデータ修正
2. ConfigurationLoader関連の問題調査
3. 実機通信統合テストの再評価

---

**作成日**: 2025年11月27日
**作成者**: Claude Code Assistant
