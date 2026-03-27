using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Features.History.UndoRedo;

namespace SvgEditor.Web.Tests.Features.History.UndoRedo;

[TestClass]
public sealed class UndoRedoHandlerTests
{
    private static EditorState CreateState(string viewBox) => new EditorState
    {
        Document = new SvgDocument { ViewBox = viewBox }
    };

    [TestMethod]
    public async Task Undo_RestoresDocument()
    {
        var state = CreateState("current");
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("before", new SvgDocument { ViewBox = "original" }));
        var handler = new UndoHandler(state, stack);

        await handler.Handle(new UndoCommand());

        Assert.AreEqual("original", state.Document!.ViewBox);
    }

    [TestMethod]
    public async Task Undo_EmptyStack_IsNoOp()
    {
        var state = CreateState("current");
        var stack = new HistoryStack();
        var handler = new UndoHandler(state, stack);

        await handler.Handle(new UndoCommand());

        Assert.AreEqual("current", state.Document!.ViewBox);
    }

    [TestMethod]
    public async Task Redo_RestoresDocument()
    {
        var state = CreateState("current");
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("before", new SvgDocument { ViewBox = "original" }));

        // Undo to push to redo stack
        var undoDoc = stack.Undo(state.Document!);
        state.Document = undoDoc!;

        var handler = new RedoHandler(state, stack);
        await handler.Handle(new RedoCommand());

        Assert.AreEqual("current", state.Document!.ViewBox);
    }

    [TestMethod]
    public async Task Undo_NotifiesStateChanged()
    {
        var state = CreateState("current");
        var stack = new HistoryStack();
        stack.Push(new HistoryEntry("before", new SvgDocument { ViewBox = "original" }));
        var handler = new UndoHandler(state, stack);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new UndoCommand());

        Assert.IsTrue(notified);
    }

    [TestMethod]
    public async Task Redo_EmptyStack_IsNoOp()
    {
        var state = CreateState("current");
        var stack = new HistoryStack();
        var handler = new RedoHandler(state, stack);

        await handler.Handle(new RedoCommand());

        Assert.AreEqual("current", state.Document!.ViewBox);
    }
}
