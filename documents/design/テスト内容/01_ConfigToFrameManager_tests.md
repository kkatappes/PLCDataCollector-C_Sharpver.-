# ConfigToFrameManager テスト設計 (Step1-2関連)

## テスト方針
- **TDD手法**を使用してテスト駆動開発を実施
- **単一ブロック毎**の機能をテスト→パス確認後に複合時の動作テスト
- **入力/出力の値が既知**のものを含める
- メソッドレベルの粒度でテストを実施

## 対応する動作フロー
- **Step1**: 設定ファイル条件の読み込み
- **Step2**: 通信フレームの構築(DWordの場合は事前に分割処理ステップを入れる)

---

## 1. ConfigToFrameManager テスト設計

### 1.1 LoadConfigAsync メソッド
**目的**: 単一設定ファイル(appsettings.xlsx)の読み込み機能をテスト（既存機能保持）

#### 正常系テスト
- **TC001_LoadConfigAsync_正常**
  - 入力: 有効なappsettings.xlsx（UTF-8形式）
  - 期待出力: 全設定オブジェクトが正常に読み込まれる
    - ConnectionConfig（IpAddress="192.168.1.10", Port=5000等）
    - TimeoutConfig（ReceiveTimeoutMs=3000, ConnectTimeoutMs=5000等）
    - TargetDeviceConfig（MDeviceRange="M001-M999", DDeviceRange="D001-D999"等）
    - ActualConfigPath（実際に読み込んだファイルパス）
    - その他全設定項目

- **TC001_1_LoadConfigAsync_パス解決順序**
  - 入力: 複数候補パスに設定ファイル配置
  - 前提: ./config/test.xlsx, ./test.xlsx, 環境変数パス の順で配置
  - 期待出力: ./config/test.xlsx が優先的に読み込まれる
  - 検証項目: ActualConfigPath の値が正しいパスを示す

#### 異常系テスト
- **TC002_LoadConfigAsync_ファイル不存在**
  - 入力: 存在しないファイルパス
  - 期待出力: FileNotFoundException または ConfigurationError

- **TC003_LoadConfigAsync_フォーマット異常**
  - 入力: 不正なフォーマットのExcelファイル
  - 期待出力: FormatException または ConfigurationError

- **TC004_LoadConfigAsync_必須項目欠如**
  - 入力: 必須項目が欠如した設定ファイル
  - 期待出力: ArgumentNullException または ConfigurationError

### 1.2 GetConfig メソッド
**目的**: 読み込み済み設定の取得機能をテスト

#### 正常系テスト
- **TC005_GetConfig_ConnectionConfig取得**
  - 前提: LoadConfigAsyncが正常完了
  - 入力: ConnectionConfig型指定
  - 期待出力: 正しいConnectionConfigオブジェクト

- **TC006_GetConfig_各設定タイプ取得**
  - 前提: LoadConfigAsyncが正常完了
  - 入力: 各設定タイプ（TimeoutConfig, TargetDeviceConfig等）
  - 期待出力: 対応する設定オブジェクト

#### 異常系テスト
- **TC007_GetConfig_未初期化状態**
  - 前提: LoadConfigAsyncが未実行
  - 入力: 任意の設定タイプ
  - 期待出力: InvalidOperationException

### 1.3 SplitDwordToWord メソッド
**目的**: DWordからWordへの分割処理・デバイスデータ前処理機能をテスト

#### 正常系テスト
- **TC008_SplitDwordToWord_通常データ前処理**
  - 入力: TargetDeviceConfig（MDeviceRange="M001-M999", DataType=Word）
  - 期待出力: 前処理済みデバイス要求情報（最適化済み範囲）

- **TC009_SplitDwordToWord_DWord分割処理**
  - 入力: TargetDeviceConfig（DDeviceRange="D001-D999", DataType=DWord）
  - 期待出力: 前処理済みデバイス要求情報（DWordは16bit×2に分割済み）
  - 動作フロー成功条件: DWordの情報を読み取る場合は分割→フレーム作成のステップが正しく動作すること

- **TC010_SplitDwordToWord_混合データ型**
  - 入力: TargetDeviceConfig（MDeviceRange="M001-M100", DDeviceRange="D001-D100", DataType=Mixed）
  - 期待出力: データ型別に最適化された前処理済み要求情報

