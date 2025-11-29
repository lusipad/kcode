using System.Linq;
using Kcode.Core.Commands;

namespace Kcode.Core;

/// <summary>
/// 命令自动补全器
/// 支持 Tab 补全和命令建议
/// </summary>
public class CommandCompleter
{
    private readonly CommandRegistry _registry;
    private readonly CommandHistory _history;
    private List<string> _cachedCommands = new();
    private int _completionIndex = 0;
    private string? _completionPrefix = null;
    private List<string>? _completionCandidates = null;

    public CommandCompleter(CommandRegistry registry, CommandHistory history)
    {
        _registry = registry;
        _history = history;
        BuildCommandCache();
    }

    /// <summary>
    /// 构建命令缓存（所有可补全的命令）
    /// </summary>
    private void BuildCommandCache()
    {
        _cachedCommands.Clear();

        foreach (var descriptor in _registry.SystemCommands)
        {
            _cachedCommands.Add(descriptor.Name);
            _cachedCommands.AddRange(descriptor.Aliases);
        }

        foreach (var descriptor in _registry.MacroCommands)
        {
            _cachedCommands.Add(descriptor.Name);
            _cachedCommands.AddRange(descriptor.Aliases);
        }

        foreach (var descriptor in _registry.ApiCommands)
        {
            if (!string.IsNullOrEmpty(descriptor.Config.Pattern) &&
                descriptor.Config.Pattern.StartsWith("^"))
            {
                var prefix = ExtractCommandPrefix(descriptor.Config.Pattern);
                if (!string.IsNullOrEmpty(prefix))
                {
                    _cachedCommands.Add(prefix);
                }
            }
        }

        foreach (var alias in _registry.TextAliases.Keys)
        {
            _cachedCommands.Add(alias);
        }

        // 去重并排序
        _cachedCommands = _cachedCommands.Distinct().OrderBy(c => c).ToList();
    }

    /// <summary>
    /// 提取正则表达式的命令前缀
    /// </summary>
    private string ExtractCommandPrefix(string pattern)
    {
        // 简单提取：从 ^ 开始到第一个正则特殊字符
        var prefix = pattern.TrimStart('^');
        var specialChars = new[] { '(', '[', '{', '*', '+', '?', '\\', '|', '.' };

        var endIndex = prefix.Length;
        foreach (var ch in specialChars)
        {
            var index = prefix.IndexOf(ch);
            if (index != -1 && index < endIndex)
            {
                endIndex = index;
            }
        }

        return prefix.Substring(0, endIndex).Trim();
    }

    /// <summary>
    /// Tab 补全：获取下一个补全建议
    /// </summary>
    public string? GetNextCompletion(string input)
    {
        // 如果输入变化，重新开始补全
        if (_completionPrefix != input)
        {
            ResetCompletion(input);
        }

        if (_completionCandidates == null || _completionCandidates.Count == 0)
            return null;

        // 循环选择候选项
        var completion = _completionCandidates[_completionIndex];
        _completionIndex = (_completionIndex + 1) % _completionCandidates.Count;

        return completion;
    }

    /// <summary>
    /// 重置补全状态
    /// </summary>
    private void ResetCompletion(string input)
    {
        _completionPrefix = input;
        _completionIndex = 0;
        _completionCandidates = FindCandidates(input);
    }

    /// <summary>
    /// 查找补全候选项
    /// </summary>
    private List<string> FindCandidates(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        var candidates = new List<string>();

        // 1. 从命令缓存中查找匹配项
        candidates.AddRange(_cachedCommands.Where(cmd =>
            cmd.StartsWith(input, StringComparison.OrdinalIgnoreCase)));

        // 2. 从历史记录中查找匹配项（只取前10个）
        var historyMatches = _history.GetAll()
            .Where(cmd => cmd.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .Reverse() // 最新的优先
            .Take(10);

        candidates.AddRange(historyMatches);

        // 3. 去重并排序（历史记录优先，然后按字母序）
        return candidates
            .Distinct()
            .OrderByDescending(c => _history.GetAll().Contains(c)) // 历史记录优先
            .ThenBy(c => c)
            .ToList();
    }

    /// <summary>
    /// 获取命令建议（用于实时提示）
    /// </summary>
    public List<string> GetSuggestions(string input, int maxSuggestions = 5)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        return FindCandidates(input).Take(maxSuggestions).ToList();
    }

    /// <summary>
    /// 获取最佳补全建议（用于内联预览）
    /// </summary>
    public string? GetBestSuggestion(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var candidates = FindCandidates(input);
        return candidates.FirstOrDefault();
    }

    /// <summary>
    /// 重置补全状态（外部调用）
    /// </summary>
    public void Reset()
    {
        _completionPrefix = null;
        _completionIndex = 0;
        _completionCandidates = null;
    }

    /// <summary>
    /// 刷新命令缓存（配置变更后调用）
    /// </summary>
    public void RefreshCache()
    {
        BuildCommandCache();
        Reset();
    }

    /// <summary>
    /// 获取所有可补全的命令
    /// </summary>
    public IReadOnlyList<string> GetAllCommands()
    {
        return _cachedCommands.AsReadOnly();
    }
}
