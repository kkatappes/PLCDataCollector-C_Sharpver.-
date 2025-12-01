# TC018: ConnectAsync_UDP接続成功 テスト実装プロンプト

## 🎯 テスト目的
PlcCommunicationManager.ConnectAsync メソッドのUDP接続成功機能が正常に動作することを確認

## ⭐ 重要度: 高（★マーク付きテスト）
Phase 1基本動作確認の接続系テストとして、UDP通信の基本接続が成功することを検証

## 📋 テスト仕様

### テスト対象メソッド
```csharp
Task<ConnectionResponse> ConnectAsync(
    string ipAddress,
    int port,
    string protocol = "UDP",
    TimeoutConfig timeout = null,
    CancellationToken cancellationToken = default
)
```

### 成功条件
1. **UDP接続成功**: 指定されたIP・ポートへのUDP接続が成功
2. **ConnectionResponse生成**: 接続成功を示すレスポンスオブジェクトが生成
3. **接続状態更新**: 内部接続状態が適切に更新
4. **統計情報更新**: 接続統計が適切に記録

### テストデータ
```csharp
// 接続設定
string ipAddress = "192.168.1.100";
int port = 5001;
string protocol = "UDP";

// タイムアウト設定
TimeoutConfig timeout = new TimeoutConfig
{
    ConnectTimeoutMs = 3000,  // UDPは通常TCPより短い
    ReceiveTimeoutMs = 3000,
    SendTimeoutMs = 3000
};
```

## 🧪 テスト実装パターン

### 1. Arrange（準備）
```csharp
// PlcCommunicationManagerインスタンス作成
// MockPlcSimulatorでUDPサーバー起動
// 接続パラメータ準備
// CancellationToken準備
```

### 2. Act（実行）
```csharp
var result = await plcManager.ConnectAsync(
    ipAddress,
    port,
    "UDP",
    timeout,
    cancellationToken
);
```

### 3. Assert（検証）
```csharp
// result != null
// result.IsSuccess == true
// result.Protocol == "UDP"
// result.ConnectedEndPoint == $"{ipAddress}:{port}"
// result.ConnectionTimeMs > 0
```

## 📊 検証項目詳細

### UDP接続機能検証
- [ ] UDP接続の正常確立
- [ ] 接続タイムアウト内での完了
- [ ] UDPソケットオプションの適切な設定
- [ ] 接続状態の正確な更新

### UDP特有の検証事項
- [ ] コネクションレス通信の適切な処理
- [ ] UDPエンドポイントの正確な設定
- [ ] 送受信バッファサイズの設定
- [ ] ブロードキャスト/マルチキャスト対応

### レスポンスオブジェクト検証
- [ ] ConnectionResponse オブジェクト生成
- [ ] IsSuccess プロパティが true
- [ ] Protocol プロパティが "UDP"
- [ ] ConnectedEndPoint の正確な設定
- [ ] ConnectionTimeMs の適切な値

## 🔧 モック・依存関係

### 必要なモック
```csharp
// ILoggingManager - ログ出力用
Mock<ILoggingManager> mockLogging;

// IErrorHandler - エラー処理用
Mock<IErrorHandler> mockErrorHandler;

// IResourceManager - リソース管理用
Mock<IResourceManager> mockResourceManager;

// MockPlcSimulator - UDP接続先シミュレーター
MockPlcSimulator mockPlcServer;
```

### UDP設定値
```csharp
// UDP接続設定
UdpConnectionConfig = {
    EnableBroadcast = false,
    DontFragment = true,
    ReceiveBufferSize = 8192,
    SendBufferSize = 8192,
    Ttl = 64
};

// タイムアウト設定
ConnectTimeout = 3000ms;  // TCP短い
SocketReceiveTimeout = 3000ms;
SocketSendTimeout = 3000ms;
```

## 📈 成功基準

### 機能的成功基準
1. **接続成功**: UDP接続が例外なく成功
2. **適切な応答時間**: 接続時間が設定タイムアウト内（< 3秒）
3. **正確なレスポンス**: ConnectionResponseの全プロパティが正確
4. **状態整合性**: 接続後の内部状態が一貫

### UDP接続パフォーマンス基準
```csharp
// 期待値
ConnectionTimeMs < 500;   // UDPは通常TCPより高速
IsSuccess == true;
Protocol == "UDP";
ConnectedEndPoint == "192.168.1.100:5001";
```

### TCPとの相違点確認
```csharp
// UDP特有の確認項目
// - コネクションレス通信の対応
// - 送信前の接続確認不要
// - 受信時のエンドポイント確認
```

## 🚨 注意事項

### UDP接続実装の注意
- **コネクションレス**: TCP異なり実際の接続確認が困難
- **エラーハンドリング**: 送信エラーが即座に検出されない特性
- **タイムアウト処理**: UDP特有の動作を考慮
- **リソース管理**: UDPソケットの適切な管理

### UDP vs TCP の相違点
- **接続確認**: UDPは実際の疎通確認が必要
- **エラー検出**: 送信エラーの検出タイミングが違う
- **パフォーマンス**: 一般的にUDPの方が高速
- **信頼性**: TCPほど信頼性保証がない

## 📋 チェックリスト

### 実装前チェック
- [ ] UDP通信仕様の理解
- [ ] TCP接続との相違点理解
- [ ] MockPlcSimulatorのUDP対応確認
- [ ] UDPテストパラメータの準備

### 実装後チェック
- [ ] UDP接続が正常に成功
- [ ] ConnectionResponseが正確
- [ ] 実行時間が適切（< 1秒）
- [ ] メモリリークなし

### UDP接続テストケース
```csharp
// テストケース1: 基本UDP接続
// 正常なIP・ポートでの接続成功

// テストケース2: 接続時間測定
// TCP比較してUDP接続時間が短いことの確認

// テストケース3: UDP設定値確認
// UDPソケットオプションが正しく設定
```

### Phase 1基本動作確認での位置づけ
- **接続系（3テスト中の2番目）**
- **推定実行時間**: 10-15分
- **★重要度**: 高（最小成功基準に含まれる）
- **前提テスト**: TC017（TCP接続）
- **後続テスト**: TC021（切断）

### 最小成功基準への貢献
- **7つの★重要テストの1つ**
- **UDP通信確立**: UDPプロトコルでの基本通信確認
- **プロトコル対応**: TCP/UDP両対応の実証

### 依存関係
- **前提条件**: TC017（TCP接続）成功推奨（但し独立実行可能）
- **後続への影響**: UDP接続成功がStep4送受信の前提の一つ
- **TC066への貢献**: 完全サイクルテストでのプロトコル選択肢提供

### TCP接続テスト（TC017）との比較検証
- **プロトコル相違**: 接続応答時間・エラーハンドリングの違い
- **設定値相違**: タイムアウト値・ソケットオプションの違い
- **使用シーン**: どちらのプロトコルを使用するかの判断材料