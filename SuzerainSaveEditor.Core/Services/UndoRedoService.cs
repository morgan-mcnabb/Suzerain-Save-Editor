namespace SuzerainSaveEditor.Core.Services;

public sealed class UndoRedoService : IUndoRedoService
{
    private readonly Stack<UndoEntry> _undoStack = new();
    private readonly Stack<UndoEntry> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public event Action? StateChanged;

    public void Push(string fieldId, string? oldValue, string newValue)
    {
        if (_undoStack.TryPeek(out var top) && top.FieldId == fieldId)
        {
            _undoStack.Pop();

            if (string.Equals(top.OldValue, newValue, StringComparison.Ordinal))
            {
                _redoStack.Clear();
                StateChanged?.Invoke();
                return;
            }

            _undoStack.Push(top with { NewValue = newValue });
        }
        else
        {
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                return;

            _undoStack.Push(new UndoEntry(fieldId, oldValue, newValue));
        }

        _redoStack.Clear();
        StateChanged?.Invoke();
    }

    public UndoEntry? Undo()
    {
        if (_undoStack.Count == 0) return null;

        var entry = _undoStack.Pop();
        _redoStack.Push(entry);
        StateChanged?.Invoke();
        return entry;
    }

    public UndoEntry? Redo()
    {
        if (_redoStack.Count == 0) return null;

        var entry = _redoStack.Pop();
        _undoStack.Push(entry);
        StateChanged?.Invoke();
        return entry;
    }

    public UndoEntry? PeekUndo() => _undoStack.TryPeek(out var e) ? e : null;
    public UndoEntry? PeekRedo() => _redoStack.TryPeek(out var e) ? e : null;

    public void Clear()
    {
        if (_undoStack.Count == 0 && _redoStack.Count == 0) return;

        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
    }
}
