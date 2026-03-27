namespace SvgEditor.Web.Features.Canvas.Models;

public abstract class SvgElement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public abstract string Tag { get; }
    public Dictionary<string, string> Attributes { get; init; } = [];

    public abstract SvgElement WithOffset(double dx, double dy);
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
}
