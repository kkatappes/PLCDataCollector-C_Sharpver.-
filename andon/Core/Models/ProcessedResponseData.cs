using System;
using System.Collections.Generic;
using System.Linq;
using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// パース済みレスポンスデータ（Phase5実装版、2025-11-21）
/// 新旧構造の共存期（Phase10で旧構造削除予定）
/// </summary>
public class ProcessedResponseData
{
    // ========================================
    // 新構造（Phase5～Phase10以降で使用）
    // ========================================

    /// <summary>
    /// 元の受信生データ（16進数文字列）
    /// </summary>
    public string OriginalRawData { get; set; } = string.Empty;

    /// <summary>
    /// 処理済みデータ（デバイス名キー構造）
    /// キー例: "M0", "D100", "W0x11AA"
    /// 値: DeviceData（DeviceName, Code, Address, Value, IsDWord, IsHexAddress）
    /// Phase7: DataOutputManager/LoggingManagerで使用
    /// </summary>
    public Dictionary<string, DeviceData> ProcessedData { get; set; } = new();

    /// <summary>
    /// 処理完了時刻
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// 処理時間（ミリ秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    // ========================================
    // エラー情報
    // ========================================

    /// <summary>
    /// 処理成功フラグ
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// エラー情報
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// 警告情報
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    // ========================================
    // 統計情報（自動計算）
    // ========================================

    /// <summary>
    /// 処理済みデバイス総数
    /// </summary>
    public int TotalProcessedDevices => ProcessedData.Count;

    /// <summary>
    /// ビットデバイス数（DeviceCode.IsBitDevice()で判定）
    /// </summary>
    public int BitDeviceCount => ProcessedData.Values.Count(d => d.Code.IsBitDevice());

    /// <summary>
    /// ワードデバイス数（非ビット、非DWord）
    /// </summary>
    public int WordDeviceCount => ProcessedData.Values.Count(d => !d.Code.IsBitDevice() && !d.IsDWord);

    /// <summary>
    /// ダブルワードデバイス数
    /// </summary>
    public int DWordDeviceCount => ProcessedData.Values.Count(d => d.IsDWord);

    /// <summary>
    /// フレームタイプ（4Eフレーム解析対応）
    /// </summary>
    public FrameType FrameType { get; set; } = FrameType.Frame3E;

    // ========================================
    // ユーティリティメソッド
    // ========================================

    /// <summary>
    /// デバイス名から値を取得
    /// </summary>
    public uint? GetDeviceValue(string deviceName)
    {
        return ProcessedData.TryGetValue(deviceName, out var data) ? data.Value : null;
    }

