using SvgEditor.Web.Features.Canvas.Models;

namespace SvgEditor.Web.Features.History.Models;

public sealed class HistoryStack
{
    private readonly Stack<HistoryEntry> _undoStack = new();
    private readonly Stack<HistoryEntry> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Push(HistoryEntry entry)
    {
        _undoStack.Push(entry);
        _redoStack.Clear();
    }

    public SvgDocument? Undo(SvgDocument current)
    {
        if (!CanUndo) return null;
        var entry = _undoStack.Pop();
        _redoStack.Push(new HistoryEntry(entry.Description, current.DeepClone()));
        return entry.Snapshot;
    }

    public SvgDocument? Redo(SvgDocument current)
    {
        if (!CanRedo) return null;
        var entry = _redoStack.Pop();
        _undoStack.Push(new HistoryEntry(entry.Description, current.DeepClone()));
        return entry.Snapshot;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
