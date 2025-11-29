using System;

namespace Kcode.Core.Terminal;

public interface ITerminal
{
    bool IsOutputRedirected { get; }
    bool KeyAvailable { get; }
    ConsoleKeyInfo ReadKey(bool intercept);
    void SetCursorVisible(bool visible);
    void SetCursorPosition(int left, int top);
}
