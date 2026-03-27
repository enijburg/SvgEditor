using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

public sealed record PlanResponse
{
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    [JsonPropertyName("commands")]
    public required IReadOnlyList<SvgCommand> Commands { get; init; }

    [JsonPropertyName("validation")]
    public required ValidationInfo Validation { get; init; }
}

public sealed record ValidationInfo
{
    [JsonPropertyName("isValid")]
    public required bool IsValid { get; init; }

    [JsonPropertyName("issues")]
    public required IReadOnlyList<string> Issues { get; init; }
}
