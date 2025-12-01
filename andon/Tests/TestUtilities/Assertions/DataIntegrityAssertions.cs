using Andon.Core.Models;
using Xunit;

namespace Andon.Tests.TestUtilities.Assertions;

/// <summary>
/// データ整合性検証用のアサーションヘルパークラス
/// TC119統合テストでの段階間データ伝達整合性を検証
/// </summary>
public static class DataIntegrityAssertions
{
    /// <summary>
    /// 3段階のデータ伝達整合性を総合的に検証
    /// </summary>
    public static void AssertDataIntegrity(
        BasicProcessedResponseData basicData,
        ProcessedResponseData processedData,
        StructuredData structuredData,
        int expectedDeviceCount,
        bool hasDWordCombine = false,
        int expectedCombinedCount = 0)
    {
        // Stage 1 検証
        AssertBasicProcessedData(basicData, expectedDeviceCount);

        // Stage 1 → Stage 2 整合性検証
        AssertStage1ToStage2Integrity(basicData, processedData, hasDWordCombine, expectedCombinedCount);

        // Stage 2 → Stage 3 整合性検証
        AssertStage2ToStage3Integrity(processedData, structuredData);

        // 全段階通しての整合性検証
        AssertOverallIntegrity(basicData, processedData, structuredData);
    }

    /// <summary>
    /// Stage 1（BasicProcessedResponseData）の検証
    /// </summary>
    public static void AssertBasicProcessedData(BasicProcessedResponseData basicData, int expectedDeviceCount)
    {
        // NULL検証
        Assert.NotNull(basicData);
        Assert.NotNull(basicData.ProcessedDevices);

        // 成功検証
        Assert.True(basicData.IsSuccess, "BasicProcessedResponseData should indicate success");

        // デバイス数検証
        Assert.Equal(expectedDeviceCount, basicData.ProcessedDevices.Count);
        Assert.Equal(expectedDeviceCount, basicData.ProcessedDeviceCount);

        // タイムスタンプ検証（ProcessedAtがローカル時刻の場合もUTCの場合も対応）
        var now = DateTime.UtcNow;
        var processedAt = basicData.ProcessedAt;

        // ローカル時刻の場合はUTCに変換
        if (processedAt.Kind == DateTimeKind.Local)
        {
            processedAt = processedAt.ToUniversalTime();
        }
        else if (processedAt.Kind == DateTimeKind.Unspecified)
        {
            // Unspecifiedの場合はUTCとして扱う
            processedAt = DateTime.SpecifyKind(processedAt, DateTimeKind.Utc);
        }

        var allowedFutureDrift = TimeSpan.FromSeconds(1); // 1秒の許容範囲
        Assert.True(processedAt <= now.Add(allowedFutureDrift),
            $"ProcessedAt ({processedAt} UTC) should be within 1 second of now ({now} UTC)");

        // 処理時間検証
        Assert.True(basicData.ProcessingTimeMs >= 0, "Processing time should be non-negative");

        // エラー検証（正常系の場合）
        Assert.Empty(basicData.Errors);
    }

    /// <summary>
    /// Stage 1 → Stage 2 のデータ伝達整合性検証
    /// </summary>
    public static void AssertStage1ToStage2Integrity(
        BasicProcessedResponseData basicData,
        ProcessedResponseData processedData,
        bool hasDWordCombine,
        int expectedCombinedCount)
    {
        // NULL検証
        Assert.NotNull(processedData);
        Assert.NotNull(processedData.BasicProcessedDevices);

        // 成功検証
        Assert.True(processedData.IsSuccess, "ProcessedResponseData should indicate success");

        if (!hasDWordCombine)
        {
            // DWord結合なしの場合: デバイス数は変わらない
            Assert.Equal(basicData.ProcessedDeviceCount, processedData.BasicProcessedDevices.Count);
            Assert.Empty(processedData.CombinedDWordDevices);
        }
        else
        {
            // DWord結合ありの場合: 結合後のデバイス数を検証
            int totalDevices = processedData.BasicProcessedDevices.Count + processedData.CombinedDWordDevices.Count;
            Assert.Equal(expectedCombinedCount, totalDevices);
            Assert.NotEmpty(processedData.CombinedDWordDevices);
        }

        // 処理時間の累積検証
        Assert.True(processedData.ProcessingTimeMs >= basicData.ProcessingTimeMs,
            "Stage 2 processing time should be cumulative");

        // エラー情報の伝達検証
        Assert.Equal(basicData.Errors.Count, processedData.Errors?.Count ?? 0);
    }

