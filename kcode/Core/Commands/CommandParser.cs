using Kcode.Core.Config;

namespace Kcode.Core.Commands;

/// <summary>
/// 命令解析器
/// 支持正则匹配和参数提取
/// </summary>
public class CommandParser
{
    private readonly CommandRegistry _registry;

    public CommandParser(CommandRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// 解析命令
    /// </summary>
    public ParsedCommand? Parse(string input) => ParseInternal(input, 0);

    private ParsedCommand? ParseInternal(string input, int depth)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }
        if (depth > 5)
        {
            return new ParsedCommand
            {
                Type = CommandType.Unknown,
                Name = input,
                Input = input
            };
        }

        input = input.Trim();

        // 1. 系统命令
        foreach (var descriptor in _registry.SystemCommands)
        {
            if (descriptor.Matches(input))
            {
                return new ParsedCommand
                {
                    Type = CommandType.System,
                    Name = descriptor.Name,
                    Input = input,
                    Config = descriptor.Config
                };
            }
        }

        // 2. API 命令
        foreach (var descriptor in _registry.ApiCommands)
        {
            var match = TryMatchApiCommand(input, descriptor);
            if (match != null)
            {
                return new ParsedCommand
                {
                    Type = CommandType.Api,
                    Name = descriptor.Name,
                    Input = input,
                    Parameters = match.Parameters,
                    ApiConfig = descriptor.Config
                };
            }
        }

        // 3. 宏命令
        foreach (var descriptor in _registry.MacroCommands)
        {
            if (descriptor.Matches(input))
            {
                return new ParsedCommand
                {
                    Type = CommandType.Macro,
                    Name = descriptor.Name,
                    Input = input,
                    MacroConfig = descriptor.Config
                };
            }
        }

        // 4. 文本别名展开
        if (_registry.TryExpandAlias(input, out var expandedInput))
        {
            return ParseInternal(expandedInput, depth + 1);
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
    private CommandMatch? TryMatchApiCommand(string input, ApiCommandDescriptor descriptor)
    {
        if (descriptor.CompiledPattern == null)
        {
            return null;
        }

        var match = descriptor.CompiledPattern.Match(input);

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
        foreach (var kvp in descriptor.Config.RequestMapping)
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
