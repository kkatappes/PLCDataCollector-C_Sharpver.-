using Andon.Core.Constants;

namespace Andon.Core.Models;

/// <summary>
/// 解析設定情報
/// 3Eフレーム構造化処理の設定
/// </summary>
public class ParseConfiguration
{
    /// <summary>
    /// フレーム形式（3E）
    /// </summary>
    public string FrameFormat { get; set; } = string.Empty;

    /// <summary>
    /// データ形式（Binary/ASCII）
    /// </summary>
    public string DataFormat { get; set; } = string.Empty;

    /// <summary>
    /// 構造定義一覧
    /// </summary>
    public List<StructureDefinition> StructureDefinitions { get; set; } = new();

    /// <summary>
    /// エラー処理方針
    /// </summary>
    public string ErrorHandlingPolicy { get; set; } = "Continue";

    /// <summary>
    /// ログレベル
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// ヘッダーサイズ（4Eフレーム解析対応）
    /// 4Eフレーム: 13バイト, 3Eフレーム: 15バイト
    /// </summary>
    public int HeaderSize { get; set; } = SlmpConstants.Frame3EHeaderSize; // デフォルト: 3Eフレーム
}