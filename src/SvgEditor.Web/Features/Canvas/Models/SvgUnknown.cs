namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgUnknown : SvgElement
{
    private readonly string _tag;
    public override string Tag => _tag;

    public SvgUnknown(string tag)
    {
        _tag = tag;
    }

    public override SvgElement WithOffset(double dx, double dy)
    {
        var attrs = new Dictionary<string, string>(Attributes);
        SvgPath.ApplyTranslation(attrs, dx, dy);
        return new SvgUnknown(_tag) { Id = Id, Attributes = attrs };
    }

    public override SvgElement DeepClone() => new SvgUnknown(_tag) { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };
}
