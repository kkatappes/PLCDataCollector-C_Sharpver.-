using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;

namespace Andon.Core.Interfaces;

/// <summary>
/// PLC通信インターフェース (最重要)
/// </summary>
public interface IPlcCommunicationManager
{
    /// <summary>
    /// PLC接続
    /// </summary>
    /// <returns>接続結果</returns>
    Task<ConnectionResponse> ConnectAsync();

    /// <summary>
    /// SLMPフレーム送信
    /// </summary>
    /// <param name="frameHexString">送信するフレーム（16進数文字列）</param>
    Task SendFrameAsync(string frameHexString);

    /// <summary>
    /// SLMPレスポンス受信
    /// </summary>
    /// <returns>受信データ（16進数文字列）</returns>
    Task<string> ReceiveFrameAsync();

    /// <summary>
    /// PLCからのSLMPレスポンス受信（生データ取得）
    /// </summary>
    /// <param name="receiveTimeoutMs">受信タイムアウト時間（ミリ秒）</param>
    /// <returns>受信データの詳細情報を含むRawResponseData</returns>
    Task<RawResponseData> ReceiveResponseAsync(int receiveTimeoutMs);

    /// <summary>
    /// PLC切断
    /// </summary>
    Task<DisconnectResult> DisconnectAsync();

    /// <summary>
    /// 受信した生データを処理して基本処理済み応答データを生成
    /// Step6データ処理の基本後処理機能
    /// </summary>
    /// <param name="rawData">受信した生データ</param>
    /// <param name="processedRequestInfo">前処理済み要求情報</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>基本処理済み応答データ</returns>
    Task<BasicProcessedResponseData> ProcessReceivedRawData(
        byte[] rawData,
        ProcessedDeviceRequestInfo processedRequestInfo,
        CancellationToken cancellationToken = default);

    // Phase3.5で削除されたインターフェース定義 (2025-11-27)
    // CombineDwordData: DWord結合処理メソッド定義 - 削除理由: DWord結合機能廃止
    // 削除行数: 12行 (53-64行目)

    /// <summary>
    /// DWord結合済みデータから構造化データへの解析
    /// Step6データ処理の第3段階（最終段階）として、3Eフレーム解析による構造化データ生成
    /// </summary>
    /// <param name="processedData">DWord結合済み処理データ</param>
    /// <param name="processedRequestInfo">前処理済み要求情報（構造化設定含む）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>構造化データ</returns>
    Task<StructuredData> ParseRawToStructuredData(
        ProcessedResponseData processedData,
        ProcessedDeviceRequestInfo processedRequestInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Step3-5完全サイクル実行（接続→送受信→切断）
    /// </summary>
    /// <param name="connectionConfig">接続設定</param>
    /// <param name="timeoutConfig">タイムアウト設定</param>
    /// <param name="sendFrame">送信フレーム</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>サイクル実行結果</returns>
    Task<CycleExecutionResult> ExecuteStep3to5CycleAsync(
        ConnectionConfig connectionConfig,
        TimeoutConfig timeoutConfig,
        byte[] sendFrame,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Step3-6完全サイクル実行（接続→送受信→データ処理→切断）
    /// </summary>
    /// <param name="connectionConfig">接続設定</param>
    /// <param name="timeoutConfig">タイムアウト設定</param>
    /// <param name="sendFrame">送信フレーム</param>
    /// <param name="processedRequestInfo">前処理済み要求情報</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>完全サイクル実行結果</returns>
    Task<FullCycleExecutionResult> ExecuteFullCycleAsync(
        ConnectionConfig connectionConfig,
        TimeoutConfig timeoutConfig,
        byte[] sendFrame,
        ProcessedDeviceRequestInfo processedRequestInfo,
        CancellationToken cancellationToken = default);
}
