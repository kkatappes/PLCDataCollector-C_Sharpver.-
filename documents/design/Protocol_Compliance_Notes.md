# プロトコル準拠仕様の差分ノート

## SLMP仕様参照と実装対応

### 参照仕様書
- **CC-Link IE協会 SLMP仕様書** Version 1.12 (2019年3月)
- **三菱電機 QnAS/Q4AR/R/Q/L/FXseries プログラミングマニュアル（通信編）**
- **CC-Link IE協会 通信プロトコル仕様書**

### 実装対応レベル

| 仕様分類 | 対応状況 | 対応範囲 | 備考 |
|----------|----------|----------|------|
| **基本フレーム構成** | ✅ 完全対応 | 3E/4E Binary/ASCII | - |
| **デバイスアクセス** | ✅ 完全対応 | 全デバイスタイプ | - |
| **ランダムアクセス** | ✅ 完全対応 | Word/DWord混合 | - |
| **ブロックアクセス** | ✅ 完全対応 | Word/Bit混合 | - |
| **モニタ機能** | ✅ 完全対応 | 登録/実行 | - |
| **メモリアクセス** | ✅ 完全対応 | 直接メモリ操作 | ネットワーク=0, ノード=255のみ |
| **ファイルアクセス** | ❌ 未対応 | - | 将来実装予定 |
| **リモート制御** | 🔄 部分対応 | RUN/STOP以外 | 一部コマンドのみ |
| **診断機能** | 🔄 部分対応 | 基本機能のみ | 拡張診断は未対応 |

## フレーム仕様準拠詳細

### 3Eフレーム（ST型）

#### 要求フレーム（Binary）
```
+--------+--------+--------+--------+--------+--------+--------+--------+
| サブヘッダ | ネットワーク | PC番号  | 要求先IO番号    | 要求先局番 |
|   (2)    |    (1)    |   (1)   |      (2)      |    (1)   |
+--------+--------+--------+--------+--------+--------+--------+--------+
| データ長       | 監視タイマ     | コマンド       | サブコマンド   |
|    (2)       |    (2)       |    (2)       |    (2)       |
+--------+--------+--------+--------+--------+--------+--------+--------+
| データ...                                                           |
+--------------------------------------------------------------------+
```

**実装状況**: ✅ **完全対応**
- サブヘッダ: `0x5000` 固定
- 全フィールドの適切なエンディアン変換
- データ長の自動計算

#### 応答フレーム（Binary）
```
+--------+--------+--------+--------+--------+--------+--------+--------+
| サブヘッダ | ネットワーク | PC番号  | 応答元IO番号    | 応答元局番 |
|   (2)    |    (1)    |   (1)   |      (2)      |    (1)   |
+--------+--------+--------+--------+--------+--------+--------+--------+
| データ長       | 終了コード     | データ...                       |
|    (2)       |    (2)       |                               |
+--------+--------+--------+--------+--------+--------+--------+--------+
```

**実装状況**: ✅ **完全対応**
- サブヘッダ: `0xD000` の検証
- 終了コードの適切な解釈
- データ部の正確な抽出

### 4Eフレーム（MT型）

#### 要求フレーム（Binary）
```
+--------+--------+--------+--------+--------+--------+--------+--------+
| サブヘッダ | シリアル      | 固定値         | ネットワーク | PC番号  |
|   (2)    |    (2)      |    (2)       |    (1)     |   (1)   |
+--------+--------+--------+--------+--------+--------+--------+--------+
| 要求先IO番号    | 要求先局番 | データ長       | 監視タイマ     |
|      (2)      |    (1)   |    (2)       |    (2)       |
+--------+--------+--------+--------+--------+--------+--------+--------+
| コマンド       | サブコマンド   | データ...                       |
|    (2)       |    (2)       |                               |
+--------+--------+--------+--------+--------+--------+--------+--------+
```

**実装状況**: ✅ **完全対応**
- サブヘッダ: `0x5400` 固定
- シリアル番号の自動管理（0-255の循環）
- 固定値: `0x0000` の設定

