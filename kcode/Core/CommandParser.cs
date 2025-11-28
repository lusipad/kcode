using System.Text.RegularExpressions;

namespace Kcode.Core;

public class CommandParser
{
    // Regex to match G-code parameters like X10.5, Y-5, F100
    private static readonly Regex ParamRegex = new Regex(@"([A-Z])\s*(-?\d+(\.\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static CncCommand Parse(string input)
    {
        var cmd = new CncCommand
        {
            OriginalText = input,
            Type = CommandType.Unknown
        };

        if (string.IsNullOrWhiteSpace(input)) return cmd;

        input = input.Trim();

        if (input.StartsWith("/"))
        {
            cmd.Type = CommandType.System;
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            cmd.Name = parts[0].ToLower();
            if (parts.Length > 1)
            {
                cmd.Args = parts.Skip(1).ToArray();
            }
            return cmd;
        }

        // Assume G-Code or Macro
        var upperInput = input.ToUpper();
        var parts_gc = upperInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts_gc.Length > 0)
        {
             cmd.Name = parts_gc[0];
             
             if (cmd.Name.StartsWith("G") || cmd.Name.StartsWith("M"))
             {
                 cmd.Type = CommandType.GCode;
             }
             else
             {
                 cmd.Type = CommandType.Macro;
             }

             // Parse parameters
             foreach (Match match in ParamRegex.Matches(upperInput))
             {
                 var code = match.Groups[1].Value;
                 if (double.TryParse(match.Groups[2].Value, out double val))
                 {
                     cmd.Parameters[code] = val;
                 }
             }
        }

        return cmd;
    }
}

public enum CommandType
{
    Unknown,
    GCode,
    System,
    Macro
}

public class CncCommand
{
    public string OriginalText { get; set; } = "";
    public CommandType Type { get; set; }
    public string Name { get; set; } = ""; // e.g., "G0", "/help"
    public Dictionary<string, double> Parameters { get; set; } = new();
    public string[] Args { get; set; } = Array.Empty<string>();

    public double? GetParam(string key)
    {
        if (Parameters.TryGetValue(key, out double val)) return val;
        return null;
    }
}
