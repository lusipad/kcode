using YamlDotNet.Serialization;

namespace Kcode.Core.Config;

/// <summary>
/// 根配置模型
/// </summary>
public class RootConfig
{
    [YamlMember(Alias = "app")]
    public AppConfig App { get; set; } = new();

    [YamlMember(Alias = "transport")]
    public TransportConfig Transport { get; set; } = new();

    [YamlMember(Alias = "api")]
    public Dictionary<string, ApiEndpointConfig> Api { get; set; } = new();

    [YamlMember(Alias = "commands")]
    public CommandsConfig Commands { get; set; } = new();

    [YamlMember(Alias = "layout")]
    public LayoutConfig Layout { get; set; } = new();

    [YamlMember(Alias = "theme")]
    public ThemeConfig Theme { get; set; } = new();

    [YamlMember(Alias = "bindings")]
    public BindingsConfig Bindings { get; set; } = new();

    [YamlMember(Alias = "imports")]
    public List<string> Imports { get; set; } = new();
}

/// <summary>
/// 应用配置
/// </summary>
public class AppConfig
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "kcode";

    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "2.0.0";

    [YamlMember(Alias = "workspace")]
    public string Workspace { get; set; } = "KCode";

    [YamlMember(Alias = "model")]
    public string Model { get; set; } = "";

    [YamlMember(Alias = "usage")]
    public string Usage { get; set; } = "";

    [YamlMember(Alias = "working_directory")]
    public string WorkingDirectory { get; set; } = "D:\\Repos\\kcode";

    [YamlMember(Alias = "header_welcome")]
    public string HeaderWelcome { get; set; } = "欢迎回来！";

    [YamlMember(Alias = "prompt_placeholder")]
    public string PromptPlaceholder { get; set; } = "输入命令，例如 help";

    [YamlMember(Alias = "slogan")]
    public string Slogan { get; set; } = "现代化命令行 CNC 控制终端";

    [YamlMember(Alias = "suggestion_text")]
    public string SuggestionText { get; set; } = "Try \"/help\" to list commands.";

    [YamlMember(Alias = "branch")]
    public string Branch { get; set; } = "main";

    [YamlMember(Alias = "tokens")]
    public string Tokens { get; set; } = "0 tokens";

    [YamlMember(Alias = "permissions")]
    public string Permissions { get; set; } = "v2 mode";
}

#region 传输层配置

/// <summary>
/// 传输层配置 (支持 gRPC 和 REST)
/// </summary>
public class TransportConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "virtual"; // "grpc", "rest", "virtual"

    [YamlMember(Alias = "endpoint")]
    public string? Endpoint { get; set; }

    [YamlMember(Alias = "base_url")]
    public string? BaseUrl { get; set; }

    [YamlMember(Alias = "timeout_ms")]
    public int TimeoutMs { get; set; } = 5000;

    [YamlMember(Alias = "reconnect_interval_ms")]
    public int ReconnectIntervalMs { get; set; } = 3000;

    [YamlMember(Alias = "tls")]
    public TlsConfig? Tls { get; set; }

    [YamlMember(Alias = "auth")]
    public AuthConfig? Auth { get; set; }

    [YamlMember(Alias = "headers")]
    public Dictionary<string, string> Headers { get; set; } = new();

    [YamlMember(Alias = "services")]
    public Dictionary<string, GrpcServiceConfig>? Services { get; set; }

    [YamlMember(Alias = "endpoints")]
    public Dictionary<string, RestEndpointConfig>? Endpoints { get; set; }

    [YamlMember(Alias = "websocket")]
    public WebSocketConfig? WebSocket { get; set; }
}

/// <summary>
/// TLS 配置
/// </summary>
public class TlsConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = false;

    [YamlMember(Alias = "cert_path")]
    public string? CertPath { get; set; }
}

/// <summary>
/// 认证配置
/// </summary>
public class AuthConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "none"; // "none", "basic", "bearer", "api_key"

    [YamlMember(Alias = "token")]
    public string? Token { get; set; }

    [YamlMember(Alias = "username")]
    public string? Username { get; set; }

    [YamlMember(Alias = "password")]
    public string? Password { get; set; }
}

/// <summary>
/// gRPC 服务配置
/// </summary>
public class GrpcServiceConfig
{
    [YamlMember(Alias = "package")]
    public string Package { get; set; } = "";

    [YamlMember(Alias = "methods")]
    public Dictionary<string, GrpcMethodConfig> Methods { get; set; } = new();
}

