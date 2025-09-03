using System;
using SlmpClient.Utils;
using Xunit;

namespace SlmpClient.Tests.Utils
{
    /// <summary>
    /// Unit tests for SlmpBitConverter utility class
    /// Tests bit packing/unpacking functionality based on Python util.py patterns
    /// </summary>
    public class SlmpBitConverterTests
    {
        [Fact]
        public void UnpackBits_ValidData_ReturnsCorrectBits()
        {
            // Arrange - Test pattern from Python tests: 0x88 = 10001000 binary
            var data = new byte[] { 0x88 };
            
            // Act
            var result = SlmpBitConverter.UnpackBits(data, 8);
            
            // Assert
            Assert.Equal(8, result.Length);
            Assert.False(result[0]); // bit 0
            Assert.False(result[1]); // bit 1  
            Assert.False(result[2]); // bit 2
            Assert.True(result[3]);  // bit 3
            Assert.False(result[4]); // bit 4
            Assert.False(result[5]); // bit 5
            Assert.False(result[6]); // bit 6
            Assert.True(result[7]);  // bit 7
        }

        [Fact]
        public void UnpackBits_MultipleBytes_ReturnsCorrectBits()
        {
            // Arrange - Two bytes: 0x01 (00000001), 0x80 (10000000)
            var data = new byte[] { 0x01, 0x80 };
            
            // Act
            var result = SlmpBitConverter.UnpackBits(data, 16);
            
            // Assert
            Assert.Equal(16, result.Length);
            // First byte (0x01): bit 0 = true, others false
            Assert.True(result[0]);
            for (int i = 1; i < 8; i++)
            {
                Assert.False(result[i]);
            }
            // Second byte (0x80): bit 7 = true, others false
            for (int i = 8; i < 15; i++)
            {
                Assert.False(result[i]);
            }
            Assert.True(result[15]);
        }

        [Fact]
        public void UnpackBits_CountZero_ReturnsAllBits()
        {
            // Arrange
            var data = new byte[] { 0xFF };
            
            // Act
            var result = SlmpBitConverter.UnpackBits(data, 0);
            
            // Assert
            Assert.Equal(8, result.Length);
            for (int i = 0; i < 8; i++)
            {
                Assert.True(result[i]);
            }
        }

        [Fact]
        public void UnpackBits_CountLessThanByteSize_ReturnsRequestedBits()
        {
            // Arrange
            var data = new byte[] { 0xFF, 0xFF };
            
            // Act
            var result = SlmpBitConverter.UnpackBits(data, 10);
            
            // Assert
            Assert.Equal(10, result.Length);
            for (int i = 0; i < 10; i++)
            {
                Assert.True(result[i]);
            }
        }

        [Fact]
        public void PackBits_ValidBits_ReturnsCorrectBytes()
        {
            // Arrange - Pattern that should create 0x88 (10001000)
            var bits = new bool[] { false, false, false, true, false, false, false, true };
            
            // Act
            var result = SlmpBitConverter.PackBits(bits);
            
            // Assert
            Assert.Single(result);
            Assert.Equal(0x88, result[0]);
        }

        [Fact]
        public void PackBits_MultipleBytesPattern_ReturnsCorrectBytes()
        {
            // Arrange - Pattern: first byte = 0x01, second byte = 0x80
            var bits = new bool[16];
            bits[0] = true;  // First bit of first byte
            bits[15] = true; // Last bit of second byte
            
            // Act
            var result = SlmpBitConverter.PackBits(bits);
            
            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(0x01, result[0]);
            Assert.Equal(0x80, result[1]);
        }

        [Fact]
        public void PackBits_NotMultipleOfEight_PadsWithZeros()
        {
            // Arrange - 10 bits, should be padded to 16 bits (2 bytes)
            var bits = new bool[10];
            bits[0] = true;
            bits[9] = true;
            
            // Act
            var result = SlmpBitConverter.PackBits(bits);
            
            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(0x01, result[0]); // First byte: bit 0 set
            Assert.Equal(0x02, result[1]); // Second byte: bit 1 set (9th bit overall)
        }

