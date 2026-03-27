using System.Text.Json;
using SvgEditor.Api.Contracts;
using SvgEditor.Api.Services;
using SvgEditor.Api.Telemetry;
using SvgEditor.Api.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<CommandValidationService>();
builder.Services.AddSingleton<PromptParser>();
builder.Services.AddSingleton<ISvgTool, GetSelectionTool>();
builder.Services.AddSingleton<ISvgTool, GetDocumentSummaryTool>();
builder.Services.AddSingleton<ISvgTool, SetFillTool>();
builder.Services.AddSingleton<ISvgTool, MoveSelectionTool>();
builder.Services.AddSingleton<ISvgTool, AlignSelectionTool>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

app.MapPost("/api/copilot/plan", (PlanRequest request, PromptParser parser, CommandValidationService validator, ILogger<Program> logger) =>
{
    using var activity = TelemetryConstants.ActivitySource.StartActivity("CopilotPlan");
    activity?.SetTag("copilot.prompt", request.Prompt);
    activity?.SetTag("copilot.document_id", request.Context.DocumentId);
    activity?.SetTag("copilot.selection_count", request.Context.Selection.Count);

    logger.LogInformation("Copilot plan request received: {Prompt}", request.Prompt);

    var response = parser.Parse(request);

    activity?.SetTag("copilot.command_count", response.Commands.Count);
    activity?.SetTag("copilot.validation_valid", response.Validation.IsValid);

    logger.LogInformation(
        "Copilot plan generated: {CommandCount} commands, valid={IsValid}",
        response.Commands.Count,
        response.Validation.IsValid);

    return Results.Ok(response);
});

app.MapPost("/api/copilot/apply", (ApplyRequest request, CommandValidationService validator, ILogger<Program> logger) =>
{
    using var activity = TelemetryConstants.ActivitySource.StartActivity("CopilotApply");
    activity?.SetTag("copilot.document_version", request.DocumentVersion);
    activity?.SetTag("copilot.command_count", request.Commands.Count);

    logger.LogInformation(
        "Copilot apply request received: {CommandCount} commands, version={Version}",
        request.Commands.Count,
        request.DocumentVersion);

    // Validate version matches
    if (request.DocumentVersion != request.Context.DocumentVersion)
    {
        activity?.SetTag("copilot.apply_result", "stale_version");
        logger.LogWarning(
            "Copilot apply rejected: document version mismatch (expected={Expected}, actual={Actual})",
            request.Context.DocumentVersion,
            request.DocumentVersion);

        return Results.Conflict(new ApplyResponse
        {
            Success = false,
            Message = "Document has changed since planning. Please re-plan.",
            AppliedCommands = 0
        });
    }

    // Validate commands
    var validation = validator.Validate(request.Commands, request.Context);
    if (!validation.IsValid)
    {
        activity?.SetTag("copilot.apply_result", "validation_failed");
        logger.LogWarning("Copilot apply rejected: validation failed with {IssueCount} issues", validation.Issues.Count);

        return Results.BadRequest(new ApplyResponse
        {
            Success = false,
            Message = $"Validation failed: {string.Join("; ", validation.Issues)}",
            AppliedCommands = 0
        });
    }

    activity?.SetTag("copilot.apply_result", "success");
    logger.LogInformation("Copilot apply accepted: {CommandCount} commands applied", request.Commands.Count);

    return Results.Ok(new ApplyResponse
    {
        Success = true,
        Message = $"Successfully applied {request.Commands.Count} command(s).",
        AppliedCommands = request.Commands.Count
    });
});

app.Run();

// Make Program class accessible for integration tests
public partial class Program;
