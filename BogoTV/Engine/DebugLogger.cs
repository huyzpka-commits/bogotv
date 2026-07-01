using System;
using System.IO;
using System.Text;

namespace BogoTV.Engine
{
    public static class DebugLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BogoTV", "debug.log");

        private static readonly object _lock = new object();

        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    string? dir = Path.GetDirectoryName(LogPath);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogPath, line, Encoding.UTF8);
                }
            }
            catch { }
        }

        public static void Clear()
        {
            try
            {
                string? dir = Path.GetDirectoryName(LogPath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(LogPath, "", Encoding.UTF8);
            }
            catch { }
        }
    }
}
