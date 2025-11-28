using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System;
using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.UI;

namespace Kcode.Core;

public class ReplEngine
{
    private const int MaxHistory = 80;

    private readonly CommandRouter _router;
    private readonly StatusCache _statusCache;
    private readonly LayoutRenderer _ui;
    private readonly StatusBar _statusBar;
    private readonly LogService _logger;
    private readonly Dictionary<ConsoleKey, string> _shortcuts;
    private readonly List<IRenderable> _history = new();
    private readonly dynamic _config;
    private readonly string _suggestionText;
    private readonly Color _promptBorderColor;
    private readonly Color _promptAccentColor;
    private readonly Color _promptTextColor;
    private readonly Color _suggestionBorderColor;
    private bool _isRunning = true;

    public ReplEngine(CommandRouter router, StatusCache statusCache, LayoutRenderer ui, dynamic config, LogService logger)
    {
        _router = router;
        _statusCache = statusCache;
        _ui = ui;
        _statusBar = new StatusBar(config);
        _config = config;
        _logger = logger;
        _shortcuts = LoadShortcuts(config);
        _suggestionText = ConfigHelper.Get<string>(config, "Try \"/help\" to list commands.", "ui", "suggestion_text");
        _promptBorderColor = ThemeHelper.GetColor(config, "#7E57C2", "theme", "colors", "prompt_border");
        _promptAccentColor = ThemeHelper.GetColor(config, "#FF7043", "theme", "colors", "accent_primary");
        _promptTextColor = ThemeHelper.GetColor(config, "#EDE7F6", "theme", "colors", "prompt_text");
        _suggestionBorderColor = ThemeHelper.GetColor(config, "#4DD0E1", "theme", "colors", "accent_secondary");
    }

    public async Task RunAsync()
    {
        if (!EnsureConsole())
        {
            AnsiConsole.MarkupLine("[red]需要在交互式终端运行（例如 PowerShell / Windows Terminal）。[/]");
            return;
        }

        var inputBuffer = new StringBuilder();

        var layout = BuildLayout();
        RefreshLayout(layout, inputBuffer.ToString());

        try
        {
            await AnsiConsole.Live(layout)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    while (_isRunning)
                    {
                        await HandleInputAsync(inputBuffer);
                        RefreshLayout(layout, inputBuffer.ToString());
                        ctx.Refresh();
                        await Task.Delay(16);
                    }
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Live 模式不可用：{Markup.Escape(ex.Message)}[/]");
        }
    }

    private Layout BuildLayout()
    {
        return new Layout("root")
            .SplitRows(
                new Layout("header").Size(14),
                new Layout("body").Ratio(1),
                new Layout("suggestion").Size(3),
                new Layout("prompt").Size(3),
                new Layout("footer").Size(4)
            );
    }

    private void RefreshLayout(Layout layout, string input)
    {
        layout["header"].Update(_ui.BuildHeader());
        layout["body"].Update(BuildHistoryPanel());
        layout["suggestion"].Update(BuildSuggestionBar());
        layout["prompt"].Update(BuildPrompt(input));
        layout["footer"].Update(_statusBar.Render(_statusCache.Latest));
    }

