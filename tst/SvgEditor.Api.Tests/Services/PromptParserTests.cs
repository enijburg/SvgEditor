using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using SvgEditor.Api.Contracts;
using SvgEditor.Api.Services;

namespace SvgEditor.Api.Tests.Services;

[TestClass]
public sealed class CopilotPlanningServiceToolTests
{
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

    /// <summary>
    /// Invokes the named tool with the given arguments and returns the commands collected.
    /// This tests the tool functions that the Copilot SDK will call.
    /// </summary>
    private static async Task<(List<SvgCommand> Commands, object? Result)> InvokeToolAsync(
        string toolName, Dictionary<string, object?> args, EditorContext? context = null)
    {
        context ??= CreateContext("rect-1");
        var commands = new List<SvgCommand>();
        var tools = CopilotPlanningService.CreateToolsForTesting(context, commands);
        var tool = tools.First(t => t.Name == toolName);
        var result = await tool.InvokeAsync(new AIFunctionArguments(args));
        return (commands, result);
    }

    [TestMethod]
    public async Task SetFillTool_AddsSetFillCommand()
    {
        var (commands, result) = await InvokeToolAsync("set_fill", new Dictionary<string, object?>
        {
            ["elementId"] = "rect-1",
            ["fill"] = "#0000FF",
        });

        Assert.HasCount(1, commands);
        var cmd = commands[0] as SetFillCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("rect-1", cmd.ElementId);
        Assert.AreEqual("#0000FF", cmd.Fill);
    }

    [TestMethod]
    public async Task SetStrokeTool_AddsSetStrokeCommand()
    {
        var (commands, _) = await InvokeToolAsync("set_stroke", new Dictionary<string, object?>
        {
            ["elementId"] = "rect-1",
            ["stroke"] = "#FF0000",
            ["width"] = 2.0,
        });

        Assert.HasCount(1, commands);
        var cmd = commands[0] as SetStrokeCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("rect-1", cmd.ElementId);
        Assert.AreEqual("#FF0000", cmd.Stroke);
        Assert.AreEqual(2.0, cmd.Width);
    }

    [TestMethod]
    public async Task MoveElementTool_AddsMoveElementCommand()
    {
        var (commands, _) = await InvokeToolAsync("move_element", new Dictionary<string, object?>
        {
            ["elementId"] = "rect-1",
            ["dx"] = 20.0,
            ["dy"] = -10.0,
        });

        Assert.HasCount(1, commands);
        var cmd = commands[0] as MoveElementCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("rect-1", cmd.ElementId);
        Assert.AreEqual(20.0, cmd.Dx);
        Assert.AreEqual(-10.0, cmd.Dy);
    }

    [TestMethod]
    public async Task MoveSelectionTool_AddsMoveSelectionCommand()
    {
        var (commands, _) = await InvokeToolAsync("move_selection", new Dictionary<string, object?>
        {
            ["dx"] = 50.0,
            ["dy"] = 0.0,
        });

        Assert.HasCount(1, commands);
        var cmd = commands[0] as MoveSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual(50.0, cmd.Dx);
        Assert.AreEqual(0.0, cmd.Dy);
    }

    [TestMethod]
    public async Task AlignSelectionTool_AddsAlignCommand()
    {
        var (commands, _) = await InvokeToolAsync("align_selection", new Dictionary<string, object?>
        {
            ["alignment"] = "center",
        });

        Assert.HasCount(1, commands);
        var cmd = commands[0] as AlignSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("center", cmd.Alignment);
    }

    [TestMethod]
    public async Task AddArrowBetweenSelectionTool_AddsCommand()
    {
        var context = CreateContext("rect-1", "rect-2");
        var (commands, result) = await InvokeToolAsync("add_arrow_between_selection", new Dictionary<string, object?>
        {
            ["sourceElementId"] = "rect-1",
            ["targetElementId"] = "rect-2",
        }, context);

        Assert.HasCount(1, commands);
        var cmd = commands[0] as AddArrowBetweenSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("rect-1", cmd.SourceElementId);
        Assert.AreEqual("rect-2", cmd.TargetElementId);
        Assert.IsNull(cmd.Stroke);
        Assert.IsNull(cmd.StrokeWidth);
        Assert.IsNull(cmd.StrokeDashArray);
        Assert.IsNotNull(result);
        Assert.Contains("rect-1", result.ToString()!);
        Assert.Contains("rect-2", result.ToString()!);
    }