/// <summary>
/// gRPC 方法配置
/// </summary>
public class GrpcMethodConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "unary"; // "unary", "server_stream", "client_stream", "bidi_stream"

    [YamlMember(Alias = "request")]
    public Dictionary<string, string> Request { get; set; } = new();

    [YamlMember(Alias = "response")]
    public Dictionary<string, string> Response { get; set; } = new();
}

/// <summary>
/// REST 端点配置
/// </summary>
public class RestEndpointConfig
{
    [YamlMember(Alias = "method")]
    public string Method { get; set; } = "GET"; // "GET", "POST", "PUT", "DELETE"

    [YamlMember(Alias = "path")]
    public string Path { get; set; } = "";

    [YamlMember(Alias = "request")]
    public RestRequestConfig? Request { get; set; }

    [YamlMember(Alias = "response")]
    public Dictionary<string, string> Response { get; set; } = new();

    [YamlMember(Alias = "polling")]
    public PollingConfig? Polling { get; set; }
}

/// <summary>
/// REST 请求配置
/// </summary>
public class RestRequestConfig
{
    [YamlMember(Alias = "path_params")]
    public Dictionary<string, string>? PathParams { get; set; }

    [YamlMember(Alias = "query_params")]
    public Dictionary<string, string>? QueryParams { get; set; }

    [YamlMember(Alias = "body")]
    public Dictionary<string, string>? Body { get; set; }
}

/// <summary>
/// 轮询配置
/// </summary>
public class PollingConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = false;

    [YamlMember(Alias = "interval_ms")]
    public int IntervalMs { get; set; } = 100;
}

/// <summary>
/// WebSocket 配置
/// </summary>
public class WebSocketConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = false;

    [YamlMember(Alias = "url")]
    public string Url { get; set; } = "";

    [YamlMember(Alias = "reconnect_interval_ms")]
    public int ReconnectIntervalMs { get; set; } = 3000;

    [YamlMember(Alias = "subscriptions")]
    public Dictionary<string, WebSocketSubscriptionConfig> Subscriptions { get; set; } = new();
}

/// <summary>
/// WebSocket 订阅配置
/// </summary>
public class WebSocketSubscriptionConfig
{
    [YamlMember(Alias = "message_type")]
    public string MessageType { get; set; } = "";

    [YamlMember(Alias = "fields")]
    public Dictionary<string, string> Fields { get; set; } = new();
}

#endregion

#region API 接口定义

/// <summary>
/// API 端点配置 (协议无关的接口定义)
/// </summary>
public class ApiEndpointConfig
{
    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "stream")]
    public bool Stream { get; set; } = false;

    [YamlMember(Alias = "request")]
    public Dictionary<string, string> Request { get; set; } = new();

    [YamlMember(Alias = "response")]
    public Dictionary<string, string> Response { get; set; } = new();
}

#endregion

#region 命令系统配置

/// <summary>
/// 命令配置
/// </summary>
public class CommandsConfig
{
    [YamlMember(Alias = "system")]
    public Dictionary<string, SystemCommandConfig> System { get; set; } = new();

    [YamlMember(Alias = "api")]
    public Dictionary<string, ApiCommandConfig> ApiCommands { get; set; } = new();

    [YamlMember(Alias = "macros")]
    public Dictionary<string, MacroCommandConfig> Macros { get; set; } = new();

    [YamlMember(Alias = "aliases")]
    public Dictionary<string, string> Aliases { get; set; } = new();

    [YamlMember(Alias = "shortcuts")]
    public Dictionary<string, ShortcutConfig> Shortcuts { get; set; } = new();
}

/// <summary>
/// 系统命令配置
/// </summary>
public class SystemCommandConfig
{
    [YamlMember(Alias = "aliases")]
    public List<string> Aliases { get; set; } = new();

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "action")]
    public string Action { get; set; } = "";
}

/// <summary>
/// API 命令配置
/// </summary>
public class ApiCommandConfig
{
    [YamlMember(Alias = "pattern")]
    public string Pattern { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = "";

    [YamlMember(Alias = "request_mapping")]
    public Dictionary<string, string> RequestMapping { get; set; } = new();

    [YamlMember(Alias = "response_template")]
    public string ResponseTemplate { get; set; } = "";

    [YamlMember(Alias = "response_render")]
    public string? ResponseRender { get; set; }

    [YamlMember(Alias = "table_config")]
    public TableConfig? TableConfig { get; set; }
}

/// <summary>
/// 宏命令配置
/// </summary>
public class MacroCommandConfig
{
    [YamlMember(Alias = "aliases")]
    public List<string> Aliases { get; set; } = new();

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "steps")]
    public List<MacroStepConfig> Steps { get; set; } = new();

    [YamlMember(Alias = "response_template")]
    public string ResponseTemplate { get; set; } = "";
}

