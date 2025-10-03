using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// PLC接続診断サービスインターフェース
    /// SOLID原則: インターフェース分離原則適用
    /// 依存性逆転原則による抽象化定義
    /// </summary>
    public interface IConnectionDiagnostic
    {
        /// <summary>
        /// Q00CPU対応UDP接続診断
        /// </summary>
        Task<NetworkConnectivityResult> TestQ00CpuNetworkConnectivityAsync(Q00CpuNetworkDiagnosticConfig config);

        /// <summary>
        /// SLMPフレーム解析診断
        /// </summary>
        Task<SlmpFrameAnalysisResult> TestSlmpFrameAnalysisAsync(ISlmpClient slmpClient, SlmpFrameAnalysisConfig config);

        /// <summary>
        /// Q00CPUデバイス実在性確認
        /// </summary>
        Task<DeviceAccessibilityResult> TestQ00CpuDeviceAccessibilityAsync(ISlmpClient slmpClient, Q00CpuDeviceDiagnosticConfig config);

        /// <summary>
        /// ハイブリッド統合ログ出力
        /// </summary>
        Task WriteHybridDiagnosticLogAsync(CompleteDiagnosticResult diagnosticResult);

        /// <summary>
        /// Q00CPU通信品質統計測定
        /// </summary>
        Task<CommunicationQualityResult> MeasureQ00CpuCommunicationQualityAsync(ISlmpClient slmpClient, CommunicationQualityConfig config);

        /// <summary>
        /// Q00CPU診断レポート生成
        /// </summary>
        Task<Q00CpuDiagnosticReport> GenerateQ00CpuDiagnosticReportAsync(CompleteDiagnosticResult diagnosticResult);
    }
}