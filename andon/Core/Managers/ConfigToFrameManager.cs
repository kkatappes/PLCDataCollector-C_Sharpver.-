using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Utilities;

namespace Andon.Core.Managers;

/// <summary>
/// Step1-2: 設定読み込み・フレーム構築
/// Phase4-ステップ12: ReadRandomフレーム構築機能実装
/// </summary>
public class ConfigToFrameManager : IConfigToFrameManager
{
    /// <summary>
    /// SLMP単位変換定数: 1単位 = 250ミリ秒
    /// </summary>
    private const int SlmpTimeoutUnit = 250;

    /// <summary>
    /// タイムアウト値をミリ秒からSLMP単位（250ms単位）に変換
    /// </summary>
    /// <param name="timeoutMs">タイムアウト値（ミリ秒）</param>
    /// <returns>タイムアウト値（SLMP単位）</returns>
    private static ushort ConvertTimeoutMsToSlmpUnit(int timeoutMs)
    {
        return (ushort)(timeoutMs / SlmpTimeoutUnit);
    }
    /// <summary>
    /// TargetDeviceConfigからReadRandomフレームを構築
    /// </summary>
    /// <param name="config">デバイス設定</param>
    /// <returns>ReadRandomフレームのバイト配列</returns>
    /// <exception cref="ArgumentNullException">config が null の場合</exception>
    /// <exception cref="ArgumentException">デバイスリストが空、またはフレームタイプが未対応の場合</exception>
    public byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config)
    {
        // 1. null チェック
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // 2. デバイスリスト検証
        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(config));
        }

        // 3. フレームタイプ検証
        if (config.FrameType != "3E" && config.FrameType != "4E")
        {
            throw new ArgumentException($"未対応のフレームタイプ: {config.FrameType}", nameof(config));
        }

        // 4. DeviceEntryをDeviceSpecificationに変換してSlmpFrameBuilder.BuildReadRandomRequest()に渡す
        // - Phase2で実装済みのメソッドを活用
        // - 内部でReadRandom(0x0403)フレームを構築
        var deviceSpecifications = config.Devices
            .Select(d => d.ToDeviceSpecification())
            .ToList();

        byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
            deviceSpecifications,
            config.FrameType,
            config.Timeout
        );

        return frame;
    }

    /// <summary>
    /// TargetDeviceConfigからReadRandomフレームを構築（ASCII形式）
    /// </summary>
    /// <param name="config">デバイス設定</param>
    /// <returns>ReadRandomフレームの16進数文字列（大文字、スペースなし）</returns>
    /// <exception cref="ArgumentNullException">config が null の場合</exception>
    /// <exception cref="ArgumentException">デバイスリストが空、またはフレームタイプが未対応の場合</exception>
    /// <remarks>
    /// Phase2拡張: ASCII形式対応
    /// - SlmpFrameBuilder.BuildReadRandomRequestAscii() を活用
    /// - Binary形式との整合性が自動的に保証される
    /// </remarks>
    public string BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig config)
    {
        // 1. null チェック
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // 2. デバイスリスト検証
        if (config.Devices == null || config.Devices.Count == 0)
        {
            throw new ArgumentException("デバイスリストが空です", nameof(config));
        }

        // 3. フレームタイプ検証
        if (config.FrameType != "3E" && config.FrameType != "4E")
        {
            throw new ArgumentException($"未対応のフレームタイプ: {config.FrameType}", nameof(config));
        }

        // 4. DeviceEntryをDeviceSpecificationに変換してSlmpFrameBuilder.BuildReadRandomRequestAscii()に渡す
        // - Phase2拡張で実装済みのメソッドを活用
        // - 内部でReadRandom(0x0403)フレームを構築し、ASCII変換
        var deviceSpecifications = config.Devices
            .Select(d => d.ToDeviceSpecification())
            .ToList();

        string asciiFrame = SlmpFrameBuilder.BuildReadRandomRequestAscii(
            deviceSpecifications,
            config.FrameType,
            config.Timeout
        );

        return asciiFrame;
    }

    /// <summary>
    /// PlcConfigurationからReadRandomフレームを構築（ASCII形式、Excel読み込み用）
    /// </summary>
    /// <param name="config">PLC設定</param>
    /// <returns>ReadRandomフレームの16進数文字列（大文字、スペースなし）</returns>
    /// <exception cref="ArgumentNullException">config が null の場合</exception>
    /// <exception cref="ArgumentException">デバイスリストが空の場合</exception>
    public string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (config.Devices == null || config.Devices.Count == 0)
            throw new ArgumentException("デバイスリストが空です", nameof(config));

        // PlcConfiguration.Devices は既に DeviceSpecification型のリスト
        // そのままSlmpFrameBuilderに渡せる
        string asciiFrame = SlmpFrameBuilder.BuildReadRandomRequestAscii(
            config.Devices,
            frameType: config.FrameVersion,
            timeout: ConvertTimeoutMsToSlmpUnit(config.Timeout)
        );

        return asciiFrame;
    }

    /// <summary>
    /// PlcConfigurationからReadRandomフレームを構築（Binary形式、Excel読み込み用）
    /// </summary>
    /// <param name="config">PLC設定</param>
    /// <returns>ReadRandomフレームのバイト配列</returns>
    /// <exception cref="ArgumentNullException">config が null の場合</exception>
    /// <exception cref="ArgumentException">デバイスリストが空の場合</exception>
    public byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (config.Devices == null || config.Devices.Count == 0)
            throw new ArgumentException("デバイスリストが空です", nameof(config));

        // PlcConfiguration.Devices は既に DeviceSpecification型のリスト
        // そのままSlmpFrameBuilderに渡せる
        byte[] frame = SlmpFrameBuilder.BuildReadRandomRequest(
            config.Devices,
            frameType: config.FrameVersion,
            timeout: ConvertTimeoutMsToSlmpUnit(config.Timeout)
        );

        return frame;
    }
}
