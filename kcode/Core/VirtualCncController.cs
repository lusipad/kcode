using System.Collections.Generic;
using System.Globalization;

namespace Kcode.Core;

public class VirtualCncController
{
    private readonly dynamic _config;
    private readonly bool _softLimits;
    private readonly double _xMax;
    private readonly double _yMax;
    private readonly double _zMax;
    private readonly Dictionary<string, List<string>> _macros;
    private readonly Random _rand = new();

    // Coordinates (Work Coordinates)
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Z { get; private set; }

    public double Feed { get; private set; }
    public double Speed { get; private set; }
    public string State { get; private set; } = "IDLE";
    public string AlarmReason { get; private set; } = string.Empty;
    public double Temp { get; private set; } = 35.0;

    // Machine Parameters
    public Dictionary<string, double> Params { get; private set; } = new()
    {
        { "X_MAX", 500.0 },
        { "Y_MAX", 500.0 },
        { "Z_MAX", 100.0 },
        { "DEFAULT_FEED", 1000.0 },
        { "MAX_SPINDLE", 12000.0 }
    };

    public List<Tool> Tools { get; private set; } = new()
    {
        new Tool { Id = 1, Diameter = 6.0, Length = 50.0, Description = "End Mill 6mm" },
        new Tool { Id = 2, Diameter = 3.175, Length = 40.0, Description = "Ball Nose 1/8" },
        new Tool { Id = 99, Diameter = 10.0, Length = 0.0, Description = "Probe" }
    };

    public VirtualCncController(dynamic config)
    {
        _config = config;
        _macros = LoadMacros(config);

        _xMax = GetDouble(config, 500, "machine", "work_area", "x");
        _yMax = GetDouble(config, 500, "machine", "work_area", "y");
        _zMax = GetDouble(config, 100, "machine", "work_area", "z");
        _softLimits = GetBool(config, true, "machine", "soft_limits");

        Params["X_MAX"] = _xMax;
        Params["Y_MAX"] = _yMax;
        Params["Z_MAX"] = _zMax;
        Params["DEFAULT_FEED"] = GetDouble(config, 1000, "machine", "max_velocity", "x");
        Params["MAX_SPINDLE"] = GetDouble(config, 12000, "machine", "max_spindle");

        Feed = Params["DEFAULT_FEED"];
        Speed = Params["MAX_SPINDLE"] / 2;
    }

    public async Task ExecuteCommandAsync(string input)
    {
        var cmd = CommandParser.Parse(input);

        switch (cmd.Type)
        {
            case CommandType.GCode:
                await ExecuteGCodeAsync(cmd);
                break;
            case CommandType.Macro:
                await ExecuteMacroAsync(cmd.Name);
                break;
            default:
                // Unknown command types are ignored by the virtual machine.
                break;
        }
    }

    public void EStop()
    {
        State = "ALARM";
        AlarmReason = "Emergency stop activated";
    }

    public void FeedHold()
    {
        if (State == "RUN") State = "HOLD";
        else if (State == "HOLD") State = "RUN";
    }

    public MachineStatus GetStatus()
    {
        Temp = Math.Clamp(Temp + (_rand.NextDouble() - 0.5) * 0.2, 32, 55);

        return new MachineStatus
        {
            X = X,
            Y = Y,
            Z = Z,
            Feed = Feed,
            Speed = Speed,
            State = State,
            Temp = Temp,
            Alarm = AlarmReason
        };
    }

    public void ClearAlarm()
    {
        AlarmReason = string.Empty;
        if (State == "ALARM") State = "IDLE";
    }

    private async Task ExecuteMacroAsync(string name)
    {
        if (_macros.TryGetValue(name, out var lines))
        {
            foreach (var line in lines)
            {
                var inner = CommandParser.Parse(line);
                if (inner.Type == CommandType.GCode)
                {
                    await ExecuteGCodeAsync(inner);
                }
            }
            return;
        }

        // Built-in fallbacks
        if (name.Equals("HOME", StringComparison.OrdinalIgnoreCase))
        {
            await ExecuteGCodeAsync(CommandParser.Parse("G28"));
        }
        else if (name.Equals("ZERO", StringComparison.OrdinalIgnoreCase))
        {
            X = 0; Y = 0; Z = 0;
        }
    }

    private async Task ExecuteGCodeAsync(CncCommand cmd)
    {
        if (State == "ALARM") return;

        State = "RUN";

        // Simulate processing time based on distance (simplified)
        await Task.Delay(50);

        if (cmd.Name is "G0" or "G1")
        {
            var targetX = cmd.GetParam("X") ?? X;
            var targetY = cmd.GetParam("Y") ?? Y;
            var targetZ = cmd.GetParam("Z") ?? Z;
            var targetFeed = cmd.GetParam("F") ?? Feed;

            if (!WithinSoftLimit(targetX, targetY, targetZ))
            {
                State = "ALARM";
                AlarmReason = $"Soft limit triggered at X:{targetX:F2} Y:{targetY:F2} Z:{targetZ:F2}";
                return;
            }

            const int steps = 20;
            for (int i = 0; i < steps; i++)
            {
                if (State == "ALARM") return; // E-Stop triggered

                while (State == "HOLD")
                {
                    await Task.Delay(50);
                    if (State == "ALARM") return;
                }

                await Task.Delay(25);
            }

            X = targetX;
            Y = targetY;
            Z = targetZ;
            Feed = targetFeed;
        }
        else if (cmd.Name == "G28") // Home
        {
            X = 0; Y = 0; Z = 0;
        }

        if (cmd.GetParam("S") is double s) Speed = Math.Min(s, Params["MAX_SPINDLE"]);

        if (State != "ALARM") State = "IDLE";
    }

    private bool WithinSoftLimit(double x, double y, double z)
    {
        if (!_softLimits) return true;

        bool inside = x >= 0 && x <= _xMax
                      && y >= 0 && y <= _yMax
                      && z >= 0 && z <= _zMax;
        return inside;
    }

    private Dictionary<string, List<string>> LoadMacros(dynamic config)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (config is null) return result;

        try
        {
            if (config is IDictionary<object, object> dict && dict.TryGetValue("macros", out var macroObj))
            {
                if (macroObj is IDictionary<object, object> macroDict)
                {
                    foreach (var kv in macroDict)
                    {
                        var name = kv.Key.ToString() ?? string.Empty;
                        var list = new List<string>();

                        switch (kv.Value)
                        {
                            case string s:
                                list.Add(s);
                                break;
                            case IEnumerable<object> enumerable:
                                foreach (var item in enumerable)
                                {
                                    if (item != null) list.Add(item.ToString() ?? string.Empty);
                                }
                                break;
                        }

                        if (list.Count > 0) result[name] = list;
                    }
                }
            }
        }
        catch
        {
            // If config is malformed, just return the empty macro set.
        }

        return result;
    }

    private static double GetDouble(dynamic config, double fallback, params string[] path)
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

            return Convert.ToDouble(current, CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }

    private static bool GetBool(dynamic config, bool fallback, params string[] path)
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

            return current is bool b ? b : Convert.ToBoolean(current, CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }
}

public struct MachineStatus
{
    public double X, Y, Z;
    public double Feed, Speed;
    public double Temp;
    public string State;
    public string Alarm;
}

public class Tool
{
    public int Id { get; set; }
    public double Diameter { get; set; }
    public double Length { get; set; }
    public string Description { get; set; } = "";
}
