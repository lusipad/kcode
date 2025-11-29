using System.Linq;
using System.Text.RegularExpressions;
using Kcode.Core.Config;

namespace Kcode.Core.Commands;

/// <summary>
/// 基础命令描述信息
/// </summary>
public abstract record CommandDescriptor(string Name, string Description, CommandType Type)
{
    public string DisplayName => Name;
}

public sealed record SystemCommandDescriptor(
    string Name,
    string Description,
    SystemCommandConfig Config,
    IReadOnlyList<string> Aliases)
    : CommandDescriptor(Name, Description, CommandType.System)
{
    public bool Matches(string input)
    {
        if (CommandNameHelper.Equals(input, Name))
        {
            return true;
        }

        return Aliases.Any(alias => CommandNameHelper.Equals(input, alias));
    }
}

public sealed record ApiCommandDescriptor(
    string Name,
    string Description,
    ApiCommandConfig Config,
    Regex? CompiledPattern)
    : CommandDescriptor(Name, Description, CommandType.Api);

public sealed record MacroCommandDescriptor(
    string Name,
    string Description,
    MacroCommandConfig Config,
    IReadOnlyList<string> Aliases)
    : CommandDescriptor(Name, Description, CommandType.Macro)
{
    public bool Matches(string input)
    {
        if (CommandNameHelper.Equals(input, Name))
        {
            return true;
        }

        return Aliases.Any(alias => CommandNameHelper.Equals(input, alias));
    }
}
