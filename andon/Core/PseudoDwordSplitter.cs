using System;
using System.Collections.Generic;
using System.Linq;
using SlmpClient.Constants;

namespace SlmpClient.Core
{
    /// <summary>
    /// 擬似ダブルワード分割・結合実装
    /// 単一責任原則に基づく設計 + Phase 3: エラーハンドリング統合
    /// </summary>
    public class PseudoDwordSplitter : IPseudoDwordSplitter, IConversionStatistics, ICombinationStatistics
    {
        private readonly IDeviceAddressValidator _addressValidator;
        private readonly IDwordConverter _dwordConverter;
        private readonly ConversionOptions _options;

        // Phase 3: エラーハンドリング統合
        private readonly ContinuitySettings _continuitySettings;
        private readonly SlmpErrorStatistics? _errorStatistics;

        // 統計情報（GREEN PHASE: 最小実装）
        private int _totalConversions = 0;
        private int _totalWordsGenerated = 0;
        private readonly DateTime _startTime = DateTime.UtcNow;

        // 結合統計情報（GREEN PHASE 2.4: 最小実装）
        private int _totalCombinations = 0;
        private int _totalDwordsGenerated = 0;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public PseudoDwordSplitter()
            : this(null, null, null, null, null)
        {
        }

        /// <summary>
        /// 依存性注入対応コンストラクタ
        /// </summary>
        /// <param name="addressValidator">アドレス検証サービス</param>
        /// <param name="dwordConverter">DWord変換サービス</param>
        public PseudoDwordSplitter(
            IDeviceAddressValidator addressValidator,
            IDwordConverter dwordConverter)
            : this(addressValidator, dwordConverter, null, null, null)
        {
        }

        /// <summary>
        /// オプション対応コンストラクタ（GREEN PHASE: 新機能）
        /// </summary>
        /// <param name="addressValidator">アドレス検証サービス</param>
        /// <param name="dwordConverter">DWord変換サービス</param>
        /// <param name="options">変換オプション</param>
        public PseudoDwordSplitter(
            IDeviceAddressValidator? addressValidator,
            IDwordConverter? dwordConverter,
            ConversionOptions? options)
            : this(addressValidator, dwordConverter, options, null, null)
        {
        }

        /// <summary>
        /// Phase 3: エラーハンドリング統合対応コンストラクタ
        /// </summary>
        /// <param name="addressValidator">アドレス検証サービス</param>
        /// <param name="dwordConverter">DWord変換サービス</param>
        /// <param name="options">変換オプション</param>
        /// <param name="continuitySettings">継続動作設定</param>
        /// <param name="errorStatistics">エラー統計</param>
        public PseudoDwordSplitter(
            IDeviceAddressValidator? addressValidator,
            IDwordConverter? dwordConverter,
            ConversionOptions? options,
            ContinuitySettings? continuitySettings,
            SlmpErrorStatistics? errorStatistics)
        {
            _addressValidator = addressValidator ?? new DeviceAddressValidator();
            _dwordConverter = dwordConverter ?? new DwordConverter();
            _options = options ?? new ConversionOptions();
            _continuitySettings = continuitySettings ?? new ContinuitySettings();
            _errorStatistics = errorStatistics;
        }

        #region GREEN PHASE: 新プロパティ実装

        /// <summary>検証機能が有効かどうか</summary>
        public bool IsValidationEnabled => _options.EnableValidation;

        /// <summary>統計機能が有効かどうか</summary>
        public bool IsStatisticsEnabled => _options.EnableStatistics;

        /// <summary>総変換回数</summary>
        public int TotalConversions => _totalConversions;

        /// <summary>生成された総ワード数</summary>
        public int TotalWordsGenerated => _totalWordsGenerated;

        /// <summary>平均変換時間（最小実装）</summary>
        public TimeSpan AverageConversionTime => 
            _totalConversions > 0 
                ? TimeSpan.FromMilliseconds((DateTime.UtcNow - _startTime).TotalMilliseconds / _totalConversions)
                : TimeSpan.Zero;

        /// <summary>総結合回数</summary>
        public int TotalCombinations => _totalCombinations;

        /// <summary>生成された総DWord数</summary>
        public int TotalDwordsGenerated => _totalDwordsGenerated;

        /// <summary>平均結合時間（最小実装）</summary>
        public TimeSpan AverageCombinationTime => 
            _totalCombinations > 0 
                ? TimeSpan.FromMilliseconds((DateTime.UtcNow - _startTime).TotalMilliseconds / _totalCombinations)
                : TimeSpan.Zero;

        #endregion

