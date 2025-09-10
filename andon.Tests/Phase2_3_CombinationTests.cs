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
    /// Phase 2.3 Red: WordPairToDWord結合の詳細テスト群
    /// より厳密な要求を課すテストケース（現在の実装では失敗する）
    /// </summary>
    public class Phase2_3_CombinationTests
    {
        #region Phase 2.3 Red: WordPair結合境界値テスト

        /// <summary>
        /// RED: WordPair結合の境界値テスト - より詳細な検証
        /// </summary>
        [Theory]
        [InlineData(0x1234, 0x5678, 0x56781234u)] // リトルエンディアン結合
        [InlineData(0xFFFF, 0xFFFF, 0xFFFFFFFFu)] // 最大値結合
        [InlineData(0x0000, 0x0000, 0x00000000u)] // 最小値結合
        [InlineData(0x8000, 0x0000, 0x00008000u)] // ビット境界
        [InlineData(0x7FFF, 0xFFFF, 0xFFFF7FFFu)] // 最大境界
        [InlineData(0xDEAD, 0xBEEF, 0xBEEFDEADu)] // 特殊値
        public void CombineWordPair_BoundaryValueTest_ShouldCombineCorrectly(ushort lowValue, ushort highValue, uint expectedDword)
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var wordPair = new WordPair
            {
                LowWord = (DeviceCode.D, 100u, lowValue),
                HighWord = (DeviceCode.D, 101u, highValue)
            };

            // Act
            var result = splitter.CombineWordPairsToDword(new[] { wordPair });

            // Assert: より厳密な結合検証
            Assert.Single(result);
            Assert.Equal(DeviceCode.D, result[0].deviceCode);
            Assert.Equal(100u, result[0].address);
            Assert.Equal(expectedDword, result[0].value);

            // 追加検証: ビット演算の正確性
            var reconstructedLow = (ushort)(result[0].value & 0xFFFF);
            var reconstructedHigh = (ushort)((result[0].value >> 16) & 0xFFFF);
            Assert.Equal(lowValue, reconstructedLow);
            Assert.Equal(highValue, reconstructedHigh);
        }

        #endregion

        #region Phase 2.3 Red: 整合性検証テスト（失敗するテスト）

        /// <summary>
        /// RED: アドレス不整合エラーテスト - 現在の実装では検証不十分
        /// </summary>
        [Fact]
        public void CombineWordPair_AddressMismatch_ShouldThrowDetailedValidationException()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var invalidWordPair = new WordPair
            {
                LowWord = (DeviceCode.D, 100u, 0x1234),
                HighWord = (DeviceCode.D, 102u, 0x5678) // 不正: 101であるべき
            };

            // Act & Assert: アドレス不整合で詳細バリデーション例外
            var exception = Assert.Throws<DetailedValidationException>(() =>
                splitter.CombineWordPairsToDword(new[] { invalidWordPair }));

            Assert.Contains("validation errors occurred", exception.Message.ToLower());
            Assert.Contains("expected 101", exception.ValidationErrors[0].Message.ToLower());
        }

        /// <summary>
        /// RED: デバイスコード不一致テスト - 現在の実装では検証不十分
        /// </summary>
        [Fact]
        public void CombineWordPair_DeviceCodeMismatch_ShouldThrowDetailedValidationException()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var invalidWordPair = new WordPair
            {
                LowWord = (DeviceCode.D, 100u, 0x1234),
                HighWord = (DeviceCode.W, 101u, 0x5678) // 不正: Dデバイスであるべき
            };

            // Act & Assert: デバイスコード不一致で詳細バリデーション例外
            var exception = Assert.Throws<DetailedValidationException>(() =>
                splitter.CombineWordPairsToDword(new[] { invalidWordPair }));

            Assert.Contains("validation errors occurred", exception.Message.ToLower());
            Assert.Contains("different device codes", exception.ValidationErrors[0].Message.ToLower());
        }

        /// <summary>
        /// RED: null WordPair入力での例外テスト - より詳細なエラーハンドリング要求
        /// </summary>
        [Fact]
        public void CombineWordPair_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                splitter.CombineWordPairsToDword(null!));

            Assert.Equal("wordPairs", exception.ParamName);
            Assert.Contains("Value cannot be null", exception.Message);
        }

        /// <summary>
        /// RED: 空WordPair入力での動作テスト - 空コレクションの適切な処理
        /// </summary>
        [Fact]
        public void CombineWordPair_EmptyInput_ShouldReturnEmptyResult()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var emptyWordPairs = Array.Empty<WordPair>();

            // Act
            var result = splitter.CombineWordPairsToDword(emptyWordPairs);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region Phase 2.3 Red: 大量データパフォーマンステスト

        /// <summary>
        /// RED: 1000個WordPairの高速結合テスト - 現在の実装では遅すぎる
        /// </summary>
        [Fact]
        public void CombineWordPair_PerformanceTest_1000Pairs_ShouldCompleteInReasonableTime()
        {
            // Arrange: 1000個のWordPairを生成
            var splitter = new PseudoDwordSplitter();
            var wordPairs = Enumerable.Range(0, 1000)
                .Select(i => new WordPair
                {
                    LowWord = (DeviceCode.D, (uint)i * 2, (ushort)(i & 0xFFFF)),
                    HighWord = (DeviceCode.D, (uint)i * 2 + 1, (ushort)((i >> 16) & 0xFFFF))
                })
                .ToArray();

            // Act & Assert: 実行時間測定
            var stopwatch = Stopwatch.StartNew();
            var result = splitter.CombineWordPairsToDword(wordPairs);
            stopwatch.Stop();

            // パフォーマンス要求: 1000個を5ms以内で処理（現在の実装では不可能）
            Assert.True(stopwatch.ElapsedMilliseconds < 5,
                $"Performance requirement failed: {stopwatch.ElapsedMilliseconds}ms > 5ms");

            // 결과検証
            Assert.Equal(1000, result.Count);
            
            // 各結合結果の正確性検証（サンプリング）
            for (int i = 0; i < 10; i++)
            {
                var expectedValue = (uint)i; // 簡単な期待値
                Assert.Equal((uint)i * 2, result[i].address);
                Assert.Equal(DeviceCode.D, result[i].deviceCode);
            }
        }

        /// <summary>
        /// RED: メモリ使用量テスト - 大量結合でのメモリ効率
        /// </summary>
        [Fact]
        public void CombineWordPair_MemoryUsage_ShouldBeOptimal()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();

            // 初期メモリ状態
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var initialMemory = GC.GetTotalMemory(true);

            // 大量WordPair生成・結合
            var wordPairs = Enumerable.Range(0, 2000)
                .Select(i => new WordPair
                {
                    LowWord = (DeviceCode.D, (uint)i * 2, (ushort)(i & 0xFFFF)),
                    HighWord = (DeviceCode.D, (uint)i * 2 + 1, (ushort)((i >> 16) & 0xFFFF))
                })
                .ToArray();

            var result = splitter.CombineWordPairsToDword(wordPairs);

            // 결합後メモリ状態
            var finalMemory = GC.GetTotalMemory(false);
            var memoryUsed = finalMemory - initialMemory;

            // メモリ効率要求: 2000個の結合で200KB以下（現在の実装では不可能）
            Assert.True(memoryUsed < 200 * 1024,
                $"Memory usage too high: {memoryUsed / 1024.0:F2}KB > 200KB");

            Assert.Equal(2000, result.Count);
        }

        #endregion

        #region Phase 2.3 Red: 複数デバイス種別対応テスト

        /// <summary>
        /// RED: 異なるデバイス種別の混在結合テスト - より多様なデバイス対応
        /// </summary>
        [Fact]
        public void CombineWordPair_MultipleDeviceTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var wordPairs = new[]
            {
                new WordPair // データレジスタ
                {
                    LowWord = (DeviceCode.D, 100u, (ushort)0x5678),
                    HighWord = (DeviceCode.D, 101u, (ushort)0x1234)
                },
                new WordPair // 拡張データレジスタ
                {
                    LowWord = (DeviceCode.RD, 1000u, (ushort)0xEF00),
                    HighWord = (DeviceCode.RD, 1001u, (ushort)0xABCD)
                },
                new WordPair // リンクレジスタ
                {
                    LowWord = (DeviceCode.W, 500u, (ushort)0x4321),
                    HighWord = (DeviceCode.W, 501u, (ushort)0x8765)
                }
            };

            // Act
            var result = splitter.CombineWordPairsToDword(wordPairs);

            // Assert: 各デバイス種別が正しく처리される
            Assert.Equal(3, result.Count);

            // データレジスタ検証
            Assert.Equal((DeviceCode.D, 100u, 0x12345678u), result[0]);

            // 拡張データレジスタ検証
            Assert.Equal((DeviceCode.RD, 1000u, 0xABCDEF00u), result[1]);

            // リンクレジスタ検証
            Assert.Equal((DeviceCode.W, 500u, 0x87654321u), result[2]);
        }

        /// <summary>
        /// RED: 16進アドレスデバイスの特殊結合処리テスト
        /// </summary>
        [Fact]
        public void CombineWordPair_HexAddressDevices_ShouldHandleSpecialConstraints()
        {
            // Arrange: 16진アドレス表現デバイス
            var splitter = new PseudoDwordSplitter();
            var hexWordPairs = new[]
            {
                new WordPair // 入力リレー（16진）
                {
                    LowWord = (DeviceCode.X, 0xFFu, (ushort)0x5678),
                    HighWord = (DeviceCode.X, 0x100u, (ushort)0x1234)
                },
                new WordPair // 출력リレー（16진）
                {
                    LowWord = (DeviceCode.Y, 0x1FFu, (ushort)0xEF00),
                    HighWord = (DeviceCode.Y, 0x200u, (ushort)0xABCD)
                }
            };

            // Act
            var result = splitter.CombineWordPairsToDword(hexWordPairs);

            // Assert: 16진アドレスデバイスも正しく処리
            Assert.Equal(2, result.Count);

            // 各결과에서 올바른 결합 확인
            Assert.Equal((DeviceCode.X, 0xFFu, 0x12345678u), result[0]);
            Assert.Equal((DeviceCode.Y, 0x1FFu, 0xABCDEF00u), result[1]);
        }

        #endregion

        #region Phase 2.3 Red: 新機能要求テスト（현재未実装）

        /// <summary>
        /// RED: 結合統計情報取得機能テスト - 現在未実装の機能
        /// </summary>
        [Fact]
        public void CombineWordPair_Statistics_ShouldProvideDetailedCombinationStatistics()
        {
            // Arrange: 통계機能を有効にして実행
            var options = new ConversionOptions { EnableStatistics = true };
            var splitter = new PseudoDwordSplitter(null, null, options);
            var wordPairs = new[]
            {
                new WordPair
                {
                    LowWord = (DeviceCode.D, 100u, (ushort)0x5678),
                    HighWord = (DeviceCode.D, 101u, (ushort)0x1234)
                },
                new WordPair
                {
                    LowWord = (DeviceCode.RD, 200u, (ushort)0xEF00),
                    HighWord = (DeviceCode.RD, 201u, (ushort)0xABCD)
                }
            };

            // Act
            var result = splitter.CombineWordPairsToDword(wordPairs);

            // Assert: 결합統計情報が取得可能（現在未実装なので失敗）
            var stats = Assert.IsAssignableFrom<ICombinationStatistics>(splitter);
            Assert.Equal(2, stats.TotalCombinations);
            Assert.Equal(2, stats.TotalDwordsGenerated);
            Assert.True(stats.AverageCombinationTime > TimeSpan.Zero);
        }

        /// <summary>
        /// RED: スレッドセーフティテスト - 現在の실装은 스레드セーフではない
        /// </summary>
        [Fact]
        public void CombineWordPair_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var wordPairs = Enumerable.Range(0, 50)
                .Select(i => new WordPair
                {
                    LowWord = (DeviceCode.D, (uint)i * 2, (ushort)(i & 0xFFFF)),
                    HighWord = (DeviceCode.D, (uint)i * 2 + 1, (ushort)((i >> 16) & 0xFFFF))
                })
                .ToArray();

            var results = new IList<(DeviceCode, uint, uint)>[5];
            var tasks = new System.Threading.Tasks.Task[5];

            // Act: 複数スレッドで동시実行
            for (int i = 0; i < 5; i++)
            {
                var index = i;
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    results[index] = splitter.CombineWordPairsToDword(wordPairs);
                });
            }

            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert: 모든結果가一致する（スレッドセーフであることを証明）
            for (int i = 1; i < 5; i++)
            {
                Assert.Equal(results[0].Count, results[i].Count);
                for (int j = 0; j < results[0].Count; j++)
                {
                    Assert.Equal(results[0][j], results[i][j]);
                }
            }
        }

        /// <summary>
        /// RED: 결합データ整合性詳細検証テスト - 더 엄격한検証機能要求
        /// </summary>
        [Fact]
        public void CombineWordPair_DataIntegrityValidation_ShouldProvideDetailedErrorInfo()
        {
            // Arrange
            var splitter = new PseudoDwordSplitter();
            var invalidWordPairs = new[]
            {
                new WordPair // 아드레스 오류
                {
                    LowWord = (DeviceCode.D, 100u, (ushort)0x1234),
                    HighWord = (DeviceCode.D, 105u, (ushort)0x5678) // 잘못된 주소: 101이어야 함
                },
                new WordPair // 디바이스 코드 불일치
                {
                    LowWord = (DeviceCode.W, 200u, (ushort)0xABCD),
                    HighWord = (DeviceCode.D, 201u, (ushort)0xEF00) // 디바이스 코드 불일치
                }
            };

            // Act & Assert: 상세 오류 정보를 취득 가능（현재 미구현）
            var exception = Assert.Throws<DetailedValidationException>(() =>
                splitter.CombineWordPairsToDword(invalidWordPairs));

            Assert.Equal(2, exception.ValidationErrors.Count);
            Assert.Contains("address mismatch", exception.ValidationErrors[0].Message.ToLower());
            Assert.Contains("device code mismatch", exception.ValidationErrors[1].Message.ToLower());
        }

        #endregion
    }

}