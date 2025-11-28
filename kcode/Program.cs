using Spectre.Console;
using Kcode.Core;
using Kcode.UI;
using Kcode.Transport;

namespace Kcode;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Setup Console
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // AnsiConsole.Write(new FigletText("kcode").Color(Color.Orange1)); // Removed redundant print

        // 2. Initialize Core Components
        var config = new ConfigLoader().Load("Config/config.yaml");
        var controller = new VirtualCncController(config);
        var logPath = ConfigHelper.Get(config, "Logs/app.log", "logging", "path");
        var logger = new LogService(logPath);

        var transportType = ConfigHelper.Get(config, "virtual", "transport", "type").ToString()?.ToLowerInvariant() ?? "virtual";
        IControlTransport transport;
        if (transportType == "grpc")
        {
            var endpoint = ConfigHelper.Get(config, "http://localhost:50051", "transport", "endpoint");
            var timeout = ConfigHelper.Get(config, 2000, "transport", "timeout_ms");
            try
            {
                transport = new GrpcTransport(endpoint, timeout);
            }
            catch (Exception ex)
            {
                MessageSystem.ShowWarning($"gRPC 初始化失败，回退到虚拟机: {ex.Message}");
                transport = new VirtualTransport(controller);
            }
        }
        else
        {
            transport = new VirtualTransport(controller);
        }

        await using var transportDisposable = transport;
        await transport.ConnectAsync();

        await using var statusCache = new StatusCache(transport);
        await statusCache.StartAsync();

        var ui = new LayoutRenderer(config);
        var router = new CommandRouter(transport, statusCache, config, logger);

        // 3. Start REPL Loop (Header rendered inside REPL layout)
        var repl = new ReplEngine(router, statusCache, ui, config, logger);
        await repl.RunAsync();
    }
}
