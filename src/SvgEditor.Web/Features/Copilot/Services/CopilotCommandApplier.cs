using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;
using SvgEditor.Web.Features.Copilot.Models;
using SvgEditor.Web.Features.History.PushHistory;
using SvgEditor.Web.Features.Styling.UpdateStyle;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Copilot.Services;

public sealed class CopilotCommandApplier(IMediator mediator, EditorState editorState)
{
    public async Task ApplyCommandsAsync(IReadOnlyList<CopilotCommand> commands, string description)
    {
        if (editorState.Document is null)
            return;

        // Push history once before applying all commands (single undoable transaction)
        await mediator.Send(new PushHistoryCommand(description));

        foreach (var command in commands)
        {
            await ApplyCommandAsync(command);
        }
    }

    private async Task ApplyCommandAsync(CopilotCommand command)
    {
        switch (command.Type)
        {
            case "SetFill" when command.ElementId is not null && command.Fill is not null:
                await mediator.Send(new UpdateFillColorCommand([command.ElementId], command.Fill));
                break;

            case "SetStroke" when command.ElementId is not null && command.Stroke is not null:
                var element = editorState.Document?.FindById(command.ElementId);
                if (element is not null && editorState.Document is not null)
                {
                    element.Attributes["stroke"] = command.Stroke;
                    if (command.Width.HasValue)
                    {
                        element.Attributes["stroke-width"] = command.Width.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    // Update marker colors (arrowheads) to match the new stroke color
                    var markerIds = new HashSet<string>(StringComparer.Ordinal);
                    UpdateFillColorHandler.CollectReferencedMarkerIds(element, markerIds);
                    if (markerIds.Count > 0)
                    {
                        var sharedMarkerIds = UpdateFillColorHandler.FindSharedMarkerIds(
                            editorState.Document.Elements, markerIds, [command.ElementId]);

                        if (sharedMarkerIds.Count > 0)
                        {
                            var idMapping = UpdateFillColorHandler.CloneSharedMarkers(
                                editorState.Document.Elements, sharedMarkerIds);
                            UpdateFillColorHandler.RemapMarkerReferences(element, idMapping);

                            var clonedIds = new HashSet<string>(idMapping.Values, StringComparer.Ordinal);
                            UpdateFillColorHandler.UpdateMarkerColors(editorState.Document.Elements, clonedIds, command.Stroke);

                            var exclusiveIds = new HashSet<string>(
                                markerIds.Except(sharedMarkerIds), StringComparer.Ordinal);
                            if (exclusiveIds.Count > 0)
                            {
                                UpdateFillColorHandler.UpdateMarkerColors(editorState.Document.Elements, exclusiveIds, command.Stroke);
                            }
                        }
                        else
                        {
                            UpdateFillColorHandler.UpdateMarkerColors(editorState.Document.Elements, markerIds, command.Stroke);
                        }
                    }

                    editorState.NotifyStateChanged();
                }

                break;

            case "MoveElement" when command.ElementId is not null:
                await mediator.Send(new UpdateElementCommand(command.ElementId, command.Dx ?? 0, command.Dy ?? 0));
                break;

            case "MoveSelection":
                var selectedIds = editorState.SelectedElementIds;
                if (selectedIds.Count > 0)
                {
                    await mediator.Send(new UpdateMultipleElementsCommand(selectedIds, command.Dx ?? 0, command.Dy ?? 0));
                }

                break;

            case "AlignSelection":
                ApplyAlignment(command.Alignment ?? "center");
                break;
        }
    }

    private void ApplyAlignment(string alignment)
    {
        if (editorState.Document is null)
            return;

        var doc = editorState.Document;
        var selectedIds = editorState.SelectedElementIds;
        if (selectedIds.Count == 0)
            return;

        var canvasWidth = doc.Width;
        var canvasHeight = doc.Height;

        foreach (var id in selectedIds)
        {
            var element = doc.FindById(id);
            var bbox = element?.GetBoundingBox();
            if (element is null || bbox is null)
                continue;

            var (dx, dy) = alignment.ToLowerInvariant() switch
            {
                "center" => ((canvasWidth / 2) - (bbox.X + (bbox.Width / 2)), 0.0),
                "middle" => (0.0, (canvasHeight / 2) - (bbox.Y + (bbox.Height / 2))),
                "left" => (-bbox.X, 0.0),
                "right" => (canvasWidth - (bbox.X + bbox.Width), 0.0),
                "top" => (0.0, -bbox.Y),
                "bottom" => (0.0, canvasHeight - (bbox.Y + bbox.Height)),
                _ => (0.0, 0.0)
            };

            var moved = element.WithOffset(dx, dy);
            editorState.Document = doc.ReplaceElement(moved);
            doc = editorState.Document;
        }

        editorState.NotifyStateChanged();
    }
}