#### 異常系テスト
- **TC011_SplitDwordToWord_不正デバイス範囲**
  - 入力: 不正なデバイス範囲（"M1000-M999"等）
  - 期待出力: ArgumentException

### 1.4 BuildFrames メソッド
**目的**: SLMPフレーム構築機能をテスト

#### 正常系テスト
- **TC012_BuildFrames_4Eフレーム構築**
  - 入力:
    - ConnectionConfig（FrameVersion=4E, IsBinary=true）
    - ProcessedDeviceRequestInfo（前処理済みデバイス要求情報）
  - 期待出力: 正しいSLMPフレーム（16進数文字列、2つのフレーム）
  - 期待値例:
    - Mデバイス用: "54001234000000010401006400000090E8030000"
      - 構成: サブヘッダ(54001234000000) + READコマンド(0401) + サブコマンド(0100:ビット単位) + デバイスコード(6400:M機器) + 開始番号(00000090:M000) + デバイス点数(E80300:1000点)
    - Dデバイス用: "54001234000000010400A800000090E8030000"
      - 構成: サブヘッダ(54001234000000) + READコマンド(0401) + サブコマンド(0000:ワード単位) + デバイスコード(A800:D機器) + 開始番号(00000090:D000) + デバイス点数(E80300:1000点)

  - **📋 三菱電機公式リファレンスマニュアル実用例（pdf2img/sh080931q.pdf より）**:
    - **3Eフレーム構造**: サブヘッダ `50H, 00H` (バイナリ) / `"5000"` (ASCII)
    - **M100～M107ビット読み出し実例**:
      - 入力: M100から8ビット読み出し要求
      - 期待フレーム: `"5000"` + `"0401"` + `"01006400000090080000"`
      - 構成: サブヘッダ(5000) + READコマンド(0401) + サブコマンド(0100) + デバイスコード(64=M機器) + 開始デバイス番号(100=64H) + デバイス点数(8=08H)
      - 対応応答: `00H, 01H, 00H, 11H` (M100=OFF, M101=ON, M102-M106=OFF, M107=ON)

  - **🔧 実機テスト設定（4Eフレーム/ASCII、M000-M999/D000-D999）**:
    - **M000～M999ビット読み出し（1000点）**:
      - バイナリ形式（参考）: `54 00 12 34 00 00 00 00  01 04 01 00  64 00 00 00 90  E8 03 00`
      - ASCII形式（実際の送信）: `"54001234000000010401006400000090E8030000"`
      - 構成詳細:
        - サブヘッダ: `54001234000000` (4Eフレーム、バイナリ: 54H 00H 12H 34H 00H 00H 00H 00H)
        - READコマンド: `0104` (バイナリ: 01H 04H)
        - サブコマンド: `0100` (ビット単位、バイナリ: 01H 00H)
        - デバイスコード: `6400` (M機器=64H、バイナリ: 64H 00H)
        - 開始デバイス番号: `00000090` (M000=0、バイナリ: 00H 00H 00H 90H)
        - デバイス点数: `E80300` (1000点=03E8H、バイナリ: E8H 03H 00H)
      - 応答データ: 125バイト（1000ビット÷8）、ASCII形式では250文字

    - **D000～D999ワード読み出し（1000点）**:
      - バイナリ形式（参考）: `54 00 12 34 00 00 00 00  01 04 00 00  A8 00 00 00 90  E8 03 00`
      - ASCII形式（実際の送信）: `"54001234000000010400A800000090E8030000"`
      - 構成詳細:
        - サブヘッダ: `54001234000000` (4Eフレーム)
        - READコマンド: `0104`
        - サブコマンド: `0000` (ワード単位)
        - デバイスコード: `A800` (D機器=A8H)
        - 開始デバイス番号: `00000090` (D000=0)
        - デバイス点数: `E80300` (1000点=03E8H)
      - 応答データ: 2000バイト（1000ワード×2）、ASCII形式では4000文字

