using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.History.Models;

namespace SvgEditor.Web.Tests.Features.History.Models;

[TestClass]
public sealed class HistoryStackTests
{
    private static SvgDocument MakeDoc(string viewBox) => new SvgDocument { ViewBox = viewBox };

    [TestMethod]
    public void Push_CanUndo_True()
    {
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("test", MakeDoc("0 0 100 100")));

        Assert.IsTrue(stack.CanUndo);
    }

    [TestMethod]
    public void Undo_RestoresSnapshot()
    {
        var stack = new HistoryStack();
        var original = MakeDoc("0 0 200 200");
        stack.Push(new HistoryEntry("initial", original));

        var restored = stack.Undo(MakeDoc("0 0 400 400"));

        Assert.IsNotNull(restored);
        Assert.AreEqual("0 0 200 200", restored.ViewBox);
    }

    [TestMethod]
    public void Undo_EnablesRedo()
    {
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("step1", MakeDoc("A")));
        stack.Undo(MakeDoc("B"));

        Assert.IsTrue(stack.CanRedo);
    }

    [TestMethod]
    public void Redo_RestoresRedoneState()
    {
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("step1", MakeDoc("A")));
        var current = MakeDoc("B");
        stack.Undo(current);

        var redone = stack.Redo(MakeDoc("A"));

        Assert.IsNotNull(redone);
        Assert.AreEqual("B", redone.ViewBox);
    }

    [TestMethod]
    public void Push_ClearsRedoStack()
    {
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("step1", MakeDoc("A")));
        stack.Undo(MakeDoc("B"));
        Assert.IsTrue(stack.CanRedo);

        stack.Push(new HistoryEntry("step2", MakeDoc("C")));

        Assert.IsFalse(stack.CanRedo);
    }

    [TestMethod]
    public void Undo_EmptyStack_ReturnsNull()
    {
        var stack = new HistoryStack();

        var result = stack.Undo(MakeDoc("X"));

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Redo_EmptyStack_ReturnsNull()
    {
        var stack = new HistoryStack();

        var result = stack.Redo(MakeDoc("X"));

        Assert.IsNull(result);
    }
}
