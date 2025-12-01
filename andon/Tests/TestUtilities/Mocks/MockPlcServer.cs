using System.Net;
using System.Net.Sockets;
using System.Text;
using Andon.Core.Models;

namespace Andon.Tests.TestUtilities.Mocks;

/// <summary>
/// TC025テスト用MockPLCサーバー
/// 4Eフレーム形式でのレスポンス送信をサポート
/// </summary>
public class MockPlcServer
{
    private string _frameHeader = "D4001234"; // デフォルト4Eフレームヘッダー
    private byte[] _responseData = Array.Empty<byte>();
    private bool _isConfigured = false;

    /// <summary>
    /// 4Eフレーム識別ヘッダーを設定
    /// </summary>
    /// <param name="frameHeader">フレームヘッダー（16進数文字列）</param>
    public void SetResponse4EFrame(string frameHeader)
    {
        _frameHeader = frameHeader;
        _isConfigured = true;
    }

    /// <summary>
    /// M000-M999読み込み応答データを設定
    /// </summary>
    public void SetM000ToM999ReadResponse()
    {
        // memo.mdの実際のPLC受信データ(111バイト)を使用
        // 4Eフレーム構造:
        // - フレーム識別: "D4000000" (8バイト) ← 実データに合わせて修正
        // - シーケンス番号等: "00000000FFFF0300" (16バイト)
        // - データ長: "6200" (98バイト = 0x0062) Little Endian
        // - 終了コード: "0000" (正常終了)
        // - デバイスデータ部: 96バイト

        // memo.md Line 58の実データから正確に作成 (111バイト = 222文字)
        string actualResponseHex = "D4000000000000FFFF030062000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0719FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00100008000100100010000820001000080002000000000000000000000000000000000000000000000000000000000000000000000000000000";

        // 16進数文字列をバイト配列に変換
        _responseData = ConvertHexStringToBytes(actualResponseHex);
        _isConfigured = true;

        // デバッグ出力とバリデーション
        Console.WriteLine($"[MockPlcServer] SetM000ToM999ReadResponse: {_responseData.Length}バイト ({actualResponseHex.Length}文字) 設定完了");

        // バリデーション: 111バイト(222文字)であることを確認
        if (_responseData.Length != 111)
        {
            throw new InvalidOperationException($"MockPlcServer応答データ長エラー: 期待=111バイト, 実際={_responseData.Length}バイト");
        }
        if (actualResponseHex.Length != 222)
        {
            throw new InvalidOperationException($"MockPlcServer応答Hex文字列長エラー: 期待=222文字, 実際={actualResponseHex.Length}文字");
        }
    }

    /// <summary>
    /// 4E ASCII形式のReadRandomレスポンスを設定
    /// Binary形式(111バイト)をASCII形式(222文字)に変換して設定
    /// </summary>
    public void SetReadRandomResponse4EAscii()
    {
        // 既存のBinary応答データ（SetM000ToM999ReadResponse）をASCII形式に変換
        // Binary: 111バイト → ASCII: 222文字（各バイトを2文字の16進数で表現）

        // memo.mdの実際のPLC受信データ（111バイト）をASCII形式で表現
        // 4E ASCII形式フレーム構造（documents/design/フレーム構築関係/フレーム構築方法.md L.100-114）:
        //   Idx 0-1:   "D4"     (サブヘッダ、2文字)
        //   Idx 2-3:   "00"     (予約1、2文字)
        //   Idx 4-7:   "0000"   (シーケンス番号、4文字)
        //   Idx 8-11:  "0000"   (予約2、4文字)
        //   Idx 12-13: "00"     (ネットワーク番号、2文字)
        //   Idx 14-15: "FF"     (PC番号、2文字)
        //   Idx 16-19: "FF03"   (I/O番号、4文字、LE)
        //   Idx 20-21: "00"     (局番、2文字)
        //   Idx 22-25: "6200"   (データ長=98バイト、4文字、LE)
        //   Idx 26-29: "0000"   (終了コード、4文字)
        //   Idx 30~:   データ部 (192文字 = 96バイト×2)
        //   合計: 30 + 192 = 222文字

        string asciiResponse = "D4000000000000FFFF030062000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0719FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00100008000100100010000820001000080002000000000000000000000000000000000000000000000000000000000000000000000000000000";

        // バリデーション: 222文字であることを確認
        if (asciiResponse.Length != 222)
        {
            throw new InvalidOperationException(
                $"4E ASCII応答データ長エラー: 期待=222文字, 実際={asciiResponse.Length}文字"
            );
        }

        // ASCII文字列をバイト配列に変換（各ASCII文字が1バイト）
        byte[] responseBytes = Encoding.ASCII.GetBytes(asciiResponse);
        _responseData = responseBytes;
        _isConfigured = true;

        Console.WriteLine($"[MockPlcServer] SetReadRandomResponse4EAscii: {responseBytes.Length}バイト ({asciiResponse.Length}文字ASCII) 設定完了");
    }

