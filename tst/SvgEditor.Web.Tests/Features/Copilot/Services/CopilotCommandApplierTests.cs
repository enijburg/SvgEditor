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
}
