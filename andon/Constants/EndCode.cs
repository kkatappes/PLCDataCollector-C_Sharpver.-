using System;
using System.Collections.Generic;

namespace SlmpClient.Constants
{
    /// <summary>
    /// SLMP 終了コード定義
    /// Python: const.EndCode (39項目)
    /// </summary>
    public enum EndCode : ushort
    {
        #region 基本エラー (Basic Errors)
        
        /// <summary>正常終了</summary>
        Success = 0x0000,
        
        /// <summary>コマンド異常</summary>
        WrongCommand = 0xC059,
        
        /// <summary>フォーマット異常</summary>
        WrongFormat = 0xC05C,
        
        /// <summary>長さ異常</summary>
        WrongLength = 0xC061,
        
        /// <summary>処理方式異常</summary>
        WrongMethod = 0xC05F,
        
        /// <summary>同一データ</summary>
        SameData = 0xC060,
        
        #endregion
        
        #region 通信エラー (Communication Errors)
        
        /// <summary>ビジー状態</summary>
        Busy = 0xCEE0,
        
        /// <summary>要求データ長超過</summary>
        ExceedReqLength = 0xCEE1,
        
        /// <summary>応答データ長超過</summary>
        ExceedRespLength = 0xCEE2,
        
        /// <summary>サーバーが見つからない</summary>
        ServerNotFound = 0xCF10,
        
        /// <summary>設定項目異常</summary>
        WrongConfigItem = 0xCF20,
        
        /// <summary>パラメータIDが見つからない</summary>
        PrmIDNotFound = 0xCF30,
        
        /// <summary>排他書き込み未開始</summary>
        NotStartExclusiveWrite = 0xCF31,
        
        /// <summary>中継異常</summary>
        RelayFailure = 0xCF70,
        
        /// <summary>タイムアウトエラー</summary>
        TimeoutError = 0xCF71,
        
        #endregion
        
        #region CANアプリケーション関連エラー (CAN Application Errors)
        
        /// <summary>CANアプリ 読み取り許可なし</summary>
        CANAppNotPermittedRead = 0xCCC7,
        
        /// <summary>CANアプリ 書き込み専用</summary>
        CANAppWriteOnly = 0xCCC8,
        
        /// <summary>CANアプリ 読み取り専用</summary>
        CANAppReadOnly = 0xCCC9,
        
        /// <summary>CANアプリ 未定義オブジェクトアクセス</summary>
        CANAppUndefinedObjectAccess = 0xCCCA,
        
        /// <summary>CANアプリ PDOマッピング不許可</summary>
        CANAppNotPermittedPDOMapping = 0xCCCB,
        
        /// <summary>CANアプリ PDOマッピング超過</summary>
        CANAppExceedPDOMapping = 0xCCCC,
        
        /// <summary>CANアプリ サブインデックス存在なし</summary>
        CANAppNotExistSubIndex = 0xCCD3,
        
        /// <summary>CANアプリ パラメータ異常</summary>
        CANAppWrongParameter = 0xCCD4,
        
        /// <summary>CANアプリ パラメータ範囲上限超過</summary>
        CANAppMoreOverParameterRange = 0xCCD5,
        
        /// <summary>CANアプリ パラメータ範囲下限超過</summary>
        CANAppLessOverParameterRange = 0xCCD6,
        
        /// <summary>CANアプリ 転送・格納エラー</summary>
        CANAppTransOrStoreError = 0xCCDA,
        
        /// <summary>CANアプリ その他エラー</summary>
        CANAppOtherError = 0xCCFF,
        
        #endregion
        
        #region その他ネットワークエラー (Other Network Errors)
        
        /// <summary>その他のネットワークエラー</summary>
        OtherNetworkError = 0xCF00,
        
        /// <summary>データフラグメント不足</summary>
        DataFragmentShortage = 0xCF40,
        
        /// <summary>データフラグメント重複</summary>
        DataFragmentDup = 0xCF41,
        
        /// <summary>データフラグメント喪失</summary>
        DataFragmentLost = 0xCF43,
        
        /// <summary>データフラグメント非サポート</summary>
        DataFragmentNotSupport = 0xCF44,
        
        #endregion
    }
    
    /// <summary>
    /// EndCode拡張メソッド
    /// エラーコード毎の再試行可否マトリクス
    /// </summary>
    public static class EndCodeExtensions
    {
        /// <summary>
        /// 再試行推奨エラーコード（一時的エラー）
        /// </summary>
        private static readonly HashSet<EndCode> RetryableErrors = new()
        {
            EndCode.Busy,
            EndCode.TimeoutError,
            EndCode.RelayFailure,
            EndCode.ServerNotFound
        };
        
        /// <summary>
        /// 遅延後再試行推奨エラーコード
        /// </summary>
        private static readonly HashSet<EndCode> RetryWithDelayErrors = new()
        {
            EndCode.ServerNotFound,
            EndCode.OtherNetworkError
        };
        
        /// <summary>
        /// 再試行非推奨エラーコード（設定エラー）
        /// </summary>
        private static readonly HashSet<EndCode> NonRetryableErrors = new()
        {
            EndCode.WrongCommand,
            EndCode.WrongFormat,
            EndCode.WrongLength,
            EndCode.WrongMethod,
            EndCode.ExceedReqLength,
            EndCode.ExceedRespLength,
            EndCode.WrongConfigItem,
            EndCode.PrmIDNotFound,
            EndCode.NotStartExclusiveWrite
        };
        
