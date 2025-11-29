using Spectre.Console.Rendering;

namespace Kcode.Core.UI;

public interface IReplViewSession : IAsyncDisposable
{
    Task StartAsync();
    void Update(ReplViewState state);
    Task RequestRefreshAsync();
    void DisplayFullScreen(IRenderable? renderable, string? fallbackOutput);
}
