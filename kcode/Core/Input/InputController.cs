using System;
using System.Linq;
using System.Text;
using Kcode.Core.Commands;

namespace Kcode.Core.Input;

/// <summary>
/// 负责处理 REPL 的输入缓冲、历史导航和 Slash 命令建议。
/// </summary>
public class InputController
{
    private readonly CommandHistory _history;
    private readonly CommandCompleter _completer;
    private readonly List<SlashCommandEntry> _slashEntries;
    private readonly List<SlashCommandEntry> _activeSlashSuggestions = new();
    private readonly StringBuilder _buffer = new();
    private readonly int _slashDisplayCount;

    private string _currentSuggestion = string.Empty;
    private int _slashSelectionIndex = -1;
    private int _slashSuggestionWindowStart = 0;

    public InputController(
        IEnumerable<CommandDescriptor> descriptors,
        CommandHistory history,
        CommandCompleter completer,
        int slashDisplayCount = 8)
    {
        _history = history;
        _completer = completer;
        _slashDisplayCount = slashDisplayCount;

        _slashEntries = descriptors
            .Select(d => new SlashCommandEntry(
                d.Name,
                string.IsNullOrWhiteSpace(d.Description)
                    ? GetDefaultDescription(d.Type)
                    : d.Description,
                d.Type))
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        UpdateSlashSuggestions();
    }

    public string InputText => _buffer.ToString();
    public string CurrentSuggestion => _currentSuggestion;

    public InputHandleResult HandleKey(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (TryInsertSlashSelection())
                {
                    return InputHandleResult.Refresh();
                }

                var commandText = _buffer.ToString().Trim();
                ResetInputState();

                if (string.IsNullOrWhiteSpace(commandText))
                {
                    return InputHandleResult.Refresh();
                }

                return InputHandleResult.Command(commandText);

            case ConsoleKey.UpArrow:
                if (MoveSlashSelection(-1))
                {
                    return InputHandleResult.Refresh(breakKeyReading: true);
                }

                var previous = _history.NavigateUp();
                if (previous != null)
                {
                    SetInput(previous, resetHistoryNavigation: false, updateSuggestion: false);
                    return InputHandleResult.Refresh();
                }
                break;

            case ConsoleKey.DownArrow:
                if (MoveSlashSelection(1))
                {
                    return InputHandleResult.Refresh(breakKeyReading: true);
                }

                var next = _history.NavigateDown();
                if (next != null)
                {
                    SetInput(next, resetHistoryNavigation: false, updateSuggestion: false);
                    return InputHandleResult.Refresh();
                }
                break;

            case ConsoleKey.Tab:
                if (TryApplySlashSuggestion())
                {
                    return InputHandleResult.Refresh();
                }

                if (!string.IsNullOrEmpty(_currentSuggestion))
                {
                    SetInput(_currentSuggestion, resetHistoryNavigation: true);
                    return InputHandleResult.Refresh();
                }
                break;

            case ConsoleKey.Backspace:
                if (_buffer.Length > 0)
                {
                    _buffer.Length--;
                    _history.ResetNavigation();
                    _completer.Reset();
                    UpdateSuggestionFromCompleter();
                    UpdateSlashSuggestions();
                    return InputHandleResult.Refresh();
                }
                break;

            default:
                if (key.KeyChar != '\0')
                {
                    _buffer.Append(key.KeyChar);
                    _history.ResetNavigation();
                    _completer.Reset();
                    UpdateSuggestionFromCompleter();
                    UpdateSlashSuggestions();
                    return InputHandleResult.Refresh();
                }
                break;
        }

