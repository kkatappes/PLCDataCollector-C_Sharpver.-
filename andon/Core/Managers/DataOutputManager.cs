using Andon.Core.Models;
using Andon.Core.Models.ConfigModels;
using Andon.Core.Constants;
using Andon.Core.Interfaces;
using System.Text.Json;

namespace Andon.Core.Managers;

/// <summary>
/// Step7: データ出力（Excel設定ベース）
/// PlcConfigurationモデルを使用した統一設計
/// ⚠️ 注意: JSON設定用メソッドは削除済み（JSON設定廃止により不要）
/// Phase7 (2025-11-25)実装: JSON形式での不連続デバイスデータ出力
/// Phase 2-3: PlcModelをJSON出力に追加（Excel設定から読み込み）
/// </summary>
public class DataOutputManager : IDataOutputManager
{
    // Phase3: ビットデバイス分割処理用定数
    private const int BitsPerWord = 16;  // 1ワード = 16ビット
    private const int DefaultDigits = 1;  // デフォルト桁数

    /// <summary>
    /// ReadRandomデータをJSON出力（不連続デバイス対応、Phase4仕様対応）
    /// </summary>
    /// <param name="data">処理済みレスポンスデータ</param>
    /// <param name="outputDirectory">出力ディレクトリパス</param>
    /// <param name="ipAddress">IPアドレス（設定ファイルのConnection.IpAddressから取得）</param>
    /// <param name="port">ポート番号（設定ファイルのConnection.Portから取得）</param>
    /// <param name="plcModel">PLCモデル（デバイス名）</param>
    /// <param name="deviceConfig">デバイス設定情報（設定ファイルのTargetDevices.Devicesから構築）
    /// キー: デバイス名（"M0", "D100"など）
    /// 値: DeviceEntryInfo（Name=Description, Digits=1）</param>
    public void OutputToJson(
        ProcessedResponseData data,
        string outputDirectory,
        string ipAddress,
        int port,
        string plcModel,
        Dictionary<string, DeviceEntryInfo> deviceConfig)
    {
        try
        {
            // Phase4: データ検証
            ValidateInputData(data, outputDirectory, ipAddress, port, deviceConfig);

            // Phase2: ログ出力開始
            Console.WriteLine($"[INFO] JSON出力開始: IP={ipAddress}, Port={port}, デバイス数={data.ProcessedData.Count}");

            // Phase7: データ処理時刻を取得（JSON timestampフィールド用）
            var timestamp = data.ProcessedAt;

            // ファイル名生成: xxx-xxx-x-xx_zzzz_ターゲット名.json
            var ipString = ipAddress.Replace(".", "-");
            var fileName = $"{ipString}_{port}_{plcModel}.json";
            var filePath = Path.Combine(outputDirectory, fileName);

            // Phase2: ディレクトリ存在確認・自動作成
            if (!Directory.Exists(outputDirectory))
            {
                Console.WriteLine($"[INFO] 出力ディレクトリを作成: {outputDirectory}");
                Directory.CreateDirectory(outputDirectory);
            }

            // Phase3: items配列をListで生成(ビットデバイス16ビット分割対応)
            var itemsList = new List<object>();

            foreach (var kvp in data.ProcessedData)
            {
                var deviceData = kvp.Value;

                // Phase3: ビットデバイスは16ビットに分割、ワード/ダブルワードはそのまま
                if (deviceData.Code.IsBitDevice())
                {
                    AddBitDeviceItems(itemsList, deviceData, deviceConfig);
                }
                else
                {
                    AddWordDeviceItem(itemsList, kvp.Key, deviceData, deviceConfig);
                }
            }

            // JSON構造構築
            var jsonData = new
            {
                source = new
                {
                    plcModel = plcModel ?? "",  // nullの場合は空文字列
                    ipAddress = ipAddress,
                    port = port
                },
                timestamp = new
                {
                    local = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz")  // ISO 8601 with timezone
                },
                items = itemsList.ToArray()  // Listから配列に変換
            };

            // JSON出力(インデント付き、読みやすい形式)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonString = JsonSerializer.Serialize(jsonData, options);
            File.WriteAllText(filePath, jsonString);

            // Phase2: ログ出力完了
            var fileInfo = new FileInfo(filePath);
            Console.WriteLine($"[INFO] JSON出力完了: ファイル={fileName}, デバイス数={data.ProcessedData.Count}, ファイルサイズ={fileInfo.Length}バイト");
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine($"[ERROR] データがnullです: {ex.Message}");
            throw;  // 再スロー
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[ERROR] データ検証エラー: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[ERROR] データ処理が失敗しています: {ex.Message}");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"[ERROR] 書き込み権限エラー: {ex.Message}");
            throw;
        }
        catch (DirectoryNotFoundException ex)
        {
            Console.WriteLine($"[ERROR] ディレクトリが見つかりません: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"[ERROR] ファイルI/Oエラー: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] 予期しないエラー: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Phase4: 入力データを検証します
    /// </summary>
    /// <exception cref="ArgumentNullException">dataまたはdeviceConfigがnullの場合</exception>
    /// <exception cref="ArgumentException">パラメータが不正な場合</exception>
    /// <exception cref="InvalidOperationException">データ処理が失敗している場合</exception>
    private void ValidateInputData(
        ProcessedResponseData data,
        string outputDirectory,
        string ipAddress,
        int port,
        Dictionary<string, DeviceEntryInfo> deviceConfig)
    {
        // nullチェック
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "ProcessedResponseDataがnullです");
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("出力ディレクトリが指定されていません", nameof(outputDirectory));
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IPアドレスが指定されていません", nameof(ipAddress));
        }

        if (port <= 0 || port > 65535)
        {
            throw new ArgumentException($"ポート番号が不正です: {port}", nameof(port));
        }

        if (deviceConfig == null)
        {
            throw new ArgumentNullException(nameof(deviceConfig), "デバイス設定がnullです");
        }

        // ProcessedDataチェック
        if (data.ProcessedData == null)
        {
            throw new ArgumentException("ProcessedDataがnullです", nameof(data));
        }

        if (data.ProcessedData.Count == 0)
        {
            throw new ArgumentException("ProcessedDataが空です", nameof(data));
        }

        // 処理成功フラグチェック（警告のみ、例外はスローしない）
        if (!data.IsSuccess)
        {
            Console.WriteLine("[WARN] データ処理が失敗していますが、出力を継続します");
        }
    }

