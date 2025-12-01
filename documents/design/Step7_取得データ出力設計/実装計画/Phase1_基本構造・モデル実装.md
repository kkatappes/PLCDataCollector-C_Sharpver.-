# Phase1: 基本構造・モデル実装計画

## 概要
Phase1では、DataOutputManagerの基本構造とデータモデルを実装します。既存実装があるため、主に**インターフェース定義の追加**と**DeviceEntryInfoの移動**を行います。

---

## 実装対象

### 1. IDataOutputManagerインターフェース定義

#### 実装場所
- **ファイル**: `andon/Core/Interfaces/IDataOutputManager.cs`
- **現在の状態**: 空のインターフェース（TODO状態）

#### 実装内容

```csharp
public interface IDataOutputManager
{
    /// <summary>
    /// JSON形式でデータを出力します
    /// </summary>
    /// <param name="data">出力するデバイスデータ</param>
    /// <param name="outputDirectory">出力先ディレクトリパス</param>
    /// <param name="ipAddress">PLC IPアドレス</param>
    /// <param name="port">PLCポート番号</param>
    /// <param name="deviceConfig">デバイス設定情報（名前・桁数）</param>
    void OutputToJson(
        ProcessedResponseData data,
        string outputDirectory,
        string ipAddress,
        int port,
        Dictionary<string, DeviceEntryInfo> deviceConfig);

    /// <summary>
    /// JSON形式でデータを非同期出力します（将来実装）
    /// </summary>
    Task OutputToJsonAsync(
        ProcessedResponseData data,
        string outputDirectory,
        string ipAddress,
        int port,
        Dictionary<string, DeviceEntryInfo> deviceConfig);
}
```

**実装のポイント**:
- 既存の`OutputToJson()`メソッドに合わせたシグネチャ
- 非同期版`OutputToJsonAsync()`は将来実装用（Phase1では未実装でOK）
- XMLドキュメントコメントを追加

---

### 2. DeviceEntryInfoの移動（オプション）

#### 現在の配置
- **ファイル**: `andon/Core/Managers/DataOutputManager.cs`（行: 96-109）

#### 移動先
- **ファイル**: `andon/Core/Models/ConfigModels/DeviceEntryInfo.cs`

#### 移動理由
- 設定モデルとして他のConfigModelsと同じフォルダに配置
- DataOutputManagerファイルのスコープを狭める
- 将来的な拡張性向上

#### 実装内容

**新規ファイル**: `andon/Core/Models/ConfigModels/DeviceEntryInfo.cs`

```csharp
namespace Andon.Core.Models.ConfigModels
{
    /// <summary>
    /// デバイス設定情報（JSON出力用）
    /// </summary>
    public class DeviceEntryInfo
    {
        /// <summary>
        /// デバイスの説明名（センサー名・用途説明）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// データ表示桁数
        /// </summary>
        public int Digits { get; set; } = 1;
    }
}
```

**DataOutputManager.cs内の削除**:
- 行96-109の`DeviceEntryInfo`クラス定義を削除
- `using Andon.Core.Models.ConfigModels;`を追加

**注意事項**:
- Phase1では**オプション**実装（優先度: 低）
- 移動後は既存のDataOutputManagerのテストが通ることを確認
- 他のファイルで`DeviceEntryInfo`を参照している箇所がないか確認

---

### 3. DataOutputManagerの確認

#### 実装場所
- **ファイル**: `andon/Core/Managers/DataOutputManager.cs`

#### 確認事項
- 既存の`OutputToJson()`メソッドが正常動作していること
- 実装箇所: 行23-76
- `ConvertValue()`プライベートメソッドが正常動作していること（行81-90）

#### 既存実装の特徴
1. **入力型**: `ProcessedResponseData`
2. **ファイル名形式**: `yyyyMMdd_HHmmssfff_xxx-xxx-x-xx_zzzz.json`
3. **JSON構造**:
   - `source`: plcModel, ipAddress, port
   - `timestamp`: ISO 8601形式（タイムゾーン付き）
   - `items`: デバイスデータ配列

#### 既存実装の問題点（Phase2以降で対応）
- ビットデバイス16ビット分割処理が未実装
- device.numberが3桁ゼロ埋めされていない
- エラーハンドリングが未実装

---

## 実装手順

### Step 1: IDataOutputManagerインターフェース定義
1. `andon/Core/Interfaces/IDataOutputManager.cs`を開く
2. `OutputToJson()`メソッドのシグネチャを追加
3. XMLドキュメントコメントを記載
4. （オプション）`OutputToJsonAsync()`メソッドのシグネチャを追加

### Step 2: DataOutputManagerにインターフェース実装を明示
1. `andon/Core/Managers/DataOutputManager.cs`を開く
2. クラス定義に`: IDataOutputManager`を追加
   ```csharp
   public class DataOutputManager : IDataOutputManager
   ```

### Step 3: DeviceEntryInfoの移動（オプション）
1. 新規ファイル作成: `andon/Core/Models/ConfigModels/DeviceEntryInfo.cs`
2. `DataOutputManager.cs`から`DeviceEntryInfo`クラスをコピー
3. 名前空間を`Andon.Core.Models.ConfigModels`に変更
4. `DataOutputManager.cs`に`using Andon.Core.Models.ConfigModels;`を追加
5. `DataOutputManager.cs`内の`DeviceEntryInfo`クラス定義を削除

### Step 4: ビルド確認
1. `dotnet build`を実行
2. コンパイルエラーがないことを確認

### Step 5: 既存テストの実行
1. `DataOutputManagerTests`が存在する場合は実行
2. すべてのテストがパスすることを確認

---

## テスト要件

### Phase1のテスト
Phase1では**新規のテスト実装は不要**です。既存テストのパス確認のみ行います。

#### 確認項目
- [ ] `dotnet build`が成功すること
- [ ] 既存の`DataOutputManagerTests`がパスすること（存在する場合）
- [ ] インターフェース実装が正常に認識されること

---

## 完了条件

### 必須項目
- [x] `IDataOutputManager`インターフェースに`OutputToJson()`メソッドが定義されている
- [x] `DataOutputManager`が`IDataOutputManager`を実装している
- [x] `dotnet build`が成功する
- [x] 既存テストがすべてパスする（6テスト合格）

### オプション項目
- [x] `DeviceEntryInfo`が`ConfigModels`フォルダに移動されている
- [ ] `IDataOutputManager`に`OutputToJsonAsync()`メソッドが定義されている（Phase2以降で実装）

---

## 次のPhaseへの準備

Phase2では、既存の`OutputToJson()`メソッドに以下の修正を加えます:
1. device.numberの3桁ゼロ埋めフォーマット
2. JSON構造の整理
3. ログ出力の追加

Phase1完了後、Phase2実装計画を参照してください。

---

## 参照文書
- `実装ガイド.md`: 全体的な実装仕様
- `実装時対応関係.md`: 既存実装との連携方法

## 作成日時
- **作成日**: 2025年11月27日
- **対象Phase**: Phase1（基本構造・モデル実装）

---

## 実装状況

### 実装完了日時
- **実装完了日**: 2025年11月27日
- **実装時間**: 約30分

### 実装結果
- **ステータス**: ✅ 完了
- **ビルド結果**: 成功（警告のみ、エラー0件）
- **テスト結果**: 6テスト合格、失敗0件
- **実装結果詳細**: `documents/design/Step7_取得データ出力設計/実装結果/Phase1_基本構造・モデル実装結果.md`
