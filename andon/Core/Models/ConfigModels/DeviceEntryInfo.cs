namespace Andon.Core.Models.ConfigModels;

/// <summary>
/// デバイス設定情報（JSON出力用）
/// </summary>
public class DeviceEntryInfo
{
    /// <summary>
    /// センサー名・用途説明（設定ファイルのDescriptionフィールド）
    /// 例: "運転状態フラグ開始", "生産数カウンタ", "エラーカウンタ"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// データ桁数（将来の拡張用、現在は常に1）
    /// </summary>
    public int Digits { get; set; } = 1;
}
