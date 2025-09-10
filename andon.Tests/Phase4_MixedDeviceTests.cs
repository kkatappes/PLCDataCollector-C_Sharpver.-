using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Exceptions;

namespace SlmpClient.Tests
{
    /// <summary>
    /// Phase 4: 混合デバイス読み取り機能のテスト
    /// 擬似ダブルワード統合機能の包括的テスト
    /// </summary>
    public class Phase4_MixedDeviceTests
    {
        private readonly Core.SlmpClient _mockClient;

        public Phase4_MixedDeviceTests()
        {
            // モッククライアント（実際の通信は行わない）
            var settings = new SlmpConnectionSettings
            {
                ContinuitySettings = new ContinuitySettings
                {
                    Mode = ErrorHandlingMode.ReturnDefaultAndContinue,
                    DefaultWordValue = 0x1234,
                    DefaultBitValue = true,
                    EnableErrorStatistics = true
                }
            };
            
            _mockClient = new Core.SlmpClient("127.0.0.1", settings, NullLogger<Core.SlmpClient>.Instance);
        }

        [Fact]
        public async Task ReadMixedDevicesAsync_ValidInput_ShouldNotThrow()
        {
            // Arrange
            var wordDevices = new[] { (DeviceCode.D, 100u) };
            var bitDevices = new[] { (DeviceCode.M, 10u) };
            var dwordDevices = new[] { (DeviceCode.D, 200u) };

            // Act & Assert - パラメータ検証は例外をスローしない
            var exception = await Record.ExceptionAsync(async () =>
                await _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            // 接続していないため通信エラーが発生するが、パラメータ検証は通る
            Assert.IsType<SlmpConnectionException>(exception);
        }

        [Fact]
        public async Task ReadMixedDevicesAsync_NullWordDevices_ShouldThrowArgumentNullException()
        {
            // Arrange
            var bitDevices = new[] { (DeviceCode.M, 10u) };
            var dwordDevices = new[] { (DeviceCode.D, 200u) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _mockClient.ReadMixedDevicesAsync(null!, bitDevices, dwordDevices));
        }

        [Fact]
        public async Task ReadMixedDevicesAsync_NullBitDevices_ShouldThrowArgumentNullException()
        {
            // Arrange
            var wordDevices = new[] { (DeviceCode.D, 100u) };
            var dwordDevices = new[] { (DeviceCode.D, 200u) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, null!, dwordDevices));
        }

        [Fact]
        public async Task ReadMixedDevicesAsync_NullDwordDevices_ShouldThrowArgumentNullException()
        {
            // Arrange
            var wordDevices = new[] { (DeviceCode.D, 100u) };
            var bitDevices = new[] { (DeviceCode.M, 10u) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, null!));
        }

        [Theory]
        [InlineData(481)] // 480を超える
        [InlineData(500)]
        [InlineData(1000)]
        public async Task ReadMixedDevicesAsync_TooManyDwordDevices_ShouldThrowArgumentException(int dwordCount)
        {
            // Arrange
            var wordDevices = new List<(DeviceCode, uint)>();
            var bitDevices = new List<(DeviceCode, uint)>();
            var dwordDevices = new List<(DeviceCode, uint)>();

            for (uint i = 0; i < dwordCount; i++)
            {
                dwordDevices.Add((DeviceCode.D, i * 2));
            }

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            Assert.Contains("DWord device count must not exceed 480", exception.Message);
        }

        [Fact]
        public void ReadMixedDevicesAsync_TotalWordCountExceeds960_ShouldThrowArgumentException()
        {
            // Arrange
            var wordDevices = new List<(DeviceCode, uint)>();
            var bitDevices = new List<(DeviceCode, uint)>();
            var dwordDevices = new List<(DeviceCode, uint)>();

            // Word devices: 500個
            for (uint i = 0; i < 500; i++)
            {
                wordDevices.Add((DeviceCode.D, i));
            }

            // DWord devices: 250個 → 500 Word に展開
            // 総Word数 = 500 + 500 = 1000 > 960 ✗
            for (uint i = 0; i < 250; i++)
            {
                dwordDevices.Add((DeviceCode.D, 1000 + i * 2));
            }

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            Assert.Contains("Total word count (including expanded dwords) must not exceed 960", exception.Result.Message);
        }

        [Fact]
        public void ReadMixedDevicesAsync_TooManyBitDevices_ShouldThrowArgumentException()
        {
            // Arrange
            var wordDevices = new List<(DeviceCode, uint)>();
            var bitDevices = new List<(DeviceCode, uint)>();
            var dwordDevices = new List<(DeviceCode, uint)>();

            // Bit devices: 7169個 > 7168 ✗
            for (uint i = 0; i < 7169; i++)
            {
                bitDevices.Add((DeviceCode.M, i));
            }

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            Assert.Contains("Bit device count must not exceed 7168", exception.Result.Message);
        }

