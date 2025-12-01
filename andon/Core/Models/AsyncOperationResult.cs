using System;
using System.Collections.Generic;

namespace Andon.Core.Models;

/// <summary>
/// 非同期処理実行結果
/// </summary>
public class AsyncOperationResult<T>
{
    /// <summary>
    /// 処理成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 処理結果データ
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 失敗したステップ
    /// </summary>
    public string? FailedStep { get; set; }

    /// <summary>
    /// 発生した例外
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// 各ステップの実行結果
    /// </summary>
    public Dictionary<string, object> StepResults { get; set; } = new();

    /// <summary>
    /// 処理開始時刻
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 処理終了時刻
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 処理実行時間
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : TimeSpan.Zero;

    /// <summary>
    /// リソース解放完了フラグ
    /// </summary>
    public bool ResourcesReleased { get; set; }

    /// <summary>
    /// エラー詳細情報
    /// </summary>
    public ErrorDetails? ErrorDetails { get; set; }

    public AsyncOperationResult()
    {
        StartTime = DateTime.Now;
    }
}

/// <summary>
/// エラー詳細情報クラス
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// エラータイプ
    /// </summary>
    public string ErrorType { get; set; } = string.Empty;

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// エラー発生時刻
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// 失敗した操作名
    /// </summary>
    public string FailedOperation { get; set; } = string.Empty;

    /// <summary>
    /// 追加情報
    /// </summary>
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}
