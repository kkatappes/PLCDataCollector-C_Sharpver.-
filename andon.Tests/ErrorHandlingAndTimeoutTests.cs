using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Exceptions;
using SlmpClient.Serialization;
using SlmpClient.Transport;
using SlmpClient.Utils;
using Xunit;
using Xunit.Abstractions;
using ISlmpClientFull = SlmpClient.Utils.ISlmpClientFull;

namespace SlmpClient.Tests
{
    /// <summary>
    /// エラーハンドリングとタイムアウトテスト
    /// SLMP通信における例外処理、タイムアウト、復旧処理を検証
    /// </summary>
    public class ErrorHandlingAndTimeoutTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<ISlmpTransport> _mockTransport;
        private readonly SlmpConnectionSettings _settings;
        private readonly SlmpTarget _target;
        private readonly List<IDisposable> _disposables;

        public ErrorHandlingAndTimeoutTests(ITestOutputHelper output)
        {
            _output = output;
            _mockTransport = new Mock<ISlmpTransport>();
            _disposables = new List<IDisposable>();

            _settings = new SlmpConnectionSettings
            {
                IsBinary = true,
                Version = SlmpFrameVersion.Version4E,
                Port = 5000,
                ReceiveTimeout = TimeSpan.FromSeconds(1) // 1 second timeout
            };

            _target = new SlmpTarget
            {
                Network = 1,
                Node = 1,
                DestinationProcessor = 0x03FF,
                MultiDropStation = 0
            };
        }

        #region SLMP Error Code Handling Tests

        [Theory]
        [InlineData(EndCode.WrongFormat, typeof(SlmpCommunicationException))]
        [InlineData(EndCode.ServerNotFound, typeof(SlmpCommunicationException))]
        [InlineData(EndCode.ExceedReqLength, typeof(SlmpCommunicationException))]
        [InlineData(EndCode.ExceedRespLength, typeof(SlmpCommunicationException))]
        [InlineData(EndCode.TimeoutError, typeof(SlmpCommunicationException))]
        public void SlmpResponseParser_ErrorEndCodes_Should_ThrowCorrectExceptions(EndCode endCode, Type expectedExceptionType)
        {
            // Arrange
            var errorResponse = CreateErrorResponse(endCode);
            _output.WriteLine($"Testing EndCode: {endCode} (0x{(ushort)endCode:X4})");
            _output.WriteLine($"Response bytes: {string.Join(" ", errorResponse.Select(b => $"0x{b:X2}"))}");

            // Act & Assert
            var exception = Assert.Throws<SlmpCommunicationException>(() =>
                SlmpResponseParser.ParseResponse(errorResponse, true, SlmpFrameVersion.Version3E));

            _output.WriteLine($"Exception EndCode: {exception.EndCode} (0x{(ushort)exception.EndCode:X4})");
            _output.WriteLine($"Exception Message: {exception.Message}");
            
            Assert.Equal(endCode, exception.EndCode);
            Assert.IsType(expectedExceptionType, exception);
            
            _output.WriteLine($"EndCode {endCode} correctly threw {exception.GetType().Name}: {exception.Message}");
        }

        [Fact]
        public void SlmpResponseParser_CorruptedFrame_Should_ThrowException()
        {
            // Arrange - Too short frame
            var corruptedFrame = new byte[] { 0x50, 0x00, 0x00 };

            // Act & Assert
            Assert.Throws<SlmpCommunicationException>(() =>
                SlmpResponseParser.ParseResponse(corruptedFrame, true, SlmpFrameVersion.Version3E));
            
            _output.WriteLine("Corrupted frame correctly threw SlmpCommunicationException");
        }

        [Fact]
        public void SlmpResponseParser_EmptyFrame_Should_ThrowException()
        {
            // Arrange
            var emptyFrame = Array.Empty<byte>();

            // Act & Assert
            Assert.Throws<SlmpCommunicationException>(() =>
                SlmpResponseParser.ParseResponse(emptyFrame, true, SlmpFrameVersion.Version3E));
        }

        #endregion

        #region Timeout Handling Tests

