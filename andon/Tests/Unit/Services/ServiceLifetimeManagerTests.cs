using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Andon.Services;

namespace Andon.Tests.Unit.Services;

/// <summary>
/// ServiceLifetimeManager単体テスト
/// TDD Red段階: 失敗するテストを先に書く
/// </summary>
public class ServiceLifetimeManagerTests
{
    #region RegisterSingleton Tests

    [Fact]
    public void RegisterSingleton_ValidServiceAndImplementation_RegistersSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        ServiceLifetimeManager.RegisterSingleton<ITestService, TestServiceImplementation>(services);
        var provider = services.BuildServiceProvider();

        // Assert
        var service1 = provider.GetService<ITestService>();
        var service2 = provider.GetService<ITestService>();
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.Same(service1, service2); // Singleton - 同一インスタンス
    }

    [Fact]
    public void RegisterSingleton_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceLifetimeManager.RegisterSingleton<ITestService, TestServiceImplementation>(services));
    }

    #endregion

    #region RegisterTransient Tests

    [Fact]
    public void RegisterTransient_ValidServiceAndImplementation_RegistersSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        ServiceLifetimeManager.RegisterTransient<ITestService, TestServiceImplementation>(services);
        var provider = services.BuildServiceProvider();

        // Assert
        var service1 = provider.GetService<ITestService>();
        var service2 = provider.GetService<ITestService>();
        Assert.NotNull(service1);
        Assert.NotNull(service2);
        Assert.NotSame(service1, service2); // Transient - 異なるインスタンス
    }

    [Fact]
    public void RegisterTransient_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceLifetimeManager.RegisterTransient<ITestService, TestServiceImplementation>(services));
    }

    #endregion

    #region RegisterScoped Tests

    [Fact]
    public void RegisterScoped_ValidServiceAndImplementation_RegistersSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        ServiceLifetimeManager.RegisterScoped<ITestService, TestServiceImplementation>(services);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        var service1a = scope1.ServiceProvider.GetService<ITestService>();
        var service1b = scope1.ServiceProvider.GetService<ITestService>();
        Assert.NotNull(service1a);
        Assert.NotNull(service1b);
        Assert.Same(service1a, service1b); // 同一スコープ内 - 同一インスタンス

        using var scope2 = provider.CreateScope();
        var service2 = scope2.ServiceProvider.GetService<ITestService>();
        Assert.NotNull(service2);
        Assert.NotSame(service1a, service2); // 異なるスコープ - 異なるインスタンス
    }

    [Fact]
    public void RegisterScoped_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceLifetimeManager.RegisterScoped<ITestService, TestServiceImplementation>(services));
    }

    #endregion

    #region GetLifetime Tests

    [Fact]
    public void GetLifetime_RegisteredSingletonService_ReturnsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        ServiceLifetimeManager.RegisterSingleton<ITestService, TestServiceImplementation>(services);

        // Act
        var lifetime = ServiceLifetimeManager.GetLifetime<ITestService>(services);

        // Assert
        Assert.Equal(ServiceLifetime.Singleton, lifetime);
    }

    [Fact]
    public void GetLifetime_RegisteredTransientService_ReturnsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        ServiceLifetimeManager.RegisterTransient<ITestService, TestServiceImplementation>(services);

        // Act
        var lifetime = ServiceLifetimeManager.GetLifetime<ITestService>(services);

        // Assert
        Assert.Equal(ServiceLifetime.Transient, lifetime);
    }

    [Fact]
    public void GetLifetime_RegisteredScopedService_ReturnsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        ServiceLifetimeManager.RegisterScoped<ITestService, TestServiceImplementation>(services);

        // Act
        var lifetime = ServiceLifetimeManager.GetLifetime<ITestService>(services);

        // Assert
        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void GetLifetime_UnregisteredService_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var lifetime = ServiceLifetimeManager.GetLifetime<ITestService>(services);

        // Assert
        Assert.Null(lifetime);
    }

    [Fact]
    public void GetLifetime_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceLifetimeManager.GetLifetime<ITestService>(services));
    }

    #endregion

    #region IsRegistered Tests

    [Fact]
    public void IsRegistered_RegisteredService_ReturnsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        ServiceLifetimeManager.RegisterSingleton<ITestService, TestServiceImplementation>(services);

        // Act
        var isRegistered = ServiceLifetimeManager.IsRegistered<ITestService>(services);

        // Assert
        Assert.True(isRegistered);
    }

    [Fact]
    public void IsRegistered_UnregisteredService_ReturnsFalse()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var isRegistered = ServiceLifetimeManager.IsRegistered<ITestService>(services);

        // Assert
        Assert.False(isRegistered);
    }

    [Fact]
    public void IsRegistered_NullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ServiceLifetimeManager.IsRegistered<ITestService>(services));
    }

    #endregion

    #region Test Helpers

    // テスト用インターフェースと実装
    private interface ITestService { }
    private class TestServiceImplementation : ITestService { }

    #endregion
}
