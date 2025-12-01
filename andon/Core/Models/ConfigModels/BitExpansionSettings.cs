using System;

namespace Andon.Core.Models.ConfigModels
{
    /// <summary>
    /// ビット展開設定（ConMoni互換）
    ///
    /// ★Phase 2実装完了項目★
    /// - Enabled, SelectionMask, ConversionFactors
    /// - Validate()メソッド
    ///
    /// ★将来の実装計画（Phase 4以降）★
    /// - Excel設定ファイルからの読み込み機能
    /// - Excel監視・自動リロード機能
    /// - Excel → JSON変換ツール
    ///
    /// Excelフォーマット想定:
    /// | DeviceNo | DeviceName | DataType | BitExpand | ConversionFactor |
    /// |----------|------------|----------|-----------|------------------|
    /// | 0        | DATETIME   | Word     | FALSE     | 1.0              |
    /// | 10       | シャッター  | Bit      | TRUE      | 1.0              |
    ///
    /// TODO (Phase 4):
    /// - EPPlus or ClosedXMLライブラリの導入
    /// - LoadFromExcel()メソッドの実装
    /// - Excel変更監視機能の実装
    /// </summary>
    public class BitExpansionSettings
    {
        /// <summary>
        /// ビット展開機能の有効/無効
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// デバイスごとのビット展開フラグ
        /// true: 16ビット展開、false: ワード値のまま
        /// ConMoniの accessBitDataLoc 互換
        /// </summary>
        public bool[] SelectionMask { get; set; } = Array.Empty<bool>();

        /// <summary>
        /// 変換係数配列（ConMoniの digitControl 互換）
        /// 各デバイス値に乗算される係数（デフォルト: 1.0）
        /// 例: [1.0, 0.1, 10.0] → 温度センサー0.1倍、圧力センサー10倍等
        /// </summary>
        public double[] ConversionFactors { get; set; } = Array.Empty<double>();

        /// <summary>
        /// 設定の妥当性検証
        /// </summary>
        /// <exception cref="InvalidOperationException">設定が無効な場合</exception>
        public void Validate()
        {
            if (!Enabled)
                return;

            if (SelectionMask.Length == 0)
            {
                throw new InvalidOperationException(
                    "BitExpansion is enabled but SelectionMask is empty");
            }

            if (ConversionFactors.Length > 0 &&
                ConversionFactors.Length != SelectionMask.Length)
            {
                throw new InvalidOperationException(
                    $"ConversionFactors length ({ConversionFactors.Length}) " +
                    $"must match SelectionMask length ({SelectionMask.Length})");
            }
        }
    }
}
