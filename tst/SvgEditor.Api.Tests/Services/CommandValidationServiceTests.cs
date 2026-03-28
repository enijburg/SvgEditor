using SvgEditor.Api.Contracts;
using SvgEditor.Api.Services;

namespace SvgEditor.Api.Tests.Services;

[TestClass]
public sealed class CommandValidationServiceTests
{
    private readonly CommandValidationService _sut = new();

    private static EditorContext CreateContext(params string[] elementIds) => new()
    {
        DocumentId = "test-doc",
        DocumentVersion = "1",
        Canvas = new CanvasSize { Width = 1024, Height = 768 },
        Selection = elementIds,
        Elements = elementIds.Select(id => new ElementSummary
        {
            Id = id,
            Type = "rect",
            X = 10,
            Y = 20,
            Width = 100,
            Height = 50,
            Fill = "#cccccc",
            Stroke = "#000000"
        }).ToList()
    };

    [TestMethod]
    public void Validate_EmptyCommands_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var result = _sut.Validate([], context);

        Assert.IsFalse(result.IsValid);
        Assert.IsNotEmpty(result.Issues);
    }

    [TestMethod]
    public void Validate_ValidSetFillCommand_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "rect-1", Fill = "#0000FF" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Issues);
    }

    [TestMethod]
    public void Validate_SetFillWithNamedColor_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "rect-1", Fill = "blue" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_SetFillWithInvalidColor_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "rect-1", Fill = "not-a-color" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("Invalid fill color")));
    }

    [TestMethod]
    public void Validate_SetFillWithUnknownElement_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "nonexistent", Fill = "#0000FF" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("not found")));
    }

    [TestMethod]
    public void Validate_SetFillWithEmptyElementId_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "", Fill = "#0000FF" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("empty")));
    }

    [TestMethod]
    public void Validate_ValidSetStrokeCommand_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetStrokeCommand { ElementId = "rect-1", Stroke = "#FF0000", Width = 2 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_SetStrokeWithNegativeWidth_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetStrokeCommand { ElementId = "rect-1", Stroke = "#FF0000", Width = -1 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("negative")));
    }

    [TestMethod]
    public void Validate_ValidMoveElementCommand_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new MoveElementCommand { ElementId = "rect-1", Dx = 20, Dy = 0 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_MoveElementWithInfinity_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new MoveElementCommand { ElementId = "rect-1", Dx = double.PositiveInfinity, Dy = 0 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("finite")));
    }

    [TestMethod]
    public void Validate_MoveElementWithNaN_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new MoveElementCommand { ElementId = "rect-1", Dx = double.NaN, Dy = 0 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void Validate_ValidMoveSelectionCommand_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new MoveSelectionCommand { Dx = 10, Dy = -5 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_ValidAlignSelectionCommand_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new AlignSelectionCommand { Alignment = "center" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_InvalidAlignmentValue_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new AlignSelectionCommand { Alignment = "diagonal" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("Invalid alignment")));
    }

    [TestMethod]
    public void Validate_MultipleCommands_AllValid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "rect-1", Fill = "#FF0000" },
            new SetFillCommand { ElementId = "rect-2", Fill = "#00FF00" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_MultipleCommands_OneInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "rect-1", Fill = "#FF0000" },
            new SetFillCommand { ElementId = "nonexistent", Fill = "#00FF00" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void Validate_ElementIdWithXssAttempt_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "<script>alert('xss')</script>", Fill = "#0000FF" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void Validate_SetFillWithShortHexColor_ReturnsValid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = "rect-1", Fill = "#00F" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_GuidElementId_ReturnsValid()
    {
        var guid = Guid.NewGuid().ToString();
        var context = CreateContext(guid);
        var commands = new List<SvgCommand>
        {
            new SetFillCommand { ElementId = guid, Fill = "#0000FF" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void Validate_MoveWithExtremeValues_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new MoveElementCommand { ElementId = "rect-1", Dx = 200000, Dy = 0 }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("range")));
    }

    [TestMethod]
    public void Validate_ValidAddArrowBetweenSelectionCommand_ReturnsValid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand { SourceElementId = "rect-1", TargetElementId = "rect-2" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Issues);
    }

    [TestMethod]
    public void Validate_AddArrowBetweenSelection_UnknownSource_ReturnsInvalid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand { SourceElementId = "nonexistent", TargetElementId = "rect-2" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("not found")));
    }

    [TestMethod]
    public void Validate_AddArrowBetweenSelection_SameSourceAndTarget_ReturnsInvalid()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand { SourceElementId = "rect-1", TargetElementId = "rect-1" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("different")));
    }

    [TestMethod]
    public void Validate_AddArrowWithStyling_ReturnsValid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                Stroke = "#FF0000",
                StrokeWidth = 3,
                StrokeDashArray = "8 4"
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Issues);
    }

    [TestMethod]
    public void Validate_AddArrowWithInvalidDashArray_ReturnsInvalid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                StrokeDashArray = "#000000 4 2"
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("stroke-dasharray")));
    }

    [TestMethod]
    public void Validate_AddArrowWithInvalidStrokeColor_ReturnsInvalid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                Stroke = "not-a-color"
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("Invalid stroke color")));
    }

    [TestMethod]
    public void Validate_AddArrowWithNegativeStrokeWidth_ReturnsInvalid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                StrokeWidth = -1
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("negative")));
    }

    [TestMethod]
    public void Validate_AddArrowWithValidAnchors_ReturnsValid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                SourceAnchor = "border",
                TargetAnchor = "center"
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Issues);
    }

    [TestMethod]
    public void Validate_AddArrowWithInvalidSourceAnchor_ReturnsInvalid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                SourceAnchor = "invalid"
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("sourceAnchor")));
    }

    [TestMethod]
    public void Validate_AddArrowWithInvalidTargetAnchor_ReturnsInvalid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>
        {
            new AddArrowBetweenSelectionCommand
            {
                SourceElementId = "rect-1",
                TargetElementId = "rect-2",
                TargetAnchor = "edge"
            }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("targetAnchor")));
    }

    [TestMethod]
    public void Validate_AddArrowWithDirectionalAnchors_ReturnsValid()
    {
        var context = CreateContext("rect-1", "rect-2");
        var anchors = new[] { "left", "right", "top", "bottom" };

        foreach (var anchor in anchors)
        {
            var commands = new List<SvgCommand>
            {
                new AddArrowBetweenSelectionCommand
                {
                    SourceElementId = "rect-1",
                    TargetElementId = "rect-2",
                    SourceAnchor = anchor,
                    TargetAnchor = anchor
                }
            };

            var result = _sut.Validate(commands, context);

            Assert.IsTrue(result.IsValid, $"Anchor '{anchor}' should be valid");
            Assert.IsEmpty(result.Issues);
        }
    }

    [TestMethod]
    public void Validate_ValidPlaceTextOnLineCommand_ReturnsValid()
    {
        var context = CreateContext("line-1", "text-1");
        var commands = new List<SvgCommand>
        {
            new PlaceTextOnLineCommand { LineElementId = "line-1", TextElementId = "text-1" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsTrue(result.IsValid);
        Assert.IsEmpty(result.Issues);
    }

    [TestMethod]
    public void Validate_PlaceTextOnLine_UnknownLineElement_ReturnsInvalid()
    {
        var context = CreateContext("text-1");
        var commands = new List<SvgCommand>
        {
            new PlaceTextOnLineCommand { LineElementId = "nonexistent", TextElementId = "text-1" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("not found")));
    }

    [TestMethod]
    public void Validate_PlaceTextOnLine_SameLineAndText_ReturnsInvalid()
    {
        var context = CreateContext("elem-1");
        var commands = new List<SvgCommand>
        {
            new PlaceTextOnLineCommand { LineElementId = "elem-1", TextElementId = "elem-1" }
        };

        var result = _sut.Validate(commands, context);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Issues.Any(i => i.Contains("different")));
    }
}
