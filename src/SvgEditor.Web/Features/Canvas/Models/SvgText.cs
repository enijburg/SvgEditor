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

    public override SvgElement DeepClone() => new SvgText { Id = Id, Attributes = new Dictionary<string, string>(Attributes), Content = Content };

    public override BoundingBox? GetBoundingBox() => new BoundingBox(X, Y - 16, Content.Length * 8, 16);
}
