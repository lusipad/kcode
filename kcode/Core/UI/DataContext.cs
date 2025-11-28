using System.Text.Json;
using System.Text.RegularExpressions;
using Kcode.Core.Config;

namespace Kcode.Core.UI;

/// <summary>
/// 数据上下文 - 存储和管理运行时数据
/// </summary>
public class DataContext
{
    private readonly Dictionary<string, object?> _data = new();
    private readonly RootConfig _config;

    public DataContext(RootConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 设置数据
    /// </summary>
    public void Set(string key, object? value)
    {
        _data[key] = value;
    }

    /// <summary>
    /// 批量设置数据
    /// </summary>
    public void SetMany(Dictionary<string, object?> data)
    {
        foreach (var kvp in data)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 获取数据
    /// </summary>
    public object? Get(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// 通过路径获取数据 (支持 status.x, config.app.name 等)
    /// </summary>
    public object? GetByPath(string path)
    {
        var parts = path.Split('.');
        object? current = null;

        // 第一部分：确定数据源
        var source = parts[0];

        if (source == "status" || source == "meta" || _data.ContainsKey(source))
        {
            current = _data.GetValueOrDefault(source);
        }
        else if (source == "config")
        {
            current = _config;
        }
        else if (source == "theme")
        {
            current = _config.Theme;
        }
        else
        {
            return null;
        }

        // 后续部分：遍历属性
        for (int i = 1; i < parts.Length; i++)
        {
            if (current == null) return null;

            var part = parts[i];

            // 尝试作为属性访问
            var type = current.GetType();
            var property = type.GetProperty(
                part,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase
            );

            if (property != null)
            {
                current = property.GetValue(current);
            }
            // 尝试作为字典访问
            else if (current is Dictionary<string, object?> dict)
            {
                current = dict.GetValueOrDefault(part);
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    /// <summary>
    /// 获取格式化的值
    /// </summary>
    public string GetFormatted(string path, string? format = null)
    {
        var value = GetByPath(path);

        if (value == null)
        {
            return "";
        }

        // 应用格式
        if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
        {
            return formattable.ToString(format, null);
        }

        return value.ToString() ?? "";
    }

    /// <summary>
    /// 解析绑定表达式 {status.x:F3}
    /// </summary>
    public string ResolveBinding(string expression)
    {
        // 匹配 {path:format} 或 {path}
        var pattern = @"\{([a-zA-Z_][\w.]*?)(?::([^}]+))?\}";

        return Regex.Replace(expression, pattern, match =>
        {
            var path = match.Groups[1].Value;
            var format = match.Groups.Count > 2 ? match.Groups[2].Value : null;

            return GetFormatted(path, format);
        });
    }

    /// <summary>
    /// 批量解析绑定
    /// </summary>
    public Dictionary<string, string> ResolveBindings(Dictionary<string, string>? bindings)
    {
        if (bindings == null)
        {
            return new Dictionary<string, string>();
        }

        var result = new Dictionary<string, string>();

        foreach (var kvp in bindings)
        {
            result[kvp.Key] = ResolveBinding(kvp.Value);
        }

        return result;
    }

    /// <summary>
    /// 获取状态数据
    /// </summary>
    public StatusData GetStatus()
    {
        var statusDict = _data.GetValueOrDefault("status") as Dictionary<string, object?>;

        return new StatusData
        {
            X = GetDouble(statusDict, "x"),
            Y = GetDouble(statusDict, "y"),
            Z = GetDouble(statusDict, "z"),
            Feed = GetDouble(statusDict, "feed"),
            Speed = GetDouble(statusDict, "speed"),
            State = GetString(statusDict, "state"),
            Alarm = GetString(statusDict, "alarm"),
            Temp = GetDouble(statusDict, "temp"),
            StateIcon = GetStateIcon(GetString(statusDict, "state"))
        };
    }

    /// <summary>
    /// 获取元数据
    /// </summary>
    public MetaData GetMeta()
    {
        var metaDict = _data.GetValueOrDefault("meta") as Dictionary<string, object?>;

        return new MetaData
        {
            Model = GetString(metaDict, "model"),
            Workspace = GetString(metaDict, "workspace"),
            Branch = GetString(metaDict, "branch"),
            Tokens = GetString(metaDict, "tokens"),
            Permissions = GetString(metaDict, "permissions")
        };
    }

    /// <summary>
    /// 获取状态图标
    /// </summary>
    private string GetStateIcon(string state)
    {
        return state?.ToUpper() switch
        {
            "RUN" => "▶",
            "HOLD" => "⏸",
            "ALARM" => "🚨",
            "IDLE" => "●",
            _ => "○"
        };
    }

    private double GetDouble(Dictionary<string, object?>? dict, string key, double defaultValue = 0.0)
    {
        if (dict == null) return defaultValue;
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        if (value == null) return defaultValue;

        try
        {
            return Convert.ToDouble(value);
        }
        catch
        {
            return defaultValue;
        }
    }

    private string GetString(Dictionary<string, object?>? dict, string key, string defaultValue = "")
    {
        if (dict == null) return defaultValue;
        if (!dict.TryGetValue(key, out var value)) return defaultValue;
        return value?.ToString() ?? defaultValue;
    }
}

/// <summary>
/// 状态数据
/// </summary>
public class StatusData
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Feed { get; set; }
    public double Speed { get; set; }
    public string State { get; set; } = "";
    public string Alarm { get; set; } = "";
    public double Temp { get; set; }
    public string StateIcon { get; set; } = "○";
}

/// <summary>
/// 元数据
/// </summary>
public class MetaData
{
    public string Model { get; set; } = "";
    public string Workspace { get; set; } = "";
    public string Branch { get; set; } = "";
    public string Tokens { get; set; } = "";
    public string Permissions { get; set; } = "";
}
