using System;
using System.Collections.Generic;
using Xunit;
using Moq;
using SlmpClient.Constants;
using SlmpClient.Core;

namespace SlmpClient.Tests
{
    /// <summary>
    /// テストファースト開発 - 擬似ダブルワード分割機能テスト
    /// Red-Green-Refactor サイクル Phase 1: 基盤テストとインターフェース設計
    /// </summary>
    public class PseudoDwordSplitterTests
    {
        #region Phase 1.1 Red: DWordデバイス分割テストの失敗テスト作成
        
        /// <summary>
        /// REFACTOR: 単一DWordデバイスの分割テスト - 依存性注入対応設計
        /// </summary>
        [Fact]
        public void SplitDwordDevice_SingleDevice_ShouldReturnWordPairs()
        {
            // Arrange: 依存性注入パターンでテスト可能な設計
            var splitter = new PseudoDwordSplitter();
            var dwordDevice = (DeviceCode.D, 100u, 0xDEADBEEFu);
            
            // Act
            var result = splitter.SplitDwordToWordPairs(new[] { dwordDevice });
            
            // Assert: リトルエンディアン分割の確認
            // D100(0xDEADBEEF) → D100(0xBEEF), D101(0xDEAD)
            Assert.Single(result);
            Assert.Equal((DeviceCode.D, 100u, (ushort)0xBEEF), result[0].LowWord);
            Assert.Equal((DeviceCode.D, 101u, (ushort)0xDEAD), result[0].HighWord);
        }

        /// <summary>
        /// REFACTOR: 複数DWordデバイスの分割テスト - 純粋関数パターン確認
        /// </summary>
        [Fact]
        public void SplitDwordDevice_MultipleDevices_ShouldReturnCorrectWordPairs()
        {
            // Arrange: 複数デバイスの分割テスト
            var splitter = new PseudoDwordSplitter();
            var dwordDevices = new[]
            {
                (DeviceCode.D, 100u, 0x12345678u),
                (DeviceCode.D, 200u, 0xABCDEF00u)
            };
            
            // Act
            var result = splitter.SplitDwordToWordPairs(dwordDevices);
            
            // Assert: 各デバイスが正しく分割されることを確認
            Assert.Equal(2, result.Count);
            
            // 1つ目: D100(0x12345678) → D100(0x5678), D101(0x1234)
            Assert.Equal((DeviceCode.D, 100u, (ushort)0x5678), result[0].LowWord);
            Assert.Equal((DeviceCode.D, 101u, (ushort)0x1234), result[0].HighWord);
            
            // 2つ目: D200(0xABCDEF00) → D200(0xEF00), D201(0xABCD)
            Assert.Equal((DeviceCode.D, 200u, (ushort)0xEF00), result[1].LowWord);
            Assert.Equal((DeviceCode.D, 201u, (ushort)0xABCD), result[1].HighWord);
        }

        /// <summary>
        /// REFACTOR: アドレス境界値チェックテスト - Phase 3: 例外スロー対応
        /// </summary>
        [Fact]
        public void SplitDwordDevice_AddressBoundary_ShouldThrowException()
        {
            // Arrange: Phase 3で例外スローモードを明示的に指定
            var continuitySettings = new ContinuitySettings { Mode = ErrorHandlingMode.ThrowException };
            var splitter = new PseudoDwordSplitter(null, null, null, continuitySettings, null);
            var boundaryDevice = (DeviceCode.D, 65535u, 0x12345678u); // D65535 → D65536は存在しない
            
            // Act & Assert: Phase 3の詳細検証例外がスローされることを確認
            var exception = Assert.Throws<DetailedValidationException>(() =>
                splitter.SplitDwordToWordPairs(new[] { boundaryDevice }));
                
            Assert.Contains("validation errors occurred", exception.Message.ToLower());
        }

        /// <summary>
        /// REFACTOR: 4バイトアドレス必須デバイスのテスト - 特殊デバイス対応確認
        /// </summary>
        [Fact]
        public void SplitDwordDevice_4ByteAddressDevice_ShouldHandleCorrectly()
        {
            // Arrange: 拡張データレジスタ(RD)のテスト
            var splitter = new PseudoDwordSplitter();
            var rdDevice = (DeviceCode.RD, 1000u, 0xFEDCBA98u);
            
            // Act
            var result = splitter.SplitDwordToWordPairs(new[] { rdDevice });
            
            // Assert: 4バイトアドレスデバイスも正しく分割される
            Assert.Single(result);
            Assert.Equal((DeviceCode.RD, 1000u, (ushort)0xBA98), result[0].LowWord);
            Assert.Equal((DeviceCode.RD, 1001u, (ushort)0xFEDC), result[0].HighWord);
        }

        #endregion

        #region Phase 1.2 Word結合テスト（Refactor完了版）
        
        /// <summary>
        /// REFACTOR: WordペアをDWordに結合するテスト - 純粋関数パターン
        /// </summary>
        [Fact]
        public void CombineWordPairsToDword_BasicCombination_ShouldReturnCorrectDwords()
        {
            // Arrange: Word結合テスト
            var splitter = new PseudoDwordSplitter();
            var wordPairs = new[]
            {
                new WordPair
                {
                    LowWord = (DeviceCode.D, 100u, (ushort)0x5678),
                    HighWord = (DeviceCode.D, 101u, (ushort)0x1234)
                }
            };
            
            // Act
            var result = splitter.CombineWordPairsToDword(wordPairs);
            
            // Assert: リトルエンディアン結合の確認
            Assert.Single(result);
            Assert.Equal((DeviceCode.D, 100u, 0x12345678u), result[0]);
        }

