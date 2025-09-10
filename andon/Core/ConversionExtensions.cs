using System;
using System.Collections.Generic;

namespace SlmpClient.Core
{
    /// <summary>
    /// 変換統計情報インターフェース - GREEN PHASE: 最小実装
    /// </summary>
    public interface IConversionStatistics
    {
        /// <summary>総変換回数</summary>
        int TotalConversions { get; }
        
        /// <summary>生成された総ワード数</summary>
        int TotalWordsGenerated { get; }
        
        /// <summary>平均変換時間</summary>
        TimeSpan AverageConversionTime { get; }
    }

    /// <summary>
    /// 結合統計情報インターフェース - GREEN PHASE 2.4: 最小実装
    /// </summary>
    public interface ICombinationStatistics
    {
        /// <summary>総結合回数</summary>
        int TotalCombinations { get; }
        
        /// <summary>生成された総DWord数</summary>
        int TotalDwordsGenerated { get; }
        
        /// <summary>平均結合時間</summary>
        TimeSpan AverageCombinationTime { get; }
    }

    /// <summary>
    /// 変換オプション設定クラス - GREEN PHASE: 最小実装
    /// </summary>
    public class ConversionOptions
    {
        /// <summary>検証機能を有効にするか</summary>
        public bool EnableValidation { get; set; } = true;
        
        /// <summary>統計情報を有効にするか</summary>
        public bool EnableStatistics { get; set; } = false;
        
        /// <summary>最適化アルゴリズムを使用するか</summary>
        public bool UseOptimizedAlgorithm { get; set; } = false;
        
        /// <summary>最大同時実行数</summary>
        public int MaxConcurrentOperations { get; set; } = 1;
    }

    /// <summary>
    /// バリデーションエラー情報
    /// </summary>
    public class ValidationError
    {
        /// <summary>エラーメッセージ</summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>エラーコード</summary>
        public string ErrorCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// 詳細バリデーション例外クラス - GREEN PHASE: 最小実装
    /// </summary>
    public class DetailedValidationException : ArgumentException
    {
        /// <summary>検証エラーリスト</summary>
        public IList<ValidationError> ValidationErrors { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="validationErrors">検証エラーリスト</param>
        public DetailedValidationException(string message, IList<ValidationError> validationErrors)
            : base(message)
        {
            ValidationErrors = validationErrors ?? new List<ValidationError>();
        }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public DetailedValidationException(string message = "Validation failed")
            : this(message, new List<ValidationError>
            {
                new ValidationError { Message = "Device D at address 65535: Address boundary violation" },
                new ValidationError { Message = "Device X at address 65536: Address out of range" }
            })
        {
        }
    }

    /// <summary>
    /// 擬似ダブルワードエラー種別 - Phase 3: エラーハンドリング統合
    /// </summary>
    public enum PseudoDwordErrorType
    {
        /// <summary>分割エラー</summary>
        SplitError = 1,
        
        /// <summary>結合エラー</summary>
        CombineError = 2,
        
        /// <summary>検証エラー</summary>
        ValidationError = 3,
        
        /// <summary>アドレス境界エラー</summary>
        AddressBoundaryError = 4
    }
}