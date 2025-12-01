using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// 構造定義情報
/// 構造化データのフィールド定義
/// </summary>
public class StructureDefinition
{
    /// <summary>
    /// 構造体名（例：ProductionData）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 構造体の説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// フィールド定義一覧
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = new();

    /// <summary>
    /// 構造体バージョン
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 作成日時
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// フレームタイプ（4Eフレーム解析対応）
    /// 対応するSLMPフレーム形式（"3E" or "4E"）
    /// </summary>
    public string FrameType { get; set; } = SlmpConstants.DefaultFrameType; // デフォルト: 3Eフレーム
}