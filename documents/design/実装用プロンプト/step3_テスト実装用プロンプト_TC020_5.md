# TC020_5_ConnectAsync_接続時間計測精度テスト実装プロンプト

## 実装指示

**コード作成を開始してください。**

TC020_5_ConnectAsync_接続時間計測精度テストケースを、TDD手法に従って実装してください。

---

## 実装概要

### 目的
ConnectAsync()メソッドが接続処理時間を正確に計測し、ConnectionResponseに記録することを検証します。
パフォーマンス分析・診断のために重要なテストです。

### 実装対象
- **テストファイル**: `Tests/Unit/Core/Managers/PlcCommunicationManagerTests.cs`
- **テスト名前空間**: `andon.Tests.Unit.Core.Managers`
- **テストメソッド名**: `TC020_5_ConnectAsync_接続時間計測精度`

---

## 前提条件

実装開始前に以下を確認してください：

1. **依存ファイルの存在確認**
   - `Core/Managers/PlcCommunicationManager.cs`（時間計測機能実装済み）

2. **前提テスト確認**
   - TC017～TC020_4が実装済み・テストパス済みであること

---

## 実装手順（TDD Red-Green-Refactor）

### Phase 1: Red（テスト失敗）

#### Step 1-1: テストケース実装

**TC020_5: 接続時間計測精度テスト**

**Arrange（準備）**:
- テスト開始時刻記録: `testStartTime = DateTime.UtcNow`
- 期待遅延時間: `expectedDelay = 500ms`
- 許容誤差: `tolerance = 100ms`
- ConnectionConfigを作成（正常な値）
- TimeoutConfigを作成（正常な値）
- **MockSocketで500ms遅延設定**
- PlcCommunicationManagerインスタンス作成（モックSocket注入）

**Act（実行）**:
```csharp
var result = await manager.ConnectAsync(config, timeout);
var actualElapsedTime = DateTime.UtcNow - testStartTime;
```

**Assert（検証）**:
```csharp
// 基本検証
Assert.NotNull(result);
Assert.Equal(ConnectionStatus.Connected, result.Status);
Assert.NotNull(result.Socket);
Assert.NotNull(result.ConnectionTime);

// 時間範囲検証（400ms～600ms）
Assert.True(result.ConnectionTime.Value >= TimeSpan.FromMilliseconds(400));
Assert.True(result.ConnectionTime.Value <= TimeSpan.FromMilliseconds(600));

// ConnectedAtとConnectionTimeの整合性検証
var timeFromConnectedAt = result.ConnectedAt.Value - testStartTime;
var timeDifference = Math.Abs((timeFromConnectedAt - result.ConnectionTime.Value).TotalMilliseconds);
Assert.True(timeDifference <= 100);
```

---

### Phase 2: Green（最小実装）

#### Step 2-1: ConnectAsync 時間計測実装

**実装箇所**: `Core/Managers/PlcCommunicationManager.cs`

```csharp
public async Task<ConnectionResponse> ConnectAsync(ConnectionConfig config, TimeoutConfig timeout)
{
    // 前処理（null検証、IP検証、ポート検証、接続済みチェック）...

    var startTime = DateTime.UtcNow;  // 開始時刻記録

    try
    {
        _socket = CreateSocket(config);
        await _socket.ConnectAsync(new IPEndPoint(ipAddress, config.Port));
        _status = ConnectionStatus.Connected;

        var connectionTime = DateTime.UtcNow - startTime;  // 接続時間計測
        var connectedAt = DateTime.UtcNow;

        return new ConnectionResponse
        {
            Status = _status,
            Socket = _socket,
            RemoteEndPoint = _socket.RemoteEndPoint,
            ConnectedAt = connectedAt,
            ConnectionTime = connectionTime,  // 計測結果を記録
            ErrorMessage = null
        };
    }
    catch (Exception ex)
    {
        _status = ConnectionStatus.Error;
        throw;
    }
}
```

---

## 技術仕様詳細

### 時間計測方法

**DateTime.UtcNow（推奨・シンプル）**:
```csharp
var startTime = DateTime.UtcNow;
// 処理...
var connectionTime = DateTime.UtcNow - startTime;
```

**Stopwatch（高精度・参考）**:
```csharp
var stopwatch = Stopwatch.StartNew();
// 処理...
stopwatch.Stop();
var connectionTime = stopwatch.Elapsed;
```

### 時間計測の精度

- **DateTime.UtcNow**: 約1～15ms精度（システム依存）
- **Stopwatch**: 約0.1μs～1μs精度（システム依存）
- **許容誤差**: ±100ms（テスト環境の変動考慮）

### MockSocket遅延実装

```csharp
public class MockSocket : Socket
{
    private TimeSpan _connectDelay = TimeSpan.Zero;

    public void SetConnectDelay(TimeSpan delay)
    {
        _connectDelay = delay;
    }

    public override async Task ConnectAsync(EndPoint remoteEP, CancellationToken cancellationToken)
    {
        // 意図的な遅延発生
        await Task.Delay(_connectDelay, cancellationToken);
        // 接続成功をシミュレート
    }

    public override bool Connected => true;
    public override EndPoint? RemoteEndPoint => new IPEndPoint(IPAddress.Parse("192.168.1.10"), 5000);
}
```

---

## 完了条件

- [ ] TC020_5テストがパス
- [ ] ConnectionTime範囲検証パス（400ms～600ms）
- [ ] ConnectedAtとConnectionTimeの整合性確認
- [ ] 時間計測精度が妥当範囲内
- [ ] TC020_1～TC020_4も引き続きパス（回帰テスト）

---

## ログ出力

```
[Information] ConnectAsync: 接続開始 - 192.168.1.10:5000
[Information] ConnectAsync: 接続成功 - 192.168.1.10:5000 (523ms)
```

---

## 時間計測の重要性

- **パフォーマンス分析**: 接続処理時間の可視化
- **タイムアウト判定**: 計測精度がタイムアウト判定に影響
- **ログ・診断**: 正確な時間記録による問題診断
- **SLA管理**: 接続時間のSLA監視

---

以上の指示に従って、TC020_5_ConnectAsync_接続時間計測精度テストの実装を開始してください。
