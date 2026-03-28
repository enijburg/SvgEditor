using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SetFillCommand), "SetFill")]
[JsonDerivedType(typeof(SetStrokeCommand), "SetStroke")]
[JsonDerivedType(typeof(MoveElementCommand), "MoveElement")]
[JsonDerivedType(typeof(MoveSelectionCommand), "MoveSelection")]
[JsonDerivedType(typeof(AlignSelectionCommand), "AlignSelection")]
public abstract record SvgCommand;

public sealed record SetFillCommand : SvgCommand
{
    [JsonPropertyName("elementId")]
    public required string ElementId { get; init; }

    [JsonPropertyName("fill")]
    public required string Fill { get; init; }
}

public sealed record SetStrokeCommand : SvgCommand
{
    [JsonPropertyName("elementId")]
    public required string ElementId { get; init; }

    [JsonPropertyName("stroke")]
    public required string Stroke { get; init; }

    [JsonPropertyName("width")]
    public required double Width { get; init; }
}

public sealed record MoveElementCommand : SvgCommand
{
    [JsonPropertyName("elementId")]
    public required string ElementId { get; init; }

    [JsonPropertyName("dx")]
    public required double Dx { get; init; }

    [JsonPropertyName("dy")]
    public required double Dy { get; init; }
}

public sealed record MoveSelectionCommand : SvgCommand
{
    [JsonPropertyName("dx")]
    public required double Dx { get; init; }

    [JsonPropertyName("dy")]
    public required double Dy { get; init; }
}

public sealed record AlignSelectionCommand : SvgCommand
{
    [JsonPropertyName("alignment")]
    public required string Alignment { get; init; }
}
