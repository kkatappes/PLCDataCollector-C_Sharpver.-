using Microsoft.Extensions.Logging;
using Andon.Core.Models.ConfigModels;

namespace Andon.Core.Managers;

/// <summary>
/// 複数PLC設定管理クラス
/// 複数のExcelファイルから読み込んだ設定を一元管理
/// </summary>
public class MultiPlcConfigManager
{
    private readonly Dictionary<string, PlcConfiguration> _configs;
    private readonly ILogger<MultiPlcConfigManager> _logger;

    public MultiPlcConfigManager(ILogger<MultiPlcConfigManager> logger)
    {
        _logger = logger;
        _configs = new Dictionary<string, PlcConfiguration>();
    }

    /// <summary>
    /// 設定を追加
    /// </summary>
    public void AddConfiguration(PlcConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        string configName = config.ConfigurationName;

        if (_configs.ContainsKey(configName))
        {
            _logger.LogWarning($"設定が既に存在します。上書きします: {configName}");
        }

        _configs[configName] = config;
        _logger.LogInformation($"設定を追加: {configName}（デバイス数: {config.Devices.Count}）");
    }

    /// <summary>
    /// 複数の設定を一括追加
    /// </summary>
    public void AddConfigurations(IEnumerable<PlcConfiguration> configs)
    {
        if (configs == null)
            throw new ArgumentNullException(nameof(configs));

        foreach (var config in configs)
        {
            AddConfiguration(config);
        }

        _logger.LogInformation($"全設定追加完了: {_configs.Count}件");
    }

    /// <summary>
    /// 名前で設定を取得
    /// </summary>
    public PlcConfiguration GetConfiguration(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            throw new ArgumentException("設定名が指定されていません", nameof(configName));

        if (!_configs.TryGetValue(configName, out var config))
        {
            throw new KeyNotFoundException($"設定が見つかりません: {configName}");
        }

        return config;
    }

    /// <summary>
    /// 設定の存在確認
    /// </summary>
    public bool HasConfiguration(string configName)
    {
        return !string.IsNullOrWhiteSpace(configName) &&
               _configs.ContainsKey(configName);
    }

    /// <summary>
    /// 全設定を取得
    /// </summary>
    public IReadOnlyList<PlcConfiguration> GetAllConfigurations()
    {
        return _configs.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// 全設定名を取得
    /// </summary>
    public IReadOnlyList<string> GetAllConfigurationNames()
    {
        return _configs.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 設定数を取得
    /// </summary>
    public int GetConfigurationCount()
    {
        return _configs.Count;
    }

    /// <summary>
    /// 設定をクリア
    /// </summary>
    public void Clear()
    {
        int count = _configs.Count;
        _configs.Clear();
        _logger.LogInformation($"全設定をクリア: {count}件");
    }

    /// <summary>
    /// 特定の設定を削除
    /// </summary>
    public bool RemoveConfiguration(string configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
            return false;

        bool removed = _configs.Remove(configName);
        if (removed)
        {
            _logger.LogInformation($"設定を削除: {configName}");
        }

        return removed;
    }

    /// <summary>
    /// 統計情報を取得
    /// </summary>
    public ConfigurationStatistics GetStatistics()
    {
        var stats = new ConfigurationStatistics
        {
            TotalConfigurations = _configs.Count,
            TotalDevices = _configs.Values.Sum(c => c.Devices.Count),
            ConfigurationDetails = _configs.Values.Select(c => new ConfigDetail
            {
                Name = c.ConfigurationName,
                IpAddress = c.IpAddress,
                Port = c.Port,
                DeviceCount = c.Devices.Count,
                PlcModel = c.PlcModel
            }).ToList()
        };

        return stats;
    }
}

/// <summary>
/// 設定統計情報
/// </summary>
public class ConfigurationStatistics
{
    public int TotalConfigurations { get; set; }
    public int TotalDevices { get; set; }
    public List<ConfigDetail> ConfigurationDetails { get; set; } = new();
}

/// <summary>
/// 設定詳細情報
/// </summary>
public class ConfigDetail
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public int DeviceCount { get; set; }
    public string PlcModel { get; set; } = string.Empty;
}
