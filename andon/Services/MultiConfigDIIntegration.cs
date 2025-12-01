using Andon.Core.Interfaces;
using Andon.Core.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace Andon.Services;

/// <summary>
/// 複数設定DI統合
/// <para>
/// 複数設定ファイルとDIコンテナの統合を管理し、
/// 設定ファイル別のサービスプロバイダを提供します。
/// </para>
/// </summary>
/// <remarks>
/// このクラスは以下の機能を提供します：
/// <list type="bullet">
/// <item>複数設定対応サービスのDIコンテナへの登録</item>
/// <item>設定ファイル専用サービスプロバイダの作成</item>
/// <item>設定別サービス解決のサポート</item>
/// </list>
/// </remarks>
public class MultiConfigDIIntegration : IMultiConfigDIIntegration
{
    /// <summary>
    /// 複数設定対応サービスをDIコンテナに登録
    /// </summary>
    /// <param name="services">DIコンテナ</param>
    /// <param name="configManager">複数設定管理インスタンス（事前初期化済み）</param>
    /// <returns>サービス登録済みのDIコンテナ（メソッドチェーン用）</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services"/>または<paramref name="configManager"/>がnullの場合
    /// </exception>
    /// <remarks>
    /// このメソッドは以下の処理を行います：
    /// <list type="number">
    /// <item>MultiPlcConfigManagerをSingletonとして登録</item>
    /// <item>設定ファイル別のファクトリサービスを登録</item>
    /// </list>
    /// </remarks>
    public IServiceCollection RegisterMultiConfigServices(
        IServiceCollection services,
        MultiPlcConfigManager configManager)
    {
        // 引数検証
        ValidateServicesArgument(services);
        ValidateConfigManagerArgument(configManager);

        // 複数設定管理をSingletonとして登録
        services.AddSingleton(configManager);

        // 将来の拡張ポイント:
        // - ConfigToFrameManagerファクトリ登録
        // - PlcCommunicationManagerファクトリ登録
        // - 軽量インスタンス生成器登録

        return services;
    }

    /// <summary>
    /// 設定ファイル専用サービスプロバイダを作成
    /// </summary>
    /// <param name="serviceProvider">メインサービスプロバイダ</param>
    /// <param name="configName">設定ファイル名（例: PLC1_settings.json）</param>
    /// <returns>設定専用サービスプロバイダ</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="serviceProvider"/>または<paramref name="configName"/>がnullの場合
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="configName"/>が空文字列またはホワイトスペースの場合
    /// </exception>
    /// <remarks>
    /// このメソッドは以下の処理を行います：
    /// <list type="number">
    /// <item>設定ファイル専用スコープを作成</item>
    /// <item>メインプロバイダから必要なサービスをコピー</item>
    /// <item>設定名を登録</item>
    /// <item>独立したサービスプロバイダをビルドして返却</item>
    /// </list>
    /// </remarks>
    public IServiceProvider CreateConfigSpecificProvider(
        IServiceProvider serviceProvider,
        string configName)
    {
        // 引数検証
        ValidateServiceProviderArgument(serviceProvider);
        ValidateConfigNameArgument(configName);

        // 設定専用スコープ作成
        var scopedServices = new ServiceCollection();

        // メインプロバイダからサービスをコピー
        CopyRequiredServices(serviceProvider, scopedServices);

        // 設定名を登録
        scopedServices.AddSingleton(new ConfigNameProvider(configName));

        // 設定専用プロバイダをビルド
        return scopedServices.BuildServiceProvider();
    }

    #region Private Helper Methods

    /// <summary>
    /// services引数を検証
    /// </summary>
    private static void ValidateServicesArgument(IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(
                nameof(services),
                "DIコンテナ（IServiceCollection）がnullです");
        }
    }

    /// <summary>
    /// configManager引数を検証
    /// </summary>
    private static void ValidateConfigManagerArgument(MultiPlcConfigManager configManager)
    {
        if (configManager == null)
        {
            throw new ArgumentNullException(
                nameof(configManager),
                "複数設定管理インスタンス（MultiPlcConfigManager）がnullです");
        }
    }

    /// <summary>
    /// serviceProvider引数を検証
    /// </summary>
    private static void ValidateServiceProviderArgument(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(
                nameof(serviceProvider),
                "サービスプロバイダ（IServiceProvider）がnullです");
        }
    }

    /// <summary>
    /// configName引数を検証
    /// </summary>
    private static void ValidateConfigNameArgument(string configName)
    {
        if (configName == null)
        {
            throw new ArgumentNullException(
                nameof(configName),
                "設定ファイル名がnullです");
        }

        if (string.IsNullOrWhiteSpace(configName))
        {
            throw new ArgumentException(
                "設定ファイル名が空文字列またはホワイトスペースです",
                nameof(configName));
        }
    }

    /// <summary>
    /// メインプロバイダから必要なサービスをコピー
    /// </summary>
    private static void CopyRequiredServices(
        IServiceProvider serviceProvider,
        IServiceCollection scopedServices)
    {
        // MultiPlcConfigManagerをコピー
        var configManager = serviceProvider.GetService<MultiPlcConfigManager>();
        if (configManager != null)
        {
            scopedServices.AddSingleton(configManager);
        }

        // 将来の拡張ポイント:
        // - その他の共有サービスのコピー
        // - 設定別の独立サービスの登録
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// 設定名プロバイダ（内部クラス）
    /// <para>
    /// 設定ファイル名を保持し、DI経由で提供します。
    /// </para>
    /// </summary>
    private class ConfigNameProvider
    {
        /// <summary>
        /// 設定ファイル名
        /// </summary>
        public string ConfigName { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configName">設定ファイル名</param>
        public ConfigNameProvider(string configName)
        {
            ConfigName = configName;
        }
    }

    #endregion
}
