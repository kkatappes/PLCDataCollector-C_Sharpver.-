using Xunit;
using Andon.Utilities;
using System;

namespace Andon.Tests.Unit.Utilities
{
    /// <summary>
    /// BitExpansionUtilityクラスの単体テスト
    /// Phase2: ビット展開機能のテストケース実装
    /// TDD手法: Red-Green-Refactorサイクル
    /// </summary>
    public class BitExpansionUtilityTests
    {
        #region TC-BIT-001: ExpandWordToBits - 基本ビット展開テスト

        [Fact]
        public void ExpandWordToBits_AllZeros_ReturnsAllFalseBits()
        {
            // Arrange: 全ビット0のワード値
            ushort wordValue = 0x0000;

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: 16個のfalseビット配列を期待
            Assert.Equal(16, result.Length);
            Assert.All(result, bit => Assert.False(bit));
        }

        [Fact]
        public void ExpandWordToBits_AllOnes_ReturnsAllTrueBits()
        {
            // Arrange: 全ビット1のワード値
            ushort wordValue = 0xFFFF;

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: 16個のtrueビット配列を期待
            Assert.Equal(16, result.Length);
            Assert.All(result, bit => Assert.True(bit));
        }

        [Fact]
        public void ExpandWordToBits_OnlyBit0Set_ReturnsLsbFirstOrder()
        {
            // Arrange: bit0のみ1 (0x0001)
            ushort wordValue = 0x0001;

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: LSB first順序 - 配列の先頭がbit0
            Assert.Equal(16, result.Length);
            Assert.True(result[0]);  // bit0 = 1
            for (int i = 1; i < 16; i++)
            {
                Assert.False(result[i]); // bit1-15 = 0
            }
        }

        [Fact]
        public void ExpandWordToBits_OnlyBit15Set_ReturnsMsbAtEnd()
        {
            // Arrange: bit15のみ1 (0x8000)
            ushort wordValue = 0x8000;

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: LSB first順序 - 配列の最後がbit15
            Assert.Equal(16, result.Length);
            Assert.True(result[15]); // bit15 = 1
            for (int i = 0; i < 15; i++)
            {
                Assert.False(result[i]); // bit0-14 = 0
            }
        }

        [Fact]
        public void ExpandWordToBits_MultipleMiddleBitsSet_ReturnsCorrectPattern()
        {
            // Arrange: 0x0003 = 0b0000_0000_0000_0011 (bit0, bit1が1)
            ushort wordValue = 0x0003;

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: bit0とbit1が1、他は0
            Assert.Equal(16, result.Length);
            Assert.True(result[0]);  // bit0 = 1
            Assert.True(result[1]);  // bit1 = 1
            for (int i = 2; i < 16; i++)
            {
                Assert.False(result[i]); // bit2-15 = 0
            }
        }

        [Fact]
        public void ExpandWordToBits_AlternatingPattern_ReturnsCorrectBitSequence()
        {
            // Arrange: 0x00AA = 0b0000_0000_1010_1010 (偶数ビットが1)
            ushort wordValue = 0x00AA;

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: 偶数ビットが1、奇数ビットが0
            Assert.Equal(16, result.Length);
            bool[] expected = { false, true, false, true, false, true, false, true,
                                false, false, false, false, false, false, false, false };
            Assert.Equal(expected, result);
        }

        #endregion

        #region TC-BIT-002: ExpandWordToBits(int) - int版オーバーロードテスト

        [Fact]
        public void ExpandWordToBits_IntOverload_ExtractsLower16Bits()
        {
            // Arrange: 32ビットint値の下位16ビットのみ使用
            int wordValue = 0x12340003; // 下位16ビット = 0x0003

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: 下位16ビットのみ展開される
            Assert.Equal(16, result.Length);
            Assert.True(result[0]);  // bit0 = 1
            Assert.True(result[1]);  // bit1 = 1
            for (int i = 2; i < 16; i++)
            {
                Assert.False(result[i]); // bit2-15 = 0
            }
        }

        [Fact]
        public void ExpandWordToBits_IntNegativeValue_CorrectlyHandlesSigned()
        {
            // Arrange: 負のint値（符号付き）
            int wordValue = -1; // 0xFFFFFFFF（下位16ビット = 0xFFFF）

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: 全ビット1
            Assert.Equal(16, result.Length);
            Assert.All(result, bit => Assert.True(bit));
        }

        #endregion

        #region TC-BIT-003: ExpandMultipleWordsToBits - 複数ワード一括展開テスト

        [Fact]
        public void ExpandMultipleWordsToBits_EmptyArray_ReturnsEmptyBitArray()
        {
            // Arrange: 空配列
            ushort[] wordValues = Array.Empty<ushort>();

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandMultipleWordsToBits(wordValues);

            // Assert: 空のビット配列
            Assert.Empty(result);
        }

        [Fact]
        public void ExpandMultipleWordsToBits_SingleWord_Returns16Bits()
        {
            // Arrange: 単一ワード
            ushort[] wordValues = { 0x0003 };

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandMultipleWordsToBits(wordValues);

            // Assert: 16ビット配列
            Assert.Equal(16, result.Length);
            Assert.True(result[0]);
            Assert.True(result[1]);
        }

