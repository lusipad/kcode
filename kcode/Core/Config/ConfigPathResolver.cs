using System;
using System.Collections.Generic;
using System.IO;

namespace Kcode.Core.Config;

/// <summary>
/// 提供配置文件路径的解析和探测。
/// </summary>
internal static class ConfigPathResolver
{
    private static readonly string[] CandidateFolders = ["", "Config", "config"];
    private static readonly string[] CandidateFiles = ["config-virtual.yaml", "config.yaml"];

    /// <summary>
    /// 将用户提供的路径（文件或目录）标准化为绝对文件路径。
    /// </summary>
    public static string Normalize(string pathOrDirectory)
    {
        if (string.IsNullOrWhiteSpace(pathOrDirectory))
        {
            throw new ArgumentException("Config path cannot be empty.", nameof(pathOrDirectory));
        }

        var absolute = Path.IsPathRooted(pathOrDirectory)
            ? pathOrDirectory
            : Path.GetFullPath(pathOrDirectory, Directory.GetCurrentDirectory());

        if (Directory.Exists(absolute))
        {
            var resolvedFromDirectory = FindInDirectory(absolute);
            if (resolvedFromDirectory != null)
            {
                return resolvedFromDirectory;
            }
        }

        return Path.GetFullPath(absolute);
    }

    /// <summary>
    /// 在指定目录下搜索默认配置文件。
    /// </summary>
    public static string? FindInDirectory(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory))
        {
            return null;
        }

        foreach (var folder in CandidateFolders)
        {
            var dir = string.IsNullOrEmpty(folder)
                ? baseDirectory
                : Path.Combine(baseDirectory, folder);

            foreach (var file in CandidateFiles)
            {
                var candidate = Path.Combine(dir, file);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 从多个根目录中按顺序查找配置文件。
    /// </summary>
    public static string? ProbeDefaultLocations(IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var normalizedRoot = Path.IsPathRooted(root)
                ? Path.GetFullPath(root)
                : Path.GetFullPath(root, Directory.GetCurrentDirectory());

            var resolved = FindInDirectory(normalizedRoot);
            if (resolved != null)
            {
                return resolved;
            }
        }

        return null;
    }
}
