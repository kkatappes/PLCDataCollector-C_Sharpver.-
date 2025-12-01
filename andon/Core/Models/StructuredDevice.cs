namespace Andon.Core.Models;

/// <summary>
/// 構造化デバイス情報
/// 3Eフレーム解析により構造化されたデバイスデータ
/// </summary>
public class StructuredDevice
{
    /// <summary>
    /// デバイス名（例：ProductionData）
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 構造体種別
    /// </summary>
    public string StructureType { get; set; } = string.Empty;

    /// <summary>
    /// フィールド値ディクショナリ
    /// </summary>
    public Dictionary<string, object> Fields { get; set; } = new();

    /// <summary>
    /// 解析時刻
    /// </summary>
    public DateTime ParsedTimestamp { get; set; }

    /// <summary>
    /// ソースフレーム種別（3E）
    /// </summary>
    public string SourceFrameType { get; set; } = string.Empty;

    /// <summary>
    /// フィールド名一覧
    /// </summary>
    public List<string> FieldNames { get; set; } = new();

    /// <summary>
    /// 型安全なフィールド値取得
    /// </summary>
    /// <typeparam name="T">取得する型</typeparam>
    /// <param name="fieldName">フィールド名</param>
    /// <returns>フィールド値</returns>
    public T GetField<T>(string fieldName)
    {
        if (Fields.TryGetValue(fieldName, out var value))
        {
            return (T)value;
        }
        return default(T)!;
    }

    /// <summary>
    /// フィールド値設定
    /// </summary>
    /// <param name="fieldName">フィールド名</param>
    /// <param name="value">設定値</param>
    public void SetField(string fieldName, object value)
    {
        Fields[fieldName] = value;
        if (!FieldNames.Contains(fieldName))
        {
            FieldNames.Add(fieldName);
        }
    }

    /// <summary>
    /// フィールド存在確認
    /// </summary>
    /// <param name="fieldName">フィールド名</param>
    /// <returns>存在する場合true</returns>
    public bool HasField(string fieldName)
    {
        return Fields.ContainsKey(fieldName);
    }
}