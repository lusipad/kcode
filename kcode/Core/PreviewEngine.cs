using Spectre.Console;
using Spectre.Console.Rendering;

namespace Kcode.Core;

public class PreviewEngine
{
    public static IRenderable BuildPreview(string gcodeContent)
    {
        var lines = gcodeContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        double minX = 0, minY = 0, maxX = 0, maxY = 0;
        double curX = 0, curY = 0;
        
        var points = new List<(double x, double y)>();
        points.Add((0,0));

        foreach (var line in lines)
        {
            var cmd = CommandParser.Parse(line);
            if (cmd.Type == CommandType.GCode && (cmd.Name == "G0" || cmd.Name == "G1"))
            {
                if (cmd.GetParam("X") is double x) curX = x;
                if (cmd.GetParam("Y") is double y) curY = y;

                if (curX < minX) minX = curX;
                if (curX > maxX) maxX = curX;
                if (curY < minY) minY = curY;
                if (curY > maxY) maxY = curY;

                points.Add((curX, curY));
            }
        }

        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow(new Markup($"[bold]Bounding Box[/]"));
        grid.AddRow($"X: {minX:F2} to {maxX:F2} (W: {maxX-minX:F2})");
        grid.AddRow($"Y: {minY:F2} to {maxY:F2} (H: {maxY-minY:F2})");

        // Draw Canvas
        // Scale to fit 60x30 canvas
        int canvasW = 60;
        int canvasH = 30;
        var canvas = new Canvas(canvasW, canvasH);
        
        double width = maxX - minX;
        double height = maxY - minY;
        
        if (width == 0) width = 1;
        if (height == 0) height = 1;

        double scaleX = (canvasW - 1) / width;
        double scaleY = (canvasH - 1) / height;
        double scale = Math.Min(scaleX, scaleY);

        foreach (var p in points)
        {
            int px = (int)((p.x - minX) * scale);
            int py = (int)((p.y - minY) * scale); // Flip Y? Canvas usually 0,0 at top-left. CNC is bottom-left.
            
            // Invert Y for display
            py = (canvasH - 1) - py;

            if (px >= 0 && px < canvasW && py >= 0 && py < canvasH)
            {
                canvas.SetPixel(px, py, Color.Green);
            }
        }

        return new Rows(
            new Panel(grid).Header("Preview").BorderColor(Color.Blue),
            canvas
        );
    }

    public static void ShowPreview(string gcodeContent)
    {
        AnsiConsole.Write(BuildPreview(gcodeContent));
    }
}
