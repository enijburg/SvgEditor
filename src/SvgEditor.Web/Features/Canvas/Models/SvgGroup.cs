namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgGroup : SvgElement
{
    public override string Tag => "g";
    public List<SvgElement> Children { get; init; } = [];

    public override SvgElement WithOffset(double dx, double dy)
    {
        var movedChildren = Children.Select(c => c.WithOffset(dx, dy)).ToList();
        return new SvgGroup
        {
            Id = Id,
            Attributes = new Dictionary<string, string>(Attributes),
            Children = movedChildren
        };
    }

    public override SvgElement DeepClone() => new SvgGroup
    {
        Id = Id,
        Attributes = new Dictionary<string, string>(Attributes),
        Children = Children.Select(c => c.DeepClone()).ToList()
    };
}
