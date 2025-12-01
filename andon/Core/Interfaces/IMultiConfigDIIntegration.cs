using Andon.Core.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Andon.Core.Interfaces;

/// <summary>
/// 複数設定DI統合インターフェース
/// </summary>
public interface IMultiConfigDIIntegration
{
    /// <summary>
    /// 複数設定対応サービスをDIコンテナに登録
    /// </summary>
    /// <param name="services">DIコンテナ</param>
    /// <param name="configManager">複数設定管理インスタンス（事前初期化済み）</param>
    /// <returns>サービス登録済みのDIコンテナ</returns>
    IServiceCollection RegisterMultiConfigServices(
        IServiceCollection services,
        MultiPlcConfigManager configManager);

    /// <summary>
    /// 設定ファイル専用サービスプロバイダを作成
    /// </summary>
    /// <param name="serviceProvider">メインサービスプロバイダ</param>
    /// <param name="configName">設定ファイル名（例: PLC1_settings.json）</param>
    /// <returns>設定専用サービスプロバイダ</returns>
    IServiceProvider CreateConfigSpecificProvider(
        IServiceProvider serviceProvider,
        string configName);
}