    private async Task HandleInputAsync(StringBuilder inputBuffer)
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);

            if (_shortcuts.TryGetValue(key.Key, out var action))
            {
                var resp = await _router.HandleShortcutAsync(action);
                AddHistory(resp.Outputs);
                _logger.Info($"Shortcut {action} triggered");
                continue;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                var cmd = inputBuffer.ToString().Trim();
                inputBuffer.Clear();
                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    AddHistory(BuildCommandEcho(cmd));
                    await ProcessCommand(cmd);
                }
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (inputBuffer.Length > 0) inputBuffer.Length--;
            }
            else if (key.KeyChar != '\0')
            {
                inputBuffer.Append(key.KeyChar);
            }
        }
    }

    private bool EnsureConsole()
    {
        // If already interactive, keep going.
        if (AnsiConsole.Profile.Capabilities.Interactive && !Console.IsOutputRedirected)
            return true;

        // Try to allocate / enable VT on Windows when launched without console.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                if (GetConsoleWindow() == IntPtr.Zero)
                {
                    if (!AllocConsole())
                        return false;
                }

                // Rebind standard streams to the newly allocated console.
                var stdout = Console.OpenStandardOutput();
                var stdin = Console.OpenStandardInput();
                Console.SetOut(new StreamWriter(stdout) { AutoFlush = true });
                Console.SetIn(new StreamReader(stdin));

                EnableVirtualTerminal();
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_INPUT_HANDLE = -10;
    private const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const int ENABLE_PROCESSED_OUTPUT = 0x0001;
    private const int ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
    private const int ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;
    private const int ENABLE_PROCESSED_INPUT = 0x0001;

    private void EnableVirtualTerminal()
    {
        var handleOut = GetStdHandle(STD_OUTPUT_HANDLE);
        if (handleOut != IntPtr.Zero && GetConsoleMode(handleOut, out int modeOut))
        {
            modeOut |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | ENABLE_PROCESSED_OUTPUT | ENABLE_WRAP_AT_EOL_OUTPUT;
            SetConsoleMode(handleOut, modeOut);
        }

        var handleIn = GetStdHandle(STD_INPUT_HANDLE);
        if (handleIn != IntPtr.Zero && GetConsoleMode(handleIn, out int modeIn))
        {
            modeIn |= ENABLE_VIRTUAL_TERMINAL_INPUT | ENABLE_PROCESSED_INPUT;
            SetConsoleMode(handleIn, modeIn);
        }
    }

    private Panel BuildHistoryPanel()
    {
        if (_history.Count == 0)
        {
            return new Panel(new Markup("[grey53]No messages yet. Enter a command to begin.[/]"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey19)
                .Padding(1, 0, 1, 0);
        }

        return new Panel(new Rows(_history))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey19)
            .Padding(1, 0, 1, 0);
    }

    private IRenderable BuildSuggestionBar()
    {
        var text = string.IsNullOrWhiteSpace(_suggestionText)
            ? "Try \"/help\" to list commands."
            : _suggestionText;

        return new Panel(new Markup($"[grey70]> {Markup.Escape(text)}[/]"))
            .Border(BoxBorder.Rounded)
            .BorderColor(_suggestionBorderColor)
            .Padding(1, 0, 1, 0);
    }

    private Panel BuildPrompt(string input)
    {
        var accent = ThemeHelper.ToMarkup(_promptAccentColor);
        var textColor = ThemeHelper.ToMarkup(_promptTextColor);
        var prompt = new Markup($"[{accent}]>[/] [{textColor}]{Markup.Escape(input)}[/][grey35]_[/]");
        return new Panel(prompt)
            .Border(BoxBorder.Rounded)
            .BorderColor(_promptBorderColor)
            .Padding(1, 0, 1, 0);
    }

    private void AddHistory(IRenderable renderable)
    {
        _history.Add(renderable);
        TrimHistory();
    }

    private void AddHistory(IEnumerable<IRenderable> renderables)
    {
        foreach (var r in renderables) _history.Add(r);
        TrimHistory();
    }

    private void TrimHistory()
    {
        if (_history.Count > MaxHistory)
        {
            var overflow = _history.Count - MaxHistory;
            _history.RemoveRange(0, overflow);
        }
    }

    private async Task ProcessCommand(string input)
    {
        var response = await _router.HandleAsync(input);
        var outputs = response.Outputs.ToList();

        if (!outputs.Any())
        {
            IRenderable fallback = response.Result.Success
                ? MessageSystem.RenderSuccess(string.IsNullOrWhiteSpace(response.Result.Message) ? "OK" : response.Result.Message)
                : MessageSystem.RenderAlarm(string.IsNullOrWhiteSpace(response.Result.Message) ? "Error" : response.Result.Message);
            outputs.Add(fallback);
        }

        AddHistory(outputs);

        if (response.ShouldExit)
        {
            _isRunning = false;
        }
    }

    private Dictionary<ConsoleKey, string> LoadShortcuts(dynamic config)
    {
        var map = new Dictionary<ConsoleKey, string>();

        Dictionary<string, string> shortcuts = ConfigHelper.GetStringMap(config, "ui", "shortcuts");
        foreach (var kv in shortcuts)
        {
            if (Enum.TryParse<ConsoleKey>(kv.Key, true, out ConsoleKey key))
            {
                map[key] = kv.Value;
            }
        }

        // Defaults
        if (!map.ContainsKey(ConsoleKey.Escape)) map[ConsoleKey.Escape] = "estop";
        if (!map.ContainsKey(ConsoleKey.Spacebar)) map[ConsoleKey.Spacebar] = "feed_hold";

        return map;
    }

    private IRenderable BuildCommandEcho(string command)
    {
        var accent = ThemeHelper.ToMarkup(_promptAccentColor);
        return new Markup($"[{accent}]>[/] [grey78]{Markup.Escape(command)}[/]");
    }
}
