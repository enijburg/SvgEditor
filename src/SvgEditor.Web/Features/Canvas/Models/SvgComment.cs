namespace SvgEditor.Web.Features.Canvas.Models;

/// <summary>
/// Represents an XML comment node preserved from an imported SVG document.
/// Comments are not rendered visually but are maintained through the document lifecycle
/// and re-emitted during export to preserve the original SVG structure.
/// </summary>
public sealed class SvgComment : SvgElement
{
    public override string Tag => "#comment";

    public string Text { get; init; } = "";

    public override SvgElement WithOffset(double dx, double dy) => this;
    public override SvgElement WithResize(BoundingBox original, BoundingBox updated) => this;
    public override SvgElement DeepClone() => new SvgComment { Id = Id, Text = Text };
    public override BoundingBox? GetBoundingBox() => null;
}
