using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using SlmpClient.Constants;
using SlmpClient.Core;
using SlmpClient.Utils;
using Xunit;
using ISlmpClientFull = SlmpClient.Utils.ISlmpClientFull;

namespace SlmpClient.Tests
{
    /// <summary>
    /// 接続プール機能の統合テスト
    /// 複数接続の管理、ヘルスチェック、リソース管理を検証
    /// </summary>
    public class ConnectionPoolIntegrationTests : IDisposable
    {
        private readonly SlmpConnectionPool _connectionPool;
        private readonly MemoryOptimizedSlmpSettings _settings;

        public ConnectionPoolIntegrationTests()
        {
            _settings = new MemoryOptimizedSlmpSettings
            {
                MaxBufferSize = 1024,
                MaxCacheEntries = 100,
                EnableCompression = false,
                UseArrayPool = true,
                UseChunkedReading = true,
                MemoryThreshold = 1024 * 1024 // 1MB
            };

            _connectionPool = new SlmpConnectionPool(_settings, maxConnections: 5);
        }

        #region Connection Pool Basic Tests

        [Fact]
        public async Task ConnectionPool_BorrowConnection_Should_CreateNewConnection()
        {
            // Arrange
            const string address = "192.168.1.1";
            const int port = 5000;
            var initialCount = _connectionPool.ActiveConnections;

            // Act
            var connection = await _connectionPool.BorrowConnectionAsync(address, port);

            // Assert
            Assert.NotNull(connection);
            Assert.Equal(initialCount + 1, _connectionPool.ActiveConnections);
            Assert.True(connection.IsConnected);

            // Cleanup
            _connectionPool.ReturnConnection(connection, true);
        }

        [Fact]
        public async Task ConnectionPool_BorrowAndReturn_Should_ReuseConnection()
        {
            // Arrange
            const string address = "192.168.1.1";
            const int port = 5000;

            // Act - First borrow
            var connection1 = await _connectionPool.BorrowConnectionAsync(address, port);
            _connectionPool.ReturnConnection(connection1, true);

            // Second borrow - should reuse the same connection type
            var connection2 = await _connectionPool.BorrowConnectionAsync(address, port);

            // Assert
            Assert.NotNull(connection2);
            Assert.Equal(1, _connectionPool.ActiveConnections);
            Assert.True(connection2.IsConnected);

            // Cleanup
            _connectionPool.ReturnConnection(connection2, true);
        }

        [Fact]
        public async Task ConnectionPool_MaxConnections_Should_RespectLimit()
        {
            // Arrange
            const string address = "192.168.1.1";
            const int port = 5000;
            var connections = new List<ISlmpClientFull>();

            try
            {
                // Act - Borrow maximum connections
                for (int i = 0; i < _connectionPool.MaxConnections; i++)
                {
                    var connection = await _connectionPool.BorrowConnectionAsync($"{address}{i}", port);
                    connections.Add(connection);
                }

                // Assert
                Assert.Equal(_connectionPool.MaxConnections, _connectionPool.ActiveConnections);
                Assert.Equal(0, _connectionPool.AvailableConnections);

                // Try to borrow one more - should timeout or throw
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                await Assert.ThrowsAnyAsync<Exception>(async () =>
                    await _connectionPool.BorrowConnectionAsync("192.168.1.99", port, cts.Token));
            }
            finally
            {
                // Cleanup
                foreach (var connection in connections)
                {
                    _connectionPool.ReturnConnection(connection, true);
                }
            }
        }

        #endregion

        #region Health Check Tests

        [Fact]
        public async Task ConnectionPool_HealthCheck_Should_RemoveUnhealthyConnections()
        {
            // Arrange
            const string address = "192.168.1.1";
            const int port = 5000;

            var connection = await _connectionPool.BorrowConnectionAsync(address, port);
            _connectionPool.ReturnConnection(connection, false); // Mark as unhealthy

            var initialAvailable = _connectionPool.AvailableConnections;

            // Act
            await _connectionPool.PerformHealthCheckAsync();

            // Assert
            var finalAvailable = _connectionPool.AvailableConnections;
            Assert.True(finalAvailable <= initialAvailable);
        }

        [Fact]
        public void ConnectionPool_GetStatistics_Should_ReturnAccurateData()
        {
            // Act
            var stats = _connectionPool.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(_connectionPool.MaxConnections, stats.MaxConnections);
            Assert.Equal(_connectionPool.ActiveConnections, stats.ActiveConnections);
            Assert.Equal(_connectionPool.AvailableConnections, stats.AvailableConnections);
            Assert.True(stats.UsageRatio >= 0.0 && stats.UsageRatio <= 1.0);
        }

        #endregion

