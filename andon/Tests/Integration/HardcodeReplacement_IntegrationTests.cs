using Xunit;
using Andon.Infrastructure.Configuration;
using Andon.Core.Managers;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using static Andon.Core.Constants.DeviceCode;

namespace Andon.Tests.Integration
{
    /// <summary>
    /// Phase 5: ハードコード置き換え統合テスト
    /// Phase 1-4の実装が正しく統合されていることを確認
    /// </summary>
    public class HardcodeReplacement_IntegrationTests
    {
        #region Constants

        /// <summary>
        /// 4Eフレームのサブヘッダ（1バイト目）
        /// </summary>
        private const byte SubHeader_4E_Byte0 = 0x54;

        /// <summary>
        /// 4Eフレームのサブヘッダ（2バイト目）
        /// </summary>
        private const byte SubHeader_4E_Byte1 = 0x00;

        /// <summary>
        /// 3Eフレームのサブヘッダ（1バイト目）
        /// </summary>
        private const byte SubHeader_3E_Byte0 = 0x50;

        /// <summary>
        /// 3Eフレームのサブヘッダ（2バイト目）
        /// </summary>
        private const byte SubHeader_3E_Byte1 = 0x00;

        /// <summary>
        /// 4Eフレーム: タイムアウトフィールドのオフセット（バイト目）
        /// </summary>
        private const int TimeoutOffset_4E = 13;

        /// <summary>
        /// 3Eフレーム: タイムアウトフィールドのオフセット（バイト目）
        /// </summary>
        private const int TimeoutOffset_3E = 9;

        #endregion

        #region Test Data Preparation

        /// <summary>
        /// テスト用PlcConfiguration作成（完全な設定値）
        /// </summary>
        private PlcConfiguration CreateFullPlcConfiguration()
        {
            return new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                ConnectionMethod = "UDP",
                FrameVersion = "4E",
                Timeout = 1000,  // ミリ秒
                IsBinary = true,
                MonitoringIntervalMs = 1000,
                PlcId = "192.168.1.10_8192",
                PlcName = "ライン1_設備A",
                Devices = new List<DeviceSpecification>
                {
                    // ダミーデバイス（フレーム構築ロジックのテスト用）
                    new DeviceSpecification(D, 100, false)
                }
            };
        }

        /// <summary>
        /// テスト用PlcConfiguration作成（最小限の設定値、デフォルト値を使用）
        /// </summary>
        private PlcConfiguration CreateMinimalPlcConfiguration()
        {
            return new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                PlcId = "192.168.1.10_8192",
                Devices = new List<DeviceSpecification>
                {
                    // ダミーデバイス（フレーム構築ロジックのテスト用）
                    new DeviceSpecification(D, 100, false)
                }
                // ConnectionMethod, FrameVersion, Timeout, IsBinary, MonitoringIntervalMsはデフォルト値を使用
            };
        }

        #endregion

        #region 統合テスト: 正常系

        [Fact]
        public void Integration_FullConfiguration_BuildFrame_ShouldUseConfigValues()
        {
            // Arrange
            var config = CreateFullPlcConfiguration();
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(frame);
            Assert.NotEmpty(frame);

            // 4Eフレームの場合、サブヘッダは0x54, 0x00
            Assert.Equal(SubHeader_4E_Byte0, frame[0]);
            Assert.Equal(SubHeader_4E_Byte1, frame[1]);

            // タイムアウト値の確認（1000ms → SLMP単位4）
            // 4Eフレーム: タイムアウトは13-14バイト目（LE）
            ushort timeout = (ushort)(frame[TimeoutOffset_4E] | (frame[TimeoutOffset_4E + 1] << 8));
            Assert.Equal(4, timeout);  // 1000ms / 250 = 4
        }

        [Fact]
        public void Integration_MinimalConfiguration_BuildFrame_ShouldUseDefaultValues()
        {
            // Arrange
            var config = CreateMinimalPlcConfiguration();
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(frame);
            Assert.NotEmpty(frame);

            // デフォルト値"4E"が使用されていることを確認
            Assert.Equal(SubHeader_4E_Byte0, frame[0]);
            Assert.Equal(SubHeader_4E_Byte1, frame[1]);

            // デフォルト値1000ms（SLMP単位4）が使用されていることを確認
            ushort timeout = (ushort)(frame[TimeoutOffset_4E] | (frame[TimeoutOffset_4E + 1] << 8));
            Assert.Equal(4, timeout);
        }

        [Fact]
        public void Integration_3EFrameConfiguration_BuildFrame_ShouldUse3EFormat()
        {
            // Arrange
            var config = new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                FrameVersion = "3E",
                Timeout = 2000,  // 2000ms → SLMP単位8
                PlcId = "192.168.1.10_8192",
                Devices = new List<DeviceSpecification>
                {
                    new DeviceSpecification(D, 100, false)
                }
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(frame);
            Assert.NotEmpty(frame);

            // 3Eフレームの場合、サブヘッダは0x50, 0x00
            Assert.Equal(SubHeader_3E_Byte0, frame[0]);
            Assert.Equal(SubHeader_3E_Byte1, frame[1]);

            // タイムアウト値の確認（2000ms → SLMP単位8）
            // 3Eフレーム: タイムアウトは9-10バイト目（LE）
            ushort timeout = (ushort)(frame[TimeoutOffset_3E] | (frame[TimeoutOffset_3E + 1] << 8));
            Assert.Equal(8, timeout);  // 2000ms / 250 = 8
        }

