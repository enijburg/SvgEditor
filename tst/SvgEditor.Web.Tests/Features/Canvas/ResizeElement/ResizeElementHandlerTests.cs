using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.ResizeElement;

namespace SvgEditor.Web.Tests.Features.Canvas.ResizeElement;

[TestClass]
public sealed class ResizeElementHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new()
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_ResizesRect()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "10", ["width"] = "100", ["height"] = "50" } };
        var state = CreateState(rect);
        state.SelectedElementIds = ["r1"];
        var handler = new ResizeElementHandler(state);
        var original = new BoundingBox(10, 10, 100, 50);
        var updated = new BoundingBox(10, 10, 200, 100);

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, original, updated));

        var resized = (SvgRect)state.Document!.FindById("r1")!;
        Assert.AreEqual(10, resized.X);
        Assert.AreEqual(10, resized.Y);
        Assert.AreEqual(200, resized.Width);
        Assert.AreEqual(100, resized.Height);
    }

    [TestMethod]
    public async Task Handle_ResizesCircle()
    {
        var circle = new SvgCircle { Id = "c1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["r"] = "20" } };
        var state = CreateState(circle);
        state.SelectedElementIds = ["c1"];
        var handler = new ResizeElementHandler(state);
        var original = new BoundingBox(30, 30, 40, 40);
        var updated = new BoundingBox(30, 30, 80, 80);

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, original, updated));

        var resized = (SvgCircle)state.Document!.FindById("c1")!;
        Assert.AreEqual(70, resized.Cx);
        Assert.AreEqual(70, resized.Cy);
        Assert.AreEqual(40, resized.R);
    }

    [TestMethod]
    public async Task Handle_ResizesEllipse()
    {
        var ellipse = new SvgEllipse { Id = "e1", Attributes = new Dictionary<string, string> { ["cx"] = "50", ["cy"] = "50", ["rx"] = "30", ["ry"] = "20" } };
        var state = CreateState(ellipse);
        state.SelectedElementIds = ["e1"];
        var handler = new ResizeElementHandler(state);
        var original = new BoundingBox(20, 30, 60, 40);
        var updated = new BoundingBox(20, 30, 120, 80);

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, original, updated));

        var resized = (SvgEllipse)state.Document!.FindById("e1")!;
        Assert.AreEqual(80, resized.Cx);
        Assert.AreEqual(70, resized.Cy);
        Assert.AreEqual(60, resized.Rx);
        Assert.AreEqual(40, resized.Ry);
    }

    [TestMethod]
    public async Task Handle_ResizesLine()
    {
        var line = new SvgLine { Id = "l1", Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100" } };
        var state = CreateState(line);
        state.SelectedElementIds = ["l1"];
        var handler = new ResizeElementHandler(state);
        var original = new BoundingBox(0, 0, 100, 100);
        var updated = new BoundingBox(0, 0, 200, 200);

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, original, updated));

        var resized = (SvgLine)state.Document!.FindById("l1")!;
        Assert.AreEqual(0, resized.X1);
        Assert.AreEqual(0, resized.Y1);
        Assert.AreEqual(200, resized.X2);
        Assert.AreEqual(200, resized.Y2);
    }

    [TestMethod]
    public async Task Handle_ResizesImage()
    {
        var image = new SvgImage { Id = "i1", Attributes = new Dictionary<string, string> { ["x"] = "10", ["y"] = "20", ["width"] = "100", ["height"] = "80" } };
        var state = CreateState(image);
        state.SelectedElementIds = ["i1"];
        var handler = new ResizeElementHandler(state);
        var original = new BoundingBox(10, 20, 100, 80);
        var updated = new BoundingBox(10, 20, 200, 160);

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, original, updated));

        var resized = (SvgImage)state.Document!.FindById("i1")!;
        Assert.AreEqual(10, resized.X);
        Assert.AreEqual(20, resized.Y);
        Assert.AreEqual(200, resized.Width);
        Assert.AreEqual(160, resized.Height);
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var rect = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0", ["width"] = "50", ["height"] = "50" } };
        var state = CreateState(rect);
        state.SelectedElementIds = ["r1"];
        var handler = new ResizeElementHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, new BoundingBox(0, 0, 50, 50), new BoundingBox(0, 0, 100, 100)));

        Assert.IsTrue(notified);
    }

    [TestMethod]
    public async Task Handle_NoDocument_Throws()
    {
        var state = new EditorState();
        var handler = new ResizeElementHandler(state);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => handler.Handle(new ResizeElementCommand(["r1"], new BoundingBox(0, 0, 50, 50), new BoundingBox(0, 0, 100, 100))));
    }

    [TestMethod]
    public async Task Handle_ResizesMultipleElements()
    {
        var r1 = new SvgRect { Id = "r1", Attributes = new Dictionary<string, string> { ["x"] = "0", ["y"] = "0", ["width"] = "50", ["height"] = "50" } };
        var r2 = new SvgRect { Id = "r2", Attributes = new Dictionary<string, string> { ["x"] = "50", ["y"] = "50", ["width"] = "50", ["height"] = "50" } };
        var state = CreateState(r1, r2);
        state.SelectedElementIds = ["r1", "r2"];
        var handler = new ResizeElementHandler(state);
        var original = new BoundingBox(0, 0, 100, 100);
        var updated = new BoundingBox(0, 0, 200, 200);

        await handler.Handle(new ResizeElementCommand(state.SelectedElementIds, original, updated));

        var resized1 = (SvgRect)state.Document!.FindById("r1")!;
        var resized2 = (SvgRect)state.Document!.FindById("r2")!;
        Assert.AreEqual(0, resized1.X);
        Assert.AreEqual(0, resized1.Y);
        Assert.AreEqual(100, resized1.Width);
        Assert.AreEqual(100, resized1.Height);
        Assert.AreEqual(100, resized2.X);
        Assert.AreEqual(100, resized2.Y);
        Assert.AreEqual(100, resized2.Width);
        Assert.AreEqual(100, resized2.Height);
    }
}
