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

    public override SvgElement DeepClone() => new SvgLine { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };
}
