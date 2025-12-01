using Andon.Core.Constants;
using Andon.Core.Models;

namespace Andon.Utilities;

/// <summary>
/// SLMPフレーム構築ユーティリティ
/// </summary>
public class SlmpFrameBuilder
{
    // ========== クラスレベル定数・フィールド ==========

    /// <summary>
    /// シーケンス番号管理（Phase1で実装済み）
    /// </summary>
    private static readonly Andon.Core.Managers.SequenceNumberManager _sequenceManager = new();

    /// <summary>
    /// SLMP最大フレーム長（PySLMPClientから採用）
    /// </summary>
    private const int MAX_FRAME_LENGTH = 8194;

    /// <summary>
    /// ReadRandom非対応デバイス（PySLMPClientから採用・改善）
    /// </summary>
    private static readonly DeviceCode[] _unsupportedDevicesForReadRandom = new[]
    {
        DeviceCode.TS,  // タイマ接点
        DeviceCode.TC,  // タイマコイル
        DeviceCode.CS,  // カウンタ接点
        DeviceCode.CC   // カウンタコイル
    };

    /// <summary>
    /// ReadRandom(0x0403)要求フレームを構築
    /// </summary>
    /// <param name="devices">読み出すデバイスのリスト</param>
    /// <param name="frameType">フレームタイプ（3E/4E）</param>
    /// <param name="timeout">監視タイマ（250ms単位、デフォルト8秒=32）</param>
    /// <returns>送信用バイト配列</returns>
    /// <exception cref="ArgumentException">デバイスリストが空、または無効なパラメータの場合</exception>
    public static byte[] BuildReadRandomRequest(
        List<DeviceSpecification>? devices,
        string frameType = "3E",
        ushort timeout = 32)
    {
        // 1. 入力検証（andon強化版 + PySLMPClient要素）
        ValidateInputs(devices, frameType);

        // 2. フレーム構築
        var frame = new List<byte>();

        // 2-1. ヘッダ構築（ConMoni方式 + PySLMPClient自動管理）
        ushort sequenceNumber = _sequenceManager.GetNext(frameType);
        frame.AddRange(BuildSubHeader(frameType, sequenceNumber));

        // 2-2. ネットワーク設定構築（ConMoni明確な構造）
        frame.AddRange(BuildNetworkConfig());

        // 2-3. データ長プレースホルダ
        int dataLengthPosition = frame.Count;
        frame.AddRange(new byte[] { 0x00, 0x00 });

        // 2-4. コマンド部構築（PySLMPClient一括処理スタイル）
        frame.AddRange(BuildCommandSection(
            timeout,
            0x0403,  // ReadRandom
            0x0000,  // サブコマンド
            (byte)devices!.Count,
            0x00     // Dword点数=0固定
        ));

        // 2-5. デバイス指定部構築（ConMoni方式）
        frame.AddRange(BuildDeviceSpecificationSection(devices));

        // 2-6. データ長更新（PySLMPClient計算式 + ConMoni実装）
        UpdateDataLength(frame, dataLengthPosition, frameType);

        // 2-7. フレーム検証（PySLMPClientから採用）
        ValidateFrame(frame.ToArray());

        return frame.ToArray();
    }

    /// <summary>
    /// ReadRandom(0x0403)要求フレームを構築（ASCII形式）
    /// Binary形式を構築後、16進数文字列（ASCII）に変換
    /// </summary>
    /// <param name="devices">読み出すデバイスのリスト</param>
    /// <param name="frameType">フレームタイプ（3E/4E）</param>
    /// <param name="timeout">監視タイマ（250ms単位、デフォルト8秒=32）</param>
    /// <returns>16進数文字列（大文字、スペースなし）</returns>
    /// <exception cref="ArgumentException">デバイスリストが空、または無効なパラメータの場合</exception>
    /// <remarks>
    /// アプローチ: Binary→ASCII変換
    /// - Binary実装を再利用し、コード重複を避ける
    /// - Binary実装との整合性が自動的に保証される
    /// - Convert.ToHexString()を使用（.NET 5.0以上）
    /// </remarks>
    public static string BuildReadRandomRequestAscii(
        List<DeviceSpecification>? devices,
        string frameType = "3E",
        ushort timeout = 32)
    {
        // Binary形式フレームを構築
        byte[] binaryFrame = BuildReadRandomRequest(devices, frameType, timeout);

        // Binaryを16進数文字列（ASCII）に変換
        // Convert.ToHexString()は大文字で返す
        return Convert.ToHexString(binaryFrame);
    }


