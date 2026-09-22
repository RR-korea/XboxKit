using System;
using System.IO;
using System.Text;

namespace XboxKit.GUI
{
    public class FileLogger
    {
        private static FileLogger? _instance;
        private static readonly object _lock = new();
        private readonly string _logFilePath;
        private readonly StreamWriter? _writer;

        private FileLogger()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string logDir = Path.Combine(baseDir, "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _logFilePath = Path.Combine(logDir, $"xboxkit_{timestamp}.log");

                FileStream fs = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                _writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };

                Log($"=== XboxKit GUI 실행 로그 시작 ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
                Log($"OS: {Environment.OSVersion}, .NET: {Environment.Version}");
                Log($"BaseDirectory: {baseDir}");
            }
            catch (Exception ex)
            {
                _logFilePath = "";
                System.Diagnostics.Debug.WriteLine($"로그 파일 생성 실패: {ex.Message}");
            }
        }

        public static FileLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new FileLogger();
                    }
                }
                return _instance;
            }
        }

        public string LogFilePath => _logFilePath;

        public void Log(string message)
        {
            if (_writer == null) return;
            lock (_lock)
            {
                try
                {
                    string time = DateTime.Now.ToString("HH:mm:ss.fff");
                    _writer.WriteLine($"[{time}] {message}");
                }
                catch
                {
                    // 로깅 실패로 인한 본 프로그램 중단 방지
                }
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                try
                {
                    Log($"=== XboxKit GUI 세션 종료 ({DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===");
                    _writer?.Flush();
                    _writer?.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