/// <summary>
/// 宏步骤配置
/// </summary>
public class MacroStepConfig
{
    [YamlMember(Alias = "endpoint")]
    public string Endpoint { get; set; } = "";

    [YamlMember(Alias = "request")]
    public Dictionary<string, object> Request { get; set; } = new();
}

/// <summary>
/// 快捷键配置
/// </summary>
public class ShortcutConfig
{
    [YamlMember(Alias = "action")]
    public string Action { get; set; } = "";

    [YamlMember(Alias = "feedback")]
    public string Feedback { get; set; } = "";
}

/// <summary>
/// 表格配置
/// </summary>
public class TableConfig
{
    [YamlMember(Alias = "title")]
    public string Title { get; set; } = "";

    [YamlMember(Alias = "columns")]
    public List<TableColumnConfig> Columns { get; set; } = new();
}

/// <summary>
/// 表格列配置
/// </summary>
public class TableColumnConfig
{
    [YamlMember(Alias = "header")]
    public string Header { get; set; } = "";

    [YamlMember(Alias = "field")]
    public string Field { get; set; } = "";

    [YamlMember(Alias = "color")]
    public string? Color { get; set; }
}

#endregion

#region 布局系统配置

/// <summary>
/// 布局配置
/// </summary>
public class LayoutConfig
{
    [YamlMember(Alias = "structure")]
    public LayoutStructureConfig Structure { get; set; } = new();

    [YamlMember(Alias = "regions")]
    public Dictionary<string, RegionConfig> Regions { get; set; } = new();
}

/// <summary>
/// 布局结构配置
/// </summary>
public class LayoutStructureConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "rows"; // "rows", "columns", "grid"

    [YamlMember(Alias = "children")]
    public List<LayoutChildConfig> Children { get; set; } = new();
}

/// <summary>
/// 布局子元素配置
/// </summary>
public class LayoutChildConfig
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = "";

    [YamlMember(Alias = "size")]
    public int? Size { get; set; }

    [YamlMember(Alias = "ratio")]
    public int? Ratio { get; set; }
}

/// <summary>
/// 区域配置
/// </summary>
public class RegionConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "panel"; // "panel", "grid", "rows", "columns"

    [YamlMember(Alias = "border")]
    public string? Border { get; set; }

    [YamlMember(Alias = "border_color")]
    public string? BorderColor { get; set; }

    [YamlMember(Alias = "padding")]
    public List<int>? Padding { get; set; }

    [YamlMember(Alias = "content")]
    public ComponentConfig Content { get; set; } = new();
}

/// <summary>
/// 组件配置
/// </summary>
public class ComponentConfig
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "text"; // "text", "history", "input", "status_bar", "divider", etc.

    [YamlMember(Alias = "value")]
    public string? Value { get; set; }

    [YamlMember(Alias = "color")]
    public string? Color { get; set; }

    [YamlMember(Alias = "style")]
    public string? Style { get; set; }

    [YamlMember(Alias = "columns")]
    public int? Columns { get; set; }

    [YamlMember(Alias = "children")]
    public List<ComponentConfig>? Children { get; set; }

    [YamlMember(Alias = "lines")]
    public string? Lines { get; set; }

    [YamlMember(Alias = "items")]
    public string? Items { get; set; }

    [YamlMember(Alias = "title")]
    public string? Title { get; set; }

    [YamlMember(Alias = "sections")]
    public string? Sections { get; set; }

    [YamlMember(Alias = "badges")]
    public string? Badges { get; set; }

    [YamlMember(Alias = "prefix")]
    public string? Prefix { get; set; }

    [YamlMember(Alias = "prefix_color")]
    public string? PrefixColor { get; set; }

    [YamlMember(Alias = "text_color")]
    public string? TextColor { get; set; }

    [YamlMember(Alias = "cursor")]
    public string? Cursor { get; set; }

    [YamlMember(Alias = "cursor_color")]
    public string? CursorColor { get; set; }

    [YamlMember(Alias = "empty_text")]
    public string? EmptyText { get; set; }

    [YamlMember(Alias = "bindings")]
    public Dictionary<string, string>? Bindings { get; set; }
}

