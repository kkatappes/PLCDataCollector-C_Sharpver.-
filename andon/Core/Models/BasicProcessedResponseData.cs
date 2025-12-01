using System;
using System.Collections.Generic;

namespace Andon.Core.Models
{
    /// <summary>
    /// 基本処理済み応答データを表すモデル
    /// ProcessReceivedRawDataメソッドの戻り値として使用
    /// </summary>
    public class BasicProcessedResponseData
    {
        /// <summary>
        /// 処理成功フラグ
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 処理済みデバイスリスト
        /// </summary>
        public List<ProcessedDevice> ProcessedDevices { get; set; } = new();

        /// <summary>
        /// 処理日時
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// 処理時間（ミリ秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// エラー情報リスト
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告情報リスト
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 処理済みデバイス数
        /// </summary>
        public int ProcessedDeviceCount { get; set; }

        /// <summary>
        /// 総データサイズ（バイト）
        /// </summary>
        public int TotalDataSizeBytes { get; set; }

        /// <summary>
        /// 処理済みデバイスを追加
        /// </summary>
        /// <param name="deviceName">デバイス名</param>
        /// <param name="value">デバイス値</param>
        /// <param name="dataType">データ型</param>
        public void AddProcessedDevice(string deviceName, object value, string dataType)
        {
            var parts = deviceName.Split('D', 'M', 'X', 'Y');
            var deviceType = deviceName.Substring(0, 1);
            var address = 0;

            if (parts.Length > 1 && int.TryParse(parts[1], out address))
            {
                ProcessedDevices.Add(new ProcessedDevice
                {
                    DeviceType = deviceType,
                    Address = address,
                    Value = value,
                    DataType = dataType,
                    ProcessedAt = DateTime.Now,
                    DeviceName = deviceName
                });
                ProcessedDeviceCount = ProcessedDevices.Count;
            }
        }

        /// <summary>
        /// エラー情報を追加
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ</param>
        public void AddError(string errorMessage)
        {
            Errors.Add(errorMessage);
        }

        /// <summary>
        /// 警告情報を追加
        /// </summary>
        /// <param name="warningMessage">警告メッセージ</param>
        public void AddWarning(string warningMessage)
        {
            Warnings.Add(warningMessage);
        }
    }
}