using Microsoft.Extensions.Configuration;
using Andon.Core.Models.ConfigModels;

namespace Andon.Infrastructure.Configuration;

/// <summary>
/// 設定ファイル読み込みマネージャー
/// </summary>
public class ConfigurationLoader
{
    private readonly IConfiguration _configuration;

    public ConfigurationLoader(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// PLC接続設定を読み込み
    /// </summary>
    /// <returns>TargetDeviceConfig</returns>
    /// <exception cref="InvalidOperationException">設定が不正な場合</exception>
    public TargetDeviceConfig LoadPlcConnectionConfig()
    {
        var config = new TargetDeviceConfig();

        // 基本設定の読み込み
        var connectionSection = _configuration.GetSection("PlcCommunication:Connection");
        config.FrameType = connectionSection["FrameVersion"] ?? "4E";

        // タイムアウト設定（ReceiveTimeoutMsをSLMPタイムアウトに変換: ms / 250）
        var timeoutsSection = _configuration.GetSection("PlcCommunication:Timeouts");
        var receiveTimeoutMs = int.Parse(timeoutsSection["ReceiveTimeoutMs"] ?? "8000");
        config.Timeout = (ushort)(receiveTimeoutMs / 250);

        // Devicesリストの読み込み
        var devicesSection = _configuration.GetSection("PlcCommunication:TargetDevices:Devices");
        if (devicesSection.Exists() && devicesSection.GetChildren().Any())
        {
            config.Devices = devicesSection.Get<List<DeviceEntry>>() ?? new List<DeviceEntry>();
        }

        // 検証
        ValidateConfig(config);

        return config;
    }

    /// <summary>
    /// 設定の検証
    /// </summary>
    /// <param name="config">検証対象の設定</param>
    /// <exception cref="InvalidOperationException">設定が不正な場合</exception>
    private void ValidateConfig(TargetDeviceConfig config)
    {
        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new InvalidOperationException(
                "デバイスリストが空です。appsettings.jsonの\"PlcCommunication:TargetDevices:Devices\"を設定してください。"
            );
        }

        if (config.Devices.Count > 255)
        {
            throw new InvalidOperationException(
                $"デバイス点数が上限を超えています: {config.Devices.Count}点（最大255点）"
            );
        }

        // フレームタイプの検証
        if (config.FrameType != "3E" && config.FrameType != "4E")
        {
            throw new InvalidOperationException(
                $"未対応のフレームタイプ: {config.FrameType}（有効な値: \"3E\", \"4E\"）"
            );
        }

        // 各デバイスの検証
        foreach (var deviceEntry in config.Devices)
        {
            // DeviceEntryをDeviceSpecificationに変換して検証
            var deviceSpec = deviceEntry.ToDeviceSpecification();

            // ReadRandom対応チェック
            deviceSpec.ValidateForReadRandom();

            // デバイス番号範囲チェック
            deviceSpec.ValidateDeviceNumberRange();
        }
    }
}
