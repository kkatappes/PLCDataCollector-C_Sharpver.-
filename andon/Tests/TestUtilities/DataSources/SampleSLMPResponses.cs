namespace Andon.Tests.TestUtilities.DataSources;

/// <summary>
/// SLMPレスポンスのサンプルデータを提供するクラス
/// TC119統合テスト用のレスポンスデータを定義
/// </summary>
public static class SampleSLMPResponses
{
    /// <summary>
    /// M000-M999読み取り応答（ReadRandom形式: 1000デバイス = 2000バイト）
    /// 正常応答ヘッダ + 2000バイトのワードデータ（各ビットを2バイトワードで表現）
    /// Phase13: ReadRandom(0x0403)形式に対応
    /// </summary>
    public static readonly byte[] M000_M999_ResponseBytes = GenerateM000M999Response();

    /// <summary>
    /// D000-D999読み取り応答（ReadRandom形式: 1000デバイス = 2000バイト）
    /// 正常応答ヘッダ + 2000バイトのワードデータ
    /// Phase13: ReadRandom(0x0403)形式に対応（既に正しい形式）
    /// </summary>
    public static readonly byte[] D000_D999_ResponseBytes = GenerateD000D999Response();

    /// <summary>
    /// M000-M999応答の16進文字列表現
    /// </summary>
    public static string M000_M999_ResponseHex => BitConverter.ToString(M000_M999_ResponseBytes).Replace("-", "");

    /// <summary>
    /// D000-D999応答の16進文字列表現
    /// </summary>
    public static string D000_D999_ResponseHex => BitConverter.ToString(D000_D999_ResponseBytes).Replace("-", "");

    /// <summary>
    /// M000-M999読み取り応答を生成（4Eフレーム、ReadRandom形式）
    /// フォーマット: サブヘッダ(2) + シーケンス番号(2) + 予約(2) + ネットワーク番号(1) + PC番号(1) +
    ///              要求先ユニットI/O番号(2) + 要求先ユニット局番号(1) + 応答データ長(2) + 終了コード(2) + ワードデータ(2000)
    /// Phase13: ReadRandom(0x0403)形式対応 - 各ビットを2バイトワードで表現
    /// </summary>
    public static byte[] GenerateM000M999Response()
    {
        var response = new List<byte>();

        // サブヘッダ (2バイト) - 4E応答 (0xD4 0x00)
        response.AddRange(new byte[] { 0xD4, 0x00 });

        // シーケンス番号 (2バイト)
        response.AddRange(new byte[] { 0x01, 0x00 });

        // 予約 (2バイト)
        response.AddRange(new byte[] { 0x00, 0x00 });

        // ネットワーク番号 (1バイト)
        response.Add(0x00);

        // PC番号 (1バイト)
        response.Add(0xFF);

        // 要求先ユニットI/O番号 (2バイト)
        response.AddRange(new byte[] { 0xFF, 0x03 });

        // 要求先ユニット局番号 (1バイト)
        response.Add(0x00);

        // Phase13修正: ReadRandom(0x0403)形式に変更
        // 応答データ長 (2バイト) - 終了コード(2) + ワードデータ(2000) = 2002バイト (リトルエンディアン)
        // 各ビットを2バイトワードで表現: 1000デバイス × 2バイト = 2000バイト
        response.AddRange(new byte[] { 0xD2, 0x07 }); // 0x07D2 = 2002

        // 終了コード (2バイト) - 0000 (正常終了)
        response.AddRange(new byte[] { 0x00, 0x00 });

        // Phase13修正: ReadRandom形式のワードデータ (2000バイト = 1000ワード)
        // 各ビットデバイスを2バイトワードで表現
        // テストデータ: 交互に0と1のパターン (M0=1, M1=0, M2=1, M3=0, ...)
        for (int i = 0; i < 1000; i++)
        {
            // 奇数番号=1(ON), 偶数番号=0(OFF)のパターン
            ushort value = (ushort)(i % 2 == 0 ? 1 : 0);
            response.Add((byte)(value & 0xFF));        // 下位バイト
            response.Add((byte)((value >> 8) & 0xFF)); // 上位バイト
        }

        return response.ToArray();
    }

