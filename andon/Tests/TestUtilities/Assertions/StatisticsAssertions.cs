using Andon.Core.Models;
using Xunit;

namespace Andon.Tests.TestUtilities.Assertions;

/// <summary>
/// 統計情報検証用ヘルパー（TC122対応）
/// 複数サイクル実行時の統計累積検証をサポート
/// </summary>
public static class StatisticsAssertions
{
    /// <summary>
    /// 累積統計検証
    /// </summary>
    /// <param name="initial">初期統計</param>
    /// <param name="final">最終統計</param>
    /// <param name="expectedCycles">期待されるサイクル数</param>
    /// <param name="expectedResponseTimes">期待されるレスポンス時間配列（ミリ秒）</param>
    public static void AssertCumulativeStatistics(
        ConnectionStats initial,
        ConnectionStats final,
        int expectedCycles,
        double[] expectedResponseTimes)
    {
        // 接続回数検証
        Assert.Equal(expectedCycles, final.TotalConnections - initial.TotalConnections);

        // 切断回数検証
        Assert.Equal(expectedCycles, final.TotalDisconnections - initial.TotalDisconnections);

        // レスポンス時間履歴検証
        var newResponseTimes = final.ResponseTimeHistory.Count - initial.ResponseTimeHistory.Count;
        Assert.Equal(expectedResponseTimes.Length, newResponseTimes);

        // 平均レスポンス時間検証（±1ms許容）
        if (expectedResponseTimes.Length > 0)
        {
            double expectedAverage = expectedResponseTimes.Average();
            Assert.InRange(final.AverageResponseTime, expectedAverage - 1, expectedAverage + 1);
        }
    }

    /// <summary>
    /// 統計進行検証（単調増加確認）
    /// </summary>
    /// <param name="statsHistory">統計履歴リスト</param>
    public static void AssertStatisticsProgression(List<ConnectionStats> statsHistory)
    {
        for (int i = 1; i < statsHistory.Count; i++)
        {
            // 総接続回数は単調増加
            Assert.True(
                statsHistory[i].TotalConnections >= statsHistory[i - 1].TotalConnections,
                $"Cycle {i}: TotalConnections should be monotonically increasing");

            // フレーム送信数は単調増加
            Assert.True(
                statsHistory[i].TotalFramesSent >= statsHistory[i - 1].TotalFramesSent,
                $"Cycle {i}: TotalFramesSent should be monotonically increasing");

            // レスポンス時間履歴数は単調増加
            Assert.True(
                statsHistory[i].ResponseTimeHistory.Count >= statsHistory[i - 1].ResponseTimeHistory.Count,
                $"Cycle {i}: ResponseTimeHistory count should be monotonically increasing");
        }
    }

    /// <summary>
    /// サイクル別統計検証
    /// </summary>
    /// <param name="cycle">サイクル番号</param>
    /// <param name="cycleStats">サイクル統計</param>
    /// <param name="expectedResponseTime">期待されるレスポンス時間（ミリ秒）</param>
    /// <param name="tolerance">許容誤差（ミリ秒）</param>
    public static void AssertCycleStatistics(
        int cycle,
        ConnectionStats cycleStats,
        double expectedResponseTime,
        double tolerance = 10.0)
    {
        // サイクル番号分の接続があるか
        Assert.True(
            cycleStats.TotalConnections >= cycle,
            $"Cycle {cycle}: Expected at least {cycle} connections, got {cycleStats.TotalConnections}");

        // レスポンス時間が期待範囲内か
        if (cycleStats.ResponseTimeHistory.Count >= cycle)
        {
            var latestResponseTime = cycleStats.ResponseTimeHistory[cycle - 1];
            Assert.InRange(
                latestResponseTime,
                expectedResponseTime - tolerance,
                expectedResponseTime + tolerance);
        }
    }

    /// <summary>
    /// エラー混入時統計検証
    /// </summary>
    /// <param name="stats">統計情報</param>
    /// <param name="expectedTotalConnections">期待される総接続試行回数（リトライ含む）</param>
    /// <param name="expectedSuccessfulConnections">期待される成功接続回数</param>
    /// <param name="expectedRetries">期待されるリトライ回数</param>
    /// <param name="expectedErrors">期待されるエラー回数</param>
    public static void AssertErrorStatistics(
        ConnectionStats stats,
        int expectedTotalConnections,
        int expectedSuccessfulConnections,
        int expectedRetries,
        int expectedErrors)
    {
        // 総接続試行回数検証
        Assert.Equal(expectedTotalConnections, stats.TotalConnections);

        // 成功接続回数検証
        Assert.Equal(expectedSuccessfulConnections, stats.SuccessfulConnections);

        // リトライ回数検証
        Assert.Equal(expectedRetries, stats.TotalRetries);

        // エラー回数検証
        Assert.Equal(expectedErrors, stats.ErrorCount);

        // 成功率検証
        double expectedSuccessRate = (double)expectedSuccessfulConnections / expectedTotalConnections * 100;
        Assert.InRange(stats.SuccessRate, expectedSuccessRate - 0.1, expectedSuccessRate + 0.1);
    }

    /// <summary>
    /// レスポンス時間統計検証
    /// </summary>
    /// <param name="stats">統計情報</param>
    /// <param name="expectedMin">期待される最短時間（ミリ秒）</param>
    /// <param name="expectedMax">期待される最長時間（ミリ秒）</param>
    /// <param name="expectedAverage">期待される平均時間（ミリ秒）</param>
    /// <param name="tolerance">許容誤差（ミリ秒）</param>
    public static void AssertResponseTimeStatistics(
        ConnectionStats stats,
        double expectedMin,
        double expectedMax,
        double expectedAverage,
        double tolerance = 10.0)
    {
        // 最短レスポンス時間検証
        Assert.InRange(stats.MinResponseTime, expectedMin - tolerance, expectedMin + tolerance);

        // 最長レスポンス時間検証
        Assert.InRange(stats.MaxResponseTime, expectedMax - tolerance, expectedMax + tolerance);

        // 平均レスポンス時間検証
        Assert.InRange(stats.AverageResponseTime, expectedAverage - tolerance, expectedAverage + tolerance);
    }

    /// <summary>
    /// リトライ履歴検証
    /// </summary>
    /// <param name="stats">統計情報</param>
    /// <param name="expectedRetryReasons">期待されるリトライ理由のリスト</param>
    public static void AssertRetryHistory(
        ConnectionStats stats,
        List<string> expectedRetryReasons)
    {
        Assert.Equal(expectedRetryReasons.Count, stats.RetryHistory.Count);

        for (int i = 0; i < expectedRetryReasons.Count; i++)
        {
            Assert.Contains(expectedRetryReasons[i], stats.RetryHistory[i].Reason);
        }
    }

    /// <summary>
    /// 統計精度検証（標準偏差）
    /// </summary>
    /// <param name="stats">統計情報</param>
    /// <param name="expectedStdDev">期待される標準偏差（ミリ秒）</param>
    /// <param name="tolerance">許容誤差（ミリ秒）</param>
    public static void AssertStatisticalAccuracy(
        ConnectionStats stats,
        double expectedStdDev,
        double tolerance = 5.0)
    {
        Assert.InRange(
            stats.ResponseTimeStandardDeviation,
            expectedStdDev - tolerance,
            expectedStdDev + tolerance);
    }
}
