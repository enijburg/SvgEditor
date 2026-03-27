namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgPath : SvgElement
{
    public override string Tag => "path";

    public string D
    {
        get => Attributes.GetValueOrDefault("d", string.Empty);
        init => Attributes["d"] = value;
    }

    public override SvgElement WithOffset(double dx, double dy)
    {
        // For MVP: apply transform attribute to move paths
        var attrs = new Dictionary<string, string>(Attributes);
        ApplyTranslation(attrs, dx, dy);
        return new SvgPath { Id = Id, Attributes = attrs };
    }

    public override SvgElement DeepClone() => new SvgPath { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };

    internal static void ApplyTranslation(Dictionary<string, string> attrs, double dx, double dy)
    {
        if (attrs.TryGetValue("transform", out var existing) && existing.StartsWith("translate(", StringComparison.Ordinal))
        {
            var inner = existing["translate(".Length..^1];
            var parts = inner.Split(',');
            var tx = double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v1) ? v1 : 0;
            var ty = parts.Length > 1 && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v2) ? v2 : 0;
            attrs["transform"] = $"translate({SvgElement.FormatDoubleStatic(tx + dx)},{SvgElement.FormatDoubleStatic(ty + dy)})";
        }
        else
        {
            attrs["transform"] = $"translate({SvgElement.FormatDoubleStatic(dx)},{SvgElement.FormatDoubleStatic(dy)})";
        }
    }
}
