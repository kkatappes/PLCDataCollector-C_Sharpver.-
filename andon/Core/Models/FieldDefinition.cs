namespace Andon.Core.Models;

/// <summary>
/// フィールド定義情報
/// 構造化データの個別フィールド定義
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// フィールド名（例：ProductId）
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// デバイスアドレス（例：100、または"D100_32bit"）
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// データ型（Int16/Int32/UInt16/UInt32/Boolean/String）
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// フィールドの説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 必須フィールドかどうか
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// デフォルト値
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// バリデーションルール
    /// </summary>
    public string? ValidationRule { get; set; }
}