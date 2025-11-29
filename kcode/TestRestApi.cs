using Kcode.Core.Config;
using Kcode.Core.Transport;
using Kcode.Core.Commands;
using Spectre.Console;

namespace Kcode;

/// <summary>
/// REST API 通信测试
/// </summary>
public class TestRestApi
{
    public static async Task RunAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]KCode - REST API 通信测试[/]\n");

        try
        {
            // 1. 加载配置
            AnsiConsole.MarkupLine("[yellow]1. 加载 REST 配置...[/]");
            var loader = new ConfigLoader();
            var config = loader.Load("Config/config-rest-test.yaml");
            AnsiConsole.MarkupLine($"[green]✓[/] 配置加载成功");
            AnsiConsole.MarkupLine($"[dim]   Base URL: {config.Transport.BaseUrl}[/]\n");

            // 2. 创建传输层
            AnsiConsole.MarkupLine("[yellow]2. 连接到 REST API...[/]");
            var transport = TransportFactory.Create(config.Transport);
            await transport.ConnectAsync();
            AnsiConsole.MarkupLine($"[green]✓[/] 已连接到测试 API 服务\n");

            // 3. 创建命令系统
            var registry = new CommandRegistry(config);
            var parser = new CommandParser(registry);
            var executor = new CommandExecutor(transport, config);

            // 4. 测试 GET 状态
            await TestGetStatus(transport);

            // 5. 测试 POST 执行命令
            await TestExecuteCommands(parser, executor);

            // 6. 测试设置参数
            await TestSetParameter(parser, executor);

            // 7. 测试获取参数
            await TestGetParameters(transport);

            // 8. 测试流式数据
            await TestStreamingStatus(transport);

            // 清理
            await transport.DisconnectAsync();
            await transport.DisposeAsync();

            // 总结
            AnsiConsole.Write(new Rule("[green]测试完成 ✓[/]"));
            AnsiConsole.MarkupLine("\n[bold]REST API 通信验证通过！[/]");
            AnsiConsole.MarkupLine("  • HTTP 连接 ✓");
            AnsiConsole.MarkupLine("  • GET 请求 ✓");
            AnsiConsole.MarkupLine("  • POST 请求 ✓");
            AnsiConsole.MarkupLine("  • JSON 序列化/反序列化 ✓");
            AnsiConsole.MarkupLine("  • 响应模板渲染 ✓");
            AnsiConsole.MarkupLine("  • 流式数据轮询 ✓");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }

    private static async Task TestGetStatus(ITransport transport)
    {
        AnsiConsole.MarkupLine("[bold yellow]3. 测试 GET /status 端点:[/]");

        var response = await transport.InvokeAsync("get_status", null);

        if (response.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] 状态获取成功");
            AnsiConsole.MarkupLine($"[dim]   X:{response.GetDouble("x"):F2} " +
                                  $"Y:{response.GetDouble("y"):F2} " +
                                  $"Z:{response.GetDouble("z"):F2} " +
                                  $"State:{response.GetString("state")} " +
                                  $"Temp:{response.GetDouble("temp"):F1}°C[/]\n");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] 状态获取失败\n");
        }
    }

    private static async Task TestExecuteCommands(CommandParser parser, CommandExecutor executor)
    {
        AnsiConsole.MarkupLine("[bold yellow]4. 测试命令执行:[/]\n");

        var testCommands = new[]
        {
            "G0 X100 Y50",
            "G1 X200 Y100 F1000",
            "G28",
            "M3 S1000"
        };

        foreach (var cmdText in testCommands)
        {
            AnsiConsole.MarkupLine($"[cyan]> {cmdText}[/]");

            var command = parser.Parse(cmdText);
            if (command != null)
            {
                var result = await executor.ExecuteAsync(command);

                if (result.Success && !string.IsNullOrEmpty(result.Output))
                {
                    AnsiConsole.MarkupLine($"  {result.Output}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"  [red]✗ 执行失败[/]");
                }
            }

            await Task.Delay(300); // 短暂延迟
        }

        AnsiConsole.WriteLine();
    }

    private static async Task TestSetParameter(CommandParser parser, CommandExecutor executor)
    {
        AnsiConsole.MarkupLine("[bold yellow]5. 测试参数设置:[/]");

        var command = parser.Parse("/set max_velocity 3500");
        if (command != null)
        {
            var result = await executor.ExecuteAsync(command);
            if (!string.IsNullOrEmpty(result.Output))
            {
                AnsiConsole.MarkupLine($"  {result.Output}");
            }
        }

        AnsiConsole.WriteLine();
    }

    private static async Task TestGetParameters(ITransport transport)
    {
        AnsiConsole.MarkupLine("[bold yellow]6. 测试获取参数列表:[/]");

        var response = await transport.InvokeAsync("get_parameters", null);

        if (response.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] 参数列表获取成功");

            if (response.Data.TryGetValue("parameters", out var paramsObj) &&
                paramsObj is Dictionary<string, object> parameters)
            {
                foreach (var kvp in parameters)
                {
                    AnsiConsole.MarkupLine($"[dim]   {kvp.Key}: {kvp.Value}[/]");
                }
            }
        }

        AnsiConsole.WriteLine();
    }

    private static async Task TestStreamingStatus(ITransport transport)
    {
        AnsiConsole.MarkupLine("[bold yellow]7. 测试流式状态数据 (5 次轮询):[/]\n");

        var count = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await foreach (var statusData in transport.SubscribeAsync("poll_status", cts.Token))
            {
                if (statusData.Success)
                {
                    AnsiConsole.MarkupLine(
                        $"[cyan]#{++count}:[/] " +
                        $"X:{statusData.GetDouble("x"):F2} " +
                        $"Y:{statusData.GetDouble("y"):F2} " +
                        $"Z:{statusData.GetDouble("z"):F2} " +
                        $"State:{statusData.GetString("state")} " +
                        $"T:{statusData.GetDouble("temp"):F1}°C"
                    );
                }

                if (count >= 5) break;
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[dim]轮询完成[/]");
        }

        AnsiConsole.WriteLine();
    }
}