        return InputHandleResult.None;
    }

    public SlashViewState GetSlashViewState()
    {
        var items = GetVisibleSlashEntriesInternal();
        var window = GetSlashWindowInfo();
        return new SlashViewState(items, window);
    }

    private IReadOnlyList<(SlashCommandEntry Entry, bool IsSelected)> GetVisibleSlashEntriesInternal()
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return Array.Empty<(SlashCommandEntry, bool)>();
        }

        var windowSize = Math.Min(_slashDisplayCount, _activeSlashSuggestions.Count);
        var start = _slashSuggestionWindowStart;
        if (start + windowSize > _activeSlashSuggestions.Count)
        {
            start = _activeSlashSuggestions.Count - windowSize;
        }

        var result = new List<(SlashCommandEntry, bool)>(windowSize);
        for (int i = 0; i < windowSize; i++)
        {
            var absoluteIndex = start + i;
            var entry = _activeSlashSuggestions[absoluteIndex];
            var isSelected = absoluteIndex == _slashSelectionIndex;
            result.Add((entry, isSelected));
        }

        return result;
    }

    private (int Start, int End, int Total)? GetSlashWindowInfo()
    {
        if (_activeSlashSuggestions.Count <= _slashDisplayCount || _activeSlashSuggestions.Count == 0)
        {
            return null;
        }

        var visibleCount = Math.Min(_slashDisplayCount, _activeSlashSuggestions.Count);
        var start = _slashSuggestionWindowStart + 1;
        var end = _slashSuggestionWindowStart + visibleCount;
        return (start, end, _activeSlashSuggestions.Count);
    }

    private void ResetInputState()
    {
        _buffer.Clear();
        _currentSuggestion = string.Empty;
        _history.ResetNavigation();
        _completer.Reset();
        UpdateSlashSuggestions();
    }

    private void SetInput(string? value, bool resetHistoryNavigation, bool updateSuggestion = true)
    {
        _buffer.Clear();
        if (!string.IsNullOrEmpty(value))
        {
            _buffer.Append(value);
        }

        if (resetHistoryNavigation)
        {
            _history.ResetNavigation();
        }

        _completer.Reset();
        if (updateSuggestion)
        {
            UpdateSuggestionFromCompleter();
        }
        else
        {
            _currentSuggestion = string.Empty;
        }
        UpdateSlashSuggestions();
    }

    private void UpdateSuggestionFromCompleter()
    {
        var text = _buffer.ToString();
        _currentSuggestion = string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : _completer.GetBestSuggestion(text) ?? string.Empty;
    }

    private bool MoveSlashSelection(int delta)
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return false;
        }

        if (_slashSelectionIndex < 0)
        {
            _slashSelectionIndex = delta >= 0 ? 0 : _activeSlashSuggestions.Count - 1;
            EnsureSlashSelectionVisible();
            return true;
        }

        var nextIndex = _slashSelectionIndex + delta;
        nextIndex = Math.Clamp(nextIndex, 0, _activeSlashSuggestions.Count - 1);
        if (nextIndex == _slashSelectionIndex)
        {
            return false;
        }

        _slashSelectionIndex = nextIndex;
        EnsureSlashSelectionVisible();
        return true;
    }

    private bool TryApplySlashSuggestion()
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return false;
        }

        var index = _slashSelectionIndex >= 0 ? _slashSelectionIndex : 0;
        var candidate = _activeSlashSuggestions[index].Name;
        _buffer.Clear();
        _buffer.Append(candidate);
        _buffer.Append(' ');
        _slashSelectionIndex = -1;
        UpdateSlashSuggestions();
        UpdateSuggestionFromCompleter();
        return true;
    }

    private bool TryInsertSlashSelection()
    {
        if (_activeSlashSuggestions.Count == 0)
        {
            return false;
        }

        var trimmed = _buffer.ToString().Trim();
        var normalizedInput = string.IsNullOrWhiteSpace(trimmed)
            ? "/"
            : CommandNameHelper.Normalize(trimmed);

        var index = _slashSelectionIndex >= 0 ? _slashSelectionIndex : 0;
        var candidate = _activeSlashSuggestions[index].Name;

        if (!candidate.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
        {
            _buffer.Clear();
            _buffer.Append(candidate);
            _buffer.Append(' ');
            UpdateSlashSuggestions();
            UpdateSuggestionFromCompleter();
            return true;
        }

        return false;
    }

    private void UpdateSlashSuggestions()
    {
        var rawInput = _buffer.ToString();
        if (string.IsNullOrWhiteSpace(rawInput) ||
            !rawInput.TrimStart().StartsWith("/", StringComparison.Ordinal))
        {
            _activeSlashSuggestions.Clear();
            _slashSelectionIndex = -1;
            _slashSuggestionWindowStart = 0;
            return;
        }

        var normalized = CommandNameHelper.Normalize(rawInput.Trim());
        var matches = _slashEntries
            .Where(entry => entry.Name.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _activeSlashSuggestions.Clear();
        _activeSlashSuggestions.AddRange(matches);

        if (_activeSlashSuggestions.Count == 0)
        {
            _slashSelectionIndex = -1;
        }
        else if (_slashSelectionIndex < 0 || _slashSelectionIndex >= _activeSlashSuggestions.Count)
        {
            _slashSelectionIndex = 0;
        }

        EnsureSlashSelectionVisible();
    }

    private void EnsureSlashSelectionVisible()
    {
        if (_activeSlashSuggestions.Count <= _slashDisplayCount)
        {
            _slashSuggestionWindowStart = 0;
            return;
        }

        if (_slashSelectionIndex < 0)
        {
            _slashSuggestionWindowStart = 0;
            return;
        }

        if (_slashSelectionIndex < _slashSuggestionWindowStart)
        {
            _slashSuggestionWindowStart = _slashSelectionIndex;
        }
        else if (_slashSelectionIndex >= _slashSuggestionWindowStart + _slashDisplayCount)
        {
            _slashSuggestionWindowStart = _slashSelectionIndex - _slashDisplayCount + 1;
        }

        var maxStart = _activeSlashSuggestions.Count - _slashDisplayCount;
        _slashSuggestionWindowStart = Math.Clamp(_slashSuggestionWindowStart, 0, Math.Max(0, maxStart));
    }

    private static string GetDefaultDescription(CommandType type) =>
        type switch
        {
            CommandType.System => "系统命令",
            CommandType.Api => "API 命令",
            CommandType.Macro => "宏命令",
            _ => "命令"
        };
}
