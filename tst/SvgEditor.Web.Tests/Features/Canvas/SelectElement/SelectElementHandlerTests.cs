using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.SelectElement;

namespace SvgEditor.Web.Tests.Features.Canvas.SelectElement;

[TestClass]
public sealed class SelectElementHandlerTests
{
    private static EditorState CreateState(params SvgElement[] elements) => new()
    {
        Document = new SvgDocument { Elements = [.. elements] }
    };

    [TestMethod]
    public async Task Handle_SelectsSingleElement()
    {
        var state = CreateState(new SvgRect { Id = "r1" });
        var handler = new SelectElementHandler(state);

        await handler.Handle(new SelectElementCommand("r1"));

        Assert.AreEqual("r1", state.SelectedElementId);
        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("r1", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_ClearsSelectionWhenNullId()
    {
        var state = CreateState(new SvgRect { Id = "r1" });
        state.SelectedElementId = "r1";
        state.SelectedElementIds = ["r1"];
        var handler = new SelectElementHandler(state);

        await handler.Handle(new SelectElementCommand(null));

        Assert.IsNull(state.SelectedElementId);
        Assert.IsEmpty(state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_CtrlClick_AddsToSelection()
    {
        var state = CreateState(new SvgRect { Id = "r1" }, new SvgRect { Id = "r2" });
        state.SelectedElementId = "r1";
        state.SelectedElementIds = ["r1"];
        var handler = new SelectElementHandler(state);

        await handler.Handle(new SelectElementCommand("r2", CtrlKey: true));

        Assert.HasCount(2, state.SelectedElementIds);
        Assert.Contains("r1", state.SelectedElementIds);
        Assert.Contains("r2", state.SelectedElementIds);
    }

    [TestMethod]
    public async Task Handle_CtrlClick_RemovesFromSelection()
    {
        var state = CreateState(new SvgRect { Id = "r1" }, new SvgRect { Id = "r2" });
        state.SelectedElementIds = ["r1", "r2"];
        var handler = new SelectElementHandler(state);

        await handler.Handle(new SelectElementCommand("r1", CtrlKey: true));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("r2", state.SelectedElementIds);
        Assert.DoesNotContain("r1", state.SelectedElementIds);
        Assert.AreEqual("r2", state.SelectedElementId);
    }

    [TestMethod]
    public async Task Handle_CtrlClick_LastElementRemoved_ClearsSelectedId()
    {
        var state = CreateState(new SvgRect { Id = "r1" });
        state.SelectedElementIds = ["r1"];
        state.SelectedElementId = "r1";
        var handler = new SelectElementHandler(state);

        await handler.Handle(new SelectElementCommand("r1", CtrlKey: true));

        Assert.IsEmpty(state.SelectedElementIds);
        Assert.IsNull(state.SelectedElementId);
    }

    [TestMethod]
    public async Task Handle_RegularClick_ReplacesSelection()
    {
        var state = CreateState(new SvgRect { Id = "r1" }, new SvgRect { Id = "r2" });
        state.SelectedElementIds = ["r1", "r2"];
        var handler = new SelectElementHandler(state);

        await handler.Handle(new SelectElementCommand("r2"));

        Assert.HasCount(1, state.SelectedElementIds);
        Assert.Contains("r2", state.SelectedElementIds);
        Assert.AreEqual("r2", state.SelectedElementId);
    }

    [TestMethod]
    public async Task Handle_NotifiesStateChanged()
    {
        var state = CreateState(new SvgRect { Id = "r1" });
        var handler = new SelectElementHandler(state);
        var notified = false;
        state.OnStateChanged += () => notified = true;

        await handler.Handle(new SelectElementCommand("r1"));

        Assert.IsTrue(notified);
    }
}
