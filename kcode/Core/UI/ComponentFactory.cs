using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Core.Config;

namespace Kcode.Core.UI;

/// <summary>
/// 组件工厂 - 根据配置创建 Spectre.Console 组件
/// </summary>
public class ComponentFactory
{
    private readonly DataContext _dataContext;
    private readonly ThemeConfig _theme;

    public ComponentFactory(DataContext dataContext, ThemeConfig theme)
    {
        _dataContext = dataContext;
        _theme = theme;
    }

    /// <summary>
    /// 创建组件
    /// </summary>
    public IRenderable CreateComponent(ComponentConfig config)
    {
        return config.Type.ToLower() switch
        {
            "text" => CreateText(config),
            "panel" => CreatePanel(config),
            "table" => CreateTable(config),
            "rule" => CreateRule(config),
            "markup" => CreateMarkup(config),
            "rows" => CreateRows(config),
            "columns" => CreateColumns(config),
            _ => new Text($"[red]Unknown component type: {config.Type}[/]")
        };
    }

    /// <summary>
    /// 创建文本组件
    /// </summary>
    private IRenderable CreateText(ComponentConfig config)
    {
        var value = ResolveValue(config.Value ?? "");
        var color = ResolveColor(config.Color);
        var style = config.Style ?? "";

        var markup = $"[{style} {color}]{value}[/]";
        return new Markup(markup);
    }

    /// <summary>
    /// 创建面板组件
    /// </summary>
    private IRenderable CreatePanel(ComponentConfig config)
    {
        var content = config.Children?.FirstOrDefault();
        var innerContent = content != null
            ? CreateComponent(content)
            : new Text("");

        var panel = new Panel(innerContent)
        {
            Border = BoxBorder.Rounded
        };

        return panel;
    }

    /// <summary>
    /// 创建表格组件
    /// </summary>
    private IRenderable CreateTable(ComponentConfig config)
    {
        var table = new Table();

        // 添加列
        if (config.Children != null)
        {
            foreach (var col in config.Children)
            {
                var header = ResolveValue(col.Value ?? "");
                table.AddColumn(new TableColumn(header));
            }
        }

        // TODO: 添加数据行
        table.AddRow("示例", "数据");

        return table;
    }

    /// <summary>
    /// 创建规则组件
    /// </summary>
    private IRenderable CreateRule(ComponentConfig config)
    {
        var title = ResolveValue(config.Title ?? "");
        return new Rule($"[{ResolveColor(config.Color)}]{title}[/]");
    }

    /// <summary>
    /// 创建标记组件
    /// </summary>
    private IRenderable CreateMarkup(ComponentConfig config)
    {
        var value = ResolveValue(config.Value ?? "");
        return new Markup(value);
    }

    /// <summary>
    /// 创建行组件
    /// </summary>
    private IRenderable CreateRows(ComponentConfig config)
    {
        if (config.Children == null || !config.Children.Any())
        {
            return new Text("");
        }

        var renderables = config.Children
            .Select(CreateComponent)
            .ToArray();

        return new Rows(renderables);
    }

    /// <summary>
    /// 创建列组件
    /// </summary>
    private IRenderable CreateColumns(ComponentConfig config)
    {
        if (config.Children == null || !config.Children.Any())
        {
            return new Text("");
        }

        var columns = new Columns(
            config.Children.Select(CreateComponent).ToArray()
        );

        return columns;
    }

    /// <summary>
    /// 解析值 (支持数据绑定)
    /// </summary>
    private string ResolveValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        // 如果包含绑定表达式 {path}
        if (value.Contains('{'))
        {
            return _dataContext.ResolveBinding(value);
        }

        return value;
    }

    /// <summary>
    /// 解析颜色
    /// </summary>
    private string ResolveColor(string? color)
    {
        if (string.IsNullOrEmpty(color))
        {
            return "white";
        }

        // 如果是主题颜色引用
        if (color.StartsWith("{theme.colors."))
        {
            var colorName = color.TrimStart('{').TrimEnd('}');
            var themeColor = _dataContext.GetByPath(colorName)?.ToString();
            return themeColor ?? color;
        }

        return color;
    }

    /// <summary>
    /// 解析 Spectre.Console 颜色
    /// </summary>
    private Color ParseColor(string colorString)
    {
        // Hex 颜色
        if (colorString.StartsWith("#"))
        {
            try
            {
                var hex = colorString.TrimStart('#');
                if (hex.Length == 6)
                {
                    var r = Convert.ToByte(hex[0..2], 16);
                    var g = Convert.ToByte(hex[2..4], 16);
                    var b = Convert.ToByte(hex[4..6], 16);
                    return new Color(r, g, b);
                }
            }
            catch
            {
                return Color.White;
            }
        }

        // 颜色名称
        return colorString.ToLower() switch
        {
            "red" => Color.Red,
            "green" => Color.Green,
            "blue" => Color.Blue,
            "yellow" => Color.Yellow,
            "cyan" => Color.Cyan,
            "magenta" => Color.Magenta,
            "white" => Color.White,
            "grey" or "gray" => Color.Grey,
            "orange" => Color.Orange1,
            "purple" => Color.Purple,
            _ => Color.White
        };
    }

    /// <summary>
    /// 解析边框样式
    /// </summary>
    private BoxBorder ParseBorder(string border)
    {
        return border.ToLower() switch
        {
            "rounded" => BoxBorder.Rounded,
            "square" => BoxBorder.Square,
            "double" => BoxBorder.Double,
            "heavy" => BoxBorder.Heavy,
            "none" => BoxBorder.None,
            _ => BoxBorder.Rounded
        };
    }
}
