using Microsoft.Extensions.DependencyInjection;

namespace Andon.Services;

/// <summary>
/// サービスライフタイム管理
/// TDD Green段階: テストを通すための最小限の実装
/// </summary>
public static class ServiceLifetimeManager
{
    /// <summary>
    /// Singletonライフタイムでサービスを登録する
    /// </summary>
    /// <typeparam name="TService">サービス型（インターフェース）</typeparam>
    /// <typeparam name="TImplementation">実装型</typeparam>
    /// <param name="services">サービスコレクション</param>
    /// <exception cref="ArgumentNullException">servicesがnullの場合</exception>
    public static IServiceCollection RegisterSingleton<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddSingleton<TService, TImplementation>();
    }

    /// <summary>
    /// Transientライフタイムでサービスを登録する
    /// </summary>
    /// <typeparam name="TService">サービス型（インターフェース）</typeparam>
    /// <typeparam name="TImplementation">実装型</typeparam>
    /// <param name="services">サービスコレクション</param>
    /// <exception cref="ArgumentNullException">servicesがnullの場合</exception>
    public static IServiceCollection RegisterTransient<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddTransient<TService, TImplementation>();
    }

    /// <summary>
    /// Scopedライフタイムでサービスを登録する
    /// </summary>
    /// <typeparam name="TService">サービス型（インターフェース）</typeparam>
    /// <typeparam name="TImplementation">実装型</typeparam>
    /// <param name="services">サービスコレクション</param>
    /// <exception cref="ArgumentNullException">servicesがnullの場合</exception>
    public static IServiceCollection RegisterScoped<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.AddScoped<TService, TImplementation>();
    }

    /// <summary>
    /// 登録されたサービスのライフタイムを取得する
    /// </summary>
    /// <typeparam name="TService">サービス型</typeparam>
    /// <param name="services">サービスコレクション</param>
    /// <returns>サービスライフタイム（未登録の場合はnull）</returns>
    /// <exception cref="ArgumentNullException">servicesがnullの場合</exception>
    public static ServiceLifetime? GetLifetime<TService>(IServiceCollection services)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
        return descriptor?.Lifetime;
    }

    /// <summary>
    /// サービスが登録されているかを確認する
    /// </summary>
    /// <typeparam name="TService">サービス型</typeparam>
    /// <param name="services">サービスコレクション</param>
    /// <returns>登録されている場合true、それ以外false</returns>
    /// <exception cref="ArgumentNullException">servicesがnullの場合</exception>
    public static bool IsRegistered<TService>(IServiceCollection services)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.Any(d => d.ServiceType == typeof(TService));
    }
}
