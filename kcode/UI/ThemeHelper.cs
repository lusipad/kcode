using System.Globalization;
using System.Linq;
using Spectre.Console;
using Kcode.Core;

namespace Kcode.UI;

public static class ThemeHelper
{
    public static Color GetColor(dynamic config, string fallbackHex, params string[] path)
    {
        var colorValue = ConfigHelper.Get(config, fallbackHex, path);
        return ParseColor(colorValue, fallbackHex);
    }

    public static string GetColorMarkup(dynamic config, string fallbackHex, params string[] path)
    {
        var color = GetColor(config, fallbackHex, path);
        return ToMarkup(color);
    }

    public static string ToMarkup(Color color) => $"#{color.ToHex()}";

    private static Color ParseColor(string value, string fallbackHex)
    {
        var parsed = TryParse(value) ?? TryParse(fallbackHex);
        return parsed ?? new Color(128, 128, 128);
    }

    public static bool UseEmojis(dynamic config) =>
        ConfigHelper.Get(config, true, "theme", "emojis");

    private static Color? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Length == 3)
        {
            trimmed = string.Concat(trimmed.Select(c => new string(c, 2)));
        }

        if (trimmed.Length != 6)
        {
            return null;
        }

        try
        {
            var r = byte.Parse(trimmed[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var g = byte.Parse(trimmed.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b = byte.Parse(trimmed.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Color(r, g, b);
        }
        catch
        {
            return null;
        }
    }
}
