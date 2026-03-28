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

        // Default anchor is "border" — path starts at source border (150, 110) and ends at target border (300, 200)
        Assert.Contains("M 150 110", arrow.D, StringComparison.Ordinal);
        Assert.Contains("300 200", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_CenterAnchor_UsesElementCenters()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);
        var targetBBox = new BoundingBox(300, 200, 100, 60);

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "test-marker", "test-arrow",
            sourceAnchor: "center", targetAnchor: "center");

        // Center-to-center: source center (100, 80), target center (350, 230)
        Assert.Contains("M 100 80", arrow.D, StringComparison.Ordinal);
        Assert.Contains("350 230", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_MixedAnchors_BorderToCenterAndCenterToBorder()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);
        var targetBBox = new BoundingBox(300, 200, 100, 60);

        // Border source → center target
        var (_, arrow1) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m1", "a1",
            sourceAnchor: "border", targetAnchor: "center");

        Assert.Contains("M 150 110", arrow1.D, StringComparison.Ordinal);
        Assert.Contains("350 230", arrow1.D, StringComparison.Ordinal);

        // Center source → border target
        var (_, arrow2) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m2", "a2",
            sourceAnchor: "center", targetAnchor: "border");

        Assert.Contains("M 100 80", arrow2.D, StringComparison.Ordinal);
        Assert.Contains("300 200", arrow2.D, StringComparison.Ordinal);
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

    [TestMethod]
    public void ComputeBorderIntersection_RayToRight_IntersectsRightEdge()
    {
        var bbox = new BoundingBox(0, 0, 200, 100);
        // Center = (100, 50), target to the right at (300, 50)
        var (x, y) = CopilotCommandApplier.ComputeBorderIntersection(bbox, 100, 50, 300, 50);

        Assert.AreEqual(200, x, 0.01); // right edge
        Assert.AreEqual(50, y, 0.01);  // horizontally centered
    }

    [TestMethod]
    public void ComputeBorderIntersection_RayDiagonal_IntersectsCorrectEdge()
    {
        var bbox = new BoundingBox(50, 50, 100, 60);
        // Center = (100, 80), target at (350, 230) → exits right/bottom corner
        var (x, y) = CopilotCommandApplier.ComputeBorderIntersection(bbox, 100, 80, 350, 230);

        Assert.AreEqual(150, x, 0.01);
        Assert.AreEqual(110, y, 0.01);
    }

    [TestMethod]
    public void ComputeBorderIntersection_RayToLeft_IntersectsLeftEdge()
    {
        var bbox = new BoundingBox(200, 100, 100, 60);
        // Center = (250, 130), target to the left at (0, 130)
        var (x, y) = CopilotCommandApplier.ComputeBorderIntersection(bbox, 250, 130, 0, 130);

        Assert.AreEqual(200, x, 0.01); // left edge
        Assert.AreEqual(130, y, 0.01);
    }

    [TestMethod]
    public void ComputeBorderIntersection_ZeroDirection_ReturnsCenterPoint()
    {
        var bbox = new BoundingBox(50, 50, 100, 60);
        // fromX == toX, fromY == toY
        var (x, y) = CopilotCommandApplier.ComputeBorderIntersection(bbox, 100, 80, 100, 80);

        Assert.AreEqual(100, x, 0.01);
        Assert.AreEqual(80, y, 0.01);
    }

    [TestMethod]
    public async Task AddArrowBetweenSelection_BorderAnchor_StartsAtBorder()
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
                SourceAnchor = "border",
                TargetAnchor = "border"
            }
        ], "test border arrow");

        var doc = state.Document!;
        var arrow = doc.Elements.OfType<SvgPath>().FirstOrDefault();
        Assert.IsNotNull(arrow);

        // Border anchored: should start at source border (150, 110) not center (100, 80)
        Assert.Contains("M 150 110", arrow.D, StringComparison.Ordinal);
        // Target border at (300, 200) not center (350, 230)
        Assert.Contains("300 200", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_RightAnchor_StartsFromRightEdge()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);  // right edge x = 150
        var targetBBox = new BoundingBox(300, 200, 100, 60);

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m1", "a1",
            sourceAnchor: "right", targetAnchor: "border");

        // Source right edge midpoint: (150, 80)
        Assert.Contains("M 150 80", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_LeftAnchor_StartsFromLeftEdge()
    {
        var sourceBBox = new BoundingBox(300, 200, 100, 60);  // left edge x = 300
        var targetBBox = new BoundingBox(50, 50, 100, 60);

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m1", "a1",
            sourceAnchor: "left", targetAnchor: "border");

        // Source left edge midpoint: (300, 230)
        Assert.Contains("M 300 230", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_TopAnchor_StartsFromTopEdge()
    {
        var sourceBBox = new BoundingBox(50, 200, 100, 60);  // top edge y = 200
        var targetBBox = new BoundingBox(50, 50, 100, 60);

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m1", "a1",
            sourceAnchor: "top", targetAnchor: "border");

        // Source top edge midpoint: (100, 200)
        Assert.Contains("M 100 200", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_BottomAnchor_StartsFromBottomEdge()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);  // bottom edge y = 110
        var targetBBox = new BoundingBox(50, 300, 100, 60);

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m1", "a1",
            sourceAnchor: "bottom", targetAnchor: "border");

        // Source bottom edge midpoint: (100, 110)
        Assert.Contains("M 100 110", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void BuildArrowElements_DirectionalAnchors_RightToLeft()
    {
        var sourceBBox = new BoundingBox(50, 50, 100, 60);   // right edge x=150, cy=80
        var targetBBox = new BoundingBox(300, 50, 100, 60);  // left edge x=300, cy=80

        var (_, arrow) = CopilotCommandApplier.BuildArrowElements(
            sourceBBox, targetBBox, "m1", "a1",
            sourceAnchor: "right", targetAnchor: "left");

        // Source right midpoint: (150, 80), target left midpoint: (300, 80)
        Assert.Contains("M 150 80", arrow.D, StringComparison.Ordinal);
        Assert.Contains("300 80", arrow.D, StringComparison.Ordinal);
    }

    [TestMethod]
    public void ResolveAnchorPoint_AllDirections_ReturnCorrectEdgeMidpoints()
    {
        var bbox = new BoundingBox(100, 200, 80, 40);
        // center = (140, 220), left edge x=100, right edge x=180, top y=200, bottom y=240

        var (lx, ly) = CopilotCommandApplier.ResolveAnchorPoint(bbox, 140, 220, 0, 0, "left");
        Assert.AreEqual(100, lx, 0.01);
        Assert.AreEqual(220, ly, 0.01);

        var (rx, ry) = CopilotCommandApplier.ResolveAnchorPoint(bbox, 140, 220, 0, 0, "right");
        Assert.AreEqual(180, rx, 0.01);
        Assert.AreEqual(220, ry, 0.01);

        var (tx, ty) = CopilotCommandApplier.ResolveAnchorPoint(bbox, 140, 220, 0, 0, "top");
        Assert.AreEqual(140, tx, 0.01);
        Assert.AreEqual(200, ty, 0.01);

        var (bx, by) = CopilotCommandApplier.ResolveAnchorPoint(bbox, 140, 220, 0, 0, "bottom");
        Assert.AreEqual(140, bx, 0.01);
        Assert.AreEqual(240, by, 0.01);

        var (cx, cy) = CopilotCommandApplier.ResolveAnchorPoint(bbox, 140, 220, 0, 0, "center");
        Assert.AreEqual(140, cx, 0.01);
        Assert.AreEqual(220, cy, 0.01);
    }

    [TestMethod]
    public void NormalizeAnchor_ValidValues_ReturnsLowercase()
    {
        Assert.AreEqual("border", CopilotCommandApplier.NormalizeAnchor(null));
        Assert.AreEqual("border", CopilotCommandApplier.NormalizeAnchor(""));
        Assert.AreEqual("border", CopilotCommandApplier.NormalizeAnchor("border"));
        Assert.AreEqual("border", CopilotCommandApplier.NormalizeAnchor("Border"));
        Assert.AreEqual("center", CopilotCommandApplier.NormalizeAnchor("center"));
        Assert.AreEqual("center", CopilotCommandApplier.NormalizeAnchor("Center"));
        Assert.AreEqual("left", CopilotCommandApplier.NormalizeAnchor("left"));
        Assert.AreEqual("right", CopilotCommandApplier.NormalizeAnchor("Right"));
        Assert.AreEqual("top", CopilotCommandApplier.NormalizeAnchor("TOP"));
        Assert.AreEqual("bottom", CopilotCommandApplier.NormalizeAnchor("Bottom"));
        Assert.AreEqual("border", CopilotCommandApplier.NormalizeAnchor("invalid"));
    }
}
