namespace Andon.Core.Constants;

/// <summary>
/// SLMP関連定数
/// </summary>
public static class SlmpConstants
{
    #region フレーム形式定数

    /// <summary>
    /// 3Eフレーム形式識別子
    /// </summary>
    public const string Frame3E = "3E";

    /// <summary>
    /// 4Eフレーム形式識別子
    /// </summary>
    public const string Frame4E = "4E";

    /// <summary>
    /// デフォルトフレーム形式（3E）
    /// </summary>
    public const string DefaultFrameType = Frame3E;

    #endregion

    #region ヘッダーサイズ定数

    /// <summary>
    /// 3Eフレームヘッダーサイズ（バイト）
    /// </summary>
    public const int Frame3EHeaderSize = 15;

    /// <summary>
    /// 4Eフレームヘッダーサイズ（バイト）
    /// </summary>
    public const int Frame4EHeaderSize = 13;

    #endregion

    #region データ形式定数

    /// <summary>
    /// バイナリデータ形式
    /// </summary>
    public const string DataFormatBinary = "Binary";

    /// <summary>
    /// ASCIIデータ形式
    /// </summary>
    public const string DataFormatAscii = "ASCII";

    /// <summary>
    /// デフォルトデータ形式（Binary）
    /// </summary>
    public const string DefaultDataFormat = DataFormatBinary;

    #endregion

    #region 正常終了コード

    /// <summary>
    /// SLMP正常終了コード
    /// </summary>
    public const ushort NormalEndCode = 0x0000;

    #endregion
}


/// <summary>
/// SLMP固定通信設定（Phase1）
/// memo.md送信フレーム仕様準拠
/// </summary>
public static class SlmpFixedSettings
{
    // ========================================
    // フレーム・プロトコル設定
    // ========================================

    /// <summary>
    /// フレームバージョン（4E）
    /// </summary>
    public const string FrameVersion = "4E";

    /// <summary>
    /// 通信プロトコル（UDP）
    /// </summary>
    public const string Protocol = "UDP";

    // ========================================
    // 通信対象設定（memo.mdフレームから抽出）
    // ========================================

    /// <summary>
    /// ネットワーク番号
    /// </summary>
    public const byte NetworkNumber = 0x00;

    /// <summary>
    /// 局番（全局指定）
    /// </summary>
    public const byte StationNumber = 0xFF;

    /// <summary>
    /// I/O番号
    /// </summary>
    public const ushort IoNumber = 0x03FF;

    /// <summary>
    /// マルチドロップ局番
    /// </summary>
    public const byte MultiDropStation = 0x00;

    // ========================================
    // タイムアウト設定
    // ========================================

    /// <summary>
    /// 監視タイマ（32 = 8秒）
    /// </summary>
    public const ushort MonitorTimer = 0x0020;

    /// <summary>
    /// 受信タイムアウト（ミリ秒）
    /// </summary>
    public const int ReceiveTimeoutMs = 500;

    // ========================================
    // コマンド設定
    // ========================================

    /// <summary>
    /// コマンド（ReadRandom）
    /// </summary>
    public const ushort Command = 0x0403;

    /// <summary>
    /// サブコマンド
    /// </summary>
    public const ushort SubCommand = 0x0000;

    // ========================================
    // フレームヘッダ固定値
    // ========================================

    /// <summary>
    /// 4Eフレーム サブヘッダ
    /// </summary>
    public static readonly byte[] SubHeader_4E = { 0x54, 0x00 };

    /// <summary>
    /// シーケンス番号（予約）
    /// </summary>
    public static readonly byte[] Serial = { 0x00, 0x00 };

    /// <summary>
    /// 予約領域
    /// </summary>
    public static readonly byte[] Reserved = { 0x00, 0x00 };

    // ========================================
    // フレーム構築メソッド
    // ========================================

    /// <summary>
    /// 固定設定でフレームヘッダを構築
    /// memo.md送信フレーム仕様準拠
    /// </summary>
    /// <param name="dataLength">データ長（バイト）</param>
    /// <returns>19バイトのフレームヘッダ</returns>
    public static byte[] BuildFrameHeader(int dataLength)
    {
        var header = new List<byte>();

        // サブヘッダ (0-1)
        header.AddRange(SubHeader_4E);

        // シリアル (2-3)、予約 (4-5)
        header.AddRange(Serial);
        header.AddRange(Reserved);

        // ネットワーク番号 (6)
        header.Add(NetworkNumber);

        // 局番 (7)
        header.Add(StationNumber);

        // I/O番号 (8-9) リトルエンディアン
        header.AddRange(BitConverter.GetBytes(IoNumber));

        // マルチドロップ (10)
        header.Add(MultiDropStation);

        // データ長 (11-12) リトルエンディアン
        header.AddRange(BitConverter.GetBytes((ushort)dataLength));

        // 監視タイマ (13-14) リトルエンディアン
        header.AddRange(BitConverter.GetBytes(MonitorTimer));

        // コマンド (15-16) リトルエンディアン
        header.AddRange(BitConverter.GetBytes(Command));

        // サブコマンド (17-18) リトルエンディアン
        header.AddRange(BitConverter.GetBytes(SubCommand));

        return header.ToArray();
    }
}
