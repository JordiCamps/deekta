using System.Text;

namespace Deekta;

/// <summary>
/// Minimal append-only file logger. Writes only events and errors — never audio
/// content or transcription text — to %AppData%\deekta\logs\deekta-yyyyMMdd.log.
/// </summary>
internal static class Logger
{
    private static readonly object Gate = new();
    private static string _logFile = string.Empty;

    public static void Init()
    {
        try
        {
            string name = $"deekta-{DateTime.Now:yyyyMMdd}.log";
            _logFile = Path.Combine(AppPaths.LogsDir, name);
        }
        catch
        {
            // Logging must never crash the app; if we can't set up a file, stay silent.
            _logFile = string.Empty;
        }
    }

    public static void Info(string message) => Write("INFO", message, null);

    public static void Warn(string message) => Write("WARN", message, null);

    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        if (string.IsNullOrEmpty(_logFile))
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
              .Append(" [").Append(level).Append("] ")
              .Append(message);
            if (ex is not null)
            {
                sb.Append(" | ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }
            sb.AppendLine();

            lock (Gate)
            {
                File.AppendAllText(_logFile, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Swallow logging failures.
        }
    }
}
