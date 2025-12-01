using Andon.Core.Models.ConfigModels;

namespace Andon.Core.Interfaces;

/// <summary>
/// 設定読み込み・フレーム構築インターフェース
/// </summary>
public interface IConfigToFrameManager
{
    /// <summary>
    /// TargetDeviceConfigからReadRandomフレームを構築
    /// </summary>
    byte[] BuildReadRandomFrameFromConfig(TargetDeviceConfig config);

    /// <summary>
    /// TargetDeviceConfigからReadRandomフレームを構築（ASCII形式）
    /// </summary>
    string BuildReadRandomFrameFromConfigAscii(TargetDeviceConfig config);

    /// <summary>
    /// PlcConfigurationからReadRandomフレームを構築（Binary形式、Excel読み込み用）
    /// </summary>
    byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config);

    /// <summary>
    /// PlcConfigurationからReadRandomフレームを構築（ASCII形式、Excel読み込み用）
    /// </summary>
    string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config);
}
