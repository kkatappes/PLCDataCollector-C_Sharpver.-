# Phase6: 設定ファイル構造の変更

## ステータス
✅ **完了** - 2025-11-21

## 概要
appsettings.jsonの構造を変更し、デバイス指定方式を「リスト指定」に変更します。

**注意**: readコマンド(0x0401)は廃止されました。本システムはread_randomコマンド(0x0403)のみをサポートします。

**Phase4仕様変更(2025-11-20)との整合性**:
- Random READで全デバイス（ビット/ワード/ダブルワード混在）を一括取得
- 通信回数: 1回で完結（送受信各1回）
- DeviceDataクラスによるデバイス名キー構造（"M000", "D000", "D002"）

## 前提条件
- ✅ Phase2完了: SlmpFrameBuilder.BuildReadRandomRequest()実装済み（ビット/ワード/ダブルワード混在対応）
- ✅ Phase4完了: ConfigToFrameManager実装済み、PlcCommunicationManager統合済み
- ⏳ Phase5未完了: ReadRandomレスポンスパース実装予定

## 実装ステップ

### ステップ17: appsettings.jsonの更新

#### 設定構造

```json
{
  "PlcConnection": {
    "FrameVersion": "3E",
    "Timeout": 8000,
    "Devices": [
      {
        "DeviceType": "D",
        "DeviceNumber": 100,
        "Description": "生産数カウンタ"
      },
      {
        "DeviceType": "D",
        "DeviceNumber": 105,
        "Description": "エラーカウンタ"
      },
      {
        "DeviceType": "M",
        "DeviceNumber": 200,
        "Description": "運転状態フラグ"
      },
      {
        "DeviceType": "W",
        "DeviceNumber": 4522,
        "IsHexAddress": true,
        "Description": "通信ステータス（W0x11AA）"
      }
    ]
  }
}
```

**利点**:
- ✅ 飛び飛びのデバイスを指定可能
- ✅ 異なるデバイス種別（D, M, W等）を混在可能
- ✅ デバイスごとに説明を記載可能（可読性向上）
- ✅ 16進アドレスデバイス（W, X, Y等）に対応

#### 変化点
- **変更前**: TargetDevicesで範囲指定（MDeviceRange/DDeviceRange: Start-End形式）
- **変更後**: リスト指定形式（Devicesリスト形式）を採用
  - 飛び飛びのデバイス指定が可能
  - 異なるデバイス種別（M, D, W等）の混在が可能
  - ビット・ワード・ダブルワード混在指定が可能（Phase4仕様変更対応）

---

### ステップ18: ConfigurationLoaderの修正

#### 実装対象
`andon/Infrastructure/Configuration/ConfigurationLoader.cs`

#### 実装内容

1. **Devicesリストの読み込み**
   - Devicesリストの読み込み
   - DeviceEntryオブジェクトの構築

2. **検証ロジック追加**
   - Devicesリストが空でないこと
   - DeviceTypeが有効であること
   - DeviceNumberが範囲内であること

#### 実装コード（実装完了）

実装場所: `andon/Infrastructure/Configuration/ConfigurationLoader.cs`

**主な機能**:
1. **Devicesリストの読み込み**
   - DeviceEntry型で読み込み
   - DeviceSpecificationに変換して格納
   - 16進アドレスデバイス（IsHexAddress）対応

2. **設定パスの調整**
   - `PlcCommunication:Connection:FrameVersion`
   - `PlcCommunication:Timeouts:ReceiveTimeoutMs`（SLMPタイムアウトに自動変換: ms / 250）
   - `PlcCommunication:TargetDevices:Devices`

3. **検証ロジック**
   - デバイスリスト空チェック
   - 255点上限チェック
   - フレームタイプ検証（"3E" or "4E"）
   - 各デバイスのReadRandom対応チェック（`ValidateForReadRandom()`）
   - デバイス番号範囲チェック（`ValidateDeviceNumberRange()`）

**DeviceEntry型**:
- 設定ファイルから読み込む中間型
- `ToDeviceSpecification()`メソッドでDeviceSpecificationに変換
- 実装場所: `andon/Core/Models/ConfigModels/DeviceEntry.cs`

#### 変化点
- **変更前**: TODO状態、設定読み込み機能なし
- **変更後**: Devicesリスト対応完了（2025-11-21）
  - DeviceEntry → DeviceSpecification変換
  - Phase4仕様変更対応（ビット/ワード/ダブルワード混在）
  - 厳密なバリデーション実装

---

### ステップ19: 設定ファイルのバリデーションテスト

#### 実装対象
`andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs`

#### テスト内容

1. **Devicesリストの読み込みテスト**
   - Devicesリストが正しく読み込まれること
   - DeviceTypeが正しく解析されること
   - IsHexAddressが正しく読み込まれること
   - Descriptionが正しく読み込まれること

2. **検証テスト**
   - 空のDevicesリストで例外がスローされること
   - 256点以上で例外がスローされること
   - 不正なDeviceTypeで例外がスローされること

#### テストコード（実装完了）

実装場所: `andon/Tests/Unit/Infrastructure/Configuration/ConfigurationLoaderTests.cs`

**テストケース一覧**（全8テスト）:
1. **TC_Step19_001**: Devicesリストの読み込みテスト（正常系）
   - D100, M200の2デバイス読み込み検証
   - FrameType, Timeout, DeviceCode, DeviceNumber検証

2. **TC_Step19_002**: 16進アドレスデバイスの読み込みテスト
   - W4522 (0x11AA)の読み込み検証
   - IsHexAddress=trueの検証