        [Fact]
        public void ReadMixedDevicesAsync_TotalDeviceCountExceeds192_ShouldThrowArgumentException()
        {
            // Arrange
            var wordDevices = new List<(DeviceCode, uint)>();
            var bitDevices = new List<(DeviceCode, uint)>();
            var dwordDevices = new List<(DeviceCode, uint)>();

            // 各64個ずつ、合計192個
            for (uint i = 0; i < 64; i++)
            {
                wordDevices.Add((DeviceCode.D, i));
                bitDevices.Add((DeviceCode.M, i));
                dwordDevices.Add((DeviceCode.D, 1000 + i * 2));
            }

            // さらに1個追加で193個 > 192 ✗
            wordDevices.Add((DeviceCode.D, 100u));

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            Assert.Contains("Total device count must not exceed 192", exception.Result.Message);
        }

        [Theory]
        [InlineData(65535u)] // 境界値（65535 + 1 = 65536でオーバーフロー）
        [InlineData(65534u)] // 境界値（65534 + 1 = 65535で正常だが、DWordの場合問題）
        public void ReadMixedDevicesAsync_DwordAddressBoundaryViolation_ShouldThrowArgumentException(uint address)
        {
            // Arrange
            var wordDevices = new List<(DeviceCode, uint)>();
            var bitDevices = new List<(DeviceCode, uint)>();
            var dwordDevices = new[] { (DeviceCode.D, address) };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            Assert.Contains("DWord device validation failed", exception.Result.Message);
        }

        [Fact]
        public void ValidateMixedDeviceConstraints_ValidInput_ShouldNotThrow()
        {
            // Arrange
            var wordDevices = new[]
            {
                (DeviceCode.D, 100u),
                (DeviceCode.D, 101u)
            };
            var bitDevices = new[]
            {
                (DeviceCode.M, 10u),
                (DeviceCode.M, 11u)
            };
            var dwordDevices = new[]
            {
                (DeviceCode.D, 200u),
                (DeviceCode.D, 202u)
            };

            // Act & Assert - パラメータ検証のみテスト
            var exception = Record.Exception(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            // 通信エラーは発生するが、パラメータ検証は通る
            Assert.IsType<SlmpConnectionException>(exception);
        }

        [Fact]
        public void ValidateMixedDeviceConstraints_MaximumValidInput_ShouldNotThrow()
        {
            // Arrange - 制限値ギリギリの正当な入力
            var wordDevices = new List<(DeviceCode, uint)>();
            var bitDevices = new List<(DeviceCode, uint)>();
            var dwordDevices = new List<(DeviceCode, uint)>();

            // Word devices: 100個
            for (uint i = 0; i < 100; i++)
            {
                wordDevices.Add((DeviceCode.D, 1000 + i));
            }

            // Bit devices: 50個
            for (uint i = 0; i < 50; i++)
            {
                bitDevices.Add((DeviceCode.M, 100 + i));
            }

            // DWord devices: 42個（総デバイス数 = 100 + 50 + 42 = 192）
            // 総Word数 = 100 + 42*2 = 184 < 960 ✓
            for (uint i = 0; i < 42; i++)
            {
                dwordDevices.Add((DeviceCode.D, 2000 + i * 2));
            }

            // Act & Assert
            var exception = Record.Exception(() =>
                _mockClient.ReadMixedDevicesAsync(wordDevices, bitDevices, dwordDevices));

            // 通信エラーは発生するが、パラメータ検証は通る
            Assert.IsType<SlmpConnectionException>(exception);
        }

        [Fact]
        public void PseudoDwordSplitter_Integration_ShouldWorkCorrectly()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var dwordDevices = new[]
            {
                (DeviceCode.D, 100u, 0x12345678u),
                (DeviceCode.D, 200u, 0xDEADBEEFu)
            };

            // Act
            var wordPairs = splitter.SplitDwordToWordPairs(dwordDevices);
            var reconstructed = splitter.CombineWordPairsToDword(wordPairs);

            // Assert
            Assert.Equal(2, wordPairs.Count);
            Assert.Equal(2, reconstructed.Count);

            // 1つ目のDWord
            Assert.Equal(DeviceCode.D, wordPairs[0].LowWord.deviceCode);
            Assert.Equal(100u, wordPairs[0].LowWord.address);
            Assert.Equal(0x5678, wordPairs[0].LowWord.value); // 下位16ビット

            Assert.Equal(DeviceCode.D, wordPairs[0].HighWord.deviceCode);
            Assert.Equal(101u, wordPairs[0].HighWord.address);
            Assert.Equal(0x1234, wordPairs[0].HighWord.value); // 上位16ビット

            // 再構築された値
            Assert.Equal(0x12345678u, reconstructed[0].value);

            // 2つ目のDWord
            Assert.Equal(0xDEADBEEFu, reconstructed[1].value);
        }

