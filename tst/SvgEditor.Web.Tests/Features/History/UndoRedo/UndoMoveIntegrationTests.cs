using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Features.History.PushHistory;
using SvgEditor.Web.Features.History.UndoRedo;

namespace SvgEditor.Web.Tests.Features.History.UndoRedo;

[TestClass]
public sealed class UndoMoveIntegrationTests
{
    private static (EditorState State, PushHistoryHandler Push, UpdateElementHandler Update, UndoHandler Undo, RedoHandler Redo)
        CreateHandlers(params SvgElement[] elements)
    {
        var state = new EditorState { Document = new SvgDocument { Elements = [.. elements] } };
        var stack = new HistoryStack();
        return (
            state,
            new PushHistoryHandler(state, stack),
            new UpdateElementHandler(state),
            new UndoHandler(state, stack),
            new RedoHandler(state, stack)
        );
    }

    [TestMethod]
    public async Task Undo_RestoresRectPositionAfterMove()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var (state, push, update, undo, _) = CreateHandlers(rect);

        await push.Handle(new PushHistoryCommand("Move element"));
        await update.Handle(new UpdateElementCommand("r1", 50, 30));

        var moved = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(60, moved.X);
        Assert.AreEqual(50, moved.Y);

        await undo.Handle(new UndoCommand());

        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, restored.X);
        Assert.AreEqual(20, restored.Y);
    }

    [TestMethod]
    public async Task Undo_RestoresCirclePositionAfterMove()
    {
        var circle = new SvgCircle { Id = "c1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "60", ["r"] = "25" } };
        var (state, push, update, undo, _) = CreateHandlers(circle);

        await push.Handle(new PushHistoryCommand("Move element"));
        await update.Handle(new UpdateElementCommand("c1", 10, -15));

        await undo.Handle(new UndoCommand());

        var restored = (SvgCircle)state.Document!.FindById("c1")!;
        Assert.AreEqual(50, restored.Cx);
        Assert.AreEqual(60, restored.Cy);
    }

    [TestMethod]
    public async Task Undo_RestoresPositionAfterMultipleIncrementalMoves()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var (state, push, update, undo, _) = CreateHandlers(rect);

        await push.Handle(new PushHistoryCommand("Move element"));

        await update.Handle(new UpdateElementCommand("r1", 5, 5));
        await update.Handle(new UpdateElementCommand("r1", 5, 5));
        await update.Handle(new UpdateElementCommand("r1", 5, 5));

        var moved = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(25, moved.X);
        Assert.AreEqual(35, moved.Y);

        await undo.Handle(new UndoCommand());

        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, restored.X);
        Assert.AreEqual(20, restored.Y);
    }

    [TestMethod]
    public async Task Redo_RestoresPositionAfterUndo()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var (state, push, update, undo, redo) = CreateHandlers(rect);

        await push.Handle(new PushHistoryCommand("Move element"));
        await update.Handle(new UpdateElementCommand("r1", 50, 30));

        await undo.Handle(new UndoCommand());
        await redo.Handle(new RedoCommand());

        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(60, restored.X);
        Assert.AreEqual(50, restored.Y);
    }

    [TestMethod]
    public async Task UndoRedo_SnapshotsAreIndependentOfLaterMutations()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var (state, push, update, undo, _) = CreateHandlers(rect);

        await push.Handle(new PushHistoryCommand("Move element"));
        await update.Handle(new UpdateElementCommand("r1", 50, 30));

        await undo.Handle(new UndoCommand());

        Assert.AreEqual(10, ((SvgRect)state.Document!.FindById("r1")!).X);

        await push.Handle(new PushHistoryCommand("Move again"));
        await update.Handle(new UpdateElementCommand("r1", 100, 100));

        await undo.Handle(new UndoCommand());

        var restored = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, restored.X);
        Assert.AreEqual(20, restored.Y);
    }
}
