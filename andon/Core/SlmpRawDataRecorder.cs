using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMP生データ記録システム
    /// TDD Red-Green-Refactor サイクルで実装
    /// SOLID原則適用: 依存性注入とファサードパターンで責任分離
    /// </summary>
    public class SlmpRawDataRecorder : ISlmpRawDataRecorder
    {
        private readonly ILogger<SlmpRawDataRecorder> _logger;
        private readonly string _logFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public SlmpRawDataRecorder(ILogger<SlmpRawDataRecorder> logger, string logFilePath)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrEmpty(logFilePath))
                throw new ArgumentException("Log file path cannot be null or empty", nameof(logFilePath));

            _logFilePath = logFilePath;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            // ログディレクトリを作成
            var logDir = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
        }

        /// <summary>
        /// SLMPフレームを16進ダンプとして記録
        /// GREEN Phase: 詳細フレーム解析機能を追加
        /// </summary>
        public async Task RecordSlmpFrameAsync(SlmpFrameData frameData)
        {
            // GREEN: 詳細フレーム解析を追加
            var frameAnalysis = AnalyzeSlmpFrame(frameData.ResponseFrame);

            var logEntry = new
            {
                EntryType = "SLMP_RAW_DATA",
                Timestamp = DateTime.Now,
                RequestFrameHex = ConvertToHexString(frameData.RequestFrame),
                ResponseFrameHex = ConvertToHexString(frameData.ResponseFrame),
                DeviceAddress = frameData.DeviceAddress,
                OperationType = frameData.OperationType,
                ResponseTimeMs = frameData.ResponseTimeMs,
                Success = frameData.Success,
                ReadValue = frameData.ReadValue,
                DetailedFrameAnalysis = frameAnalysis.FrameFormat,
                SubHeader = frameAnalysis.SubHeader,
                EndCode = frameAnalysis.EndCode,
                DataTypeAnalysis = frameAnalysis.DataTypeAnalysis
            };

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
            await File.WriteAllTextAsync(_logFilePath, json);
        }

        /// <summary>
        /// SLMPフレームを解析して詳細情報を取得
        /// </summary>
        private static SlmpDetailedFrameAnalysis AnalyzeSlmpFrame(byte[] responseFrame)
        {
            if (responseFrame == null || responseFrame.Length < 7)
            {
                return new SlmpDetailedFrameAnalysis
                {
                    SubHeader = "Unknown",
                    EndCode = "Unknown",
                    DataTypeAnalysis = "Insufficient data",
                    FrameFormat = "Invalid frame"
                };
            }

            // 基本的なSLMPフレーム解析
            var subHeader = BitConverter.ToUInt16(responseFrame, 0);
            var endCode = responseFrame.Length >= 11 ? BitConverter.ToUInt16(responseFrame, 9) : (ushort)0;

            return new SlmpDetailedFrameAnalysis
            {
                SubHeader = $"0x{subHeader:X4}",
                SubHeaderDescription = subHeader == 0x00D0 ? "4E Binary Response" : "Unknown Response Type",
                EndCode = $"0x{endCode:X4}",
                EndCodeDescription = endCode == 0x0000 ? "Success" : "Error",
                DataTypeAnalysis = responseFrame.Length > 11 ? "Contains data" : "Header only",
                FrameFormat = "SLMP 4E Binary"
            };
        }

        /// <summary>
        /// バイト配列を16進文字列に変換
        /// </summary>
        private static string ConvertToHexString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            return string.Join(" ", bytes.Select(b => $"{b:X2}"));
        }

        /// <summary>
        /// SLMPバッチ操作効率を記録
        /// GREEN Phase: バッチ操作効率記録機能を実装
        /// </summary>
        public async Task RecordBatchOperationAsync(SlmpBatchOperationData batchData)
        {
            // GREEN: バッチ操作効率記録機能を実装
            var logEntry = new
            {
                EntryType = "SLMP_BATCH_OPERATION",
                Timestamp = DateTime.Now,
                OperationType = batchData.OperationType,
                DeviceAddresses = batchData.DeviceAddresses,
                TotalResponseTimeMs = batchData.TotalResponseTimeMs,
                IndividualResponseTimes = batchData.IndividualResponseTimes,
                BatchEfficiency = batchData.BatchEfficiency,
                PerformanceBenefit = batchData.PerformanceBenefit,
                Success = batchData.Success,
                ReadValues = batchData.ReadValues,
                ErrorMessage = batchData.ErrorMessage
            };

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
            await File.WriteAllTextAsync(_logFilePath, json);
        }

        /// <summary>
        /// SLMP通信統計を記録
        /// RED Phase: 最小限の実装（テスト失敗させる）
        /// </summary>
        public async Task RecordCommunicationStatisticsAsync(string sessionId, SlmpCommunicationStatistics statistics)
        {
            // RED: 意図的に実装しない（テストを失敗させるため）
            await Task.CompletedTask;
        }

        /// <summary>
        /// リソースを非同期的に解放
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            // RED: 最小限の実装
            await Task.CompletedTask;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを同期的に解放
        /// </summary>
        public void Dispose()
        {
            // RED: 最小限の実装
            GC.SuppressFinalize(this);
        }
    }
}