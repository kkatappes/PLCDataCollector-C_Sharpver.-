# Step6統合処理実装記録

## 実装対象
PlcCommunicationManagerのStep6データ処理における3段階統合処理
- ProcessReceivedRawData（第1段階）
- CombineDwordData（第2段階）
- ParseRawToStructuredData（第3段階）

## 実装判断と設計

### 段階間データ伝達の設計方針
**判断根拠:**
- 各段階で独立したデータモデルを使用することで、責任分離を実現
- 中間データの整合性を検証可能にするため、各段階で明示的なデータ変換を実施
- エラー情報と警告情報を段階間で累積させることで、全体の処理履歴を追跡可能に

**データフロー:**
```
byte[] rawData
  ↓ [Stage 1: ProcessReceivedRawData]
BasicProcessedResponseData (基本処理済みデータ)
  ├─ ProcessedDevices: List<BasicProcessedDevice>
  ├─ ProcessingTimeMs: 処理時間
  ├─ Errors: エラーリスト
  └─ Warnings: 警告リスト
  ↓ [Stage 2: CombineDwordData]
ProcessedResponseData (DWord結合済みデータ)
  ├─ BasicProcessedDevices: 基本デバイスリスト
  ├─ CombinedDWordDevices: 結合済みDWordデバイスリスト
  ├─ ProcessingTimeMs: 累積処理時間
  ├─ Errors: 累積エラーリスト
  └─ Warnings: 累積警告リスト
  ↓ [Stage 3: ParseRawToStructuredData]
StructuredData (構造化データ)
  ├─ StructuredDevices: List<StructuredDevice>
  ├─ ParseSteps: 解析手順記録
  ├─ ProcessingTimeMs: 累積処理時間
  └─ TimestampParsed: 解析完了時刻
```

### DWord結合処理の統合実装方針
**判断根拠:**
- DWord結合はオプション機能のため、ProcessedDeviceRequestInfo.DWordCombineTargetsの有無で判定
- 結合対象外のデバイスは元の値を保持
- 結合されたデバイスは新しいデバイス名（例: "D500D501"）で表現

**実装検討事項:**
1. **結合対象の特定方法:**
   - 検討案A: デバイス番号範囲で判定（例: D500-D999）
   - 検討案B: DWordCombineTargetsリストで明示的に指定
   - **採用: 案B** - より柔軟で明示的、テストも容易

2. **結合済みデバイス数の計算:**
   - 元のデバイス数: 1000
   - 結合対象: 500個（D500-D999）
   - 結合後のDWordデバイス: 250個
   - 結合対象外: 500個（D0-D499）
   - **合計: 750個**

### パフォーマンス考慮事項
**処理時間の計測:**
- 各段階で個別の処理時間を計測
- ProcessingTimeMsは累積値として次段階に引き継ぐ
- テストでは各段階の処理時間を個別に検証可能

**メモリ使用量:**
- 1000デバイスの大容量データでの安定性を確保
- 中間データは必要最小限の情報のみ保持
- リスト操作はLINQを活用して効率化

### エラーハンドリング方針
**スロー例外の定義:**
```csharp
- DataIntegrityException: 段階間データ不整合
- InvalidDataTransformationException: データ変換エラー
- ProcessingSequenceException: 処理順序エラー
```

**エラー伝達方式:**
- 各段階でのエラーはErrorsリストに追加
- 致命的エラーは例外をスロー
- 警告レベルのエラーはWarningsリストに追加して処理継続

## 発生した問題と解決過程
（実装中に記録）

## 技術選択の根拠とトレードオフ
（実装中に記録）

## テスト戦略
1. 単一段階テスト: 各段階の単体テストは別途実施済み
2. 統合テスト: TC119で3段階のデータ伝達整合性を検証
3. データパターン:
   - パターン1: M000-M999（ビット型、結合なし）
   - パターン2: D000-D999（ワード型、DWord結合あり）

---
実装詳細は以下に随時記録