    /// <summary>
    /// ビットデバイス一覧を取得
    /// </summary>
    public List<string> GetBitDevices()
    {
        return ProcessedData.Where(kv => kv.Value.Code.IsBitDevice()).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// ワードデバイス一覧を取得
    /// </summary>
    public List<string> GetWordDevices()
    {
        return ProcessedData.Where(kv => !kv.Value.Code.IsBitDevice() && !kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }

    /// <summary>
    /// ダブルワードデバイス一覧を取得
    /// </summary>
    public List<string> GetDWordDevices()
    {
        return ProcessedData.Where(kv => kv.Value.IsDWord).Select(kv => kv.Key).ToList();
    }

    // ========================================
    // 旧構造（Phase5～Phase10で維持、Phase10で削除）
    // ========================================

    /// <summary>
    /// 旧構造：ビット・ワードデバイスリスト
    /// Phase10で削除予定
    /// Phase7完了時点で使用箇所ゼロにする
    /// </summary>
    [Obsolete("Phase10で削除予定。ProcessedDataプロパティを使用してください。")]
    public List<ProcessedDevice> BasicProcessedDevices
    {
        get => ConvertToProcessedDevices();
        set
        {
            // 旧構造からの逆変換（Phase5～7で一時的に許可、Phase7で使用禁止、Phase10で削除）
            ProcessedData.Clear();
            foreach (var device in value)
            {
                // Value プロパティ（object型）から値を取得、RawValueがゼロの場合のフォールバック
                uint deviceValue;
                if (device.RawValue != 0)
                {
                    deviceValue = device.RawValue;
                }
                else if (device.Value is ushort u16)
                {
                    deviceValue = u16;
                }
                else if (device.Value is uint u32)
                {
                    deviceValue = u32;
                }
                else if (device.Value is int i32)
                {
                    deviceValue = (uint)i32;
                }
                else if (device.Value is short i16)
                {
                    deviceValue = (ushort)i16;
                }
                else
                {
                    deviceValue = Convert.ToUInt32(device.Value);
                }

                var deviceData = new DeviceData
                {
                    DeviceName = device.DeviceName,
                    Code = Enum.Parse<DeviceCode>(device.DeviceType),
                    Address = device.Address,
                    Value = deviceValue,
                    IsDWord = false,
                    IsHexAddress = false
                };
                ProcessedData[device.DeviceName] = deviceData;
            }
        }
    }

    /// <summary>
    /// 旧構造：DWordデバイスリスト
    /// Phase10で削除予定
    /// Phase7完了時点で使用箇所ゼロにする
    /// </summary>
    [Obsolete("Phase10で削除予定。ProcessedDataプロパティを使用してください。")]
    public List<CombinedDWordDevice> CombinedDWordDevices
    {
        get => ConvertToCombinedDWordDevices();
        set
        {
            // 旧構造からの逆変換（Phase5～7で一時的に許可、Phase7で使用禁止、Phase10で削除）
            // 既存のDWordデバイス以外は保持
            var nonDWordDevices = ProcessedData.Where(kv => !kv.Value.IsDWord).ToList();
            ProcessedData.Clear();
            foreach (var kvp in nonDWordDevices)
            {
                ProcessedData[kvp.Key] = kvp.Value;
            }

            foreach (var dwordDevice in value)
            {
                var deviceData = new DeviceData
                {
                    DeviceName = dwordDevice.DeviceName,
                    Code = Enum.Parse<DeviceCode>(dwordDevice.DeviceType),
                    Address = dwordDevice.LowWordAddress,
                    Value = dwordDevice.CombinedValue,
                    IsDWord = true,
                    IsHexAddress = false
                };
                ProcessedData[dwordDevice.DeviceName] = deviceData;
            }
        }
    }

    // ========================================
    // 変換メソッド（Phase10で削除予定）
    // ========================================

    /// <summary>
    /// DeviceData → ProcessedDevice変換
    /// ConMoni互換性維持：ビット展開、変換係数対応
    /// Phase10で削除予定
    /// </summary>
    [Obsolete("Phase10で削除予定")]
    private List<ProcessedDevice> ConvertToProcessedDevices()
    {
        var result = new List<ProcessedDevice>();

        foreach (var kvp in ProcessedData.Where(kv => !kv.Value.IsDWord))
        {
            var deviceData = kvp.Value;
            var processed = new ProcessedDevice
            {
                DeviceName = deviceData.DeviceName,
                DeviceType = deviceData.Code.ToString(),
                Address = deviceData.Address,
                RawValue = (ushort)deviceData.Value,
                ConvertedValue = deviceData.Value,  // Phase6で変換係数対応予定
                ConversionFactor = 1.0,
                DataType = deviceData.IsDWord ? "DWord" : "Word",
                ProcessedAt = ProcessedAt,
                Value = deviceData.Value
            };

            // ビット展開（ConMoni方式：LSB first）
            if (deviceData.Code.IsBitDevice())
            {
                processed.IsBitExpanded = true;
                processed.ExpandedBits = ExpandWordToBits((ushort)deviceData.Value);
            }

            result.Add(processed);
        }

        return result;
    }

    /// <summary>
    /// DeviceData → CombinedDWordDevice変換
    /// Phase10で削除予定
    /// </summary>
    [Obsolete("Phase10で削除予定")]
    private List<CombinedDWordDevice> ConvertToCombinedDWordDevices()
    {
        return ProcessedData
            .Where(kv => kv.Value.IsDWord)
            .Select(kv => new CombinedDWordDevice
            {
                DeviceName = kv.Key,
                DeviceType = kv.Value.Code.ToString(),
                CombinedValue = kv.Value.Value,
                LowWordAddress = kv.Value.Address,
                HighWordAddress = kv.Value.Address + 1,
                LowWordValue = (ushort)(kv.Value.Value & 0xFFFF),
                HighWordValue = (ushort)(kv.Value.Value >> 16),
                CombinedAt = ProcessedAt
            })
            .ToList();
    }

    /// <summary>
    /// ワード値をビット配列に展開（ConMoni方式：LSB first）
    /// Phase10で削除予定
    /// </summary>
    [Obsolete("Phase10で削除予定")]
    private bool[] ExpandWordToBits(ushort value)
    {
        var bits = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            bits[i] = ((value >> i) & 1) == 1;
        }
        return bits;
    }
}
