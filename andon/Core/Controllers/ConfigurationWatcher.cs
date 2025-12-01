using Andon.Core.Interfaces;

namespace Andon.Core.Controllers;

/// <summary>
/// 設定ファイル変更イベント引数
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    /// <summary>
    /// 変更されたファイルのパス
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// 設定ファイル変更監視・動的再読み込み
/// Phase3 Part7: IConfigurationWatcherインターフェース実装
/// </summary>
public class ConfigurationWatcher : IConfigurationWatcher, IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly Dictionary<string, DateTime> _lastEventTimes = new();
    private readonly TimeSpan _debounceInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 設定ファイル変更イベント
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? OnConfigurationChanged;

    /// <summary>
    /// 監視中かどうか
    /// </summary>
    public bool IsWatching => _watcher?.EnableRaisingEvents ?? false;

    /// <summary>
    /// 設定ファイル監視を開始する（JSON形式）
    /// </summary>
    /// <param name="configDirectory">設定ファイルディレクトリパス</param>
    public void StartWatching(string configDirectory)
    {
        if (_watcher != null)
        {
            StopWatching();
        }

        _watcher = new FileSystemWatcher(configDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.json"
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Excel設定ファイル監視を開始する（Excel形式: *.xlsx）
    /// Phase3 Part7実装
    /// </summary>
    /// <param name="configDirectory">設定ファイルディレクトリパス</param>
    public void StartWatchingExcel(string configDirectory)
    {
        if (_watcher != null)
        {
            StopWatching();
        }

        _watcher = new FileSystemWatcher(configDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = "*.xlsx"
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// 設定ファイル監視を停止する
    /// </summary>
    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnFileChanged;
            _watcher.Created -= OnFileChanged;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    /// <summary>
    /// ファイル変更イベントハンドラー（デバウンス処理付き）
    /// </summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var now = DateTime.Now;
        var filePath = e.FullPath;

        // デバウンス処理: 最後のイベントから一定時間以内の重複イベントを無視
        lock (_lastEventTimes)
        {
            if (_lastEventTimes.TryGetValue(filePath, out var lastTime))
            {
                if (now - lastTime < _debounceInterval)
                {
                    return; // 重複イベントを無視
                }
            }

            _lastEventTimes[filePath] = now;
        }

        OnConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
        {
            FilePath = filePath
        });
    }

    /// <summary>
    /// リソース解放
    /// </summary>
    public void Dispose()
    {
        StopWatching();
    }
}
