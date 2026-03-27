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

    public override SvgElement DeepClone() => new SvgCircle { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };
}
