using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SlmpClient.Constants;
using SlmpClient.Core;

namespace SlmpClient.Examples
{
    /// <summary>
    /// Phase 4: 混合デバイス読み取り機能の使用例
    /// 擬似ダブルワード統合機能のデモンストレーション
    /// </summary>
    public class Phase4_MixedDeviceExample
    {
        private readonly ILogger<Phase4_MixedDeviceExample> _logger;

        public Phase4_MixedDeviceExample(ILogger<Phase4_MixedDeviceExample> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 基本的な混合デバイス読み取りの例
        /// 実装計画書のPhase 4.3の最終使用例に基づく
        /// </summary>
        public async Task BasicMixedDeviceReadExample()
        {
            _logger.LogInformation("=== Phase 4: Basic Mixed Device Read Example ===");

            // Phase 4.3の最終使用例
            var client = new Core.SlmpClient("192.168.1.100", settings: null);
            await client.ConnectAsync();

            try
            {
                (ushort[] wordData, bool[] bitData, uint[] dwordData) = await client.ReadMixedDevicesAsync(
                    wordDevices: new[]
                    {
                        (DeviceCode.D, 100u),  // D100
                        (DeviceCode.D, 200u)   // D200
                    },
                    bitDevices: new[]
                    {
                        (DeviceCode.M, 10u),   // M10
                        (DeviceCode.M, 20u)    // M20
                    },
                    dwordDevices: new[]
                    {
                        (DeviceCode.D, 300u),  // D300 (内部分割→D300,D301→結合)
                        (DeviceCode.D, 400u)   // D400 (内部分割→D400,D401→結合)
                    }
                );

                // 結果表示
                _logger.LogInformation("Word devices read successfully:");
                _logger.LogInformation("  D100 = {Value}", wordData[0]);
                _logger.LogInformation("  D200 = {Value}", wordData[1]);

                _logger.LogInformation("Bit devices read successfully:");
                _logger.LogInformation("  M10 = {Value}", bitData[0]);
                _logger.LogInformation("  M20 = {Value}", bitData[1]);

                _logger.LogInformation("DWord devices read successfully (split internally):");
                _logger.LogInformation("  D300-D301 combined = 0x{Value:X8} ({Value})", dwordData[0], dwordData[0]);
                _logger.LogInformation("  D400-D401 combined = 0x{Value:X8} ({Value})", dwordData[1], dwordData[1]);
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }

        /// <summary>
        /// 大量デバイス読み取りの例（SLMP制限値検証を含む）
        /// </summary>
        public async Task LargeScaleDeviceReadExample()
        {
            _logger.LogInformation("=== Phase 4: Large Scale Device Read Example ===");

            var client = new Core.SlmpClient("192.168.1.100", settings: null);
            await client.ConnectAsync();

            try
            {
                // 大量デバイス読み取り（制限値ギリギリ）
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

                // DWord devices: 40個（内部で80のWordに展開される）
                // 総Word数 = 100 + (40 * 2) = 180 < 960 ✓
                for (uint i = 0; i < 40; i++)
                {
                    dwordDevices.Add((DeviceCode.D, 2000 + i * 2)); // 偶数アドレス
                }

                _logger.LogInformation("Reading {WordCount} words, {BitCount} bits, {DwordCount} dwords",
                    wordDevices.Count, bitDevices.Count, dwordDevices.Count);

                (ushort[] wordData, bool[] bitData, uint[] dwordData) = await client.ReadMixedDevicesAsync(
                    wordDevices, bitDevices, dwordDevices);

                _logger.LogInformation("Successfully read all devices:");
                _logger.LogInformation("  Word data length: {Length}", wordData.Length);
                _logger.LogInformation("  Bit data length: {Length}", bitData.Length);
                _logger.LogInformation("  DWord data length: {Length}", dwordData.Length);

                // サンプル結果表示
                if (wordData.Length > 0)
                    _logger.LogInformation("  First word (D1000): {Value}", wordData[0]);
                if (bitData.Length > 0)
                    _logger.LogInformation("  First bit (M100): {Value}", bitData[0]);
                if (dwordData.Length > 0)
                    _logger.LogInformation("  First dword (D2000-D2001): 0x{Value:X8}", dwordData[0]);
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }

        /// <summary>
        /// エラーハンドリング機能の例
        /// </summary>
        public async Task ErrorHandlingExample()
        {
            _logger.LogInformation("=== Phase 4: Error Handling Example ===");

            // 継続モード設定
            var settings = new SlmpConnectionSettings
            {
                ContinuitySettings = new ContinuitySettings
                {
                    Mode = ErrorHandlingMode.ReturnDefaultAndContinue,
                    DefaultWordValue = 0xFFFF,
                    DefaultBitValue = true,
                    EnableErrorStatistics = true
                }
            };

            var client = new Core.SlmpClient("192.168.1.100", settings);
            await client.ConnectAsync();

            try
            {
                // 意図的に無効なアドレスを含むデバイス群
                (ushort[] wordData, bool[] bitData, uint[] dwordData) = await client.ReadMixedDevicesAsync(
                    wordDevices: new[]
                    {
                        (DeviceCode.D, 100u),   // 正常
                        (DeviceCode.D, 65535u)  // 境界値（エラーの可能性）
                    },
                    bitDevices: new[]
                    {
                        (DeviceCode.M, 10u)     // 正常
                    },
                    dwordDevices: new[]
                    {
                        (DeviceCode.D, 300u),   // 正常
                        (DeviceCode.D, 65534u)  // エラー（65535が必要だがオーバーフロー）
                    }
                );

                _logger.LogInformation("Read completed with error handling:");
                _logger.LogInformation("  Word data: [{Values}]", string.Join(", ", wordData));
                _logger.LogInformation("  Bit data: [{Values}]", string.Join(", ", bitData));
                _logger.LogInformation("  DWord data: [0x{Value1:X8}, 0x{Value2:X8}]", 
                    dwordData.Length > 0 ? dwordData[0] : 0,
                    dwordData.Length > 1 ? dwordData[1] : 0);

                // エラー統計確認
                var stats = client.ErrorStatistics;
                _logger.LogInformation("Error statistics:");
                _logger.LogInformation("  Total operations: {Count}", stats.TotalOperations);
                _logger.LogInformation("  Total errors: {Count}", stats.TotalErrors);
                _logger.LogInformation("  Continued operations: {Count}", stats.TotalContinuedOperations);
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }

        /// <summary>
        /// パフォーマンス測定の例
        /// </summary>
        public async Task PerformanceMeasurementExample()
        {
            _logger.LogInformation("=== Phase 4: Performance Measurement Example ===");

            var client = new Core.SlmpClient("192.168.1.100", settings: null);
            await client.ConnectAsync();

            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // 複数回実行してパフォーマンスを測定
                const int iterations = 10;
                for (int i = 0; i < iterations; i++)
                {
                    (ushort[] wordData, bool[] bitData, uint[] dwordData) = await client.ReadMixedDevicesAsync(
                        wordDevices: new[]
                        {
                            (DeviceCode.D, 100u),
                            (DeviceCode.D, 101u),
                            (DeviceCode.D, 102u)
                        },
                        bitDevices: new[]
                        {
                            (DeviceCode.M, 10u),
                            (DeviceCode.M, 11u)
                        },
                        dwordDevices: new[]
                        {
                            (DeviceCode.D, 200u),
                            (DeviceCode.D, 202u)
                        }
                    );

                    if (i == 0)
                    {
                        _logger.LogInformation("First iteration results:");
                        _logger.LogInformation("  Words: [{Values}]", string.Join(", ", wordData));
                        _logger.LogInformation("  Bits: [{Values}]", string.Join(", ", bitData));
                        _logger.LogInformation("  DWords: [0x{Value1:X8}, 0x{Value2:X8}]", dwordData[0], dwordData[1]);
                    }
                }

                stopwatch.Stop();

                var averageTime = stopwatch.ElapsedMilliseconds / (double)iterations;
                _logger.LogInformation("Performance measurement completed:");
                _logger.LogInformation("  Iterations: {Count}", iterations);
                _logger.LogInformation("  Total time: {Time}ms", stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("  Average time per operation: {Time:F2}ms", averageTime);
                _logger.LogInformation("  Operations per second: {Rate:F1}", 1000.0 / averageTime);
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }

        /// <summary>
        /// SLMP制限値検証の例
        /// </summary>
        public async Task ConstraintValidationExample()
        {
            _logger.LogInformation("=== Phase 4: Constraint Validation Example ===");

            var client = new Core.SlmpClient("192.168.1.100", settings: null);
            await client.ConnectAsync();

            try
            {
                // 制限値内での正常なケース
                _logger.LogInformation("Testing valid constraints...");
                (ushort[] wordData, bool[] bitData, uint[] dwordData) = await client.ReadMixedDevicesAsync(
                    wordDevices: new[] { (DeviceCode.D, 100u) },
                    bitDevices: new[] { (DeviceCode.M, 10u) },
                    dwordDevices: new[] { (DeviceCode.D, 200u) }
                );
                _logger.LogInformation("Valid constraint test passed");

                // 制限値超過のテスト
                _logger.LogInformation("Testing constraint violations...");

                try
                {
                    // DWordデバイス数制限超過（481 > 480）
                    var largeDwordDevices = new List<(DeviceCode, uint)>();
                    for (uint i = 0; i < 481; i++)
                    {
                        largeDwordDevices.Add((DeviceCode.D, 1000 + i * 2));
                    }

                    await client.ReadMixedDevicesAsync(
                        new List<(DeviceCode, uint)>(),
                        new List<(DeviceCode, uint)>(),
                        largeDwordDevices
                    );

                    _logger.LogWarning("Constraint validation failed - should have thrown exception");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("Constraint validation working correctly: {Message}", ex.Message);
                }

                try
                {
                    // アドレス境界違反のテスト
                    await client.ReadMixedDevicesAsync(
                        new List<(DeviceCode, uint)>(),
                        new List<(DeviceCode, uint)>(),
                        new[] { (DeviceCode.D, 65535u) } // 65535 + 1 = 65536 (overflow)
                    );

                    _logger.LogWarning("Address boundary validation failed - should have thrown exception");
                }
                catch (ArgumentException ex)
                {
                    _logger.LogInformation("Address boundary validation working correctly: {Message}", ex.Message);
                }
            }
            finally
            {
                await client.DisconnectAsync();
                await client.DisposeAsync();
            }
        }

        /// <summary>
        /// 全てのテスト例を実行
        /// </summary>
        public async Task RunAllExamples()
        {
            _logger.LogInformation("Starting Phase 4 Mixed Device Examples...");

            try
            {
                await BasicMixedDeviceReadExample();
                await Task.Delay(1000);

                await LargeScaleDeviceReadExample();
                await Task.Delay(1000);

                await ErrorHandlingExample();
                await Task.Delay(1000);

                await PerformanceMeasurementExample();
                await Task.Delay(1000);

                await ConstraintValidationExample();

                _logger.LogInformation("All Phase 4 examples completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running Phase 4 examples: {Message}", ex.Message);
                throw;
            }
        }
    }
}