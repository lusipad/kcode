using Spectre.Console;
using Spectre.Console.Rendering;
using Kcode.Core.Config;

namespace Kcode.Core.UI;

/// <summary>
/// 布局引擎 - 解析和渲染 UI 布局
/// </summary>
public class LayoutEngine
{
    private readonly RootConfig _config;
    private readonly DataContext _dataContext;
    private readonly ComponentFactory _componentFactory;

    public LayoutEngine(RootConfig config, DataContext dataContext)
    {
        _config = config;
        _dataContext = dataContext;
        _componentFactory = new ComponentFactory(dataContext, config.Theme);
    }

    /// <summary>
    /// 渲染完整布局
    /// </summary>
    public Layout RenderLayout()
    {
        var layout = new Layout("Root");

        // 如果没有配置布局，使用默认布局
        if (_config.Layout.Regions.Count == 0)
        {
            return CreateDefaultLayout();
        }

        // 根据配置构建布局
        return BuildLayout(layout);
    }

    /// <summary>
    /// 渲染状态栏 - 机器状态
    /// </summary>
    public IRenderable RenderStatusBar()
    {
        var status = _dataContext.GetStatus();

        // 使用更精致的格式，带 emoji 和分隔符
        var statusText = string.Join(" [dim]|[/] ",
            $"[purple]📍 X:{status.X:F2} Y:{status.Y:F2} Z:{status.Z:F2}[/]",
            $"[cyan]⚡ F:{status.Feed:F0} S:{status.Speed:F0}[/]",
            $"[green]{status.StateIcon} {status.State}[/]",
            $"[yellow]🌡️ {status.Temp:F1}°C[/]"
        );

        return new Markup(statusText);
    }

    /// <summary>
    /// 渲染页脚徽章 - 元信息
    /// </summary>
    public IRenderable RenderFooterBadges()
    {
        var meta = _dataContext.GetMeta();

        // 使用更精致的格式，类似 Claude Code 状态栏
        var badgeText = string.Join(" [dim]|[/] ",
            $"[dodgerblue1]🤖 {meta.Model}[/]",
            $"[yellow3]📁 {meta.Workspace}[/]",
            $"[springgreen3]🌿 {meta.Branch}[/]",
            $"[magenta]💎 {meta.Tokens}[/]"
        );

        return new Markup(badgeText);
    }

    /// <summary>
    /// 构建布局
    /// </summary>
    private Layout BuildLayout(Layout root)
    {
        // 简化实现：只支持基本的区域渲染
        var structure = _config.Layout.Structure;

        if (structure.Type == "rows")
        {
            foreach (var child in structure.Children)
            {
                var regionId = child.Id;

                if (_config.Layout.Regions.TryGetValue(regionId, out var regionConfig))
                {
                    var content = _componentFactory.CreateComponent(regionConfig.Content);
                    root.SplitRows(new Layout(regionId).Update(content));
                }
            }
        }

        return root;
    }

    /// <summary>
    /// 创建默认布局
    /// </summary>
    private Layout CreateDefaultLayout()
    {
        var layout = new Layout("Root");

        layout.SplitRows(
            new Layout("Header").Size(3),
            new Layout("Body").Ratio(1),
            new Layout("Footer").Size(4)
        );

        // 默认内容
        layout["Header"].Update(new Panel(new Markup("[cyan]KCode v2[/]")));
        layout["Body"].Update(new Panel(new Text("主体区域")));
        layout["Footer"].Update(RenderStatusBar());

        return layout;
    }
}
