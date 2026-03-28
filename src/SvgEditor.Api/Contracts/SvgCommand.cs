using System.Text.Json.Serialization;

namespace SvgEditor.Api.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SetFillCommand), "SetFill")]
[JsonDerivedType(typeof(SetStrokeCommand), "SetStroke")]
[JsonDerivedType(typeof(MoveElementCommand), "MoveElement")]
[JsonDerivedType(typeof(MoveSelectionCommand), "MoveSelection")]
[JsonDerivedType(typeof(AlignSelectionCommand), "AlignSelection")]
[JsonDerivedType(typeof(AddArrowBetweenSelectionCommand), "AddArrowBetweenSelection")]
[JsonDerivedType(typeof(PlaceTextOnLineCommand), "PlaceTextOnLine")]
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

public sealed record AddArrowBetweenSelectionCommand : SvgCommand
{
    [JsonPropertyName("sourceElementId")]
    public required string SourceElementId { get; init; }

    [JsonPropertyName("targetElementId")]
    public required string TargetElementId { get; init; }

    [JsonPropertyName("stroke")]
    public string? Stroke { get; init; }

    [JsonPropertyName("strokeWidth")]
    public double? StrokeWidth { get; init; }

    [JsonPropertyName("strokeDashArray")]
    public string? StrokeDashArray { get; init; }

    [JsonPropertyName("sourceAnchor")]
    public string? SourceAnchor { get; init; }

    [JsonPropertyName("targetAnchor")]
    public string? TargetAnchor { get; init; }
}

public sealed record PlaceTextOnLineCommand : SvgCommand
{
    [JsonPropertyName("lineElementId")]
    public required string LineElementId { get; init; }

    [JsonPropertyName("textElementId")]
    public required string TextElementId { get; init; }
}
