using SuzerainSaveEditor.Core.Services;

namespace SuzerainSaveEditor.Tests.Services;

public sealed class UndoRedoServiceTests
{
    private readonly UndoRedoService _sut = new();

    // initial state

    [Fact]
    public void InitialState_CannotUndoOrRedo()
    {
        Assert.False(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Undo_EmptyStack_ReturnsNull()
    {
        Assert.Null(_sut.Undo());
    }

    [Fact]
    public void Redo_EmptyStack_ReturnsNull()
    {
        Assert.Null(_sut.Redo());
    }

    // push + undo basics

    [Fact]
    public void Push_SingleEntry_CanUndo()
    {
        _sut.Push("field1", "old", "new");

        Assert.True(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Undo_SingleEntry_ReturnsEntry()
    {
        _sut.Push("field1", "old", "new");

        var entry = _sut.Undo();

        Assert.NotNull(entry);
        Assert.Equal("field1", entry.FieldId);
        Assert.Equal("old", entry.OldValue);
        Assert.Equal("new", entry.NewValue);
    }

    [Fact]
    public void Undo_SingleEntry_MovesToRedoStack()
    {
        _sut.Push("field1", "old", "new");

        _sut.Undo();

        Assert.False(_sut.CanUndo);
        Assert.True(_sut.CanRedo);
    }

    [Fact]
    public void Undo_TwoEntries_ReturnsInLifoOrder()
    {
        _sut.Push("field1", "a", "b");
        _sut.Push("field2", "c", "d");

        var second = _sut.Undo();
        var first = _sut.Undo();

        Assert.Equal("field2", second!.FieldId);
        Assert.Equal("field1", first!.FieldId);
    }

    // redo basics

    [Fact]
    public void Redo_AfterUndo_ReturnsEntry()
    {
        _sut.Push("field1", "old", "new");
        _sut.Undo();

        var entry = _sut.Redo();

        Assert.NotNull(entry);
        Assert.Equal("field1", entry.FieldId);
        Assert.Equal("old", entry.OldValue);
        Assert.Equal("new", entry.NewValue);
    }

    [Fact]
    public void Redo_AfterUndo_MovesBackToUndoStack()
    {
        _sut.Push("field1", "old", "new");
        _sut.Undo();

        _sut.Redo();

        Assert.True(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Redo_MultipleUndos_RedoesInOrder()
    {
        _sut.Push("field1", "a", "b");
        _sut.Push("field2", "c", "d");
        _sut.Undo();
        _sut.Undo();

        var first = _sut.Redo();
        var second = _sut.Redo();

        Assert.Equal("field1", first!.FieldId);
        Assert.Equal("field2", second!.FieldId);
    }

    // push clears redo stack

    [Fact]
    public void Push_AfterUndo_ClearsRedoStack()
    {
        _sut.Push("field1", "a", "b");
        _sut.Undo();
        Assert.True(_sut.CanRedo);

        _sut.Push("field2", "c", "d");

        Assert.False(_sut.CanRedo);
    }

    // coalescing

    [Fact]
    public void Push_ConsecutiveSameField_Coalesces()
    {
        _sut.Push("field1", "original", "1");
        _sut.Push("field1", "1", "10");
        _sut.Push("field1", "10", "100");

        var entry = _sut.Undo();

        Assert.NotNull(entry);
        Assert.Equal("field1", entry.FieldId);
        Assert.Equal("original", entry.OldValue);
        Assert.Equal("100", entry.NewValue);

        // only one entry was on the stack
        Assert.False(_sut.CanUndo);
    }

    [Fact]
    public void Push_DifferentFieldBetween_StopsCoalescing()
    {
        _sut.Push("field1", "a", "b");
        _sut.Push("field2", "c", "d");
        _sut.Push("field1", "b", "e");

        // three separate entries
        var third = _sut.Undo();
        var second = _sut.Undo();
        var first = _sut.Undo();

        Assert.Equal("field1", third!.FieldId);
        Assert.Equal("b", third.OldValue);
        Assert.Equal("e", third.NewValue);

        Assert.Equal("field2", second!.FieldId);
        Assert.Equal("field1", first!.FieldId);
        Assert.Equal("a", first.OldValue);
        Assert.Equal("b", first.NewValue);
    }

    [Fact]
    public void Push_CoalesceBackToOriginal_DropsEntry()
    {
        _sut.Push("field1", "original", "changed");
        _sut.Push("field1", "changed", "original");

        // user typed something then typed back — no undo entry should remain
        Assert.False(_sut.CanUndo);
    }

    [Fact]
    public void Push_CoalesceBackToOriginal_ClearsRedoStack()
    {
        _sut.Push("field1", "a", "b");
        _sut.Undo();
        Assert.True(_sut.CanRedo);

        // coalesce back to original on a new field
        _sut.Push("field2", "x", "y");
        _sut.Push("field2", "y", "x");

        // redo was cleared by first push of field2
        Assert.False(_sut.CanRedo);
    }

    // no-op detection

    [Fact]
    public void Push_SameOldAndNewValue_SkippedEntirely()
    {
        _sut.Push("field1", "same", "same");

        Assert.False(_sut.CanUndo);
    }

    [Fact]
    public void Push_NullOldAndNewValue_NotSkipped()
    {
        // null old value (field didn't exist) to a real value is a valid edit
        _sut.Push("field1", null, "new");

        Assert.True(_sut.CanUndo);
        var entry = _sut.Undo();
        Assert.Null(entry!.OldValue);
        Assert.Equal("new", entry.NewValue);
    }

    // clear

    [Fact]
    public void Clear_EmptiesBothStacks()
    {
        _sut.Push("field1", "a", "b");
        _sut.Push("field2", "c", "d");
        _sut.Undo();

        _sut.Clear();

        Assert.False(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Clear_EmptyStacks_DoesNotFireEvent()
    {
        var firedCount = 0;
        _sut.StateChanged += () => firedCount++;

        _sut.Clear();

        Assert.Equal(0, firedCount);
    }

    // StateChanged event

    [Fact]
    public void Push_FiresStateChanged()
    {
        var fired = false;
        _sut.StateChanged += () => fired = true;

        _sut.Push("field1", "old", "new");

        Assert.True(fired);
    }

    [Fact]
    public void Undo_FiresStateChanged()
    {
        _sut.Push("field1", "old", "new");

        var fired = false;
        _sut.StateChanged += () => fired = true;

        _sut.Undo();

        Assert.True(fired);
    }

    [Fact]
    public void Redo_FiresStateChanged()
    {
        _sut.Push("field1", "old", "new");
        _sut.Undo();

        var fired = false;
        _sut.StateChanged += () => fired = true;

        _sut.Redo();

        Assert.True(fired);
    }

    [Fact]
    public void Clear_WithEntries_FiresStateChanged()
    {
        _sut.Push("field1", "old", "new");

        var fired = false;
        _sut.StateChanged += () => fired = true;

        _sut.Clear();

        Assert.True(fired);
    }

    [Fact]
    public void Push_Coalescing_FiresStateChanged()
    {
        _sut.Push("field1", "old", "v1");

        var fired = false;
        _sut.StateChanged += () => fired = true;

        _sut.Push("field1", "v1", "v2");

        Assert.True(fired);
    }

    // undo/redo cycle round-trip

    [Fact]
    public void UndoRedo_FullCycle_RestoresState()
    {
        _sut.Push("field1", "a", "b");
        _sut.Push("field2", "c", "d");

        // undo both
        var u2 = _sut.Undo();
        var u1 = _sut.Undo();
        Assert.Equal("d", u2!.NewValue);
        Assert.Equal("b", u1!.NewValue);

        // redo both
        var r1 = _sut.Redo();
        var r2 = _sut.Redo();
        Assert.Equal("b", r1!.NewValue);
        Assert.Equal("d", r2!.NewValue);

        Assert.True(_sut.CanUndo);
        Assert.False(_sut.CanRedo);
    }

    [Fact]
    public void Undo_ThenPush_ClearsRedoAndAddsNew()
    {
        _sut.Push("field1", "a", "b");
        _sut.Push("field2", "c", "d");

        _sut.Undo();
        _sut.Push("field3", "e", "f");

        // redo stack was cleared, only field1 and field3 on undo
        Assert.False(_sut.CanRedo);

        var top = _sut.Undo();
        Assert.Equal("field3", top!.FieldId);

        var bottom = _sut.Undo();
        Assert.Equal("field1", bottom!.FieldId);

        Assert.False(_sut.CanUndo);
    }

    // edge case: undo empty after clear

    [Fact]
    public void Undo_AfterClear_ReturnsNull()
    {
        _sut.Push("field1", "a", "b");
        _sut.Clear();

        Assert.Null(_sut.Undo());
    }

    [Fact]
    public void Redo_AfterClear_ReturnsNull()
    {
        _sut.Push("field1", "a", "b");
        _sut.Undo();
        _sut.Clear();

        Assert.Null(_sut.Redo());
    }

    // bool field coalescing (rapid toggles)

    [Fact]
    public void Push_BoolToggle_CoalescesRapidToggles()
    {
        _sut.Push("boolField", "True", "False");
        _sut.Push("boolField", "False", "True");

        // toggled back to original — entry dropped
        Assert.False(_sut.CanUndo);
    }

    [Fact]
    public void Push_BoolToggle_SingleFlip_UndoRestores()
    {
        _sut.Push("boolField", "True", "False");

        var entry = _sut.Undo();
        Assert.Equal("True", entry!.OldValue);
        Assert.Equal("False", entry.NewValue);
    }

    // multiple sequential operations

    [Fact]
    public void InterleavedUndoRedo_MaintainsCorrectState()
    {
        _sut.Push("f1", "a", "b");
        _sut.Push("f2", "c", "d");
        _sut.Push("f3", "e", "f");

        // undo f3
        var u1 = _sut.Undo();
        Assert.Equal("f3", u1!.FieldId);

        // redo f3
        var r1 = _sut.Redo();
        Assert.Equal("f3", r1!.FieldId);

        // undo f3 and f2
        _sut.Undo();
        var u3 = _sut.Undo();
        Assert.Equal("f2", u3!.FieldId);

        // push new — clears redo (f2, f3 gone from redo)
        _sut.Push("f4", "g", "h");
        Assert.False(_sut.CanRedo);

        // undo stack: f1, f4
        var top = _sut.Undo();
        Assert.Equal("f4", top!.FieldId);
        top = _sut.Undo();
        Assert.Equal("f1", top!.FieldId);
        Assert.False(_sut.CanUndo);
    }
}
