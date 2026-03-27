namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgRect : SvgElement
{
    public override string Tag => "rect";

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

    public double Width
    {
        get => ParseDouble(Attributes.GetValueOrDefault("width"));
        init => Attributes["width"] = FormatDouble(value);
    }

    public double Height
    {
        get => ParseDouble(Attributes.GetValueOrDefault("height"));
        init => Attributes["height"] = FormatDouble(value);
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
            ["x"] = FormatDouble(X + dx),
            ["y"] = FormatDouble(Y + dy)
        };
        return new SvgRect { Id = Id, Attributes = attrs };
    }

    public override SvgElement DeepClone() => new SvgRect { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };
}