        #region Chunk Processing Tests

        [Fact]
        public async Task ChunkProcessor_ProcessChunksAsync_Should_HandleLargeBitData()
        {
            // Arrange
            using var chunkProcessor = new ChunkProcessor<bool[]>();
            const int totalCount = 10000;
            const int chunkSize = 256;
            var processedData = new List<bool[]>();

            // Act
            await foreach (var chunk in chunkProcessor.ProcessChunksAsync<bool[]>(
                totalCount,
                chunkSize,
                async (offset, size, ct) =>
                {
                    await Task.Delay(1, ct); // Simulate processing time
                    var result = new bool[size];
                    for (int i = 0; i < size; i++)
                    {
                        result[i] = (offset + i) % 2 == 0;
                    }
                    return result;
                }))
            {
                processedData.Add(chunk);
            }

            // Assert
            var expectedChunks = (int)Math.Ceiling((double)totalCount / chunkSize);
            Assert.Equal(expectedChunks, processedData.Count);

            var totalProcessed = processedData.Sum(chunk => chunk.Length);
            Assert.Equal(totalCount, totalProcessed);

            // Verify data integrity
            for (int i = 0; i < processedData.Count - 1; i++)
            {
                Assert.Equal(chunkSize, processedData[i].Length);
            }
        }

        [Fact]
        public async Task SlmpClientChunkExtensions_ConsolidateChunksAsync_Should_MergeCorrectly()
        {
            // Arrange
            var chunks = new[]
            {
                new ushort[] { 1, 2, 3 },
                new ushort[] { 4, 5, 6 },
                new ushort[] { 7, 8, 9 }
            };

            async IAsyncEnumerable<ushort[]> GetChunksAsync()
            {
                foreach (var chunk in chunks)
                {
                    await Task.Delay(1);
                    yield return chunk;
                }
            }

            // Act
            var consolidated = await GetChunksAsync().ConsolidateChunksAsync();

            // Assert
            var expected = new ushort[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Assert.Equal(expected, consolidated);
        }

        #endregion

        #region Performance and Memory Tests

        [Fact]
        public async Task ConnectionPool_ConcurrentOperations_Should_BeThreadSafe()
        {
            // Arrange
            const int concurrentOperations = 20;
            const string baseAddress = "192.168.1.";
            const int port = 5000;

            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            // Act
            for (int i = 0; i < concurrentOperations; i++)
            {
                int index = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var address = $"{baseAddress}{index % 5}"; // Limit to 5 different addresses
                        var connection = await _connectionPool.BorrowConnectionAsync(address, port);
                        
                        // Simulate some work
                        await Task.Delay(10);
                        
                        _connectionPool.ReturnConnection(connection, true);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task MemoryOptimizer_UnderLoad_Should_MaintainLowMemoryUsage()
        {
            // Arrange
            using var memoryOptimizer = new MemoryOptimizer();
            const int iterations = 1000;
            const int bufferSize = 1024;

            var initialMemory = GC.GetTotalMemory(true);

            // Act
            for (int i = 0; i < iterations; i++)
            {
                using var buffer = memoryOptimizer.RentBuffer(bufferSize);
                
                // Simulate some processing
                var span = buffer.Memory.Span;
                for (int j = 0; j < Math.Min(100, span.Length); j++)
                {
                    span[j] = (byte)(i % 256);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);

            // Assert
            var memoryIncrease = finalMemory - initialMemory;
            
            // Memory increase should be significantly less than naive allocation
            var naiveAllocation = iterations * bufferSize;
            Assert.True(memoryIncrease < naiveAllocation * 0.1, 
                $"Memory increase ({memoryIncrease}) should be less than 10% of naive allocation ({naiveAllocation})");
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ConnectionPool_InvalidAddress_Should_ThrowException()
        {
            // Arrange
            const string? invalidAddress = null;
            const int port = 5000;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _connectionPool.BorrowConnectionAsync(invalidAddress!, port));
        }

        [Fact]
        public async Task ChunkProcessor_InvalidParameters_Should_ThrowException()
        {
            // Arrange
            using var chunkProcessor = new ChunkProcessor<int>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await foreach (var chunk in chunkProcessor.ProcessChunksAsync<int>(
                    -1, // Invalid total count
                    100,
                    async (offset, size, ct) => await Task.FromResult(0)))
                {
                    // Should not reach here
                }
            });

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await foreach (var chunk in chunkProcessor.ProcessChunksAsync<int>(
                    100,
                    0, // Invalid chunk size
                    async (offset, size, ct) => await Task.FromResult(0)))
                {
                    // Should not reach here
                }
            });
        }

        #endregion

        public void Dispose()
        {
            _connectionPool?.Dispose();
        }
    }
}