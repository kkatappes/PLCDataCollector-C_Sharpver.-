using Xunit;
using Andon.Infrastructure.Configuration;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Models;
using Andon.Core.Constants;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Andon.Tests.Integration;

/// <summary>
/// Phase 2-5: SettingsValidator統合テスト
/// TDD Red Phase: 失敗するテストを先に作成
/// ConfigurationLoaderExcel.ValidateConfiguration()がSettingsValidatorのメソッドを使用することを確認
/// </summary>
public class Phase2_5_SettingsValidator_IntegrationTests
{
    /// <summary>
    /// リフレクションを使用してprivateメソッドValidateConfiguration()を呼び出すヘルパーメソッド
    /// </summary>
    private void InvokeValidateConfiguration(PlcConfiguration config)
    {
        var loader = new ConfigurationLoaderExcel();
        var methodInfo = typeof(ConfigurationLoaderExcel).GetMethod("ValidateConfiguration",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo == null)
        {
            throw new InvalidOperationException("ValidateConfiguration method not found");
        }

        try
        {
            methodInfo.Invoke(loader, new object[] { config });
        }
        catch (TargetInvocationException ex)
        {
            // リフレクション経由での例外を元の例外として再スロー
            if (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
            throw;
        }
    }

    /// <summary>
    /// 正常なPlcConfigurationを作成するヘルパーメソッド
    /// </summary>
    private PlcConfiguration CreateValidPlcConfiguration()
    {
        return new PlcConfiguration
        {
            IpAddress = "192.168.1.1",
            Port = 8192,
            MonitoringIntervalMs = 1000,
            SavePath = "./output",
            PlcModel = "TestPLC",
            Devices = new List<DeviceSpecification>
            {
                new DeviceSpecification(DeviceCode.D, 100)
                {
                    ItemName = "TestDevice",
                    Digits = 1,
                    Unit = "word"
                }
            },
            SourceExcelFile = "test.xlsx"
        };
    }

    /// <summary>
    /// テストケース1: 不正なIPアドレス - SettingsValidator.ValidateIpAddress()のエラーメッセージが表示される
    /// Red Phase: このテストは失敗することを期待（現在は独自実装のエラーメッセージ）
    /// </summary>
    [Fact]
    public void test_ValidateConfiguration_不正なIPアドレス_SettingsValidator使用()
    {
        // Arrange
        var config = CreateValidPlcConfiguration();
        config.IpAddress = "999.999.999.999"; // 不正なIPアドレス

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            InvokeValidateConfiguration(config));

        // SettingsValidator.ValidateIpAddress()のエラーメッセージであることを確認
        // 期待: "IPAddressの形式が不正です"
        // 現在: "IPアドレスの形式が不正です"
        Assert.Contains("IPAddressの形式が不正です", ex.Message);
    }

    /// <summary>
    /// テストケース2: ポート範囲外 - SettingsValidator.ValidatePort()のエラーメッセージが表示される
    /// Red Phase: このテストは失敗することを期待（現在は独自実装のエラーメッセージ）
    /// </summary>
    [Fact]
    public void test_ValidateConfiguration_ポート範囲外_SettingsValidator使用()
    {
        // Arrange
        var config = CreateValidPlcConfiguration();
        config.Port = 99999; // 範囲外（1～65535）

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            InvokeValidateConfiguration(config));

        // SettingsValidator.ValidatePort()のエラーメッセージであることを確認
        // 期待: "Portの値が範囲外です"
        // 現在: "ポート番号が範囲外です"
        Assert.Contains("Portの値が範囲外です", ex.Message);
    }

    /// <summary>
    /// テストケース3: MonitoringIntervalMs範囲外 - SettingsValidator.ValidateMonitoringIntervalMs()のエラーメッセージが表示される
    /// Red Phase: このテストは失敗することを期待（現在は独自実装のエラーメッセージ、範囲も異なる）
    /// ⚠️ 重要: SettingsValidatorの範囲（100～60000ms）を使用する
    /// </summary>
    [Fact]
    public void test_ValidateConfiguration_MonitoringIntervalMs範囲外_SettingsValidator使用()
    {
        // Arrange
        var config = CreateValidPlcConfiguration();
        config.MonitoringIntervalMs = 50; // 範囲外（100～60000ms）

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            InvokeValidateConfiguration(config));

        // SettingsValidator.ValidateMonitoringIntervalMs()のエラーメッセージであることを確認
        // 期待: "MonitoringIntervalMsの値が範囲外です"
        // 現在: "データ取得周期が範囲外です"（かつ範囲が1～86400000ms）
        Assert.Contains("MonitoringIntervalMsの値が範囲外です", ex.Message);
    }

    /// <summary>
    /// テストケース4: 全項目正常 - SettingsValidatorを使用しても正常に検証が通る
    /// </summary>
    [Fact]
    public void test_ValidateConfiguration_全項目正常_SettingsValidator使用()
    {
        // Arrange
        var config = CreateValidPlcConfiguration();
        config.IpAddress = "172.30.40.40";
        config.Port = 8192;
        config.MonitoringIntervalMs = 1000;

        // Act & Assert
        // 例外が発生しないことを確認
        var exception = Record.Exception(() => InvokeValidateConfiguration(config));
        Assert.Null(exception);
    }
}
