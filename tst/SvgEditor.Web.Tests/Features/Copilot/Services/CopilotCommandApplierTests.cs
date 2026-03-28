using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Copilot.Models;
using SvgEditor.Web.Features.Copilot.Services;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Tests.Features.Copilot.Services;

[TestClass]
public sealed class CopilotCommandApplierTests
{
    /// <summary>
    /// A no-op mediator that records sent requests but does nothing.
    /// </summary>
    private sealed class StubMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult(default(TResponse)!);
    }

    [TestMethod]
    public async Task SetStroke_UpdatesMarkerColorsForArrowLine()
    {
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#000000" /></marker>"""
        };
        var line = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#000000", ["stroke-width"] = "2",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var state = new EditorState
        {
            Document = new SvgDocument { Elements = [defs, line] }
        };
        var applier = new CopilotCommandApplier(new StubMediator(), state);

        await applier.ApplyCommandsAsync(
        [
            new CopilotCommand { Type = "SetStroke", ElementId = "l1", Stroke = "#00ff00", Width = 3.0 }
        ], "test");

        var updatedLine = state.Document!.FindById("l1")!;
        Assert.AreEqual("#00ff00", updatedLine.Attributes["stroke"]);
        Assert.AreEqual("3", updatedLine.Attributes["stroke-width"]);

        var updatedDefs = (SvgUnknown)state.Document.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("fill=\"#00ff00\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.DoesNotContain("fill=\"#000000\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task SetStroke_WithoutMarkers_DoesNotThrow()
    {
        var line = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#000000"
            }
        };
        var state = new EditorState
        {
            Document = new SvgDocument { Elements = [line] }
        };
        var applier = new CopilotCommandApplier(new StubMediator(), state);

        await applier.ApplyCommandsAsync(
        [
            new CopilotCommand { Type = "SetStroke", ElementId = "l1", Stroke = "#ff0000", Width = 2.0 }
        ], "test");

        Assert.AreEqual("#ff0000", state.Document!.FindById("l1")!.Attributes["stroke"]);
    }

    [TestMethod]
    public async Task SetStroke_SharedMarker_ClonesForSelectedElement()
    {
        var defs = new SvgUnknown("defs")
        {
            Id = "d1",
            Attributes = [],
            InnerXml = """<marker xmlns="http://www.w3.org/2000/svg" id="arrowhead" markerWidth="10" markerHeight="7" refX="10" refY="3.5" orient="auto"><polygon points="0 0, 10 3.5, 0 7" fill="#000000" /></marker>"""
        };
        var line1 = new SvgLine
        {
            Id = "l1",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "0", ["y1"] = "0", ["x2"] = "100", ["y2"] = "100",
                ["stroke"] = "#000000",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var line2 = new SvgLine
        {
            Id = "l2",
            Attributes = new Dictionary<string, string>
            {
                ["x1"] = "200", ["y1"] = "0", ["x2"] = "300", ["y2"] = "100",
                ["stroke"] = "#000000",
                ["marker-end"] = "url(#arrowhead)"
            }
        };
        var state = new EditorState
        {
            Document = new SvgDocument { Elements = [defs, line1, line2] }
        };
        var applier = new CopilotCommandApplier(new StubMediator(), state);

        await applier.ApplyCommandsAsync(
        [
            new CopilotCommand { Type = "SetStroke", ElementId = "l1", Stroke = "#00ff00", Width = 2.0 }
        ], "test");

        // line1 should reference a cloned marker
        var updatedLine1 = state.Document!.FindById("l1")!;
        Assert.AreNotEqual("url(#arrowhead)", updatedLine1.Attributes["marker-end"]);
        Assert.IsTrue(updatedLine1.Attributes["marker-end"].StartsWith("url(#arrowhead-", StringComparison.Ordinal));

        // line2 should still reference the original marker
        var updatedLine2 = state.Document!.FindById("l2")!;
        Assert.AreEqual("url(#arrowhead)", updatedLine2.Attributes["marker-end"]);

        // Original marker should retain original color
        var updatedDefs = (SvgUnknown)state.Document.Elements.First(e => e is SvgUnknown { Tag: "defs" });
        Assert.Contains("fill=\"#000000\"", updatedDefs.InnerXml, StringComparison.Ordinal);
        Assert.Contains("fill=\"#00ff00\"", updatedDefs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task AddArrowBetweenSelection_CreatesArchedArrowPath()
    {
        var r1 = new SvgRect
        {
            Id = "r1",
            Attributes = new Dictionary<string, string>
            {
                ["x"] = "50", ["y"] = "50", ["width"] = "100", ["height"] = "60"
            }
        };
        var r2 = new SvgRect
        {
            Id = "r2",
            Attributes = new Dictionary<string, string>
            {
                ["x"] = "300", ["y"] = "200", ["width"] = "100", ["height"] = "60"
            }
        };
        var state = new EditorState
        {
            Document = new SvgDocument { Width = 800, Height = 600, Elements = [r1, r2] }
        };
        var applier = new CopilotCommandApplier(new StubMediator(), state);

        await applier.ApplyCommandsAsync(
        [
            new CopilotCommand { Type = "AddArrowBetweenSelection", SourceElementId = "r1", TargetElementId = "r2" }
        ], "test arrow");

        var doc = state.Document!;

        // Should have defs + r1 + r2 + arrow path = 4 elements
        Assert.HasCount(4, doc.Elements);

        // First element should be a defs with a marker
        var defs = doc.Elements.OfType<SvgUnknown>().FirstOrDefault(e => e.Tag == "defs");
        Assert.IsNotNull(defs);
        Assert.Contains("marker", defs.InnerXml, StringComparison.Ordinal);
        Assert.Contains("polygon", defs.InnerXml, StringComparison.Ordinal);

        // Last element should be the arrow path
        var arrow = doc.Elements.OfType<SvgPath>().FirstOrDefault();
        Assert.IsNotNull(arrow);
        Assert.AreEqual("none", arrow.Attributes["fill"]);
        Assert.AreEqual("#333333", arrow.Attributes["stroke"]);
        Assert.IsTrue(arrow.Attributes["marker-end"].StartsWith("url(#arrowhead-", StringComparison.Ordinal));

        // Path should contain a quadratic bezier curve (Q command)
        Assert.Contains("M ", arrow.D, StringComparison.Ordinal);
        Assert.Contains(" Q ", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task AddArrowBetweenSelection_MissingElement_DoesNotCrash()
    {
        var r1 = new SvgRect
        {
            Id = "r1",
            Attributes = new Dictionary<string, string>
            {
                ["x"] = "50", ["y"] = "50", ["width"] = "100", ["height"] = "60"
            }
        };
        var state = new EditorState
        {
            Document = new SvgDocument { Width = 800, Height = 600, Elements = [r1] }
        };
        var applier = new CopilotCommandApplier(new StubMediator(), state);

        await applier.ApplyCommandsAsync(
        [
            new CopilotCommand { Type = "AddArrowBetweenSelection", SourceElementId = "r1", TargetElementId = "nonexistent" }
        ], "test arrow");

        // Document should be unchanged
        Assert.HasCount(1, state.Document!.Elements);
    }

    [TestMethod]
    public void BuildArrowElements_CreatesCorrectArcBetweenElements()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);
        var targetBBox = new BoundingBox(300, 200, 100, 60);

        var (defs, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "test-marker", "test-arrow");

        // Defs should contain the marker
        Assert.AreEqual("defs", defs.Tag);
        Assert.Contains("id=\"test-marker\"", defs.InnerXml, StringComparison.Ordinal);

        // Arrow should reference the marker
        Assert.AreEqual("test-arrow", arrow.Id);
        Assert.AreEqual("url(#test-marker)", arrow.Attributes["marker-end"]);

        // Path should start at source center (100, 80) and end at target center (350, 230)
        Assert.Contains("M 100 80", arrow.D, StringComparison.Ordinal);
        Assert.Contains("350 230", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_WithCustomStyling_AppliesStrokeAndDash()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);
        var targetBBox = new BoundingBox(300, 200, 100, 60);

        var (defs, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "test-marker", "test-arrow",
            stroke: "#FF0000", strokeWidth: "3", strokeDashArray: "8 4");

        // Arrow styling
        Assert.AreEqual("#FF0000", arrow.Attributes["stroke"]);
        Assert.AreEqual("3", arrow.Attributes["stroke-width"]);
        Assert.AreEqual("8 4", arrow.Attributes["stroke-dasharray"]);

        // Marker should use the custom stroke color
        Assert.Contains("fill=\"#FF0000\"", defs.InnerXml, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_WithoutDashArray_DoesNotIncludeDashAttribute()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);
        var targetBBox = new BoundingBox(300, 200, 100, 60);

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "test-marker", "test-arrow");

        Assert.IsFalse(arrow.Attributes.ContainsKey("stroke-dasharray"));
    }

    [TestMethod]
    public async Task AddArrowBetweenSelection_WithDashStyling_AppliesStrokeAndDash()
    {
        var r1 = new SvgRect
        {
            Id = "r1",
            Attributes = new Dictionary<string, string>
            {
                ["x"] = "50", ["y"] = "50", ["width"] = "100", ["height"] = "60"
            }
        };
        var r2 = new SvgRect
        {
            Id = "r2",
            Attributes = new Dictionary<string, string>
            {
                ["x"] = "300", ["y"] = "200", ["width"] = "100", ["height"] = "60"
            }
        };
        var state = new EditorState
        {
            Document = new SvgDocument { Width = 800, Height = 600, Elements = [r1, r2] }
        };
        var applier = new CopilotCommandApplier(new StubMediator(), state);

        await applier.ApplyCommandsAsync(
        [
            new CopilotCommand
            {
                Type = "AddArrowBetweenSelection",
                SourceElementId = "r1",
                TargetElementId = "r2",
                Stroke = "#000000",
                Width = 3.0,
                StrokeDashArray = "8 4"
            }
        ], "test dashed arrow");

        var doc = state.Document!;
        var arrow = doc.Elements.OfType<SvgPath>().FirstOrDefault();
        Assert.IsNotNull(arrow);
        Assert.AreEqual("#000000", arrow.Attributes["stroke"]);
        Assert.AreEqual("3", arrow.Attributes["stroke-width"]);
        Assert.AreEqual("8 4", arrow.Attributes["stroke-dasharray"]);

        // Marker should match the arrow stroke color
        var defs = doc.Elements.OfType<SvgUnknown>().FirstOrDefault(e => e.Tag == "defs");
        Assert.IsNotNull(defs);
        Assert.Contains("fill=\"#000000\"", defs.InnerXml, StringComparison.Ordinal);
    }
}
