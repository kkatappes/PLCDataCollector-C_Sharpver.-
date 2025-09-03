using System;
using System.Collections.Generic;

namespace SlmpClient.Constants
{
    /// <summary>
    /// SLMP PDU (Protocol Data Unit) 定義
    /// Python: const.PDU
    /// </summary>
    public enum Pdu : byte
    {
        #region STフレーム PDU (3Eフレーム)
        
        /// <summary>読み取り要求 ST</summary>
        RdReqST = 1,
        
        /// <summary>書き込み要求 ST</summary>
        WrReqST = 2,
        
        /// <summary>読み取り応答 ST</summary>
        RdResST = 3,
        
        /// <summary>書き込み応答 ST</summary>
        WrResST = 4,
        
        /// <summary>読み取りエラー応答 ST</summary>
        RdErrST = 5,
        
        /// <summary>書き込みエラー応答 ST</summary>
        WrErrST = 6,
        
        /// <summary>オンデマンド要求 ST</summary>
        OdReqST = 7,
        
        #endregion
        
        #region MTフレーム PDU (4Eフレーム)
        
        /// <summary>読み取り要求 MT</summary>
        RdReqMT = 8,
        
        /// <summary>書き込み要求 MT</summary>
        WrReqMT = 9,
        
        /// <summary>読み取り応答 MT</summary>
        RdResMT = 10,
        
        /// <summary>書き込み応答 MT</summary>
        WrResMT = 11,
        
        /// <summary>読み取りエラー応答 MT</summary>
        RdErrMT = 12,
        
        /// <summary>書き込みエラー応答 MT</summary>
        WrErrMT = 13,
        
        /// <summary>オンデマンド要求 MT</summary>
        OdReqMT = 14,
        
        #endregion
        
        #region EMTフレーム PDU (拡張メッセージ)
        
        /// <summary>要求 EMT</summary>
        ReqEMT = 15,
        
        /// <summary>応答 EMT</summary>
        ResEMT = 16,
        
        /// <summary>プッシュ EMT</summary>
        PushEMT = 17,
        
        #endregion
        
        #region LMTフレーム PDU (ローカルメッセージ)
        
        /// <summary>要求 LMT</summary>
        ReqLMT = 18,
        
        /// <summary>応答 LMT</summary>
        ResLMT = 19,
        
        /// <summary>エラー LMT</summary>
        ErrLMT = 20,
        
        #endregion
    }
    
    /// <summary>
    /// SLMPフレームバージョン定義
    /// </summary>
    public enum SlmpFrameVersion : byte
    {
        /// <summary>3Eフレーム (ST型)</summary>
        Version3E = 3,
        
        /// <summary>4Eフレーム (MT型)</summary>
        Version4E = 4
    }
    
    /// <summary>
    /// PDU拡張メソッド
    /// Python: const.py の ST_PDU, MT_PDU, EMT_PDU, LMT_PDU 相当
    /// </summary>
    public static class PduExtensions
    {
        /// <summary>
        /// STフレーム (3Eフレーム) PDU一覧
        /// Python: ST_PDU
        /// </summary>
        private static readonly HashSet<Pdu> StPduTypes = new()
        {
            Pdu.RdReqST, Pdu.WrReqST, Pdu.RdResST, Pdu.WrResST,
            Pdu.RdErrST, Pdu.WrErrST, Pdu.OdReqST
        };
        
        /// <summary>
        /// MTフレーム (4Eフレーム) PDU一覧
        /// Python: MT_PDU
        /// </summary>
        private static readonly HashSet<Pdu> MtPduTypes = new()
        {
            Pdu.RdReqMT, Pdu.WrReqMT, Pdu.RdResMT, Pdu.WrResMT,
            Pdu.RdErrMT, Pdu.WrErrMT, Pdu.OdReqMT
        };
        
        /// <summary>
        /// EMTフレーム (拡張メッセージ) PDU一覧
        /// Python: EMT_PDU
        /// </summary>
        private static readonly HashSet<Pdu> EmtPduTypes = new()
        {
            Pdu.ReqEMT, Pdu.ResEMT, Pdu.PushEMT
        };
        
        /// <summary>
        /// LMTフレーム (ローカルメッセージ) PDU一覧
        /// Python: LMT_PDU
        /// </summary>
        private static readonly HashSet<Pdu> LmtPduTypes = new()
        {
            Pdu.ReqLMT, Pdu.ResLMT, Pdu.ErrLMT
        };
        
