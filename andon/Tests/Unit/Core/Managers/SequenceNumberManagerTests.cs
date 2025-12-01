using Xunit;
using Andon.Core.Managers;
using System.Collections.Concurrent;

namespace Andon.Tests.Unit.Core.Managers;

/// <summary>
/// SequenceNumberManager のユニットテスト
/// Phase1: 準備と基礎クラス実装
/// </summary>
public class SequenceNumberManagerTests
{
    /// <summary>
    /// TC001: 初期値が0であること
    /// </summary>
    [Fact]
    public void GetCurrent_初期状態_ゼロを返す()
    {
        // Arrange
        var manager = new SequenceNumberManager();

        // Act
        var current = manager.GetCurrent();

        // Assert
        Assert.Equal((ushort)0, current);
    }

    /// <summary>
    /// TC002: 3Eフレームでは常に0を返すこと
    /// </summary>
    [Fact]
    public void GetNext_3Eフレーム_常にゼロを返す()
    {
        // Arrange
        var manager = new SequenceNumberManager();

        // Act
        var seq1 = manager.GetNext("3E");
        var seq2 = manager.GetNext("3E");
        var seq3 = manager.GetNext("3E");

        // Assert
        Assert.Equal((ushort)0, seq1);
        Assert.Equal((ushort)0, seq2);
        Assert.Equal((ushort)0, seq3);
    }

    /// <summary>
    /// TC003: 4Eフレームで呼び出すたびにインクリメントされること
    /// </summary>
    [Fact]
    public void GetNext_4Eフレーム_インクリメントされる()
    {
        // Arrange
        var manager = new SequenceNumberManager();

        // Act
        var seq1 = manager.GetNext("4E");
        var seq2 = manager.GetNext("4E");
        var seq3 = manager.GetNext("4E");

        // Assert
        Assert.Equal((ushort)0, seq1);
        Assert.Equal((ushort)1, seq2);
        Assert.Equal((ushort)2, seq3);
    }

    /// <summary>
    /// TC004: 0xFF超過時に0にリセットされること
    /// </summary>
    [Fact]
    public void GetNext_4Eフレーム_0xFF超過でロールオーバー()
    {
        // Arrange
        var manager = new SequenceNumberManager();

        // Act
        // 256回呼び出して0xFFを超える
        for (int i = 0; i < 256; i++)
        {
            manager.GetNext("4E");
        }

        var afterRollover = manager.GetNext("4E");

        // Assert
        Assert.Equal((ushort)0, afterRollover);
    }

    /// <summary>
    /// TC005: 並行呼び出しでも正しくインクリメントされること
    /// </summary>
    [Fact]
    public void GetNext_並行呼び出し_スレッドセーフ()
    {
        // Arrange
        var manager = new SequenceNumberManager();
        var results = new ConcurrentBag<ushort>();
        const int threadCount = 10;
        const int callsPerThread = 20; // 256未満にして重複を回避

        // Act
        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < callsPerThread; i++)
                {
                    var seq = manager.GetNext("4E");
                    results.Add(seq);
                }
            }))
            .ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.Equal(threadCount * callsPerThread, results.Count);
        Assert.Equal(results.Count, results.Distinct().Count()); // 重複なし（200 < 256）
    }

    /// <summary>
    /// TC006: Reset()で0に戻ること
    /// </summary>
    [Fact]
    public void Reset_実行後_ゼロにリセットされる()
    {
        // Arrange
        var manager = new SequenceNumberManager();
        manager.GetNext("4E");
        manager.GetNext("4E");
        manager.GetNext("4E");

        // Act
        manager.Reset();
        var afterReset = manager.GetCurrent();

        // Assert
        Assert.Equal((ushort)0, afterReset);
    }
}