        #region Phase 3: エラーハンドリング統合メソッド

        /// <summary>
        /// Phase 3: エラーハンドリング統合処理
        /// </summary>
        /// <param name="errorType">エラー種別</param>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="address">アドレス</param>
        /// <param name="exception">発生した例外</param>
        /// <param name="defaultResult">デフォルト結果</param>
        /// <returns>処理結果またはデフォルト値</returns>
        private T HandleError<T>(PseudoDwordErrorType errorType, DeviceCode deviceCode, uint address, Exception exception, T defaultResult)
        {
            // エラー統計記録
            _errorStatistics?.RecordError(errorType.ToString(), deviceCode.ToString(), address, exception, _continuitySettings);

            // エラーハンドリングモードに応じた処理
            switch (_continuitySettings.Mode)
            {
                case ErrorHandlingMode.ThrowException:
                    throw exception;

                case ErrorHandlingMode.ReturnDefaultAndContinue:
                    _errorStatistics?.RecordContinuedOperation(errorType.ToString(), deviceCode.ToString(), address, defaultResult?.ToString() ?? "null");
                    return defaultResult;

                case ErrorHandlingMode.RetryThenDefault:
                    // 簡易リトライ実装（将来拡張可能）
                    try
                    {
                        // リトライロジックは複雑になるため、現在はデフォルト値返却
                        _errorStatistics?.RecordContinuedOperation(errorType.ToString(), deviceCode.ToString(), address, $"Retry failed, using default: {defaultResult}");
                        return defaultResult;
                    }
                    catch
                    {
                        _errorStatistics?.RecordContinuedOperation(errorType.ToString(), deviceCode.ToString(), address, defaultResult?.ToString() ?? "null");
                        return defaultResult;
                    }

                default:
                    throw exception;
            }
        }

        #endregion

        /// <summary>
        /// DWordデバイスをWordペアに分割 + Phase 3: エラーハンドリング統合
        /// </summary>
        public IList<WordPair> SplitDwordToWordPairs(
            IEnumerable<(DeviceCode deviceCode, uint address, uint value)> dwordDevices)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(dwordDevices);

                var deviceList = dwordDevices.ToList();
                _errorStatistics?.RecordOperation();
                
