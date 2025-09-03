using System;
using SlmpClient.Utils;
using Xunit;

namespace SlmpClient.Tests.Utils
{
    /// <summary>
    /// Unit tests for DataProcessor utility class
    /// Tests data conversion functionality for SLMP protocol
    /// </summary>
    public class DataProcessorTests
    {
        [Fact]
        public void BytesToUshortArray_ValidData_ReturnsCorrectValues()
        {
            // Arrange - Little-endian byte order: 0x1234, 0x0002
            var bytes = new byte[] { 0x34, 0x12, 0x02, 0x00 };
            
            // Act
            var result = DataProcessor.BytesToUshortArray(bytes);
            
            // Assert
            Assert.Equal(2, result.Length);
            Assert.Equal(0x1234, result[0]);
            Assert.Equal(0x0002, result[1]);
        }

        [Fact]
        public void UshortArrayToBytes_ValidData_ReturnsCorrectBytes()
        {
            // Arrange
            var values = new ushort[] { 0x1234, 0x0002 };
            
            // Act
            var result = DataProcessor.UshortArrayToBytes(values);
            
            // Assert
            Assert.Equal(4, result.Length);
            Assert.Equal(0x34, result[0]); // Low byte of 0x1234
            Assert.Equal(0x12, result[1]); // High byte of 0x1234
            Assert.Equal(0x02, result[2]); // Low byte of 0x0002
            Assert.Equal(0x00, result[3]); // High byte of 0x0002
        }

        [Fact]
        public void BytesToUshortArray_OddLength_ThrowsArgumentException()
        {
            // Arrange - 3 bytes (not divisible by 2)
            var bytes = new byte[] { 0x34, 0x12, 0x02 };
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => DataProcessor.BytesToUshortArray(bytes));
        }

        [Fact]
        public void BytesToUshortArray_EmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var bytes = Array.Empty<byte>();
            
            // Act
            var result = DataProcessor.BytesToUshortArray(bytes);
            
            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void UshortArrayToBytes_EmptyArray_ReturnsEmptyArray()
        {
            // Arrange
            var values = Array.Empty<ushort>();
            
            // Act
            var result = DataProcessor.UshortArrayToBytes(values);
            
            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void BytesToUshortArray_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DataProcessor.BytesToUshortArray(null!));
        }

        [Fact]
        public void UshortArrayToBytes_NullData_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => DataProcessor.UshortArrayToBytes(null!));
        }

        [Theory]
        [InlineData(new byte[] { 0xFF, 0xFF }, new ushort[] { 0xFFFF })]
        [InlineData(new byte[] { 0x00, 0x00 }, new ushort[] { 0x0000 })]
        [InlineData(new byte[] { 0xFF, 0x00 }, new ushort[] { 0x00FF })]
        [InlineData(new byte[] { 0x00, 0xFF }, new ushort[] { 0xFF00 })]
        public void BytesToUshortArray_EdgeCases_ReturnsCorrectValues(byte[] input, ushort[] expected)
        {
            // Act
            var result = DataProcessor.BytesToUshortArray(input);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(new ushort[] { 0xFFFF }, new byte[] { 0xFF, 0xFF })]
        [InlineData(new ushort[] { 0x0000 }, new byte[] { 0x00, 0x00 })]
        [InlineData(new ushort[] { 0x00FF }, new byte[] { 0xFF, 0x00 })]
        [InlineData(new ushort[] { 0xFF00 }, new byte[] { 0x00, 0xFF })]
        public void UshortArrayToBytes_EdgeCases_ReturnsCorrectBytes(ushort[] input, byte[] expected)
        {
            // Act
            var result = DataProcessor.UshortArrayToBytes(input);
            
            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void BytesToUshortArray_MultipleValues_ReturnsCorrectOrder()
        {
            // Arrange - Test pattern from Python tests: 0x1234, 0x0002, 0x1DEF
            var bytes = new byte[] { 0x34, 0x12, 0x02, 0x00, 0xEF, 0x1D };
            
            // Act
            var result = DataProcessor.BytesToUshortArray(bytes);
            
            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal(0x1234, result[0]);
            Assert.Equal(0x0002, result[1]);
            Assert.Equal(0x1DEF, result[2]);
        }

        [Fact]
        public void UshortArrayToBytes_MultipleValues_ReturnsCorrectOrder()
        {
            // Arrange
            var values = new ushort[] { 0x1234, 0x0002, 0x1DEF };
            
            // Act
            var result = DataProcessor.UshortArrayToBytes(values);
            
            // Assert
            Assert.Equal(6, result.Length);
            Assert.Equal(0x34, result[0]); // Low byte of 0x1234
            Assert.Equal(0x12, result[1]); // High byte of 0x1234
            Assert.Equal(0x02, result[2]); // Low byte of 0x0002
            Assert.Equal(0x00, result[3]); // High byte of 0x0002
            Assert.Equal(0xEF, result[4]); // Low byte of 0x1DEF
            Assert.Equal(0x1D, result[5]); // High byte of 0x1DEF
        }

        [Fact]
        public void ConversionRoundTrip_PreservesData()
        {
            // Arrange
            var originalValues = new ushort[] { 0x1234, 0xABCD, 0x0000, 0xFFFF, 0x5678 };
            
            // Act
            var bytes = DataProcessor.UshortArrayToBytes(originalValues);
            var convertedBack = DataProcessor.BytesToUshortArray(bytes);
            
            // Assert
            Assert.Equal(originalValues, convertedBack);
        }

        [Fact]
        public void BytesToUshortArray_MaxValues_HandlesCorrectly()
        {
            // Arrange - Test with maximum ushort values
            var bytes = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0x7F };
            
            // Act
            var result = DataProcessor.BytesToUshortArray(bytes);
            
            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal(ushort.MaxValue, result[0]); // 0xFFFF
            Assert.Equal(ushort.MinValue, result[1]); // 0x0000
            Assert.Equal(0x7FFF, result[2]);         // 0x7FFF
        }

        [Fact]
        public void UshortArrayToBytes_MaxValues_HandlesCorrectly()
        {
            // Arrange
            var values = new ushort[] { ushort.MaxValue, ushort.MinValue, 0x7FFF };
            
            // Act
            var result = DataProcessor.UshortArrayToBytes(values);
            
            // Assert
            Assert.Equal(6, result.Length);
            Assert.Equal(0xFF, result[0]); // Low byte of 0xFFFF
            Assert.Equal(0xFF, result[1]); // High byte of 0xFFFF
            Assert.Equal(0x00, result[2]); // Low byte of 0x0000
            Assert.Equal(0x00, result[3]); // High byte of 0x0000
            Assert.Equal(0xFF, result[4]); // Low byte of 0x7FFF
            Assert.Equal(0x7F, result[5]); // High byte of 0x7FFF
        }

        [Fact]
        public void BytesToUshortArray_LargeArray_PerformanceTest()
        {
            // Arrange - Test with larger data set
            var bytes = new byte[2000]; // 1000 ushort values
            for (int i = 0; i < bytes.Length; i += 2)
            {
                bytes[i] = (byte)(i & 0xFF);
                bytes[i + 1] = (byte)((i >> 8) & 0xFF);
            }
            
            // Act
            var result = DataProcessor.BytesToUshortArray(bytes);
            
            // Assert
            Assert.Equal(1000, result.Length);
            
            // Verify a few values
            Assert.Equal(0x0002, result[1]); // bytes[2]=2, bytes[3]=0 -> 0x0002
            Assert.Equal(0x0004, result[2]); // bytes[4]=4, bytes[5]=0 -> 0x0004
        }
    }
}