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
