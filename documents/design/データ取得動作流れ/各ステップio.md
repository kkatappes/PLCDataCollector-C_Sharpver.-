各ステップi/o

- Step1 設定ファイル読み込み
    - in:
        - 設定ソース
            - 入力源: C:\Users\1010821\Desktop\python\andon\dist\appsettings.xlsx
            - 形式: JSON
            - 文字コード: UTF-8
        - ルートキーと構造
            - ルートキー: PlcCommunication
            - フィールド
                - Connection:
                    - IpAddress: string, 必須, 既定 ""
                    - Port: integer, 必須, 既定 ""
                    - UseTcp: boolean, 必須, 既定 false
                    - IsBinary: boolean, 必須, 既定 true
                    - FrameVersion: string, 必須, 既定 "4E"

                - Timeouts:
                    - ReceiveTimeoutMs: integer, 任意, 既定 900
                    - ConnectTimeoutMs: integer, 任意, 既定 3000

                - TargetDevices:
                    - MDeviceRange:
                        - Start: integer, 必須, 既定 0
                        - End: integer, 必須, 既定 999
                        - DeviceCode: string, 必須, 既定 "M"
                    - DDeviceRange:
                        - Start: integer, 必須, 既定 0
                        - End: integer, 必須, 既定 999
                        - DeviceCode: string, 必須, 既定 "D"

                - MonitoringIntervalMs: integer, 必須, 既定 1000

            - ルートキー: SystemResources
            - フィールド
                - MemoryLimitKB: integer, 任意, 既定 450
                - MaxBufferSize: integer, 必須, 既定 2048
                - MemoryThresholdKB: integer, 任意, 既定 512

            - ルートキー: Logging:
            - フィールド
                - ConsoleOutput:
                    - FilePath: string, 必須, 既定 "logs/terminal_output.txt"
                    - MaxFileSizeMB: integer, 任意, 既定 10
                    - MaxFileCount: integer, 任意, 既定 5
                    - FlushIntervalMs: integer, 任意, 既定 1000
                - DetailedLog:
                    - FilePath: string, 必須, 既定 "logs/rawdata_analysis.json"
                    - MaxFileSizeMB: integer, 任意, 既定 50
                    - RetentionDays: integer, 任意, 既定 14

    - out:
        - 読み込んだ以下の各種設定値
            - ConnectionConfig（IpAddress, Port, UseTcp, IsBinary, FrameVersion）
            - TimeoutConfig（ReceiveTimeoutMs, ConnectTimeoutMs）
            - TargetDeviceConfig（MDeviceRange, DDeviceRange）
            - MonitoringIntervalMs
            - SystemResourcesConfig（MemoryLimitKB, MaxBufferSize, MemoryThresholdKB）
            - LoggingConfig（ConsoleOutput, DetailedLog）

- Step2 通信フレーム構築
    - in:必須, ConnectionConfig（IpAddress, Port, UseTcp, IsBinary, FrameVersion）, TargetDeviceConfig（MDeviceRange, DDeviceRange）
    - out:設定値から生成したインスタンス(SLMPフレーム,16進数文字列), 既定値から生成される文字列 540000010000001B001004030000024D00000003F440000003E8

- Step3 PLC接続処理
    - in:必須, ConnectionConfig（IpAddress, Port, UseTcp）, TimeoutConfig（ConnectTimeoutMs）
    - out:接続レスポンス(Status, Socket, RemoteEndPoint, ConnectedAt, ConnectionTime)

- Step4 PLCへのリクエスト送信/PLCからのデータ受信
    - in:必須, 設定値から生成したインスタンス, TimeoutConfig（ReceiveTimeoutMs）
    - out:各種PLCの状態/生データ(16進数)

- Step5 PLC切断処理
    - in:必須, 切断/リソース管理情報
    - out:接続統計/リソース状態

- Step6 受信データ解析
    - in:必須, Step4で受信した生データ(16進数)
    - out:構造化データ(ヘッダー,データ)

- Step7 データ出力
    - in:必須, 構造化データ(ヘッダー,データ),LoggingConfig（ConsoleOutput, DetailedLog）
    - out:コンソール/ファイル出力