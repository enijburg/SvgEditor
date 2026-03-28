namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgLine : SvgElement
{
    public override string Tag => "line";

    public double X1
    {
        get => ParseDouble(Attributes.GetValueOrDefault("x1"));
        init => Attributes["x1"] = FormatDouble(value);
    }

    public double Y1
    {
        get => ParseDouble(Attributes.GetValueOrDefault("y1"));
        init => Attributes["y1"] = FormatDouble(value);
    }

    public double X2
    {
        get => ParseDouble(Attributes.GetValueOrDefault("x2"));
        init => Attributes["x2"] = FormatDouble(value);
    }

    public double Y2
    {
        get => ParseDouble(Attributes.GetValueOrDefault("y2"));
        init => Attributes["y2"] = FormatDouble(value);
    }

    public override SvgElement WithOffset(double dx, double dy)
    {
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["x1"] = FormatDouble(X1 + dx),
            ["y1"] = FormatDouble(Y1 + dy),
            ["x2"] = FormatDouble(X2 + dx),
            ["y2"] = FormatDouble(Y2 + dy)
        };
        return new SvgLine { Id = Id, Attributes = attrs };
    }

    public override SvgElement WithResize(BoundingBox original, BoundingBox updated)
    {
        var (nx1, ny1) = MapPoint(X1, Y1, original, updated);
        var (nx2, ny2) = MapPoint(X2, Y2, original, updated);
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["x1"] = FormatDouble(nx1),
            ["y1"] = FormatDouble(ny1),
            ["x2"] = FormatDouble(nx2),
            ["y2"] = FormatDouble(ny2)
        };
        return new SvgLine { Id = Id, Attributes = attrs };
    }

    public override SvgElement DeepClone() => new SvgLine { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };

    public override BoundingBox? GetBoundingBox()
    {
        var minX = Math.Min(X1, X2);
        var minY = Math.Min(Y1, Y2);
        var maxX = Math.Max(X1, X2);
        var maxY = Math.Max(Y1, Y2);
        return new BoundingBox(minX, minY, maxX - minX, maxY - minY);
    }
}
