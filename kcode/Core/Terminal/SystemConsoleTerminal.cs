using System;

namespace Kcode.Core.Terminal;

public class SystemConsoleTerminal : ITerminal
{
    public bool IsOutputRedirected => Console.IsOutputRedirected;
    public bool KeyAvailable => Console.KeyAvailable;

    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public void SetCursorVisible(bool visible) => Console.CursorVisible = visible;

    public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);
}
