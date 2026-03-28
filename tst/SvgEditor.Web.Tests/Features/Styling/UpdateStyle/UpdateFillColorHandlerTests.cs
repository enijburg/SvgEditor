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

    [TestMethod]
    public async Task Handle_SharedMarker_ClonesForSelectedElement()
    {
        // Two lines sharing the same marker definition
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#e74c3c" /></marker>"""
        };
        var line1 = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#e74c3c",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var line2 = new SvgLine
        {
            Id = "l2",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "200", ["y1"] = "0", ["x2"] = "300", ["y2"] = "100",
                ["stroke"] = "#e74c3c",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var state = CreateState(defs, line1, line2);
        var handler = new UpdateFillColorHandler(state);

        // Change color of only line1
        await handler.Handle(new UpdateFillColorCommand(["l1"], "#00ff00"));

        // line1's stroke should be updated
        var updatedLine1 = state.Document!.FindById("l1")!;
        Assert.AreEqual("#00ff00", updatedLine1.Attributes["stroke"]);

        // line2 should still reference the original marker and retain original stroke
        var updatedLine2 = state.Document!.FindById("l2")!;
        Assert.AreEqual("#e74c3c", updatedLine2.Attributes["stroke"]);
        Assert.AreEqual("url(#arrowhead)", updatedLine2.Attributes["marker-end"]);

        // line1 should now reference a new cloned marker (not the original)
        Assert.AreNotEqual("url(#arrowhead)", updatedLine1.Attributes["marker-end"]);
        Assert.IsTrue(updatedLine1.Attributes["marker-end"].StartsWith("url(#arrowhead-", StringComparison.Ordinal));

        // The original marker in defs should retain the original color
        var updatedDefs = (SvgUnknown)state.Document.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("id=\"arrowhead\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.Contains("fill=\"#e74c3c\"", updatedDefs.InnerXml, StringComparison.Ordinal);

        // The cloned marker should have the new color
        Assert.Contains("fill=\"#00ff00\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Handle_SharedMarker_MultipleMarkerAttributes_ClonesAll()
    {
        // Line with both marker-start and marker-end shared with another line
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrow-start" markerWidth="10" markerHeight="7" refX="0" refY="3.5" orient="auto"><polygon points="0 3.5, 10 0, 10 7" fill="#ff0000" /></marker><marker xmlns="http://www.w3.org/2000/svg" id="arrow-end" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#ff0000" /></marker>"""
        };
        var line1 = new SvgLine
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
        var line2 = new SvgLine
        {
            Id = "l2",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "200", ["y1"] = "0", ["x2"] = "300", ["y2"] = "100",
                ["stroke"] = "#ff0000",
                ["marker-start"] = "url(#arrow-start)",
                ["marker-end"] = "url(#arrow-end)"
            }
        };
        var state = CreateState(defs, line1, line2);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#0000ff"));

        // line2 should still reference original markers
        var updatedLine2 = state.Document!.FindById("l2")!;
        Assert.AreEqual("url(#arrow-start)", updatedLine2.Attributes["marker-start"]);
        Assert.AreEqual("url(#arrow-end)", updatedLine2.Attributes["marker-end"]);

        // line1 should reference cloned markers
        var updatedLine1 = state.Document!.FindById("l1")!;
        Assert.IsTrue(updatedLine1.Attributes["marker-start"].StartsWith("url(#arrow-start-", StringComparison.Ordinal));
        Assert.IsTrue(updatedLine1.Attributes["marker-end"].StartsWith("url(#arrow-end-", StringComparison.Ordinal));

        // Original markers should keep original color
        var updatedDefs = (SvgUnknown)state.Document.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("id=\"arrow-start\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.Contains("id=\"arrow-end\"", updatedDefs.InnerXml, StringComparison.Ordinal);

        // Cloned markers should have new color
        Assert.Contains("fill=\"#0000ff\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task Handle_PartiallySharedMarker_OnlyClonesSharedOnes()
    {
        // Line1 has marker-start (exclusive) and marker-end (shared with line2)
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="exclusive-start" markerWidth="10" markerHeight="7" refX="0" refY="3.5" orient="auto"><polygon points="0 3.5, 10 0, 10 7" fill="#ff0000" /></marker><marker xmlns="http://www.w3.org/2000/svg" id="shared-end" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#ff0000" /></marker>"""
        };
        var line1 = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#ff0000",
                ["marker-start"] = "url(#exclusive-start)",
                ["marker-end"] = "url(#shared-end)"
            }
        };
        var line2 = new SvgLine
        {
            Id = "l2",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "200", ["y1"] = "0", ["x2"] = "300", ["y2"] = "100",
                ["stroke"] = "#ff0000",
                ["marker-end"] = "url(#shared-end)"
            }
        };
        var state = CreateState(defs, line1, line2);
        var handler = new UpdateFillColorHandler(state);

        await handler.Handle(new UpdateFillColorCommand(["l1"], "#0000ff"));

        var updatedLine1 = state.Document!.FindById("l1")!;

        // exclusive-start should be updated in place (not cloned)
        Assert.AreEqual("url(#exclusive-start)", updatedLine1.Attributes["marker-start"]);

        // shared-end should be cloned
        Assert.IsTrue(updatedLine1.Attributes["marker-end"].StartsWith("url(#shared-end-", StringComparison.Ordinal));

        // Original shared-end should retain old color
        var updatedDefs = (SvgUnknown)state.Document.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        // The original "exclusive-start" should have new color (updated in place)
        Assert.Contains("id=\"exclusive-start\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }
}
