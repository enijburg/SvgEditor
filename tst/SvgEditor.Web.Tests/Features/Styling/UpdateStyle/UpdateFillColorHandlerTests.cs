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

    [TestMethod]
    public async Task Handle_UpdatesStrokeOnLine()
    {
        var line = new SvgLine { Id = "l1", Attributes = new Dictionary<string, string> { ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100", ["stroke"] = "#000000" } };
        var state = CreateState(line);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#ff0000"));

        var updated = state.Document!.FindById("l1")!;
        Assert.AreEqual("#ff0000", updated.Attributes["stroke"]);
    }

    [TestMethod]
    public async Task Handle_UpdatesStrokeOnPathWithFillNone()
    {
        var path = new SvgPath { Id = "p1", Attributes = new Dictionary<string, string> { ["d"] = "M0 0 L100 100", ["fill"] = "none", ["stroke"] = "#0000ff" } };
        var state = CreateState(path);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["p1"], "#00ff00"));

        var updated = state.Document!.FindById("p1")!;
        Assert.AreEqual("#00ff00", updated.Attributes["stroke"]);
        Assert.AreEqual("none", updated.Attributes["fill"]);
    }

    [TestMethod]
    public async Task Handle_UpdatesMarkerFillForArrowLine()
    {
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#e74c3c" /></marker>"""
        };
        var line = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "50", ["y1"] = "150", ["x2"] = "300", ["y2"] = "150",
                ["stroke"] = "#e74c3c", ["stroke-width"] = "3",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var state = CreateState(defs, line);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#00ff00"));

        var updatedLine = state.Document!.FindById("l1")!;
        Assert.AreEqual("#00ff00", updatedLine.Attributes["stroke"]);

        var updatedDefs = (SvgUnknown)state.Document.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("fill=\"#00ff00\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.DoesNotContain("fill=\"#e74c3c\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Handle_UpdatesMultipleMarkerReferences()
    {
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrow-start" markerWidth="10" markerHeight="7" refX="0" refY="3.5" orient="auto"><polygon points="0 3.5, 10 0, 10 7" fill="#ff0000" /></marker><marker xmlns="http://www.w3.org/2000/svg" id="arrow-end" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#ff0000" /></marker>"""
        };
        var line = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#ff0000",
                ["marker-start"] = "url(#arrow-start)",
                ["marker-end"] = "url(#arrow-end)"
            }
        };
        var state = CreateState(defs, line);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#0000ff"));

        var updatedDefs = (SvgUnknown)state.Document!.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.DoesNotContain("fill=\"#ff0000\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.Contains("fill=\"#0000ff\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Handle_DoesNotUpdateUnreferencedMarkers()
    {
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#e74c3c" /></marker><marker xmlns="http://www.w3.org/2000/svg" id="other-marker" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#999999" /></marker>"""
        };
        var line = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#e74c3c",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var state = CreateState(defs, line);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#00ff00"));

        var updatedDefs = (SvgUnknown)state.Document!.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("fill=\"#999999\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Handle_UpdatesMarkerStrokeWhenPresent()
    {
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#ff0000" stroke="#ff0000" /></marker>"""
        };
        var line = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#ff0000",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var state = CreateState(defs, line);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#00ff00"));

        var updatedDefs = (SvgUnknown)state.Document!.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("fill=\"#00ff00\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.Contains("stroke=\"#00ff00\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }
}