                // GREEN PHASE: 詳細バリデーション機能 + Phase 3: エラーハンドリング統合
                if (_options.EnableValidation)
                {
                    var validationErrors = new List<ValidationError>();
                    
                    foreach (var device in deviceList)
                    {
                        try
                        {
                            _addressValidator.ValidateAddressBoundary(device.deviceCode, device.address);
                        }
                        catch (ArgumentException ex)
                        {
                            // Phase 3: 個別デバイスエラーハンドリング
                            if (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
                            {
                                // 継続モードでは個別エラーを記録して続行
                                _errorStatistics?.RecordError(
                                    PseudoDwordErrorType.AddressBoundaryError.ToString(),
                                    device.deviceCode.ToString(),
                                    device.address,
                                    ex,
                                    _continuitySettings);
                                continue;
                            }
                            
                            validationErrors.Add(new ValidationError 
                            { 
                                Message = $"Device {device.deviceCode} at address {device.address}: {ex.Message}",
                                ErrorCode = "BOUNDARY_VIOLATION"
                            });
                        }
                    }
                    
                    if (validationErrors.Count > 0)
                    {
                        var validationException = new DetailedValidationException("Multiple validation errors occurred", validationErrors);
                        
                        // Phase 3: バリデーションエラーのハンドリング
                        if (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
                        {
                            return HandleError(PseudoDwordErrorType.ValidationError, DeviceCode.D, 0, validationException, new List<WordPair>());
                        }
                        throw validationException;
                    }
                }

                var result = new List<WordPair>();
                
                foreach (var device in deviceList)
                {
                    try
                    {
                        // 基本的なアドレス境界チェック（従来通り）
                        if (!_options.EnableValidation)
                        {
                            _addressValidator.ValidateAddressBoundary(device.deviceCode, device.address);
                        }
                        
                        // DWord → WordPair変換（純粋関数パターン）
                        var wordPair = _dwordConverter.SplitDwordToWordPair(device);
                        result.Add(wordPair);
                    }
                    catch (Exception ex)
                    {
                        // Phase 3: 個別変換エラーハンドリング
                        var defaultWordPair = new WordPair
                        {
                            LowWord = (device.deviceCode, device.address, _continuitySettings.DefaultWordValue),
                            HighWord = (device.deviceCode, device.address + 1, _continuitySettings.DefaultWordValue)
                        };
                        
                        var handledResult = HandleError(PseudoDwordErrorType.SplitError, device.deviceCode, device.address, ex, defaultWordPair);
                        result.Add(handledResult);
                    }
                }

                // GREEN PHASE: 統計情報記録
                if (_options.EnableStatistics)
                {
                    _totalConversions += deviceList.Count;
                    _totalWordsGenerated += result.Count * 2; // 各WordPairは2つのWordを生成
                }

                return result;
            }
            catch (Exception ex) when (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
            {
                // Phase 3: 全体的なエラーハンドリング
                return HandleError(PseudoDwordErrorType.SplitError, DeviceCode.D, 0, ex, new List<WordPair>());
            }
        }

        /// <summary>
        /// WordペアをDWordデバイスに結合 + Phase 3: エラーハンドリング統合
        /// </summary>
        public IList<(DeviceCode deviceCode, uint address, uint value)> CombineWordPairsToDword(
            IEnumerable<WordPair> wordPairs)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(wordPairs, nameof(wordPairs));

                var wordPairList = wordPairs.ToList();
                _errorStatistics?.RecordOperation();

                // GREEN PHASE 2.4: 詳細バリデーション機能 + Phase 3: エラーハンドリング統合
                if (_options.EnableValidation)
                {
                    var validationErrors = new List<ValidationError>();

                    foreach (var wordPair in wordPairList)
                    {
                        try
                        {
                            // アドレス整合性チェック
                            if (wordPair.HighWord.address != wordPair.LowWord.address + 1)
                            {
                                var addressError = new ValidationError
                                {
                                    Message = $"Address mismatch - LowWord address: {wordPair.LowWord.address}, " +
                                             $"HighWord address: {wordPair.HighWord.address}, expected {wordPair.LowWord.address + 1}",
                                    ErrorCode = "ADDRESS_MISMATCH"
                                };

                                if (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
                                {
                                    _errorStatistics?.RecordError(
                                        PseudoDwordErrorType.ValidationError.ToString(),
                                        wordPair.LowWord.deviceCode.ToString(),
                                        wordPair.LowWord.address,
                                        new ArgumentException(addressError.Message),
                                        _continuitySettings);
                                    continue;
                                }
                                validationErrors.Add(addressError);
                            }

                            // デバイスコード整合性チェック
                            if (wordPair.LowWord.deviceCode != wordPair.HighWord.deviceCode)
                            {
                                var deviceError = new ValidationError
                                {
                                    Message = $"Device code mismatch - different device codes: " +
                                             $"{wordPair.LowWord.deviceCode} vs {wordPair.HighWord.deviceCode}",
                                    ErrorCode = "DEVICE_CODE_MISMATCH"
                                };

                                if (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
                                {
                                    _errorStatistics?.RecordError(
                                        PseudoDwordErrorType.ValidationError.ToString(),
                                        wordPair.LowWord.deviceCode.ToString(),
                                        wordPair.LowWord.address,
                                        new ArgumentException(deviceError.Message),
                                        _continuitySettings);
                                    continue;
                                }
                                validationErrors.Add(deviceError);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
                            {
                                _errorStatistics?.RecordError(
                                    PseudoDwordErrorType.ValidationError.ToString(),
                                    wordPair.LowWord.deviceCode.ToString(),
                                    wordPair.LowWord.address,
                                    ex,
                                    _continuitySettings);
                                continue;
                            }
                            throw;
                        }
                    }

                    if (validationErrors.Count > 0)
                    {
                        var validationException = new DetailedValidationException("Multiple validation errors occurred", validationErrors);
                        
                        // Phase 3: バリデーションエラーのハンドリング
                        if (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
                        {
                            return HandleError(PseudoDwordErrorType.ValidationError, DeviceCode.D, 0, validationException, new List<(DeviceCode, uint, uint)>());
                        }
                        throw validationException;
                    }
                }

                var result = new List<(DeviceCode deviceCode, uint address, uint value)>();

                foreach (var pair in wordPairList)
                {
                    try
                    {
                        var combinedResult = _dwordConverter.CombineWordPairToDword(pair);
                        result.Add(combinedResult);
                    }
                    catch (Exception ex)
                    {
                        // Phase 3: 個別結合エラーハンドリング
                        var defaultResult = (pair.LowWord.deviceCode, pair.LowWord.address, (uint)0);
                        var handledResult = HandleError(PseudoDwordErrorType.CombineError, pair.LowWord.deviceCode, pair.LowWord.address, ex, defaultResult);
                        result.Add(handledResult);
                    }
                }

                // GREEN PHASE 2.4: 結合統計情報記録
                if (_options.EnableStatistics)
                {
                    _totalCombinations += wordPairList.Count;
                    _totalDwordsGenerated += result.Count;
                }

                return result;
            }
            catch (Exception ex) when (_continuitySettings.Mode != ErrorHandlingMode.ThrowException)
            {
                // Phase 3: 全体的なエラーハンドリング
                return HandleError(PseudoDwordErrorType.CombineError, DeviceCode.D, 0, ex, new List<(DeviceCode, uint, uint)>());
            }
        }
    }

    #region 依存性注入対応サービス

    /// <summary>
    /// デバイスアドレス検証インターフェース
    /// </summary>
    public interface IDeviceAddressValidator
    {
        /// <summary>
        /// アドレス境界検証
        /// </summary>
        /// <param name="deviceCode">デバイスコード</param>
        /// <param name="address">開始アドレス</param>
        /// <exception cref="ArgumentException">境界違反時</exception>
        void ValidateAddressBoundary(DeviceCode deviceCode, uint address);
    }

    /// <summary>
    /// DWord変換サービスインターフェース
    /// </summary>
    public interface IDwordConverter
    {
        /// <summary>
        /// DWordをWordペアに分割（純粋関数）
        /// </summary>
        WordPair SplitDwordToWordPair((DeviceCode deviceCode, uint address, uint value) dwordDevice);

        /// <summary>
        /// WordペアをDWordに結合（純粋関数）
        /// </summary>
        (DeviceCode deviceCode, uint address, uint value) CombineWordPairToDword(WordPair wordPair);
    }

    /// <summary>
    /// デバイスアドレス検証実装
    /// </summary>
    public class DeviceAddressValidator : IDeviceAddressValidator
    {
        private const uint MAX_ADDRESS_BOUNDARY = 65535;

        /// <summary>
        /// アドレス境界検証（純粋関数パターン）
        /// </summary>
        public void ValidateAddressBoundary(DeviceCode deviceCode, uint address)
        {
            // 基本的な境界チェック（分割時に+1アドレスが必要）
            if (address >= MAX_ADDRESS_BOUNDARY)
            {
                throw new ArgumentException(
                    $"Address exceeds boundary limit - address boundary violation. " +
                    $"Device: {deviceCode}, Address: {address}, MaxBoundary: {MAX_ADDRESS_BOUNDARY - 1}");
            }

            // 4バイトアドレス必須デバイスの場合の追加検証
            if (deviceCode.Is4ByteAddress())
            {
                // 4バイトアドレスデバイスの場合は範囲がより広い可能性があるが
                // 安全のため同じ制限を適用
                ValidateSpecialDeviceConstraints(deviceCode, address);
            }
        }

        /// <summary>
        /// 特殊デバイス制約検証
        /// </summary>
        private static void ValidateSpecialDeviceConstraints(DeviceCode deviceCode, uint address)
        {
            // 拡張データレジスタ(RD)などの特殊制約があれば追加
            switch (deviceCode)
            {
                case DeviceCode.RD:
                    // 拡張データレジスタの場合の特殊制約
                    if (address > 1048575) // 2^20 - 1
                    {
                        throw new ArgumentException(
                            $"Extended data register address out of range: {address}");
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// DWord変換サービス実装（純粋関数パターン）
    /// </summary>
    public class DwordConverter : IDwordConverter
    {
        /// <summary>
        /// DWordをWordペアに分割（純粋関数）
        /// リトルエンディアン形式で分割
        /// </summary>
        public WordPair SplitDwordToWordPair((DeviceCode deviceCode, uint address, uint value) dwordDevice)
        {
            // リトルエンディアン分割
            var lowWord = (ushort)(dwordDevice.value & 0xFFFF);        // 下位16ビット
            var highWord = (ushort)((dwordDevice.value >> 16) & 0xFFFF); // 上位16ビット

            return new WordPair
            {
                LowWord = (dwordDevice.deviceCode, dwordDevice.address, lowWord),
                HighWord = (dwordDevice.deviceCode, dwordDevice.address + 1, highWord)
            };
        }

        /// <summary>
        /// WordペアをDWordに結合（純粋関数）
        /// リトルエンディアン形式で結合
        /// </summary>
        public (DeviceCode deviceCode, uint address, uint value) CombineWordPairToDword(WordPair wordPair)
        {
            // リトルエンディアン結合
            var dwordValue = (uint)(wordPair.LowWord.value | (wordPair.HighWord.value << 16));

            return (wordPair.LowWord.deviceCode, wordPair.LowWord.address, dwordValue);
        }
    }

    #endregion
}