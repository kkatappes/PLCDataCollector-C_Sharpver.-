using Andon.Core.Interfaces;
using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Utilities;

namespace Andon.Core.Managers;

/// <summary>
/// Step1-2: 設定読み込み・フレーム構築（Excel設定ベース）
/// PlcConfigurationモデルを使用した統一設計
/// ⚠️ 注意: PlcConnectionConfig用メソッドは削除済み（JSON設定廃止により不要）
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
