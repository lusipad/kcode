using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Core;

namespace Kcode.UI;

public class LayoutRenderer
{
    private readonly dynamic _config;

    public LayoutRenderer(dynamic config)
    {
        _config = config;
    }

    public void RenderHeader()
    {
        AnsiConsole.Write(BuildHeader());
    }

    public IRenderable BuildHeader()
    {
        object cfg = _config;
        var appName = ConfigHelper.Get<string>(cfg, "kcode", "app", "name");
        var version = ConfigHelper.Get<string>(cfg, "0.1.0", "app", "version");
        var port = ConfigHelper.Get<string>(cfg, "COM3", "app", "port");

        var welcome = ConfigHelper.Get<string>(cfg, "Welcome back!", "ui", "header", "welcome");
        var contextLines = ConfigHelper.GetStringList(cfg, "ui", "header", "context_lines");
        if (contextLines.Count == 0)
        {
            contextLines = ConfigHelper.GetStringList(cfg, "ui", "header", "lines");
        }
        contextLines = contextLines
            .Select(l => FormatLine(l, appName, version, port))
            .ToList();

        var logoLines = ConfigHelper.GetStringList(cfg, "ui", "header", "logo");
        if (logoLines.Count == 0)
        {
            logoLines = new List<string>
            {
                "  __  __  ",
                " / _|/ _| ",
                "| |_| |_  ",
                "|  _|  _| ",
                "|_| |_|   "
            };
        }

        var tipsTitle = ConfigHelper.Get<string>(cfg, "Tips for getting started", "ui", "header", "tips", "title");
        var tipsItems = ConfigHelper.GetStringList(cfg, "ui", "header", "tips", "items");
        if (tipsItems.Count == 0)
        {
            tipsItems = new List<string> { "Run /help to discover commands." };
        }

        var activityTitle = ConfigHelper.Get<string>(cfg, "Recent activity", "ui", "header", "activity", "title");
        var activityItems = ConfigHelper.GetStringList(cfg, "ui", "header", "activity", "items");
        if (activityItems.Count == 0)
        {
            activityItems = new List<string> { "No recent activity" };
        }

        var accent = ThemeHelper.GetColor(cfg, "#FF7043", "theme", "colors", "panel_border");
        var dividerColor = ThemeHelper.GetColor(cfg, "#F57C00", "theme", "colors", "panel_divider");
        var textColor = ThemeHelper.GetColor(cfg, "#F4E3D7", "theme", "colors", "header_text");
        var accentMarkup = ThemeHelper.ToMarkup(accent);
        var dividerMarkup = ThemeHelper.ToMarkup(dividerColor);
        var textMarkup = ThemeHelper.ToMarkup(textColor);

        var grid = new Grid();
        grid.AddColumn(new GridColumn().Padding(0, 0, 3, 0));
        grid.AddColumn();
        grid.AddRow(BuildLeftColumn(welcome, logoLines, contextLines, textMarkup, accentMarkup),
            BuildRightColumn(tipsTitle, tipsItems, activityTitle, activityItems, dividerColor, textMarkup, accentMarkup));

        var panel = new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(accent))
            .Padding(1, 0, 1, 0)
            .Header(new PanelHeader($"[bold {accentMarkup}]{Markup.Escape(appName)} v{version}[/]", Justify.Left));

        return new Rows(
            panel,
            new Rule().RuleStyle(new Style(dividerColor))
        );
    }

    private string FormatLine(string line, string appName, string version, string port)
    {
        return line
            .Replace("{name}", appName)
            .Replace("{version}", version)
            .Replace("{port}", port);
    }

    private IRenderable BuildLeftColumn(string welcome, List<string> logoLines, List<string> context, string textColor, string accentMarkup)
    {
        var blocks = new List<IRenderable>();

        if (!string.IsNullOrWhiteSpace(welcome))
        {
            blocks.Add(new Markup($"[bold {accentMarkup}]{Markup.Escape(welcome)}[/]"));
        }

        if (logoLines.Count > 0)
        {
            var logo = string.Join("\n", logoLines.Select(line => $"[{textColor}]{Markup.Escape(line)}[/]"));
            blocks.Add(new Markup(logo));
        }

        if (context.Count > 0)
        {
            var info = string.Join("\n", context.Select(line => $"[{textColor}]{Markup.Escape(line)}[/]"));
            blocks.Add(new Markup(info));
        }

        if (blocks.Count == 0)
        {
            blocks.Add(new Markup($"[{textColor}]Ready to connect[/]"));
        }

        return new Rows(blocks.Select(b => new Padder(b, new Padding(0, 0, 0, 1))).ToArray());
    }

    private IRenderable BuildRightColumn(
        string tipsTitle,
        List<string> tipsItems,
        string activityTitle,
        List<string> activityItems,
        Color dividerColor,
        string textColor,
        string accentMarkup)
    {
        var segments = new List<IRenderable>
        {
            BuildListBlock(tipsTitle, tipsItems, accentMarkup, textColor)
        };

        segments.Add(new Rule().RuleStyle(new Style(dividerColor)));
        segments.Add(BuildListBlock(activityTitle, activityItems, accentMarkup, textColor));

        return new Rows(segments.ToArray());
    }

    private IRenderable BuildListBlock(string title, List<string> items, string accentMarkup, string textColor)
    {
        if (items.Count == 0)
        {
            items.Add("No data");
        }

        var header = new Markup($"[bold {accentMarkup}]{Markup.Escape(title)}[/]");
        var body = string.Join("\n", items.Select(item => $"[{textColor}]{Markup.Escape(item)}[/]"));

        return new Rows(
            new Padder(header, new Padding(0, 0, 0, 1)),
            new Markup(body)
        );
    }
}