    [TestMethod]
    public async Task AddArrowBetweenSelectionTool_WithStyling_AddsCommandWithStyling()
    {
        var context = CreateContext("rect-1", "rect-2");
        var (commands, _) = await InvokeToolAsync("add_arrow_between_selection", new Dictionary<string, object?>
        {
            ["sourceElementId"] = "rect-1",
            ["targetElementId"] = "rect-2",
            ["stroke"] = "#FF0000",
            ["strokeWidth"] = 3.0,
            ["strokeDashArray"] = "8 4",
        }, context);

        Assert.HasCount(1, commands);
        var cmd = commands[0] as AddArrowBetweenSelectionCommand;
        Assert.IsNotNull(cmd);
        Assert.AreEqual("rect-1", cmd.SourceElementId);
        Assert.AreEqual("rect-2", cmd.TargetElementId);
        Assert.AreEqual("#FF0000", cmd.Stroke);
        Assert.AreEqual(3.0, cmd.StrokeWidth);
        Assert.AreEqual("8 4", cmd.StrokeDashArray);
    }

    [TestMethod]
    public async Task GetSelectionTool_ReturnsSelectedElements()
    {
        var context = CreateContext("rect-1", "rect-2");
        var (commands, result) = await InvokeToolAsync("get_selection", [], context);

        Assert.IsEmpty(commands);
        Assert.IsNotNull(result);
        var json = result.ToString()!;
        Assert.Contains("rect-1", json);
        Assert.Contains("rect-2", json);
    }

    [TestMethod]
    public async Task GetDocumentSummaryTool_ReturnsDocumentInfo()
    {
        var context = CreateContext("rect-1");
        var (commands, result) = await InvokeToolAsync("get_document_summary", [], context);

        Assert.IsEmpty(commands);
        Assert.IsNotNull(result);
        var json = result.ToString()!;
        Assert.Contains("test-doc", json);
        Assert.Contains("1024", json);
    }

    [TestMethod]
    public void CreateTools_CreatesAllExpectedTools()
    {
        var context = CreateContext("rect-1");
        var commands = new List<SvgCommand>();
        var tools = CopilotPlanningService.CreateToolsForTesting(context, commands);

        Assert.HasCount(8, tools);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("set_fill", toolNames);
        Assert.Contains("set_stroke", toolNames);
        Assert.Contains("move_element", toolNames);
        Assert.Contains("move_selection", toolNames);
        Assert.Contains("align_selection", toolNames);
        Assert.Contains("add_arrow_between_selection", toolNames);
        Assert.Contains("get_selection", toolNames);
        Assert.Contains("get_document_summary", toolNames);
    }

    [TestMethod]
    public async Task MultipleToolInvocations_AccumulateCommands()
    {
        var context = CreateContext("rect-1", "rect-2");
        var commands = new List<SvgCommand>();
        var tools = CopilotPlanningService.CreateToolsForTesting(context, commands);

        var setFill = tools.First(t => t.Name == "set_fill");
        var moveSelection = tools.First(t => t.Name == "move_selection");

        await setFill.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["elementId"] = "rect-1",
            ["fill"] = "#FF0000",
        }));
        await setFill.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["elementId"] = "rect-2",
            ["fill"] = "#00FF00",
        }));
        await moveSelection.InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?>
        {
            ["dx"] = 10.0,
            ["dy"] = 0.0,
        }));

        Assert.HasCount(3, commands);
        Assert.IsInstanceOfType<SetFillCommand>(commands[0]);
        Assert.IsInstanceOfType<SetFillCommand>(commands[1]);
        Assert.IsInstanceOfType<MoveSelectionCommand>(commands[2]);
    }

    [TestMethod]
    public async Task PlanAsync_WhenCopilotUnavailable_ReturnsUnavailableResponse()
    {
        var service = new CopilotPlanningService(NullLogger<CopilotPlanningService>.Instance);
        var request = new PlanRequest
        {
            Prompt = "Make rect blue",
            Context = CreateContext("rect-1")
        };

        // The Copilot CLI is not installed in the test environment,
        // so this should return a graceful error response
        var response = await service.PlanAsync(request);

        Assert.IsFalse(response.Validation.IsValid);
        Assert.IsEmpty(response.Commands);
        // Either Copilot is not available or session fails - both are expected
        Assert.IsNotEmpty(response.Validation.Issues);
    }
}