#### 応答フレーム（Binary）
```
+--------+--------+--------+--------+--------+--------+--------+--------+
| サブヘッダ | シリアル      | 固定値         | ネットワーク | PC番号  |
|   (2)    |    (2)      |    (2)       |    (1)     |   (1)   |
+--------+--------+--------+--------+--------+--------+--------+--------+
| 応答元IO番号    | 応答元局番 | データ長       | 終了コード     |
|      (2)      |    (1)   |    (2)       |    (2)       |
+--------+--------+--------+--------+--------+--------+--------+--------+
| データ...                                                           |
+--------------------------------------------------------------------+
```

**実装状況**: ✅ **完全対応**  
- シリアル番号の照合による応答マッチング
- 非同期応答の適切な処理

## デバイスコード対応表

### 標準デバイス

| デバイス名 | コード(Hex) | アドレス表現 | 実装状況 | 備考 |
|------------|-------------|--------------|----------|------|
| **SM** | 0x91 | 10進 | ✅ | 特殊リレー |
| **SD** | 0xA9 | 10進 | ✅ | 特殊レジスタ |
| **X** | 0x9C | 16進 | ✅ | 入力リレー |
| **Y** | 0x9D | 16進 | ✅ | 出力リレー |
| **M** | 0x90 | 10進 | ✅ | 内部リレー |
| **L** | 0x92 | 10進 | ✅ | ラッチリレー |
| **F** | 0x93 | 10進 | ✅ | アナンシエータ |
| **V** | 0x94 | 10進 | ✅ | エッジリレー |
| **B** | 0xA0 | 16進 | ✅ | リンクリレー |
| **D** | 0xA8 | 10進 | ✅ | データレジスタ |
| **W** | 0xB4 | 16進 | ✅ | リンクレジスタ |

### タイマー・カウンタ

| デバイス名 | コード(Hex) | アドレス表現 | 実装状況 | 備考 |
|------------|-------------|--------------|----------|------|
| **TS** | 0xC1 | 10進 | ✅ | タイマ接点 |
| **TC** | 0xC0 | 10進 | ✅ | タイマコイル |
| **TN** | 0xC2 | 10進 | ✅ | タイマ現在値 |
| **LTS** | 0x51 | 10進 | ✅ | 長時間タイマ接点 |
| **LTC** | 0x50 | 10進 | ✅ | 長時間タイマコイル |
| **LTN** | 0x52 | 10進 | ✅ | 長時間タイマ現在値 |
| **SS** | 0xC7 | 10進 | ✅ | 積算タイマ接点 |
| **SC** | 0xC6 | 10進 | ✅ | 積算タイマコイル |
| **SN** | 0xC8 | 10進 | ✅ | 積算タイマ現在値 |
| **CS** | 0xC4 | 10進 | ✅ | カウンタ接点 |
| **CC** | 0xC3 | 10進 | ✅ | カウンタコイル |
| **CN** | 0xC5 | 10進 | ✅ | カウンタ現在値 |

### 特殊デバイス

| デバイス名 | コード(Hex) | アドレス表現 | 実装状況 | 制限事項 |
|------------|-------------|--------------|----------|----------|
| **SB** | 0xA1 | 16進 | ✅ | リンク特殊リレー |
| **SW** | 0xB5 | 16進 | ✅ | リンク特殊レジスタ |
| **DX** | 0xA2 | 16進 | ✅ | 直接入力 |
| **DY** | 0xA3 | 16進 | ✅ | 直接出力 |
| **Z** | 0xCC | 10進 | ✅ | インデックスレジスタ |
| **LZ** | 0x62 | 10進 | ✅ | 長時間インデックス |
| **R** | 0xAF | 10進 | ✅ | ファイルレジスタ |
| **ZR** | 0xB0 | 16進 | ✅ | 拡張ファイルレジスタ |
| **RD** | 0x2C | 10進 | ✅ | 拡張データレジスタ |

## コマンド実装状況

### デバイスアクセス系

| コマンド名 | コード(Hex) | 実装状況 | テスト状況 | 備考 |
|------------|-------------|----------|------------|------|
| **Device_Read** | 0x0401 | ✅ 完了 | ✅ 完了 | ビット・ワード読み取り |
| **Device_Write** | 0x1401 | ✅ 完了 | ✅ 完了 | ビット・ワード書き込み |
| **Device_ReadRandom** | 0x0403 | ✅ 完了 | ✅ 完了 | ランダム読み取り |
| **Device_WriteRandom** | 0x1402 | ✅ 完了 | ✅ 完了 | ランダム書き込み |
| **Device_EntryMonitorDevice** | 0x0801 | ✅ 完了 | ✅ 完了 | モニタ登録 |
| **Device_ExecuteMonitor** | 0x0802 | ✅ 完了 | ✅ 完了 | モニタ実行 |
| **Device_ReadBlock** | 0x0406 | ✅ 完了 | ✅ 完了 | ブロック読み取り |
| **Device_WriteBlock** | 0x1406 | ✅ 完了 | ✅ 完了 | ブロック書き込み |

