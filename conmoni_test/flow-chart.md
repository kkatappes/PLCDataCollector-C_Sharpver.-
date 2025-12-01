# Run メソッドのフローチャート

以下は `void Run()` メソッドの処理フローを表すMermaidフローチャートです。

```mermaid
flowchart TD
    A[開始] --> B[無限ループ開始<br/>while true]
    B --> C[設定リストから次のPLC設定を取得<br/>foreach settings in _targetList]
    C --> D[PLC機器に接続してデータ取得<br/>SocketConnection settings]
    D --> E{データ取得成功？<br/>response, key, dateTime が有効？}

    E -->|無効<br/>null または default| F[次の設定へスキップ<br/>continue]
    F --> G{全設定処理完了？}

    E -->|有効| H[SQL INSERT文を定義<br/>INSERT INTO cbm...]
    H --> I[データベース挿入リトライループ開始<br/>success = false<br/>while not success]
    I --> J[パラメータ化クエリでデータ挿入試行<br/>using NpgsqlCommand]

    J --> K{データベース挿入成功？}
    K -->|成功| L[成功フラグを設定<br/>success = true]
    L --> G

    K -->|エラー発生| M[エラーログ出力<br/>Console.WriteLine]
    M --> N[既存のDB接続をクローズ<br/>_connection.Close]
    N --> O[データベースに再接続<br/>EstablishConnection]
    O --> I

    G -->|まだある| C
    G -->|全て完了| P[CPU使用率抑制のため<br/>10ms スリープ<br/>Thread.Sleep 10]
    P --> B
```

## 処理の概要

このフローチャートは以下の主要な処理を表現しています：

### 1. メインループ
- アプリケーションは無限ループで動作
- 各サイクルで全てのPLC設定を順次処理

### 2. データ取得処理
- 各PLC設定に対してSocketConnectionメソッドを呼び出し
- TCP/UDP通信でPLC機器からデータを取得
- データ取得に失敗した場合は次の設定にスキップ

### 3. データベース保存処理
- 取得したデータをPostgreSQLデータベースに保存
- パラメータ化クエリを使用してSQLインジェクション攻撃を防止
- データベースエラー発生時は自動的に再接続を試行

### 4. エラーハンドリング
- ソケット通信エラーは個別に処理（次の設定へ続行）
- データベースエラーは接続の再確立で対応
- 全ての例外は適切にログ出力

### 5. パフォーマンス最適化
- 各サイクル後に10msのスリープでCPU使用率を制御
- リソースの適切な解放（usingステートメント使用）

このアーキテクチャにより、システムは高い可用性と安定性を維持しながら、複数のPLC機器から継続的にデータを収集できます。