    // ========== プライベートメソッド（機能別分割） ==========

    /// <summary>
    /// 入力パラメータを検証します。
    /// andon既存 + PySLMPClient要素強化
    /// </summary>
    /// <param name="devices">デバイスリスト</param>
    /// <param name="frameType">フレームタイプ</param>
    private static void ValidateInputs(
        List<DeviceSpecification>? devices,
        string frameType)
    {
        // 1. デバイスリスト基本検証（既存）
        if (devices == null || devices.Count == 0)
        {
            throw new ArgumentException(
                "デバイスリストが空です",
                nameof(devices));
        }

        // 2. デバイス点数上限チェック（既存）
        if (devices.Count > 255)
        {
            throw new ArgumentException(
                $"デバイス点数が上限を超えています: {devices.Count}点（最大255点）",
                nameof(devices));
        }

        // 3. フレームタイプ検証（既存）
        if (frameType != "3E" && frameType != "4E")
        {
            throw new ArgumentException(
                $"未対応のフレームタイプ: {frameType}",
                nameof(frameType));
        }

        // 4. ReadRandom対応デバイスチェック（★新規追加）
        foreach (var device in devices)
        {
            if (_unsupportedDevicesForReadRandom.Contains(device.Code))
            {
                throw new ArgumentException(
                    $"ReadRandomコマンドは {device.Code} デバイスに対応していません。" +
                    $"対応していないデバイス: {string.Join(", ", _unsupportedDevicesForReadRandom)}",
                    nameof(devices));
            }
        }
    }

    /// <summary>
    /// サブヘッダを構築します。
    /// PySLMPClientのシーケンス番号対応
    /// </summary>
    /// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
    /// <param name="sequenceNumber">シーケンス番号（4Eの場合）</param>
    /// <returns>サブヘッダバイト配列</returns>
    private static byte[] BuildSubHeader(string frameType, ushort sequenceNumber)
    {
        if (frameType == "3E")
        {
            // 標準3Eフレーム（フレーム構築方法.md準拠）
            return new byte[] { 0x50, 0x00 };
        }
        else // "4E"
        {
            // 4Eフレーム（シーケンス番号含む）
            var header = new List<byte>();
            header.AddRange(new byte[] { 0x54, 0x00 });              // サブヘッダ
            header.AddRange(BitConverter.GetBytes(sequenceNumber));  // シーケンス番号（LE）
            header.AddRange(new byte[] { 0x00, 0x00 });              // 予約
            return header.ToArray();
        }
    }

    /// <summary>
    /// ネットワーク設定部を構築します。
    /// ConMoniの明確な構造を採用
    /// </summary>
    /// <returns>ネットワーク設定バイト配列（5バイト）</returns>
    private static byte[] BuildNetworkConfig()
    {
        var config = new List<byte>();
        config.Add(0x00);        // ネットワーク番号（自ネットワーク）
        config.Add(0xFF);        // 局番（全局）
        config.AddRange(BitConverter.GetBytes((ushort)0x03FF));  // I/O番号（LE）
        config.Add(0x00);        // マルチドロップ局番（未使用）
        return config.ToArray();
    }