### システム系

| コマンド名 | コード(Hex) | 実装状況 | テスト状況 | 備考 |
|------------|-------------|----------|------------|------|
| **RemoteControl_ReadTypeName** | 0x0101 | ✅ 完了 | ✅ 完了 | 型名読み取り |
| **SelfTest** | 0x0619 | ✅ 完了 | ✅ 完了 | セルフテスト |
| **ClearError** | 0x1617 | ✅ 完了 | ✅ 完了 | エラークリア |
| **Memory_Read** | 0x0613 | ✅ 完了 | ✅ 完了 | メモリ読み取り |
| **Memory_Write** | 0x1613 | ✅ 完了 | ✅ 完了 | メモリ書き込み |

### 未実装コマンド

| コマンド名 | コード(Hex) | 実装予定 | 優先度 | 理由 |
|------------|-------------|----------|-------|------|
| **File_Read** | 0x0206 | 🔄 Phase4 | 低 | ファイル操作は使用頻度低 |
| **File_Write** | 0x1203 | 🔄 Phase4 | 低 | 同上 |
| **RemoteControl_RemoteRun** | 0x1001 | 🔄 Phase3 | 中 | セキュリティ考慮必要 |
| **RemoteControl_RemoteStop** | 0x1002 | 🔄 Phase3 | 中 | 同上 |
| **Drive_ReadDiskState** | 0x0205 | ❌ 未定 | 低 | 特殊用途 |

## 仕様曖昧点と実装判断

### 1. エンディアン処理

**仕様の曖昧さ**:
- 数値データのバイト順序について明示的な記載が一部不足
- アドレス指定での16進/10進混在時の解釈

**実装判断**:
```csharp
// リトルエンディアンで統一（SLMP仕様書記載通り）
public ushort ReadUInt16(ReadOnlySpan<byte> buffer, int offset)
{
    return BinaryPrimitives.ReadUInt16LittleEndian(buffer[offset..]);
}

// 16進デバイスのアドレス処理
public string FormatDeviceAddress(DeviceCode deviceCode, uint address)
{
    if (deviceCode.IsHexAddress())
        return $"{deviceCode}*{address:X6}";  // 16進表記
    else
        return $"{deviceCode}*{address:D6}";  // 10進表記
}
```

### 2. タイムアウト処理

**仕様の曖昧さ**:
- 監視タイマの単位（250ms）は明記されているが、0指定時の動作が不明
- ネットワーク遅延とPLC処理時間の区別が不明確

**実装判断**:
```csharp
public class SlmpConnectionSettings
{
    // 0指定時はデフォルト値を使用（仕様書に従い、無制限ではない）
    public ushort DefaultCommandTimeout { get; set; } = 20; // 5秒
    
    // ネットワークレベルとアプリケーションレベルを分離
    public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
```

### 3. エラーコード解釈

**仕様の曖昧さ**:
- 一部のエラーコードで複数の原因が考えられる場合の判別方法が不明
- ベンダー固有エラーコードの扱い

**実装判断**:
```csharp
public static class EndCodeExtensions
{
    // 標準エラーコードのみ定義済み列挙型で処理
    // 未知のエラーコードは数値のまま情報提供
    public static string GetDetailedMessage(this EndCode endCode, SlmpCommand? command = null)
    {
        return endCode switch
        {
            EndCode.WrongCommand when command.HasValue => 
                $"Command {command} is not supported by this PLC",
            EndCode.WrongCommand => 
                "Command is not supported by this PLC",
            // ...
            _ when (int)endCode >= 0xF000 => 
                $"Vendor-specific error: 0x{(int)endCode:X4}",
            _ => 
                $"Unknown error code: 0x{(int)endCode:X4}"
        };
    }
}
```

### 4. デバイス範囲チェック

