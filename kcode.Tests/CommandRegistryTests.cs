using System.Collections.Generic;
using System.Linq;
using Kcode.Core.Config;
using Kcode.Core.Commands;
using Xunit;

namespace Kcode.Tests;

public class CommandRegistryTests
{
    private static RootConfig CreateConfig()
    {
        return new RootConfig
        {
            Commands = new CommandsConfig
            {
                System = new Dictionary<string, SystemCommandConfig>
                {
                    ["help"] = new SystemCommandConfig
                    {
                        Action = "builtin:help",
                        Description = "显示帮助",
                        Aliases = new List<string> { "?", "h" }
                    },
                    ["/clear"] = new SystemCommandConfig
                    {
                        Action = "builtin:clear",
                        Description = "清屏"
                    }
                },
                ApiCommands = new Dictionary<string, ApiCommandConfig>
                {
                    ["gcode"] = new ApiCommandConfig
                    {
                        Pattern = "^G0.*",
                        Endpoint = "execute"
                    }
                },
                Macros = new Dictionary<string, MacroCommandConfig>
                {
                    ["home"] = new MacroCommandConfig
                    {
                        Description = "回零",
                        Aliases = new List<string> { "回零" }
                    }
                },
                Aliases = new Dictionary<string, string>
                {
                    ["/h "] = "/help ",
                    ["gg " ] = "gcode "
                }
            }
        };
    }

    [Fact]
    public void SystemCommands_NormalizedNamesIncludeLeadingSlash()
    {
        var registry = new CommandRegistry(CreateConfig());
        Assert.Contains(registry.SystemCommands, cmd => cmd.Name == "/help");
        Assert.Contains(registry.SystemCommands, cmd => cmd.Name == "/clear");
        Assert.All(registry.SystemCommands, cmd => Assert.StartsWith("/", cmd.Name));
    }

    [Fact]
    public void TryExpandAlias_ReplacesPrefix()
    {
        var registry = new CommandRegistry(CreateConfig());
        var success = registry.TryExpandAlias("/h foo", out var expanded);
        Assert.True(success);
        Assert.Equal("/help foo", expanded);
    }

    [Fact]
    public void ApiCommandDescriptors_CompileRegex()
    {
        var registry = new CommandRegistry(CreateConfig());
        var descriptor = registry.ApiCommands.Single(cmd => cmd.Name == "/gcode");
        Assert.NotNull(descriptor.CompiledPattern);
        Assert.Matches(descriptor.CompiledPattern!, "G0 X10");
    }
}
