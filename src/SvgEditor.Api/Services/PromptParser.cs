using System.Globalization;
using System.Text.RegularExpressions;
using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Services;

public sealed partial class PromptParser
{
    private const double DefaultStrokeWidth = 1;

    private static readonly Dictionary<string, string> ColorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["red"] = "#FF0000",
        ["blue"] = "#0000FF",
        ["green"] = "#008000",
        ["yellow"] = "#FFFF00",
        ["black"] = "#000000",
        ["white"] = "#FFFFFF",
        ["orange"] = "#FFA500",
        ["purple"] = "#800080",
        ["pink"] = "#FFC0CB",
        ["cyan"] = "#00FFFF",
        ["magenta"] = "#FF00FF",
        ["gray"] = "#808080",
        ["grey"] = "#808080",
        ["brown"] = "#A52A2A",
        ["navy"] = "#000080",
        ["teal"] = "#008080",
    };

    [GeneratedRegex(@"#([0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3})")]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(@"(-?\d+(?:\.\d+)?)\s*(?:pixels?|px)\s+(?:to\s+the\s+)?(left|right|up|down)", RegexOptions.IgnoreCase)]
    private static partial Regex MovePatternRegex();

    [GeneratedRegex(@"(?:move|shift)\s+.*?(?:to\s+the\s+)?(left|right|up|down)\s+(?:by\s+)?(-?\d+(?:\.\d+)?)\s*(?:pixels?|px)?", RegexOptions.IgnoreCase)]
    private static partial Regex MoveAltPatternRegex();

    [GeneratedRegex(@"(?:align|center)\s+.*?(left|right|top|bottom|center|middle|horizontally|vertically)", RegexOptions.IgnoreCase)]
    private static partial Regex AlignPatternRegex();

    public PlanResponse Parse(PlanRequest request)
    {
        var prompt = request.Prompt.Trim();
        var context = request.Context;
        var commands = new List<SvgCommand>();
        var summary = "";

        // Try stroke (before fill, since stroke keywords are more specific)
        if (TryParseStrokeCommand(prompt, context, out var strokeCommands, out var strokeSummary))
        {
            commands.AddRange(strokeCommands);
            summary = strokeSummary;
        }
        // Try fill color change
        else if (TryParseFillCommand(prompt, context, out var fillCommands, out var fillSummary))
        {
            commands.AddRange(fillCommands);
            summary = fillSummary;
        }
        // Try move
        else if (TryParseMoveCommand(prompt, context, out var moveCommands, out var moveSummary))
        {
            commands.AddRange(moveCommands);
            summary = moveSummary;
        }
        // Try align
        else if (TryParseAlignCommand(prompt, context, out var alignCommands, out var alignSummary))
        {
            commands.AddRange(alignCommands);
            summary = alignSummary;
        }
        else
        {
            return new PlanResponse
            {
                Summary = "Could not understand the request.",
                Commands = [],
                Validation = new ValidationInfo
                {
                    IsValid = false,
                    Issues = [$"Unable to parse prompt: '{prompt}'"]
                }
            };
        }

        var validation = new CommandValidationService().Validate(commands, context);
        return new PlanResponse
        {
            Summary = summary,
            Commands = commands,
            Validation = validation
        };
    }

    private static bool TryParseFillCommand(string prompt, EditorContext context,
        out List<SvgCommand> commands, out string summary)
    {
        commands = [];
        summary = "";

        var lowerPrompt = prompt.ToLowerInvariant();
        if (!lowerPrompt.Contains("color") && !lowerPrompt.Contains("fill") &&
            !lowerPrompt.Contains("make") && !lowerPrompt.Contains("change") &&
            !lowerPrompt.Contains("set") && !lowerPrompt.Contains("paint"))
            return false;

        var color = ExtractColor(prompt);
        if (color is null)
            return false;

        if (context.Selection.Count == 0)
            return false;

        foreach (var elementId in context.Selection)
        {
            commands.Add(new SetFillCommand { ElementId = elementId, Fill = color });
        }

        summary = context.Selection.Count == 1
            ? $"Change fill color of {context.Selection[0]} to {color}."
            : $"Change fill color of {context.Selection.Count} elements to {color}.";

        return true;
    }

    private static bool TryParseMoveCommand(string prompt, EditorContext context,
        out List<SvgCommand> commands, out string summary)
    {
        commands = [];
        summary = "";

        var match = MovePatternRegex().Match(prompt);
        if (!match.Success)
            match = MoveAltPatternRegex().Match(prompt);

        if (!match.Success)
            return false;

        double amount;
        string direction;

        if (match.Groups.Count >= 3 && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val1))
        {
            amount = val1;
            direction = match.Groups[2].Value.ToLowerInvariant();
        }
        else if (match.Groups.Count >= 3 && double.TryParse(match.Groups[2].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val2))
        {
            direction = match.Groups[1].Value.ToLowerInvariant();
            amount = val2;
        }
        else
        {
            return false;
        }

        var (dx, dy) = direction switch
        {
            "left" => (-amount, 0.0),
            "right" => (amount, 0.0),
            "up" => (0.0, -amount),
            "down" => (0.0, amount),
            _ => (0.0, 0.0)
        };

        if (dx == 0 && dy == 0)
            return false;

        if (context.Selection.Count > 0)
        {
            commands.Add(new MoveSelectionCommand { Dx = dx, Dy = dy });
            summary = $"Move selection {amount}px {direction}.";
        }

        return commands.Count > 0;
    }

    private static bool TryParseAlignCommand(string prompt, EditorContext context,
        out List<SvgCommand> commands, out string summary)
    {
        commands = [];
        summary = "";

        var match = AlignPatternRegex().Match(prompt);
        if (!match.Success)
            return false;

        var rawAlignment = match.Groups[1].Value.ToLowerInvariant();
        var alignment = rawAlignment switch
        {
            "horizontally" => "center",
            "vertically" => "middle",
            _ => rawAlignment
        };

        if (context.Selection.Count == 0)
            return false;

        commands.Add(new AlignSelectionCommand { Alignment = alignment });
        summary = $"Align selection {alignment}.";
        return true;
    }

    private static bool TryParseStrokeCommand(string prompt, EditorContext context,
        out List<SvgCommand> commands, out string summary)
    {
        commands = [];
        summary = "";

        var lowerPrompt = prompt.ToLowerInvariant();
        if (!lowerPrompt.Contains("stroke") && !lowerPrompt.Contains("border") && !lowerPrompt.Contains("outline"))
            return false;

        var color = ExtractColor(prompt);
        if (color is null)
            return false;

        if (context.Selection.Count == 0)
            return false;

        foreach (var elementId in context.Selection)
        {
            commands.Add(new SetStrokeCommand { ElementId = elementId, Stroke = color, Width = DefaultStrokeWidth });
        }

        summary = context.Selection.Count == 1
            ? $"Change stroke of {context.Selection[0]} to {color}."
            : $"Change stroke of {context.Selection.Count} elements to {color}.";

        return true;
    }

    private static string? ExtractColor(string prompt)
    {
        var hexMatch = HexColorRegex().Match(prompt);
        if (hexMatch.Success)
            return hexMatch.Value;

        foreach (var (name, hex) in ColorNames)
        {
            if (prompt.Contains(name, StringComparison.OrdinalIgnoreCase))
                return hex;
        }

        return null;
    }
}
