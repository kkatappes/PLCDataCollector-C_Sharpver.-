# PlcCommunicationManager テスト設計 Part1 (Step3-5関連)

## テスト方針
- **Step3-6ブロック（PlcCommunicationManager）を優先**して実装・テスト
- **TDD手法**を使用してテスト駆動開発を実施
- **単一ブロック毎**の機能をテスト→パス確認後に複合時の動作テスト
- メソッドレベルの粒度でテストを実施

## 対応する動作フロー
- **Step3**: PLCへの接続処理
- **Step4**: PLCにリクエストを送信/リクエスト情報に従ってレスポンスを受信
- **Step5**: 接続切断処理

---

## 2. PlcCommunicationManager テスト設計（最優先実装）

### 2.1 ConnectAsync メソッド
**目的**: PLC接続機能をテスト

#### 正常系テスト
- **TC017_ConnectAsync_TCP接続成功**
  - 入力:
    - ConnectionConfig（IpAddress="192.168.1.10", Port=5000, UseTcp=true）
    - TimeoutConfig（ConnectTimeoutMs=5000, SendTimeoutMs=3000, ReceiveTimeoutMs=3000）
  - 期待出力: ConnectionResponse（接続成功オブジェクト）
    - Status=ConnectionStatus.Connected（接続成功）
    - Socket（System.Net.Sockets.Socket型）: 接続済みSocketインスタンス
    - RemoteEndPoint（System.Net.EndPoint型）: "192.168.1.10:5000"が正確に記録
    - ConnectedAt（DateTime型）: 接続完了時刻が記録済み
    - ConnectionTime（TimeSpan型）: 接続処理にかかった時間
      - 検証: ConnectionTime.TotalMilliseconds > 0 && < ConnectTimeoutMs
    - ErrorMessage=null（成功時はnull）

- **TC017_1_ConnectAsync_ソケットタイムアウト設定確認**
  - 前提: TC017のTCP接続成功後
  - 検証項目:
    - Socket.SendTimeout == TimeoutConfig.SendTimeoutMs（送信タイムアウト設定）
    - Socket.ReceiveTimeout == TimeoutConfig.ReceiveTimeoutMs（受信タイムアウト設定）
  - 重要性: Step4（送受信処理）で個別にタイムアウト制御を実装する必要がなくなる
  - 設計方針: ソケットレベルでのタイムアウト設定により、保守性が向上する

- **TC018_ConnectAsync_UDP接続成功（実データ基準）**
  - 入力:
    - ConnectionConfig（IpAddress="172.30.40.15", Port=8192, UseTcp=false）
    - TimeoutConfig（ConnectTimeoutMs=5000, SendTimeoutMs=3000, ReceiveTimeoutMs=500）
  - 期待出力: ConnectionResponse（UDP接続成功オブジェクト）
    - Status=ConnectionStatus.Connected
    - Socket: UDP用Socketインスタンス
    - RemoteEndPoint設定済み（"172.30.40.15:8192"）
    - ConnectedAt設定済み
    - ConnectionTime記録済み
    - UDP疎通確認方法（TDD・オフライン環境対応）:
      - テスト実施方法: PLCシミュレータまたはネットワークモックを使用
      - 模擬送信フレーム（4E形式マルチリード、213バイト）:
        ```
        5400000000000000FFFF0300C800200003040000300048EE00A84BEE00A852EE00A85CEE00A8AA1801A8DC1801A8A41901A8B81901A8CC1901A8E01901A8200000 90B2DE00907ADF0090DEDF009050E0009061E00090A6E00090BBE00090CEE00090E006009CF006009C0407009C2007009C4007009C5007009C6707009C7707009C 8907009C9A07009CAE07009CBE07009CDE07009C2208009C3208009C4508009C5508009C6808009C0817009C2017009C3017009C4817009C6017009C7017009C 8217009CA017009C00090 09D20090 09D40090 09D
        ```
        - 構成: 4E形式マルチリードコマンド（0x0304）、48デバイスポイント指定
      - 模擬応答データ（4E形式レスポンス、111バイト）:
        ```
        D4000000000000FFFF03006200000 0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0719FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00100008000100100010000820001000080002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
        ```
        - 構成: 4E形式正常応答（終了コード0x0000）、96バイトのデバイスデータ
      - モック動作: ConnectTimeoutMs内（500ms以内）に正常応答を返却
      - 検証項目:
        - Socket.Connected状態確認
        - RemoteEndPoint="172.30.40.15:8192"設定確認
        - 応答受信完了確認（111バイト受信）
        - ConnectionTime < ConnectTimeoutMs（5000ms）確認
      - 重要: 実際のPLC機器不要、完全オフライン環境でのテスト実施

#### 異常系テスト
- **TC019_ConnectAsync_接続タイムアウト**
  - 入力:
    - ConnectionConfig（IpAddress="192.168.100.200", Port=5000）: 到達不可能なIPアドレス
    - TimeoutConfig（ConnectTimeoutMs=1000）
  - 期待動作:
    - TimeoutException がスローされる
    - 例外メッセージ: "接続タイムアウト: 192.168.100.200:5000（タイムアウト時間: 1000ms）"
    - ConnectionResponse は返却されない（例外発生のため）
  - テスト実装例:
    Assert.ThrowsAsync<TimeoutException>(async () =>
        await manager.ConnectAsync(config, timeout));

