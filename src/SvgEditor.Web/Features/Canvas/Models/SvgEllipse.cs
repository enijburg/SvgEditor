namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgEllipse : SvgElement
{
    public override string Tag => "ellipse";

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

    public double Rx
    {
        get => ParseDouble(Attributes.GetValueOrDefault("rx"));
        init => Attributes["rx"] = FormatDouble(value);
    }

    public double Ry
    {
        get => ParseDouble(Attributes.GetValueOrDefault("ry"));
        init => Attributes["ry"] = FormatDouble(value);
    }

    public override SvgElement WithOffset(double dx, double dy)
    {
        var attrs = new Dictionary<string, string>(Attributes)
        {
            ["cx"] = FormatDouble(Cx + dx),
            ["cy"] = FormatDouble(Cy + dy)
        };
        return new SvgEllipse { Id = Id, Attributes = attrs };
    }

    public override SvgElement DeepClone() => new SvgEllipse { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };

    public override BoundingBox? GetBoundingBox() => new BoundingBox(Cx - Rx, Cy - Ry, Rx * 2, Ry * 2);
}
