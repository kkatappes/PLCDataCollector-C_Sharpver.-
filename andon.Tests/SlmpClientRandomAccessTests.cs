using System;
using System.Collections.Generic;
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
    /// Unit tests for SLMP random access operations (Phase2.2)
    /// Based on Python test patterns for random device access
    /// </summary>
    public class SlmpClientRandomAccessTests
    {
        private readonly Mock<ISlmpTransport> _mockTransport;
        private readonly SlmpConnectionSettings _settings;
        private readonly SlmpTarget _target;

        public SlmpClientRandomAccessTests()
        {
            _mockTransport = new Mock<ISlmpTransport>();
            _settings = new SlmpConnectionSettings
            {
                IsBinary = true,
                Version = SlmpFrameVersion.Version4E,
                Port = 5000
            };
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
        public async Task ReadRandomDevicesAsync_ValidRequest_ReturnsCorrectData()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)>
            {
                (DeviceCode.D, 0),
                (DeviceCode.M, 100)
            };
            
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>
            {
                (DeviceCode.D, 1500)
            };
            
            // Mock response: word data (2 bytes each) + dword data (4 bytes each)
            // Pattern from Python tests: 0x1995, 0x2030 for words, 0x4C544F4E for dword
            var responseData = new byte[]
            {
                0xD4, 0x00, 0x00, 0x00, 0x00, 0x00, // 4E response header
                0x01, 0x01, 0x03, 0xFF, 0x00,       // target response
                0x0A, 0x00,                          // data length (8 bytes data + 2 bytes end code)
                0x00, 0x00,                          // end code (success)
                // Word data (4 bytes total for 2 words)
                0x95, 0x19,                          // Word 1: 0x1995 (little-endian)
                0x30, 0x20,                          // Word 2: 0x2030 (little-endian)
                // Dword data (4 bytes total for 1 dword)
                0x4E, 0x4F, 0x54, 0x4C              // Dword 1: 0x4C544F4E (little-endian)
            };

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            // Act
            var result = await client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0);

            // Assert
            Assert.NotNull(result.wordData);
            Assert.NotNull(result.dwordData);
            Assert.Equal(2, result.wordData.Length);
            Assert.Equal(1, result.dwordData.Length);
            
            Assert.Equal(0x1995, result.wordData[0]);
            Assert.Equal(0x2030, result.wordData[1]);
            Assert.Equal(0x4C544F4Eu, result.dwordData[0]);

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task WriteRandomBitDevicesAsync_ValidRequest_CompletesSuccessfully()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var devices = new List<(DeviceCode deviceCode, uint address, bool value)>
            {
                (DeviceCode.M, 50, false),
                (DeviceCode.Y, 0x2F, true)
            };
            
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

            // Act & Assert (should not throw)
            await client.WriteRandomBitDevicesAsync(devices, 0);

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task WriteRandomWordDevicesAsync_ValidRequest_CompletesSuccessfully()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address, ushort value)>
            {
                (DeviceCode.D, 0, 0x0550),
                (DeviceCode.M, 100, 0x0540)
            };
            
            var dwordDevices = new List<(DeviceCode deviceCode, uint address, uint value)>
            {
                (DeviceCode.D, 1500, 0x04391202)
            };
            
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

            // Act & Assert (should not throw)
            await client.WriteRandomWordDevicesAsync(wordDevices, dwordDevices, 0);

            // Verify transport was called
            _mockTransport.Verify(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_NullWordDevices_ThrowsArgumentNullException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.ReadRandomDevicesAsync(null!, dwordDevices, 0));
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_NullDwordDevices_ThrowsArgumentNullException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.ReadRandomDevicesAsync(wordDevices, null!, 0));
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_NoDevicesSpecified_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)>();
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0));
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_TooManyDevices_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)>();
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();
            
            // Create 193 devices (exceeds limit of 192)
            for (int i = 0; i < 100; i++)
            {
                wordDevices.Add((DeviceCode.D, (uint)i));
            }
            for (int i = 0; i < 93; i++)
            {
                dwordDevices.Add((DeviceCode.D, (uint)(1000 + i)));
            }

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0));
        }

        [Fact]
        public async Task WriteRandomBitDevicesAsync_NullDevices_ThrowsArgumentNullException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.WriteRandomBitDevicesAsync(null!, 0));
        }

        [Fact]
        public async Task WriteRandomBitDevicesAsync_EmptyDevices_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var devices = new List<(DeviceCode deviceCode, uint address, bool value)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.WriteRandomBitDevicesAsync(devices, 0));
        }

        [Fact]
        public async Task WriteRandomBitDevicesAsync_TooManyDevices_ThrowsArgumentException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var devices = new List<(DeviceCode deviceCode, uint address, bool value)>();
            
            // Create 193 devices (exceeds limit of 192)
            for (int i = 0; i < 193; i++)
            {
                devices.Add((DeviceCode.M, (uint)i, true));
            }

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => client.WriteRandomBitDevicesAsync(devices, 0));
        }

        [Fact]
        public async Task WriteRandomWordDevicesAsync_NullWordDevices_ThrowsArgumentNullException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var dwordDevices = new List<(DeviceCode deviceCode, uint address, uint value)>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => client.WriteRandomWordDevicesAsync(null!, dwordDevices, 0));
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_NotConnected_ThrowsException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(false);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)> { (DeviceCode.D, 0) };
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();

            // Act & Assert
            await Assert.ThrowsAsync<SlmpConnectionException>(
                () => client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0));
        }

        [Fact]
        public async Task WriteRandomBitDevicesAsync_NotConnected_ThrowsException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(false);
            
            var devices = new List<(DeviceCode deviceCode, uint address, bool value)> { (DeviceCode.M, 0, true) };

            // Act & Assert
            await Assert.ThrowsAsync<SlmpConnectionException>(
                () => client.WriteRandomBitDevicesAsync(devices, 0));
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_SlmpError_ThrowsSlmpCommunicationException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)> { (DeviceCode.D, 0) };
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();
            
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
                () => client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0));
        }

        [Fact]
        public async Task ReadRandomDevicesAsync_TransportException_ThrowsSlmpCommunicationException()
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)> { (DeviceCode.D, 0) };
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();
            
            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Transport error"));

            // Act & Assert
            await Assert.ThrowsAsync<SlmpCommunicationException>(
                () => client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0));
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        [InlineData(2, 2)]
        [InlineData(10, 5)]
        public async Task ReadRandomDevicesAsync_VariousDeviceCounts_WorksCorrectly(int wordCount, int dwordCount)
        {
            // Arrange
            var client = CreateClientWithMockTransport();
            _mockTransport.Setup(t => t.IsConnected).Returns(true);
            
            var wordDevices = new List<(DeviceCode deviceCode, uint address)>();
            var dwordDevices = new List<(DeviceCode deviceCode, uint address)>();
            
            for (int i = 0; i < wordCount; i++)
            {
                wordDevices.Add((DeviceCode.D, (uint)i));
            }
            for (int i = 0; i < dwordCount; i++)
            {
                dwordDevices.Add((DeviceCode.D, (uint)(100 + i)));
            }
            
            var dataSize = wordCount * 2 + dwordCount * 4;
            var responseData = new byte[15 + dataSize]; // Header + data
            
            // Build response header
            Array.Copy(new byte[] { 0xD4, 0x00, 0x00, 0x00, 0x00, 0x00 }, 0, responseData, 0, 6);
            Array.Copy(new byte[] { 0x01, 0x01, 0x03, 0xFF, 0x00 }, 0, responseData, 6, 5);
            Array.Copy(System.BitConverter.GetBytes((ushort)(dataSize + 2)), 0, responseData, 11, 2);
            Array.Copy(new byte[] { 0x00, 0x00 }, 0, responseData, 13, 2);

            _mockTransport.Setup(t => t.SendAndReceiveAsync(
                It.IsAny<byte[]>(), 
                It.IsAny<TimeSpan>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(responseData);

            // Act
            var result = await client.ReadRandomDevicesAsync(wordDevices, dwordDevices, 0);

            // Assert
            Assert.NotNull(result.wordData);
            Assert.NotNull(result.dwordData);
            Assert.Equal(wordCount, result.wordData.Length);
            Assert.Equal(dwordCount, result.dwordData.Length);
        }
    }
}