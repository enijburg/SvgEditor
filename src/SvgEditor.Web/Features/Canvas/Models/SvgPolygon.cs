namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgPolygon : SvgElement
{
    public override string Tag => "polygon";

    public string Points
    {
        get => Attributes.GetValueOrDefault("points", string.Empty);
        init => Attributes["points"] = value;
    }

    public override SvgElement WithOffset(double dx, double dy) =>
        new SvgPolygon { Id = Id, Attributes = new Dictionary<string, string>(Attributes) { ["points"] = TranslatePoints(Points, dx, dy) } };

    public override SvgElement DeepClone() => new SvgPolygon { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };

    public override BoundingBox? GetBoundingBox() => ComputePointsBoundingBox(Points);

    internal static BoundingBox? ComputePointsBoundingBox(string points)
    {
        var pairs = points.Trim().Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (pairs.Length < 2) return null;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            if (double.TryParse(pairs[i], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(pairs[i + 1], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var y))
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return minX > maxX ? null : new BoundingBox(minX, minY, maxX - minX, maxY - minY);
    }

    private static string TranslatePoints(string points, double dx, double dy)
    {
        var pairs = points.Trim().Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Text.StringBuilder();
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            if (i > 0) result.Append(' ');
            if (double.TryParse(pairs[i], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(pairs[i + 1], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var y))
            {
                result.Append(FormatDouble(x + dx));
                result.Append(',');
                result.Append(FormatDouble(y + dy));
            }
        }
        return result.ToString();
    }
}