**仕様の曖昧さ**:
- PLCシリーズごとのデバイス範囲が異なるが、統一的な範囲チェック方法が不明
- アドレス上限の動的取得方法が仕様に記載なし

**実装判断**:
```csharp
public class DeviceAddressValidator
{
    // 保守的な範囲チェック（最も制限が厳しいPLCに合わせる）
    private static readonly Dictionary<DeviceCode, uint> MaxAddresses = new()
    {
        [DeviceCode.D] = 0xFFFF,    // 65535（Q00J等の制限）
        [DeviceCode.M] = 0x1FFF,    // 8191（同上）
        [DeviceCode.X] = 0x1FFF,    // 8191（16進表記）
        [DeviceCode.Y] = 0x1FFF,    // 8191（同上）
        // ...
    };
    
    public void ValidateAddress(DeviceCode deviceCode, uint address, ushort count)
    {
        if (MaxAddresses.TryGetValue(deviceCode, out var maxAddr))
        {
            if (address + count - 1 > maxAddr)
                throw new ArgumentOutOfRangeException(nameof(address), 
                    $"Address range {address}-{address + count - 1} exceeds maximum {maxAddr} for device {deviceCode}");
        }
    }
}
```

## ベンダー差異と対応

### 三菱電機 Q シリーズ

**特徴**:
- 標準SLMP仕様に完全準拠
- 全コマンド対応
- 高速応答

**実装での特別考慮**: なし（標準実装で対応）

### 三菱電機 L シリーズ

**差異**:
- 一部高度なコマンドが未対応
- メモリアクセスの制限が厳しい

**実装での対応**:
```csharp
public class LSeriesPlcProfile : IPlcProfile
{
    public bool SupportsMemoryAccess => false;
    public ushort MaxDeviceReadCount => 960; // Qシリーズより少ない
    
    public void ValidateCommand(SlmpCommand command)
    {
        var unsupportedCommands = new[]
        {
            SlmpCommand.ExtendUnit_Read,
            SlmpCommand.ExtendUnit_Write,
            // ...
        };
        
        if (unsupportedCommands.Contains(command))
            throw new SlmpCommunicationException(EndCode.WrongCommand);
    }
}
```

### 他社SLMP対応機器

**一般的な差異**:
- エラーコードの一部が異なる
- タイムアウト処理の実装差
- サポートするフレーム形式の制限

**実装での対応**:
```csharp
public class SlmpCompatibilityMode
{
    public bool StrictErrorCodeChecking { get; set; } = false;
    public bool AllowUnknownEndCodes { get; set; } = true;
    public TimeSpan AdditionalTimeout { get; set; } = TimeSpan.Zero;
    
    public void ApplyCompatibilitySettings(SlmpConnectionSettings settings)
    {
        if (!StrictErrorCodeChecking)
        {
            // 未知のエラーコードを警告として処理
            settings.TreatUnknownErrorsAsWarnings = true;
        }
        
        settings.ReceiveTimeout = settings.ReceiveTimeout.Add(AdditionalTimeout);
    }
}
```

## 将来課題（TODO）

### Phase 3 実装予定

| 機能 | 影響範囲 | 実装難易度 | 備考 |
|------|----------|------------|------|
| **ファイルアクセス** | 中 | 高 | セキュリティ考慮必要 |
| **リモート制御拡張** | 中 | 中 | RUN/STOP/PAUSE等 |
| **診断機能拡張** | 小 | 中 | エラー履歴等 |
| **パラメータ設定** | 大 | 高 | 設定変更機能 |

### Phase 4 検討項目

| 機能 | 優先度 | 検討事項 |
|------|--------|----------|
| **CC-Link IE TSN対応** | 低 | 新しい通信仕様への対応 |
| **セキュリティ機能** | 中 | 暗号化通信、認証機能 |
| **高速化機能** | 中 | UDP Multicast等 |
| **冗長化機能** | 低 | 複数PLC同時接続 |

### 検証が必要な項目

1. **大容量データ転送** (3000点以上)
   - メモリ使用量の最適化
   - タイムアウト値の調整

2. **高頻度通信** (1000回/秒以上)
   - パフォーマンス影響の確認
   - リソースリークの確認

3. **長時間連続運用** (24時間以上)
   - メモリリークの確認
   - 接続安定性の確認

このプロトコル準拠仕様により、SLMP標準に準拠した実装の品質と互換性を保証できます。