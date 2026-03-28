using System.Text.Json.Serialization;

namespace SvgEditor.Web.Features.Copilot.Models;

public sealed record CopilotEditorContext
{
    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("documentVersion")]
    public required string DocumentVersion { get; init; }

    [JsonPropertyName("canvas")]
    public required CopilotCanvasSize Canvas { get; init; }

    [JsonPropertyName("selection")]
    public required IReadOnlyList<string> Selection { get; init; }

    [JsonPropertyName("elements")]
    public required IReadOnlyList<CopilotElementSummary> Elements { get; init; }
}

public sealed record CopilotCanvasSize
{
    [JsonPropertyName("width")]
    public required double Width { get; init; }

    [JsonPropertyName("height")]
    public required double Height { get; init; }
}

public sealed record CopilotElementSummary
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("x")]
    public double? X { get; init; }

    [JsonPropertyName("y")]
    public double? Y { get; init; }

    [JsonPropertyName("width")]
    public double? Width { get; init; }

    [JsonPropertyName("height")]
    public double? Height { get; init; }

    [JsonPropertyName("fill")]
    public string? Fill { get; init; }

    [JsonPropertyName("stroke")]
    public string? Stroke { get; init; }
}

public sealed record CopilotPlanRequest
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("context")]
    public required CopilotEditorContext Context { get; init; }
}

public sealed record CopilotPlanResponse
{
    [JsonPropertyName("summary")]
    public string Summary { get; init; } = "";

    [JsonPropertyName("commands")]
    public IReadOnlyList<CopilotCommand> Commands { get; init; } = [];

    [JsonPropertyName("validation")]
    public CopilotValidationInfo Validation { get; init; } = new();
}

public sealed record CopilotValidationInfo
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("issues")]
    public IReadOnlyList<string> Issues { get; init; } = [];
}

public sealed record CopilotCommand
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    [JsonPropertyName("elementId")]
    public string? ElementId { get; init; }

    [JsonPropertyName("fill")]
    public string? Fill { get; init; }

    [JsonPropertyName("stroke")]
    public string? Stroke { get; init; }

    [JsonPropertyName("width")]
    public double? Width { get; init; }

    [JsonPropertyName("dx")]
    public double? Dx { get; init; }

    [JsonPropertyName("dy")]
    public double? Dy { get; init; }

    [JsonPropertyName("alignment")]
    public string? Alignment { get; init; }

    [JsonPropertyName("sourceElementId")]
    public string? SourceElementId { get; init; }

    [JsonPropertyName("targetElementId")]
    public string? TargetElementId { get; init; }

    [JsonPropertyName("strokeDashArray")]
    public string? StrokeDashArray { get; init; }

    [JsonPropertyName("sourceAnchor")]
    public string? SourceAnchor { get; init; }

    [JsonPropertyName("targetAnchor")]
    public string? TargetAnchor { get; init; }
}

public sealed record CopilotApplyRequest
{
    [JsonPropertyName("commands")]
    public required IReadOnlyList<CopilotCommand> Commands { get; init; }

    [JsonPropertyName("documentVersion")]
    public required string DocumentVersion { get; init; }

    [JsonPropertyName("context")]
    public required CopilotEditorContext Context { get; init; }
}

public sealed record CopilotApplyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = "";

    [JsonPropertyName("appliedCommands")]
    public int AppliedCommands { get; init; }
}
