namespace Kcode.Core;

public static class ConfigHelper
{
    public static T Get<T>(dynamic config, T fallback, params string[] path)
    {
        try
        {
            object? current = config;
            foreach (var key in path)
            {
                if (current is IDictionary<object, object> dict && dict.TryGetValue(key, out var next))
                {
                    current = next;
                }
                else
                {
                    return fallback;
                }
            }

            if (current is T t) return t;
            return (T)Convert.ChangeType(current, typeof(T));
        }
        catch
        {
            return fallback;
        }
    }

    public static List<string> GetStringList(dynamic config, params string[] path)
    {
        try
        {
            object? current = config;
            foreach (var key in path)
            {
                if (current is IDictionary<object, object> dict && dict.TryGetValue(key, out var next))
                {
                    current = next;
                }
                else
                {
                    return new List<string>();
                }
            }

            var result = new List<string>();
            switch (current)
            {
                case IEnumerable<object> enumerable:
                    foreach (var item in enumerable)
                    {
                        if (item != null) result.Add(item.ToString() ?? string.Empty);
                    }
                    break;
                case string s:
                    result.Add(s);
                    break;
            }
            return result;
        }
        catch
        {
            return new List<string>();
        }
    }

    public static Dictionary<string, string> GetStringMap(dynamic config, params string[] path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            object? current = config;
            foreach (var key in path)
            {
                if (current is IDictionary<object, object> dict && dict.TryGetValue(key, out var next))
                {
                    current = next;
                }
                else
                {
                    return result;
                }
            }

            if (current is IDictionary<object, object> kvs)
            {
                foreach (var kv in kvs)
                {
                    var name = kv.Key.ToString() ?? string.Empty;
                    var val = kv.Value?.ToString() ?? string.Empty;
                    result[name] = val;
                }
            }
        }
        catch
        {
            // ignore and return partial result
        }

        return result;
    }

    public static List<(string Template, string Color)> GetFooterSections(dynamic config)
    {
        return GetTemplateColorList(config, "ui", "footer", "sections");
    }

    public static List<(string Template, string Color)> GetTemplateColorList(dynamic config, params string[] path)
    {
        var sections = new List<(string Template, string Color)>();

        try
        {
            object? current = config;
            foreach (var key in path)
            {
                if (current is IDictionary<object, object> dict && dict.TryGetValue(key, out var next))
                {
                    current = next;
                }
                else
                {
                    return sections;
                }
            }

            if (current is IEnumerable<object> list)
            {
                foreach (var item in list)
                {
                    if (item is IDictionary<object, object> sec)
                    {
                        var tpl = sec.TryGetValue("template", out var tplObj) ? tplObj?.ToString() ?? "" : "";
                        var color = sec.TryGetValue("color", out var cObj) ? cObj?.ToString() ?? "grey" : "grey";
                        if (!string.IsNullOrWhiteSpace(tpl))
                        {
                            sections.Add((tpl, color));
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return sections;
    }
}