    /// <summary>
    /// Phase3: ビットデバイスを16ビットに分割してitems配列に追加
    /// SLMPプロトコル仕様: ビットデバイスはMSB-firstで受信されるため、
    /// ビット順を反転してLSB-firstに変換する必要がある。
    /// </summary>
    /// <param name="itemsList">追加先のリスト</param>
    /// <param name="deviceData">デバイスデータ</param>
    /// <param name="deviceConfig">デバイス設定情報</param>
    private void AddBitDeviceItems(
        List<object> itemsList,
        DeviceData deviceData,
        Dictionary<string, DeviceEntryInfo> deviceConfig)
    {
        uint bitValue = deviceData.Value;

        // 1ワード(16ビット)を個別のビットに展開
        // 重要: SLMPではMSB-first、PLCデバイスアドレスはLSB-first
        //       ビット15(MSB) → M0, ビット0(LSB) → M15
        for (int i = 0; i < BitsPerWord; i++)
        {
            // ビット順反転: ビット15を最初に取得(M0に対応)
            int bitIndex = BitsPerWord - 1 - i;
            int bit = ExtractBit(bitValue, bitIndex);

            // デバイス名とアドレス生成
            string bitDeviceName = $"{deviceData.Code}{deviceData.Address + i}";
            string bitDeviceNumber = (deviceData.Address + i).ToString("D3");

            // 設定情報取得
            var (name, digits) = GetDeviceConfigInfo(bitDeviceName, deviceConfig);

            // items配列に追加
            itemsList.Add(new
            {
                name = name,
                device = new
                {
                    code = deviceData.Code.ToString(),
                    number = bitDeviceNumber
                },
                digits = digits,
                unit = "bit",
                value = bit
            });
        }
    }

    /// <summary>
    /// Phase3: ワード/ダブルワードデバイスをitems配列に追加（分割なし）
    /// </summary>
    /// <param name="itemsList">追加先のリスト</param>
    /// <param name="deviceKey">デバイスキー（例: "D100"）</param>
    /// <param name="deviceData">デバイスデータ</param>
    /// <param name="deviceConfig">デバイス設定情報</param>
    private void AddWordDeviceItem(
        List<object> itemsList,
        string deviceKey,
        DeviceData deviceData,
        Dictionary<string, DeviceEntryInfo> deviceConfig)
    {
        // 設定情報取得
        var (name, digits) = GetDeviceConfigInfo(deviceKey, deviceConfig);

        // items配列に追加
        itemsList.Add(new
        {
            name = name,
            device = new
            {
                code = deviceData.Code.ToString(),
                number = deviceData.Address.ToString("D3")
            },
            digits = digits,
            unit = deviceData.Type.ToLower(),
            value = ConvertValue(deviceData)
        });
    }

    /// <summary>
    /// Phase3: デバイス設定情報を取得（共通処理）
    /// </summary>
    /// <param name="deviceName">デバイス名（例: "M0", "D100"）</param>
    /// <param name="deviceConfig">デバイス設定情報</param>
    /// <returns>名前と桁数のタプル</returns>
    private (string name, int digits) GetDeviceConfigInfo(
        string deviceName,
        Dictionary<string, DeviceEntryInfo> deviceConfig)
    {
        if (deviceConfig.TryGetValue(deviceName, out var config))
        {
            return (config.Name, config.Digits);
        }
        return (deviceName, DefaultDigits);
    }

    /// <summary>
    /// Phase3: ビット抽出（ビット演算）
    /// </summary>
    /// <param name="value">元の値</param>
    /// <param name="bitIndex">抽出するビット位置（0～15）</param>
    /// <returns>ビット値（0 or 1）</returns>
    private static int ExtractBit(uint value, int bitIndex)
    {
        return (int)((value >> bitIndex) & 1);
    }

    /// <summary>
    /// DeviceDataの値を適切な型に変換
    /// </summary>
    private object ConvertValue(DeviceData deviceData)
    {
        return deviceData.Type.ToLower() switch
        {
            "bit" => deviceData.Value,  // 0 or 1
            "word" => deviceData.Value,  // uint16
            "dword" => deviceData.Value,  // uint32
            _ => deviceData.Value
        };
    }
}
