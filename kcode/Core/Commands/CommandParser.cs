using System.Text.RegularExpressions;
using Kcode.Core.Config;

namespace Kcode.Core.Commands;

/// <summary>
/// 命令解析器
/// 支持正则匹配和参数提取
/// </summary>
public class CommandParser
{
    private readonly RootConfig _config;

    public CommandParser(RootConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 解析命令
    /// </summary>
    public ParsedCommand? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        input = input.Trim();

        // 1. 检查系统命令
        foreach (var kvp in _config.Commands.System)
        {
            if (IsSystemCommandMatch(input, kvp.Key, kvp.Value))
            {
                return new ParsedCommand
                {
                    Type = CommandType.System,
                    Name = CommandNameHelper.Normalize(kvp.Key),
                    Input = input,
                    Config = kvp.Value
                };
            }
        }

        // 2. 检查 API 命令 (正则匹配)
        foreach (var kvp in _config.Commands.ApiCommands)
        {
            var match = TryMatchApiCommand(input, kvp.Value);
            if (match != null)
            {
                return new ParsedCommand
                {
                    Type = CommandType.Api,
                    Name = CommandNameHelper.Normalize(kvp.Key),
                    Input = input,
                    Parameters = match.Parameters,
                    ApiConfig = kvp.Value
                };
            }
        }

        // 3. 检查宏命令
        foreach (var kvp in _config.Commands.Macros)
        {
            if (IsMacroCommandMatch(input, kvp.Key, kvp.Value))
            {
                return new ParsedCommand
                {
                    Type = CommandType.Macro,
                    Name = CommandNameHelper.Normalize(kvp.Key),
                    Input = input,
                    MacroConfig = kvp.Value
                };
            }
        }

        // 4. 检查别名
        foreach (var kvp in _config.Commands.Aliases)
        {
            if (input.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                var expandedInput = input.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
                return Parse(expandedInput); // 递归解析
            }
        }

        // 5. 未匹配 - 可能是原始命令
        return new ParsedCommand
        {
            Type = CommandType.Unknown,
            Name = "unknown",
            Input = input
        };
    }

    /// <summary>
    /// 检查系统命令是否匹配
    /// </summary>
    private bool IsSystemCommandMatch(string input, string commandName, SystemCommandConfig config)
    {
        // 检查命令名
        if (CommandNameHelper.Equals(input, commandName))
        {
            return true;
        }

        return config.Aliases.Any(alias => CommandNameHelper.Equals(input, alias));
    }

    /// <summary>
    /// 尝试匹配 API 命令
    /// </summary>
    private CommandMatch? TryMatchApiCommand(string input, ApiCommandConfig config)
    {
        if (string.IsNullOrEmpty(config.Pattern))
        {
            return null;
        }

        var regex = new Regex(config.Pattern, RegexOptions.IgnoreCase);
        var match = regex.Match(input);

        if (!match.Success)
        {
            return null;
        }

        // 提取参数
        var parameters = new Dictionary<string, object>();

        // 提取捕获组
        for (int i = 1; i < match.Groups.Count; i++)
        {
            parameters[$"${i}"] = match.Groups[i].Value;
        }

        // 添加完整输入
        parameters["$input"] = input;

        // 映射到请求字段
        var requestMapping = new Dictionary<string, object>();
        foreach (var kvp in config.RequestMapping)
        {
            var value = ResolveParameterValue(kvp.Value, parameters);
            requestMapping[kvp.Key] = value;
        }

        return new CommandMatch
        {
            Parameters = requestMapping
        };
    }

    /// <summary>
    /// 检查宏命令是否匹配
    /// </summary>
    private bool IsMacroCommandMatch(string input, string macroName, MacroCommandConfig config)
    {
        // 检查宏名称
        if (CommandNameHelper.Equals(input, macroName))
        {
            return true;
        }

        return config.Aliases.Any(alias => CommandNameHelper.Equals(input, alias));
    }

    /// <summary>
    /// 解析参数值 (支持 $input, $1, $2 等)
    /// </summary>
    private object ResolveParameterValue(string template, Dictionary<string, object> parameters)
    {
        // 如果是参数引用
        if (template.StartsWith("$"))
        {
            if (parameters.TryGetValue(template, out var value))
            {
                return value;
            }
        }

        // 否则返回原始值
        return template;
    }
}

/// <summary>
/// 解析后的命令
/// </summary>
public class ParsedCommand
{
    public CommandType Type { get; set; }
    public string Name { get; set; } = "";
    public string Input { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = new();

    // 配置引用
    public SystemCommandConfig? Config { get; set; }
    public ApiCommandConfig? ApiConfig { get; set; }
    public MacroCommandConfig? MacroConfig { get; set; }
}

/// <summary>
/// 命令类型
/// </summary>
public enum CommandType
{
    System,     // 系统命令 (help, exit 等)
    Api,        // API 命令 (调用后端接口)
    Macro,      // 宏命令 (多步骤序列)
    Unknown     // 未知命令
}

/// <summary>
/// 命令匹配结果
/// </summary>
internal class CommandMatch
{
    public Dictionary<string, object> Parameters { get; set; } = new();
}

internal static class CommandNameHelper
{
    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var trimmed = value.Trim();
        return trimmed.StartsWith("/", StringComparison.Ordinal) ? trimmed : "/" + trimmed;
    }

    public static bool Equals(string left, string right)
    {
        return string.Equals(
            Normalize(left),
            Normalize(right),
            StringComparison.OrdinalIgnoreCase);
    }
}
