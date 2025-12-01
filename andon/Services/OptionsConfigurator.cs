using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Andon.Services;

/// <summary>
/// Optionsパターン設定
/// appsettings.jsonからの設定読み込みとIOptions<T>パターンの設定を提供
/// </summary>
public class OptionsConfigurator
{
    /// <summary>
    /// Options設定を構成
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="configuration">設定情報</param>
    /// <returns>設定済みサービスコレクション</returns>
    /// <exception cref="ArgumentNullException">引数がnullの場合</exception>
    public IServiceCollection ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // ConnectionConfig設定
        services.Configure<ConnectionConfig>(configuration.GetSection("ConnectionConfig"));

        // TimeoutConfig設定
        services.Configure<TimeoutConfig>(configuration.GetSection("TimeoutConfig"));

        // SystemResourcesConfig設定
        services.Configure<SystemResourcesConfig>(configuration.GetSection("SystemResourcesConfig"));

        // LoggingConfig設定
        services.Configure<LoggingConfig>(configuration.GetSection("LoggingConfig"));

        return services;
    }

    /// <summary>
    /// Options検証設定を構成
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <returns>設定済みサービスコレクション</returns>
    /// <exception cref="ArgumentNullException">引数がnullの場合</exception>
    public IServiceCollection ValidateOptions(IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // ConnectionConfigのバリデーション
        services.AddOptions<ConnectionConfig>()
            .Validate(config =>
            {
                if (string.IsNullOrWhiteSpace(config.IpAddress))
                    return false;
                if (config.Port <= 0 || config.Port > 65535)
                    return false;
                return true;
            }, "ConnectionConfig validation failed: IpAddress must not be empty and Port must be between 1 and 65535");

        // TimeoutConfigのバリデーション
        services.AddOptions<TimeoutConfig>()
            .Validate(config =>
            {
                if (config.ConnectTimeoutMs <= 0)
                    return false;
                if (config.SendTimeoutMs <= 0)
                    return false;
                if (config.ReceiveTimeoutMs <= 0)
                    return false;
                return true;
            }, "TimeoutConfig validation failed: All timeout values must be positive");

        // SystemResourcesConfigのバリデーション
        services.AddOptions<SystemResourcesConfig>()
            .Validate(config =>
            {
                if (config.MaxConcurrentConnections <= 0)
                    return false;
                if (config.MaxMemoryUsageMb <= 0)
                    return false;
                if (config.MaxLogFileSizeMb <= 0)
                    return false;
                return true;
            }, "SystemResourcesConfig validation failed: All resource limits must be positive");

        // LoggingConfigのバリデーション
        services.AddOptions<LoggingConfig>()
            .Validate(config =>
            {
                if (string.IsNullOrWhiteSpace(config.LogLevel))
                    return false;
                var validLogLevels = new[] { "Trace", "Debug", "Information", "Warning", "Error", "Critical" };
                if (!validLogLevels.Contains(config.LogLevel))
                    return false;
                if (string.IsNullOrWhiteSpace(config.LogFilePath))
                    return false;
                return true;
            }, "LoggingConfig validation failed: LogLevel must be valid and LogFilePath must not be empty");

        return services;
    }
}
