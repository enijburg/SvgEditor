using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;

namespace SvgEditor.Web.Tests.Features.Canvas.UpdateElement;

[TestClass]
public sealed class UpdateMultipleElementsHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new()
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_MovesMultipleElements()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var r2 = new SvgRect { Id = "r2", Attributes = new Dictionary<string, string> { ["x"] = "50", ["y"] = "60" } };
        var state = CreateState(r1, r2);
        var handler = new UpdateMultipleElementsHandler(state);

        await handler.Handle(new UpdateMultipleElementsCommand(["r1", "r2"], 5, -3));

        var moved1 = (SvgRect)state.Document!.FindById("r1")!;
        var moved2 = (SvgRect)state.Document!.FindById("r2")!;
        Assert.AreEqual(15, moved1.X);
        Assert.AreEqual(17, moved1.Y);
        Assert.AreEqual(55, moved2.X);
        Assert.AreEqual(57, moved2.Y);
    }

    [TestMethod]
    public async Task Handle_SkipsUnknownIds()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var state = CreateState(r1);
        var handler = new UpdateMultipleElementsHandler(state);

        await handler.Handle(new UpdateMultipleElementsCommand(["r1", "nonexistent"], 5, 5));

        var moved = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(15, moved.X);
        Assert.AreEqual(25, moved.Y);
    }

    [TestMethod]
    public async Task Handle_NoDocument_Throws()
    {
        var state = new EditorState { Document = null };
        var handler = new UpdateMultipleElementsHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateMultipleElementsCommand(["any"], 0, 0)));
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0" } };
        var state = CreateState(r1);
        var handler = new UpdateMultipleElementsHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new UpdateMultipleElementsCommand(["r1"], 1, 1));

        Assert.IsTrue(notified);
    }
}
