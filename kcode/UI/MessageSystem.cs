using Spectre.Console;
using Spectre.Console.Rendering;

namespace Kcode.UI;

public static class MessageSystem
{
    public static IRenderable RenderInfo(string message) =>
        BuildBubble("i", "INFO", Color.DeepSkyBlue1, message);

    public static IRenderable RenderSuccess(string message) =>
        BuildBubble("ok", "OK", Color.SpringGreen2, message);

    public static IRenderable RenderWarning(string message) =>
        BuildBubble("!", "WARN", Color.Gold1, message);

    public static IRenderable RenderError(string message) =>
        BuildBubble("x", "ERR", Color.OrangeRed1, message);

    public static IRenderable RenderAlarm(string message) =>
        BuildBubble("!!", "ALARM", Color.Red1, message, Color.White);

    public static void ShowInfo(string message)
    {
        AnsiConsole.Write(RenderInfo(message));
        AnsiConsole.WriteLine();
    }

    public static void ShowSuccess(string message)
    {
        AnsiConsole.Write(RenderSuccess(message));
        AnsiConsole.WriteLine();
    }

    public static void ShowWarning(string message)
    {
        AnsiConsole.Write(RenderWarning(message));
        AnsiConsole.WriteLine();
    }

    public static void ShowError(string message)
    {
        AnsiConsole.Write(RenderError(message));
        AnsiConsole.WriteLine();
    }

    public static void ShowAlarm(string message)
    {
        AnsiConsole.Write(RenderAlarm(message));
    }

    private static IRenderable BuildBubble(string icon, string label, Color borderColor, string message, Color? headerColor = null)
    {
        var headerMarkup = ThemeHelper.ToMarkup(headerColor ?? borderColor);
        var textMarkup = ThemeHelper.ToMarkup(Color.Grey89);
        var content = new Rows(
            new Markup($"[{headerMarkup}] {Markup.Escape(icon)}  [bold]{Markup.Escape(label)}[/][/]"),
            new Markup($"[{textMarkup}]{Markup.Escape(message)}[/]")
        );

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(borderColor)
            .Padding(1, 0, 1, 0);
    }
}
