using System.Text.Json;

namespace Kcode.Core;

/// <summary>
/// 命令历史记录管理器
/// 支持导航、持久化和搜索
/// </summary>
public class CommandHistory
{
    private readonly List<string> _history = new();
    private int _currentIndex = -1;
    private readonly string _historyFilePath;
    private readonly int _maxHistorySize;

    public CommandHistory(string historyFilePath = ".kcode_history", int maxHistorySize = 1000)
    {
        _historyFilePath = historyFilePath;
        _maxHistorySize = maxHistorySize;
        LoadFromFile();
    }

    /// <summary>
    /// 添加命令到历史记录
    /// </summary>
    public void Add(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        // 避免连续重复命令
        if (_history.Count > 0 && _history[^1] == command)
            return;

        _history.Add(command);

        // 限制历史记录大小
        while (_history.Count > _maxHistorySize)
        {
            _history.RemoveAt(0);
        }

        // 重置索引到末尾
        _currentIndex = -1;

        // 自动保存
        SaveToFile();
    }

    /// <summary>
    /// 向上导航（获取更早的命令）
    /// </summary>
    public string? NavigateUp()
    {
        if (_history.Count == 0)
            return null;

        if (_currentIndex == -1)
        {
            // 从最新的命令开始
            _currentIndex = _history.Count - 1;
        }
        else if (_currentIndex > 0)
        {
            // 向上移动
            _currentIndex--;
        }

        return _currentIndex >= 0 && _currentIndex < _history.Count
            ? _history[_currentIndex]
            : null;
    }

    /// <summary>
    /// 向下导航（获取更新的命令）
    /// </summary>
    public string? NavigateDown()
    {
        if (_history.Count == 0 || _currentIndex == -1)
            return null;

        _currentIndex++;

        if (_currentIndex >= _history.Count)
        {
            // 到达末尾，重置
            _currentIndex = -1;
            return string.Empty; // 返回空字符串表示清空输入
        }

        return _history[_currentIndex];
    }

    /// <summary>
    /// 重置导航索引
    /// </summary>
    public void ResetNavigation()
    {
        _currentIndex = -1;
    }

    /// <summary>
    /// 搜索历史记录（支持反向搜索 Ctrl+R）
    /// </summary>
    public List<string> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        return _history
            .Where(cmd => cmd.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .Reverse() // 最新的在前
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// 获取所有历史记录
    /// </summary>
    public IReadOnlyList<string> GetAll()
    {
        return _history.AsReadOnly();
    }

    /// <summary>
    /// 获取最近的 N 条记录
    /// </summary>
    public IReadOnlyList<string> GetRecent(int count)
    {
        var startIndex = Math.Max(0, _history.Count - count);
        return _history.Skip(startIndex).ToList().AsReadOnly();
    }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
        SaveToFile();
    }

    /// <summary>
    /// 从文件加载历史记录
    /// </summary>
    private void LoadFromFile()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                var history = JsonSerializer.Deserialize<List<string>>(json);

                if (history != null)
                {
                    _history.Clear();
                    _history.AddRange(history);
                }
            }
        }
        catch (Exception ex)
        {
            // 静默失败，不影响程序运行
            Console.Error.WriteLine($"Failed to load history: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存历史记录到文件
    /// </summary>
    private void SaveToFile()
    {
        try
        {
            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_historyFilePath, json);
        }
        catch (Exception ex)
        {
            // 静默失败，不影响程序运行
            Console.Error.WriteLine($"Failed to save history: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取历史记录统计
    /// </summary>
    public (int Total, int Unique) GetStatistics()
    {
        return (_history.Count, _history.Distinct().Count());
    }
}
