using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Kcode.UI;

namespace Kcode.Core.Config;

/// <summary>
/// 配置加载器
/// 支持 imports、变量引用和环境变量
/// </summary>
public class ConfigLoader
{
    private readonly IDeserializer _deserializer;
    private readonly HashSet<string> _loadedFiles = new();
    private readonly Dictionary<string, object?> _variableCache = new();

    public ConfigLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// 加载配置文件
    /// </summary>
    public RootConfig Load(string configPath)
    {
        _loadedFiles.Clear();
        _variableCache.Clear();

        configPath = ConfigPathResolver.Normalize(configPath);

        try
        {
            var rootConfig = LoadConfigFile(configPath);

            // 解析变量引用和环境变量
            var yaml = SerializeToYaml(rootConfig);
            yaml = ResolveVariables(yaml, rootConfig);
            yaml = ResolveEnvironmentVariables(yaml);

            // 反序列化最终配置
            var finalConfig = _deserializer.Deserialize<RootConfig>(yaml);

            return finalConfig;
        }
        catch (Exception ex)
        {
            MessageSystem.ShowError($"Failed to load config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 加载配置文件 (递归处理 imports)
    /// </summary>
    private RootConfig LoadConfigFile(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Config file not found: {configPath}");
        }

        var absolutePath = Path.GetFullPath(configPath);

        if (_loadedFiles.Contains(absolutePath))
        {
            throw new InvalidOperationException($"Circular import detected: {configPath}");
        }

        _loadedFiles.Add(absolutePath);

        var yaml = File.ReadAllText(absolutePath);
        var config = _deserializer.Deserialize<RootConfig>(yaml);

        // 处理 imports
        if (config.Imports.Any())
        {
            var baseDir = Path.GetDirectoryName(absolutePath) ?? "";

            foreach (var importPath in config.Imports)
            {
                var fullImportPath = Path.IsPathRooted(importPath)
                    ? importPath
                    : Path.Combine(baseDir, importPath);

                var importedConfig = LoadConfigFile(fullImportPath);

                // 合并配置 (主配置优先)
                config = MergeConfigs(config, importedConfig);
            }
        }

        return config;
    }

    /// <summary>
    /// 合并两个配置 (baseConfig 优先)
    /// </summary>
    private RootConfig MergeConfigs(RootConfig baseConfig, RootConfig importedConfig)
    {
        // Transport
        if (string.IsNullOrEmpty(baseConfig.Transport.Type))
        {
            baseConfig.Transport = importedConfig.Transport;
        }

        // API - 合并字典
        foreach (var kvp in importedConfig.Api)
        {
            if (!baseConfig.Api.ContainsKey(kvp.Key))
            {
                baseConfig.Api[kvp.Key] = kvp.Value;
            }
        }

        // Commands - 合并命令
        MergeDictionaries(baseConfig.Commands.System, importedConfig.Commands.System);
        MergeDictionaries(baseConfig.Commands.ApiCommands, importedConfig.Commands.ApiCommands);
        MergeDictionaries(baseConfig.Commands.Macros, importedConfig.Commands.Macros);
        MergeDictionaries(baseConfig.Commands.Aliases, importedConfig.Commands.Aliases);
        MergeDictionaries(baseConfig.Commands.Shortcuts, importedConfig.Commands.Shortcuts);

        // Layout - 合并区域
        if (baseConfig.Layout.Regions.Count == 0)
        {
            baseConfig.Layout = importedConfig.Layout;
        }
        else
        {
            foreach (var kvp in importedConfig.Layout.Regions)
            {
                if (!baseConfig.Layout.Regions.ContainsKey(kvp.Key))
                {
                    baseConfig.Layout.Regions[kvp.Key] = kvp.Value;
                }
            }
        }

        // Theme - 合并颜色和图标
        if (string.IsNullOrEmpty(baseConfig.Theme.Name))
        {
            baseConfig.Theme = importedConfig.Theme;
        }

        // Bindings
        if (baseConfig.Bindings.Status == null)
        {
            baseConfig.Bindings.Status = importedConfig.Bindings.Status;
        }
        if (baseConfig.Bindings.Meta == null)
        {
            baseConfig.Bindings.Meta = importedConfig.Bindings.Meta;
        }

        return baseConfig;
    }

    /// <summary>
    /// 合并字典 (target 优先)
    /// </summary>
    private void MergeDictionaries<TKey, TValue>(Dictionary<TKey, TValue> target, Dictionary<TKey, TValue> source)
        where TKey : notnull
    {
        foreach (var kvp in source)
        {
            if (!target.ContainsKey(kvp.Key))
            {
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// 解析变量引用 {path.to.value}
    /// </summary>
    private string ResolveVariables(string yaml, RootConfig config)
    {
        // 正则: 匹配 {path.to.value}
        var variablePattern = @"\{([a-zA-Z_][\w.]*)\}";

        return Regex.Replace(yaml, variablePattern, match =>
        {
            var path = match.Groups[1].Value;
            var value = GetValueByPath(config, path);
            return value?.ToString() ?? match.Value;
        });
    }

    /// <summary>
    /// 解析环境变量 ${ENV_VAR}
    /// </summary>
    private string ResolveEnvironmentVariables(string yaml)
    {
        // 正则: 匹配 ${ENV_VAR}
        var envPattern = @"\$\{([A-Z_][A-Z0-9_]*)\}";

        return Regex.Replace(yaml, envPattern, match =>
        {
            var envVar = match.Groups[1].Value;
            var value = Environment.GetEnvironmentVariable(envVar);

            if (string.IsNullOrEmpty(value))
            {
                MessageSystem.ShowWarning($"Environment variable not found: {envVar}");
                return match.Value;
            }

            return value;
        });
    }

    /// <summary>
    /// 通过路径获取配置值
    /// </summary>
    private object? GetValueByPath(RootConfig config, string path)
    {
        if (_variableCache.TryGetValue(path, out var cachedValue))
        {
            return cachedValue;
        }

        var parts = path.Split('.');
        object? current = config;

        foreach (var part in parts)
        {
            if (current == null) break;

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
            else
            {
                // 尝试作为字典访问
                if (current is System.Collections.IDictionary dict && dict.Contains(part))
                {
                    current = dict[part];
                }
                else
                {
                    return null;
                }
            }
        }

        _variableCache[path] = current;
        return current;
    }

    /// <summary>
    /// 序列化配置为 YAML
    /// </summary>
    private string SerializeToYaml(RootConfig config)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        return serializer.Serialize(config);
    }
}
