using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

public sealed record EditorContext
{
    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("documentVersion")]
    public required string DocumentVersion { get; init; }

    [JsonPropertyName("canvas")]
    public required CanvasSize Canvas { get; init; }

    [JsonPropertyName("selection")]
    public required IReadOnlyList<string> Selection { get; init; }

    [JsonPropertyName("elements")]
    public required IReadOnlyList<ElementSummary> Elements { get; init; }
}

public sealed record CanvasSize
{
    [JsonPropertyName("width")]
    public required double Width { get; init; }

    [JsonPropertyName("height")]
    public required double Height { get; init; }
}

public sealed record ElementSummary
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
