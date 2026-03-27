using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.SelectElement;

namespace SvgEditor.Web.Tests.Features.Canvas.SelectElement;

[TestClass]
public sealed class FenceSelectHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new()
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_SelectsElementsInFence()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "20", ["height"] = "20" } };
        var r2 = new SvgRect { Id = "r2", Attributes = new Dictionary<string, string> { ["x"] = "100", ["y"] = "100", ["width"] = "20", ["height"] = "20" } };
        var state = CreateState(r1, r2);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 50, 50)));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("r1", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_SelectsMultipleElements()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "20", ["height"] = "20" } };
        var r2 = new SvgRect { Id = "r2", Attributes = new Dictionary<string, string> { ["x"] = "30", ["y"] = "30", ["width"] = "20", ["height"] = "20" } };
        var state = CreateState(r1, r2);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 60, 60)));

        Assert.HasCount(2, state.SelectedElementIds);
        Assert.Contains("r1", state.SelectedElementIds);
        Assert.Contains("r2", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_EmptyFence_SelectsNothing()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "100", ["y"] = "100", ["width"] = "20", ["height"] = "20" } };
        var state = CreateState(r1);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 10, 10)));

        Assert.IsEmpty(state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_SelectsCircleInFence()
    {
        var c1 = new SvgCircle { Id = "c1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["r"] = "10" } };
        var state = CreateState(c1);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(35, 35, 30, 30)));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("c1", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_NoDocument_DoesNothing()
    {
        var state = new EditorState { Document = null };
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 100, 100)));

        Assert.IsEmpty(state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "20", ["height"] = "20" } };
        var state = CreateState(r1);
        var handler = new FenceSelectHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 50, 50)));

        Assert.IsTrue(notified);
    }

    [TestMethod]
    public void Intersects_ReturnsTrue_WhenFullyContained()
    {
        Assert.IsTrue(FenceSelectHandler.Intersects(
            new BoundingBox(0, 0, 100, 100),
            new BoundingBox(10, 10, 20, 20)));
    }

    [TestMethod]
    public void Intersects_ReturnsFalse_WhenNotOverlapping()
    {
        Assert.IsFalse(FenceSelectHandler.Intersects(
            new BoundingBox(0, 0, 10, 10),
            new BoundingBox(20, 20, 10, 10)));
    }

    [TestMethod]
    public void Intersects_ReturnsTrue_WhenPartiallyOverlapping()
    {
        Assert.IsTrue(FenceSelectHandler.Intersects(
            new BoundingBox(0, 0, 50, 50),
            new BoundingBox(25, 25, 50, 50)));
    }

    [TestMethod]
    public void Intersects_ReturnsTrue_WhenExactMatch()
    {
        Assert.IsTrue(FenceSelectHandler.Intersects(
            new BoundingBox(10, 10, 30, 30),
            new BoundingBox(10, 10, 30, 30)));
    }

    [TestMethod]
    public async Task Handle_PartialOverlap_SelectsElement()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "40", ["y"] = "40", ["width"] = "30", ["height"] = "30" } };
        var state = CreateState(r1);
        var handler = new FenceSelectHandler(state);

        // Fence partially overlaps r1 (0,0)-(50,50) vs element (40,40)-(70,70)
        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 50, 50)));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("r1", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_SingleMatch_SetsSelectedElementId()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "20", ["height"] = "20" } };
        var state = CreateState(r1);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 50, 50)));

        Assert.AreEqual("r1", state.SelectedElementId);
    }

    [TestMethod]
    public async Task Handle_MultipleMatches_ClearsSelectedElementId()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "20", ["height"] = "20" } };
        var r2 = new SvgRect { Id = "r2", Attributes = new Dictionary<string, string> { ["x"] = "30", ["y"] = "30", ["width"] = "20", ["height"] = "20" } };
        var state = CreateState(r1, r2);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 60, 60)));

        Assert.IsNull(state.SelectedElementId);
    }

    [TestMethod]
    public async Task Handle_SelectsImageInFence()
    {
        var img = new SvgImage { Id = "img1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "50", ["height"] = "40" } };
        var state = CreateState(img);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(0, 0, 100, 100)));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("img1", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_SelectsTextPartiallyOutsideFence()
    {
        // Text at (50, 100) has bounding box (50, 84, width, 16) - extends above Y=84
        // Fence from (40, 90) should intersect even though top of text bbox is above fence
        var text = new SvgText { Id = "t1", Attributes = new Dictionary<string, string> { ["x"] = "50", ["y"] = "100" }, Content = "Hello" };
        var state = CreateState(text);
        var handler = new FenceSelectHandler(state);

        await handler.Handle(new FenceSelectCommand(new BoundingBox(40, 90, 100, 20)));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("t1", state.SelectedElementIds);
    }

    [TestMethod]
    public void Intersects_ReturnsFalse_WhenEdgesTouching()
    {
        // Two rectangles sharing an edge but not overlapping
        Assert.IsFalse(FenceSelectHandler.Intersects(
            new BoundingBox(0, 0, 10, 10),
            new BoundingBox(10, 0, 10, 10)));
    }
}