- **TC020_ConnectAsync_接続拒否**
  - 入力:
    - ConnectionConfig（IpAddress="192.168.1.10", Port=9999）: 接続拒否するポート
    - TimeoutConfig（ConnectTimeoutMs=5000）
  - 期待動作:
    - SocketException（または ConnectionException）がスローされる
    - 例外メッセージ: "接続拒否: 192.168.1.10:9999"
    - ConnectionResponse は返却されない（例外発生のため）
  - テスト実装例:
    Assert.ThrowsAsync<SocketException>(async () =>
        await manager.ConnectAsync(config, timeout));

- **TC020_1_ConnectAsync_不正IPアドレス**
  - 入力:
    - ConnectionConfig（IpAddress="999.999.999.999", Port=5000）: 不正なIPアドレス形式
    - TimeoutConfig（ConnectTimeoutMs=5000）
  - 期待出力: ArgumentException または FormatException
    - ErrorMessage: "不正なIPアドレス形式: 999.999.999.999"

- **TC020_2_ConnectAsync_不正ポート番号**
  - 入力:
    - ConnectionConfig（IpAddress="192.168.1.10", Port=70000）: 範囲外のポート番号（有効範囲: 1-65535）
    - TimeoutConfig（ConnectTimeoutMs=5000）
  - 期待出力: ArgumentOutOfRangeException
    - ErrorMessage: "ポート番号が範囲外です: 70000（有効範囲: 1-65535）"

- **TC020_3_ConnectAsync_null入力**
  - 入力: ConnectionConfig=null または TimeoutConfig=null
  - 期待出力: ArgumentNullException
    - ErrorMessage: "ConnectionConfigまたはTimeoutConfigがnullです"

- **TC020_4_ConnectAsync_既に接続済み状態での再接続**
  - 前提: ConnectAsync成功済み（Status=Connected、Socket接続中）
  - 入力: 同じConnectionConfigで再度ConnectAsync実行
  - 期待出力: InvalidOperationException
    - ErrorMessage: "既に接続済みです。再接続する場合は先にDisconnectAsync()を実行してください"
  - 設計判断: 既存接続を自動切断せず、明示的な切断を要求する安全設計

- **TC020_5_ConnectAsync_接続時間計測精度**
  - 前提: 意図的に遅延を発生させる環境（モック・スタブで遅延制御）
  - 入力:
    - ConnectionConfig（IpAddress="192.168.1.10", Port=5000）
    - 意図的遅延: 500ms
  - 検証項目:
    - ConnectionTime >= 実際の接続時間（誤差±100ms以内）
    - ConnectedAt（DateTime）と処理開始時刻の差 == ConnectionTime（誤差±100ms以内）
    - 時間計測の精度確認
  - 重要性: パフォーマンス分析、タイムアウト判定の正確性に直結

### 2.2 SendFrameAsync メソッド
**目的**: PLCへのリクエスト送信機能をテスト

#### 正常系テスト
- **TC021_SendFrameAsync_正常送信（実データ基準）**
  - 前提: ConnectAsyncが成功済み（UDP: 172.30.40.15:8192）
  - 入力: 有効な4E形式マルチリードSLMPフレーム（213バイト）
    - HEX文字列:
      ```
      5400000000000000FFFF0300C800200003040000300048EE00A84BEE00A852EE00A85CEE00A8AA1801A8DC1801A8A41901A8B81901A8CC1901A8E01901A8200000 90B2DE00907ADF0090DEDF009050E0009061E00090A6E00090BBE00090CEE00090E006009CF006009C0407009C2007009C4007009C5007009C6707009C7707009C 8907009C9A07009CAE07009CBE07009CDE07009C2208009C3208009C4508009C5508009C6808009C0817009C2017009C3017009C4817009C6017009C7017009C 8217009CA017009C00090

09D20090 09D40090 09D
      ```
    - フレーム構成:
      - サブヘッダ: `5400` (4E形式)
      - シーケンス番号: `0000`
      - 予約: `0000`
      - ネットワーク情報: `00FFFF0300` (ネットワーク番号:00, PC番号:FF, I/O番号:03FF, 局番:00)
      - データ長: `C800` (200バイト、リトルエンディアン)
      - 監視タイマ: `2000` (32秒、リトルエンディアン)
      - コマンド: `0304` (マルチリードコマンド)
      - サブコマンド: `0000` (ワード単位)
      - データポイント数: `3000` (48点、リトルエンディアン)
      - デバイス指定: 48個の[デバイスコード(1byte)+開始番号(3bytes、リトルエンディアン)]
        - 例: `A8 48 EE 00` → D機器(A8) + 開始番号0xEE48(60968)
        - 例: `90 B2 DE 00` → M機器(90) + 開始番号0xDEB2(57010)
  - 期待出力:
    - 送信完了ステータス（SendResult.Success）
    - 送信バイト数: 213バイト
    - 送信統計記録更新（送信フレーム数+1、送信バイト数+213）