#endregion

#region 主题系统配置

/// <summary>
/// 主题配置
/// </summary>
public class ThemeConfig
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "Claude 暗色";

    [YamlMember(Alias = "colors")]
    public ThemeColorsConfig Colors { get; set; } = new();

    [YamlMember(Alias = "icons")]
    public ThemeIconsConfig Icons { get; set; } = new();
}

/// <summary>
/// 主题颜色配置
/// </summary>
public class ThemeColorsConfig
{
    [YamlMember(Alias = "background")]
    public string Background { get; set; } = "#000000";

    [YamlMember(Alias = "foreground")]
    public string Foreground { get; set; } = "#F4E3D7";

    [YamlMember(Alias = "accent_primary")]
    public string AccentPrimary { get; set; } = "#FF7043";

    [YamlMember(Alias = "accent_secondary")]
    public string AccentSecondary { get; set; } = "#4DD0E1";

    [YamlMember(Alias = "accent_tertiary")]
    public string AccentTertiary { get; set; } = "#CE93D8";

    [YamlMember(Alias = "success")]
    public string Success { get; set; } = "#66BB6A";

    [YamlMember(Alias = "warning")]
    public string Warning { get; set; } = "#FFEE58";

    [YamlMember(Alias = "error")]
    public string Error { get; set; } = "#EF5350";

    [YamlMember(Alias = "panel_border")]
    public string PanelBorder { get; set; } = "#FF7043";

    [YamlMember(Alias = "panel_divider")]
    public string PanelDivider { get; set; } = "#F57C00";

    [YamlMember(Alias = "header_text")]
    public string HeaderText { get; set; } = "#F4E3D7";

    [YamlMember(Alias = "prompt_border")]
    public string PromptBorder { get; set; } = "#7E57C2";

    [YamlMember(Alias = "prompt_text")]
    public string PromptText { get; set; } = "#EDE7F6";

    [YamlMember(Alias = "footer_notice")]
    public string FooterNotice { get; set; } = "#FF4081";

    [YamlMember(Alias = "footer_badge")]
    public string FooterBadge { get; set; } = "#4DD0E1";

    [YamlMember(Alias = "state_colors")]
    public Dictionary<string, string> StateColors { get; set; } = new()
    {
        ["IDLE"] = "green",
        ["RUN"] = "cyan",
        ["HOLD"] = "yellow",
        ["ALARM"] = "red"
    };
}

/// <summary>
/// 主题图标配置
/// </summary>
public class ThemeIconsConfig
{
    [YamlMember(Alias = "enabled")]
    public bool Enabled { get; set; } = true;

    [YamlMember(Alias = "set")]
    public Dictionary<string, string> Set { get; set; } = new()
    {
        ["success"] = "✓",
        ["error"] = "✗",
        ["warning"] = "⚠",
        ["info"] = "ℹ",
        ["home"] = "🏠",
        ["tool"] = "🔧",
        ["temp"] = "🌡️",
        ["speed"] = "🚀",
        ["position"] = "📍",
        ["alarm"] = "🚨",
        ["pause"] = "⏸",
        ["play"] = "▶",
        ["stop"] = "⏹"
    };
}

#endregion

#region 数据绑定配置

/// <summary>
/// 数据绑定配置
/// </summary>
public class BindingsConfig
{
    [YamlMember(Alias = "status")]
    public DataBindingConfig? Status { get; set; }

    [YamlMember(Alias = "meta")]
    public DataBindingConfig? Meta { get; set; }
}

/// <summary>
/// 数据绑定配置
/// </summary>
public class DataBindingConfig
{
    [YamlMember(Alias = "source")]
    public string Source { get; set; } = "";

    [YamlMember(Alias = "refresh_ms")]
    public int? RefreshMs { get; set; }

    [YamlMember(Alias = "fields")]
    public Dictionary<string, FieldBindingConfig> Fields { get; set; } = new();
}

/// <summary>
/// 字段绑定配置
/// </summary>
public class FieldBindingConfig
{
    [YamlMember(Alias = "path")]
    public string Path { get; set; } = "";

    [YamlMember(Alias = "format")]
    public string? Format { get; set; }

    [YamlMember(Alias = "transform")]
    public Dictionary<string, string>? Transform { get; set; }
}

#endregion
