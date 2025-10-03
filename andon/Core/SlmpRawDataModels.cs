using System;

namespace SlmpClient.Core
{
    /// <summary>
    /// SLMPフレームデータモデル
    /// SLMP生データ記録用のデータ構造
    /// </summary>
    public class SlmpFrameData
    {
        /// <summary>送信フレーム（バイナリ）</summary>
        public byte[] RequestFrame { get; set; } = Array.Empty<byte>();

        /// <summary>受信フレーム（バイナリ）</summary>
        public byte[] ResponseFrame { get; set; } = Array.Empty<byte>();

        /// <summary>デバイスアドレス</summary>
        public string DeviceAddress { get; set; } = string.Empty;

        /// <summary>操作タイプ</summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>応答時間（ミリ秒）</summary>
        public double ResponseTimeMs { get; set; }

        /// <summary>操作成功フラグ</summary>
        public bool Success { get; set; }

        /// <summary>読み取り値（オプション）</summary>
        public object? ReadValue { get; set; }

        /// <summary>エラーメッセージ（失敗時）</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// SLMPバッチ操作データモデル
    /// 複数デバイス一括操作の効率記録用
    /// </summary>
    public class SlmpBatchOperationData
    {
        /// <summary>操作タイプ</summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>対象デバイスアドレス配列</summary>
        public string[] DeviceAddresses { get; set; } = Array.Empty<string>();

        /// <summary>総応答時間（ミリ秒）</summary>
        public double TotalResponseTimeMs { get; set; }

        /// <summary>個別応答時間配列</summary>
        public double[] IndividualResponseTimes { get; set; } = Array.Empty<double>();

        /// <summary>バッチ効率説明</summary>
        public string BatchEfficiency { get; set; } = string.Empty;

        /// <summary>パフォーマンス利益</summary>
        public string PerformanceBenefit { get; set; } = string.Empty;

        /// <summary>操作成功フラグ</summary>
        public bool Success { get; set; }

        /// <summary>読み取り値配列（成功時）</summary>
        public object[]? ReadValues { get; set; }

        /// <summary>エラーメッセージ（失敗時）</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// SLMP詳細フレーム解析結果
    /// </summary>
    public class SlmpDetailedFrameAnalysis
    {
        /// <summary>サブヘッダー</summary>
        public string SubHeader { get; set; } = string.Empty;

        /// <summary>サブヘッダー説明</summary>
        public string SubHeaderDescription { get; set; } = string.Empty;

        /// <summary>エンドコード</summary>
        public string EndCode { get; set; } = string.Empty;

        /// <summary>エンドコード説明</summary>
        public string EndCodeDescription { get; set; } = string.Empty;

        /// <summary>データ型解析</summary>
        public string DataTypeAnalysis { get; set; } = string.Empty;

        /// <summary>フレーム形式</summary>
        public string FrameFormat { get; set; } = string.Empty;
    }
}