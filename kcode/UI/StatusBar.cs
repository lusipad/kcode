using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Core;

namespace Kcode.UI;

public class StatusBar
{
    private readonly dynamic _config;
    private readonly List<(string Template, string Color)> _sections;
    private readonly List<(string Template, string Color)> _badges;
    private readonly Dictionary<string, string> _metaValues;
    private readonly string _noticeTemplate;
    private readonly Color _noticeColor;
    private static readonly Regex PlaceholderRegex = new(@"\{(?<key>[a-zA-Z_]+)(:(?<format>[A-Za-z0-9\.]+))?\}", RegexOptions.Compiled);

    public StatusBar(dynamic config)
    {
        _config = config;
        _sections = ConfigHelper.GetFooterSections(config);
        if (_sections.Count == 0)
        {
            _sections = new List<(string, string)>
            {
                ("X:{x:F3}", "purple"),
                ("Y:{y:F3}", "purple"),
                ("Z:{z:F3}", "purple"),
                ("F:{feed:F0} S:{speed:F0}", "cyan"),
                ("{state_icon} {state}", "green")
            };
        }

        _badges = ConfigHelper.GetTemplateColorList(config, "ui", "footer", "badges");
        if (_badges.Count == 0)
        {
            _badges = new List<(string, string)>
            {
                ("BOT {model}", "dodgerblue1"),
                ("TOK {tokens}", "magenta")
            };
        }

        _metaValues = ConfigHelper.GetStringMap(config, "ui", "footer", "meta_values");
        if (_metaValues.Count == 0)
        {
            _metaValues["model"] = "Virtual CNC";
            _metaValues["workspace"] = "workspace";
            _metaValues["branch"] = "main";
            _metaValues["tokens"] = "ready";
            _metaValues["permissions"] = "permissions normal";
        }

        _noticeTemplate = ConfigHelper.Get<string>(config, ">> {permissions}", "ui", "footer", "notice");
        _noticeColor = ThemeHelper.GetColor(config, "#FF4081", "theme", "colors", "footer_notice");
    }

    public IRenderable Render(MachineStatus status)
    {
        var renderables = new List<IRenderable>();
        var values = BuildValueMap(status);

        var metricSegments = BuildSegments(_sections, values);
        var badgeSegments = BuildSegments(_badges, values);

        var firstRowParts = new List<string>();
        if (metricSegments.Count > 0)
        {
            firstRowParts.Add(string.Join("   ", metricSegments));
        }
        if (badgeSegments.Count > 0)
        {
            firstRowParts.Add(string.Join("   ", badgeSegments));
        }

        if (firstRowParts.Count > 0)
        {
            renderables.Add(new Markup(string.Join("    ", firstRowParts)));
        }

        var notice = RenderTemplate(_noticeTemplate, values);
        if (!string.IsNullOrWhiteSpace(notice))
        {
            renderables.Add(new Markup($"[{ThemeHelper.ToMarkup(_noticeColor)}]{Markup.Escape(notice)}[/]"));
        }

        if (renderables.Count == 0)
        {
            renderables.Add(new Markup("[grey37]No status[/]"));
        }

        return new Panel(new Rows(renderables.ToArray()))
            .Border(BoxBorder.None)
            .Padding(1, 0, 1, 0);
    }

    private Dictionary<string, object?> BuildValueMap(MachineStatus status)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            { "x", status.X },
            { "y", status.Y },
            { "z", status.Z },
            { "feed", status.Feed },
            { "speed", status.Speed },
            { "state", status.State ?? string.Empty },
            { "temp", status.Temp },
            { "alarm", status.Alarm ?? string.Empty }
        };

        var stateIcon = status.State switch
        {
            "RUN" => ">",
            "HOLD" => "||",
            "ALARM" => "!!",
            _ => "."
        };

        map["state_icon"] = stateIcon;

        foreach (var kv in _metaValues)
        {
            map[kv.Key] = kv.Value;
        }

        return map;
    }

    private string RenderTemplate(string template, Dictionary<string, object?> values)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            var key = match.Groups["key"].Value.ToLowerInvariant();
            var format = match.Groups["format"].Success ? match.Groups["format"].Value : string.Empty;

            if (!values.TryGetValue(key, out var value) || value is null)
            {
                return string.Empty;
            }

            if (value is IFormattable formattable && !string.IsNullOrEmpty(format))
            {
                return formattable.ToString(format, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            }

            if (value is IFormattable formattableDefault)
            {
                return formattableDefault.ToString(null, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            }

            return value.ToString() ?? string.Empty;
        });
    }

    private List<string> BuildSegments(List<(string Template, string Color)> templates, Dictionary<string, object?> values)
    {
        var segments = new List<string>();
        foreach (var template in templates)
        {
            var text = RenderTemplate(template.Template, values);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var color = string.IsNullOrWhiteSpace(template.Color) ? "grey" : template.Color;
            segments.Add($"[{color}]{Markup.Escape(text)}[/]");
        }

        return segments;
    }
}
