using System.Text;
using System.Text.RegularExpressions;

namespace Kcode.Core.Template;

/// <summary>
/// 模板引擎
/// 支持变量替换、条件渲染和循环渲染
/// </summary>
public class TemplateEngine
{
    /// <summary>
    /// 渲染模板
    /// </summary>
    public string Render(string template, Dictionary<string, object?> context)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return "";
        }

        try
        {
            // 1. 处理 if 条件
            template = ProcessConditionals(template, context);

            // 2. 处理 range 循环
            template = ProcessRanges(template, context);

            // 3. 处理变量替换
            template = ProcessVariables(template, context);

            return template;
        }
        catch (Exception ex)
        {
            return $"[red]Template error: {ex.Message}[/]";
        }
    }

    /// <summary>
    /// 处理条件语句 {{if .field}}...{{end}}
    /// </summary>
    private string ProcessConditionals(string template, Dictionary<string, object?> context)
    {
        // 匹配 {{if .field}}...{{else}}...{{end}}
        var pattern = @"\{\{if\s+\.(\w+)\}\}(.*?)(?:\{\{else\}\}(.*?))?\{\{end\}\}";

        return Regex.Replace(template, pattern, match =>
        {
            var fieldName = match.Groups[1].Value;
            var trueBlock = match.Groups[2].Value;
            var falseBlock = match.Groups.Count > 3 ? match.Groups[3].Value : "";

            var value = GetValue(context, fieldName);
            var isTrue = IsTruthy(value);

            return isTrue ? trueBlock : falseBlock;
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// 处理循环 {{range .items}}...{{end}}
    /// </summary>
    private string ProcessRanges(string template, Dictionary<string, object?> context)
    {
        // 匹配 {{range .field}}...{{end}}
        var pattern = @"\{\{range\s+\.(\w+)\}\}(.*?)\{\{end\}\}";

        return Regex.Replace(template, pattern, match =>
        {
            var fieldName = match.Groups[1].Value;
            var itemTemplate = match.Groups[2].Value;

            var value = GetValue(context, fieldName);

            if (value is System.Collections.IEnumerable enumerable)
            {
                var result = new StringBuilder();

                foreach (var item in enumerable)
                {
                    // 为每个项创建新的上下文
                    var itemContext = new Dictionary<string, object?>();

                    if (item is Dictionary<string, object?> dict)
                    {
                        foreach (var kvp in dict)
                        {
                            itemContext[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        itemContext["item"] = item;
                    }

                    // 渲染项模板
                    var rendered = ProcessVariables(itemTemplate, itemContext);
                    result.AppendLine(rendered);
                }

                return result.ToString();
            }

            return "";
        }, RegexOptions.Singleline);
    }

    /// <summary>
    /// 处理变量替换 {{.field}}
    /// </summary>
    private string ProcessVariables(string template, Dictionary<string, object?> context)
    {
        // 匹配 {{.field}} 或 {{.field:format}}
        var pattern = @"\{\{\.(\w+)(?::([^}]+))?\}\}";

        return Regex.Replace(template, pattern, match =>
        {
            var fieldName = match.Groups[1].Value;
            var format = match.Groups.Count > 2 ? match.Groups[2].Value : null;

            var value = GetValue(context, fieldName);

            return FormatValue(value, format);
        });
    }

    /// <summary>
    /// 获取上下文值
    /// </summary>
    private object? GetValue(Dictionary<string, object?> context, string key)
    {
        if (context.TryGetValue(key, out var value))
        {
            return value;
        }

        // 尝试不区分大小写
        var entry = context.FirstOrDefault(kvp =>
            string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase));

        return entry.Value;
    }

    /// <summary>
    /// 判断值是否为真
    /// </summary>
    private bool IsTruthy(object? value)
    {
        if (value == null) return false;

        if (value is bool boolValue) return boolValue;
        if (value is int intValue) return intValue != 0;
        if (value is double doubleValue) return doubleValue != 0.0;
        if (value is string stringValue) return !string.IsNullOrWhiteSpace(stringValue);
        if (value is System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Any();
        }

        return true;
    }

    /// <summary>
    /// 格式化值
    /// </summary>
    private string FormatValue(object? value, string? format)
    {
        if (value == null) return "";

        // 如果没有格式，直接返回字符串
        if (string.IsNullOrEmpty(format))
        {
            return value.ToString() ?? "";
        }

        // 尝试应用格式
        try
        {
            if (value is IFormattable formattable)
            {
                return formattable.ToString(format, null);
            }

            return value.ToString() ?? "";
        }
        catch
        {
            return value.ToString() ?? "";
        }
    }
}

/// <summary>
/// 模板上下文构建器
/// </summary>
public class TemplateContext
{
    private readonly Dictionary<string, object?> _context = new();

    public TemplateContext Set(string key, object? value)
    {
        _context[key] = value;
        return this;
    }

    public TemplateContext SetMany(Dictionary<string, object?> values)
    {
        foreach (var kvp in values)
        {
            _context[kvp.Key] = kvp.Value;
        }
        return this;
    }

    public Dictionary<string, object?> Build()
    {
        return _context;
    }

    /// <summary>
    /// 从 TransportResponse 创建上下文
    /// </summary>
    public static Dictionary<string, object?> FromResponse(Transport.TransportResponse response)
    {
        return response.Data;
    }
}
