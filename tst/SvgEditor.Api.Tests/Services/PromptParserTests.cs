using SvgEditor.Api.Contracts;
using SvgEditor.Api.Services;

namespace SvgEditor.Api.Tests.Services;

[TestClass]
public sealed class PromptParserTests
{
    private readonly PromptParser _sut = new();

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
            X = 100,
            Y = 80,
            Width = 200,
            Height = 60,
            Fill = "#cccccc",
            Stroke = "#222222"
        }).ToList()
    };

    [TestMethod]
    public void Parse_MakeSelectedRectangleBlue_ReturnsSetFillCommand()
    {
        var context = CreateContext("rect-12");
        var request = new PlanRequest
        {
            Prompt = "Make the selected rectangle blue",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        Assert.HasCount(1, response.Commands);
        var cmd = response.Commands[0] as SetFillCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("rect-12", cmd.ElementId);
        Assert.AreEqual("#0000FF", cmd.Fill);
    }

    [TestMethod]
    public void Parse_ChangeColorToRed_ReturnsSetFillCommand()
    {
        var context = CreateContext("circle-1");
        var request = new PlanRequest
        {
            Prompt = "Change the color to red",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        Assert.HasCount(1, response.Commands);
        var cmd = response.Commands[0] as SetFillCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("#FF0000", cmd.Fill);
    }

    [TestMethod]
    public void Parse_SetFillToHexColor_ReturnsSetFillCommand()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Set fill to #FF8800",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        var cmd = response.Commands[0] as SetFillCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("#FF8800", cmd.Fill);
    }

    [TestMethod]
    public void Parse_MoveRight20Pixels_ReturnsMoveSelectionCommand()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Move the selected icon 20 pixels to the right",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        Assert.HasCount(1, response.Commands);
        var cmd = response.Commands[0] as MoveSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual(20.0, cmd.Dx);
        Assert.AreEqual(0.0, cmd.Dy);
    }

    [TestMethod]
    public void Parse_Move50PxLeft_ReturnsMoveSelectionWithNegativeDx()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Move 50 pixels to the left",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        var cmd = response.Commands[0] as MoveSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual(-50.0, cmd.Dx);
        Assert.AreEqual(0.0, cmd.Dy);
    }

    [TestMethod]
    public void Parse_Move30PxDown_ReturnsMoveSelectionWithPositiveDy()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Move 30 pixels down",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        var cmd = response.Commands[0] as MoveSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual(0.0, cmd.Dx);
        Assert.AreEqual(30.0, cmd.Dy);
    }

    [TestMethod]
    public void Parse_CenterHorizontally_ReturnsAlignSelectionCommand()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Center the selected element horizontally",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        Assert.HasCount(1, response.Commands);
        var cmd = response.Commands[0] as AlignSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("center", cmd.Alignment);
    }

    [TestMethod]
    public void Parse_AlignLeft_ReturnsAlignSelectionCommand()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Align the selection to the left",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        var cmd = response.Commands[0] as AlignSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("left", cmd.Alignment);
    }

    [TestMethod]
    public void Parse_UnknownPrompt_ReturnsInvalidResponse()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Do something completely unknown",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsFalse(response.Validation.IsValid);
        Assert.IsEmpty(response.Commands);
        Assert.Contains("Could not understand", response.Summary);
    }

    [TestMethod]
    public void Parse_NoSelection_FillCommand_ReturnsInvalid()
    {
        var context = new EditorContext
        {
            DocumentId = "test",
            DocumentVersion = "1",
            Canvas = new CanvasSize { Width = 1024, Height = 768 },
            Selection = [],
            Elements = []
        };
        var request = new PlanRequest
        {
            Prompt = "Make it blue",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsFalse(response.Validation.IsValid);
    }

    [TestMethod]
    public void Parse_MultipleSelectedElements_FillCommand_CreatesMultipleCommands()
    {
        var context = CreateContext("rect-1", "rect-2", "rect-3");
        var request = new PlanRequest
        {
            Prompt = "Change color to green",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        Assert.HasCount(3, response.Commands);
        Assert.IsTrue(response.Commands.All(c => c is SetFillCommand));
    }

    [TestMethod]
    public void Parse_StrokeCommand_ReturnsSetStrokeCommands()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Set the stroke to red",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        Assert.HasCount(1, response.Commands);
        var cmd = response.Commands[0] as SetStrokeCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("#FF0000", cmd.Stroke);
    }

    [TestMethod]
    public void Parse_SummaryContainsElementId()
    {
        var context = CreateContext("my-rect");
        var request = new PlanRequest
        {
            Prompt = "Make it blue",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.Contains("my-rect", response.Summary);
    }

    [TestMethod]
    public void Parse_PaintGreen_ReturnsSetFillCommand()
    {
        var context = CreateContext("rect-1");
        var request = new PlanRequest
        {
            Prompt = "Paint it green",
            Context = context
        };

        var response = _sut.Parse(request);

        Assert.IsTrue(response.Validation.IsValid);
        var cmd = response.Commands[0] as SetFillCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("#008000", cmd.Fill);
    }
}
