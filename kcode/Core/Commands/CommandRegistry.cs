using System.Linq;
using System.Text.RegularExpressions;
using Kcode.Core.Config;

namespace Kcode.Core.Commands;

/// <summary>
/// 命令注册中心，集中提供命令元数据以供解析、补全和 UI 使用。
/// </summary>
public class CommandRegistry
{
    private readonly List<SystemCommandDescriptor> _systemCommands = new();
    private readonly List<ApiCommandDescriptor> _apiCommands = new();
    private readonly List<MacroCommandDescriptor> _macroCommands = new();
    private readonly List<CommandDescriptor> _allCommands = new();
    private readonly Dictionary<string, string> _textAliases = new(StringComparer.OrdinalIgnoreCase);

    public CommandRegistry(RootConfig config)
    {
        BuildSystemCommands(config);
        BuildApiCommands(config);
        BuildMacroCommands(config);
        BuildTextAliases(config);

        _allCommands.AddRange(_systemCommands);
        _allCommands.AddRange(_apiCommands);
        _allCommands.AddRange(_macroCommands);
        _allCommands.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<CommandDescriptor> AllCommands => _allCommands;
    public IReadOnlyList<SystemCommandDescriptor> SystemCommands => _systemCommands;
    public IReadOnlyList<ApiCommandDescriptor> ApiCommands => _apiCommands;
    public IReadOnlyList<MacroCommandDescriptor> MacroCommands => _macroCommands;
    public IReadOnlyDictionary<string, string> TextAliases => _textAliases;

    public bool TryExpandAlias(string input, out string expanded)
    {
        foreach (var kvp in _textAliases)
        {
            if (input.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                expanded = input.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
                return true;
            }
        }

        expanded = input;
        return false;
    }

    private void BuildSystemCommands(RootConfig config)
    {
        foreach (var kvp in config.Commands.System)
        {
            var name = CommandNameHelper.Normalize(kvp.Key);
            var aliases = kvp.Value.Aliases?
                .Select(CommandNameHelper.Normalize)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var descriptor = new SystemCommandDescriptor(
                name,
                string.IsNullOrWhiteSpace(kvp.Value.Description) ? "系统命令" : kvp.Value.Description,
                kvp.Value,
                aliases);

            _systemCommands.Add(descriptor);
        }
    }

    private void BuildApiCommands(RootConfig config)
    {
        foreach (var kvp in config.Commands.ApiCommands)
        {
            var name = CommandNameHelper.Normalize(kvp.Key);
            var description = string.IsNullOrWhiteSpace(kvp.Value.Description)
                ? "API 命令"
                : kvp.Value.Description;

            Regex? compiled = null;
            if (!string.IsNullOrWhiteSpace(kvp.Value.Pattern))
            {
                compiled = new Regex(
                    kvp.Value.Pattern,
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            _apiCommands.Add(new ApiCommandDescriptor(
                name,
                description,
                kvp.Value,
                compiled));
        }
    }

    private void BuildMacroCommands(RootConfig config)
    {
        foreach (var kvp in config.Commands.Macros)
        {
            var name = CommandNameHelper.Normalize(kvp.Key);
            var aliases = kvp.Value.Aliases?
                .Select(CommandNameHelper.Normalize)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var descriptor = new MacroCommandDescriptor(
                name,
                string.IsNullOrWhiteSpace(kvp.Value.Description) ? "宏命令" : kvp.Value.Description,
                kvp.Value,
                aliases);

            _macroCommands.Add(descriptor);
        }
    }

    private void BuildTextAliases(RootConfig config)
    {
        foreach (var kvp in config.Commands.Aliases)
        {
            if (!_textAliases.ContainsKey(kvp.Key))
            {
                _textAliases[kvp.Key] = kvp.Value;
            }
        }
    }
}
