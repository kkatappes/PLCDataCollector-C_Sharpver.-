namespace Andon.Core.Managers;

/// <summary>
/// シーケンス番号管理クラス
/// PySLMPClientから採用：4Eフレーム用シーケンス番号自動管理
/// </summary>
public class SequenceNumberManager
{
    private ushort _sequenceNumber = 0;
    private readonly object _lock = new object();

    /// <summary>
    /// 次のシーケンス番号を取得します。
    /// </summary>
    /// <param name="frameType">フレームタイプ（"3E" or "4E"）</param>
    /// <returns>シーケンス番号（3Eの場合は常に0、4Eの場合は自動インクリメント）</returns>
    public ushort GetNext(string frameType)
    {
        // 3Eフレームでは常に0を返す
        if (frameType == "3E")
        {
            return 0;
        }

        // 4Eフレームでは自動インクリメント
        lock (_lock)
        {
            // PySLMPClient方式：0xFF超過時ロールオーバー
            // シーケンス番号は1バイト（0～255）の範囲で管理
            if (_sequenceNumber > 0xFF)
            {
                _sequenceNumber = 0;
            }

            ushort current = _sequenceNumber;
            _sequenceNumber++;
            return current;
        }
    }

    /// <summary>
    /// シーケンス番号をリセットします。
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _sequenceNumber = 0;
        }
    }

    /// <summary>
    /// 現在のシーケンス番号を取得します（テスト用）。
    /// </summary>
    /// <returns>現在のシーケンス番号</returns>
    public ushort GetCurrent()
    {
        lock (_lock)
        {
            return _sequenceNumber;
        }
    }
}
