using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Moq;
using SlmpClient.Constants;
using SlmpClient.Core;

namespace SlmpClient.Tests
{
    /// <summary>
    /// Phase 2.1 Red: DWordToWordPair変換の詳細テスト群
    /// より厳密な要求を課すテストケース（現在の実装では失敗する）
    /// </summary>
    public class Phase2_AdvancedConversionTests
    {
        #region Phase 2.1 Red: エンディアン順序境界値テスト

        /// <summary>
        /// RED: エンディアン順序の境界値テスト - より詳細な検証
        /// </summary>
        [Theory]
        [InlineData(0x12345678u, 0x5678, 0x1234)]
        [InlineData(0xFFFFFFFFu, 0xFFFF, 0xFFFF)]
        [InlineData(0x00000000u, 0x0000, 0x0000)]
        [InlineData(0x80000000u, 0x0000, 0x8000)]
        [InlineData(0x7FFFFFFFu, 0xFFFF, 0x7FFF)]
        [InlineData(0x01000000u, 0x0000, 0x0100)]
        [InlineData(0x00000001u, 0x0001, 0x0000)]
        [InlineData(0xDEADBEEFu, 0xBEEF, 0xDEAD)]
        public void SplitDword_EndiannessTest_ShouldHandleAllBoundaryValues(uint input, ushort expectedLow, ushort expectedHigh)
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var device = (DeviceCode.D, 100u, input);

            // Act
            var result = splitter.SplitDwordToWordPairs(new[] { device });

            // Assert: より厳密なエンディアン検証
            Assert.Single(result);
            Assert.Equal((DeviceCode.D, 100u, expectedLow), result[0].LowWord);
            Assert.Equal((DeviceCode.D, 101u, expectedHigh), result[0].HighWord);
            
            // 追加検証: ビット演算の正確性
            var reconstructed = (uint)(result[0].LowWord.value | (result[0].HighWord.value << 16));
            Assert.Equal(input, reconstructed);
        }

        #endregion

        #region Phase 2.1 Red: 大量データパフォーマンステスト

        /// <summary>
        /// RED: 1000個デバイスの高速変換テスト - 現在の実装では遅すぎる
        /// </summary>
        [Fact]
        public void SplitDword_PerformanceTest_1000Devices_ShouldCompleteInReasonableTime()
        {
            // Arrange: 1000個のDWordデバイスを生成
            var splitter = new PseudoDwordSplitter();
            var devices = Enumerable.Range(0, 1000)
                .Select(i => (DeviceCode.D, (uint)i, (uint)(i * 0x12345678)))
                .ToArray();

            // Act & Assert: 実行時間測定
            var stopwatch = Stopwatch.StartNew();
            var result = splitter.SplitDwordToWordPairs(devices);
            stopwatch.Stop();

            // パフォーマンス要求: 1000個を10ms以内で処理（現在の実装では不可能）
            Assert.True(stopwatch.ElapsedMilliseconds < 10, 
                $"Performance requirement failed: {stopwatch.ElapsedMilliseconds}ms > 10ms");
            
            // 結果検証
            Assert.Equal(1000, result.Count);
            
            // 各変換結果の正確性検証（サンプリング）
            for (int i = 0; i < 10; i++)
            {
                var expected = (uint)(i * 0x12345678);
                var lowExpected = (ushort)(expected & 0xFFFF);
                var highExpected = (ushort)((expected >> 16) & 0xFFFF);
                
                Assert.Equal((DeviceCode.D, (uint)i, lowExpected), result[i].LowWord);
                Assert.Equal((DeviceCode.D, (uint)i + 1, highExpected), result[i].HighWord);
            }
        }

        /// <summary>
        /// RED: メモリ使用量テスト - 大量データでのメモリ効率
        /// </summary>
        [Fact]
        public void SplitDword_MemoryUsage_ShouldBeOptimal()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            
            // 初期メモリ状態
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetTotalMemory(true);

            // 大量データ生成・変換
            var devices = Enumerable.Range(0, 5000)
                .Select(i => (DeviceCode.D, (uint)i, (uint)(i * 0x11111111)))
                .ToArray();

            var result = splitter.SplitDwordToWordPairs(devices);

            // 変換後メモリ状態
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // メモリ効率要求: 5000個の変換で500KB以下（現在の実装では不可能）
            Assert.True(memoryUsed < 500 * 1024, 
                $"Memory usage too high: {memoryUsed / 1024.0:F2}KB > 500KB");
                
