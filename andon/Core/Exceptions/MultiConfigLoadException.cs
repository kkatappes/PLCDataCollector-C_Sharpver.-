namespace Andon.Core.Exceptions;

/// <summary>
/// 複数設定ファイル専用例外（カスタム）
/// </summary>
public class MultiConfigLoadException : Exception
{
    public MultiConfigLoadException() { }
    public MultiConfigLoadException(string message) : base(message) { }
    public MultiConfigLoadException(string message, Exception inner) : base(message, inner) { }
}
