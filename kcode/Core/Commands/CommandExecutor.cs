using Kcode.Core.Config;
using Kcode.Core.Template;
using Kcode.Core.Transport;
using Kcode.UI;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Kcode.Core.Commands;

/// <summary>
/// 命令执行器
/// 支持 builtin、api 和 macro 命令
/// </summary>
public class CommandExecutor
{
    private readonly ITransport _transport;
    private readonly TemplateEngine _templateEngine;
    private readonly RootConfig _config;
    private readonly string _secondaryTextMarkup;

    public CommandExecutor(ITransport transport, RootConfig config)
    {
        _transport = transport;
        _config = config;
        _templateEngine = new TemplateEngine();
        _secondaryTextMarkup = ThemeHelper.GetColorMarkup(config, "#8E8EA0", "theme", "colors", "muted_text");
    }

    /// <summary>
    /// 执行命令
    /// </summary>
    public async Task<CommandExecutionResult> ExecuteAsync(
        ParsedCommand command,
        CancellationToken ct = default)
    {
        try
        {
            return command.Type switch
            {
                CommandType.System => await ExecuteSystemCommandAsync(command, ct),
                CommandType.Api => await ExecuteApiCommandAsync(command, ct),
                CommandType.Macro => await ExecuteMacroCommandAsync(command, ct),
                CommandType.Unknown => CreateErrorResult($"Unknown command: {command.Input}"),
                _ => CreateErrorResult("Invalid command type")
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Command execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行系统命令
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteSystemCommandAsync(
        ParsedCommand command,
        CancellationToken ct)
    {
        if (command.Config == null)
        {
            return CreateErrorResult("System command config not found");
        }

        var action = command.Config.Action;

        // 解析 action: builtin:action_name
        if (action.StartsWith("builtin:"))
        {
            var actionName = action["builtin:".Length..];
            return await ExecuteBuiltinActionAsync(actionName, command, ct);
        }

        return CreateErrorResult($"Unknown action: {action}");
    }

    /// <summary>
    /// 执行内置动作
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteBuiltinActionAsync(
        string actionName,
        ParsedCommand command,
        CancellationToken ct)
    {
        return actionName.ToLower() switch
        {
            "help" => await Task.FromResult(ExecuteHelp()),
            "exit" => await Task.FromResult(ExecuteExit()),
            "clear" => await Task.FromResult(ExecuteClear()),
            "status_panel" => await Task.FromResult(ExecuteStatusPanel()),
            _ => CreateErrorResult($"Unknown builtin action: {actionName}")
        };
    }

    /// <summary>
    /// 执行 API 命令
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteApiCommandAsync(
        ParsedCommand command,
        CancellationToken ct)
    {
        if (command.ApiConfig == null)
        {
            return CreateErrorResult("API command config not found");
        }

        var endpoint = command.ApiConfig.Endpoint;

        // 调用传输层
        var response = await _transport.InvokeAsync(endpoint, command.Parameters, ct);

        if (!response.Success)
        {
            return CreateErrorResult(response.ErrorMessage ?? "API call failed");
        }

        // 渲染响应模板
        var output = RenderResponseTemplate(command.ApiConfig, response);

        return new CommandExecutionResult
        {
            Success = true,
            Output = output,
            Data = response.Data
        };
    }

    /// <summary>
    /// 执行宏命令
    /// </summary>
    private async Task<CommandExecutionResult> ExecuteMacroCommandAsync(
        ParsedCommand command,
        CancellationToken ct)
    {
        if (command.MacroConfig == null)
        {
            return CreateErrorResult("Macro command config not found");
        }

        var results = new List<TransportResponse>();

        // 执行所有步骤
        foreach (var step in command.MacroConfig.Steps)
        {
            var response = await _transport.InvokeAsync(
                step.Endpoint,
                step.Request.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
                ct);

            results.Add(response);

            if (!response.Success)
            {
                return CreateErrorResult($"Macro step failed: {response.ErrorMessage}");
            }
        }

        // 渲染响应模板
        var lastResponse = results.Last();
        var output = _templateEngine.Render(
            command.MacroConfig.ResponseTemplate,
            lastResponse.Data);

        return new CommandExecutionResult
        {
            Success = true,
            Output = output,
            Data = lastResponse.Data
        };
    }

    /// <summary>
    /// 渲染响应模板
    /// </summary>
    private string RenderResponseTemplate(ApiCommandConfig config, TransportResponse response)
    {
        if (!string.IsNullOrEmpty(config.ResponseTemplate))
        {
            return _templateEngine.Render(config.ResponseTemplate, response.Data);
        }

        // 默认响应格式
        if (response.Success)
        {
            return response.GetString("message", "Command executed successfully");
        }
        else
        {
            return $"[red]Error:[/] {response.ErrorMessage}";
        }
    }

    #region 内置命令实现

    private CommandExecutionResult ExecuteHelp()
    {
        var sections = new List<IRenderable>();

        // 标题
        sections.Add(new Rule("[bold dodgerblue1]📖 帮助信息[/]")
        {
            Justification = Justify.Left,
            Style = Style.Parse("dodgerblue1")
        });

        sections.Add(Text.NewLine);

        // 系统命令表格
        if (_config.Commands.System.Any())
        {
            var systemTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey50)
                .AddColumn(new TableColumn("[yellow]💻 系统命令[/]").Width(15))
                .AddColumn(new TableColumn("[dim]说明[/]"));

            foreach (var kvp in _config.Commands.System.OrderBy(x => x.Key))
            {
                var commandName = CommandNameHelper.Normalize(kvp.Key);
                systemTable.AddRow(
                    $"[cyan]{commandName}[/]",
                    FormatDescription(kvp.Value.Description)
                );
            }

            sections.Add(systemTable);
            sections.Add(Text.NewLine);
        }

        // API 命令表格
        if (_config.Commands.ApiCommands.Any())
        {
            var apiTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey50)
                .AddColumn(new TableColumn("[yellow]⚡ API 命令[/]").Width(15))
                .AddColumn(new TableColumn("[dim]说明[/]"))
                .AddColumn(new TableColumn("[dim]端点[/]").NoWrap());

            foreach (var kvp in _config.Commands.ApiCommands.OrderBy(x => x.Key))
            {
                var commandName = CommandNameHelper.Normalize(kvp.Key);
                apiTable.AddRow(
                    $"[cyan]{commandName}[/]",
                    FormatDescription(kvp.Value.Description),
                    SecondaryText(kvp.Value.Endpoint ?? string.Empty)
                );
            }

            sections.Add(apiTable);
            sections.Add(Text.NewLine);
        }

        // 宏命令表格
        if (_config.Commands.Macros.Any())
        {
            var macroTable = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey50)
                .AddColumn(new TableColumn("[yellow]🔧 宏命令[/]").Width(15))
                .AddColumn(new TableColumn("[dim]说明[/]"))
                .AddColumn(new TableColumn("[dim]步骤数[/]").Width(10));

            foreach (var kvp in _config.Commands.Macros.OrderBy(x => x.Key))
            {
                var commandName = CommandNameHelper.Normalize(kvp.Key);
                macroTable.AddRow(
                    $"[cyan]{commandName}[/]",
                    FormatDescription(kvp.Value.Description),
                    SecondaryText(kvp.Value.Steps.Count.ToString())
                );
            }

            sections.Add(macroTable);
            sections.Add(Text.NewLine);
        }

        // 提示信息
        var tipsPanel = new Panel(new Markup(
            "[dim]💡 提示:[/]\n" +
            "  • 使用 [cyan]Tab[/] 键自动补全命令\n" +
            "  • 使用 [cyan]↑ ↓[/] 键浏览历史命令\n" +
            "  • 输入命令时会显示[dim]虚位补全预览[/]\n" +
            "  • 输入 [cyan]help <命令名>[/] 查看详细帮助 [dim](未来)[/]"
        ))
        {
            Header = new PanelHeader("[dodgerblue1]使用技巧[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("grey50"),
            Padding = new Padding(2, 1, 2, 1)
        };

        sections.Add(tipsPanel);

        // 返回可渲染对象，避免预渲染导致的 markup 错误
        return new CommandExecutionResult
        {
            Success = true,
            Renderable = new Rows(sections),
            RequiresFullScreen = true
        };
    }

    private CommandExecutionResult ExecuteExit()
    {
        return new CommandExecutionResult
        {
            Success = true,
            ShouldExit = true,
            Output = "[green]Goodbye![/]"
        };
    }

    private CommandExecutionResult ExecuteClear()
    {
        return new CommandExecutionResult
        {
            Success = true,
            ShouldClear = true,
            Output = ""
        };
    }

    private CommandExecutionResult ExecuteStatusPanel()
    {
        // 创建美化的状态面板
        var statusPanel = BuildBeautifulStatusPanel();

        return new CommandExecutionResult
        {
            Success = true,
            Renderable = statusPanel
        };
    }

    private IRenderable BuildBeautifulStatusPanel()
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(20));
        grid.AddColumn(new GridColumn());

