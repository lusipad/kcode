using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Kcode.Core.Config;
using Kcode.Core.Input;
using Kcode.Core.Terminal;
using Kcode.UI;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Kcode.Core.UI;

public class SpectreReplView : IReplView
{
    private readonly RootConfig _config;
    private readonly LayoutEngine _layoutEngine;
    private readonly ITerminal _terminal;
    private readonly int _slashPanelHeight;
    private readonly List<string> _promptTips;
    private int _promptTipIndex;
    private readonly object _tipLock = new();

    private readonly string _headerColor;
    private readonly string _accentColor;
    private readonly string _accentSecondaryColor;
    private readonly string _mutedColor;
    private readonly string _panelBorderColor;
    private readonly string _promptBorderColor;
    private readonly string _promptTextColor;

    public SpectreReplView(RootConfig config, LayoutEngine layoutEngine, ITerminal terminal, int slashDisplayCount)
    {
        _config = config;
        _layoutEngine = layoutEngine;
        _terminal = terminal;

        _slashPanelHeight = Math.Max(4, slashDisplayCount + 2);
        _promptTips = BuildPromptTips(config);

        _headerColor = ThemeHelper.GetColorMarkup(config, "#F4E3D7", "theme", "colors", "header_text");
        _accentColor = ThemeHelper.GetColorMarkup(config, "#FF7043", "theme", "colors", "accent_primary");
        _accentSecondaryColor = ThemeHelper.GetColorMarkup(config, "#4DD0E1", "theme", "colors", "accent_secondary");
        _mutedColor = ThemeHelper.GetColorMarkup(config, "#8E8EA0", "theme", "colors", "muted_text");
        _panelBorderColor = ThemeHelper.GetColorMarkup(config, "#3F3F46", "theme", "colors", "panel_border");
        _promptBorderColor = ThemeHelper.GetColorMarkup(config, "#7E57C2", "theme", "colors", "prompt_border");
        _promptTextColor = ThemeHelper.GetColorMarkup(config, "#EDE7F6", "theme", "colors", "prompt_text");
    }

    public IReplViewSession BeginSession(ReplViewState initialState)
    {
        var tip = GetNextPromptTip();
        var snapshot = CloneState(initialState);
        return new SpectreReplViewSession(this, snapshot, tip, _slashPanelHeight, _terminal);
    }

    internal string GetNextPromptTip()
    {
        lock (_tipLock)
        {
            if (_promptTips.Count == 0)
            {
                return string.Empty;
            }

            var tip = _promptTips[_promptTipIndex];
            _promptTipIndex = (_promptTipIndex + 1) % _promptTips.Count;
            return tip;
        }
    }

    internal ReplViewState CloneState(ReplViewState state)
    {
        var history = state.History?.ToArray() ?? Array.Empty<IRenderable>();
        var slashItems = state.SlashState.Items?.ToArray()
            ?? Array.Empty<(SlashCommandEntry Entry, bool IsSelected)>();
        var slashState = new SlashViewState(slashItems, state.SlashState.WindowInfo);

        return state with
        {
            History = history,
            SlashState = slashState
        };
    }

    internal IRenderable BuildRootRenderable(ReplViewState state, string tip, int slashHeight)
    {
        var layout = new Layout("root");
        layout.SplitRows(
            new Layout("header").Size(5),
            new Layout("space-header").Size(1),
            new Layout("history").Ratio(1),
            new Layout("space-history").Size(1),
            new Layout("slash").Size(Math.Max(3, slashHeight)),
            new Layout("space-slash").Size(1),
            new Layout("prompt").Size(4),
            new Layout("tips").Size(2),
            new Layout("space-prompt").Size(1),
            new Layout("footer").Size(4)
        );

        layout["header"].Update(BuildHeader());
        layout["history"].Update(BuildHistorySection(state));
        layout["slash"].Update(BuildSlashSection(state.SlashState));
        layout["prompt"].Update(BuildPromptPanel(state));
        layout["tips"].Update(BuildTipsLine(tip));
        layout["footer"].Update(BuildFooter());

        layout["space-header"].Update(Text.Empty);
        layout["space-history"].Update(Text.Empty);
        layout["space-slash"].Update(Text.Empty);
        layout["space-prompt"].Update(Text.Empty);

        return layout;
    }

