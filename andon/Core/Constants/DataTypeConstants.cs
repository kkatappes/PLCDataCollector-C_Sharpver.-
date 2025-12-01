namespace Andon.Core.Constants;

/// <summary>
/// データ型関連定数
/// ParseRawToStructuredDataメソッドで使用されるデータ型定数
/// </summary>
public static class DataTypeConstants
{
    #region サポート対応データ型

    /// <summary>
    /// 16ビット符号付き整数
    /// </summary>
    public const string Int16 = "Int16";

    /// <summary>
    /// 32ビット符号付き整数
    /// </summary>
    public const string Int32 = "Int32";

    /// <summary>
    /// 16ビット符号なし整数
    /// </summary>
    public const string UInt16 = "UInt16";

    /// <summary>
    /// 32ビット符号なし整数
    /// </summary>
    public const string UInt32 = "UInt32";

    /// <summary>
    /// ブール値
    /// </summary>
    public const string Boolean = "Boolean";

    /// <summary>
    /// 文字列
    /// </summary>
    public const string String = "String";

    /// <summary>
    /// ワード型（16ビット）
    /// </summary>
    public const string Word = "Word";

    /// <summary>
    /// ビット型
    /// </summary>
    public const string Bit = "Bit";

    #endregion

    #region データ型セット

    /// <summary>
    /// サポートされている全データ型のセット
    /// </summary>
    public static readonly HashSet<string> SupportedDataTypes = new()
    {
        Int16, Int32, UInt16, UInt32, Boolean, String, Word, Bit
    };

    /// <summary>
    /// 数値型データ型のセット
    /// </summary>
    public static readonly HashSet<string> NumericDataTypes = new()
    {
        Int16, Int32, UInt16, UInt32, Word
    };

    /// <summary>
    /// 整数型データ型のセット
    /// </summary>
    public static readonly HashSet<string> IntegerDataTypes = new()
    {
        Int16, Int32, UInt16, UInt32
    };

    #endregion

    #region データ型検証

    /// <summary>
    /// サポートされているデータ型かどうかを判定
    /// </summary>
    /// <param name="dataType">判定するデータ型</param>
    /// <returns>サポート済みの場合true</returns>
    public static bool IsSupported(string dataType)
    {
        return SupportedDataTypes.Contains(dataType);
    }

    /// <summary>
    /// 数値型データ型かどうかを判定
    /// </summary>
    /// <param name="dataType">判定するデータ型</param>
    /// <returns>数値型の場合true</returns>
    public static bool IsNumeric(string dataType)
    {
        return NumericDataTypes.Contains(dataType);
    }

    #endregion
}