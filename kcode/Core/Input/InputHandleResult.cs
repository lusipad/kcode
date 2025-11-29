namespace Kcode.Core.Input;

public readonly record struct InputHandleResult(
    bool NeedsRefresh,
    bool ShouldBreakKeyReading,
    bool CommandSubmitted,
    string? CommandText)
{
    public static InputHandleResult None => new(false, false, false, null);

    public static InputHandleResult Refresh(bool breakKeyReading = false) =>
        new(true, breakKeyReading, false, null);

    public static InputHandleResult Command(string command) =>
        new(true, false, true, command);
}