        /// <summary>
        /// REFACTOR: 依存性注入パターンのテスト - テスト可能な設計確認
        /// </summary>
        [Fact]
        public void DependencyInjection_MockValidator_ShouldWorkCorrectly()
        {
            // Arrange: モックを使った依存性注入テスト
            var mockValidator = new Mock<IDeviceAddressValidator>();
            var mockConverter = new Mock<IDwordConverter>();
            
            var splitter = new PseudoDwordSplitter(mockValidator.Object, mockConverter.Object);
            var testDevice = (DeviceCode.D, 100u, 0x12345678u);
            
            var expectedWordPair = new WordPair
            {
                LowWord = (DeviceCode.D, 100u, (ushort)0x5678),
                HighWord = (DeviceCode.D, 101u, (ushort)0x1234)
            };
            
            // モックの設定
            mockConverter.Setup(c => c.SplitDwordToWordPair(testDevice))
                         .Returns(expectedWordPair);
            
            // Act
            var result = splitter.SplitDwordToWordPairs(new[] { testDevice });
            
            // Assert: 依存性注入が正しく動作することを確認
            Assert.Single(result);
            Assert.Equal(expectedWordPair, result[0]);
            
            // モックが呼ばれたことを確認
            mockValidator.Verify(v => v.ValidateAddressBoundary(DeviceCode.D, 100u), Times.Once);
            mockConverter.Verify(c => c.SplitDwordToWordPair(testDevice), Times.Once);
        }

        #endregion

        #region Phase 3: エラーハンドリング統合テスト

        /// <summary>
        /// Phase 3: 継続動作モード - デフォルト値返却テスト
        /// </summary>
        [Fact]
        public void SplitDwordDevice_ContinuityMode_ShouldReturnDefaultValues()
        {
            // Arrange: Phase 3で継続動作モードを指定
            var continuitySettings = new ContinuitySettings { Mode = ErrorHandlingMode.ReturnDefaultAndContinue };
            var errorStatistics = new SlmpErrorStatistics();
            var splitter = new PseudoDwordSplitter(null, null, null, continuitySettings, errorStatistics);
            var boundaryDevice = (DeviceCode.D, 65536u, 0x12345678u); // D65536は確実に境界外

            // Act: エラーが発生する操作を実行
            var result = splitter.SplitDwordToWordPairs(new[] { boundaryDevice });

            // Assert: デフォルト値が返却されることを確認
            Assert.Single(result);
            Assert.Equal((DeviceCode.D, 65536u, (ushort)0), result[0].LowWord);
            Assert.Equal((DeviceCode.D, 65537u, (ushort)0), result[0].HighWord);

            // エラー統計が記録されていることを確認
            var summary = errorStatistics.GetSummary();
            Assert.True(summary.TotalErrors > 0);
            Assert.True(summary.TotalContinuedOperations > 0);
        }

        /// <summary>
        /// Phase 3: リトライ後デフォルト返却モードテスト
        /// </summary>
        [Fact]
        public void SplitDwordDevice_RetryThenDefaultMode_ShouldReturnDefaultValues()
        {
            // Arrange: Phase 3でリトライ後デフォルトモードを指定
            var continuitySettings = new ContinuitySettings { Mode = ErrorHandlingMode.RetryThenDefault };
            var errorStatistics = new SlmpErrorStatistics();
            var splitter = new PseudoDwordSplitter(null, null, null, continuitySettings, errorStatistics);
            var boundaryDevice = (DeviceCode.D, 65536u, 0x12345678u);

            // Act: エラーが発生する操作を実行
            var result = splitter.SplitDwordToWordPairs(new[] { boundaryDevice });

            // Assert: デフォルト値が返却されることを確認
            Assert.Single(result);
            Assert.Equal((DeviceCode.D, 65536u, (ushort)0), result[0].LowWord);
            Assert.Equal((DeviceCode.D, 65537u, (ushort)0), result[0].HighWord);

            // エラー統計が記録されていることを確認
            var summary = errorStatistics.GetSummary();
            Assert.True(summary.TotalErrors > 0);
            Assert.True(summary.TotalContinuedOperations > 0);
        }

        /// <summary>
        /// Phase 3: 結合操作でのエラーハンドリングテスト
        /// </summary>
        [Fact]
        public void CombineWordPairs_ContinuityMode_ShouldHandleErrors()
        {
            // Arrange: Phase 3で継続動作モードを指定
            var continuitySettings = new ContinuitySettings { Mode = ErrorHandlingMode.ReturnDefaultAndContinue };
            var errorStatistics = new SlmpErrorStatistics();
            var splitter = new PseudoDwordSplitter(null, null, null, continuitySettings, errorStatistics);
            
            // 不正なWordPair（アドレス不整合）
            var invalidWordPairs = new[]
            {
                new WordPair
                {
                    LowWord = (DeviceCode.D, 100u, (ushort)0x1234),
                    HighWord = (DeviceCode.D, 105u, (ushort)0x5678) // アドレス不整合: 101ではなく105
                }
            };

            // Act: エラーが発生する結合操作を実行
            var result = splitter.CombineWordPairsToDword(invalidWordPairs);

            // Assert: エラーが処理されることを確認
            Assert.NotNull(result);
        }

        #endregion
    }
}