using System.Collections.ObjectModel;
using NLog;

namespace SimulatorApp.Logging;

/// <summary>应用日志门面：同时写文件（NLog）和 UI 日志列表。</summary>
public class AppLogger
{
    private static readonly Logger _nlog = LogManager.GetCurrentClassLogger();

    /// <summary>UI 绑定的日志列表（最多保留 500 条）。</summary>
    public ObservableCollection<string> LogEntries { get; } = new();

    private const int MaxEntries = 500;

    public void Info(string msg)
    {
        _nlog.Info(msg);
        Append("INFO", msg);
    }

    public void Warn(string msg)
    {
        _nlog.Warn(msg);
        Append("WARN", msg);
    }

    public void Error(string msg, Exception? ex = null)
    {
        if (ex != null) _nlog.Error(ex, msg);
        else _nlog.Error(msg);
        Append("ERROR", ex != null ? $"{msg} | {ex.Message}" : msg);
    }

    public void Debug(string msg) => _nlog.Debug(msg);

    private void Append(string level, string msg)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff}  [{level}]  {msg}";
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            LogEntries.Add(line);
            while (LogEntries.Count > MaxEntries)
                LogEntries.RemoveAt(0);
        });
    }
}