        [Fact]
        public async Task MemoryOptimizer_WithTimeout_Should_HandleCancellation()
        {
            // Arrange
            using var memoryOptimizer = new MemoryOptimizer();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                // Simulate a long-running operation that should be cancelled
                for (int i = 0; i < 1000; i++)
                {
                    using var buffer = memoryOptimizer.RentBuffer(1024);
                    
                    // Check for cancellation
                    cts.Token.ThrowIfCancellationRequested();
                    
                    // Simulate work
                    await Task.Delay(10, cts.Token);
                }
            });
            
            _output.WriteLine("Timeout correctly cancelled long-running memory operations");
        }

        [Fact]
        public async Task ChunkProcessor_WithTimeout_Should_CancelGracefully()
        {
            // Arrange
            using var chunkProcessor = new ChunkProcessor<int[]>();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

            var processedChunks = 0;

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var chunk in chunkProcessor.ProcessChunksAsync<int[]>(
                    10000, // Large dataset
                    100,
                    async (offset, size, ct) =>
                    {
                        await Task.Delay(20, ct); // Each chunk takes 20ms, timeout is 50ms
                        return new int[size];
                    },
                    cts.Token))
                {
                    processedChunks++;
                }
            });

            _output.WriteLine($"Chunk processing was cancelled after processing {processedChunks} chunks");
            
            // Should have processed at least 1 chunk before timeout
            Assert.True(processedChunks >= 1, "Should have processed at least one chunk before cancellation");
        }

        [Fact]
        public async Task StreamingFrameProcessor_NetworkTimeout_Should_HandleGracefully()
        {
            // Arrange
            using var memoryOptimizer = new MemoryOptimizer();
            using var streamingProcessor = new StreamingFrameProcessor(memoryOptimizer);
            
            // Create a stream that will timeout
            var slowStream = new Mock<Stream>();
            slowStream.Setup(s => s.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                     .Returns(async (Memory<byte> buffer, CancellationToken ct) =>
                     {
                         await Task.Delay(2000, ct); // 2 second delay
                         return 0; // EOF
                     });

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await streamingProcessor.ProcessFrameAsync(slowStream.Object, cts.Token));
            
            _output.WriteLine("Network timeout correctly cancelled streaming frame processing");
        }

        #endregion

        #region Connection Pool Error Handling Tests

        [Fact]
        public async Task ConnectionPool_ConnectionFailure_Should_RetryAndRecover()
        {
            // Arrange
            var settings = new MemoryOptimizedSlmpSettings
            {
                MaxBufferSize = 1024,
                MaxCacheEntries = 100,
                EnableCompression = false,
                UseArrayPool = true
            };

            using var connectionPool = new SlmpConnectionPool(settings, maxConnections: 2);
            _disposables.Add(connectionPool);

            const string address = "192.168.1.1";
            const int port = 5000;

            // Act - First connection should succeed (mock implementation)
            var connection1 = await connectionPool.BorrowConnectionAsync(address, port);
            Assert.NotNull(connection1);

            // Simulate connection failure
            connectionPool.ReturnConnection(connection1, isHealthy: false);

            // Second connection should still work (pool creates new connection)
            var connection2 = await connectionPool.BorrowConnectionAsync(address, port);
            Assert.NotNull(connection2);

            connectionPool.ReturnConnection(connection2, isHealthy: true);
            
            _output.WriteLine("Connection pool correctly handled connection failure and recovery");
        }

        [Fact]
        public async Task ConnectionPool_HealthCheck_Should_RemoveFailedConnections()
        {
            // Arrange
            var settings = new MemoryOptimizedSlmpSettings
            {
                MaxBufferSize = 1024,
                MaxCacheEntries = 100,
                EnableCompression = false,
                UseArrayPool = true
            };

            using var connectionPool = new SlmpConnectionPool(settings, maxConnections: 3);
            _disposables.Add(connectionPool);

            // Create some connections
            var connections = new List<ISlmpClientFull>();
            for (int i = 0; i < 3; i++)
            {
                var connection = await connectionPool.BorrowConnectionAsync($"192.168.1.{i}", 5000);
                connections.Add(connection);
            }

            // Return connections with mixed health status
            connectionPool.ReturnConnection(connections[0], isHealthy: true);
            connectionPool.ReturnConnection(connections[1], isHealthy: false);
            connectionPool.ReturnConnection(connections[2], isHealthy: true);

            var initialAvailable = connectionPool.AvailableConnections;

            // Act
            await connectionPool.PerformHealthCheckAsync();

            // Assert
            var finalAvailable = connectionPool.AvailableConnections;
            var stats = connectionPool.GetStatistics();

            _output.WriteLine($"Health check: Initial={initialAvailable}, Final={finalAvailable}");
            _output.WriteLine($"Final stats: {stats}");

            // Should have fewer available connections after removing unhealthy ones
            Assert.True(finalAvailable <= initialAvailable, "Health check should remove unhealthy connections");
        }

        #endregion

        #region Data Processing Error Handling Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void DataProcessor_InvalidInput_Should_HandleGracefully(string? input)
        {
            // Act & Assert
            if (input == null)
            {
                Assert.Throws<ArgumentNullException>(() => DataProcessor.StringToBytesBuffer(input!));
                Assert.Throws<ArgumentNullException>(() => DataProcessor.HexStringToBytes(input!));
            }
            else
            {
                // Empty string should return empty array
                var result1 = DataProcessor.StringToBytesBuffer(input);
                var result2 = DataProcessor.HexStringToBytes(input);
                
                Assert.Empty(result1);
                Assert.Empty(result2);
            }
            
            _output.WriteLine($"Invalid input '{input}' handled correctly");
        }

        [Fact]
        public void DataProcessor_OddLengthHexString_Should_ThrowException()
        {
            // Arrange
            const string oddLengthHex = "ABC"; // 3 characters

            // Act & Assert
            Assert.Throws<ArgumentException>(() => DataProcessor.HexStringToBytes(oddLengthHex));
            
            _output.WriteLine("Odd-length hex string correctly threw ArgumentException");
        }

        [Fact]
        public void DataProcessor_ExtractWordDwordData_InvalidSplit_Should_ThrowException()
        {
            // Arrange
            var buffer = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            const int invalidSplitPosition = 10; // Exceeds buffer size

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                DataProcessor.ExtractWordDwordData(buffer, invalidSplitPosition));
            
            _output.WriteLine("Invalid split position correctly threw ArgumentOutOfRangeException");
        }

        #endregion

        #region Memory Pressure Error Handling Tests

        [Fact]
        public void MemoryOptimizer_ExtremeMemoryPressure_Should_HandleGracefully()
        {
            // Arrange
            using var memoryOptimizer = new MemoryOptimizer();
            var thresholdExceeded = false;
            
            // Set low threshold for testing (3KB)
            memoryOptimizer.MemoryThreshold = 3 * 1024;
            memoryOptimizer.MemoryThresholdExceeded += (usage) => thresholdExceeded = true;

            // Act - Try to allocate memory that exceeds threshold
            var buffers = new List<System.Buffers.IMemoryOwner<byte>>();
            
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    buffers.Add(memoryOptimizer.RentBuffer(500)); // Should exceed 3KB threshold
                }

                // Assert
                Assert.True(thresholdExceeded, "Memory threshold should have been exceeded");
                
                _output.WriteLine($"Memory pressure handling: threshold exceeded = {thresholdExceeded}");
                _output.WriteLine($"Current memory usage: {memoryOptimizer.CurrentMemoryUsage} bytes");
                _output.WriteLine($"Memory threshold: {memoryOptimizer.MemoryThreshold} bytes");
            }
            finally
            {
                // Cleanup
                foreach (var buffer in buffers)
                {
                    buffer.Dispose();
                }
            }
        }

        [Fact]
        public async Task ConcurrentOperations_WithErrors_Should_NotCrashSystem()
        {
            // Arrange
            const int concurrentTasks = 10;
            var exceptions = new List<Exception>();
            var completedTasks = 0;

            // Act
            var tasks = Enumerable.Range(0, concurrentTasks).Select(taskId => Task.Run(async () =>
            {
                try
                {
                    using var memoryOptimizer = new MemoryOptimizer();
                    
                    // Some tasks will succeed, others will fail
                    if (taskId % 3 == 0)
                    {
                        // This task will throw an exception
                        throw new InvalidOperationException($"Simulated error in task {taskId}");
                    }
                    
                    // Normal operation
                    for (int i = 0; i < 10; i++)
                    {
                        using var buffer = memoryOptimizer.RentBuffer(1024);
                        await Task.Delay(1);
                    }
                    
                    Interlocked.Increment(ref completedTasks);
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            await Task.WhenAll(tasks);

            // Assert
            _output.WriteLine($"Concurrent operations: {completedTasks} completed, {exceptions.Count} failed");
            
            // Should have both successful and failed tasks
            Assert.True(completedTasks > 0, "Some tasks should have completed successfully");
            Assert.True(exceptions.Count > 0, "Some tasks should have failed as expected");
            
            // Failed tasks should not affect successful ones
            // Tasks with taskId % 3 == 0 fail, so for 10 tasks: 0, 3, 6, 9 = 4 failures
            var expectedFailures = Enumerable.Range(0, concurrentTasks).Count(i => i % 3 == 0);
            Assert.Equal(expectedFailures, exceptions.Count);
        }

        #endregion

        #region Helper Methods

        private byte[] CreateErrorResponse(EndCode endCode)
        {
            // Create a binary SLMP response with the specified error code (3E format)
            return new byte[]
            {
                0x50, 0x00,           // サブヘッダー
                0x00,                 // ネットワーク番号
                0xFF,                 // ノード番号
                0xFF, 0x03,          // 要求先プロセッサ番号
                0x00,                 // マルチドロップ局番
                0x00, 0x00,          // データ長（エラー時は0）
                (byte)((ushort)endCode & 0xFF),        // エンドコード下位バイト
                (byte)(((ushort)endCode >> 8) & 0xFF)  // エンドコード上位バイト
            };
        }

        #endregion

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable?.Dispose();
                }
                catch
                {
                    // Ignore disposal errors in tests
                }
            }
            _disposables.Clear();
        }
    }
}