    internal IRenderable SafeMarkupOrText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Text.Empty;
        }

        try
        {
            return new Markup(text);
        }
        catch (InvalidOperationException)
        {
            return new Text(text);
        }
    }

    private IRenderable BuildHeader()
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(40));
        grid.AddColumn(new GridColumn());

        var name = string.IsNullOrWhiteSpace(_config.App.Name) ? "kcode" : _config.App.Name;
        var slogan = string.IsNullOrWhiteSpace(_config.App.Slogan)
            ? "配置驱动的多协议控制台"
            : _config.App.Slogan;

        grid.AddRow(
            new Markup($"[{_headerColor}]⚡ {Markup.Escape(name)}[/] [dim]v{Markup.Escape(_config.App.Version ?? "1.0")}[/]"),
            new Markup($"[{_accentColor}]{Markup.Escape(slogan)}[/]")
        );

        grid.AddRow(
            new Markup($"[{_mutedColor}]模型[/] [white]{Markup.Escape(GetDisplayValue(_config.App.Model))}[/]"),
            new Markup($"[{_mutedColor}]权限[/] [white]{Markup.Escape(GetDisplayValue(_config.App.Permissions))}[/]")
        );

        grid.AddRow(
            new Markup($"[{_mutedColor}]工作区[/] [white]{Markup.Escape(GetDisplayValue(_config.App.Workspace))}[/]"),
            new Markup($"[{_mutedColor}]分支[/] [white]{Markup.Escape(GetDisplayValue(_config.App.Branch))}[/]")
        );

        return new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(_panelBorderColor),
            Padding = new Padding(1, 0, 1, 0)
        };
    }

    private IRenderable BuildHistorySection(ReplViewState state)
    {
        if (state.History.Count == 0)
        {
            return new Markup($"[{_mutedColor}]暂无输出，输入命令开始吧[/]");
        }

        return new Rows(state.History.ToArray());
    }

    private IRenderable BuildSlashSection(SlashViewState slashState)
    {
        if (slashState.Items.Count == 0)
        {
            return Text.Empty;
        }

        var table = new Table()
            .NoBorder()
            .HideHeaders()
            .AddColumn(new TableColumn("命令").Width(20))
            .AddColumn(new TableColumn("说明"));

        foreach (var (entry, isSelected) in slashState.Items)
        {
            var name = Markup.Escape(entry.Name);
            var description = string.IsNullOrWhiteSpace(entry.Description)
                ? "-"
                : entry.Description;

            var display = isSelected
                ? $"[black on {_accentSecondaryColor}] {name} [/]"
                : $"[{_accentSecondaryColor}]{name}[/]";

            table.AddRow(
                new Markup(display),
                new Markup($"[{_mutedColor}]{Markup.Escape(description)}[/]")
            );
        }

        var elements = new List<IRenderable>
        {
            new Markup($"[{_accentSecondaryColor}]/ 命令列表[/]"),
            Text.NewLine,
            table
        };

        if (slashState.WindowInfo.HasValue)
        {
            var window = slashState.WindowInfo.Value;
            elements.Add(new Markup($"[grey50]{window.Start}-{window.End}/{window.Total}[/]"));
        }

        return new Rows(elements);
    }

    private IRenderable BuildPromptPanel(ReplViewState state)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().Width(3));
        grid.AddColumn(new GridColumn());

        var inputMarkup = BuildInputMarkup(state.InputText, state.Suggestion);
        grid.AddRow(new Markup($"[{_accentColor}]›[/]"), inputMarkup);

        return new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(_promptBorderColor),
            Padding = new Padding(1, 0, 1, 0)
        };
    }

    private IRenderable BuildTipsLine(string tip)
    {
        var text = string.IsNullOrWhiteSpace(tip)
            ? "Tab 自动补全 · ↑↓ 浏览历史 · / 浏览命令"
            : tip;

        return new Markup($"[{_mutedColor}]TIP[/] {Markup.Escape(text)}");
    }

    private IRenderable BuildFooter()
    {
        var grid = new Grid();
        grid.AddColumns(new GridColumn(), new GridColumn());

        grid.AddRow(
            _layoutEngine.RenderStatusBar(),
            _layoutEngine.RenderFooterBadges());

        return grid;
    }

    private IRenderable BuildInputMarkup(string input, string suggestion)
    {
        var builder = new StringBuilder();
        var escapedInput = string.IsNullOrEmpty(input) ? string.Empty : Markup.Escape(input);
        builder.Append($"[{_promptTextColor}]{escapedInput}[/]");

        var ghost = BuildGhostSuggestion(input, suggestion);
        if (!string.IsNullOrEmpty(ghost))
        {
            builder.Append($"[{_mutedColor}]{Markup.Escape(ghost)}[/]");
        }

        return new Markup(builder.ToString());
    }

    private static string BuildGhostSuggestion(string input, string suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(input))
        {
            return suggestion;
        }

        if (suggestion.StartsWith(input, StringComparison.OrdinalIgnoreCase) &&
            suggestion.Length > input.Length)
        {
            return suggestion[input.Length..];
        }

        return string.Empty;
    }

    private static string GetDisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "N/A" : value;

    private static List<string> BuildPromptTips(RootConfig config)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void AddTip(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var normalized = text.Trim();
            if (seen.Add(normalized))
            {
                result.Add(normalized);
            }
        }

        AddTip(config.App.SuggestionText);
        AddTip(config.App.PromptPlaceholder);
        AddTip("/ 输入可浏览命令列表");
        AddTip("Tab 键可自动补全命令");
        AddTip("↑/↓ 浏览历史命令");

        return result;
    }

    private sealed class SpectreReplViewSession : IReplViewSession
    {
        private readonly SpectreReplView _view;
        private readonly ITerminal _terminal;
        private readonly int _slashHeight;
        private readonly object _stateLock = new();

        private ReplViewState _state;
        private string _currentTip;

        private Channel<bool>? _refreshChannel;
        private CancellationTokenSource? _renderLoopCts;
        private Task? _renderLoopTask;
        private TaskCompletionSource<bool>? _liveReadyTcs;
        private bool _disposed;

        public SpectreReplViewSession(
            SpectreReplView view,
            ReplViewState initialState,
            string initialTip,
            int slashHeight,
            ITerminal terminal)
        {
            _view = view;
            _state = initialState;
            _currentTip = initialTip;
            _slashHeight = slashHeight;
            _terminal = terminal;
        }

        public async Task StartAsync()
        {
            await StartLiveLoopAsync().ConfigureAwait(false);
        }

        public void Update(ReplViewState state)
        {
            lock (_stateLock)
            {
                _state = _view.CloneState(state);
            }
        }

        public Task RequestRefreshAsync()
        {
            if (_disposed)
            {
                return Task.CompletedTask;
            }

            _currentTip = _view.GetNextPromptTip();

            var writer = _refreshChannel?.Writer;
            if (writer == null)
            {
                return Task.CompletedTask;
            }

            return writer.TryWrite(true)
                ? Task.CompletedTask
                : writer.WriteAsync(true).AsTask();
        }

        public void DisplayFullScreen(IRenderable? renderable, string? fallbackOutput)
        {
            if (_disposed)
            {
                return;
            }

            var wasRunning = PauseLiveLoop();

            var content = renderable ?? _view.SafeMarkupOrText(fallbackOutput ?? string.Empty);

            AnsiConsole.Clear();
            AnsiConsole.Write(content);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey50]按任意键返回...[/]");

            while (_terminal.KeyAvailable)
            {
                _terminal.ReadKey(true);
            }

            _terminal.ReadKey(true);

            if (wasRunning)
            {
                ResumeLiveLoop();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                await StopLiveLoopAsync().ConfigureAwait(false);
            }
            finally
            {
                _refreshChannel?.Writer.TryComplete();
            }
        }

        private async Task StartLiveLoopAsync()
        {
            _refreshChannel = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false
            });

            _renderLoopCts = new CancellationTokenSource();
            _liveReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var state = SnapshotState();
            _renderLoopTask = Task.Run(() => RunLiveAsync(state, _renderLoopCts.Token));

            await _liveReadyTcs.Task.ConfigureAwait(false);
        }

        private bool PauseLiveLoop()
        {
            if (_renderLoopCts == null || _renderLoopTask == null)
            {
                return false;
            }

            var cts = _renderLoopCts;
            _renderLoopCts = null;
            cts.Cancel();

            try
            {
                _renderLoopTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            _renderLoopTask = null;
            cts.Dispose();
            _refreshChannel = null;
            _liveReadyTcs = null;
            return true;
        }

        private void ResumeLiveLoop()
        {
            StartLiveLoopAsync().GetAwaiter().GetResult();
            RequestRefreshAsync().GetAwaiter().GetResult();
        }

        private async Task StopLiveLoopAsync()
        {
            if (_renderLoopCts != null)
            {
                _renderLoopCts.Cancel();
            }

            if (_renderLoopTask != null)
            {
                try
                {
                    await _renderLoopTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // ignored
                }
            }
        }

        private ReplViewState SnapshotState()
        {
            lock (_stateLock)
            {
                return _state;
            }
        }

        private async Task RunLiveAsync(ReplViewState initialState, CancellationToken token)
        {
            var initialRenderable = _view.BuildRootRenderable(initialState, _currentTip, _slashHeight);
            try
            {
                await AnsiConsole.Live(initialRenderable)
                    .AutoClear(false)
                    .Overflow(VerticalOverflow.Visible)
                    .StartAsync(async ctx =>
                    {
                        ctx.Refresh();
                        _liveReadyTcs?.TrySetResult(true);

                        var reader = _refreshChannel?.Reader;
                        if (reader == null)
                        {
                            return;
                        }

                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                await WaitForRefreshAsync(reader, token).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }

                            var snapshot = SnapshotState();
                            var updated = _view.BuildRootRenderable(snapshot, _currentTip, _slashHeight);
                            ctx.UpdateTarget(updated);
                            ctx.Refresh();
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                // ignored
            }

            _renderLoopCts?.Dispose();
            _renderLoopCts = null;
            _renderLoopTask = null;
            _refreshChannel = null;
            _liveReadyTcs = null;
        }

        private static async Task WaitForRefreshAsync(ChannelReader<bool> reader, CancellationToken token)
        {
            while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                while (reader.TryRead(out _))
                {
                    // drain
                }

                return;
            }
        }
    }
}
