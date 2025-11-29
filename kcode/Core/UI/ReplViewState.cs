using Spectre.Console.Rendering;
using Kcode.Core.Input;

namespace Kcode.Core.UI;

public record ReplViewState(
    IReadOnlyList<IRenderable> History,
    string InputText,
    string Suggestion,
    SlashViewState SlashState);