    /// <summary>
    /// 3E ASCII形式のReadRandomレスポンスを設定
    /// 4E ASCII応答から3E ASCII応答を生成
    /// </summary>
    public void SetReadRandomResponse3EAscii()
    {
        // 3E ASCII形式フレーム構造（documents/design/フレーム構築関係/フレーム構築方法.md L.61-72）:
        //   Idx 0-1:   "D0"     (サブヘッダ、2文字)
        //   Idx 2-3:   "00"     (ネットワーク番号、2文字)
        //   Idx 4-5:   "FF"     (PC番号、2文字)
        //   Idx 6-9:   "FF03"   (I/O番号、4文字、LE)
        //   Idx 10-11: "00"     (局番、2文字)
        //   Idx 12-15: "6200"   (データ長=98バイト、4文字、LE)
        //   Idx 16-19: "0000"   (終了コード、4文字)
        //   Idx 20~:   データ部 (192文字 = 96バイト×2)
        //   合計: 20 + 192 = 212文字

        // 4E ASCII応答から3E ASCII応答を生成
        string ascii4E = "D4000000000000FFFF030062000000FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF0719FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF00100008000100100010000820001000080002000000000000000000000000000000000000000000000000000000000000000000000000000000";

        // 4Eヘッダ（30文字）を3Eヘッダ（20文字）に変換
        string ascii3EHeader = "D0" +                          // サブヘッダ (2文字) ← 4E "D4"を"D0"に
                               ascii4E.Substring(12, 2) +      // ネットワーク番号 (2文字) ← 4E Idx12-13
                               ascii4E.Substring(14, 2) +      // PC番号 (2文字) ← 4E Idx14-15
                               ascii4E.Substring(16, 4) +      // I/O番号 (4文字) ← 4E Idx16-19
                               ascii4E.Substring(20, 2) +      // 局番 (2文字) ← 4E Idx20-21
                               ascii4E.Substring(22, 4) +      // データ長 (4文字) ← 4E Idx22-25
                               ascii4E.Substring(26, 4);       // 終了コード (4文字) ← 4E Idx26-29

        // データ部（4E Idx30以降）を結合
        string ascii3EResponse = ascii3EHeader + ascii4E.Substring(30);

        // バリデーション: 212文字であることを確認
        if (ascii3EResponse.Length != 212)
        {
            throw new InvalidOperationException(
                $"3E ASCII応答データ長エラー: 期待=212文字, 実際={ascii3EResponse.Length}文字"
            );
        }

        // ASCII文字列をバイト配列に変換
        byte[] responseBytes = Encoding.ASCII.GetBytes(ascii3EResponse);
        _responseData = responseBytes;
        _isConfigured = true;

        Console.WriteLine($"[MockPlcServer] SetReadRandomResponse3EAscii: {responseBytes.Length}バイト ({ascii3EResponse.Length}文字ASCII) 設定完了");
    }

