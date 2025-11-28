using Spectre.Console;
using Kcode.Core;
using Kcode.Core.Config;
using Kcode.Core.Transport;

namespace Kcode;

class Program
{
    static async Task Main(string[] args)
    {
        // Setup Console
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Check for virtual-mode test
        if (args.Length > 0 && args[0] == "--test-virtual")
        {
            await TestVirtualMode.RunAsync();
            return;
        }

        // Check for REST API test mode
        if (args.Length > 0 && args[0] == "--test-rest")
        {
            await TestRestApi.RunAsync();
            return;
        }

        await RunClientAsync(args);
    }

    static async Task RunClientAsync(string[] args)
    {
        try
        {
            var configPath = ResolveConfigPath(args);
            AnsiConsole.MarkupLine($"[bold cyan]Starting KCode ({configPath})...[/]\n");

            // 加载 v2 配置
            var loader = new ConfigLoader();
            var config = loader.Load(configPath);

            // 创建传输层
            var transport = TransportFactory.Create(config.Transport);

            // 创建 REPL 引擎
            var repl = new ReplEngine(config, transport);

            // 运行
            await repl.RunAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    static string ResolveConfigPath(string[] args)
    {
        var argValue = GetConfigArgument(args);
        if (!string.IsNullOrWhiteSpace(argValue))
        {
            return ConfigPathResolver.Normalize(argValue);
        }

        var searchRoots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
            Path.Combine(Directory.GetCurrentDirectory(), "kcode"),
            Path.Combine(AppContext.BaseDirectory, "..")
        };

        var detected = ConfigPathResolver.ProbeDefaultLocations(searchRoots);
        if (detected != null)
        {
            return detected;
        }

        // fallback: use the build输出目录
        var fallback = Path.Combine(AppContext.BaseDirectory, "Config", "config-virtual.yaml");
        return ConfigPathResolver.Normalize(fallback);
    }

    static string? GetConfigArgument(string[] args)
    {
        const string Prefix = "--config=";

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals("--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }

            if (arg.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[Prefix.Length..];
            }
        }

        return null;
    }
}