        /// <summary>
        /// エラーコードが再試行可能かどうかを判定
        /// </summary>
        /// <param name="endCode">エラーコード</param>
        /// <returns>再試行可能な場合はtrue</returns>
        public static bool IsRetryable(this EndCode endCode)
        {
            return RetryableErrors.Contains(endCode);
        }
        
        /// <summary>
        /// エラーコードが遅延後再試行推奨かどうかを判定
        /// </summary>
        /// <param name="endCode">エラーコード</param>
        /// <returns>遅延後再試行推奨の場合はtrue</returns>
        public static bool IsRetryWithDelay(this EndCode endCode)
        {
            return RetryWithDelayErrors.Contains(endCode);
        }
        
        /// <summary>
        /// エラーコードが再試行非推奨かどうかを判定
        /// </summary>
        /// <param name="endCode">エラーコード</param>
        /// <returns>再試行非推奨の場合はtrue</returns>
        public static bool IsNonRetryable(this EndCode endCode)
        {
            return NonRetryableErrors.Contains(endCode);
        }
        
        /// <summary>
        /// エラーコードが成功を表すかどうかを判定
        /// </summary>
        /// <param name="endCode">エラーコード</param>
        /// <returns>成功の場合はtrue</returns>
        public static bool IsSuccess(this EndCode endCode)
        {
            return endCode == EndCode.Success;
        }
        
        /// <summary>
        /// エラーコードの重要度を取得
        /// </summary>
        /// <param name="endCode">エラーコード</param>
        /// <returns>エラーの重要度</returns>
        public static ErrorSeverity GetSeverity(this EndCode endCode)
        {
            return endCode switch
            {
                EndCode.Success => ErrorSeverity.None,
                EndCode.Busy or EndCode.TimeoutError => ErrorSeverity.Warning,
                EndCode.RelayFailure or EndCode.ServerNotFound => ErrorSeverity.Error,
                EndCode.WrongCommand or EndCode.WrongFormat or EndCode.WrongLength => ErrorSeverity.Critical,
                _ => ErrorSeverity.Error
            };
        }
        
        /// <summary>
        /// エラーコードから日本語メッセージを取得
        /// </summary>
        /// <param name="endCode">エラーコード</param>
        /// <returns>日本語エラーメッセージ</returns>
        public static string GetJapaneseMessage(this EndCode endCode)
        {
            return endCode switch
            {
                EndCode.Success => "正常終了",
                EndCode.WrongCommand => "コマンド異常",
                EndCode.WrongFormat => "フォーマット異常",
                EndCode.WrongLength => "長さ異常",
                EndCode.WrongMethod => "処理方式異常",
                EndCode.SameData => "同一データ",
                EndCode.Busy => "ビジー状態",
                EndCode.ExceedReqLength => "要求データ長超過",
                EndCode.ExceedRespLength => "応答データ長超過",
                EndCode.ServerNotFound => "サーバーが見つからない",
                EndCode.WrongConfigItem => "設定項目異常",
                EndCode.PrmIDNotFound => "パラメータIDが見つからない",
                EndCode.NotStartExclusiveWrite => "排他書き込み未開始",
                EndCode.RelayFailure => "中継異常",
                EndCode.TimeoutError => "タイムアウトエラー",
                EndCode.CANAppNotPermittedRead => "CANアプリ 読み取り許可なし",
                EndCode.CANAppWriteOnly => "CANアプリ 書き込み専用",
                EndCode.CANAppReadOnly => "CANアプリ 読み取り専用",
                EndCode.CANAppUndefinedObjectAccess => "CANアプリ 未定義オブジェクトアクセス",
                EndCode.CANAppNotPermittedPDOMapping => "CANアプリ PDOマッピング不許可",
                EndCode.CANAppExceedPDOMapping => "CANアプリ PDOマッピング超過",
                EndCode.CANAppNotExistSubIndex => "CANアプリ サブインデックス存在なし",
                EndCode.CANAppWrongParameter => "CANアプリ パラメータ異常",
                EndCode.CANAppMoreOverParameterRange => "CANアプリ パラメータ範囲上限超過",
                EndCode.CANAppLessOverParameterRange => "CANアプリ パラメータ範囲下限超過",
                EndCode.CANAppTransOrStoreError => "CANアプリ 転送・格納エラー",
                EndCode.CANAppOtherError => "CANアプリ その他エラー",
                EndCode.OtherNetworkError => "その他のネットワークエラー",
                EndCode.DataFragmentShortage => "データフラグメント不足",
                EndCode.DataFragmentDup => "データフラグメント重複",
                EndCode.DataFragmentLost => "データフラグメント喪失",
                EndCode.DataFragmentNotSupport => "データフラグメント非サポート",
                _ => $"未定義エラー (0x{(ushort)endCode:X4})"
            };
        }
    }
    
    /// <summary>
    /// エラーの重要度
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>エラーなし</summary>
        None,
        
        /// <summary>警告</summary>
        Warning,
        
        /// <summary>エラー</summary>
        Error,
        
        /// <summary>致命的エラー</summary>
        Critical
    }
}