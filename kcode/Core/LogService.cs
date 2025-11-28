using System.Text;

namespace Kcode.Core;

public class LogService
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public LogService(string logPath)
    {
        _logPath = logPath;
        var dir = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (_lock)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    public IEnumerable<string> Tail(int lines)
    {
        if (!File.Exists(_logPath)) yield break;

        // Simple tail implementation
        var all = File.ReadLines(_logPath).ToList();
        foreach (var line in all.TakeLast(lines))
        {
            yield return line;
        }
    }
}
