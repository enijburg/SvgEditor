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

    public override BoundingBox? GetBoundingBox()
    {
        // Approximate: parse M/L/C coordinates from the d attribute for a rough bounding box
        var d = D;
        if (string.IsNullOrWhiteSpace(d)) return null;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        var found = false;

        // Extract all numbers from the path data
        int i = 0;
        while (i < d.Length)
        {
            // Skip non-numeric characters (letters, whitespace, commas)
            while (i < d.Length && !char.IsDigit(d[i]) && d[i] != '-' && d[i] != '.')
                i++;
            if (i >= d.Length) break;

            int start = i;
            if (d[i] == '-') i++;
            while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '.')) i++;
            if (i == start) { i++; continue; }

            if (!double.TryParse(d.AsSpan(start, i - start), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var x))
                continue;

            // Skip separators
            while (i < d.Length && (d[i] == ',' || d[i] == ' ')) i++;
            if (i >= d.Length) break;

            int start2 = i;
            if (i < d.Length && d[i] == '-') i++;
            while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '.')) i++;
            if (i == start2) continue;

            if (!double.TryParse(d.AsSpan(start2, i - start2), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var y))
                continue;

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            found = true;
        }

        if (!found) return null;

        var (tx, ty) = ParseTranslation();
        return new BoundingBox(minX + tx, minY + ty, maxX - minX, maxY - minY);
    }

    private (double Tx, double Ty) ParseTranslation()
    {
        if (!Attributes.TryGetValue("transform", out var transform) ||
            !transform.StartsWith("translate(", StringComparison.Ordinal))
            return (0, 0);

        var inner = transform["translate(".Length..^1];
        var parts = inner.Split(',');
        var tx = double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v1) ? v1 : 0;
        var ty = parts.Length > 1 && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v2) ? v2 : 0;
        return (tx, ty);
    }

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
