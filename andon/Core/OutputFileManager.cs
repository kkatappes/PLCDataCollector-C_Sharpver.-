using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SlmpClient.Core
{
    /// <summary>
    /// 出力ファイル管理クラス
    /// ファイル作成保証機能、権限チェック、フォールバック処理を提供
    /// Phase 1実装: 出力ファイル統一対応
    /// </summary>
    public class OutputFileManager
    {
        private readonly ILogger<OutputFileManager> _logger;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガー</param>
        public OutputFileManager(ILogger<OutputFileManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 出力ディレクトリの存在確認と作成保証
        /// 書き込み権限チェック付き
        /// </summary>
        /// <param name="filePath">対象ファイルパス</param>
        /// <returns>確認済みファイルパス</returns>
        /// <exception cref="IOException">ディレクトリ作成や権限チェックに失敗した場合</exception>
        public async Task<string> EnsureOutputDirectoryAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("ファイルパスが指定されていません", nameof(filePath));

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    directory = "."; // 現在のディレクトリを使用
                }

                // ディレクトリが存在しない場合は作成
                if (!Directory.Exists(directory))
                {
                    _logger.LogInformation("出力ディレクトリを作成中: {Directory}", directory);
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("出力ディレクトリ作成完了: {Directory}", directory);
                }

                // 書き込み権限チェック
                await ValidateWritePermissionAsync(directory);

                _logger.LogDebug("出力ファイルパス確認完了: {FilePath}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "出力ディレクトリの確保に失敗: {FilePath}", filePath);
                throw new IOException($"出力ディレクトリの確保に失敗しました: {filePath}", ex);
            }
        }

        /// <summary>
        /// 書き込み権限の検証
        /// </summary>
        /// <param name="directory">検証対象ディレクトリ</param>
        /// <returns>非同期タスク</returns>
        private async Task ValidateWritePermissionAsync(string directory)
        {
            var testFile = Path.Combine(directory, $"_permission_test_{Guid.NewGuid()}.tmp");

            try
            {
                _logger.LogDebug("書き込み権限チェック開始: {Directory}", directory);
                await File.WriteAllTextAsync(testFile, "permission_test");

                // ファイルが実際に作成されたかチェック
                if (!File.Exists(testFile))
                {
                    throw new IOException("テストファイルの作成に失敗しました");
                }

                _logger.LogDebug("書き込み権限チェック成功: {Directory}", directory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "書き込み権限チェック失敗: {Directory}", directory);
                throw new IOException($"ディレクトリ '{directory}' への書き込み権限がありません", ex);
            }
            finally
            {
                // テストファイルをクリーンアップ
                try
                {
                    if (File.Exists(testFile))
                    {
                        File.Delete(testFile);
                        _logger.LogDebug("テストファイル削除完了: {TestFile}", testFile);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "テストファイルの削除に失敗: {TestFile}", testFile);
                    // クリーンアップ失敗は権限チェックの結果に影響しない
                }
            }
        }

        /// <summary>
        /// ファイル作成保証機能（フォールバック付き）
        /// ファイルが存在しない場合は空ファイルを作成
        /// </summary>
        /// <param name="filePath">対象ファイルパス</param>
        /// <param name="createIfNotExists">ファイルが存在しない場合に作成するか</param>
        /// <returns>確認済みファイルパス</returns>
        public async Task<string> EnsureFileExistsAsync(string filePath, bool createIfNotExists = true)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("ファイルパスが指定されていません", nameof(filePath));

            try
            {
                // ディレクトリの確保
                await EnsureOutputDirectoryAsync(filePath);

                // ファイルが存在しない場合は作成
                if (!File.Exists(filePath) && createIfNotExists)
                {
                    _logger.LogInformation("出力ファイルを作成中: {FilePath}", filePath);
                    await File.WriteAllTextAsync(filePath, string.Empty);
                    _logger.LogInformation("出力ファイル作成完了: {FilePath}", filePath);
                }

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ファイル作成保証処理に失敗: {FilePath}", filePath);
                throw new IOException($"ファイル作成保証処理に失敗しました: {filePath}", ex);
            }
        }

        /// <summary>
        /// フォールバック用の代替ファイルパス生成
        /// 権限エラーなどで本来のパスが使用できない場合の代替パス
        /// </summary>
        /// <param name="originalFilePath">元のファイルパス</param>
        /// <returns>代替ファイルパス</returns>
        public string GenerateFallbackPath(string originalFilePath)
        {
            if (string.IsNullOrWhiteSpace(originalFilePath))
                return Path.Combine(Path.GetTempPath(), "andon_fallback.log");

            try
            {
                var fileName = Path.GetFileName(originalFilePath);
                var extension = Path.GetExtension(fileName);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

                var fallbackFileName = $"{nameWithoutExtension}_fallback_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var fallbackPath = Path.Combine(Path.GetTempPath(), "andon", fallbackFileName);

                _logger.LogWarning("フォールバックパス生成: {Original} -> {Fallback}", originalFilePath, fallbackPath);
                return fallbackPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "フォールバックパス生成に失敗: {OriginalPath}", originalFilePath);
                return Path.Combine(Path.GetTempPath(), $"andon_emergency_{Guid.NewGuid()}.log");
            }
        }

        /// <summary>
        /// ファイルサイズチェック
        /// ログローテーションが必要かどうかを判定
        /// </summary>
        /// <param name="filePath">チェック対象ファイル</param>
        /// <param name="maxSizeMB">最大サイズ（MB）</param>
        /// <returns>ローテーションが必要な場合true</returns>
        public bool IsRotationNeeded(string filePath, int maxSizeMB)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var fileInfo = new FileInfo(filePath);
                var maxSizeBytes = maxSizeMB * 1024 * 1024;

                return fileInfo.Length > maxSizeBytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ファイルサイズチェックに失敗: {FilePath}", filePath);
                return false; // エラー時はローテーション不要として処理継続
            }
        }
    }
}