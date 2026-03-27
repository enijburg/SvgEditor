using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Features.History.PushHistory;
using SvgEditor.Web.Features.History.UndoRedo;

namespace SvgEditor.Web.Tests.Features.History.UndoRedo;

[TestClass]
public sealed class UndoMoveIntegrationTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new EditorState
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Undo_RestoresRectPositionAfterMove()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var state = CreateState(rect);
        var stack = new HistoryStack();

        // Push history snapshot before move (simulates what CanvasPage does)
        var pushHandler = new PushHistoryHandler(state, stack);
        await pushHandler.Handle(new PushHistoryCommand("Move element"));

        // Move element
        var updateHandler = new UpdateElementHandler(state);
        await updateHandler.Handle(new UpdateElementCommand("r1", 50, 30));

        // Verify element moved
        var moved = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(60, moved.X);
        Assert.AreEqual(50, moved.Y);

        // Undo
        var undoHandler = new UndoHandler(state, stack);
        await undoHandler.Handle(new UndoCommand());

        // Verify element is back at original position
        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, restored.X);
        Assert.AreEqual(20, restored.Y);
    }

    [TestMethod]
    public async Task Undo_RestoresCirclePositionAfterMove()
    {
        var circle = new SvgCircle { Id = "c1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "60", ["r"] = "25" } };
        var state = CreateState(circle);
        var stack = new HistoryStack();

        var pushHandler = new PushHistoryHandler(state, stack);
        await pushHandler.Handle(new PushHistoryCommand("Move element"));

        var updateHandler = new UpdateElementHandler(state);
        await updateHandler.Handle(new UpdateElementCommand("c1", 10, -15));

        var undoHandler = new UndoHandler(state, stack);
        await undoHandler.Handle(new UndoCommand());

        var restored = (SvgCircle)state.Document!.FindById("c1")!;
        Assert.AreEqual(50, restored.Cx);
        Assert.AreEqual(60, restored.Cy);
    }

    [TestMethod]
    public async Task Undo_RestoresPositionAfterMultipleIncrementalMoves()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var state = CreateState(rect);
        var stack = new HistoryStack();

        // Push history once before the first move (like CanvasPage does)
        var pushHandler = new PushHistoryHandler(state, stack);
        await pushHandler.Handle(new PushHistoryCommand("Move element"));

        // Simulate multiple incremental moves during a drag
        var updateHandler = new UpdateElementHandler(state);
        await updateHandler.Handle(new UpdateElementCommand("r1", 5, 5));
        await updateHandler.Handle(new UpdateElementCommand("r1", 5, 5));
        await updateHandler.Handle(new UpdateElementCommand("r1", 5, 5));

        // Verify element moved to final position
        var moved = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(25, moved.X);
        Assert.AreEqual(35, moved.Y);

        // Single undo should restore to the original position
        var undoHandler = new UndoHandler(state, stack);
        await undoHandler.Handle(new UndoCommand());

        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, restored.X);
        Assert.AreEqual(20, restored.Y);
    }

    [TestMethod]
    public async Task Redo_RestoresPositionAfterUndo()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var state = CreateState(rect);
        var stack = new HistoryStack();

        var pushHandler = new PushHistoryHandler(state, stack);
        await pushHandler.Handle(new PushHistoryCommand("Move element"));

        var updateHandler = new UpdateElementHandler(state);
        await updateHandler.Handle(new UpdateElementCommand("r1", 50, 30));

        // Undo
        var undoHandler = new UndoHandler(state, stack);
        await undoHandler.Handle(new UndoCommand());

        // Redo
        var redoHandler = new RedoHandler(state, stack);
        await redoHandler.Handle(new RedoCommand());

        // Should be at the moved position
        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(60, restored.X);
        Assert.AreEqual(50, restored.Y);
    }

    [TestMethod]
    public async Task UndoRedo_SnapshotsAreIndependentOfLaterMutations()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var state = CreateState(rect);
        var stack = new HistoryStack();

        // Push history + move
        var pushHandler = new PushHistoryHandler(state, stack);
        await pushHandler.Handle(new PushHistoryCommand("Move element"));

        var updateHandler = new UpdateElementHandler(state);
        await updateHandler.Handle(new UpdateElementCommand("r1", 50, 30));

        // Undo restores to original
        var undoHandler = new UndoHandler(state, stack);
        await undoHandler.Handle(new UndoCommand());

        Assert.AreEqual(10, ((SvgRect)state.Document!.FindById("r1")!).X);

        // Push a new history + move again (this clears redo)
        await pushHandler.Handle(new PushHistoryCommand("Move again"));
        await updateHandler.Handle(new UpdateElementCommand("r1", 100, 100));

        // Undo should go back to the state right after the first undo (x=10)
        await undoHandler.Handle(new UndoCommand());

        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, restored.X);
        Assert.AreEqual(20, restored.Y);
    }
}
