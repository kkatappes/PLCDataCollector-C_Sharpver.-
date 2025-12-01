using Andon.Core.Controllers;

namespace Andon.Core.Interfaces;

/// <summary>
/// 設定監視インターフェース
/// Phase3 Part7実装
/// </summary>
public interface IConfigurationWatcher
{
    /// <summary>
    /// 設定ファイル変更イベント
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? OnConfigurationChanged;

    /// <summary>
    /// 監視中かどうか
    /// </summary>
    bool IsWatching { get; }

    /// <summary>
    /// JSON設定ファイル監視を開始する
    /// </summary>
    /// <param name="configDirectory">設定ファイルディレクトリパス</param>
    void StartWatching(string configDirectory);

    /// <summary>
    /// Excel設定ファイル監視を開始する
    /// </summary>
    /// <param name="configDirectory">設定ファイルディレクトリパス</param>
    void StartWatchingExcel(string configDirectory);

    /// <summary>
    /// 設定ファイル監視を停止する
    /// </summary>
    void StopWatching();
}
