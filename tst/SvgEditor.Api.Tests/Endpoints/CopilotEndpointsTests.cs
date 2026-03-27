using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tests.Endpoints;

[TestClass]
public sealed class CopilotEndpointsTests
{
    private static WebApplicationFactory<Program>? _factory;
    private static HttpClient? _client;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

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
    public async Task Plan_ValidFillRequest_ReturnsOkWithCommands()
    {
        var request = new PlanRequest
        {
            Prompt = "Make the selected rectangle blue",
            Context = CreateContext("rect-12")
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/plan", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Validation.IsValid);
        Assert.IsNotEmpty(result.Commands);
        Assert.IsGreaterThan(0, result.Summary.Length);
    }

    [TestMethod]
    public async Task Plan_MoveRequest_ReturnsOkWithMoveCommand()
    {
        var request = new PlanRequest
        {
            Prompt = "Move 20 pixels to the right",
            Context = CreateContext("rect-1")
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/plan", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Validation.IsValid);
    }

    [TestMethod]
    public async Task Plan_UnknownPrompt_ReturnsOkWithInvalidValidation()
    {
        var request = new PlanRequest
        {
            Prompt = "Do something completely random and unknown",
            Context = CreateContext("rect-1")
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/plan", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Validation.IsValid);
    }

    [TestMethod]
    public async Task Apply_ValidRequest_ReturnsOkWithSuccess()
    {
        var context = CreateContext("rect-1");
        var request = new ApplyRequest
        {
            Commands = [new SetFillCommand { ElementId = "rect-1", Fill = "#0000FF" }],
            DocumentVersion = "1",
            Context = context
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/apply", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.AppliedCommands);
    }

    [TestMethod]
    public async Task Apply_StaleVersion_ReturnsConflict()
    {
        var context = new EditorContext
        {
            DocumentId = "test-doc",
            DocumentVersion = "2",
            Canvas = new CanvasSize { Width = 1024, Height = 768 },
            Selection = ["rect-1"],
            Elements = [new ElementSummary { Id = "rect-1", Type = "rect" }]
        };

        var request = new ApplyRequest
        {
            Commands = [new SetFillCommand { ElementId = "rect-1", Fill = "#0000FF" }],
            DocumentVersion = "1",
            Context = context
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/apply", request);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.Contains("changed", result.Message);
    }

    [TestMethod]
    public async Task Apply_InvalidCommand_ReturnsBadRequest()
    {
        var context = CreateContext("rect-1");
        var request = new ApplyRequest
        {
            Commands = [new SetFillCommand { ElementId = "nonexistent", Fill = "#0000FF" }],
            DocumentVersion = "1",
            Context = context
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/apply", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ApplyResponse>();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public async Task Apply_InvalidColorValue_ReturnsBadRequest()
    {
        var context = CreateContext("rect-1");
        var request = new ApplyRequest
        {
            Commands = [new SetFillCommand { ElementId = "rect-1", Fill = "<script>alert(1)</script>" }],
            DocumentVersion = "1",
            Context = context
        };

        var response = await _client!.PostAsJsonAsync("/api/copilot/apply", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
