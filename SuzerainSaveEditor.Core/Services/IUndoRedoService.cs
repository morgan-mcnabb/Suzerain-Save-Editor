namespace SuzerainSaveEditor.Core.Services;

// manages undo/redo stacks for field-level edits
public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }

    // push a new edit onto the undo stack.
    // consecutive edits to the same field are coalesced into one entry.
    // clears the redo stack.
    void Push(string fieldId, string? oldValue, string newValue);

    // pop the top undo entry, move it to redo stack, and return it.
    // returns null if the undo stack is empty.
    UndoEntry? Undo();

    // pop the top redo entry, move it to undo stack, and return it.
    // returns null if the redo stack is empty.
    UndoEntry? Redo();

    // peek at the top entry without modifying stacks
    UndoEntry? PeekUndo();
    UndoEntry? PeekRedo();

    // clear both stacks (called on file load, save, and revert all)
    void Clear();

    // fired when CanUndo or CanRedo changes
    event Action? StateChanged;
}
