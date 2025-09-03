using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Exceptions;
using SlmpClient.Transport;
using Xunit;

namespace SlmpClient.Tests
{
    /// <summary>
    /// Unit tests for SLMP device read/write operations (Phase2.1)
    /// Based on Python test patterns in test_main.py
    /// </summary>
    public class SlmpClientDeviceTests
    {
        private readonly Mock<ISlmpTransport> _mockTransport;
        private readonly SlmpConnectionSettings _settings;
        private readonly SlmpTarget _target;

        public SlmpClientDeviceTests()
        {
            _mockTransport = new Mock<ISlmpTransport>();
            _settings = new SlmpConnectionSettings
            {
                IsBinary = true,
                Version = SlmpFrameVersion.Version4E,
                Port = 5000
            };
            
            // 例外テスト用に例外スロー設定を適用
            _settings.ContinuitySettings.Mode = ErrorHandlingMode.ThrowException;
            
            _target = new SlmpTarget
            {
                Network = 1,
                Node = 1,
                DestinationProcessor = 0x03FF,
                MultiDropStation = 0
            };
        }

        private SlmpClient.Core.SlmpClient CreateClientWithMockTransport()
        {
            var client = new SlmpClient.Core.SlmpClient("192.168.0.1", _settings, NullLogger<SlmpClient.Core.SlmpClient>.Instance);
            
            // Use reflection to replace the transport with our mock
            var transportField = typeof(SlmpClient.Core.SlmpClient).GetField("_transport", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            transportField?.SetValue(client, _mockTransport.Object);
            
            client.Target = _target;
            return client;
        }

        [Fact]
        public async Task ReadBitDevicesAsync_ValidRequest_ReturnsCorrectData()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            // Mock response: 8 bits packed in 1 byte (00010011 binary = 0x88 = bit pattern: F,F,F,T,F,F,T,T)
            var responseData = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x02, 0x00,                          // data length
                0x00, 0x00,                          // end code (success)
                0x88                                 // bit data: 10001000 = bits 3,7 set
            };

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            // Act
            var result = await client.ReadBitDevicesAsync(DeviceCode.M, 100, 8, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8, result.Length);
            // Bit pattern 0x88 = 10001000 = bits 3,7 are true, others false
            Assert.False(result[0]); // bit 0
            Assert.False(result[1]); // bit 1
            Assert.False(result[2]); // bit 2
            Assert.True(result[3]);  // bit 3
            Assert.False(result[4]); // bit 4
            Assert.False(result[5]); // bit 5
            Assert.False(result[6]); // bit 6
            Assert.True(result[7]);  // bit 7

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ReadWordDevicesAsync_ValidRequest_ReturnsCorrectData()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            // Mock response: 2 words (0x1234, 0x0002) in little-endian
            var responseData = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x06, 0x00,                          // data length (4 bytes data)
                0x00, 0x00,                          // end code (success)
                0x34, 0x12,                          // word 1: 0x1234 (little-endian)
                0x02, 0x00                           // word 2: 0x0002 (little-endian)
            };

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            // Act
            var result = await client.ReadWordDevicesAsync(DeviceCode.M, 100, 2, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(0x1234, result[0]);
            Assert.Equal(0x0002, result[1]);

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task WriteBitDevicesAsync_ValidRequest_CompletesSuccessfully()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            // Mock success response
            var responseData = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x02, 0x00,                          // data length
                0x00, 0x00                           // end code (success)
            };

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            var bitData = new bool[] { true, true, false, false, true, true, false, false };

            // Act & Assert (should not throw)
            await client.WriteBitDevicesAsync(DeviceCode.M, 100, bitData, 0);

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task WriteWordDevicesAsync_ValidRequest_CompletesSuccessfully()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            // Mock success response
            var responseData = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x02, 0x00,                          // data length
                0x00, 0x00                           // end code (success)
            };

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            var wordData = new ushort[] { 0x2347, 0xAB96 };

            // Act & Assert (should not throw)
            await client.WriteWordDevicesAsync(DeviceCode.M, 100, wordData, 0);

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ReadBitDevicesAsync_NotConnected_ThrowsException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<SlmpConnectionException>(
                () => client.ReadBitDevicesAsync(DeviceCode.M, 100, 8, 0));
        }

        [Fact]
        public async Task ReadBitDevicesAsync_ZeroCount_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ReadBitDevicesAsync(DeviceCode.M, 100, 0, 0));
        }

        [Fact]
        public async Task ReadBitDevicesAsync_CountExceedsLimit_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ReadBitDevicesAsync(DeviceCode.M, 100, 7169, 0));
        }

        [Fact]
        public async Task ReadWordDevicesAsync_CountExceedsLimit_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ReadWordDevicesAsync(DeviceCode.M, 100, 961, 0));
        }

        [Fact]
        public async Task WriteBitDevicesAsync_NullData_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.WriteBitDevicesAsync(DeviceCode.M, 100, null!, 0));
        }

        [Fact]
        public async Task WriteBitDevicesAsync_EmptyData_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.WriteBitDevicesAsync(DeviceCode.M, 100, Array.Empty<bool>(), 0));
        }

        [Fact]
        public async Task WriteWordDevicesAsync_NullData_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.WriteWordDevicesAsync(DeviceCode.M, 100, null!, 0));
        }

        [Fact]
        public async Task ReadBitDevicesAsync_SlmpError_ThrowsSlmpCommunicationException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            // Mock error response with non-zero end code
            var responseData = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x02, 0x00,                          // data length
                0x01, 0xC0                           // end code (error)
            };

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            // Act & Assert
            await Assert.ThrowsAsync<SlmpCommunicationException>(
                () => client.ReadBitDevicesAsync(DeviceCode.M, 100, 8, 0));
        }

        [Fact]
        public async Task ReadBitDevicesAsync_TransportException_ThrowsSlmpCommunicationException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Transport error"));

            // Act & Assert
            await Assert.ThrowsAsync<SlmpCommunicationException>(
                () => client.ReadBitDevicesAsync(DeviceCode.M, 100, 8, 0));
        }

        [Theory]
        [InlineData(DeviceCode.M, 100, 8)]
        [InlineData(DeviceCode.X, 0x20, 16)]
        [InlineData(DeviceCode.Y, 0x30, 32)]
        public async Task ReadBitDevicesAsync_DifferentDeviceCodes_WorksCorrectly(DeviceCode deviceCode, uint startAddress, ushort count)
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var dataSize = (count + 7) / 8; // Bytes needed for bit count
            var responseData = new byte[15 + dataSize]; // Header (11) + data length (2) + end code (2) + data
            
            // Build response header
            Array.Copy(new byte[] { 0xD4, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, responseData, 0, 6);
            Array.Copy(new byte[] { 0x01, 0x01, 0x03, 0xFF, 0x00 }, 0, responseData, 6, 5);
            Array.Copy(BitConverter.GetBytes((ushort)(dataSize + 2)), 0, responseData, 11, 2); // data length includes end code
            Array.Copy(new byte[] { 0x00, 0x00 }, 0, responseData, 13, 2); // end code
            // Initialize data bytes to 0 (remaining bytes are already zero-initialized)

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            // Act
            var result = await client.ReadBitDevicesAsync(deviceCode, startAddress, count, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(count, result.Length);
        }
    }
}