    /// <summary>
    /// コマンド部を構築します。
    /// PySLMPClientの一括処理スタイル
    /// </summary>
    /// <param name="timeout">監視タイマ（250ms単位）</param>
    /// <param name="command">コマンド（例: 0x0403 = ReadRandom）</param>
    /// <param name="subCommand">サブコマンド（例: 0x0000 = ワード単位）</param>
    /// <param name="wordCount">ワード点数</param>
    /// <param name="dwordCount">Dword点数（常に0）</param>
    /// <returns>コマンド部バイト配列（8バイト）</returns>
    private static byte[] BuildCommandSection(
        ushort timeout,
        ushort command,
        ushort subCommand,
        byte wordCount,
        byte dwordCount)
    {
        var section = new List<byte>();
        section.AddRange(BitConverter.GetBytes(timeout));     // 監視タイマ（2バイトLE）
        section.AddRange(BitConverter.GetBytes(command));     // コマンド（2バイトLE）
        section.AddRange(BitConverter.GetBytes(subCommand));  // サブコマンド（2バイトLE）
        section.Add(wordCount);                               // ワード点数（1バイト）
        section.Add(dwordCount);                              // Dword点数（1バイト、常に0）
        return section.ToArray();
    }

    /// <summary>
    /// デバイス指定部を構築します。
    /// ConMoni方式（各デバイス4バイト）
    /// </summary>
    /// <param name="devices">デバイス指定リスト</param>
    /// <returns>デバイス指定部バイト配列（4バイト×デバイス数）</returns>
    private static byte[] BuildDeviceSpecificationSection(
        List<DeviceSpecification> devices)
    {
        var section = new List<byte>();

        foreach (var device in devices)
        {
            // デバイス番号（3バイト、リトルエンディアン）
            section.Add((byte)(device.DeviceNumber & 0xFF));           // 下位バイト
            section.Add((byte)((device.DeviceNumber >> 8) & 0xFF));    // 中位バイト
            section.Add((byte)((device.DeviceNumber >> 16) & 0xFF));   // 上位バイト

            // デバイスコード（1バイト）
            section.Add((byte)device.Code);
        }

        return section.ToArray();
    }

    /// <summary>
    /// データ長フィールドを更新します。
    /// PySLMPClientの明快な計算 + ConMoniの動的更新
    /// </summary>
    /// <param name="frame">フレームバイト配列</param>
    /// <param name="dataLengthPosition">データ長フィールドの位置</param>
    /// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
    private static void UpdateDataLength(
        List<byte> frame,
        int dataLengthPosition,
        string frameType)
    {
        // データ長 = データ長フィールドの次のバイト(監視タイマ)から最後までのバイト数
        // 3E: 監視タイマ(2) + コマンド(2) + サブコマンド(2) + Word点数(1) + Dword点数(1) + デバイス指定部(4 * デバイス数)
        // 4E: 監視タイマ(2) + コマンド(2) + サブコマンド(2) + Word点数(1) + Dword点数(1) + デバイス指定部(4 * デバイス数)

        // データ長 = データ長フィールドの次のバイト以降の全バイト数
        int dataLength = frame.Count - (dataLengthPosition + 2);

        // リトルエンディアンで書き込み
        frame[dataLengthPosition] = (byte)(dataLength & 0xFF);
        frame[dataLengthPosition + 1] = (byte)((dataLength >> 8) & 0xFF);
    }

    /// <summary>
    /// 完成したフレームを検証します。
    /// PySLMPClientから採用
    /// </summary>
    /// <param name="frame">フレームバイト配列</param>
    private static void ValidateFrame(byte[] frame)
    {
        if (frame.Length > MAX_FRAME_LENGTH)
        {
            throw new InvalidOperationException(
                $"フレーム長が上限を超えています: {frame.Length}バイト（最大{MAX_FRAME_LENGTH}バイト）");
        }

        if (frame.Length == 0)
        {
            throw new InvalidOperationException("フレームが空です");
        }
    }
}