        [Fact]
        public void PackBits_EmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var bits = Array.Empty<bool>();
            
            // Act
            var result = SlmpBitConverter.PackBits(bits);
            
            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void UnpackBits_EmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var data = Array.Empty<byte>();
            
            // Act
            var result = SlmpBitConverter.UnpackBits(data, 0);
            
            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void UnpackBits_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SlmpBitConverter.UnpackBits(null!, 8));
        }

        [Fact]
        public void PackBits_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SlmpBitConverter.PackBits(null!));
        }

        [Fact]
        public void UnpackBits_NegativeCount_ThrowsArgumentException()
        {
            // Arrange
            var data = new byte[] { 0xFF };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SlmpBitConverter.UnpackBits(data, -1));
        }

        [Fact]
        public void UnpackBits_CountExceedsData_ThrowsArgumentException()
        {
            // Arrange
            var data = new byte[] { 0xFF };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SlmpBitConverter.UnpackBits(data, 9));
        }

        [Fact]
        public void IntArrayToBits_ValidData_ReturnsCorrectBits()
        {
            // Arrange
            var intArray = new int[] { 0, 1, 0, 1, 2, -1 };
            
            // Act
            var result = SlmpBitConverter.IntArrayToBits(intArray);
            
            // Assert
            Assert.Equal(6, result.Length);
            Assert.False(result[0]); // 0 -> false
            Assert.True(result[1]);  // 1 -> true
            Assert.False(result[2]); // 0 -> false
            Assert.True(result[3]);  // 1 -> true
            Assert.True(result[4]);  // 2 -> true (non-zero)
            Assert.True(result[5]);  // -1 -> true (non-zero)
        }

        [Fact]
        public void BitsToIntArray_ValidData_ReturnsCorrectInts()
        {
            // Arrange
            var bits = new bool[] { false, true, false, true };
            
            // Act
            var result = SlmpBitConverter.BitsToIntArray(bits);
            
            // Assert
            Assert.Equal(4, result.Length);
            Assert.Equal(0, result[0]);
            Assert.Equal(1, result[1]);
            Assert.Equal(0, result[2]);
            Assert.Equal(1, result[3]);
        }

        [Fact]
        public void IntArrayToBits_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SlmpBitConverter.IntArrayToBits(null!));
        }

        [Fact]
        public void BitsToIntArray_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SlmpBitConverter.BitsToIntArray(null!));
        }

        [Theory]
        [InlineData(new byte[] { 0x00 }, 8, new bool[] { false, false, false, false, false, false, false, false })]
        [InlineData(new byte[] { 0xFF }, 8, new bool[] { true, true, true, true, true, true, true, true })]
        [InlineData(new byte[] { 0x0F }, 8, new bool[] { true, true, true, true, false, false, false, false })]
        [InlineData(new byte[] { 0xF0 }, 8, new bool[] { false, false, false, false, true, true, true, true })]
        public void UnpackBits_VariousPatterns_ReturnsExpectedResults(byte[] input, int count, bool[] expected)
        {
            // Act
            var result = SlmpBitConverter.UnpackBits(input, count);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(new bool[] { false, false, false, false, false, false, false, false }, new byte[] { 0x00 })]
        [InlineData(new bool[] { true, true, true, true, true, true, true, true }, new byte[] { 0xFF })]
        [InlineData(new bool[] { true, true, true, true, false, false, false, false }, new byte[] { 0x0F })]
        [InlineData(new bool[] { false, false, false, false, true, true, true, true }, new byte[] { 0xF0 })]
        public void PackBits_VariousPatterns_ReturnsExpectedResults(bool[] input, byte[] expected)
        {
            // Act
            var result = SlmpBitConverter.PackBits(input);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void PackUnpackRoundTrip_PreservesData()
        {
            // Arrange
            var originalBits = new bool[] { true, false, true, false, true, true, false, false, true };
            
            // Act
            var packed = SlmpBitConverter.PackBits(originalBits);
            var unpacked = SlmpBitConverter.UnpackBits(packed, originalBits.Length);
            
            // Assert
            Assert.Equal(originalBits, unpacked);
        }
    }
}