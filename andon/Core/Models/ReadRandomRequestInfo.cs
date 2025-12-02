namespace Andon.Core.Models;

/// <summary>
/// ReadRandom(0x0403)コマンド用リクエスト情報（Phase12恒久対策）
/// ReadRandom(0x0403)コマンドは複数デバイス型の混在、不連続アドレス指定が可能
/// </summary>
/// <remarks>
/// <para><b>Phase12での設計目的</b>:</para>
/// <list type="bullet">
///   <item><description>ReadRandom(0x0403)専用のリクエスト情報クラス</description></item>
///   <item><description>複数デバイス型混在対応（D, M, X混合可能）</description></item>
///   <item><description>不連続アドレス指定対応</description></item>
///   <item><description>ProcessedDeviceRequestInfoから分離して責務を明確化</description></item>
/// </list>
///
/// <para><b>ProcessedDeviceRequestInfoとの違い</b>:</para>
/// <list type="bullet">
///   <item><description>ProcessedDeviceRequestInfo: テスト用途専用（TC029/TC037）</description></item>
///   <item><description>ReadRandomRequestInfo: 本番実装用ReadRandom(0x0403)専用</description></item>
/// </list>
///
/// <para><b>使用例</b>:</para>
/// <code>
/// var requestInfo = new ReadRandomRequestInfo
/// {
///     DeviceSpecifications = new List&lt;DeviceSpecification&gt;
///     {
///         new DeviceSpecification(DeviceCode.D, 100),
///         new DeviceSpecification(DeviceCode.M, 200),
///         new DeviceSpecification(DeviceCode.X, 0)
///     },
///     FrameType = FrameType.Frame4E,
///     RequestedAt = DateTime.UtcNow
/// };
/// </code>
/// </remarks>
public class ReadRandomRequestInfo
{
    /// <summary>
    /// 読み出し対象デバイス仕様リスト
    /// </summary>
    /// <remarks>
    /// <para>デフォルトで空リストとして初期化されます。</para>
    /// <para>ReadRandom(0x0403)コマンドでは、複数のデバイス型を混在させて指定可能です。</para>
    /// <para>例: [D100, M200, X0] のように異なるデバイス型を同時に指定できます。</para>
    /// </remarks>
    public List<DeviceSpecification> DeviceSpecifications { get; set; } = new();

    /// <summary>
    /// フレーム型（3E/4E）
    /// </summary>
    /// <remarks>
    /// <para>SLMP通信のフレームバージョンを指定します。</para>
    /// <list type="bullet">
    ///   <item><description>FrameType.Frame3E: 3Eフレーム（標準）</description></item>
    ///   <item><description>FrameType.Frame4E: 4Eフレーム（シーケンス番号付き）</description></item>
    /// </list>
    /// </remarks>
    public FrameType FrameType { get; set; }

    /// <summary>
    /// 要求日時（UTC）
    /// </summary>
    /// <remarks>
    /// <para>リクエスト生成時刻を記録します。</para>
    /// <para>通常は <see cref="DateTime.UtcNow"/> を使用します。</para>
    /// <para>レスポンス処理時に遅延時間の計算等に使用されます。</para>
    /// </remarks>
    public DateTime RequestedAt { get; set; }
}