        [Fact]
        public void ExpandMultipleWordsToBits_MultipleWords_ReturnsConcatenatedBits()
        {
            // Arrange: 複数ワード
            ushort[] wordValues = { 0x0001, 0x0002 };

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandMultipleWordsToBits(wordValues);

            // Assert: 32ビット配列（16ビット × 2ワード）
            Assert.Equal(32, result.Length);

            // 1つ目のワード（0x0001）: bit0のみ1
            Assert.True(result[0]);
            for (int i = 1; i < 16; i++)
            {
                Assert.False(result[i]);
            }

            // 2つ目のワード（0x0002）: bit1のみ1
            Assert.False(result[16]);
            Assert.True(result[17]);
            for (int i = 18; i < 32; i++)
            {
                Assert.False(result[i]);
            }
        }

        [Fact]
        public void ExpandMultipleWordsToBits_ThreeWords_Returns48Bits()
        {
            // Arrange: 3ワード
            ushort[] wordValues = { 0x0001, 0x0002, 0x0004 };

            // Act: ビット展開実行
            bool[] result = BitExpansionUtility.ExpandMultipleWordsToBits(wordValues);

            // Assert: 48ビット配列（16ビット × 3ワード）
            Assert.Equal(48, result.Length);
        }

        #endregion

        #region ConMoni互換性確認テスト

        [Fact]
        public void ExpandWordToBits_ConMoniEquivalentTest_MatchesPythonBehavior()
        {
            // Arrange: ConMoniと同じ入力値
            // Python: binary = format(3, '016b')  # "0000000000000011"
            //         binary[::-1]                 # "1100000000000000" (LSB first)
            ushort wordValue = 0x0003;

            // Act
            bool[] result = BitExpansionUtility.ExpandWordToBits(wordValue);

            // Assert: ConMoniのPython実装と同じ結果
            // [1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
            Assert.True(result[0]);
            Assert.True(result[1]);
            for (int i = 2; i < 16; i++)
            {
                Assert.False(result[i]);
            }
        }

        #endregion

        #region TC-BIT-004: ExpandWithSelectionMask - 選択的ビット展開テスト

        [Fact]
        public void ExpandWithSelectionMask_EmptyArrays_ReturnsEmptyList()
        {
            // Arrange: 空配列
            ushort[] wordValues = Array.Empty<ushort>();
            bool[] bitExpansionMask = Array.Empty<bool>();

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(wordValues, bitExpansionMask);

            // Assert: 空リスト
            Assert.Empty(result);
        }

        [Fact]
        public void ExpandWithSelectionMask_AllWordMode_ReturnsAllWordValues()
        {
            // Arrange: 全てワードモード（ビット展開しない）
            ushort[] wordValues = { 3, 255, 1 };
            bool[] bitExpansionMask = { false, false, false };

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(wordValues, bitExpansionMask);

            // Assert: 3つのdouble値
            Assert.Equal(3, result.Count);
            Assert.Equal(3.0, (double)result[0]);
            Assert.Equal(255.0, (double)result[1]);
            Assert.Equal(1.0, (double)result[2]);
        }

        [Fact]
        public void ExpandWithSelectionMask_AllBitMode_ReturnsAllExpandedBits()
        {
            // Arrange: 全てビットモード
            ushort[] wordValues = { 0x0003 };
            bool[] bitExpansionMask = { true };

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(wordValues, bitExpansionMask);

            // Assert: 16ビット
            Assert.Equal(16, result.Count);
            Assert.True((bool)result[0]);
            Assert.True((bool)result[1]);
            for (int i = 2; i < 16; i++)
            {
                Assert.False((bool)result[i]);
            }
        }

        [Fact]
        public void ExpandWithSelectionMask_MixedMode_ReturnsMixedData()
        {
            // Arrange: 混合モード
            // words[0]=3, mask=false → ワード値3.0
            // words[1]=255, mask=true → 16ビット展開
            // words[2]=1, mask=false → ワード値1.0
            ushort[] wordValues = { 0x0003, 0x00FF, 0x0001 };
            bool[] bitExpansionMask = { false, true, false };

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(wordValues, bitExpansionMask);

            // Assert: 1(ワード) + 16(ビット) + 1(ワード) = 18要素
            Assert.Equal(18, result.Count);

            // 1つ目: ワード値
            Assert.IsType<double>(result[0]);
            Assert.Equal(3.0, (double)result[0]);

            // 2つ目: 16ビット展開 (0xFF = 全ビット1の下位8ビット)
            for (int i = 1; i < 9; i++)
            {
                Assert.IsType<bool>(result[i]);
                Assert.True((bool)result[i]); // bit0-7 = 1
            }
            for (int i = 9; i < 17; i++)
            {
                Assert.IsType<bool>(result[i]);
                Assert.False((bool)result[i]); // bit8-15 = 0
            }

            // 3つ目: ワード値
            Assert.IsType<double>(result[17]);
            Assert.Equal(1.0, (double)result[17]);
        }

