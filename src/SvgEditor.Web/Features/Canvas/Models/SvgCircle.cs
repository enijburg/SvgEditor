namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgCircle : SvgElement
{
    public override string Tag => "circle";

    public double Cx
    {
        get => ParseDouble(Attributes.GetValueOrDefault("cx"));
        init => Attributes["cx"] = FormatDouble(value);
    }

    public double Cy
    {
        get => ParseDouble(Attributes.GetValueOrDefault("cy"));
        init => Attributes["cy"] = FormatDouble(value);
    }

    public double R
    {
        get => ParseDouble(Attributes.GetValueOrDefault("r"));
        init => Attributes["r"] = FormatDouble(value);
    }

    public override SvgElement WithOffset(double dx, double dy)
    {
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["cx"] = FormatDouble(Cx + dx),
            ["cy"] = FormatDouble(Cy + dy)
        };
        return new SvgCircle { Id = Id, Attributes = attrs };
    }

    public override SvgElement WithResize(BoundingBox original, BoundingBox updated)
    {
        var (nx, ny, nw, nh) = MapRect(Cx - R, Cy - R, R * 2, R * 2, original, updated);
        var nr = Math.Min(nw, nh) / 2;
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["cx"] = FormatDouble(nx + nw / 2),
            ["cy"] = FormatDouble(ny + nh / 2),
            ["r"] = FormatDouble(nr)
        };
        return new SvgCircle { Id = Id, Attributes = attrs };
    }

    public override SvgElement DeepClone() => new SvgCircle { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };

    public override BoundingBox? GetBoundingBox() => new BoundingBox(Cx - R, Cy - R, R * 2, R * 2);
}
