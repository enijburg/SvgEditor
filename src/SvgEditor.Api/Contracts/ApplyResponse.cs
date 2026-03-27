using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

public sealed record ApplyResponse
{
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("appliedCommands")]
    public required int AppliedCommands { get; init; }
}
