namespace Andon;

/// <summary>
/// コマンドライン引数解析
/// </summary>
public class CommandLineOptions
{
    /// <summary>
    /// 設定ファイルディレクトリパス
    /// </summary>
    public string ConfigPath { get; set; } = "./config/";

    /// <summary>
    /// バージョン情報を表示するか
    /// </summary>
    public bool ShowVersion { get; set; } = false;

    /// <summary>
    /// ヘルプ情報を表示するか
    /// </summary>
    public bool ShowHelp { get; set; } = false;

    /// <summary>
    /// コマンドライン引数をパースする
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>パース済みオプション</returns>
    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case "--config":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        options.ConfigPath = args[++i];
                    }
                    break;

                case "--version":
                case "-v":
                    options.ShowVersion = true;
                    break;

                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;

                // 不明なオプションは無視
                default:
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// ヘルプメッセージを取得する
    /// </summary>
    /// <returns>ヘルプメッセージ</returns>
    public static string GetHelpMessage()
    {
        return @"Andon - PLC通信データ取得・記録システム

Usage:
  andon [options]

Options:
  -c, --config <path>    設定ファイルディレクトリパス（デフォルト: ./config/）
  -v, --version          バージョン情報を表示
  -h, --help             ヘルプ情報を表示

Examples:
  andon                          デフォルト設定で実行
  andon --config /path/to/config カスタム設定パスで実行
  andon --version                バージョン情報を表示
  andon --help                   ヘルプ情報を表示
";
    }

    /// <summary>
    /// バージョン情報を取得する
    /// </summary>
    /// <returns>バージョン情報</returns>
    public static string GetVersionMessage()
    {
        return @"Andon - PLC通信データ取得・記録システム
Version: 1.0.0
";
    }
}
