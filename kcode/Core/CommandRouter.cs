using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Transport;
using Kcode.UI;

namespace Kcode.Core;

public record RouterResponse(CommandResult Result, IEnumerable<IRenderable> Outputs, bool ShouldExit = false);

public class CommandRouter
{
    private readonly IControlTransport _transport;
    private readonly StatusCache _statusCache;
    private readonly Dictionary<string, List<string>> _macros;
    private readonly Dictionary<string, string> _aliases;
    private List<(string Command, string Description)> _systemEntries = new();
    private readonly LogService _logger;

    public CommandRouter(IControlTransport transport, StatusCache statusCache, dynamic config, LogService logger)
    {
        _transport = transport;
        _statusCache = statusCache;
        _logger = logger;
        _macros = LoadMacros(config);
        _aliases = LoadAliases(config);
        _systemEntries = LoadSystemEntries(config);
    }

    public async Task<RouterResponse> HandleAsync(string input, CancellationToken ct = default)
    {
        var outputs = new List<IRenderable>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return new RouterResponse(new CommandResult(true), outputs);
        }

        if (input.StartsWith("/"))
        {
            return await HandleSystemAsync(input, ct);
        }

        var routedInput = ApplyAlias(input);
        _logger.Info($"EXECUTE {routedInput}");
        if (_macros.TryGetValue(routedInput, out var lines))
        {
            foreach (var line in lines)
            {
                outputs.Add(MessageSystem.RenderInfo($"Macro line: {line}"));
                var res = await _transport.ExecuteAsync(line, ct);
                if (!res.Success)
                {
                    outputs.Add(MessageSystem.RenderAlarm(res.Message));
                    _logger.Warn($"Macro failed: {res.Message}");
                    return new RouterResponse(res, outputs);
                }
            }
            outputs.Add(MessageSystem.RenderSuccess("Macro completed"));
            _logger.Info("Macro completed");
            return new RouterResponse(new CommandResult(true, "Macro completed"), outputs);
        }
        else
        {
            var res = await _transport.ExecuteAsync(routedInput, ct);
            if (!res.Success)
            {
                outputs.Add(MessageSystem.RenderAlarm(res.Message));
                _logger.Warn($"Command failed: {res.Message}");
            }
            else
            {
                _logger.Info("Command OK");
            }
            return new RouterResponse(res, outputs);
        }
    }

    public async Task<RouterResponse> HandleShortcutAsync(string action, CancellationToken ct = default)
    {
        var outputs = new List<IRenderable>();
        switch (action.ToLowerInvariant())
        {
            case "estop":
                {
                    var res = await _transport.TriggerEStopAsync(ct);
                    outputs.Add(MessageSystem.RenderAlarm(res.Message));
                    return new RouterResponse(res, outputs);
                }
            case "feed_hold":
            case "feedhold":
                {
                    var res = await _transport.ToggleFeedHoldAsync(ct);
                    outputs.Add(MessageSystem.RenderInfo(res.Message));
                    return new RouterResponse(res, outputs);
                }
            default:
                outputs.Add(MessageSystem.RenderWarning($"Unknown shortcut action: {action}"));
                return new RouterResponse(new CommandResult(false, "Unknown shortcut"), outputs);
        }
    }

    private async Task<RouterResponse> HandleSystemAsync(string input, CancellationToken ct)
    {
        var outputs = new List<IRenderable>();
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0].ToLowerInvariant();

        switch (cmd)
        {
            case "/exit":
                outputs.Add(MessageSystem.RenderInfo("Exiting..."));
                _logger.Info("Exit requested");
                return new RouterResponse(new CommandResult(true, "exit"), outputs, ShouldExit: true);
            case "/help":
                outputs.Add(BuildHelpPanel());
                return new RouterResponse(new CommandResult(true), outputs);
            case "/status":
                outputs.Add(BuildStatusPanel(_statusCache.Latest));
                return new RouterResponse(new CommandResult(true), outputs);
            case "/params":
                outputs.Add(BuildParamsPanel(_transport.GetParameters()));
                return new RouterResponse(new CommandResult(true), outputs);
            case "/tools":
                outputs.Add(BuildToolsPanel(_transport.GetTools()));
                return new RouterResponse(new CommandResult(true), outputs);
            case "/reset":
                {
                    var res = await _transport.ResetAsync(ct);
                    outputs.Add(res.Success ? MessageSystem.RenderSuccess(res.Message) : MessageSystem.RenderWarning(res.Message));
                    _logger.Info($"Reset: {res.Message}");
                    return new RouterResponse(res, outputs);
                }
            case "/set":
                {
                    if (parts.Length != 3)
                    {
                        outputs.Add(MessageSystem.RenderWarning("Usage: /set <PARAM> <VALUE>"));
                        _logger.Warn("Set param usage error");
                        return new RouterResponse(new CommandResult(false, "Invalid arguments"), outputs);
                    }
                    var key = parts[1];
                    if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var val))
                    {
                        outputs.Add(MessageSystem.RenderError("Invalid value."));
                        _logger.Warn("Set param invalid value");
                        return new RouterResponse(new CommandResult(false, "Invalid value"), outputs);
                    }
                    var res = await _transport.SetParameterAsync(key, val, ct);
                    outputs.Add(res.Success ? MessageSystem.RenderSuccess(res.Message) : MessageSystem.RenderWarning(res.Message));
                    _logger.Info($"Set param {key}={val}: {res.Success}");
                    return new RouterResponse(res, outputs);
                }
            case "/preview":
                {
                    var content = input.Substring("/preview".Length).Trim();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        outputs.Add(MessageSystem.RenderWarning("Usage: /preview <gcode lines>"));
                        _logger.Warn("Preview usage error");
                        return new RouterResponse(new CommandResult(false, "No content"), outputs);
                    }
                    content = content.Replace(";", "\n");
                    outputs.Add(PreviewEngine.BuildPreview(content));
                    _logger.Info("Preview rendered");
                    return new RouterResponse(new CommandResult(true), outputs);
                }
            case "/logs":
                {
                    var lines = _logger.Tail(50).ToList();
                    if (!lines.Any())
                    {
                        outputs.Add(MessageSystem.RenderInfo("No logs yet."));
                    }
                    else
                    {
                        var table = new Table().NoBorder().AddColumn("Logs");
                        foreach (var line in lines)
                        {
                            table.AddRow(new Markup(Markup.Escape(line)));
                        }
                        outputs.Add(new Panel(table).Header("Recent Logs").BorderColor(Color.Grey));
                    }
                    return new RouterResponse(new CommandResult(true), outputs);
                }
            default:
                outputs.Add(MessageSystem.RenderWarning($"Unknown command: {input}"));
                _logger.Warn($"Unknown command: {input}");
                return new RouterResponse(new CommandResult(false, "Unknown command"), outputs);
        }
    }

    private string ApplyAlias(string input)
    {
        var key = input.Trim().ToLowerInvariant();
        return _aliases.TryGetValue(key, out var mapped) ? mapped : input;
    }

    private Panel BuildHelpPanel()
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Command[/]")
            .AddColumn("[bold]Description[/]");

        foreach (var entry in _systemEntries)
        {
            table.AddRow(entry.Command, entry.Description);
        }

        return new Panel(table)
            .Header("[bold orange1]Commands[/]")
            .BorderColor(Color.Orange1);
    }

    private Panel BuildParamsPanel(IReadOnlyDictionary<string, double> parameters)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Parameter[/]")
            .AddColumn("[bold]Value[/]");

        foreach (var kv in parameters)
        {
            table.AddRow(kv.Key, kv.Value.ToString());
        }

        return new Panel(table)
            .Header("[bold orange1]Machine Parameters[/]")
            .BorderColor(Color.Orange1);
    }

    private Panel BuildToolsPanel(IReadOnlyList<Tool> tools)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[bold]T#[/]")
            .AddColumn("[bold]Dia[/]")
            .AddColumn("[bold]Len[/]")
            .AddColumn("[bold]Desc[/]");

        foreach (var t in tools)
        {
            table.AddRow(t.Id.ToString(), t.Diameter.ToString("F2"), t.Length.ToString("F2"), t.Description);
        }

        return new Panel(table)
            .Header("[bold orange1]Tool Table[/]")
            .BorderColor(Color.Orange1);
    }

    private Panel BuildStatusPanel(MachineStatus status)
    {
        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("[bold]Status[/]", $"{status.State}");
        grid.AddRow("[bold]Position[/]", $"X:{status.X:F3} Y:{status.Y:F3} Z:{status.Z:F3}");
        grid.AddRow("[bold]Feed[/]", $"{status.Feed}");
        grid.AddRow("[bold]Speed[/]", $"{status.Speed}");
        grid.AddRow("[bold]Temp[/]", $"{status.Temp:F1}°C");
        if (!string.IsNullOrWhiteSpace(status.Alarm))
        {
            grid.AddRow("[bold red]Alarm[/]", status.Alarm);
        }

        return new Panel(grid)
            .Header("[bold orange1]Machine Status[/]")
            .BorderColor(Color.Orange1);
    }

    private Dictionary<string, List<string>> LoadMacros(dynamic config)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (config is null) return result;

        try
        {
            if (config is IDictionary<object, object> dict && dict.TryGetValue("macros", out var macroObj))
            {
                if (macroObj is IDictionary<object, object> macroDict)
                {
                    foreach (var kv in macroDict)
                    {
                        var name = kv.Key.ToString() ?? string.Empty;
                        var list = new List<string>();

                        switch (kv.Value)
                        {
                            case string s:
                                list.Add(s);
                                break;
                            case IEnumerable<object> enumerable:
                                foreach (var item in enumerable)
                                {
                                    if (item != null) list.Add(item.ToString() ?? string.Empty);
                                }
                                break;
                        }

                        if (list.Count > 0) result[name] = list;
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return result;
    }

    private Dictionary<string, string> LoadAliases(dynamic config)
    {
        return ConfigHelper.GetStringMap(config, "commands", "aliases");
    }

    private List<(string Command, string Description)> LoadSystemEntries(dynamic config)
    {
        var entries = new List<(string Command, string Description)>
        {
            ("/help", "Show help"),
            ("/status", "Show machine status"),
            ("/params", "List parameters"),
            ("/tools", "List tools"),
            ("/reset", "Clear alarm"),
            ("/set <key> <val>", "Set parameter"),
            ("/preview <gcode>", "Preview path"),
            ("/exit", "Exit application")
        };

        try
        {
            if (config is IDictionary<object, object> dict
                && dict.TryGetValue("commands", out var cmdObj)
                && cmdObj is IDictionary<object, object> cmdDict
                && cmdDict.TryGetValue("system", out var sysObj)
                && sysObj is IEnumerable<object> sysList)
            {
                entries.Clear();
                foreach (var item in sysList)
                {
                    if (item is IDictionary<object, object> entry)
                    {
                        var name = entry.TryGetValue("name", out var nObj) ? nObj?.ToString() ?? "" : "";
                        var desc = entry.TryGetValue("description", out var dObj) ? dObj?.ToString() ?? "" : "";
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            entries.Add((name, desc));
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore, keep defaults
        }

        return entries;
    }
}
