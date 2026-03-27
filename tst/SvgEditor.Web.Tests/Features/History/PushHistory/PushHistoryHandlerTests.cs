using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Features.History.PushHistory;

namespace SvgEditor.Web.Tests.Features.History.PushHistory;

[TestClass]
public sealed class PushHistoryHandlerTests
{
    [TestMethod]
    public async Task Handle_PushesSnapshotToStack()
    {
        var state = new EditorState
        {
            Document = new SvgDocument
            {
                Elements = [new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } }]
            }
        };
        var stack = new HistoryStack();
        var handler = new PushHistoryHandler(state, stack);

        await handler.Handle(new PushHistoryCommand("test move"));

        Assert.IsTrue(stack.CanUndo);
    }

    [TestMethod]
    public async Task Handle_SnapshotIsIndependentOfOriginal()
    {
        var state = new EditorState
        {
            Document = new SvgDocument
            {
                Elements = [new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10" } }]
            }
        };
        var stack = new HistoryStack();
        var handler = new PushHistoryHandler(state, stack);

        await handler.Handle(new PushHistoryCommand("before move"));

        // Mutate the original document
        state.Document = state.Document.ReplaceElement(
            new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "99" } });

        // Undo should restore x=10
        var restored = stack.Undo(state.Document);
        var restoredRect = (SvgRect)restored!.FindById("r1")!;
        Assert.AreEqual(10, restoredRect.X);
    }

    [TestMethod]
    public async Task Handle_NoDocument_DoesNothing()
    {
        var state = new EditorState { Document = null };
        var stack = new HistoryStack();
        var handler = new PushHistoryHandler(state, stack);

        await handler.Handle(new PushHistoryCommand("noop"));

        Assert.IsFalse(stack.CanUndo);
    }
}
