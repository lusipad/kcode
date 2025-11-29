using System.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Core.Config;
using Kcode.Core.Input;
using Kcode.Core.Transport;
using Kcode.Core.Commands;
using Kcode.Core.UI;
using Kcode.Core.Terminal;
using Kcode.UI;

namespace Kcode.Core;

/// <summary>
/// REPL 引擎
/// 整合配置驱动架构的所有组件
/// </summary>
public class ReplEngine
{
    private readonly RootConfig _config;
    private readonly ITransport _transport;
    private readonly DataContext _dataContext;
    private readonly BindingEngine _bindingEngine;
    private readonly LayoutEngine _layoutEngine;
    private readonly IReplView _view;
    private readonly ITerminal _terminal;
    private readonly CommandRegistry _commandRegistry;
    private readonly CommandParser _parser;
    private readonly CommandExecutor _executor;
    private readonly CommandHistory _history;
    private readonly CommandCompleter _completer;
    private readonly InputController _inputController;
    private const int SlashSuggestionDisplayCount = 8;

    public ReplEngine(RootConfig config, ITransport transport, ITerminal? terminal = null, IReplView? view = null)
    {
        _config = config;
        _transport = transport;
        _terminal = terminal ?? new SystemConsoleTerminal();

        // 初始化组件
        _dataContext = new DataContext(config);
        _bindingEngine = new BindingEngine(transport, _dataContext, config);
        _layoutEngine = new LayoutEngine(config, _dataContext);
        _view = view ?? new SpectreReplView(config, _layoutEngine, _terminal, SlashSuggestionDisplayCount);
        _commandRegistry = new CommandRegistry(config);
        _parser = new CommandParser(_commandRegistry);
        _executor = new CommandExecutor(transport, config);
        _history = new CommandHistory(".kcode_history", 1000);
        _completer = new CommandCompleter(_commandRegistry, _history);
        _inputController = new InputController(
            _commandRegistry.AllCommands,
            _history,
            _completer,
            SlashSuggestionDisplayCount);

        // 初始化元数据
        InitializeMetaData();
    }

