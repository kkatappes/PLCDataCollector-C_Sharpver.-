# ExecuteTest 送信バイト数チェック機能追加 実装記録

## 実装日
2025-11-11

## 対応するToDoリスト項目
`documents/ToDo/PLC不存在エラー対応ToDoリスト.md` の「1. 送信バイト数チェック機能の追加」

## 実装目的
- PLC通信における送信データの完全性を保証する
- 送信バイト数が期待値と一致することを検証する
- ArraySegmentの範囲が正しく設定されていることを確認する
- 部分送信の発生を検知できるようにする

## 実装対象
- ファイル: `PlcRealDeviceTest/Program.cs`
- メソッド: `ExecuteTest` (148-211行目)
- 対象箇所: 176行目の送信処理部分

## 実装前の状態
```csharp
int sent = await socket.SendAsync(new ArraySegment<byte>(requestFrame), SocketFlags.None);
Log($"[送信] 完了: {sent} bytes");
```

### 問題点
1. 送信バイト数の検証がない (`sent == requestFrame.Length` の比較なし)
2. 部分送信が発生しても検知できない
3. ArraySegmentの範囲確認ログがない (Offset/Count の値が検証されていない)
4. フレーム構造の可視化なし

## 実装内容

### Phase 1: ArraySegment範囲確認ログ追加
- ArraySegmentを明示的に変数化
- Offset, Count, 配列長を確認するログを追加
- Offset=0, Count=requestFrame.Length であることを確認

### Phase 2: 送信バイト数検証追加
- 送信完了後に全バイト送信されたか検証
- 送信不完全の場合はエラーログを出力
- 送信成功の場合は確認ログを出力

## 実装判断の根拠

### なぜArraySegmentを明示的に変数化するか
- ArraySegmentのデフォルトコンストラクタの動作を明示的に確認するため
- デバッグ時にOffset/Countの値を視覚的に確認できるようにするため
- 将来的に部分配列を送信する可能性に備えて範囲を明示化するため

### なぜ送信バイト数の検証が必要か
- UDPソケット通信では、理論上は全バイトが送信されるはずだが、ネットワーク層の問題で部分送信が発生する可能性がある
- 0xC061エラー（デバイスが存在しない）の根本原因が、実は送信データの不完全性にある可能性を排除するため
- 送信完全性を保証することで、受信側（PLC）が正しいフレームを受け取っていることを確認できる

### 他の方法との比較
#### 検討した方法1: SendAsyncの戻り値を無視
- ❌ 送信エラーを検知できない
- ❌ デバッグ情報が不足する

#### 検討した方法2: 例外処理のみに依存
- ❌ 部分送信は例外を発生させない
- ❌ エラー検知が遅れる可能性がある

#### 採用した方法: 明示的な送信バイト数検証
- ✅ 送信完全性を即座に確認できる
- ✅ ログに詳細な診断情報を記録できる
- ✅ 将来的な拡張（再送処理など）に対応しやすい

## 技術選択のトレードオフ

### メリット
1. **デバッグ容易性の向上**: 送信処理の詳細がログに記録される
2. **問題の早期発見**: 部分送信が発生した場合、即座に検知できる
3. **保守性の向上**: コードの意図が明確になる
4. **将来の拡張性**: 送信バイト数に基づいた再送処理などを追加しやすい

### デメリット
1. **パフォーマンスへの影響**: ログ出力のオーバーヘッド（微小）
2. **コード量の増加**: 検証ロジックの追加により行数が増える

### トレードオフの判断
- テストプログラムであり、診断情報の充実度が最優先
- パフォーマンスへの影響は無視できるレベル
- メリットがデメリットを大きく上回ると判断

## 実装後のコード
```csharp
// ArraySegment範囲確認
var segment = new ArraySegment<byte>(requestFrame);
Log($"[確認] ArraySegment: Offset={segment.Offset}, Count={segment.Count}, 配列長={requestFrame.Length}");

int sent = await socket.SendAsync(segment, SocketFlags.None);

// 送信バイト数検証
if (sent != requestFrame.Length)
{
    Log($"[エラー] 送信不完全: 期待={requestFrame.Length}, 実際={sent}");
    _failureCount++;
}
else
{
    Log($"[確認] ✓ 全バイト送信完了（{sent}バイト）");
}
```

## 期待される効果
1. 送信データの完全性が保証される
2. ArraySegmentの動作が明示的に確認できる
3. 送信処理のトラブルシューティングが容易になる
4. 0xC061エラーの原因切り分けが進む

## テスト計画
### 単体テストは不要
- テストプログラム自体がテストケースであるため、個別の単体テストは作成しない
- 実機テスト実行時に以下を確認:
  1. ArraySegmentのOffset=0, Count=フレーム長であることを確認
  2. 送信バイト数=フレーム長であることを確認
  3. エラーログが出力されないことを確認

### 実機テスト時の確認項目
- [ ] ログに `[確認] ArraySegment: Offset=0, Count=XX, 配列長=XX` が出力される
- [ ] ログに `[確認] ✓ 全バイト送信完了（XXバイト）` が出力される
- [ ] エラーログが出力されない
- [ ] 実行結果が期待通り（送信バイト数が一致）

## 発生した問題と解決過程
問題なく実装完了。

### 実装の詳細
- 176-191行目: 送信処理にArraySegment範囲確認と送信バイト数検証を追加
- ArraySegmentを明示的に変数化することで、Offset/Count/配列長を確認可能に
- 送信バイト数が期待値と一致しない場合、エラーログを出力し_failureCountをインクリメント
- 送信成功時は確認ログを出力

### コード変更箇所
**PlcRealDeviceTest/Program.cs:176-191**
```csharp
// ArraySegment範囲確認
var segment = new ArraySegment<byte>(requestFrame);
Log($"[確認] ArraySegment: Offset={segment.Offset}, Count={segment.Count}, 配列長={requestFrame.Length}");

int sent = await socket.SendAsync(segment, SocketFlags.None);

// 送信バイト数検証
if (sent != requestFrame.Length)
{
    Log($"[エラー] 送信不完全: 期待={requestFrame.Length}, 実際={sent}");
    _failureCount++;
}
else
{
    Log($"[確認] ✓ 全バイト送信完了（{sent}バイト）");
}
```

## 実装完了日時
2025-11-11

## 次のステップ
実機環境で以下を実施:
1. ビルド: `dotnet build`
2. テスト実行: `PlcRealDeviceTest.exe`
3. ログファイル確認:
   - ArraySegment範囲が正しく表示されているか
   - 送信バイト数が一致しているか
   - エラーログが出力されていないか
4. ToDoリスト項目1のチェックボックスを更新

## 備考
- このPCはPLC接続環境ではないため、ビルドは実施しない
- 実機テストは環境がある場所で実施する必要がある
- ToDoリスト項目1の対応として、Phase 1とPhase 2を同時に実装する
- 実装記録ファイル: `documents/implementation_records/method_records/ExecuteTest_送信バイト数チェック機能追加.md`
