namespace SvgEditor.Web.Features.Canvas.Models;

public abstract class SvgElement
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public abstract string Tag { get; }
    public Dictionary<string, string> Attributes { get; init; } = [];

    public abstract SvgElement WithOffset(double dx, double dy);
    public abstract SvgElement DeepClone();

    protected static double ParseDouble(string? value, double defaultValue = 0) =>
        double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : defaultValue;

    protected static string FormatDouble(double value) => FormatDoubleStatic(value);

    internal static string FormatDoubleStatic(double value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