        [Fact]
        public void ExpandWithSelectionMask_WithConversionFactors_AppliesFactorsCorrectly()
        {
            // Arrange: 変換係数適用
            ushort[] wordValues = { 10, 100, 5 };
            bool[] bitExpansionMask = { false, false, false };
            double[] conversionFactors = { 0.1, 10.0, 1.0 };

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(
                wordValues, bitExpansionMask, conversionFactors);

            // Assert: 変換係数が適用される
            Assert.Equal(3, result.Count);
            Assert.Equal(1.0, (double)result[0]);   // 10 * 0.1 = 1.0
            Assert.Equal(1000.0, (double)result[1]); // 100 * 10.0 = 1000.0
            Assert.Equal(5.0, (double)result[2]);    // 5 * 1.0 = 5.0
        }

        [Fact]
        public void ExpandWithSelectionMask_ConversionFactorWithBitExpansion_AppliesBeforeExpansion()
        {
            // Arrange: 変換係数適用後にビット展開
            // 重要: ConMoniでは変換係数適用→ビット展開の順序
            ushort[] wordValues = { 2 };
            bool[] bitExpansionMask = { true };
            double[] conversionFactors = { 10.0 };

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(
                wordValues, bitExpansionMask, conversionFactors);

            // Assert: 2 * 10 = 20 → 0x0014をビット展開
            // 20 = 0b00010100
            // LSB first: [0,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0]
            Assert.Equal(16, result.Count);
            Assert.False((bool)result[0]); // bit0 = 0
            Assert.False((bool)result[1]); // bit1 = 0
            Assert.True((bool)result[2]);  // bit2 = 1
            Assert.False((bool)result[3]); // bit3 = 0
            Assert.True((bool)result[4]);  // bit4 = 1
            for (int i = 5; i < 16; i++)
            {
                Assert.False((bool)result[i]);
            }
        }

        [Fact]
        public void ExpandWithSelectionMask_ArrayLengthMismatch_ThrowsArgumentException()
        {
            // Arrange: 配列長不一致
            ushort[] wordValues = { 1, 2, 3 };
            bool[] bitExpansionMask = { false, true }; // 長さ不一致

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                BitExpansionUtility.ExpandWithSelectionMask(wordValues, bitExpansionMask));

            Assert.Contains("Array length mismatch", ex.Message);
            Assert.Contains("wordValues=3", ex.Message);
            Assert.Contains("bitExpansionMask=2", ex.Message);
        }

        [Fact]
        public void ExpandWithSelectionMask_ConversionFactorLengthMismatch_ThrowsArgumentException()
        {
            // Arrange: 変換係数配列長不一致
            ushort[] wordValues = { 1, 2, 3 };
            bool[] bitExpansionMask = { false, false, false };
            double[] conversionFactors = { 0.1, 1.0 }; // 長さ不一致

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                BitExpansionUtility.ExpandWithSelectionMask(
                    wordValues, bitExpansionMask, conversionFactors));

            Assert.Contains("Array length mismatch", ex.Message);
            Assert.Contains("wordValues=3", ex.Message);
            Assert.Contains("conversionFactors=2", ex.Message);
        }

        [Fact]
        public void ExpandWithSelectionMask_ConMoniCompatibilityTest()
        {
            // Arrange: ConMoniと同じテストケース
            // ConMoni: words = [3, 255, 1, 2]
            //          mask = [False, True, False, True]
            //          factors = [1.0, 1.0, 0.1, 10.0]
            ushort[] wordValues = { 0x0003, 0x00FF, 0x0001, 0x0002 };
            bool[] bitExpansionMask = { false, true, false, true };
            double[] conversionFactors = { 1.0, 1.0, 0.1, 10.0 };

            // Act
            var result = BitExpansionUtility.ExpandWithSelectionMask(
                wordValues, bitExpansionMask, conversionFactors);

            // Assert: 期待結果
            // [0]: 3.0 (ワード)
            // [1-16]: 0xFF展開 (16ビット)
            // [17]: 0.1 (ワード, 1 * 0.1)
            // [18-33]: 20展開 (16ビット, 2 * 10 = 20)
            Assert.Equal(1 + 16 + 1 + 16, result.Count);

            // words[0] = 3 * 1.0 = 3.0
            Assert.Equal(3.0, (double)result[0]);

            // words[1] = 255 * 1.0 = 255をビット展開
            // 0xFF = 下位8ビット全て1
            for (int i = 1; i < 9; i++)
                Assert.True((bool)result[i]);
            for (int i = 9; i < 17; i++)
                Assert.False((bool)result[i]);

            // words[2] = 1 * 0.1 = 0.1
            Assert.Equal(0.1, (double)result[17]);

            // words[3] = 2 * 10.0 = 20をビット展開
            // 20 = 0b00010100
            Assert.False((bool)result[18]); // bit0
            Assert.False((bool)result[19]); // bit1
            Assert.True((bool)result[20]);  // bit2
            Assert.False((bool)result[21]); // bit3
            Assert.True((bool)result[22]);  // bit4
        }

        #endregion
    }
}