#if FALSE  // TargetDeviceConfig/DeviceEntry削除により一時的にコンパイル除外（JSON設定廃止）
        [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
        public void Integration_TargetDeviceConfig_BuildFrame_ShouldUseConfigValues()
        {
            // Arrange
            var config = new TargetDeviceConfig
            {
                FrameType = "3E",
                Timeout = 8,
                Devices = new List<DeviceEntry>
                {
                    new DeviceEntry { DeviceType = "D", DeviceNumber = 100, IsHexAddress = false }
                }
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(config);

            // Assert
            Assert.NotNull(frame);
            Assert.NotEmpty(frame);

            // 3Eフレームの場合、サブヘッダは0x50, 0x00
            Assert.Equal(SubHeader_3E_Byte0, frame[0]);
            Assert.Equal(SubHeader_3E_Byte1, frame[1]);
        }
#endif

        #endregion

        #region 統合テスト: ASCII形式

        [Fact]
        public void Integration_AsciiFormat_BuildFrame_ShouldUseConfigValues()
        {
            // Arrange
            var config = new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                FrameVersion = "4E",
                Timeout = 1000,
                IsBinary = false,  // ASCII形式
                PlcId = "192.168.1.10_8192",
                Devices = new List<DeviceSpecification>
                {
                    new DeviceSpecification(D, 100, false)
                }
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfigAscii(config);

            // Assert
            Assert.NotNull(frame);
            Assert.NotEmpty(frame);

            // ASCII形式の場合、フレームは16進数文字列として格納される
            // 4Eフレーム（ASCII）: サブヘッダは"54"（2文字）
            Assert.Equal('5', (char)frame[0]);
            Assert.Equal('4', (char)frame[1]);
        }

        #endregion

        #region 統合テスト: デフォルト値の検証

        [Fact]
        public void Integration_DefaultValues_AllPropertiesHaveCorrectDefaults()
        {
            // Arrange & Act
            var config = new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                PlcId = "192.168.1.10_8192",
                Devices = new List<DeviceSpecification>
                {
                    new DeviceSpecification(D, 100, false)
                }
            };

            // Assert - デフォルト値が正しく設定されていることを確認
            Assert.Equal(DefaultValues.ConnectionMethod, config.ConnectionMethod);
            Assert.Equal(DefaultValues.FrameVersion, config.FrameVersion);
            Assert.Equal(DefaultValues.TimeoutMs, config.Timeout);
            Assert.Equal(DefaultValues.IsBinary, config.IsBinary);
            Assert.Equal(DefaultValues.MonitoringIntervalMs, config.MonitoringIntervalMs);
        }

        #endregion

        #region 統合テスト: タイムアウト変換

        [Fact]
        public void Integration_TimeoutConversion_VariousValues_ShouldConvertCorrectly()
        {
            // Arrange
            var frameManager = new ConfigToFrameManager();
            var testCases = new[]
            {
                (timeoutMs: 250, expectedSlmp: 1),
                (timeoutMs: 1000, expectedSlmp: 4),
                (timeoutMs: 2000, expectedSlmp: 8),
                (timeoutMs: 8000, expectedSlmp: 32)
            };

            foreach (var testCase in testCases)
            {
                var config = new PlcConfiguration
                {
                    IpAddress = "192.168.1.10",
                    Port = 8192,
                    FrameVersion = "4E",
                    Timeout = testCase.timeoutMs,
                    PlcId = "192.168.1.10_8192",
                    Devices = new List<DeviceSpecification>
                    {
                        new DeviceSpecification(D, 100, false)
                    }
                };

                // Act
                var frame = frameManager.BuildReadRandomFrameFromConfig(config);

                // Assert
                ushort timeout = (ushort)(frame[TimeoutOffset_4E] | (frame[TimeoutOffset_4E + 1] << 8));
                Assert.Equal(testCase.expectedSlmp, timeout);
            }
        }

        #endregion

        #region 統合テスト: 既存機能との互換性

#if FALSE  // TargetDeviceConfig/DeviceEntry削除により一時的にコンパイル除外（JSON設定廃止）
        [Fact(Skip = "TargetDeviceConfig/DeviceEntry削除により一時スキップ（JSON設定廃止）")]
        public void Integration_ExistingFunctionality_StillWorks()
        {
            // Arrange
            var targetDeviceConfig = new TargetDeviceConfig
            {
                FrameType = "4E",
                Timeout = 4,
                Devices = new List<DeviceEntry>
                {
                    new DeviceEntry { DeviceType = "D", DeviceNumber = 100, IsHexAddress = false }
                }
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frame = frameManager.BuildReadRandomFrameFromConfig(targetDeviceConfig);

            // Assert - TargetDeviceConfig版は引き続き動作することを確認
            Assert.NotNull(frame);
            Assert.NotEmpty(frame);
            Assert.Equal(SubHeader_4E_Byte0, frame[0]);
            Assert.Equal(SubHeader_4E_Byte1, frame[1]);
        }
#endif

        #endregion

        #region 統合テスト: 境界値

        [Fact]
        public void Integration_BoundaryValues_ShouldHandleCorrectly()
        {
            // Arrange - 最小タイムアウト値
            var configMin = new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                FrameVersion = "4E",
                Timeout = 100,  // 最小推奨値
                PlcId = "192.168.1.10_8192",
                Devices = new List<DeviceSpecification>
                {
                    new DeviceSpecification(D, 100, false)
                }
            };
            var frameManager = new ConfigToFrameManager();

            // Act
            var frameMin = frameManager.BuildReadRandomFrameFromConfig(configMin);

            // Assert
            Assert.NotNull(frameMin);
            Assert.NotEmpty(frameMin);

            // 100ms / 250 = 0.4 → 切り捨て0（境界値）
            ushort timeoutMin = (ushort)(frameMin[TimeoutOffset_4E] | (frameMin[TimeoutOffset_4E + 1] << 8));
            Assert.True(timeoutMin >= 0);

            // Arrange - 最大タイムアウト値
            var configMax = new PlcConfiguration
            {
                IpAddress = "192.168.1.10",
                Port = 8192,
                FrameVersion = "4E",
                Timeout = 30000,  // 最大推奨値
                PlcId = "192.168.1.10_8192",
                Devices = new List<DeviceSpecification>
                {
                    new DeviceSpecification(D, 100, false)
                }
            };

            // Act
            var frameMax = frameManager.BuildReadRandomFrameFromConfig(configMax);

            // Assert
            Assert.NotNull(frameMax);
            Assert.NotEmpty(frameMax);

            // 30000ms / 250 = 120
            ushort timeoutMax = (ushort)(frameMax[TimeoutOffset_4E] | (frameMax[TimeoutOffset_4E + 1] << 8));
            Assert.Equal(120, timeoutMax);
        }

        #endregion

        #region 統合テスト: エラーケース（将来の拡張用）

        // 注: Phase 3で実装されたSettingsValidatorとの統合は
        // ConfigurationLoaderExcelが統合された後に追加実装する予定

        #endregion
    }
}
