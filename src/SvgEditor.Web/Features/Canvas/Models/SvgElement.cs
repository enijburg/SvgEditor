namespace SvgEditor.Web.Features.Canvas.Models;

public abstract class SvgElement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public abstract string Tag { get; }
    public Dictionary<string, string> Attributes { get; init; } = [];

    public abstract SvgElement WithOffset(double dx, double dy);
    public abstract SvgElement WithResize(BoundingBox original, BoundingBox updated);
    public abstract SvgElement DeepClone();
    public abstract BoundingBox? GetBoundingBox();

    /// <summary>
    /// Returns the attribute name that represents the visible foreground color.
    /// For stroke-based elements (lines, polylines, and elements with fill="none"),
    /// the foreground is "stroke". For filled shapes, it is "fill".
    /// </summary>
    public string GetForegroundColorAttribute()
    {
        // Lines have no fill area — their foreground is always stroke
        if (this is SvgLine)
            return "stroke";

        // For other elements, if fill is explicitly "none" and a stroke is present,
        // the foreground color is the stroke
        var fill = Attributes.GetValueOrDefault("fill");
        if (string.Equals(fill, "none", StringComparison.OrdinalIgnoreCase) && Attributes.ContainsKey("stroke"))
            return "stroke";

        return "fill";
    }

    protected static double ParseDouble(string? value, double defaultValue = 0) =>
        double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;

    protected static string FormatDouble(double value) => FormatDoubleStatic(value);

    internal static string FormatDoubleStatic(double value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    protected static (double X, double Y) MapPoint(double x, double y, BoundingBox original, BoundingBox updated)
    {
        var sx = original.Width > 0 ? updated.Width / original.Width : 1;
        var sy = original.Height > 0 ? updated.Height / original.Height : 1;
        return (updated.X + (x - original.X) * sx, updated.Y + (y - original.Y) * sy);
    }

    protected static (double X, double Y, double Width, double Height) MapRect(
        double x, double y, double w, double h, BoundingBox original, BoundingBox updated)
    {
        var (nx, ny) = MapPoint(x, y, original, updated);
        var sx = original.Width > 0 ? updated.Width / original.Width : 1;
        var sy = original.Height > 0 ? updated.Height / original.Height : 1;
        return (nx, ny, Math.Abs(w * sx), Math.Abs(h * sy));
    }

    internal static bool TryExtractPathEndpoints(string pathData, out double startX, out double startY, out double endX, out double endY)
    {
        startX = startY = endX = endY = 0;
        if (string.IsNullOrWhiteSpace(pathData)) return false;

        // Read first coordinate pair after an initial 'M'
        var i = 0;
        while (i < pathData.Length && pathData[i] != 'M' && pathData[i] != 'm') i++;
        if (i >= pathData.Length) return false;
        i++; // skip 'M'
        if (!TryReadCoordPair(pathData, ref i, out startX, out startY)) return false;

        // Find last numeric pair in the string by scanning from the end
        var j = pathData.Length - 1;
        // Move backwards to find the last digit or '-' or '.'
        while (j >= 0 && !(char.IsDigit(pathData[j]) || pathData[j] == '-' || pathData[j] == '.')) j--;
        if (j < 0) return false;

        // Walk backwards to find the start of the last number
        var yEndEnd = j + 1;
        var yStart = j;
        while (yStart >= 0 && (char.IsDigit(pathData[yStart]) || pathData[yStart] == '.' || pathData[yStart] == '-')) yStart--;
        yStart++;

        // Then find the x start before separators
        var xEnd = yStart - 1;
        while (xEnd >= 0 && (pathData[xEnd] == ' ' || pathData[xEnd] == ',')) xEnd--;
        if (xEnd < 0) return false;
        var xStart = xEnd;
        while (xStart >= 0 && (char.IsDigit(pathData[xStart]) || pathData[xStart] == '.' || pathData[xStart] == '-')) xStart--;
        xStart++;

        if (xStart < 0 || yStart >= yEndEnd) return false;

        if (!double.TryParse(pathData.AsSpan(xStart, xEnd - xStart + 1), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out endX))
            return false;
        if (!double.TryParse(pathData.AsSpan(yStart, yEndEnd - yStart), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out endY))
            return false;

        return true;
    }

    private static void SkipWhitespaceAndCommas(string s, ref int i)
    {
        while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++;
    }

    private static bool TryReadNumber(string s, ref int i, out double value)
    {
        value = 0;
        SkipWhitespaceAndCommas(s, ref i);
        if (i >= s.Length) return false;
        var start = i;
        if (i < s.Length && (s[i] == '-' || s[i] == '+')) i++;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
        if (i == start) return false;
        return double.TryParse(s.AsSpan(start, i - start), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadCoordPair(string s, ref int i, out double x, out double y)
    {
        x = 0; y = 0;
        if (!TryReadNumber(s, ref i, out x)) return false;
        if (!TryReadNumber(s, ref i, out y)) return false;
        return true;
    }

    private static void ExpandBounds(double x, double y, ref double minX, ref double minY, ref double maxX, ref double maxY)
    {
        minX = Math.Min(minX, x);
        minY = Math.Min(minY, y);
        maxX = Math.Max(maxX, x);
        maxY = Math.Max(maxY, y);
    }

    private static void ExpandWithQuadBezierBounds(double x0, double y0, double x1, double y1, double x2, double y2,
        ref double minX, ref double minY, ref double maxX, ref double maxY)
    {
        ExpandBounds(x0, y0, ref minX, ref minY, ref maxX, ref maxY);
        ExpandBounds(x2, y2, ref minX, ref minY, ref maxX, ref maxY);
        var dx = x0 - 2 * x1 + x2;
        if (Math.Abs(dx) > 1e-10)
        {
            var t = (x0 - x1) / dx;
            if (t > 0 && t < 1)
            {
                var xt = (1 - t) * (1 - t) * x0 + 2 * (1 - t) * t * x1 + t * t * x2;
                minX = Math.Min(minX, xt);
                maxX = Math.Max(maxX, xt);
            }
        }
        var dy = y0 - 2 * y1 + y2;
        if (Math.Abs(dy) > 1e-10)
        {
            var t = (y0 - y1) / dy;
            if (t > 0 && t < 1)
            {
                var yt = (1 - t) * (1 - t) * y0 + 2 * (1 - t) * t * y1 + t * t * y2;
                minY = Math.Min(minY, yt);
                maxY = Math.Max(maxY, yt);
            }
        }
    }
}
