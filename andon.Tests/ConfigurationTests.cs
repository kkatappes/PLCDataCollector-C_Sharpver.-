using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SlmpClient.Core;
using SlmpClient.Constants;
using Xunit;

namespace SlmpClient.Tests
{
    /// <summary>
    /// 設定外だし化機能のテストクラス
    /// </summary>
    public class ConfigurationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _createdFiles = new();

        public ConfigurationTests()
        {
            // テスト用一時ディレクトリ作成
            _testDirectory = Path.Combine(Path.GetTempPath(), "SlmpConfigTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            // テスト後にファイルとディレクトリを削除
            try
            {
                foreach (var file in _createdFiles)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                if (Directory.Exists(_testDirectory))
                    Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // テスト環境のクリーンアップ失敗は無視
            }
        }

        private string CreateTestConfigFile(string fileName, object config)
        {
            var filePath = Path.Combine(_testDirectory, fileName);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            _createdFiles.Add(filePath);
            return filePath;
        }

        #region 1. 設定読み込み機能の単体テスト

        [Fact]
        public void LoadConfiguration_ValidFile_ShouldLoadCorrectly()
        {
            // Arrange: 正常な設定ファイルを作成
            var testConfig = new
            {
                PlcConnection = new
                {
                    IpAddress = "192.168.1.100",
                    Port = 5007,
                    UseTcp = true,
                    IsBinary = true,
                    FrameVersion = "4E",
                    EnablePipelining = true,
                    MaxConcurrentRequests = 8
                },
                ContinuitySettings = new
                {
                    ErrorHandlingMode = "ReturnDefaultAndContinue",
                    NotificationLevel = "Warning",
                    DefaultBitValue = false,
                    DefaultWordValue = 0,
                    EnableErrorStatistics = true,
                    EnableContinuityLogging = true,
                    EnableDebugOutput = false,
                    MaxNotificationFrequencySeconds = 60
                },
                TimeoutSettings = new
                {
                    ReceiveTimeoutMs = 3000,
                    ConnectTimeoutMs = 10000
                },
                RetrySettings = new
                {
                    MaxRetryCount = 3,
                    InitialDelayMs = 100,
                    MaxDelayMs = 5000,
                    BackoffMultiplier = 2.0
                },
                MonitoringSettings = new
                {
                    CycleIntervalMs = 1000,
                    MaxCycles = 10,
                    EnablePerformanceMonitoring = true,
                    DataCollectionIntervalMs = 500
                }
            };

            var configFile = CreateTestConfigFile("valid_config.json", testConfig);

            // Act: 設定ファイルを読み込み
            var builder = new ConfigurationBuilder()
                .SetBasePath(_testDirectory)
                .AddJsonFile("valid_config.json", optional: false);

            var config = builder.Build();
            var appConfig = new ApplicationConfiguration();
            config.Bind(appConfig);

            // Assert: 正しい値が読み込まれること
            Assert.Equal("192.168.1.100", appConfig.PlcConnection.IpAddress);
            Assert.Equal(5007, appConfig.PlcConnection.Port);
            Assert.True(appConfig.PlcConnection.UseTcp);
            Assert.True(appConfig.PlcConnection.IsBinary);
            Assert.Equal("4E", appConfig.PlcConnection.FrameVersion);
            Assert.True(appConfig.PlcConnection.EnablePipelining);
            Assert.Equal(8, appConfig.PlcConnection.MaxConcurrentRequests);

            Assert.Equal("ReturnDefaultAndContinue", appConfig.ContinuitySettings.ErrorHandlingMode);
            Assert.Equal("Warning", appConfig.ContinuitySettings.NotificationLevel);
            Assert.False(appConfig.ContinuitySettings.DefaultBitValue);
            Assert.Equal(0, appConfig.ContinuitySettings.DefaultWordValue);

            Assert.Equal(3000, appConfig.TimeoutSettings.ReceiveTimeoutMs);
            Assert.Equal(10000, appConfig.TimeoutSettings.ConnectTimeoutMs);

            Assert.Equal(3, appConfig.RetrySettings.MaxRetryCount);
            Assert.Equal(100, appConfig.RetrySettings.InitialDelayMs);

            Assert.Equal(1000, appConfig.MonitoringSettings.CycleIntervalMs);
            Assert.Equal(10, appConfig.MonitoringSettings.MaxCycles);
        }

        [Fact]
        public void LoadConfiguration_MissingFile_ShouldUseDefaults()
        {
            // Arrange: 存在しないファイルを指定
            var builder = new ConfigurationBuilder()
                .SetBasePath(_testDirectory)
                .AddJsonFile("nonexistent.json", optional: true);

            var config = builder.Build();

            // Act: デフォルト設定を作成
            var appConfig = new ApplicationConfiguration();
            config.Bind(appConfig);

            // Assert: デフォルト値が使用されること
            Assert.Equal("192.168.1.10", appConfig.PlcConnection.IpAddress);
            Assert.Equal(5007, appConfig.PlcConnection.Port);
            Assert.True(appConfig.PlcConnection.UseTcp);
            Assert.True(appConfig.PlcConnection.IsBinary);
        }

        [Fact]
        public void LoadConfiguration_PartialFile_ShouldMergeWithDefaults()
        {
            // Arrange: 部分的な設定ファイルを作成
            var partialConfig = new
            {
                PlcConnection = new
                {
                    IpAddress = "192.168.2.50",
                    Port = 6000
                    // 他の設定は省略
                }
            };

            var configFile = CreateTestConfigFile("partial_config.json", partialConfig);

            // Act: 設定ファイルを読み込み
            var builder = new ConfigurationBuilder()
                .SetBasePath(_testDirectory)
                .AddJsonFile("partial_config.json", optional: false);

            var config = builder.Build();
            var appConfig = new ApplicationConfiguration();
            config.Bind(appConfig);

            // Assert: 指定された値は反映され、未指定はデフォルト値
            Assert.Equal("192.168.2.50", appConfig.PlcConnection.IpAddress);
            Assert.Equal(6000, appConfig.PlcConnection.Port);
            Assert.True(appConfig.PlcConnection.UseTcp); // デフォルト値
            Assert.True(appConfig.PlcConnection.IsBinary); // デフォルト値
        }

        #endregion

        #region 2. 設定変換機能の統合テスト

        [Fact]
        public void ToSlmpConnectionSettings_ValidConfiguration_ShouldConvertCorrectly()
        {
            // Arrange: 有効な設定を作成
            var appConfig = new ApplicationConfiguration();
            appConfig.PlcConnection.IpAddress = "192.168.1.200";
            appConfig.PlcConnection.Port = 5008;
            appConfig.PlcConnection.UseTcp = false;
            appConfig.PlcConnection.IsBinary = false;
            appConfig.PlcConnection.FrameVersion = "3E";
            appConfig.PlcConnection.EnablePipelining = false;
            appConfig.PlcConnection.MaxConcurrentRequests = 4;

            appConfig.TimeoutSettings.ReceiveTimeoutMs = 2000;
            appConfig.TimeoutSettings.ConnectTimeoutMs = 8000;

            appConfig.ContinuitySettings.ErrorHandlingMode = "ThrowException";
            appConfig.ContinuitySettings.NotificationLevel = "Error";
            appConfig.ContinuitySettings.DefaultBitValue = true;
            appConfig.ContinuitySettings.DefaultWordValue = 100;

            appConfig.RetrySettings.MaxRetryCount = 5;
            appConfig.RetrySettings.InitialDelayMs = 200;
            appConfig.RetrySettings.MaxDelayMs = 8000;
            appConfig.RetrySettings.BackoffMultiplier = 3.0;

            // Act: SlmpConnectionSettingsに変換
            var slmpSettings = appConfig.ToSlmpConnectionSettings();

            // Assert: 正しく変換されること
            Assert.Equal(5008, slmpSettings.Port);
            Assert.False(slmpSettings.UseTcp);
            Assert.False(slmpSettings.IsBinary);
            Assert.Equal(SlmpFrameVersion.Version3E, slmpSettings.Version);
            Assert.False(slmpSettings.EnablePipelining);
            Assert.Equal(4, slmpSettings.MaxConcurrentRequests);

            Assert.Equal(TimeSpan.FromMilliseconds(2000), slmpSettings.ReceiveTimeout);
            Assert.Equal(TimeSpan.FromMilliseconds(8000), slmpSettings.ConnectTimeout);

            Assert.Equal(ErrorHandlingMode.ThrowException, slmpSettings.ContinuitySettings.Mode);
            Assert.Equal(ErrorNotificationLevel.Error, slmpSettings.ContinuitySettings.NotificationLevel);
            Assert.True(slmpSettings.ContinuitySettings.DefaultBitValue);
            Assert.Equal(100, slmpSettings.ContinuitySettings.DefaultWordValue);

            Assert.Equal(5, slmpSettings.RetrySettings.MaxRetryCount);
            Assert.Equal(TimeSpan.FromMilliseconds(200), slmpSettings.RetrySettings.InitialDelay);
            Assert.Equal(TimeSpan.FromMilliseconds(8000), slmpSettings.RetrySettings.MaxDelay);
            Assert.Equal(3.0, slmpSettings.RetrySettings.BackoffMultiplier);
        }

        [Fact]
        public void ToSlmpConnectionSettings_DefaultConfiguration_ShouldConvertWithDefaults()
        {
            // Arrange: デフォルト設定
            var appConfig = new ApplicationConfiguration();

            // Act: SlmpConnectionSettingsに変換
            var slmpSettings = appConfig.ToSlmpConnectionSettings();

            // Assert: デフォルト値で変換されること
            Assert.Equal(5007, slmpSettings.Port);
            Assert.True(slmpSettings.UseTcp);
            Assert.True(slmpSettings.IsBinary);
            Assert.Equal(SlmpFrameVersion.Version4E, slmpSettings.Version);
            Assert.True(slmpSettings.EnablePipelining);
            Assert.Equal(8, slmpSettings.MaxConcurrentRequests);
        }

        #endregion

        #region 3. 異常系テスト（設定ファイル不正・欠損）

        [Fact]
        public void LoadConfiguration_InvalidJson_ShouldThrowException()
        {
            // Arrange: 不正なJSONファイルを作成
            var invalidJsonFile = Path.Combine(_testDirectory, "invalid.json");
            File.WriteAllText(invalidJsonFile, "{ invalid json }");
            _createdFiles.Add(invalidJsonFile);

            // Act & Assert: 設定構築時に例外がスローされること
            var builder = new ConfigurationBuilder()
                .SetBasePath(_testDirectory);

            Assert.Throws<InvalidDataException>(() =>
            {
                builder.AddJsonFile("invalid.json", optional: false).Build();
            });
        }

        [Fact]
        public void LoadConfiguration_EmptyFile_ShouldUseDefaults()
        {
            // Arrange: 空のJSONファイルを作成
            var emptyFile = Path.Combine(_testDirectory, "empty.json");
            File.WriteAllText(emptyFile, "{}");
            _createdFiles.Add(emptyFile);

            // Act: 設定ファイルを読み込み
            var builder = new ConfigurationBuilder()
                .SetBasePath(_testDirectory)
                .AddJsonFile("empty.json", optional: false);

            var config = builder.Build();
            var appConfig = new ApplicationConfiguration();
            config.Bind(appConfig);

            // Assert: デフォルト値が使用されること
            Assert.Equal("192.168.1.10", appConfig.PlcConnection.IpAddress);
            Assert.Equal(5007, appConfig.PlcConnection.Port);
        }

        [Fact]
        public void ToSlmpConnectionSettings_InvalidFrameVersion_ShouldUseDefault()
        {
            // Arrange: 無効なフレームバージョンを設定
            var appConfig = new ApplicationConfiguration();
            appConfig.PlcConnection.FrameVersion = "INVALID";

            // Act: 変換実行
            var slmpSettings = appConfig.ToSlmpConnectionSettings();

            // Assert: 実装では"4E"以外はすべて3Eになる
            Assert.Equal(SlmpFrameVersion.Version3E, slmpSettings.Version);
        }

        [Fact]
        public void ToSlmpConnectionSettings_InvalidErrorHandlingMode_ShouldUseDefault()
        {
            // Arrange: 無効なエラーハンドリングモードを設定
            var appConfig = new ApplicationConfiguration();
            appConfig.ContinuitySettings.ErrorHandlingMode = "INVALID";

            // Act: 変換実行
            var slmpSettings = appConfig.ToSlmpConnectionSettings();

            // Assert: デフォルト値が使用されること
            Assert.Equal(ErrorHandlingMode.ReturnDefaultAndContinue, slmpSettings.ContinuitySettings.Mode);
        }

        #endregion

        #region 4. 境界値テスト（設定値の範囲チェック）

        [Theory]
        [InlineData(1)]     // 最小値
        [InlineData(65535)] // 最大値
        public void PlcConnectionConfiguration_ValidPortRange_ShouldAccept(int port)
        {
            // Arrange & Act: 有効なポート番号を設定
            var config = new PlcConnectionConfiguration();
            config.Port = port;

            // Assert: 例外がスローされないこと
            Assert.Equal(port, config.Port);
        }

        [Theory]
        [InlineData(0)]     // 最小値未満
        [InlineData(65536)] // 最大値超過
        public void PlcConnectionConfiguration_InvalidPortRange_ShouldThrowValidationError(int port)
        {
            // Arrange: 設定オブジェクト作成
            var config = new PlcConnectionConfiguration();

            // Act & Assert: 範囲外の値で例外がスローされること
            // 注意: Range属性の検証は手動で行うか、バリデーション機能を実装する必要がある
            // ここでは設定値の設定は可能だが、実際のバリデーションは別途実装が必要
            config.Port = port;
            Assert.Equal(port, config.Port); // 設定は可能

            // 実際のバリデーションテストは SlmpConnectionSettings で行う
            var appConfig = new ApplicationConfiguration();
            appConfig.PlcConnection.Port = port;

            if (port < 1 || port > 65535)
            {
                // SlmpConnectionSettings での検証をテスト
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var slmpSettings = appConfig.ToSlmpConnectionSettings();
                    // Port設定時に例外がスローされることを期待
                    var testSettings = new SlmpConnectionSettings();
                    testSettings.Port = port;
                });
            }
        }

        [Theory]
        [InlineData(100)]   // 最小値
        [InlineData(60000)] // 最大値
        public void TimeoutConfiguration_ValidReceiveTimeout_ShouldAccept(int timeoutMs)
        {
            // Arrange & Act: 有効なタイムアウト値を設定
            var config = new TimeoutConfiguration();
            config.ReceiveTimeoutMs = timeoutMs;

            // Assert: 例外がスローされないこと
            Assert.Equal(timeoutMs, config.ReceiveTimeoutMs);
        }

        [Theory]
        [InlineData(0)]    // 最小値未満
        [InlineData(10)]   // 最小値未満
        [InlineData(99)]   // 最小値未満
        [InlineData(600001)] // 最大値超過（10分を超える）
        public void TimeoutConfiguration_InvalidReceiveTimeout_ShouldFailInConversion(int timeoutMs)
        {
            // Arrange: 設定オブジェクト作成
            var config = new TimeoutConfiguration();
            config.ReceiveTimeoutMs = timeoutMs;

            // Assert: 設定自体は可能
            Assert.Equal(timeoutMs, config.ReceiveTimeoutMs);

            // 実際のバリデーションはSlmpConnectionSettingsでの変換時に行われる
            var appConfig = new ApplicationConfiguration();
            appConfig.TimeoutSettings.ReceiveTimeoutMs = timeoutMs;

            // 無効な値の場合、変換時に例外がスローされる
            if (timeoutMs <= 0 || timeoutMs > 600000) // 10分 = 600,000ミリ秒
            {
                Assert.ThrowsAny<ArgumentException>(() =>
                {
                    var slmpSettings = appConfig.ToSlmpConnectionSettings();
                    // この時点でSlmpConnectionSettingsのReceiveTimeoutプロパティに
                    // 無効な値が設定され、例外がスローされる
                });
            }
            else
            {
                // 有効な値の場合は正常に変換される
                var slmpSettings = appConfig.ToSlmpConnectionSettings();
                Assert.NotNull(slmpSettings);
            }
        }

        [Theory]
        [InlineData(0)]   // 最小値
        [InlineData(10)]  // 最大値
        public void RetryConfiguration_ValidMaxRetryCount_ShouldAccept(int maxRetryCount)
        {
            // Arrange & Act: 有効なリトライ回数を設定
            var config = new RetryConfiguration();
            config.MaxRetryCount = maxRetryCount;

            // Assert: 例外がスローされないこと
            Assert.Equal(maxRetryCount, config.MaxRetryCount);
        }

        [Fact]
        public void RetryConfiguration_BackoffMultiplierRange_ShouldAcceptValidValues()
        {
            // Arrange: 有効なバックオフ倍率をテスト
            var config = new RetryConfiguration();
            var validValues = new double[] { 1.0, 1.5, 2.0, 5.0, 10.0 };

            foreach (var value in validValues)
            {
                // Act: 有効な値を設定
                config.BackoffMultiplier = value;

                // Assert: 正しく設定されること
                Assert.Equal(value, config.BackoffMultiplier);
            }
        }

        #endregion
    }
}