- **TC013_BuildFrames_4Eフレーム構築**
  - 入力:
    - ConnectionConfig（FrameVersion=4E, IsBinary=true）
    - ProcessedDeviceRequestInfo（SplitDwordToWordで分割済み要求情報）
  - 期待出力: 正しいSLMPフレーム（16進数文字列、複数フレーム対応）

  - **📋 三菱電機公式リファレンスマニュアル実用例（pdf2img/sh080931q.pdf より）**:
    - **4Eフレーム構造**: サブヘッダ `54H, 00H, 12H, 34H, 00H, 00H, 00H, 00H` (バイナリ) / `"54001234000000"` (ASCII)
    - **M100～M131ワード読み出し実例**:
      - 入力: M100から2ワード読み出し要求
      - 期待フレーム: `"54001234000000"` + `"0104"` + `"00006400000090020000"`
      - 構成: サブヘッダ(54001234000000) + READコマンド(0104) + サブコマンド(0000) + デバイスコード(64=M機器) + 開始デバイス番号(100=64H) + デバイス点数(2=02H)
      - 対応応答: `34H, 12H, 02H, 00H` (2ワード分のデータ)

  - **🔧 実機テスト設定との対応（4Eフレーム/ASCII）**:
    - 実機テスト: M000-M999/D000-D999、4Eフレーム、ASCII形式
    - TC013での検証: 上記実機テスト設定のフレーム構築とASCII変換の正確性確認

- **TC014_BuildFrames_ASCIIフレーム構築**
  - 入力:
    - ConnectionConfig（IsBinary=false）
    - ProcessedDeviceRequestInfo（前処理済み要求情報）
  - 期待出力: ASCII形式のSLMPフレーム

  - **📋 三菱電機公式リファレンスマニュアル実用例（pdf2img/sh080931q.pdf より）**:
    - **ASCIIフレーム構造**: バイナリフレームの各バイトを2文字のASCII16進数文字列に変換
    - **M100～M107ビット読み出しASCII実例**:
      - 入力: M100から8ビット読み出し要求（ASCII形式）
      - バイナリ版: `01H, 04H, 01H, 00H, 64H, 00H, 00H, 90H, 08H, 00H`
      - 期待フレーム（ASCII）: `"30313034303130303634303030303930303830303030"`
      - 構成: 各バイトが2文字のASCII16進数に変換（例: 01H → "3031", 04H → "3034"）
      - 対応応答（ASCII）: `"30303031303030313131"` (00H,01H,00H,11H をASCII変換)

#### 異常系テスト
- **TC015_BuildFrames_前処理未実行**
  - 入力:
    - ConnectionConfig（有効）
    - ProcessedDeviceRequestInfo（null または未処理）
  - 期待出力: InvalidOperationException

- **TC016_BuildFrames_不正接続設定**
  - 入力:
    - ConnectionConfig（不正なFrameVersion）
    - ProcessedDeviceRequestInfo（有効）
  - 期待出力: ArgumentException

### 1.5 MultiConfigManager テスト設計
**目的**: 複数設定ファイル対応・共有データ + 軽量インスタンス アーキテクチャをテスト

#### 正常系テスト
- **TC016_1_LoadAllFromDirectoryAsync_複数ファイル正常読み込み**
  - 入力:
    - configDirectory="./test_config/"
    - ファイル構成: PLC1_settings.xlsx, PLC2_settings.xlsx, PLC3_settings.xlsx
  - 期待出力:
    - Dictionary<string, ConfigToFrameManager> (3つの軽量インスタンス)
    - LoadResult.LoadedFiles = ["PLC1_settings.xlsx", "PLC2_settings.xlsx", "PLC3_settings.xlsx"]
    - 共有データ領域に3つの設定が保存される

- **TC016_2_LoadAllFromDirectoryAsync_パターンマッチング**
  - 入力:
    - configDirectory="./test_config/"
    - filePattern="PLC*_settings.xlsx"
    - ファイル構成: PLC1_settings.xlsx, PLC2_settings.xlsx, backup.xlsx, temp.xlsx
  - 期待出力: PLC1_settings.xlsx, PLC2_settings.xlsx のみ読み込み、他は除外

- **TC016_3_CreateManagersAsync_軽量インスタンス生成**
  - 前提: LoadAllFromDirectoryAsync実行済み
  - 入力: configFileNames=["PLC1_settings.xlsx", "PLC2_settings.xlsx"]
  - 期待出力:
    - ConfigToFrameManager[] (2つの軽量インスタンス)
    - 各インスタンスは共有データを参照
    - メモリ使用量が単体インスタンスより効率的

