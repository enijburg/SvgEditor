using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Styling.UpdateStyle;

namespace SvgEditor.Web.Tests.Features.Styling.UpdateStyle;

[TestClass]
public sealed class UpdateFillColorHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new()
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_UpdatesFillOnSingleElement()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0", ["fill"] = "#000000" } };
        var state = CreateState(rect);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["r1"], "#ff0000"));

        var updated = state.Document!.FindById("r1")!;
        Assert.AreEqual("#ff0000", updated.Attributes["fill"]);
    }

    [TestMethod]
    public async Task Handle_UpdatesFillOnMultipleElements()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["fill"] = "#000000" } };
        var r2 = new SvgCircle { Id = "c1", Attributes = new Dictionary<string, string> { ["fill"] = "#111111" } };
        var state = CreateState(r1, r2);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["r1", "c1"], "#00ff00"));

        Assert.AreEqual("#00ff00", state.Document!.FindById("r1")!.Attributes["fill"]);
        Assert.AreEqual("#00ff00", state.Document!.FindById("c1")!.Attributes["fill"]);
    }

    [TestMethod]
    public async Task Handle_AddsFillWhenNotPresent()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "0" } };
        var state = CreateState(rect);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["r1"], "#0000ff"));

        Assert.AreEqual("#0000ff", state.Document!.FindById("r1")!.Attributes["fill"]);
    }

    [TestMethod]
    public async Task Handle_NoDocument_Throws()
    {
        var state = new EditorState { Document = null };
        var handler = new UpdateFillColorHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateFillColorCommand(["r1"], "#ff0000")));
    }

    [TestMethod]
    public async Task Handle_UnknownElementId_Throws()
    {
        var state = CreateState();
        var handler = new UpdateFillColorHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new UpdateFillColorCommand(["nonexistent"], "#ff0000")));
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["fill"] = "#000000" } };
        var state = CreateState(rect);
        var handler = new UpdateFillColorHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new UpdateFillColorCommand(["r1"], "#ff0000"));

        Assert.IsTrue(notified);
    }
}
