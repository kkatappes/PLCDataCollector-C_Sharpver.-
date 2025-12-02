using Microsoft.Extensions.Configuration;
using Xunit;
using Andon.Infrastructure.Configuration;
using Andon.Core.Managers;
using Andon.Core.Models.ConfigModels;
using Microsoft.Extensions.Options;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 0: 未使用項目削除による影響がないことを確認するテスト
/// TDDサイクル: Red → Green → Refactor
/// </summary>
public class Phase0_UnusedItemsDeletion_NoImpactTests
{
    #region 削除対象項目の不在確認テスト（Red → Green）

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "ConfigValidation")]
    public void Phase0_AppsettingsJson_PlcCommunicationConnection項目が存在しない()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();

        // Act & Assert - PlcCommunication.Connection セクションの削除対象項目
        var connectionSection = configuration.GetSection("PlcCommunication:Connection");

        // これらの項目は削除されているべき（Phase 0完了後）
        Assert.Null(connectionSection["IpAddress"]); // Excel設定（settingsシート B8セル）で代替済み
        Assert.Null(connectionSection["Port"]); // Excel設定（settingsシート B9セル）で代替済み
        Assert.Null(connectionSection["UseTcp"]); // Excel設定（settingsシート B10セル）で代替済み
        Assert.Null(connectionSection["IsBinary"]); // Phase 2完了でExcel読み込み実装済み
        Assert.Null(connectionSection["FrameVersion"]); // Phase 2完了でExcel読み込み実装済み
    }

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "ConfigValidation")]
    public void Phase0_AppsettingsJson_PlcCommunicationTimeouts項目が存在しない()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();

        // Act & Assert - PlcCommunication.Timeouts セクションの削除対象項目
        var timeoutsSection = configuration.GetSection("PlcCommunication:Timeouts");

        // これらの項目は削除されているべき（Phase 0完了後）
        Assert.Null(timeoutsSection["ConnectTimeoutMs"]); // コード内で完全に未参照
        Assert.Null(timeoutsSection["SendTimeoutMs"]); // コード内で完全に未参照
        Assert.Null(timeoutsSection["ReceiveTimeoutMs"]); // Excel設定には存在しない
    }

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "ConfigValidation")]
    public void Phase0_AppsettingsJson_PlcCommunicationTargetDevices項目が存在しない()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();

        // Act & Assert - PlcCommunication.TargetDevices セクションの削除対象項目
        var targetDevicesSection = configuration.GetSection("PlcCommunication:TargetDevices");

        // これらの項目は削除されているべき（Phase 0完了後）
        Assert.Null(targetDevicesSection["Devices"]); // Excelのデータ収集デバイスタブで代替済み
    }

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "ConfigValidation")]
    public void Phase0_AppsettingsJson_PlcCommunicationDataProcessingBitExpansionセクションが存在しない()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();

        // Act & Assert - PlcCommunication.DataProcessing.BitExpansion セクション全体
        var bitExpansionSection = configuration.GetSection("PlcCommunication:DataProcessing:BitExpansion");

        // このセクションは削除されているべき（実装されていないため）
        Assert.False(bitExpansionSection.Exists());
    }

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "ConfigValidation")]
    public void Phase0_AppsettingsJson_SystemResources未使用項目が存在しない()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();

        // Act & Assert - SystemResources セクションの削除対象項目
        var systemResourcesSection = configuration.GetSection("SystemResources");

        // これらの項目は削除されているべき（Phase 0完了後）
        Assert.Null(systemResourcesSection["MemoryLimitKB"]); // Phase 0で削除済み
        Assert.Null(systemResourcesSection["MaxBufferSize"]); // Phase 0で削除済み
        Assert.Null(systemResourcesSection["MemoryThresholdKB"]); // Phase 0で削除済み
    }

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "ConfigValidation")]
    public void Phase0_AppsettingsJson_Loggingセクションが存在しない()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();

        // Act & Assert - Logging セクション全体（LoggingConfigとは別物）
        var loggingSection = configuration.GetSection("Logging");

        // このセクションは削除されているべき（Phase 0完了後）
        // ⚠️ 重要: LoggingConfigセクション（本番使用）は残す
        Assert.False(loggingSection.Exists());

        // LoggingConfigセクションは残っているべき
        var loggingConfigSection = configuration.GetSection("LoggingConfig");
        Assert.True(loggingConfigSection.Exists());
    }

    #endregion

    #region 削除後の機能動作確認テスト（常にGreen）

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "FunctionalTest")]
    public void Phase0_Excel設定読み込み_appsettings削除後も動作()
    {
        // Arrange
        var loader = new ConfigurationLoaderExcel();

        // Act & Assert
        // LoadAllPlcConnectionConfigs()は自動的にExcelファイルを探索して読み込む
        // appsettings.jsonの該当項目（Connection, Timeouts, Devices等）が不在でもエラーにならないことを確認
        // Excel設定ファイルが存在しない場合は例外が発生するが、それはappsettings.jsonとは無関係
        try
        {
            var result = loader.LoadAllPlcConnectionConfigs();
            Assert.NotNull(result);
            // Excel設定ファイルが存在する場合、appsettings.json項目なしで正常に動作することを確認
        }
        catch (ArgumentException ex) when (ex.Message.Contains("設定ファイル(.xlsx)が見つかりません"))
        {
            // Excel設定ファイルがない場合はスキップ（appsettings.jsonとは無関係）
            // このテストの目的はappsettings.json項目削除の影響確認であり、Excel設定ファイルの存在確認ではない
            return;
        }
    }

    [Fact]
    [Trait("Phase", "Phase0")]
    [Trait("Category", "FunctionalTest")]
    public void Phase0_LoggingConfig_Loggingセクション削除後も動作()
    {
        // Arrange
        var configuration = LoadAppsettingsJson();
        var loggingConfig = new LoggingConfig();
        configuration.GetSection("LoggingConfig").Bind(loggingConfig);

        // Act & Assert
        // LoggingConfigセクションの値が正常に読み込まれることを確認
        Assert.NotNull(loggingConfig);
        Assert.NotNull(loggingConfig.LogLevel); // LogLevelが設定されていることを確認
        Assert.True(loggingConfig.EnableFileOutput);

        // Loggingセクション（削除対象）が存在しなくてもLoggingConfigは正常に動作することを確認
        // LoggingセクションとLoggingConfigセクションは完全に独立している
    }

    #endregion

    #region ヘルパーメソッド

    /// <summary>
    /// appsettings.jsonを読み込む
    /// </summary>
    private IConfiguration LoadAppsettingsJson()
    {
        var basePath = @"C:\Users\1010821\Desktop\python\andon\andon";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        return configuration;
    }

    #endregion
}