- **TC016_4_SharedConfigData_メモリ効率確認**
  - 前提: 3つの設定ファイル読み込み済み
  - 検証項目:
    - 設定データは1つだけ（重複無し）
    - 軽量インスタンスは設定データを参照のみ
    - 総メモリ使用量 < (単体インスタンス × 3)

#### 異常系テスト
- **TC016_5_LoadAllFromDirectoryAsync_部分失敗**
  - 入力:
    - ファイル構成: PLC1_settings.xlsx(正常), PLC2_settings.xlsx(破損), PLC3_settings.xlsx(正常)
    - allowPartialSuccess=true
  - 期待出力:
    - LoadResult.LoadedFiles = ["PLC1_settings.xlsx", "PLC3_settings.xlsx"]
    - LoadResult.FailedFiles = ["PLC2_settings.xlsx"]
    - 成功した設定ファイルのみ利用可能

- **TC016_6_LoadAllFromDirectoryAsync_ディレクトリ不存在**
  - 入力: configDirectory="./not_exist/"
  - 期待出力: DirectoryNotFoundException

- **TC016_7_GetSharedConfigData_存在しない設定**
  - 入力: configFileName="NotExist_settings.xlsx"
  - 期待出力: KeyNotFoundException

#### パフォーマンステスト
- **TC016_8_Performance_複数インスタンス作成速度**
  - 検証項目: 10個の設定ファイルから軽量インスタンス生成時間 < 1秒

- **TC016_9_Performance_メモリ使用量比較**
  - 検証項目:
    - 従来方式 vs 共有データ方式のメモリ使用量比較
    - 30%以上のメモリ削減を確認

#### ReleaseSharedDataメソッドテスト
- **TC016_10_ReleaseSharedData_特定ファイル解放**
  - 入力: configFileName="PLC1_settings.xlsx"
  - 期待出力:
    - ReleasedMemoryKB（解放されたメモリ量）
    - 指定ファイルの共有データのみ解放
    - 他の設定ファイルデータは保持

- **TC016_11_ReleaseSharedData_全体解放**
  - 入力: configFileName=null（全体指定）
  - 期待出力:
    - ReleasedMemoryKB（全解放メモリ量）
    - 全共有データの解放完了
    - メモリ使用量の大幅削減確認

- **TC016_12_ReleaseSharedData_長時間稼働最適化**
  - 前提: 8時間以上の連続稼働状態
  - 手順:
    1. 複数設定ファイル読み込み
    2. 長時間稼働シミュレーション
    3. ReleaseSharedData実行
  - 期待出力: メモリ最適化効果の確認

---

## テストデータ
- **appsettings.xlsx**: 正常系テスト用設定ファイル（M001-M999, D001-D999対応）
- **invalid_settings.xlsx**: 異常系テスト用設定ファイル
- **sample_slmp_frames.txt**: 既知のSLMPフレームサンプル（DWord分割対応）

## 📋 公式リファレンスデータ（三菱電機公式SLMP仕様書）
- **参照元**: `C:\Users\1010821\Desktop\python\andon\pdf2img\sh080931q.pdf`
- **三菱電機公式SLMPリファレンスマニュアル**: 98ページの詳細な技術仕様書
- **TC012～TC014で活用する既知の入力出力例**:
  - **M100～M107ビット読み出し通信例**:
    - 要求: `01H, 04H, 01H, 00H, 64H, 00H, 00H, 90H, 08H, 00H`
    - 応答: `00H, 01H, 00H, 11H` (M100=OFF, M101=ON, M102-M106=OFF, M107=ON)
  - **M100～M131ワード読み出し通信例**:
    - 要求: `01H, 04H, 00H, 00H, 64H, 00H, 00H, 90H, 02H, 00H`
    - 応答: `34H, 12H, 02H, 00H` (2ワード分のデータ)
  - **3E/4Eフレーム構造詳細**: 第4章「伝文フォーマット」
  - **READコマンド(0401)詳細**: 第5章「コマンド」
- **テスト品質向上**: 実際のPLC通信で使用される標準パターンによる検証
- **トレーサビリティ**: 公式仕様書への直接的な参照
