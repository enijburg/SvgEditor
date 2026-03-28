using SvgEditor.Web.Features.Canvas.DeleteElement;
using SvgEditor.Web.Features.Canvas.Models;

namespace SvgEditor.Web.Tests.Features.Canvas.DeleteElement;

[TestClass]
public sealed class DeleteElementHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new EditorState
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_DeletesSingleElement()
    {
        var rect = new SvgRect { Id = "r1" };
        var state = CreateState(rect, new SvgRect { Id = "r2" });
        state.SelectedElementId = "r1";
        state.SelectedElementIds = ["r1"];
        var handler = new DeleteElementHandler(state);

        await handler.Handle(new DeleteElementCommand(["r1"]));

        Assert.IsNull(state.Document!.FindById("r1"));
        Assert.IsNotNull(state.Document.FindById("r2"));
        Assert.HasCount(1, state.Document.Elements);
    }

    [TestMethod]
    public async Task Handle_DeletesMultipleElements()
    {
        var state = CreateState(
            new SvgRect { Id = "r1" },
            new SvgRect { Id = "r2" },
            new SvgRect { Id = "r3" });
        state.SelectedElementIds = ["r1", "r2"];
        var handler = new DeleteElementHandler(state);

        await handler.Handle(new DeleteElementCommand(["r1", "r2"]));

        Assert.IsNull(state.Document!.FindById("r1"));
        Assert.IsNull(state.Document.FindById("r2"));
        Assert.IsNotNull(state.Document.FindById("r3"));
        Assert.HasCount(1, state.Document.Elements);
    }

    [TestMethod]
    public async Task Handle_ClearsSelection()
    {
        var rect = new SvgRect { Id = "r1" };
        var state = CreateState(rect);
        state.SelectedElementId = "r1";
        state.SelectedElementIds = ["r1"];
        var handler = new DeleteElementHandler(state);

        await handler.Handle(new DeleteElementCommand(["r1"]));

        Assert.IsNull(state.SelectedElementId);
        Assert.IsEmpty(state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_NoDocument_Throws()
    {
        var state = new EditorState { Document = null };
        var handler = new DeleteElementHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteElementCommand(["any"])));
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var rect = new SvgRect { Id = "r1" };
        var state = CreateState(rect);
        var handler = new DeleteElementHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new DeleteElementCommand(["r1"]));

        Assert.IsTrue(notified);
    }

    [TestMethod]
    public async Task Handle_UnknownId_IsIgnored()
    {
        var rect = new SvgRect { Id = "r1" };
        var state = CreateState(rect);
        var handler = new DeleteElementHandler(state);

        await handler.Handle(new DeleteElementCommand(["nonexistent"]));

        Assert.IsNotNull(state.Document!.FindById("r1"));
        Assert.HasCount(1, state.Document.Elements);
    }
}
