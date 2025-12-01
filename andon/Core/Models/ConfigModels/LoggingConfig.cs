namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// ログ設定
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// ログレベル（Information, Debug, Warning, Error等）
    /// </summary>
    public string LogLevel { get; init; } = "Information";

    /// <summary>
    /// ファイル出力有効フラグ
    /// </summary>
    public bool EnableFileOutput { get; init; } = true;

    /// <summary>
    /// コンソール出力有効フラグ
    /// </summary>
    public bool EnableConsoleOutput { get; init; } = true;

    /// <summary>
    /// ログファイルパス
    /// </summary>
    public string LogFilePath { get; init; } = "logs/andon.log";

    /// <summary>
    /// ログファイルの最大サイズ（MB）
    /// </summary>
    public int MaxLogFileSizeMb { get; init; } = 10;

    /// <summary>
    /// 保持するログファイルの最大数
    /// </summary>
    public int MaxLogFileCount { get; init; } = 7;

    /// <summary>
    /// 日付ベースのローテーションを有効化
    /// </summary>
    public bool EnableDateBasedRotation { get; init; } = false;
}