    /// <summary>
    /// 启动 REPL
    /// </summary>
    public async Task RunAsync()
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive || _terminal.IsOutputRedirected)
        {
            AnsiConsole.MarkupLine("[yellow]检测到非交互式终端，请在 PowerShell / Windows Terminal 等交互式环境中运行。[/]");
            return;
        }

        try
        {
            if (AnsiConsole.Profile.Capabilities.Interactive && !_terminal.IsOutputRedirected)
            {
                try
                {
                    await ShowStartupAnimation();
                }
                catch (IOException)
                {
                    // 非交互式终端不支持清屏/定位，忽略启动动画
                }
                catch (InvalidOperationException)
                {
                    // 同上
                }
            }

            // 连接传输层
            await _transport.ConnectAsync();

            // 启动数据绑定
            await _bindingEngine.StartAsync();

            // 主循环（使用 Live 模式显示完整界面）
            await MainLoopAsync();
        }
        finally
        {
            // 清理资源
            await _bindingEngine.StopAsync();
            await _transport.DisconnectAsync();
        }
    }

    /// <summary>
    /// 显示启动动画 - 纯 ASCII 扫描效果（1秒）带颜色渐变
    /// </summary>
    private async Task ShowStartupAnimation()
    {
        AnsiConsole.Clear();
        _terminal.SetCursorVisible(false);

        // ASCII Logo
        var logo = new[]
        {
            "  ██╗  ██╗  ██████╗  ██████╗  ██████╗  ███████╗",
            "  ██║ ██╔╝ ██╔════╝ ██╔═══██╗ ██╔══██╗ ██╔════╝",
            "  █████╔╝  ██║      ██║   ██║ ██║  ██║ █████╗  ",
            "  ██╔═██╗  ██║      ██║   ██║ ██║  ██║ ██╔══╝  ",
            "  ██║  ██╗ ╚██████╗ ╚██████╔╝ ██████╔╝ ███████╗",
            "  ╚═╝  ╚═╝  ╚═════╝  ╚═════╝  ╚═════╝  ╚══════╝"
        };

        // 渐变色方案：赛博朋克风格（紫红 → 蓝 → 青）
        var gradientColors = new[] { "magenta", "purple", "mediumorchid", "dodgerblue1", "deepskyblue1", "cyan" };

        var startRow = 3;
        var maxWidth = logo[0].Length;

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // 快速扫描效果 - 从左到右逐列显示，带颜色渐变和拖尾光晕
        for (int col = 0; col < maxWidth; col += 2)
        {
            for (int row = 0; row < logo.Length; row++)
            {
                _terminal.SetCursorPosition(0, startRow + row);

                // 根据行数选择渐变颜色
                var color = gradientColors[row];

                // 显示已扫描的部分
                var currentLength = Math.Min(col + 2, logo[row].Length);
                var visiblePart = logo[row].Substring(0, currentLength);

                // 扫描线拖尾效果：最新的6个字符更亮
                if (currentLength > 6)
                {
                    var solidPart = logo[row].Substring(0, currentLength - 6);
                    var glowPart = logo[row].Substring(currentLength - 6, 6);
                    AnsiConsole.Markup($"[{color}]{solidPart}[/][bold {color}]{glowPart}[/]");
                }
                else
                {
                    AnsiConsole.Markup($"[bold {color}]{visiblePart}[/]");
                }
            }

            await Task.Delay(8); // 每列 8ms，总计约 200ms
        }

        await Task.Delay(100);

        // 闪烁高亮效果 - 保持渐变
        for (int i = 0; i < 2; i++)
        {
            // 高亮
            for (int row = 0; row < logo.Length; row++)
            {
                _terminal.SetCursorPosition(0, startRow + row);
                var color = gradientColors[row];
                AnsiConsole.Markup($"[bold {color}]{logo[row]}[/]");
            }
            await Task.Delay(100);

            // 普通
            for (int row = 0; row < logo.Length; row++)
            {
                _terminal.SetCursorPosition(0, startRow + row);
                var color = gradientColors[row];
                AnsiConsole.Markup($"[{color}]{logo[row]}[/]");
            }
            await Task.Delay(100);
        }

        var slogan = string.IsNullOrWhiteSpace(_config.App.Slogan)
            ? "现代化命令行 CNC 控制终端"
            : _config.App.Slogan;

        // 显示版本信息（带渐变色）
        _terminal.SetCursorPosition(0, startRow + logo.Length + 1);
        AnsiConsole.MarkupLine($"[dim]        {Markup.Escape(slogan)}[/] [yellow]v{_config.App.Version}[/]");

        _terminal.SetCursorPosition(0, startRow + logo.Length + 2);
        AnsiConsole.MarkupLine($"[dim]        传输: [/][dodgerblue1]{_config.Transport.Type}[/] [green]✓ Ready[/]");

        await Task.Delay(300);
        _terminal.SetCursorVisible(true);
    }

    /// <summary>
    /// 主循环 - 使用 Live 模式实时刷新界面
    /// </summary>
    private async Task MainLoopAsync()
    {
        var historyPanels = new List<IRenderable>();
        bool exitRequested = false;

        while (!exitRequested)
        {
            bool requestFullScreen = false;
            IRenderable? fullScreenRenderable = null;
            string? fullScreenOutput = null;
            string? fullScreenCommand = null;

            await using var viewSession = _view.BeginSession(CreateViewState(historyPanels));
            await viewSession.StartAsync();

            bool isRunning = true;
            while (isRunning)
            {
                bool needsRefresh = false;

                while (_terminal.KeyAvailable)
                {
                    var key = _terminal.ReadKey(true);
                    var inputResult = _inputController.HandleKey(key);

                    if (inputResult.CommandSubmitted && !string.IsNullOrWhiteSpace(inputResult.CommandText))
                    {
                        var cmd = inputResult.CommandText;
                        historyPanels.Add(new Markup($"[cyan]> {Markup.Escape(cmd)}[/]"));
                        TrimHistory(historyPanels);

                        var command = _parser.Parse(cmd);
                        if (command != null)
                        {
                            var result = await _executor.ExecuteAsync(command);

                            if (result.RequiresFullScreen)
                            {
                                requestFullScreen = true;
                                fullScreenRenderable = result.Renderable;
                                fullScreenOutput = string.IsNullOrEmpty(result.Output) ? null : result.Output;
                                fullScreenCommand = cmd;
                                isRunning = false;
                                break;
                            }

                            if (result.ShouldExit)
                            {
                                if (result.Renderable != null)
                                {
                                    historyPanels.Add(result.Renderable);
                                }
                                else if (!string.IsNullOrEmpty(result.Output))
                                {
                                    historyPanels.Add(new Markup(result.Output));
                                }
                                TrimHistory(historyPanels);
                                exitRequested = true;
                                isRunning = false;
                                break;
                            }

                            if (result.ShouldClear)
                            {
                                historyPanels.Clear();
                            }
                            else if (result.Renderable != null)
                            {
                                historyPanels.Add(result.Renderable);
                            }
                            else if (!string.IsNullOrEmpty(result.Output))
                            {
                                historyPanels.Add(new Markup(result.Output));
                            }

                            TrimHistory(historyPanels);
                        }
                        else
                        {
                            historyPanels.Add(new Markup("[red]? 未知命令[/]"));
                            TrimHistory(historyPanels);
                        }

                        _history.Add(cmd);
                        needsRefresh = true;
                    }

                    if (inputResult.NeedsRefresh)
                    {
                        needsRefresh = true;
                    }

                    if (inputResult.ShouldBreakKeyReading)
                    {
                        break;
                    }
                }

                if (needsRefresh)
                {
                    viewSession.Update(CreateViewState(historyPanels));
                    await viewSession.RequestRefreshAsync();
                }

                if (!needsRefresh)
                {
                    await Task.Delay(16);
                }
            }

            if (requestFullScreen)
            {
                while (_terminal.KeyAvailable)
                {
                    _terminal.ReadKey(true);
                }
                viewSession.DisplayFullScreen(fullScreenRenderable, fullScreenOutput);
                if (!string.IsNullOrEmpty(fullScreenCommand))
                {
                    historyPanels.Add(new Markup($"[dim]{fullScreenCommand} 输出已全屏显示[/]"));
                    TrimHistory(historyPanels);
                }
                continue;
            }

            break;
        }
    }
    private ReplViewState CreateViewState(List<IRenderable> history) =>
        new(
            history,
            _inputController.InputText,
            _inputController.CurrentSuggestion,
            _inputController.GetSlashViewState());

    private static void TrimHistory(List<IRenderable> history)
    {
        const int max = 50;
        while (history.Count > max)
        {
            history.RemoveAt(0);
        }
    }

    /// <summary>
    /// 初始化元数据
    /// </summary>
    private void InitializeMetaData()
    {
        _dataContext.Set("meta", new Dictionary<string, object?>
        {
            ["model"] = _config.App.Model,
            ["workspace"] = _config.App.Workspace,
            ["branch"] = _config.App.Branch,
            ["tokens"] = _config.App.Tokens,
            ["permissions"] = _config.App.Permissions
        });

        // 初始化状态数据（默认值）
        _dataContext.Set("status", new Dictionary<string, object?>
        {
            ["x"] = 0.0,
            ["y"] = 0.0,
            ["z"] = 0.0,
            ["feed"] = 0.0,
            ["speed"] = 0.0,
            ["state"] = "IDLE",
            ["alarm"] = "",
            ["temp"] = 25.0
        });
    }

}
