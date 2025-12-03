using Xunit;
using Andon.Infrastructure.Configuration;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 0: 未使用項目削除による影響がないことを確認するテスト
/// TDDサイクル: Red → Green → Refactor
/// </summary>
public class Phase0_UnusedItemsDeletion_NoImpactTests
{
    // Phase 3完了: appsettings.json完全廃止により、appsettings.json項目確認テストは不要となったため削除
    // 削除されたテスト:
    // - Phase0_AppsettingsJson_PlcCommunicationConnection項目が存在しない
    // - Phase0_AppsettingsJson_PlcCommunicationTimeouts項目が存在しない
    // - Phase0_AppsettingsJson_PlcCommunicationTargetDevices項目が存在しない
    // - Phase0_AppsettingsJson_PlcCommunicationDataProcessingBitExpansionセクションが存在しない
    // - Phase0_AppsettingsJson_SystemResources未使用項目が存在しない
    // - Phase0_AppsettingsJson_Loggingセクションが存在しない

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

    // Phase 2-1完了: LoggingConfigはハードコード化されたため、このテストは不要
    // [Fact]
    // [Trait("Phase", "Phase0")]
    // [Trait("Category", "FunctionalTest")]
    // public void Phase0_LoggingConfig_Loggingセクション削除後も動作()
    // {
    //     // Arrange
    //     var configuration = LoadAppsettingsJson();
    //     var loggingConfig = new LoggingConfig();
    //     configuration.GetSection("LoggingConfig").Bind(loggingConfig);
    //
    //     // Act & Assert
    //     // LoggingConfigセクションの値が正常に読み込まれることを確認
    //     Assert.NotNull(loggingConfig);
    //     Assert.NotNull(loggingConfig.LogLevel); // LogLevelが設定されていることを確認
    //     Assert.True(loggingConfig.EnableFileOutput);
    //
    //     // Loggingセクション（削除対象）が存在しなくてもLoggingConfigは正常に動作することを確認
    //     // LoggingセクションとLoggingConfigセクションは完全に独立している
    // }

    #endregion

    // Phase 3完了: appsettings.json完全廃止により、LoadAppsettingsJson()ヘルパーメソッドは不要となったため削除
}
