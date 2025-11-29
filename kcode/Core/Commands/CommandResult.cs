using Spectre.Console.Rendering;

namespace Kcode.Core.Commands;

/// <summary>
/// 命令执行结果（UI 无关）
/// </summary>
public class CommandExecutionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 输出文本 (Spectre markup 由上层决定是否使用)
    /// </summary>
    public string Output { get; set; } = "";

    /// <summary>
    /// 可渲染对象（优先使用，避免 markup 解析错误）
    /// </summary>
    public IRenderable? Renderable { get; set; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>
    /// 是否请求退出程序
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// 是否请求清屏
    /// </summary>
    public bool ShouldClear { get; set; }

    /// <summary>
    /// 是否提示展示状态面板
    /// </summary>
    public bool ShouldShowStatus { get; set; }

    /// <summary>
    /// 是否需要全屏展示
    /// </summary>
    public bool RequiresFullScreen { get; set; }
}
