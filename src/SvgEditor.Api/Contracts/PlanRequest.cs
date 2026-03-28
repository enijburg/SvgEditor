using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

public sealed record PlanRequest
{
    [JsonPropertyName("prompt")]
    public required string Prompt { get; init; }

    [JsonPropertyName("context")]
    public required EditorContext Context { get; init; }
}