    /// <summary>
    /// D000-D999読み取り応答を生成（4Eフレーム）
    /// フォーマット: サブヘッダ(2) + シーケンス番号(2) + 予約(2) + ネットワーク番号(1) + PC番号(1) +
    ///              要求先ユニットI/O番号(2) + 要求先ユニット局番号(1) + 応答データ長(2) + 終了コード(2) + ワードデータ(2000)
    /// </summary>
    public static byte[] GenerateD000D999Response()
    {
        var response = new List<byte>();

        // サブヘッダ (2バイト) - 4E応答 (0xD4 0x00)
        response.AddRange(new byte[] { 0xD4, 0x00 });

        // シーケンス番号 (2バイト)
        response.AddRange(new byte[] { 0x01, 0x00 });

        // 予約 (2バイト)
        response.AddRange(new byte[] { 0x00, 0x00 });

        // ネットワーク番号 (1バイト)
        response.Add(0x00);

        // PC番号 (1バイト)
        response.Add(0xFF);

        // 要求先ユニットI/O番号 (2バイト)
        response.AddRange(new byte[] { 0xFF, 0x03 });

        // 要求先ユニット局番号 (1バイト)
        response.Add(0x00);

        // 応答データ長 (2バイト) - 終了コード(2) + ワードデータ(2000) = 2002バイト (リトルエンディアン)
        response.AddRange(new byte[] { 0xD2, 0x07 });

        // 終了コード (2バイト) - 0000 (正常終了)
        response.AddRange(new byte[] { 0x00, 0x00 });

        // ワードデータ (2000バイト = 1000ワード)
        // テストデータ: 連番パターン (0x0000, 0x0001, 0x0002, ...)
        for (int i = 0; i < 1000; i++)
        {
            // リトルエンディアンで格納
            ushort value = (ushort)i;
            response.Add((byte)(value & 0xFF));        // 下位バイト
            response.Add((byte)((value >> 8) & 0xFF)); // 上位バイト
        }

        return response.ToArray();
    }

    /// <summary>
    /// 指定したデバイス数のM機器応答を生成（カスタムサイズ用）
    /// </summary>
    public static byte[] GenerateMDeviceResponse(int deviceCount)
    {
        var response = new List<byte>();

        // ヘッダ部分
        response.AddRange(new byte[] { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 });

        // データ長（ビットデータのバイト数）
        int byteCount = (deviceCount + 7) / 8;
        response.AddRange(new byte[] { (byte)(byteCount & 0xFF), (byte)((byteCount >> 8) & 0xFF) });

        // 終了コード
        response.AddRange(new byte[] { 0x00, 0x00 });

        // ビットデータ
        for (int i = 0; i < byteCount; i++)
        {
            response.Add((byte)(i % 2 == 0 ? 0x55 : 0xAA));
        }

        return response.ToArray();
    }

    /// <summary>
    /// 指定したデバイス数のD機器応答を生成（カスタムサイズ用）
    /// </summary>
    public static byte[] GenerateDDeviceResponse(int deviceCount)
    {
        var response = new List<byte>();

        // ヘッダ部分
        response.AddRange(new byte[] { 0xD0, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00 });

        // データ長（ワードデータのバイト数）
        int byteCount = deviceCount * 2;
        response.AddRange(new byte[] { (byte)(byteCount & 0xFF), (byte)((byteCount >> 8) & 0xFF) });

        // 終了コード
        response.AddRange(new byte[] { 0x00, 0x00 });

        // ワードデータ
        for (int i = 0; i < deviceCount; i++)
        {
            ushort value = (ushort)i;
            response.Add((byte)(value & 0xFF));
            response.Add((byte)((value >> 8) & 0xFF));
        }

        return response.ToArray();
    }
}
