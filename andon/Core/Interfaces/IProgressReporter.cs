namespace Andon.Core.Interfaces;

/// <summary>
/// 進捗報告インターフェース
/// </summary>
/// <summary>
/// 進捗報告インターフェース
/// IProgress<T>を実装し、リアルタイム進捗報告を提供
/// </summary>
/// <typeparam name="T">進捗情報型（ProgressInfo または string）</typeparam>
public interface IProgressReporter<in T> : IProgress<T>
{
    // IProgress<T>.Report(T value)メソッドを継承
}
