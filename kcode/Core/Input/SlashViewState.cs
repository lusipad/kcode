namespace Kcode.Core.Input;

public record SlashViewState(
    IReadOnlyList<(SlashCommandEntry Entry, bool IsSelected)> Items,
    (int Start, int End, int Total)? WindowInfo);
