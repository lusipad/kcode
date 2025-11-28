using System.Linq;
using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Core.Config;
using Kcode.Core.Transport;
using Kcode.Core.Commands;
using Kcode.Core.UI;
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
    private readonly CommandParser _parser;
    private readonly CommandExecutor _executor;
    private readonly CommandHistory _history;
    private readonly CommandCompleter _completer;
    private readonly string _mutedTextMarkup;
    private readonly string _dividerMarkup;
    private readonly string _ghostTextMarkup;
    private readonly List<SlashCommandEntry> _slashCommandEntries;
    private readonly List<SlashCommandEntry> _activeSlashSuggestions = new();
    private int _slashSelectionIndex = -1;
    private int _slashSuggestionWindowStart = 0;
    private const int SlashSuggestionDisplayCount = 8;

    public ReplEngine(RootConfig config, ITransport transport)
    {
        _config = config;
        _transport = transport;

        // 初始化组件
        _dataContext = new DataContext(config);
        _bindingEngine = new BindingEngine(transport, _dataContext, config);
        _layoutEngine = new LayoutEngine(config, _dataContext);
        _parser = new CommandParser(config);
        _executor = new CommandExecutor(transport, config);
        _history = new CommandHistory(".kcode_history", 1000);
        _completer = new CommandCompleter(config, _history);
        _mutedTextMarkup = ThemeHelper.GetColorMarkup(config, "#8E8EA0", "theme", "colors", "muted_text");
        _dividerMarkup = ThemeHelper.GetColorMarkup(config, "#4A4A58", "theme", "colors", "divider");
        _ghostTextMarkup = ThemeHelper.GetColorMarkup(config, "#6B6B7B", "theme", "colors", "prompt_ghost");
        _slashCommandEntries = BuildSlashCommandEntries(config);
        UpdateSlashSuggestions(string.Empty);

        // 初始化元数据
        InitializeMetaData();
    }

    /// <summary>
    /// 启动 REPL
    /// </summary>
    public async Task RunAsync()
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive || Console.IsOutputRedirected)
        {
            AnsiConsole.MarkupLine("[yellow]检测到非交互式终端，请在 PowerShell / Windows Terminal 等交互式环境中运行。[/]");
            return;
        }

        try
        {
            if (AnsiConsole.Profile.Capabilities.Interactive && !Console.IsOutputRedirected)
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
        Console.CursorVisible = false;

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
                Console.SetCursorPosition(0, startRow + row);

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
                Console.SetCursorPosition(0, startRow + row);
                var color = gradientColors[row];
                AnsiConsole.Markup($"[bold {color}]{logo[row]}[/]");
            }
            await Task.Delay(100);

            // 普通
            for (int row = 0; row < logo.Length; row++)
            {
                Console.SetCursorPosition(0, startRow + row);
                var color = gradientColors[row];
                AnsiConsole.Markup($"[{color}]{logo[row]}[/]");
            }
            await Task.Delay(100);
        }

        var slogan = string.IsNullOrWhiteSpace(_config.App.Slogan)
            ? "现代化命令行 CNC 控制终端"
            : _config.App.Slogan;

        // 显示版本信息（带渐变色）
        Console.SetCursorPosition(0, startRow + logo.Length + 1);
        AnsiConsole.MarkupLine($"[dim]        {Markup.Escape(slogan)}[/] [yellow]v{_config.App.Version}[/]");

        Console.SetCursorPosition(0, startRow + logo.Length + 2);
        AnsiConsole.MarkupLine($"[dim]        传输: [/][dodgerblue1]{_config.Transport.Type}[/] [green]✓ Ready[/]");

        await Task.Delay(300);
        Console.CursorVisible = true;
    }

    /// <summary>
    /// 主循环 - 使用 Live 模式实时刷新界面
    /// </summary>
    private async Task MainLoopAsync()
    {
        var inputBuffer = new StringBuilder();
        var historyPanels = new List<IRenderable>();
        var currentSuggestion = string.Empty;
        bool exitRequested = false;

        while (!exitRequested)
        {
            bool requestFullScreen = false;
            IRenderable? fullScreenRenderable = null;
            string? fullScreenOutput = null;
            string? fullScreenCommand = null;
            bool inputChanged = false;

            var layout = BuildLayout();
            RefreshLayout(layout, inputBuffer.ToString(), currentSuggestion, historyPanels);

            try
            {
                await AnsiConsole.Live(layout)
                    .AutoClear(false)
                    .StartAsync(async ctx =>
                    {
                        bool isRunning = true;

                        while (isRunning)
                        {
                            bool needsImmediateRefresh = false;
                            bool breakKeyReading = false;

                            while (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);

                                if (key.Key == ConsoleKey.Enter)
                                {
                                    if (HandleSlashEnter(inputBuffer))
                                    {
                                        inputChanged = true;
                                        continue;
                                    }

                                    var cmd = inputBuffer.ToString().Trim();
                                    inputBuffer.Clear();
                                    currentSuggestion = string.Empty;
                                    _history.ResetNavigation();
                                    _completer.Reset();
                                    inputChanged = true;

                                    if (!string.IsNullOrWhiteSpace(cmd))
                                    {
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
                                    }
                                }
                                else if (key.Key == ConsoleKey.UpArrow)
                                {
                                    if (MoveSlashSelection(-1))
                                    {
                                        needsImmediateRefresh = true;
                                        breakKeyReading = true;
                                    }
                                    else
                                    {
                                        var previousCmd = _history.NavigateUp();
                                        if (previousCmd != null)
                                        {
                                            inputBuffer.Clear();
                                            inputBuffer.Append(previousCmd);
                                            currentSuggestion = string.Empty;
                                            inputChanged = true;
                                        }
                                    }
                                }
                                else if (key.Key == ConsoleKey.DownArrow)
                                {
                                    if (MoveSlashSelection(1))
                                    {
                                        needsImmediateRefresh = true;
                                        breakKeyReading = true;
                                    }
                                    else
                                    {
                                        var nextCmd = _history.NavigateDown();
                                        if (nextCmd != null)
                                        {
                                            inputBuffer.Clear();
                                            if (!string.IsNullOrEmpty(nextCmd))
                                            {
                                                inputBuffer.Append(nextCmd);
                                            }
                                            currentSuggestion = string.Empty;
                                            inputChanged = true;
                                        }
                                    }
                                }
                                else if (key.Key == ConsoleKey.Tab)
                                {
                                    if (TryApplySlashSuggestion(inputBuffer))
                                    {
                                        currentSuggestion = string.Empty;
                                        inputChanged = true;
                                        continue;
                                    }

                                    if (!string.IsNullOrEmpty(currentSuggestion))
                                    {
                                        inputBuffer.Clear();
                                        inputBuffer.Append(currentSuggestion);
                                        currentSuggestion = string.Empty;
                                        inputChanged = true;
                                    }
                                }
                                else if (key.Key == ConsoleKey.Backspace)
                                {
                                    if (inputBuffer.Length > 0)
                                    {
                                        inputBuffer.Length--;
                                        _history.ResetNavigation();
                                        _completer.Reset();
                                        currentSuggestion = _completer.GetBestSuggestion(inputBuffer.ToString()) ?? string.Empty;
                                        inputChanged = true;
                                    }
                                }
                                else if (key.KeyChar != '\0')
                                {
                                    inputBuffer.Append(key.KeyChar);
                                    _history.ResetNavigation();
                                    _completer.Reset();
                                    currentSuggestion = _completer.GetBestSuggestion(inputBuffer.ToString()) ?? string.Empty;
                                    inputChanged = true;
                                }

                                if (breakKeyReading)
                                {
                                    break;
                                }
                            }

                            if (inputChanged)
                            {
                                UpdateSlashSuggestions(inputBuffer.ToString());
                                inputChanged = false;
                                needsImmediateRefresh = true;
                            }

                            if (needsImmediateRefresh)
                            {
                                RefreshLayout(layout, inputBuffer.ToString(), currentSuggestion, historyPanels);
                                ctx.Refresh();
                                needsImmediateRefresh = false;
                                continue;
                            }

                            RefreshLayout(layout, inputBuffer.ToString(), currentSuggestion, historyPanels);
                            ctx.Refresh();

                            if (!isRunning)
                            {
                                break;
                            }

                            await Task.Delay(16);
                        }
                    });
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Live 模式不可用：{Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[yellow]提示：请在真实的交互式终端（PowerShell/Windows Terminal）中运行[/]");
                break;
            }

            if (requestFullScreen)
            {
                ShowFullScreenContent(fullScreenRenderable, fullScreenOutput);
                if (!string.IsNullOrEmpty(fullScreenCommand))
                {
                    historyPanels.Add(new Markup(Muted($"{fullScreenCommand} 输出已全屏显示")));
                    TrimHistory(historyPanels);
                }
                continue;
            }

            break;
        }
    }
    /// <summary>
    /// 构建布局 - 完全参考 Claude Code 风格
    /// </summary>
    private Layout BuildLayout()
    {
        var inputArea = new Layout("input_area")
            .Size(SlashSuggestionDisplayCount + 2 + 3);

        inputArea.SplitRows(
            new Layout("slash_suggestions").Size(SlashSuggestionDisplayCount + 2),
            new Layout("prompt").Size(3)
        );

        return new Layout("root")
            .SplitRows(
                new Layout("header").Size(8),
                new Layout("body").Ratio(1),
                inputArea,
                new Layout("footer").Size(3)
            );
    }

    /// <summary>
    /// 刷新布局
    /// </summary>
    private void RefreshLayout(Layout layout, string input, string suggestion, List<IRenderable> history)
    {
        layout["header"].Update(BuildHeader());
        layout["body"].Update(BuildHistoryPanel(history));
        layout["slash_suggestions"].Update(BuildSlashSuggestionPanel());
        layout["prompt"].Update(BuildPrompt(input, suggestion));
        layout["footer"].Update(BuildFooter());
    }

    /// <summary>
    /// 构建 Header - 简洁文字布局
    /// </summary>
    private IRenderable BuildHeader()
    {
        var app = _config.App;
        var welcome = string.IsNullOrWhiteSpace(app.HeaderWelcome) ? "欢迎回来！" : app.HeaderWelcome;
        var model = string.IsNullOrWhiteSpace(app.Model) ? null : app.Model;
        var usage = string.IsNullOrWhiteSpace(app.Usage) ? null : app.Usage;
        var workingDir = string.IsNullOrWhiteSpace(app.WorkingDirectory) ? "D:\\Repos\\kcode" : app.WorkingDirectory;
        var slogan = string.IsNullOrWhiteSpace(app.Slogan) ? "现代化命令行 CNC 控制终端" : app.Slogan;

        // 左侧：kcode 字样
        var leftColumn = new Rows(
            new Markup($"[bold dodgerblue1]kcode[/] [dim]v{_config.App.Version}[/]"),
            new Text(""),
            new Markup($"[dim]{Markup.Escape(slogan)}[/]")
        );

        // 右侧：欢迎语 + 信息
        var rightElements = new List<IRenderable>
        {
            new Markup($"[dim]{Markup.Escape(welcome)}[/]"),
            new Text("")
        };

        if (!string.IsNullOrWhiteSpace(model) || !string.IsNullOrWhiteSpace(usage))
        {
            var label = string.Join(" · ", new[]
            {
                model,
                usage
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            if (!string.IsNullOrWhiteSpace(label))
            {
                rightElements.Add(new Markup($"[dim]{Markup.Escape(label)}[/]"));
            }
        }

        rightElements.Add(new Markup($"[yellow]{_config.Transport.Type}[/] [dim]transport[/]"));
        rightElements.Add(new Markup($"[dim]{Markup.Escape(workingDir)}[/]"));

        var rightColumn = new Rows(rightElements.ToArray());

        // 使用 Grid 创建左右分割
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(30));  // 左列宽度
        grid.AddColumn(new GridColumn().Width(2).PadLeft(1).PadRight(1));  // 分割线列
        grid.AddColumn(new GridColumn());  // 右列

        var dividerLines = string.Join("\n", Enumerable.Repeat(Divider("│"), 5));

        grid.AddRow(
            leftColumn,
            new Markup(dividerLines),
            rightColumn
        );

        return new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey30),
            Padding = new Padding(2, 0, 2, 0)
        };
    }

    /// <summary>
    /// 构建历史面板
    /// </summary>
    private IRenderable BuildHistoryPanel(List<IRenderable> history)
    {
        if (history.Count == 0)
        {
            return new Panel(new Markup("[dim]命令输出将显示在这里...[/]"))
            {
                Border = BoxBorder.None
            };
        }

        var rows = new Rows(history.ToArray());
        return new Panel(rows)
        {
            Border = BoxBorder.None
        };
    }

    /// <summary>
    /// 构建输入提示 - 带灰色上下边框 + 虚位补全
    /// </summary>
    private IRenderable BuildPrompt(string input, string suggestion)
    {
        // 构建输入内容
        string displayText;
        var ghostBar = Ghost("│");
        if (string.IsNullOrEmpty(input))
        {
            // 空输入时显示提示文字
            var placeholder = string.IsNullOrWhiteSpace(_config.App.PromptPlaceholder)
                ? "[dim]/model to try Opus 4.5[/]"
                : $"[dim]{Markup.Escape(_config.App.PromptPlaceholder)}[/]";
            displayText = placeholder;
        }
        else
        {
            // 有输入时显示内容
            var displayInput = input.Length > 120 ? input.Substring(input.Length - 120) : input;

            // 添加虚位补全预览
            if (!string.IsNullOrEmpty(suggestion) && suggestion.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                // 提取补全的部分（去掉已输入的前缀）
                var completionPart = suggestion.Substring(input.Length);
                displayText = $"{Markup.Escape(displayInput)}[dim]{Markup.Escape(completionPart)}[/]{ghostBar}";
            }
            else
            {
                displayText = $"{Markup.Escape(displayInput)}{ghostBar}";
            }
        }

        // Claude Code 风格：极简提示符
        var content = new Markup($"{Ghost(">")} {displayText}");

        // 带灰色边框（只有上下边框）
        return new Panel(content)
        {
            Border = BoxBorder.Heavy,
            BorderStyle = new Style(Color.Grey30),
            Padding = new Padding(1, 0, 1, 0),
            Expand = true
        };
    }

    /// <summary>
    /// 构建页脚 - 多行状态栏 + 徽章（带emoji图标）
    /// </summary>
    private IRenderable BuildFooter()
    {
        var statusBar = _layoutEngine.RenderStatusBar();
        var badges = _layoutEngine.RenderFooterBadges();

        return new Rows(statusBar, badges);
    }

    private IRenderable BuildSlashSuggestionPanel()
    {
        var visibleEntries = GetVisibleSlashEntries();

        if (visibleEntries.Count == 0)
        {
            return new Panel(new Markup("[dim]/ 输入可浏览命令列表[/]"))
            {
                Border = BoxBorder.None,
                Padding = new Padding(1, 0, 1, 0),
                Expand = true
            };
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey37)
            .HideHeaders()
            .AddColumn(new TableColumn("[dim]命令[/]").Width(24))
            .AddColumn(new TableColumn("[dim]说明[/]"));

        for (int i = 0; i < visibleEntries.Count; i++)
        {
            var entry = visibleEntries[i];
            var absoluteIndex = Math.Min(_slashSuggestionWindowStart + i, _activeSlashSuggestions.Count - 1);
            var isSelected = absoluteIndex == _slashSelectionIndex;

            var nameMarkup = isSelected
                ? $"[bold yellow]{Markup.Escape(entry.Name)}[/]"
                : $"[cyan]{Markup.Escape(entry.Name)}[/]";
            var descMarkup = string.IsNullOrWhiteSpace(entry.Description)
                ? "[dim]无说明[/]"
                : Markup.Escape(entry.Description);
            table.AddRow(nameMarkup, descMarkup);
        }

        var panel = new Panel(table)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey37),
            Padding = new Padding(1, 0, 1, 0),
            Expand = true
        };

        if (_activeSlashSuggestions.Count > SlashSuggestionDisplayCount)
        {
            var start = _slashSuggestionWindowStart + 1;
            var end = _slashSuggestionWindowStart + visibleEntries.Count;
            var footer = new Markup($"[dim]{start}-{end} / {_activeSlashSuggestions.Count}[/]");
            return new Rows(panel, footer);
        }

        return panel;
    }

    private static void TrimHistory(List<IRenderable> history)
    {
        const int max = 50;
        while (history.Count > max)
        {
            history.RemoveAt(0);
        }
    }

    private void ShowFullScreenContent(IRenderable? renderable, string? fallbackOutput)
    {
        AnsiConsole.Clear();

        if (renderable != null)
        {
            AnsiConsole.Write(renderable);
        }
        else if (!string.IsNullOrWhiteSpace(fallbackOutput))
        {
            AnsiConsole.MarkupLine(fallbackOutput);
        }
        else
        {
            AnsiConsole.MarkupLine(Muted("无输出"));
        }

        AnsiConsole.MarkupLine("\n" + Muted("按任意键返回界面..."));
        Console.ReadKey(true);
    }

    private string Muted(string text) => Colorize(_mutedTextMarkup, text);
    private string Divider(string text) => Colorize(_dividerMarkup, text);
    private string Ghost(string text) => Colorize(_ghostTextMarkup, text);
    private static string Colorize(string colorMarkup, string text) =>
        $"[{colorMarkup}]{Markup.Escape(text)}[/]";

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

    private List<SlashCommandEntry> BuildSlashCommandEntries(RootConfig config)
    {
        var entries = new List<SlashCommandEntry>();

        foreach (var kvp in config.Commands.System)
        {
            entries.Add(new SlashCommandEntry(
                CommandNameHelper.Normalize(kvp.Key),
                string.IsNullOrWhiteSpace(kvp.Value.Description) ? "系统命令" : kvp.Value.Description,
                CommandType.System));
        }

        foreach (var kvp in config.Commands.ApiCommands)
        {
            entries.Add(new SlashCommandEntry(
                CommandNameHelper.Normalize(kvp.Key),
                string.IsNullOrWhiteSpace(kvp.Value.Description) ? "API 命令" : kvp.Value.Description,
                CommandType.Api));
        }

        foreach (var kvp in config.Commands.Macros)
        {
            entries.Add(new SlashCommandEntry(
                CommandNameHelper.Normalize(kvp.Key),
                string.IsNullOrWhiteSpace(kvp.Value.Description) ? "宏命令" : kvp.Value.Description,
                CommandType.Macro));
        }

        return entries
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void UpdateSlashSuggestions(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.TrimStart().StartsWith("/", StringComparison.Ordinal))
        {
            _activeSlashSuggestions.Clear();
            _slashSelectionIndex = -1;
            return;
        }

        var normalized = CommandNameHelper.Normalize(input.Trim());
        var matches = _slashCommandEntries
            .Where(entry => entry.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _activeSlashSuggestions.Clear();
        _activeSlashSuggestions.AddRange(matches);

        if (_activeSlashSuggestions.Count == 0)
        {
            _slashSelectionIndex = -1;
        }
        else if (_slashSelectionIndex < 0 || _slashSelectionIndex >= _activeSlashSuggestions.Count)
        {
            _slashSelectionIndex = 0;
        }

        EnsureSlashSelectionVisible();
    }

    private bool MoveSlashSelection(int delta)
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return false;
        }

        if (_slashSelectionIndex < 0)
        {
            _slashSelectionIndex = delta >= 0 ? 0 : _activeSlashSuggestions.Count - 1;
            EnsureSlashSelectionVisible();
            return true;
        }

        var nextIndex = _slashSelectionIndex + delta;
        if (nextIndex < 0)
        {
            nextIndex = 0;
        }
        else if (nextIndex >= _activeSlashSuggestions.Count)
        {
            nextIndex = _activeSlashSuggestions.Count - 1;
        }

        _slashSelectionIndex = nextIndex;
        EnsureSlashSelectionVisible();
        return true;
    }

    private bool TryApplySlashSuggestion(StringBuilder buffer)
    {
        return ApplySlashCandidate(buffer);
    }

    private bool HandleSlashEnter(StringBuilder buffer)
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return false;
        }

        var trimmed = buffer.ToString().Trim();
        var normalizedInput = string.IsNullOrWhiteSpace(trimmed)
            ? "/"
            : CommandNameHelper.Normalize(trimmed);

        var index = _slashSelectionIndex >= 0 ? _slashSelectionIndex : 0;
        var candidate = _activeSlashSuggestions[index].Name;

        if (string.IsNullOrWhiteSpace(trimmed) ||
            !candidate.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
        {
            ApplySlashCandidate(buffer);
            return true;
        }

        return false;
    }

    private bool ApplySlashCandidate(StringBuilder buffer)
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return false;
        }

        var index = _slashSelectionIndex >= 0 ? _slashSelectionIndex : 0;
        var candidate = _activeSlashSuggestions[index].Name;
        buffer.Clear();
        buffer.Append(candidate);
        buffer.Append(' ');
        _slashSelectionIndex = -1;
        UpdateSlashSuggestions(buffer.ToString());
        return true;
    }

    private IReadOnlyList<SlashCommandEntry> GetVisibleSlashEntries()
    {
        if (_activeSlashSuggestions.Count <= SlashSuggestionDisplayCount)
        {
            _slashSuggestionWindowStart = 0;
            return _activeSlashSuggestions;
        }

        if (_slashSuggestionWindowStart + SlashSuggestionDisplayCount > _activeSlashSuggestions.Count)
        {
            _slashSuggestionWindowStart = _activeSlashSuggestions.Count - SlashSuggestionDisplayCount;
        }

        return _activeSlashSuggestions
            .Skip(_slashSuggestionWindowStart)
            .Take(SlashSuggestionDisplayCount)
            .ToList();
    }

    private void EnsureSlashSelectionVisible()
    {
        if (_activeSlashSuggestions.Count <= SlashSuggestionDisplayCount)
        {
            _slashSuggestionWindowStart = 0;
            return;
        }

        if (_slashSelectionIndex < 0)
        {
            _slashSuggestionWindowStart = 0;
            return;
        }

        if (_slashSelectionIndex < _slashSuggestionWindowStart)
        {
            _slashSuggestionWindowStart = _slashSelectionIndex;
        }
        else if (_slashSelectionIndex >= _slashSuggestionWindowStart + SlashSuggestionDisplayCount)
        {
            _slashSuggestionWindowStart = _slashSelectionIndex - SlashSuggestionDisplayCount + 1;
        }

        var maxStart = _activeSlashSuggestions.Count - SlashSuggestionDisplayCount;
        if (_slashSuggestionWindowStart > maxStart)
        {
            _slashSuggestionWindowStart = maxStart;
        }
        if (_slashSuggestionWindowStart < 0)
        {
            _slashSuggestionWindowStart = 0;
        }
    }

    private record SlashCommandEntry(string Name, string Description, CommandType Type);
}