        /// <summary>
        /// 要求PDU一覧
        /// </summary>
        private static readonly HashSet<Pdu> RequestPduTypes = new()
        {
            Pdu.RdReqST, Pdu.WrReqST, Pdu.OdReqST,
            Pdu.RdReqMT, Pdu.WrReqMT, Pdu.OdReqMT,
            Pdu.ReqEMT, Pdu.ReqLMT
        };
        
        /// <summary>
        /// 応答PDU一覧
        /// </summary>
        private static readonly HashSet<Pdu> ResponsePduTypes = new()
        {
            Pdu.RdResST, Pdu.WrResST,
            Pdu.RdResMT, Pdu.WrResMT,
            Pdu.ResEMT, Pdu.ResLMT
        };
        
        /// <summary>
        /// エラー応答PDU一覧
        /// </summary>
        private static readonly HashSet<Pdu> ErrorPduTypes = new()
        {
            Pdu.RdErrST, Pdu.WrErrST,
            Pdu.RdErrMT, Pdu.WrErrMT,
            Pdu.ErrLMT
        };
        
        /// <summary>
        /// PDUがSTフレーム (3Eフレーム) 用かどうかを判定
        /// Python: pdu in const.ST_PDU
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>STフレーム用の場合はtrue</returns>
        public static bool IsStFramePdu(this Pdu pdu)
        {
            return StPduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUがMTフレーム (4Eフレーム) 用かどうかを判定
        /// Python: pdu in const.MT_PDU
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>MTフレーム用の場合はtrue</returns>
        public static bool IsMtFramePdu(this Pdu pdu)
        {
            return MtPduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUがEMTフレーム (拡張メッセージ) 用かどうかを判定
        /// Python: pdu in const.EMT_PDU
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>EMTフレーム用の場合はtrue</returns>
        public static bool IsEmtFramePdu(this Pdu pdu)
        {
            return EmtPduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUがLMTフレーム (ローカルメッセージ) 用かどうかを判定
        /// Python: pdu in const.LMT_PDU
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>LMTフレーム用の場合はtrue</returns>
        public static bool IsLmtFramePdu(this Pdu pdu)
        {
            return LmtPduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUが要求タイプかどうかを判定
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>要求タイプの場合はtrue</returns>
        public static bool IsRequestPdu(this Pdu pdu)
        {
            return RequestPduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUが応答タイプかどうかを判定
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>応答タイプの場合はtrue</returns>
        public static bool IsResponsePdu(this Pdu pdu)
        {
            return ResponsePduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUがエラー応答タイプかどうかを判定
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>エラー応答タイプの場合はtrue</returns>
        public static bool IsErrorPdu(this Pdu pdu)
        {
            return ErrorPduTypes.Contains(pdu);
        }
        
        /// <summary>
        /// PDUから対応するフレームバージョンを取得
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>対応するフレームバージョン</returns>
        public static SlmpFrameVersion GetFrameVersion(this Pdu pdu)
        {
            return pdu.IsStFramePdu() ? SlmpFrameVersion.Version3E : SlmpFrameVersion.Version4E;
        }
        
        /// <summary>
        /// PDUから説明文字列を取得
        /// </summary>
        /// <param name="pdu">PDU</param>
        /// <returns>PDUの説明</returns>
        public static string GetDescription(this Pdu pdu)
        {
            return pdu switch
            {
                Pdu.RdReqST => "読み取り要求 (3E)",
                Pdu.WrReqST => "書き込み要求 (3E)",
                Pdu.RdResST => "読み取り応答 (3E)",
                Pdu.WrResST => "書き込み応答 (3E)",
                Pdu.RdErrST => "読み取りエラー応答 (3E)",
                Pdu.WrErrST => "書き込みエラー応答 (3E)",
                Pdu.OdReqST => "オンデマンド要求 (3E)",
                Pdu.RdReqMT => "読み取り要求 (4E)",
                Pdu.WrReqMT => "書き込み要求 (4E)",
                Pdu.RdResMT => "読み取り応答 (4E)",
                Pdu.WrResMT => "書き込み応答 (4E)",
                Pdu.RdErrMT => "読み取りエラー応答 (4E)",
                Pdu.WrErrMT => "書き込みエラー応答 (4E)",
                Pdu.OdReqMT => "オンデマンド要求 (4E)",
                Pdu.ReqEMT => "要求 (EMT)",
                Pdu.ResEMT => "応答 (EMT)",
                Pdu.PushEMT => "プッシュ (EMT)",
                Pdu.ReqLMT => "要求 (LMT)",
                Pdu.ResLMT => "応答 (LMT)",
                Pdu.ErrLMT => "エラー (LMT)",
                _ => "不明PDU"
            };
        }
    }
}