3. **TC_Step19_003**: 空のDevicesリストで例外がスローされること
   - InvalidOperationException検証
   - エラーメッセージ確認

4. **TC_Step19_004**: 256点以上で例外がスローされること
   - 256デバイス登録→例外検証
   - 上限255点チェック

5. **TC_Step19_005**: 不正なDeviceTypeで例外がスローされること
   - DeviceType="INVALID"→ArgumentException検証

6. **TC_Step19_006**: 不正なフレームタイプで例外がスローされること
   - FrameVersion="5E"→InvalidOperationException検証

7. **TC_Step19_007**: 混在デバイスタイプの読み込みテスト
   - M, D, W混在の3デバイス読み込み検証
   - Phase4仕様変更対応確認

8. **TC_Step19_008**: デフォルト値のテスト
   - FrameVersion未指定→"4E"デフォルト
   - Timeout未指定→32デフォルト（8000ms / 250）

---

## 完了条件
- ✅ appsettings.jsonが新形式に更新済み（2025-11-21）
  - Devicesリスト形式採用
  - M, D, W混在デバイス7点登録
  - 16進アドレスデバイス（W4522）対応
- ✅ DeviceEntry型実装完了（2025-11-21）
  - 設定ファイル→DeviceSpecification変換
  - `andon/Core/Models/ConfigModels/DeviceEntry.cs`
- ✅ ConfigurationLoader.LoadPlcConnectionConfig()実装完了（2025-11-21）
  - Devicesリスト読み込み
  - DeviceEntry→DeviceSpecification変換
  - 厳密なバリデーション（ReadRandom対応、デバイス番号範囲）
- ✅ ValidateConfig()実装完了（2025-11-21）
  - デバイスリスト空チェック
  - 255点上限チェック
  - フレームタイプ検証
  - 各デバイスの詳細検証
- ✅ ConfigurationLoaderTests全8テスト実装完了（2025-11-21）
  - 正常系: 通常デバイス、16進デバイス、混在デバイス、デフォルト値
  - 異常系: 空リスト、上限超過、不正DeviceType、不正FrameType

## 次フェーズへの依存関係
- **Phase5（レスポンス処理の修正）**: ReadRandomレスポンスパース実装
  - DeviceDataクラス実装（デバイス名キー構造: "M000", "D000", "D002"）
  - ビット・ワード・ダブルワード混在データのパース
  - DWordDeviceCountの動的算出
- **Phase7（データ出力処理の修正）**: 不連続デバイスのCSV出力対応
  - DeviceDataクラスからのCSV出力
  - 飛び飛びデバイスの適切な出力
- **Phase8（統合テスト）**: 設定ファイルからの一連フロー検証
  - 設定読み込み→フレーム構築→送信→受信→パース→出力
  - Phase4仕様変更（1回通信）の動作確認

## リスク管理
| リスク | 影響 | 対策 |
|--------|------|------|
| **設定ファイル解析エラー** | 中 | ・詳細な検証ロジック実装<br>・エラーメッセージの充実 |
| **設定ファイルの肥大化** | 低 | ・外部ファイルへの分割も検討可能 |

---

## Phase4仕様変更(2025-11-20)との整合性まとめ

### Phase6での対応状況

#### ✅ 対応済み項目

1. **デバイス混在指定への対応**
   - appsettings.jsonでM, D, W等を混在指定可能
   - DeviceEntry → DeviceSpecification変換で全デバイス種別対応
   - ConfigurationLoaderTestsで混在デバイステスト実装（TC_Step19_007）

2. **1回通信フローとの整合性**
   - Devicesリスト全体を`TargetDeviceConfig.Devices`として保持
   - ConfigToFrameManager（Phase4実装）で全デバイスを単一フレームに変換
   - Random READ(0x0403)で一括取得

3. **ビットデバイス対応**
   - DeviceCode.Mなどビットデバイスもリスト指定可能
   - Phase2のSlmpFrameBuilderがビット/ワード/ダブルワード混在対応済み
   - 16点=1ワード換算ロジック不要（Random READが自動対応）

4. **型設計の整合性**
   - TargetDeviceConfig.DevicesがList<DeviceSpecification>として統一
   - Phase5で実装予定のDeviceDataクラスとの連携準備完了

#### 📝 Phase5への引き継ぎ事項

以下はPhase5（レスポンス処理）で実装予定:
1. DeviceDataクラスの実装（デバイス名キー: "M000", "D000", "D002"）
2. ProcessReceivedRawData()での処理完結
3. DWordDeviceCountの動的算出（OriginalRequestから）
4. ビット・ワード・ダブルワード混在データのパース

### Phase6とPhase4の関係性

```
Phase6 (設定読み込み)
  ↓ appsettings.json
  ↓ Devicesリスト形式
  ↓ ConfigurationLoader
  ↓ TargetDeviceConfig.Devices (List<DeviceSpecification>)
  ↓
Phase4 (フレーム構築・通信)
  ↓ ConfigToFrameManager
  ↓ SlmpFrameBuilder.BuildReadRandomRequest()
  ↓ Random READ(0x0403)フレーム
  ↓ 1回の送受信で全デバイス一括取得
  ↓
Phase5 (レスポンス処理)
  ↓ SlmpDataParser.ParseReadRandomResponse()
  ↓ DeviceData (デバイス名キー構造)
  ↓ ビット・ワード・ダブルワード混在パース
```

**結論**: Phase6はPhase4仕様変更(2025-11-20)に**完全に対応**しています。

---

**作成日**: 2025-11-18
**最終更新**: 2025-11-21（Phase4仕様変更対応、実装完了）
**元ドキュメント**: read_to_readrandom_migration_plan.md