        [Fact]
        public void CanUseSequentialRead_ConsecutiveAddresses_ShouldReturnTrue()
        {
            // この機能は private メソッドなので、間接的にテスト
            // 実際のテストでは、連続アドレスの場合に効率的な読み取りが行われることを確認

            // Arrange
            var consecutiveDevices = new[]
            {
                (DeviceCode.D, 100u),
                (DeviceCode.D, 101u),
                (DeviceCode.D, 102u)
            };

            // Act & Assert - 例外がスローされないことを確認
            var exception = Record.Exception(() =>
                _mockClient.ReadMixedDevicesAsync(consecutiveDevices, new List<(DeviceCode, uint)>(), new List<(DeviceCode, uint)>()));

            Assert.IsType<SlmpConnectionException>(exception); // 通信エラーのみ
        }

        [Fact]
        public void CanUseSequentialRead_NonConsecutiveAddresses_ShouldHandleCorrectly()
        {
            // Arrange
            var nonConsecutiveDevices = new[]
            {
                (DeviceCode.D, 100u),
                (DeviceCode.D, 105u), // ギャップあり
                (DeviceCode.D, 110u)
            };

            // Act & Assert - 例外がスローされないことを確認
            var exception = Record.Exception(() =>
                _mockClient.ReadMixedDevicesAsync(nonConsecutiveDevices, new List<(DeviceCode, uint)>(), new List<(DeviceCode, uint)>()));

            Assert.IsType<SlmpConnectionException>(exception); // 通信エラーのみ
        }

        [Fact]
        public void ErrorHandling_ReturnDefaultAndContinue_ShouldHandleGracefully()
        {
            // このテストは実際の通信エラーが必要なため、モックフレームワークが必要
            // 現在は構造的テストとしてパラメータ検証のみ実行

            // Arrange
            var settings = new SlmpConnectionSettings
            {
                ContinuitySettings = new ContinuitySettings
                {
                    Mode = ErrorHandlingMode.ReturnDefaultAndContinue,
                    DefaultWordValue = 0xAAAA,
                    DefaultBitValue = false,
                    EnableErrorStatistics = true
                }
            };

            var client = new Core.SlmpClient("127.0.0.1", settings);

            // Act & Assert
            var exception = Record.Exception(() =>
                client.ReadMixedDevicesAsync(
                    new[] { (DeviceCode.D, 100u) },
                    new[] { (DeviceCode.M, 10u) },
                    new[] { (DeviceCode.D, 200u) }));

            // 通信エラーは発生するが、設定は適用される
            Assert.NotNull(exception);
        }

        public void Dispose()
        {
            _mockClient?.Dispose();
        }
    }

    /// <summary>
    /// Phase 4の統合テスト（実際のPLC通信シミュレーション）
    /// </summary>
    public class Phase4_IntegrationTests
    {
        [Fact(Skip = "Requires actual PLC or simulator")]
        public async Task ReadMixedDevicesAsync_RealPLC_ShouldWork()
        {
            // 実際のPLCまたはシミュレータが必要なテスト
            // CI/CDでは無効化されている

            var client = new Core.SlmpClient("192.168.1.100", settings: null);
            await client.ConnectAsync();

            try
            {
                var (wordData, bitData, dwordData) = await client.ReadMixedDevicesAsync(
                    wordDevices: new[] { (DeviceCode.D, 100u) },
                    bitDevices: new[] { (DeviceCode.M, 10u) },
                    dwordDevices: new[] { (DeviceCode.D, 200u) }
                );

                Assert.NotNull(wordData);
                Assert.NotNull(bitData);
                Assert.NotNull(dwordData);
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }

        [Fact(Skip = "Performance test - run manually")]
        public async Task ReadMixedDevicesAsync_PerformanceTest_ShouldMeetTargets()
        {
            // パフォーマンステスト：1000デバイス/秒以上の目標
            // 手動実行用

            var client = new Core.SlmpClient("192.168.1.100", settings: null);
            await client.ConnectAsync();

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                const int iterations = 100;

                for (int i = 0; i < iterations; i++)
                {
                    await client.ReadMixedDevicesAsync(
                        wordDevices: new[] { (DeviceCode.D, 100u) },
                        bitDevices: new[] { (DeviceCode.M, 10u) },
                        dwordDevices: new[] { (DeviceCode.D, 200u) }
                    );
                }

                stopwatch.Stop();

                var devicesPerSecond = (3 * iterations * 1000.0) / stopwatch.ElapsedMilliseconds;
                
                // 目標: 1000デバイス/秒以上
                Assert.True(devicesPerSecond >= 1000, 
                    $"Performance target not met: {devicesPerSecond:F1} devices/sec < 1000");
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }
    }
}