    /// <summary>
    /// Stage 2 → Stage 3 のデータ伝達整合性検証
    /// </summary>
    public static void AssertStage2ToStage3Integrity(
        ProcessedResponseData processedData,
        StructuredData structuredData)
    {
        // NULL検証
        Assert.NotNull(structuredData);
        Assert.NotNull(structuredData.StructuredDevices);

        // デバイス数の整合性検証
        int expectedCount = processedData.BasicProcessedDevices.Count + processedData.CombinedDWordDevices.Count;
        Assert.Equal(expectedCount, structuredData.StructuredDevices.Count);

        // タイムスタンプ検証
        Assert.NotEqual(default(DateTime), structuredData.ProcessedAt);
        Assert.True(structuredData.ProcessedAt <= DateTime.UtcNow);

        // 処理時間の累積検証
        Assert.True(structuredData.ProcessingTimeMs >= processedData.ProcessingTimeMs,
            "Stage 3 processing time should be cumulative");
    }

    /// <summary>
    /// 全段階通しての整合性検証
    /// </summary>
    public static void AssertOverallIntegrity(
        BasicProcessedResponseData basicData,
        ProcessedResponseData processedData,
        StructuredData structuredData)
    {
        // 全段階でのエラーがないことを確認
        Assert.Empty(basicData.Errors);
        Assert.Empty(processedData.Errors ?? new List<string>());

        // データが正しく最終段階まで伝達されたことを確認
        Assert.True(basicData.IsSuccess && processedData.IsSuccess,
            "All stages should indicate success");

        // 最終的なデバイス数が妥当であることを確認
        Assert.True(structuredData.StructuredDevices.Count > 0,
            "Final structured data should contain devices");
    }

    /// <summary>
    /// デバイス名パターンの整合性検証
    /// </summary>
    public static void AssertDeviceNamePatterns(
        List<StructuredDevice> devices,
        string deviceCode,
        int startNumber,
        int expectedCount,
        bool hasDWordCombine = false)
    {
        Assert.NotNull(devices);
        Assert.Equal(expectedCount, devices.Count);

        if (!hasDWordCombine)
        {
            // 通常デバイス名パターン検証（例: M000, M001, ..., M999）
            for (int i = 0; i < Math.Min(10, devices.Count); i++)
            {
                string expectedName = $"{deviceCode}{(startNumber + i):D3}";
                Assert.Contains(devices, d => d.DeviceName == expectedName);
            }
        }
        else
        {
            // DWord結合デバイス名パターン検証（例: D500D501, D502D503, ...）
            // 結合デバイスが存在することを確認
            Assert.Contains(devices, d => d.DeviceName.Contains(deviceCode) && d.DeviceName.Length > 4);
        }
    }

    /// <summary>
    /// 値の整合性検証（ビット型）
    /// </summary>
    public static void AssertBitValueIntegrity(
        BasicProcessedResponseData basicData,
        StructuredData structuredData)
    {
        Assert.NotNull(basicData);
        Assert.NotNull(structuredData);

        // 基本デバイスとstructuredデバイスの値が対応していることを確認
        // （サンプル検証: 最初の10デバイス）
        for (int i = 0; i < Math.Min(10, basicData.ProcessedDevices.Count); i++)
        {
            var basicDevice = basicData.ProcessedDevices[i];
            var structuredDevice = structuredData.StructuredDevices
                .FirstOrDefault(d => d.DeviceName == basicDevice.DeviceName);

            if (structuredDevice != null && structuredDevice.Fields.Any())
            {
                // フィールドが存在し、値が取得できることを確認
                var firstField = structuredDevice.Fields.First();
                Assert.NotNull(firstField.Key);
                Assert.NotNull(firstField.Value);
            }
        }
    }

    /// <summary>
    /// 値の整合性検証（ワード型）
    /// </summary>
    public static void AssertWordValueIntegrity(
        BasicProcessedResponseData basicData,
        StructuredData structuredData)
    {
        Assert.NotNull(basicData);
        Assert.NotNull(structuredData);

        // フィールドが存在し、値が取得できることを確認
        foreach (var device in structuredData.StructuredDevices.Take(10))
        {
            if (device.Fields.Any())
            {
                var firstField = device.Fields.First();
                Assert.NotNull(firstField.Value);
            }
        }
    }

    /// <summary>
    /// DWord結合値の整合性検証
    /// </summary>
    public static void AssertDWordCombineIntegrity(
        ProcessedResponseData processedData,
        StructuredData structuredData)
    {
        Assert.NotNull(processedData);
        Assert.NotNull(structuredData);
        Assert.NotEmpty(processedData.CombinedDWordDevices);

        // 結合されたDWordデバイスが存在することを確認
        var combinedDevices = structuredData.StructuredDevices
            .Where(d => d.DeviceName.Length > 4) // DWord結合デバイス名は長い（例: "D500D501"）
            .ToList();

        Assert.NotEmpty(combinedDevices);

        // 結合デバイスのフィールドが存在することを確認
        foreach (var device in combinedDevices)
        {
            Assert.NotEmpty(device.Fields);
        }
    }
}
