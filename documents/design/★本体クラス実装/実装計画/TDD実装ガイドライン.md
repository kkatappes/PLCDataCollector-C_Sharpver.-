# TDD実装時の共通注意点

## 1. Red-Green-Refactorサイクルの厳守
- **必ずテストを先に書く**（Red）
- **テストを通すための最小限のコードのみ実装**（Green）
- **動作を保ったままリファクタリング**（Refactor）

## 2. 小さなステップで進める
- 1つのTDDサイクルは15分以内に完了するサイズにする
- 大きな機能は複数のTDDサイクルに分割する

## 3. テスト可能な設計
- **依存性注入**: すべての依存関係はコンストラクタで注入
- **純粋関数の活用**: 副作用のない関数を優先
- **単一責任原則**: 各クラスは1つの責任のみを持つ

## 4. モック/スタブの活用
- 外部依存（PLC通信、ファイルI/O等）は必ずモック化
- Moqライブラリを使用してインターフェースをモック

```csharp
// モックの例
var mockLogger = new Mock<ILoggingManager>();
mockLogger.Setup(m => m.LogInfo(It.IsAny<string>())).Returns(Task.CompletedTask);
```

## 5. 境界値テスト
- 正常系だけでなく、境界値・異常系も必ずテスト
- 空配列、null、タイムアウト、例外発生などをカバー

## 6. コードカバレッジ目標
- **Phase 1**: 85%以上
- **Phase 2以降**: 90%以上

## 7. 非同期処理のテスト
- `async/await`を正しく使用
- タイムアウトテストには`Task.Delay()`や`CancellationTokenSource`を活用

## 8. テスト実行の自動化
```bash
# 全テスト実行
dotnet test

# 特定のテストクラス実行
dotnet test --filter "FullyQualifiedName~TimerServiceTests"

# カバレッジ測定
dotnet test /p:CollectCoverage=true
```

---

## 参照ドキュメント

- **TDD手法**: `documents/development_methodology/development-methodology.md`
- **プロジェクト構造設計**: `documents/design/プロジェクト構造設計.md`
- **クラス設計**: `documents/design/クラス設計.md`
- **テスト内容**: `documents/design/テスト内容.md`
- **実装チェックリスト**: `documents/design/実装チェックリスト.md`
