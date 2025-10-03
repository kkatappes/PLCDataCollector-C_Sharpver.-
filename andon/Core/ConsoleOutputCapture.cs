using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SlmpClient.Core
{
    /// <summary>
    /// コンソール出力をキャプチャして外部ファイルに保存するクラス
    /// </summary>
    public class ConsoleOutputCapture : TextWriter
    {
        private readonly TextWriter _originalOut;
        private readonly TextWriter _fileWriter;
        private readonly bool _enableConsoleOutput;

        public ConsoleOutputCapture(string logFilePath, bool enableConsoleOutput = true)
        {
            _originalOut = Console.Out;
            _enableConsoleOutput = enableConsoleOutput;

            // ログディレクトリを作成
            var logDir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // ファイルライターを作成（追記モード）
            var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _fileWriter = new StreamWriter(fileStream, Encoding.UTF8)
            {
                AutoFlush = true
            };

            // コンソール出力を置き換え
            Console.SetOut(this);
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (_enableConsoleOutput)
            {
                _originalOut.Write(value);
            }
            _fileWriter.Write(value);
        }

        public override void Write(string value)
        {
            if (_enableConsoleOutput)
            {
                _originalOut.Write(value);
            }
            _fileWriter.Write(value);
        }

        public override void WriteLine(string value)
        {
            var timestampedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {value}";

            if (_enableConsoleOutput)
            {
                _originalOut.WriteLine(value);
            }
            _fileWriter.WriteLine(timestampedLine);
        }

        public override void WriteLine()
        {
            if (_enableConsoleOutput)
            {
                _originalOut.WriteLine();
            }
            _fileWriter.WriteLine();
        }

        public override async Task WriteAsync(char value)
        {
            if (_enableConsoleOutput)
            {
                await _originalOut.WriteAsync(value);
            }
            await _fileWriter.WriteAsync(value);
        }

        public override async Task WriteAsync(string value)
        {
            if (_enableConsoleOutput)
            {
                await _originalOut.WriteAsync(value);
            }
            await _fileWriter.WriteAsync(value);
        }

        public override async Task WriteLineAsync(string value)
        {
            var timestampedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {value}";

            if (_enableConsoleOutput)
            {
                await _originalOut.WriteLineAsync(value);
            }
            await _fileWriter.WriteLineAsync(timestampedLine);
        }

        public override async Task WriteLineAsync()
        {
            if (_enableConsoleOutput)
            {
                await _originalOut.WriteLineAsync();
            }
            await _fileWriter.WriteLineAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // コンソール出力を元に戻す
                Console.SetOut(_originalOut);

                _fileWriter?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// ログファイルにメッセージを直接書き込み（タイムスタンプ付き）
        /// </summary>
        public async Task WriteLogMessageAsync(string message, string level = "INFO")
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            await _fileWriter.WriteLineAsync(logEntry);
        }

        /// <summary>
        /// セッション開始のマーカーを記録
        /// </summary>
        public async Task WriteSessionStartAsync()
        {
            var separator = new string('=', 80);
            await _fileWriter.WriteLineAsync();
            await _fileWriter.WriteLineAsync(separator);
            await WriteLogMessageAsync($"セッション開始: SLMP継続監視", "SESSION");
            await WriteLogMessageAsync($"プロセスID: {System.Diagnostics.Process.GetCurrentProcess().Id}", "SESSION");
            await _fileWriter.WriteLineAsync(separator);
            await _fileWriter.WriteLineAsync();
        }

        /// <summary>
        /// セッション終了のマーカーを記録
        /// </summary>
        public async Task WriteSessionEndAsync()
        {
            var separator = new string('=', 80);
            await _fileWriter.WriteLineAsync();
            await _fileWriter.WriteLineAsync(separator);
            await WriteLogMessageAsync("セッション終了", "SESSION");
            await _fileWriter.WriteLineAsync(separator);
            await _fileWriter.WriteLineAsync();
        }
    }
}