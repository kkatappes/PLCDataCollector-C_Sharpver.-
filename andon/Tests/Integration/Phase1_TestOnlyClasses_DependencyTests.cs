using Xunit;
using System.Reflection;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 1: テスト専用項目の整理 - 削除完了確認テスト
/// 削除対象クラスが正しく削除されたことを検証
/// </summary>
public class Phase1_TestOnlyClasses_DependencyTests
{
    /// <summary>
    /// TC001: ResourceManagerが削除されたことを確認
    /// </summary>
    [Fact]
    public void Test_ResourceManager_削除完了()
    {
        // Arrange
        var andonAssembly = Assembly.Load("andon");

        // Act
        var resourceManagerType = andonAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ResourceManager");

        // Assert
        // ResourceManagerクラスが削除されていることを確認
        Assert.Null(resourceManagerType);
    }

    /// <summary>
    /// TC002: IResourceManagerが削除されたことを確認
    /// </summary>
    [Fact]
    public void Test_IResourceManager_削除完了()
    {
        // Arrange
        var andonAssembly = Assembly.Load("andon");

        // Act
        var iResourceManagerType = andonAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "IResourceManager");

        // Assert
        // IResourceManagerインターフェースが削除されていることを確認
        Assert.Null(iResourceManagerType);
    }

    /// <summary>
    /// TC003: ConfigurationLoaderが削除されたことを確認
    /// </summary>
    [Fact]
    public void Test_ConfigurationLoader_削除完了()
    {
        // Arrange
        var andonAssembly = Assembly.Load("andon");

        // Act
        var configurationLoaderType = andonAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "ConfigurationLoader");

        // Assert
        // ConfigurationLoaderクラスが削除されていることを確認
        Assert.Null(configurationLoaderType);
    }

    /// <summary>
    /// TC004: SystemResourcesConfigが削除されたことを確認
    /// </summary>
    [Fact]
    public void Test_SystemResourcesConfig_削除完了()
    {
        // Arrange
        var andonAssembly = Assembly.Load("andon");

        // Act
        var systemResourcesConfigType = andonAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "SystemResourcesConfig");

        // Assert
        // SystemResourcesConfigクラスが削除されていることを確認
        Assert.Null(systemResourcesConfigType);
    }

    // Phase 3完了: appsettings.json完全廃止により、appsettings.jsonファイル確認テストは不要となったため削除
    // 削除されたテスト:
    // - Test_SystemResourcesセクション_削除完了
}