    /// <summary>
    /// TC121用：完全なSLMP応答データを設定（3EフレームBinary形式）
    /// D100～D109までの10個のワードデバイスの読み取り応答を設定
    /// 注意: TC121はIsBinary=falseだが、PlcCommunicationManagerがBinaryに変換するため、ここではBinary形式で応答
    /// </summary>
    public void SetCompleteReadResponse()
    {
        // 3EフレームBinary応答データ（D100-D109読み取り）
        // フォーマット: サブヘッダ(2) + ネットワーク(1) + PC(1) + I/O(2) + 局番(1) + データ長(2) + 終了コード(2) + デバイスデータ
        // データ長 = 終了コード(2バイト) + デバイスデータ(20バイト) = 22バイト = 0x0016
        var responseBytes = new byte[]
        {
            0xD0, 0x00,              // サブヘッダ（3EフレームBinary応答）
            0x00,                    // ネットワーク番号
            0xFF,                    // PC番号
            0xFF, 0x03,              // I/O番号（リトルエンディアン: 0x03FF）
            0x00,                    // 局番
            0x16, 0x00,              // データ長（22バイト = 終了コード2 + デバイスデータ20）
            0x00, 0x00,              // 終了コード（正常終了）
            // デバイスデータ（D100-D109の値: 各ワード2バイト、リトルエンディアン）
            0x39, 0x30,              // D100 = 12345 = 0x3039（LE）
            0x00, 0x00,              // D101 = 0
            0x02, 0x15,              // D102 = 5378 = 0x1502（LE）
            0xFC, 0x0E,              // D103 = 3836 = 0x0EFC（LE）
            0x01, 0x00,              // D104 = 1（品質ステータス）
            0x00, 0x00,              // D105 = 0
            0x00, 0x00,              // D106 = 0
            0x00, 0x00,              // D107 = 0
            0x00, 0x00,              // D108 = 0
            0x00, 0x00               // D109 = 0
        };

        _responseData = responseBytes;
        _isConfigured = true;
        Console.WriteLine($"[MockPlcServer] 完全読み取り応答データを設定しました: {responseBytes.Length}バイト（Binary形式）");
    }

    /// <summary>
    /// TC121用：DWord結合テスト用データを設定
    /// D100(12345) + D101(0) → D100_32bit = 12345
    /// D102(5378) + D103(3836) → D102_32bit = 251527298 (0x0EFC1502)
    /// </summary>
    public void SetDWordCombineTestData()
    {
        // SetCompleteReadResponseと同じデータを使用
        // D100=12345, D101=0 → 32bit値: 12345
        // D102=5378, D103=3836 → 32bit値: 3836*65536 + 5378 = 251527298
        SetCompleteReadResponse();
        Console.WriteLine("[MockPlcServer] DWord結合テスト用データを設定しました");
    }

    /// <summary>
    /// 応答データを直接byte[]で設定（テスト用）
    /// </summary>
    /// <param name="responseData">応答データ（byte配列）</param>
    public void SetResponseData(byte[] responseData)
    {
        _responseData = responseData;
        _isConfigured = true;
        Console.WriteLine($"[MockPlcServer] 応答データを直接設定しました: {responseData.Length}バイト");
    }

    /// <summary>
    /// 応答データを設定（内部メソッド）
    /// </summary>
    private void SetResponseData(string responseHex, FrameType frameType)
    {
        _responseData = ConvertHexStringToBytes(responseHex);
        _frameHeader = frameType == FrameType.Frame4E ? "D4001234" : "D0001000";
        _isConfigured = true;
    }

    /// <summary>
    /// 設定された応答データを取得
    /// </summary>
    /// <returns>バイトデータ</returns>
    public byte[] GetResponseData()
    {
        if (!_isConfigured)
        {
            throw new InvalidOperationException("MockPlcServerが未設定です。SetResponse4EFrame()またはSetM000ToM999ReadResponse()を呼び出してください。");
        }
        return _responseData;
    }

    /// <summary>
    /// 応答データの16進数文字列表現を取得
    /// </summary>
    /// <returns>16進数文字列</returns>
    public string GetResponseHex()
    {
        return Convert.ToHexString(GetResponseData());
    }

    /// <summary>
    /// MockSocketに応答データを設定
    /// </summary>
    /// <param name="mockSocket">設定対象のMockSocket</param>
    public void ConfigureMockSocket(MockSocket mockSocket)
    {
        var responseData = GetResponseData();
        mockSocket.SetReceiveData(responseData);
    }

    /// <summary>
    /// 16進数文字列をバイト配列に変換
    /// </summary>
    /// <param name="hexString">16進数文字列</param>
    /// <returns>バイト配列</returns>
    private static byte[] ConvertHexStringToBytes(string hexString)
    {
        if (string.IsNullOrEmpty(hexString))
            return Array.Empty<byte>();

        // 文字数が奇数の場合は先頭に0を追加
        if (hexString.Length % 2 != 0)
            hexString = "0" + hexString;

        byte[] bytes = new byte[hexString.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>
    /// フレームタイプを判定
    /// </summary>
    /// <returns>フレームタイプ</returns>
    public Andon.Core.Models.FrameType GetFrameType()
    {
        return _frameHeader.StartsWith("D4001234")
            ? Andon.Core.Models.FrameType.Frame4E
            : Andon.Core.Models.FrameType.Frame3E;
    }
}