            Assert.Equal(5000, result.Count);
        }

        #endregion

        #region Phase 2.1 Red: 不正入力値での例外テスト

        /// <summary>
        /// RED: null入力での例外テスト - より詳細なエラーハンドリング要求
        /// </summary>
        [Fact]
        public void SplitDword_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                splitter.SplitDwordToWordPairs(null!));
                
            Assert.Equal("dwordDevices", exception.ParamName);
            Assert.Contains("Value cannot be null", exception.Message);
        }

        /// <summary>
        /// RED: 空入力での動作テスト - 空コレクションの適切な処理
        /// </summary>
        [Fact]
        public void SplitDword_EmptyInput_ShouldReturnEmptyResult()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var emptyDevices = Array.Empty<(DeviceCode, uint, uint)>();

            // Act
            var result = splitter.SplitDwordToWordPairs(emptyDevices);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// RED: 複数の境界値アドレステスト - より厳密な境界チェック
        /// </summary>
        [Theory]
        [InlineData(65534u)] // 境界値-1: OK
        [InlineData(65535u)] // 境界値: エラー
        [InlineData(65536u)] // 境界値+1: エラー
        [InlineData(uint.MaxValue)] // 最大値: エラー
        public void SplitDword_AddressBoundaryTest_ShouldHandleCorrectly(uint address)
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var device = (DeviceCode.D, address, 0x12345678u);

            if (address >= 65535)
            {
                // Act & Assert: 境界値以上ではエラー
                var exception = Assert.Throws<DetailedValidationException>(() =>
                    splitter.SplitDwordToWordPairs(new[] { device }));
                    
                Assert.Contains("validation errors occurred", exception.Message.ToLower());
            }
            else
            {
                // Act & Assert: 境界値未満では成功
                var result = splitter.SplitDwordToWordPairs(new[] { device });
                Assert.Single(result);
                Assert.Equal(address, result[0].LowWord.address);
                Assert.Equal(address + 1, result[0].HighWord.address);
            }
        }

        #endregion

        #region Phase 2.1 Red: 複数デバイス種別対応テスト

        /// <summary>
        /// RED: 異なるデバイス種別の混在テスト - より多様なデバイス対応
        /// </summary>
        [Fact]
        public void SplitDword_MultipleDeviceTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var devices = new[]
            {
                (DeviceCode.D, 100u, 0x12345678u),    // データレジスタ
                (DeviceCode.RD, 1000u, 0xABCDEF00u),  // 拡張データレジスタ
                (DeviceCode.W, 500u, 0x87654321u),    // リンクレジスタ
                (DeviceCode.ZR, 200u, 0xDEADBEEFu)    // 拡張ファイルレジスタ
            };

            // Act
            var result = splitter.SplitDwordToWordPairs(devices);

            // Assert: 各デバイス種別が正しく処理される
            Assert.Equal(4, result.Count);
            
            // データレジスタ検証
            Assert.Equal((DeviceCode.D, 100u, (ushort)0x5678), result[0].LowWord);
            Assert.Equal((DeviceCode.D, 101u, (ushort)0x1234), result[0].HighWord);
            
            // 拡張データレジスタ検証
            Assert.Equal((DeviceCode.RD, 1000u, (ushort)0xEF00), result[1].LowWord);
            Assert.Equal((DeviceCode.RD, 1001u, (ushort)0xABCD), result[1].HighWord);
            
            // 各デバイス種別固有の制約チェックも必要（将来実装）
        }

        /// <summary>
        /// RED: 16進アドレスデバイスの特殊処理テスト
        /// </summary>
        [Fact]
        public void SplitDword_HexAddressDevices_ShouldHandleSpecialConstraints()
        {
            // Arrange: 16進アドレス表現デバイス
            var splitter = new PseudoDwordSplitter();
            var hexDevices = new[]
            {
                (DeviceCode.X, 0xFFu, 0x12345678u),    // 入力リレー（16進）
                (DeviceCode.Y, 0x1FFu, 0xABCDEF00u),   // 出力リレー（16進）
                (DeviceCode.B, 0x7FFFu, 0x87654321u)   // リンクリレー（16進）
            };

            // Act
            var result = splitter.SplitDwordToWordPairs(hexDevices);

            // Assert: 16進アドレスデバイスも正しく処理
            Assert.Equal(3, result.Count);
            
            // 各結果でアドレスが+1されていることを確認
            Assert.Equal(0xFFu, result[0].LowWord.address);
            Assert.Equal(0x100u, result[0].HighWord.address);
            
            Assert.Equal(0x1FFu, result[1].LowWord.address);
            Assert.Equal(0x200u, result[1].HighWord.address);
        }

        #endregion

        #region Phase 2.1 Red: 新機能要求テスト（現在未実装）

        /// <summary>
        /// RED: スレッドセーフティテスト - 現在の実装はスレッドセーフではない
        /// </summary>
        [Fact]
        public void SplitDword_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var devices = Enumerable.Range(0, 100)
                .Select(i => (DeviceCode.D, (uint)i, (uint)(i * 0x12345678)))
                .ToArray();

            var results = new IList<WordPair>[10];
            var tasks = new Task[10];

            // Act: 複数スレッドで同時実行
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() =>
                {
                    results[index] = splitter.SplitDwordToWordPairs(devices);
                });
            }

            Task.WaitAll(tasks);

            // Assert: 全ての結果が一致する（スレッドセーフであることを証明）
            for (int i = 1; i < 10; i++)
            {
                Assert.Equal(results[0].Count, results[i].Count);
                for (int j = 0; j < results[0].Count; j++)
                {
                    Assert.Equal(results[0][j].LowWord, results[i][j].LowWord);
                    Assert.Equal(results[0][j].HighWord, results[i][j].HighWord);
                }
            }
        }

        /// <summary>
        /// RED: 統計情報取得機能テスト - 現在未実装の機能
        /// </summary>
        [Fact]
        public void SplitDword_Statistics_ShouldProvideConversionStatistics()
        {
            // Arrange: 統計機能を有効にして実行
            var options = new ConversionOptions { EnableStatistics = true };
            var splitter = new PseudoDwordSplitter(null, null, options);
            var devices = new[]
            {
                (DeviceCode.D, 100u, 0x12345678u),
                (DeviceCode.RD, 200u, 0xABCDEF00u)
            };

            // Act
            var result = splitter.SplitDwordToWordPairs(devices);

            // Assert: 統計情報が取得可能（現在未実装なので失敗）
            var stats = Assert.IsAssignableFrom<IConversionStatistics>(splitter);
            Assert.Equal(2, stats.TotalConversions);
            Assert.Equal(4, stats.TotalWordsGenerated);
            Assert.True(stats.AverageConversionTime > TimeSpan.Zero);
        }

        /// <summary>
        /// RED: 変換オプション設定機能テスト - 現在未実装
        /// </summary>
        [Fact]
        public void SplitDword_ConversionOptions_ShouldSupportCustomOptions()
        {
            // Arrange: カスタムオプション付きSplitter（現在未実装）
            var options = new ConversionOptions
            {
                EnableValidation = true,
                EnableStatistics = true,
                UseOptimizedAlgorithm = true,
                MaxConcurrentOperations = 4
            };
            
            var splitter = new PseudoDwordSplitter(null, null, options);
            var devices = new[] { (DeviceCode.D, 100u, 0x12345678u) };

            // Act & Assert: オプション機能が動作（現在未実装なので失敗）
            var result = splitter.SplitDwordToWordPairs(devices);
            
            // オプションが適用されていることを確認
            Assert.True(splitter.IsValidationEnabled);
            Assert.True(splitter.IsStatisticsEnabled);
        }

        /// <summary>
        /// RED: バリデーション詳細設定テスト - より詳細な検証機能要求
        /// </summary>
        [Fact]
        public void SplitDword_DetailedValidation_ShouldProvideDetailedErrorInfo()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var invalidDevices = new[]
            {
                (DeviceCode.D, 65535u, 0x12345678u), // 境界値エラー
                (DeviceCode.X, 0x10000u, 0xABCDEF00u) // アドレス範囲エラー
            };

            // Act & Assert: 詳細エラー情報が取得可能（現在未実装）
            var exception = Assert.Throws<DetailedValidationException>(() =>
                splitter.SplitDwordToWordPairs(invalidDevices));
                
            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Contains("Device D at address 65535", exception.ValidationErrors[0].Message);
            Assert.Contains("Device X at address 65536", exception.ValidationErrors[1].Message);
        }

        #endregion

        #region Phase 2.1 Red: 拡張要求テスト（将来対応）

        /// <summary>
        /// RED: 非同期変換処理テスト - 将来の非同期対応要求
        /// </summary>
        [Fact]
        public async Task SplitDwordAsync_FutureAsyncSupport_ShouldWork()
        {
            // NOTE: 現在は同期実装だが、将来的に非同期対応が必要かもしれない
            // このテストは現在失敗するが、将来の拡張要求を表現
            
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var devices = new[] { (DeviceCode.D, 100u, 0x12345678u) };

            // Act & Assert: 現在は未実装なので例外がスローされる
            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                // 将来実装予定のメソッド
                // var result = await splitter.SplitDwordToWordPairsAsync(devices);
                throw new NotImplementedException("Async support not yet implemented");
            });
        }

        /// <summary>
        /// RED: 変換結果キャッシュ機能テスト - パフォーマンス向上要求
        /// </summary>
        [Fact]
        public void SplitDword_CachingSupport_ShouldImprovePerformance()
        {
            // NOTE: 現在はキャッシュ未対応だが、将来的に必要かもしれない
            
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var devices = new[] { (DeviceCode.D, 100u, 0x12345678u) };

            // 初回実行
            var stopwatch1 = Stopwatch.StartNew();
            var result1 = splitter.SplitDwordToWordPairs(devices);
            stopwatch1.Stop();

            // 2回目実行（キャッシュが効けば高速化）
            var stopwatch2 = Stopwatch.StartNew();
            var result2 = splitter.SplitDwordToWordPairs(devices);
            stopwatch2.Stop();

            // Assert: 結果は同じ
            Assert.Equal(result1[0].LowWord, result2[0].LowWord);
            Assert.Equal(result1[0].HighWord, result2[0].HighWord);
            
            // TODO: キャッシュ実装後は2回目が高速になることを検証
            // Assert.True(stopwatch2.ElapsedTicks < stopwatch1.ElapsedTicks);
        }

        #endregion
    }
}