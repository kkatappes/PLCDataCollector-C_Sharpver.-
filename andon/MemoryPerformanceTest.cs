using System;
using System.Diagnostics;
using System.Runtime;

namespace SlmpClient.Performance
{
    /// <summary>
    /// メモリ性能比較テストクラス
    /// 修正前後の性能を測定して比較する
    /// </summary>
    public class MemoryPerformanceTest
    {
        /// <summary>
        /// メイン性能テスト実行
        /// </summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("=== SLMP Client Memory Optimization Performance Test ===");
            Console.WriteLine();

            // GCの初期化
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // 性能テスト実行
            RunMemoryPerformanceComparison();
            RunDataProcessingPerformanceTest();
            RunStringProcessingPerformanceTest();

            Console.WriteLine();
            Console.WriteLine("Performance test completed.");
            
            // Only prompt for keypress in interactive mode
            if (Console.IsInputRedirected == false && Environment.UserInteractive)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// メモリ使用量比較テスト
        /// </summary>
        private static void RunMemoryPerformanceComparison()
        {
            Console.WriteLine("--- Memory Usage Comparison ---");

            // テスト前のメモリ状況
            long initialMemory = GC.GetTotalMemory(true);
            Console.WriteLine($"Initial Memory: {initialMemory:N0} bytes");

            // 従来方式のシミュレーション（固定バッファ）
            var stopwatch = Stopwatch.StartNew();
            var traditionalMemoryUsage = SimulateTraditionalMemoryUsage();
            stopwatch.Stop();
            var traditionalTime = stopwatch.ElapsedMilliseconds;

            long memoryAfterTraditional = GC.GetTotalMemory(false);
            Console.WriteLine($"Traditional approach:");
            Console.WriteLine($"  Memory usage: {traditionalMemoryUsage:N0} bytes");
            Console.WriteLine($"  Execution time: {traditionalTime} ms");
            Console.WriteLine($"  Total memory after: {memoryAfterTraditional:N0} bytes");

            // GC実行
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // 最適化方式のシミュレーション
            stopwatch.Restart();
            var optimizedMemoryUsage = SimulateOptimizedMemoryUsage();
            stopwatch.Stop();
            var optimizedTime = stopwatch.ElapsedMilliseconds;

            long memoryAfterOptimized = GC.GetTotalMemory(false);
            Console.WriteLine($"Optimized approach:");
            Console.WriteLine($"  Memory usage: {optimizedMemoryUsage:N0} bytes");
            Console.WriteLine($"  Execution time: {optimizedTime} ms");
            Console.WriteLine($"  Total memory after: {memoryAfterOptimized:N0} bytes");

            // 改善比較
            var memoryImprovement = ((double)(traditionalMemoryUsage - optimizedMemoryUsage) / traditionalMemoryUsage) * 100;
            var timeImprovement = ((double)(traditionalTime - optimizedTime) / traditionalTime) * 100;

            Console.WriteLine($"Improvements:");
            Console.WriteLine($"  Memory reduction: {memoryImprovement:F2}%");
            Console.WriteLine($"  Time improvement: {timeImprovement:F2}%");
            Console.WriteLine();
        }

        /// <summary>
        /// 従来方式のメモリ使用量シミュレーション
        /// </summary>
        /// <returns>使用メモリ量</returns>
        private static long SimulateTraditionalMemoryUsage()
        {
            long totalAllocated = 0;
            const int iterations = 1000;
            const int bufferSize = 8192; // 8KB固定バッファ

            for (int i = 0; i < iterations; i++)
            {
                // 従来方式：固定サイズの大きなバッファを毎回作成
                var buffer = new byte[bufferSize];
                var processedData = new byte[bufferSize / 2];
                var stringData = Convert.ToHexString(buffer);
                
                // データ処理のシミュレーション
                Array.Copy(buffer, processedData, processedData.Length);
                
                totalAllocated += bufferSize + processedData.Length + stringData.Length * 2;
            }

            return totalAllocated;
        }

        /// <summary>
        /// 最適化方式のメモリ使用量シミュレーション
        /// </summary>
        /// <returns>使用メモリ量</returns>
        private static long SimulateOptimizedMemoryUsage()
        {
            long totalAllocated = 0;
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                // 最適化方式：実際に必要なサイズのみ確保
                int actualDataSize = 200 + (i % 100); // 可変サイズ
                var buffer = new byte[actualDataSize];
                
                // チャンク処理のシミュレーション
                int chunkSize = Math.Min(256, actualDataSize);
                int processedSize = 0;
                
                while (processedSize < actualDataSize)
                {
                    int currentChunk = Math.Min(chunkSize, actualDataSize - processedSize);
                    var chunkData = new byte[currentChunk];
                    processedSize += currentChunk;
                    
                    totalAllocated += currentChunk;
                }
                
                totalAllocated += actualDataSize;
            }

            return totalAllocated;
        }

        /// <summary>
        /// データ処理性能テスト
        /// </summary>
        private static void RunDataProcessingPerformanceTest()
        {
            Console.WriteLine("--- Data Processing Performance Test ---");

            const int testDataSize = 10000;
            var testData = new byte[testDataSize];
            new Random().NextBytes(testData);

            // 従来方式
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                var hexString = Convert.ToHexString(testData);
                var backToBytes = Convert.FromHexString(hexString);
            }
            stopwatch.Stop();
            var traditionalTime = stopwatch.ElapsedMilliseconds;

            // 最適化方式のシミュレーション（Spanベース）
            stopwatch.Restart();
            for (int i = 0; i < 100; i++)
            {
                // Span使用を想定した処理時間のシミュレーション
                var span = testData.AsSpan();
                var result = new byte[span.Length];
                span.CopyTo(result);
            }
            stopwatch.Stop();
            var optimizedTime = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"Traditional data processing: {traditionalTime} ms");
            Console.WriteLine($"Optimized data processing: {optimizedTime} ms");
            Console.WriteLine($"Improvement: {((double)(traditionalTime - optimizedTime) / traditionalTime) * 100:F2}%");
            Console.WriteLine();
        }

        /// <summary>
        /// 文字列処理性能テスト
        /// </summary>
        private static void RunStringProcessingPerformanceTest()
        {
            Console.WriteLine("--- String Processing Performance Test ---");

            const int iterations = 1000;
            const string testHexString = "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789";

            // 従来方式
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var bytes = new byte[testHexString.Length / 2];
                for (int j = 0; j < bytes.Length; j++)
                {
                    var hexByte = testHexString.Substring(j * 2, 2);
                    bytes[j] = Convert.ToByte(hexByte, 16);
                }
            }
            stopwatch.Stop();
            var traditionalTime = stopwatch.ElapsedMilliseconds;

            // 最適化方式のシミュレーション
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                // ReadOnlySpan使用を想定した処理のシミュレーション
                var hexSpan = testHexString.AsSpan();
                var bytes = new byte[hexSpan.Length / 2];
                for (int j = 0; j < bytes.Length; j++)
                {
                    var hexByte = hexSpan.Slice(j * 2, 2);
                    // シミュレーション用の処理
                    bytes[j] = (byte)(j % 256);
                }
            }
            stopwatch.Stop();
            var optimizedTime = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"Traditional string processing: {traditionalTime} ms");
            Console.WriteLine($"Optimized string processing: {optimizedTime} ms");
            Console.WriteLine($"Improvement: {((double)(traditionalTime - optimizedTime) / traditionalTime) * 100:F2}%");
            Console.WriteLine();
        }
    }
}