        // 坐标位置
        grid.AddRow(
            new Markup("[yellow]📍 坐标位置[/]"),
            new Markup("")
        );
        grid.AddRow(
            new Markup("  [dim]X:[/]"),
            new Markup("[cyan]0.00[/] [dim]mm[/]")
        );
        grid.AddRow(
            new Markup("  [dim]Y:[/]"),
            new Markup("[cyan]0.00[/] [dim]mm[/]")
        );
        grid.AddRow(
            new Markup("  [dim]Z:[/]"),
            new Markup("[cyan]0.00[/] [dim]mm[/]")
        );

        grid.AddEmptyRow();

        // 运动参数
        grid.AddRow(
            new Markup("[yellow]⚡ 运动参数[/]"),
            new Markup("")
        );
        grid.AddRow(
            new Markup("  [dim]进给速度:[/]"),
            new Markup("[cyan]0[/] [dim]mm/min[/]")
        );
        grid.AddRow(
            new Markup("  [dim]主轴转速:[/]"),
            new Markup("[cyan]0[/] [dim]rpm[/]")
        );

        grid.AddEmptyRow();

        // 系统状态
        grid.AddRow(
            new Markup("[yellow]● 当前状态[/]"),
            new Markup("[green]IDLE[/]")
        );
        grid.AddRow(
            new Markup("[yellow]🌡️  温度[/]"),
            new Markup("[cyan]25.0[/] [dim]°C[/]")
        );

