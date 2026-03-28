namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgText : SvgElement
{
    public override string Tag => "text";

    public double X
    {
        get => ParseDouble(Attributes.GetValueOrDefault("x"));
        init => Attributes["x"] = FormatDouble(value);
    }

    public double Y
    {
        get => ParseDouble(Attributes.GetValueOrDefault("y"));
        init => Attributes["y"] = FormatDouble(value);
    }

    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// The ID of a <see cref="SvgLine"/> element whose geometry this text follows via its
    /// <c>path</c> attribute.  Backed by the <c>data-line-id</c> SVG attribute.
    /// When set, <see cref="WithResize"/> leaves the text's coordinates unchanged because
    /// the <c>path</c> attribute (updated by <see cref="ResizeElement.ResizeElementHandler"/>)
    /// determines the text layout position.
    /// </summary>
    public string? LinkedLineId => Attributes.GetValueOrDefault("data-line-id");

    public override SvgElement WithOffset(double dx, double dy)
    {
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["x"] = FormatDouble(X + dx),
            ["y"] = FormatDouble(Y + dy)
        };
        return new SvgText { Id = Id, Attributes = attrs, Content = Content };
    }

    public override SvgElement WithResize(BoundingBox original, BoundingBox updated)
    {
        // When the text is path-linked to a line, its layout position is controlled by the
        // SVG 'path' attribute which the ResizeElementHandler keeps in sync with the line.
        // Applying a coordinate-space remap here would fight that positioning, so we return
        // the element unchanged and let the handler do the right thing.
        if (LinkedLineId is not null)
            return this;

        var (nx, ny) = MapPoint(X, Y, original, updated);
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["x"] = FormatDouble(nx),
            ["y"] = FormatDouble(ny)
        };
        var fontSize = ParseDouble(Attributes.GetValueOrDefault("font-size"), 16);
        var sy = original.Height > 0 ? updated.Height / original.Height : 1;
        attrs["font-size"] = FormatDouble(fontSize * sy);
        return new SvgText { Id = Id, Attributes = attrs, Content = Content };
    }

    public override SvgElement DeepClone() => new SvgText { Id = Id, Attributes = new Dictionary<string, string>(Attributes), Content = Content };

    // Approximate bounding box: 8px per character width, 16px line height (rough estimate for default font size)
    public override BoundingBox? GetBoundingBox() => new BoundingBox(X, Y - 16, Content.Length * 8, 16);
}
