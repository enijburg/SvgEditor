namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgPolyline : SvgElement
{
    public override string Tag => "polyline";

    public string Points
    {
        get => Attributes.GetValueOrDefault("points", string.Empty);
        init => Attributes["points"] = value;
    }

    public override SvgElement WithOffset(double dx, double dy) =>
        new SvgPolyline { Id = Id, Attributes = new Dictionary<string, string>(Attributes) { ["points"] = TranslatePoints(Points, dx, dy) } };

    public override SvgElement DeepClone() => new SvgPolyline { Id = Id, Attributes = new Dictionary<string, string>(Attributes) };

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
