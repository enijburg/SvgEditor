using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

public sealed record ApplyRequest
{
    [JsonPropertyName("commands")]
    public required IReadOnlyList<SvgCommand> Commands { get; init; }

    [JsonPropertyName("documentVersion")]
    public required string DocumentVersion { get; init; }

    [JsonPropertyName("context")]
    public required EditorContext Context { get; init; }
}
