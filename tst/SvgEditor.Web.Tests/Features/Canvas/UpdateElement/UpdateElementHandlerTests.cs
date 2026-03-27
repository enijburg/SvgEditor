using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;

namespace SvgEditor.Web.Tests.Features.Canvas.UpdateElement;

[TestClass]
public sealed class UpdateElementHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new EditorState
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_MovesRect()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20" } };
        var state = CreateState(rect);
        var handler = new UpdateElementHandler(state);

        await handler.Handle(new UpdateElementCommand("r1", 5, -3));

        var moved = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(15, moved.X);
        Assert.AreEqual(17, moved.Y);
    }

    [TestMethod]
    public async Task Handle_MovesCircle()
    {
        var circle = new SvgCircle { Id = "c1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "60", ["r"] = "10" } };
        var state = CreateState(circle);
        var handler = new UpdateElementHandler(state);

        await handler.Handle(new UpdateElementCommand("c1", 10, 10));

        var moved = (SvgCircle)state.Document!.FindById("c1")!;
        Assert.AreEqual(60, moved.Cx);
        Assert.AreEqual(70, moved.Cy);
    }

    [TestMethod]
    public async Task Handle_MovesLine()
    {
        var line = new SvgLine { Id = "l1", Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100" } };
        var state = CreateState(line);
        var handler = new UpdateElementHandler(state);

        await handler.Handle(new UpdateElementCommand("l1", 10, 20));

        var moved = (SvgLine)state.Document!.FindById("l1")!;
        Assert.AreEqual(10, moved.X1);
        Assert.AreEqual(20, moved.Y1);
        Assert.AreEqual(110, moved.X2);
        Assert.AreEqual(120, moved.Y2);
    }

    [TestMethod]
    public async Task Handle_UnknownElementId_Throws()
    {
        var state = CreateState();
        var handler = new UpdateElementHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateElementCommand("nonexistent", 0, 0)));
    }

    [TestMethod]
    public async Task Handle_NoDocument_Throws()
    {
        var state = new EditorState { Document = null };
        var handler = new UpdateElementHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateElementCommand("any", 0, 0)));
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0" } };
        var state = CreateState(rect);
        var handler = new UpdateElementHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new UpdateElementCommand("r1", 1, 1));

        Assert.IsTrue(notified);
    }
}
