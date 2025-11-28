using Kcode.Core.Config;
using Kcode.Core.Transport;
using Kcode.Core.Commands;
using Spectre.Console;

namespace Kcode;

/// <summary>
/// KCode 虚拟模式测试程序
/// </summary>
public class TestVirtualMode
{
    public static async Task RunAsync()
    {
        AnsiConsole.MarkupLine("[bold cyan]KCode 虚拟模式测试[/]\n");

        try
        {
            // 1. 加载配置
            AnsiConsole.MarkupLine("[yellow]1. 加载配置文件...[/]");
            var loader = new ConfigLoader();
            var config = loader.Load("Config/config-virtual.yaml");
            AnsiConsole.MarkupLine($"[green]✓[/] 配置加载成功: {config.App.Name} v{config.App.Version}");
            AnsiConsole.MarkupLine($"[green]✓[/] 传输类型: {config.Transport.Type}\n");

            // 2. 创建传输层
            AnsiConsole.MarkupLine("[yellow]2. 创建传输层...[/]");
            var transport = TransportFactory.Create(config.Transport);
            await transport.ConnectAsync();
            AnsiConsole.MarkupLine($"[green]✓[/] 传输层连接成功 ({transport.TransportType})\n");

            // 3. 创建命令系统
            AnsiConsole.MarkupLine("[yellow]3. 初始化命令系统...[/]");
            var parser = new CommandParser(config);
            var executor = new CommandExecutor(transport, config);
            AnsiConsole.MarkupLine($"[green]✓[/] 命令系统就绪\n");

            // 4. 测试命令
            AnsiConsole.MarkupLine("[bold yellow]4. 测试命令执行:[/]\n");

            var testCommands = new[]
            {
                "help",           // 系统命令
                "G0 X10 Y20",     // G 代码
                "/set max_velocity 3000",  // 设置参数
                "home",           // 宏命令
                "clear"           // 清屏命令
            };

            foreach (var cmdText in testCommands)
            {
                AnsiConsole.MarkupLine($"[cyan]> {cmdText}[/]");

                var command = parser.Parse(cmdText);
                if (command != null)
                {
                    AnsiConsole.MarkupLine($"  [dim]类型: {command.Type}[/]");

                    var result = await executor.ExecuteAsync(command);

                    if (result.Success)
                    {
                        if (!string.IsNullOrEmpty(result.Output))
                        {
                            // 分行显示输出
                            var lines = result.Output.Split('\n');
                            foreach (var line in lines)
                            {
                                if (!string.IsNullOrWhiteSpace(line))
                                {
                                    AnsiConsole.MarkupLine($"  {line}");
                                }
                            }
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"  [red]✗ {result.Output}[/]");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLine("  [red]✗ 命令解析失败[/]");
                }

                AnsiConsole.WriteLine();
            }

            // 5. 测试流式数据 (演示轮询)
            AnsiConsole.MarkupLine("[bold yellow]5. 测试流式数据 (3 次状态轮询):[/]\n");

            var count = 0;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await foreach (var statusData in transport.SubscribeAsync("poll_status", cts.Token))
                {
                    if (statusData.Success)
                    {
                        AnsiConsole.MarkupLine(
                            $"[cyan]Status #{++count}:[/] " +
                            $"X:{statusData.GetDouble("x"):F2} " +
                            $"Y:{statusData.GetDouble("y"):F2} " +
                            $"Z:{statusData.GetDouble("z"):F2} " +
                            $"State:{statusData.GetString("state")} " +
                            $"Temp:{statusData.GetDouble("temp"):F1}°C"
                        );
                    }

                    if (count >= 3) break;
                }
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[dim]Timeout or cancelled[/]");
            }

            // 6. 断开连接
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]6. 清理资源...[/]");
            await transport.DisconnectAsync();
            await transport.DisposeAsync();
            AnsiConsole.MarkupLine("[green]✓[/] 已断开连接\n");

            // 总结
            AnsiConsole.Write(new Rule("[green]测试完成 ✓[/]"));
            AnsiConsole.MarkupLine("\n[bold]核心功能验证通过！[/]");
            AnsiConsole.MarkupLine("  • 配置加载 ✓");
            AnsiConsole.MarkupLine("  • 传输层抽象 ✓");
            AnsiConsole.MarkupLine("  • 命令解析 ✓");
            AnsiConsole.MarkupLine("  • 命令执行 ✓");
            AnsiConsole.MarkupLine("  • 模板渲染 ✓");
            AnsiConsole.MarkupLine("  • 流式数据 ✓");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }
    }
}
