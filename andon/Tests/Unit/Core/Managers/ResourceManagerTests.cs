using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Options;
using Xunit;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// ResourceManager単体テスト
/// </summary>
public class ResourceManagerTests
{
    #region GetCurrentMemoryUsageMb Tests

    [Fact]
    public void GetCurrentMemoryUsageMb_ReturnsPositiveValue()
    {
        // Arrange
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = 512 };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // Act
        var memoryUsage = resourceManager.GetCurrentMemoryUsageMb();

        // Assert
        Assert.True(memoryUsage > 0, "メモリ使用量は正の値である必要があります");
    }

    [Fact]
    public void GetCurrentMemoryUsageMb_ConsecutiveCalls_ReturnsSimilarValues()
    {
        // Arrange
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = 512 };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // Act
        var memoryUsage1 = resourceManager.GetCurrentMemoryUsageMb();
        var memoryUsage2 = resourceManager.GetCurrentMemoryUsageMb();

        // Assert - 2回の呼び出しで大きく変わらないこと（±10MB以内）
        Assert.True(Math.Abs(memoryUsage1 - memoryUsage2) < 10,
            $"連続呼び出しでメモリ使用量が大きく変化しています: {memoryUsage1}MB -> {memoryUsage2}MB");
    }

    #endregion

    #region GetMemoryLevel Tests

    [Fact]
    public void GetMemoryLevel_LowMemoryUsage_ReturnsNormal()
    {
        // Arrange - 高い上限を設定して必ずNormalになるようにする
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = 10000 };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // Act
        var level = resourceManager.GetMemoryLevel();

        // Assert
        Assert.Equal(MemoryLevel.Normal, level);
    }

    [Fact]
    public void GetMemoryLevel_MediumMemoryUsage_ReturnsWarning()
    {
        // Arrange - 現在の使用量の80%程度を上限に設定（Warning範囲に入るように）
        var tempConfig = new SystemResourcesConfig { MaxMemoryUsageMb = 10000 };
        var tempOptions = Options.Create(tempConfig);
        var tempManager = new ResourceManager(tempOptions);
        var currentUsage = tempManager.GetCurrentMemoryUsageMb();

        // 現在の使用量を基準にWarning範囲（70-85%）に入る上限を設定
        var targetMaxMb = (int)(currentUsage / 0.75); // 75%使用になるように設定
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = targetMaxMb };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // Act
        var level = resourceManager.GetMemoryLevel();

        // Assert
        Assert.Equal(MemoryLevel.Warning, level);
    }

    [Fact]
    public void GetMemoryLevel_HighMemoryUsage_ReturnsCritical()
    {
        // Arrange - 現在の使用量より少し大きい値を上限に設定（Critical範囲に入るように）
        var tempConfig = new SystemResourcesConfig { MaxMemoryUsageMb = 10000 };
        var tempOptions = Options.Create(tempConfig);
        var tempManager = new ResourceManager(tempOptions);
        var currentUsage = tempManager.GetCurrentMemoryUsageMb();

        // 現在の使用量を基準にCritical範囲（85%以上）に入る上限を設定
        var targetMaxMb = (int)(currentUsage / 0.90); // 90%使用になるように設定
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = targetMaxMb };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // Act
        var level = resourceManager.GetMemoryLevel();

        // Assert
        Assert.Equal(MemoryLevel.Critical, level);
    }

    #endregion

    #region ForceGarbageCollection Tests

    [Fact]
    public void ForceGarbageCollection_ExecutesWithoutException()
    {
        // Arrange
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = 512 };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // Act & Assert - 例外が発生しないことを確認
        var exception = Record.Exception(() => resourceManager.ForceGarbageCollection());
        Assert.Null(exception);
    }

    [Fact]
    public void ForceGarbageCollection_ReducesMemoryUsage()
    {
        // Arrange
        var config = new SystemResourcesConfig { MaxMemoryUsageMb = 512 };
        var options = Options.Create(config);
        var resourceManager = new ResourceManager(options);

        // メモリを一時的に使用
        var tempData = new byte[10 * 1024 * 1024]; // 10MB確保
        Array.Fill(tempData, (byte)0xFF);
        var memoryBefore = resourceManager.GetCurrentMemoryUsageMb();

        // 参照を削除
        tempData = null!;

        // Act
        resourceManager.ForceGarbageCollection();

        // Assert
        var memoryAfter = resourceManager.GetCurrentMemoryUsageMb();
        Assert.True(memoryAfter <= memoryBefore,
            $"GC実行後にメモリが減少する必要があります: Before={memoryBefore}MB, After={memoryAfter}MB");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ResourceManager(null!));
    }

    #endregion
}
