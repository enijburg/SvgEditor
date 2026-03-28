using System.Text.RegularExpressions;
using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Services;

public sealed partial class CommandValidationService
{
    private const double MaxCoordinateValue = 100000;

    private static readonly HashSet<string> ValidAlignments = new(StringComparer.OrdinalIgnoreCase)
    {
        "left", "center", "right", "top", "middle", "bottom"
    };

    private static readonly HashSet<string> ValidAnchors = new(StringComparer.OrdinalIgnoreCase)
    {
        "border", "center", "left", "right", "top", "bottom"
    };

    [GeneratedRegex(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex HexColorPattern();

    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9_\-]*$")]
    private static partial Regex SafeIdPattern();

    private static readonly HashSet<string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "white", "red", "green", "blue", "yellow", "cyan", "magenta",
        "orange", "purple", "pink", "brown", "gray", "grey", "navy", "teal",
        "lime", "aqua", "maroon", "olive", "silver", "fuchsia", "transparent", "none"
    };

    public ValidationInfo Validate(IReadOnlyList<SvgCommand> commands, EditorContext context)
    {
        var issues = new List<string>();

        if (commands.Count == 0)
        {
            issues.Add("No commands provided.");
            return new ValidationInfo { IsValid = false, Issues = issues };
        }

        var knownElementIds = new HashSet<string>(context.Elements.Select(e => e.Id));

        foreach (var command in commands)
        {
            ValidateCommand(command, knownElementIds, issues);
        }

        return new ValidationInfo { IsValid = issues.Count == 0, Issues = issues };
    }

    private static void ValidateCommand(SvgCommand command, HashSet<string> knownElementIds, List<string> issues)
    {
        switch (command)
        {
            case SetFillCommand setFill:
                ValidateElementId(setFill.ElementId, knownElementIds, issues);
                ValidateColor(setFill.Fill, "Fill", issues);
                break;

            case SetStrokeCommand setStroke:
                ValidateElementId(setStroke.ElementId, knownElementIds, issues);
                ValidateColor(setStroke.Stroke, "Stroke", issues);
                if (setStroke.Width < 0)
                    issues.Add("Stroke width must not be negative.");
                break;

            case MoveElementCommand moveElement:
                ValidateElementId(moveElement.ElementId, knownElementIds, issues);
                ValidateCoordinate(moveElement.Dx, "Dx", issues);
                ValidateCoordinate(moveElement.Dy, "Dy", issues);
                break;

            case MoveSelectionCommand moveSelection:
                ValidateCoordinate(moveSelection.Dx, "Dx", issues);
                ValidateCoordinate(moveSelection.Dy, "Dy", issues);
                break;

            case AlignSelectionCommand alignSelection:
                if (!ValidAlignments.Contains(alignSelection.Alignment))
                    issues.Add($"Invalid alignment '{alignSelection.Alignment}'. Valid values: {string.Join(", ", ValidAlignments)}.");
                break;

            case AddArrowBetweenSelectionCommand addArrow:
                ValidateElementId(addArrow.SourceElementId, knownElementIds, issues);
                ValidateElementId(addArrow.TargetElementId, knownElementIds, issues);
                if (!string.IsNullOrWhiteSpace(addArrow.SourceElementId) &&
                    !string.IsNullOrWhiteSpace(addArrow.TargetElementId) &&
                    string.Equals(addArrow.SourceElementId, addArrow.TargetElementId, StringComparison.Ordinal))
                {
                    issues.Add("Source and target element must be different.");
                }

                if (addArrow.Stroke is not null)
                    ValidateColor(addArrow.Stroke, "Stroke", issues);
                if (addArrow.StrokeWidth is < 0)
                    issues.Add("Stroke width must not be negative.");
                if (addArrow.StrokeDashArray is not null)
                    ValidateStrokeDashArray(addArrow.StrokeDashArray, issues);
                if (addArrow.SourceAnchor is not null && !ValidAnchors.Contains(addArrow.SourceAnchor))
                    issues.Add($"Invalid sourceAnchor '{addArrow.SourceAnchor}'. Valid values: {string.Join(", ", ValidAnchors)}.");
                if (addArrow.TargetAnchor is not null && !ValidAnchors.Contains(addArrow.TargetAnchor))
                    issues.Add($"Invalid targetAnchor '{addArrow.TargetAnchor}'. Valid values: {string.Join(", ", ValidAnchors)}.");

                break;

            default:
                issues.Add($"Unknown command type: {command.GetType().Name}");
                break;
        }
    }

    private static void ValidateElementId(string elementId, HashSet<string> knownElementIds, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            issues.Add("Element ID must not be empty.");
            return;
        }

        if (!SafeIdPattern().IsMatch(elementId) && !Guid.TryParse(elementId, out _))
        {
            issues.Add($"Element ID '{elementId}' contains invalid characters.");
            return;
        }

        if (!knownElementIds.Contains(elementId))
        {
            issues.Add($"Element '{elementId}' not found in the document.");
        }
    }

    private static void ValidateColor(string color, string fieldName, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            issues.Add($"{fieldName} color must not be empty.");
            return;
        }

        if (!HexColorPattern().IsMatch(color) && !NamedColors.Contains(color))
        {
            issues.Add($"Invalid {fieldName.ToLowerInvariant()} color value '{color}'.");
        }
    }

    private static void ValidateCoordinate(double value, string fieldName, List<string> issues)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            issues.Add($"{fieldName} must be a finite number.");
        }

        if (Math.Abs(value) > MaxCoordinateValue)
        {
            issues.Add($"{fieldName} value {value} is out of reasonable range.");
        }
    }

    [GeneratedRegex(@"^\d+(\.\d+)?(\s*[,\s]\s*\d+(\.\d+)?)*$")]
    private static partial Regex StrokeDashArrayPattern();

    private static void ValidateStrokeDashArray(string value, List<string> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add("stroke-dasharray must not be empty.");
            return;
        }

        if (!StrokeDashArrayPattern().IsMatch(value))
        {
            issues.Add($"Invalid stroke-dasharray value '{value}'. Expected space or comma-separated numbers (e.g. '8 4').");
        }
    }
}
