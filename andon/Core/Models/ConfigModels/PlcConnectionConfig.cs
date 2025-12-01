using Andon.Core.Constants;

namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// 個別PLC接続設定（Phase6-2: PlcId, PlcName, Priority追加）
/// </summary>
public class PlcConnectionConfig
{
    // 既存プロパティ
    public string IPAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 8192;
    public string ConnectionMethod { get; set; } = "UDP";
    public string FrameVersion { get; set; } = "3E";
    public int Timeout { get; set; } = 8000;
    public bool IsBinary { get; set; } = DefaultValues.IsBinary;
    public List<DeviceEntry> Devices { get; set; } = new();

    // ✅ Phase6-2新規追加: PLC識別用
    /// <summary>
    /// PLC識別ID（DB保存用キー、ログ出力用）
    /// </summary>
    public string PlcId { get; set; } = "PLC_001";

    /// <summary>
    /// PLC名称（人間可読）
    /// </summary>
    public string PlcName { get; set; } = "ライン1_設備A";

    /// <summary>
    /// 優先度（並列処理時のタスク優先度、1-10）
    /// </summary>
    public int Priority { get; set; } = 5;
}
