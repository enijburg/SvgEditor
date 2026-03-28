namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgUnknown(string tag) : SvgElement
{
    public override string Tag => tag;

    /// <summary>
    /// Raw inner XML content preserved from import (e.g., children of &lt;defs&gt;).
    /// </summary>
    public string InnerXml { get; set; } = "";

    public override SvgElement WithOffset(double dx, double dy)
    {
        var attrs = new Dictionary<string, string>(Attributes);
        SvgPath.ApplyTranslation(attrs, dx, dy);
        return new SvgUnknown(tag) { Id = Id, Attributes = attrs, InnerXml = InnerXml };
    }

    public override SvgElement WithResize(BoundingBox original, BoundingBox updated) => this;

    public override SvgElement DeepClone() => new SvgUnknown(tag)
    {
        Id = Id,
        Attributes = new Dictionary<string, string>(Attributes),
        InnerXml = InnerXml
    };

    public override BoundingBox? GetBoundingBox() => null;
}