- **TC022_SendFrameAsync_全機器データ取得（マルチリード）**
  - 前提: ConnectAsyncが成功済み
  - 入力: TC021と同じ4E形式マルチリードフレーム（213バイト）
  - 期待出力:
    - 一度の通信で48デバイスポイントのデータ取得完了ステータス
    - 送信完了後、ReceiveResponseAsyncで111バイトのレスポンス受信可能
  - 動作フロー成功条件:
    - 一度の通信で複数機器（M機器・D機器）から48ワード分のデータを取得
    - マルチリードコマンド（0x0304）による効率的なデータ取得

#### 異常系テスト
- **TC023_SendFrameAsync_未接続状態**
  - 前提: ConnectAsyncが未実行
  - 入力: 任意のSLMPフレーム
  - 期待出力: InvalidOperationException

- **TC024_SendFrameAsync_不正フレーム**
  - 前提: ConnectAsyncが成功済み
  - 入力: 不正な16進数文字列
  - 期待出力: FormatException

### 2.3 ReceiveResponseAsync メソッド
**目的**: PLCからのデータ受信機能をテスト

#### 正常系テスト
- **TC025_ReceiveResponseAsync_正常受信（実データ基準）**
  - 前提: SendFrameAsync（TC021のマルチリード213バイト）が成功済み
  - 入力: TimeoutConfig（ReceiveTimeoutMs=500）
  - 期待出力: PLCからの4E形式レスポンス生データ（111バイト）
    - HEX文字列:
      ```
      D4000000000000FFFF03006200000 0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0719FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00100008000100100010000820001000080002000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
      ```
    - フレーム構成:
      - サブヘッダ: `D400` (4E形式レスポンス)
      - シーケンス番号: `0000`
      - 予約: `0000`
      - ネットワーク情報: `00FFFF0300` (ネットワーク番号:00, PC番号:FF, I/O番号:03FF, 局番:00)
      - データ長: `6200` (98バイト、リトルエンディアン)
      - 終了コード: `0000` (正常終了)
      - デバイスデータ: 96バイト（48ワード × 2バイト）
        - D0-D9: 0xFFFF (65535) × 10ワード
        - 以降: 可変データ（実際のPLC内部値）
    - 受信統計記録:
      - 受信フレーム数: +1
      - 受信バイト数: +111
      - 受信時刻記録

#### 異常系テスト
- **TC026_ReceiveResponseAsync_受信タイムアウト**
  - 前提: SendFrameAsyncが成功済み
  - 入力: TimeoutConfig（ReceiveTimeoutMs=100）
  - 期待出力: TimeoutException

### 2.4 DisconnectAsync メソッド
**目的**: PLC切断機能をテスト

#### 正常系テスト
- **TC027_DisconnectAsync_正常切断**
  - 前提: ConnectAsyncが成功済み
  - 入力: 接続統計情報
  - 期待出力:
    - 切断完了ステータス
    - 接続統計情報（ConnectionTime等）
    - リソース解放完了

#### 異常系テスト
- **TC028_DisconnectAsync_未接続状態切断**
  - 前提: ConnectAsyncが未実行
  - 入力: なし
  - 期待出力: 何も処理せず正常終了

---

## Step3-5詳細フロー（動作フロー対応）

### Step3詳細フロー
1. ソケット作成
2. 接続実行
3. 接続成功判定（失敗時はエラーをスロー）
4. タイムアウト設定（送信・受信）
5. 接続情報記録
6. 接続情報返却

### Step4詳細フロー

#### Step4-1 リクエスト送信
1. 接続情報チェック（接続情報 == True?）
2. 接続が確認できない場合はエラーをスロー
3. 構築済みフレーム送信
4. 送信完了確認（送信完了?）
5. 送信失敗時はエラーをスロー
6. 送信統計記録
    ・送信フレーム数 +1
    ・送信バイト数加算
    ・送信時刻記録
7. void返却

#### Step4-2 データ受信
1. PLCレスポンス待機
2. タイムアウト内で受信確認（タイムアウト内で受信?）
3. タイムアウト時は時間内に受信無しのメッセージ出力
4. 受信統計記録
    ・受信フレーム数 +1
    ・受信バイト数加算
    ・受信時刻記録
5. 生データ(16進数)返却

### Step5詳細フロー
1. 切断要求
2. 接続状態チェック（接続中?）
3. 未接続の場合は切断完了済メッセージ出力
4. 通信全体の統計情報収集
    ・接続時間
    ・送信フレーム数
    ・受信フレーム数
    ・送信バイト数
    ・受信バイト数
    ・応答時間統計
    ・エラー回数
    ・通信成功率
5. ソケットシャットダウン
6. ソケットクローズ
7. ソケット破棄
8. 内部状態初期化
9. 接続状態返却

---

## テストデータ
- **PLCシミュレータ**: 実際のPLC無しでの通信テスト
- **ネットワークモック**: 通信エラーシミュレーション用
- **模擬応答データ**: 既知のSLMP応答パターン