        return new Panel(grid)
        {
            Header = new PanelHeader("[bold dodgerblue1]📊 系统状态[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("dodgerblue1"),
            Padding = new Padding(2, 1, 2, 1)
        };
    }

    #endregion

    private CommandExecutionResult CreateErrorResult(string message)
    {
        // 创建美化的错误面板
        var errorPanel = new Panel(new Markup(
            $"[red]❌ {Markup.Escape(message)}[/]\n\n" +
            "[dim]💡 建议:[/]\n" +
            "  • 输入 [cyan]help[/] 查看可用命令\n" +
            "  • 使用 [cyan]Tab[/] 键自动补全\n" +
            "  • 按 [cyan]↑[/] 键查看历史命令"
        ))
        {
            Header = new PanelHeader("[red]错误[/]", Justify.Left),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("red"),
            Padding = new Padding(2, 1, 2, 1)
        };

        return new CommandExecutionResult
        {
            Success = false,
            Renderable = errorPanel
        };
    }

    private string FormatDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? SecondaryText("无说明")
            : Markup.Escape(description);

    private string SecondaryText(string text) => $"[{_secondaryTextMarkup}]{Markup.Escape(text)}[/]";
}

/// <summary>
/// 命令执行结果
/// </summary>
public class CommandExecutionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 输出文本 (支持 Spectre.Console markup)
    /// </summary>
    public string Output { get; set; } = "";

    /// <summary>
    /// 可渲染对象（优先使用，避免 markup 解析错误）
    /// </summary>
    public IRenderable? Renderable { get; set; }

    /// <summary>
    /// 响应数据
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>
    /// 是否应该退出程序
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// 是否应该清屏
    /// </summary>
    public bool ShouldClear { get; set; }

    /// <summary>
    /// 是否应该显示状态面板
    /// </summary>
    public bool ShouldShowStatus { get; set; }

    /// <summary>
    /// 是否需要全屏显示此输出（避免在受限面板中被截断）
    /// </summary>
    public bool RequiresFullScreen { get; set; }
}
