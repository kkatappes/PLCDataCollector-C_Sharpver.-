# ExecuteStep3to5CycleAsync メソッド実装記録

## 実装日時
- 開始: 2025-11-06 14:25

## メソッド概要
PlcCommunicationManagerにStep3（接続）→Step4（送信）→Step4（受信）→Step5（切断）の完全サイクルを実行するメソッドを実装

## 実装判断根拠

### 1. なぜこの実装方法を選択したか

#### TCP完全サイクルの統合実装
- **選択**: 単一メソッド内でStep3-5を順次実行し、CycleExecutionResultを返す方式
- **理由**:
  - Phase 2の「連続動作確認」要件を満たすには、各ステップが中断なく連続実行される必要がある
  - 統合テストでは実際の運用シーケンス（接続→通信→切断）を検証することが重要
  - ステップ間の状態管理を一元化し、エラー伝播を適切に制御できる

#### 各ステップ結果の個別保持
- **選択**: ConnectResult, SendResult, ReceiveResult, DisconnectResultを個別プロパティとして保持
- **理由**:
  - 統合テストでは各ステップの成否を詳細に検証する必要がある
  - デバッグ時に失敗したステップを即座に特定できる
  - 統計情報（ステップ成功率等）の算出が容易

### 2. 検討した他の方法との比較

#### 方法A: 既存メソッド個別呼び出し（テスト側で実装）
```csharp
// テスト側でステップを個別呼び出し
var connectResult = await manager.ConnectAsync();
await manager.SendFrameAsync(frame);
var receiveResult = await manager.ReceiveResponseAsync(timeout);
var disconnectResult = await manager.DisconnectAsync();
```

**メリット**:
- PlcCommunicationManagerの変更不要
- 既存メソッドの再利用

**デメリット**:
- テストコードが冗長になる
- ステップ間の状態管理がテスト側に分散
- 統計情報収集が困難
- Phase 2「連続動作確認」の意図が不明確

#### 方法B: 統合メソッド実装（選択した方法）
```csharp
// PlcCommunicationManagerに統合メソッド追加
var cycleResult = await manager.ExecuteStep3to5CycleAsync(
    connectionConfig, timeoutConfig, sendFrame, cancellationToken);
```

**メリット**:
- 完全サイクル実行を明示的にサポート
- ステップ間の状態管理を一元化
- 統計情報（実行時間、成功率等）を自動収集
- Phase 2要件（連続動作確認）を明確に表現
- 将来の本番運用でも利用可能

**デメリット**:
- PlcCommunicationManagerに新規メソッド追加が必要
- 実装の複雑度がやや上がる

**結論**: 方法Bを選択。Phase 2の「連続動作確認」要件を明確に満たし、将来の本番運用での再利用性も高い。

### 3. 技術選択の根拠とトレードオフ

#### 非同期処理（async/await）
- **選択理由**: Socket通信は本質的にI/O待機を伴うため、非同期処理が必須
- **トレードオフ**: 同期処理よりもコード複雑度は上がるが、スケーラビリティとパフォーマンスを優先

#### CancellationToken対応
- **選択理由**: 長時間実行される可能性のある統合テストでは、タイムアウト制御が重要
- **トレードオフ**: パラメータが増えるが、テスト実行時間の制御可能性を優先

#### エラーハンドリング戦略
- **選択**: 各ステップでエラーが発生した場合、CycleExecutionResultにエラー情報を記録し、後続ステップをスキップ
- **理由**:
  - 接続失敗時に送信を試みるのは無意味
  - 各ステップの依存関係を明確にする
  - 部分的な成功情報も記録することで、デバッグが容易
- **トレードオフ**: エラー時の処理が複雑になるが、実用性を優先

#### ログ出力統合
- **選択**: 各ステップの開始・完了・所要時間をログ出力（将来LoggingManager連携）
- **理由**:
  - 統合テストの実行状況をリアルタイムで把握
  - 本番運用時の診断情報として有用
- **トレードオフ**: ログ出力コードが増えるが、保守性を優先

### 4. 発生した問題と解決過程

#### 問題1: （未発生、実装前準備）
- 現在は設計段階。実装開始後に発生した問題はここに記録予定

## TDD実装手順

### Phase 1: Red（テスト失敗）
1. TC115統合テスト実装
2. ExecuteStep3to5CycleAsync未実装によるテスト失敗確認

### Phase 2: Green（最小実装）
1. ExecuteStep3to5CycleAsync最小実装（ハードコーディング）
2. テストがパスすることを確認

### Phase 3: Refactor（リファクタリング）
1. 実際のTCP通信実装
2. エラーハンドリング強化
3. ログ出力追加
4. パフォーマンス最適化

## 実装予定の詳細

### メソッドシグネチャ
```csharp
public async Task<CycleExecutionResult> ExecuteStep3to5CycleAsync(
    ConnectionConfig connectionConfig,
    TimeoutConfig timeoutConfig,
    byte[] sendFrame,
    CancellationToken cancellationToken = default)
```

### 実装フロー
1. CycleExecutionResult初期化
2. 開始時刻記録
3. Step3: ConnectAsync実行 → 結果記録
4. Step4-送信: SendFrameAsync実行 → 結果記録
5. Step4-受信: ReceiveResponseAsync実行 → 結果記録
6. Step5: DisconnectAsync実行 → 結果記録
7. 総実行時間・統計情報更新
8. CycleExecutionResult返却

## 次のステップ
- Phase 1 Red: TC115テスト実装開始
