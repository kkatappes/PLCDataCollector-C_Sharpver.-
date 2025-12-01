# TC017: ConnectAsync_TCP接続成功 テスト実装プロンプト

## 🎯 テスト目的
PlcCommunicationManager.ConnectAsync メソッドのTCP接続成功機能が正常に動作することを確認

## ⭐ 重要度: 高（★マーク付きテスト）
Phase 1基本動作確認の接続系テストとして、TCP通信の基本接続が成功することを検証

## 📋 テスト仕様

### テスト対象メソッド
```csharp
Task<ConnectionResponse> ConnectAsync(
    string ipAddress,
    int port,
    string protocol = "TCP",
    TimeoutConfig timeout = null,
    CancellationToken cancellationToken = default
)
```

### 成功条件
1. **TCP接続成功**: 指定されたIP・ポートへのTCP接続が成功
2. **ConnectionResponse生成**: 接続成功を示すレスポンスオブジェクトが生成
3. **接続状態更新**: 内部接続状態が適切に更新
4. **統計情報更新**: 接続統計が適切に記録

### テストデータ
```csharp
// 接続設定
string ipAddress = "192.168.1.100";
int port = 5000;
string protocol = "TCP";

// タイムアウト設定
TimeoutConfig timeout = new TimeoutConfig
{
    ConnectTimeoutMs = 5000,
    ReceiveTimeoutMs = 3000,
    SendTimeoutMs = 3000
};
```

## 🧪 テスト実装パターン

### 1. Arrange（準備）
```csharp
// PlcCommunicationManagerインスタンス作成
// MockPlcSimulatorでTCPサーバー起動
// 接続パラメータ準備
// CancellationToken準備
```

### 2. Act（実行）
```csharp
var result = await plcManager.ConnectAsync(
    ipAddress,
    port,
    "TCP",
    timeout,
    cancellationToken
);
```

### 3. Assert（検証）
```csharp
// result != null
// result.IsSuccess == true
// result.Protocol == "TCP"
// result.ConnectedEndPoint == $"{ipAddress}:{port}"
// result.ConnectionTimeMs > 0
```

## 📊 検証項目詳細

### 基本接続機能検証
- [ ] TCP接続の正常確立
- [ ] 接続タイムアウト内での完了
- [ ] ソケットオプションの適切な設定
- [ ] 接続状態の正確な更新

### レスポンスオブジェクト検証
- [ ] ConnectionResponse オブジェクト生成
- [ ] IsSuccess プロパティが true
- [ ] Protocol プロパティが "TCP"
- [ ] ConnectedEndPoint の正確な設定
- [ ] ConnectionTimeMs の適切な値

### 内部状態検証
- [ ] 内部ソケット状態の更新
- [ ] 接続フラグの設定
- [ ] 統計情報の初期化・更新
- [ ] リソース管理の適切な実行

## 🔧 モック・依存関係

### 必要なモック
```csharp
// ILoggingManager - ログ出力用
Mock<ILoggingManager> mockLogging;

// IErrorHandler - エラー処理用
Mock<IErrorHandler> mockErrorHandler;

// IResourceManager - リソース管理用
Mock<IResourceManager> mockResourceManager;

// MockPlcSimulator - TCP接続先シミュレーター
MockPlcSimulator mockPlcServer;
```

### 設定値
```csharp
// TCP接続設定
TcpConnectionConfig = {
    KeepAlive = true,
    NoDelay = true,
    ReceiveBufferSize = 8192,
    SendBufferSize = 8192
};

// タイムアウト設定
ConnectTimeout = 5000ms;
SocketReceiveTimeout = 3000ms;
SocketSendTimeout = 3000ms;
```

## 📈 成功基準

### 機能的成功基準
1. **接続成功**: TCP接続が例外なく成功
2. **適切な応答時間**: 接続時間が設定タイムアウト内（< 5秒）
3. **正確なレスポンス**: ConnectionResponseの全プロパティが正確
4. **状態整合性**: 接続後の内部状態が一貫

### 接続パフォーマンス基準
```csharp
// 期待値
ConnectionTimeMs < 1000;  // 通常は1秒以内
IsSuccess == true;
Protocol == "TCP";
ConnectedEndPoint == "192.168.1.100:5000";
```

### 非機能的成功基準
1. **リソース管理**: 接続後のリソース適切保持
2. **ログ出力**: 接続プロセスの詳細ログ
3. **エラーハンドリング**: 内部例外の適切なハンドリング

## 🚨 注意事項

### TCP接続実装の注意
- **ソケットオプション**: KeepAlive、NoDelay等の適切な設定
- **タイムアウト処理**: ConnectTimeoutの正確な実装
- **例外処理**: ネットワークエラーの適切なハンドリング
- **リソース管理**: 接続失敗時のソケット適切解放

### テスト実装時の注意
- **非同期処理**: await/async パターンの正確な実装
- **MockPlcSimulator**: TCP接続を受け入れるテスト用サーバー
- **並行実行**: 他のテストとのポート競合回避
- **クリーンアップ**: テスト後の接続適切切断

## 📋 チェックリスト

### 実装前チェック
- [ ] TCP接続仕様の理解
- [ ] MockPlcSimulatorの準備
- [ ] 接続テストパラメータの準備
- [ ] 期待レスポンスの定義

### 実装後チェック
- [ ] TCP接続が正常に成功
- [ ] ConnectionResponseが正確
- [ ] 実行時間が適切（< 2秒）
- [ ] メモリリークなし

### TCP接続テストケース
```csharp
// テストケース1: 基本TCP接続
// 正常なIP・ポートでの接続成功

// テストケース2: 接続時間測定
// 接続時間が適切な範囲内

// テストケース3: 設定値確認
// ソケットオプションが正しく設定
```

### Phase 1基本動作確認での位置づけ
- **接続系（3テスト中の1番目）**
- **推定実行時間**: 10-15分
- **★重要度**: 高（最小成功基準に含まれる）
- **後続テスト**: TC018（UDP接続）→ TC019（受信）

### 最小成功基準への貢献
- **7つの★重要テストの1つ**
- **基本通信機能の土台**: この成功なしに後続テストは実行不可
- **TCP通信確立**: Step3-6完全サイクルの前提条件

### 依存関係
- **前提条件**: なし（独立実行可能）
- **後続への影響**: TCP接続成功がStep4送受信の前提
- **TC066への貢献**: 完全サイクルテストの基盤となる接続機能