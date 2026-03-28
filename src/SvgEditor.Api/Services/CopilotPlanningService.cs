using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Services;

public sealed class CopilotPlanningService(ILogger<CopilotPlanningService> logger) : IAsyncDisposable
{
    private CopilotClient? _client;
    private readonly SemaphoreSlim _clientLock = new(1, 1);

    private async Task<CopilotClient> GetClientAsync()
    {
        if (_client is not null)
            return _client;

        await _clientLock.WaitAsync();
        try
        {
            if (_client is not null)
                return _client;

            _client = new CopilotClient(new CopilotClientOptions
            {
                Logger = logger,
            });
            await _client.StartAsync();
            return _client;
        }
        finally
        {
            _clientLock.Release();
        }
    }

    public async Task<PlanResponse> PlanAsync(PlanRequest request, CancellationToken cancellationToken = default)
    {
        var commands = new List<SvgCommand>();

        var tools = CreateTools(request.Context, commands);

        var systemMessage = BuildSystemMessage(request.Context);

        CopilotClient client;
        try
        {
            client = await GetClientAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to Copilot CLI, ensure 'copilot' is on PATH");
            return new PlanResponse
            {
                Summary = "Copilot service is not available.",
                Commands = [],
                Validation = new ValidationInfo
                {
                    IsValid = false,
                    Issues = ["Copilot CLI is not available. Ensure GitHub Copilot CLI is installed and authenticated."]
                }
            };
        }

        string assistantMessage;
        try
        {
            assistantMessage = await RunSessionAsync(client, systemMessage, request.Prompt, tools, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Copilot session failed");
            return new PlanResponse
            {
                Summary = "Copilot session failed.",
                Commands = [],
                Validation = new ValidationInfo
                {
                    IsValid = false,
                    Issues = [$"Copilot session error: {ex.Message}"]
                }
            };
        }

        if (commands.Count == 0)
        {
            return new PlanResponse
            {
                Summary = string.IsNullOrWhiteSpace(assistantMessage) ? "Could not understand the request." : assistantMessage,
                Commands = [],
                Validation = new ValidationInfo
                {
                    IsValid = false,
                    Issues = ["No commands were generated from the prompt."]
                }
            };
        }

        var validation = new CommandValidationService().Validate(commands, request.Context);
        return new PlanResponse
        {
            Summary = string.IsNullOrWhiteSpace(assistantMessage) ? FormatSummary(commands) : assistantMessage,
            Commands = commands,
            Validation = validation
        };
    }

    private static async Task<string> RunSessionAsync(
        CopilotClient client,
        string systemMessage,
        string prompt,
        List<AIFunction> tools,
        CancellationToken cancellationToken)
    {
        var assistantContent = "";
        var done = new TaskCompletionSource();

        await using var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = "gpt-4.1",
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = systemMessage,
            },
            Tools = tools,
            AvailableTools = tools.Select(t => t.Name).ToList(),
            OnPermissionRequest = PermissionHandler.ApproveAll,
        });

        using var errorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        errorCts.CancelAfter(TimeSpan.FromSeconds(30));

        using var registration = errorCts.Token.Register(() =>
        {
            if (!done.Task.IsCompleted)
                done.TrySetException(new TimeoutException("Copilot session timed out after 30 seconds."));
        });

        using var subscription = session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageEvent msg:
                    assistantContent = msg.Data.Content ?? assistantContent;
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    done.TrySetException(new InvalidOperationException(err.Data.Message ?? "Unknown session error"));
                    break;
            }
        });

        await session.SendAsync(new MessageOptions { Prompt = prompt });
        await done.Task;

        return assistantContent;
    }

    internal static List<AIFunction> CreateToolsForTesting(EditorContext context, List<SvgCommand> commands)
        => CreateTools(context, commands);

    private static List<AIFunction> CreateTools(EditorContext context, List<SvgCommand> commands)
    {
        var skipPermission = new ReadOnlyDictionary<string, object?>(
            new Dictionary<string, object?> { ["skip_permission"] = true });

        return
        [
            AIFunctionFactory.Create(
                ([Description("The element ID to change fill color on")] string elementId,
                 [Description("The fill color as hex (e.g. #FF0000) or named color (e.g. blue)")] string fill) =>
                {
                    commands.Add(new SetFillCommand { ElementId = elementId, Fill = fill });
                    return $"Set fill of {elementId} to {fill}";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "set_fill",
                    Description = "Set the fill color of an SVG element",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                ([Description("The element ID to change stroke on")] string elementId,
                 [Description("The stroke color as hex (e.g. #FF0000) or named color")] string stroke,
                 [Description("The stroke width in pixels")] double width) =>
                {
                    commands.Add(new SetStrokeCommand { ElementId = elementId, Stroke = stroke, Width = width });
                    return $"Set stroke of {elementId} to {stroke} with width {width}";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "set_stroke",
                    Description = "Set the stroke color and width of an SVG element",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                ([Description("The element ID to move")] string elementId,
                 [Description("Horizontal offset in pixels (positive=right, negative=left)")] double dx,
                 [Description("Vertical offset in pixels (positive=down, negative=up)")] double dy) =>
                {
                    commands.Add(new MoveElementCommand { ElementId = elementId, Dx = dx, Dy = dy });
                    return $"Moved {elementId} by ({dx}, {dy})";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "move_element",
                    Description = "Move a specific SVG element by dx/dy offset",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                ([Description("Horizontal offset in pixels (positive=right, negative=left)")] double dx,
                 [Description("Vertical offset in pixels (positive=down, negative=up)")] double dy) =>
                {
                    commands.Add(new MoveSelectionCommand { Dx = dx, Dy = dy });
                    return $"Moved selection by ({dx}, {dy})";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "move_selection",
                    Description = "Move all currently selected SVG elements by dx/dy offset",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                ([Description("Alignment: left, center, right, top, middle, or bottom")] string alignment) =>
                {
                    commands.Add(new AlignSelectionCommand { Alignment = alignment });
                    return $"Aligned selection {alignment}";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "align_selection",
                    Description = "Align the currently selected SVG elements",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                ([Description("The element ID where the arrow starts from")] string sourceElementId,
                 [Description("The element ID where the arrow points to")] string targetElementId,
                 [Description("Optional stroke color as hex (e.g. #000000). Default is #333333")] string? stroke = null,
                 [Description("Optional stroke width in pixels. Default is 2")] double? strokeWidth = null,
                 [Description("Optional SVG stroke-dasharray value for dashed lines (e.g. '8 4' for dashes of 8px with 4px gaps)")] string? strokeDashArray = null,
                 [Description("Where the arrow starts: 'border' (auto-detect edge, default), 'left', 'right', 'top', 'bottom' (midpoint of that edge), or 'center' (element center)")] string? sourceAnchor = null,
                 [Description("Where the arrow ends: 'border' (auto-detect edge, default), 'left', 'right', 'top', 'bottom' (midpoint of that edge), or 'center' (element center)")] string? targetAnchor = null) =>
                {
                    commands.Add(new AddArrowBetweenSelectionCommand
                    {
                        SourceElementId = sourceElementId,
                        TargetElementId = targetElementId,
                        Stroke = stroke,
                        StrokeWidth = strokeWidth,
                        StrokeDashArray = strokeDashArray,
                        SourceAnchor = sourceAnchor,
                        TargetAnchor = targetAnchor,
                    });
                    return $"Added arched arrow from {sourceElementId} to {targetElementId}";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "add_arrow_between_selection",
                    Description = "Create an arched arrow from source to target element. Supports stroke color/width/dash and anchor modes: 'border' (auto-detect edge, default), 'left'/'right'/'top'/'bottom' (midpoint of that edge), or 'center'. Use the first selected element as source and the last as target.",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                ([Description("The element ID of the line or arrow path that the text should follow")] string lineElementId,
                 [Description("The element ID of the text element to place on the line")] string textElementId) =>
                {
                    commands.Add(new PlaceTextOnLineCommand
                    {
                        LineElementId = lineElementId,
                        TextElementId = textElementId,
                    });
                    return $"Placed text {textElementId} on line {lineElementId}";
                },
                new AIFunctionFactoryOptions
                {
                    Name = "place_text_on_line",
                    Description = "Position a text element to follow the geometry of a line or arrow path. Use the line/arrow element as lineElementId and the text element as textElementId. Typically the first selected element is the line and the second is the text.",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                () =>
                {
                    return JsonSerializer.Serialize(new
                    {
                        selectedIds = context.Selection,
                        elements = context.Elements
                            .Where(e => context.Selection.Contains(e.Id))
                            .ToList()
                    });
                },
                new AIFunctionFactoryOptions
                {
                    Name = "get_selection",
                    Description = "Get the currently selected elements and their properties",
                    AdditionalProperties = skipPermission,
                }),

            AIFunctionFactory.Create(
                () =>
                {
                    return JsonSerializer.Serialize(new
                    {
                        documentId = context.DocumentId,
                        documentVersion = context.DocumentVersion,
                        canvas = context.Canvas,
                        elementCount = context.Elements.Count,
                        selectionCount = context.Selection.Count,
                        elements = context.Elements,
                    });
                },
                new AIFunctionFactoryOptions
                {
                    Name = "get_document_summary",
                    Description = "Get a summary of the current SVG document including all elements",
                    AdditionalProperties = skipPermission,
                }),
        ];
    }

    private static string BuildSystemMessage(EditorContext context)
    {
        var elements = context.Elements.Select(e =>
            $"  - id={e.Id}, type={e.Type}" +
            (e.X.HasValue ? $", x={e.X}" : "") +
            (e.Y.HasValue ? $", y={e.Y}" : "") +
            (e.Width.HasValue ? $", width={e.Width}" : "") +
            (e.Height.HasValue ? $", height={e.Height}" : "") +
            (!string.IsNullOrEmpty(e.Fill) ? $", fill={e.Fill}" : "") +
            (!string.IsNullOrEmpty(e.Stroke) ? $", stroke={e.Stroke}" : ""));

        return $"""
            You are an SVG editor assistant. The user will describe edits they want to make to an SVG document.
            You MUST use the provided tools to execute the edits. Do NOT output raw SVG or code.

            Available tools: set_fill, set_stroke, move_element, move_selection, align_selection, add_arrow_between_selection, place_text_on_line, get_selection, get_document_summary.

            Current document state:
            - Canvas size: {context.Canvas.Width}x{context.Canvas.Height}
            - Selected element IDs: [{string.Join(", ", context.Selection)}]
            - Elements:
            {string.Join("\n", elements)}

            Rules:
            - Only use element IDs that exist in the document.
            - For color values, use hex format (#RRGGBB). Convert named colors to hex.
            - When the user says "the selected element" or similar, use the selected element IDs.
            - For move commands on all selected elements, use move_selection instead of move_element.
            - When the user asks to draw an arrow between two selected elements, use add_arrow_between_selection with the first selected element as source and the last as target. Include stroke, strokeWidth, and strokeDashArray parameters directly in the tool call for styling (e.g. dashed arrows). Do NOT use set_stroke on the arrow afterwards.
            - Arrow anchor modes: use sourceAnchor/targetAnchor to control where the arrow connects. Options: 'border' (auto-detect closest edge, default), 'left', 'right', 'top', 'bottom' (midpoint of that edge), or 'center' (element center).
            - When the user says "from border to border", use sourceAnchor='border' and targetAnchor='border'.
            - When a target is to the right of the source, prefer sourceAnchor='right' and targetAnchor='left'. When the user explicitly names a side (e.g. "from the top"), use that side as the anchor.
            - When the user asks to place text on a line or arrow (e.g. "align text with line", "put text on the arrow"), use place_text_on_line with the line/arrow element as lineElementId and the text element as textElementId. The first selected element is typically the line/arrow and the second is the text.
            - Call the tools first, then provide a brief summary of what you did.
            - Do not use any tools other than the ones listed above.
            """;
    }

    private static string FormatSummary(List<SvgCommand> commands)
    {
        var parts = commands.Select<SvgCommand, string>(cmd => cmd switch
        {
            SetFillCommand f => $"Set fill of {f.ElementId} to {f.Fill}",
            SetStrokeCommand s => $"Set stroke of {s.ElementId} to {s.Stroke}",
            MoveElementCommand m => $"Move {m.ElementId} by ({m.Dx}, {m.Dy})",
            MoveSelectionCommand ms => $"Move selection by ({ms.Dx}, {ms.Dy})",
            AlignSelectionCommand a => $"Align selection {a.Alignment}",
            AddArrowBetweenSelectionCommand arr => $"Add arched arrow from {arr.SourceElementId} to {arr.TargetElementId}",
            PlaceTextOnLineCommand ptol => $"Place text {ptol.TextElementId} on line {ptol.LineElementId}",
            _ => "Unknown command"
        });
        return string.Join(". ", parts) + ".";
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            try
            {
                await _client.StopAsync();
            }
            catch
            {
                // Best-effort cleanup
            }

            await _client.DisposeAsync();
        }

        _clientLock.Dispose();
    }
}
