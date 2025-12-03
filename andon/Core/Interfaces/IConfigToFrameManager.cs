using Andon.Core.Models.ConfigModels;

namespace Andon.Core.Interfaces;

/// <summary>
/// 設定読み込み・フレーム構築インターフェース（Excel設定ベース）
/// </summary>
public interface IConfigToFrameManager
{
    /// <summary>
    /// PlcConfigurationからReadRandomフレームを構築（Binary形式）
    /// </summary>
    byte[] BuildReadRandomFrameFromConfig(PlcConfiguration config);

    /// <summary>
    /// PlcConfigurationからReadRandomフレームを構築（ASCII形式）
    /// </summary>
    string BuildReadRandomFrameFromConfigAscii(PlcConfiguration config);
}
