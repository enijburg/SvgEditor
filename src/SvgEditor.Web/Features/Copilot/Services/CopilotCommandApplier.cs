using System.Globalization;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Canvas.UpdateElement;
using SvgEditor.Web.Features.Copilot.Models;
using SvgEditor.Web.Features.History.PushHistory;
using SvgEditor.Web.Features.Styling.UpdateStyle;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Copilot.Services;

public sealed class CopilotCommandApplier(IMediator mediator, EditorState editorState)
{
    private const double ArcOffsetRatio = 0.2;
    private const double MinimumArcOffset = 30;
    private const string DefaultArrowColor = "#333333";
    private const string DefaultArrowStrokeWidth = "2";
    private const int ArrowheadWidth = 10;
    private const int ArrowheadHeight = 7;

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
                        element.Attributes["stroke-width"] = command.Width.Value.ToString(CultureInfo.InvariantCulture);
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

            case "AddArrowBetweenSelection" when command.SourceElementId is not null && command.TargetElementId is not null:
                ApplyAddArrowBetweenSelection(command.SourceElementId, command.TargetElementId);
                break;
        }
    }

    public static (SvgUnknown Defs, SvgPath Arrow) BuildArrowElements(
        BoundingBox sourceBBox,
        BoundingBox targetBBox,
        string markerId,
        string arrowId)
    {
        var inv = CultureInfo.InvariantCulture;

        // Compute center points of source and target elements
        var sx = sourceBBox.X + (sourceBBox.Width / 2);
        var sy = sourceBBox.Y + (sourceBBox.Height / 2);
        var tx = targetBBox.X + (targetBBox.Width / 2);
        var ty = targetBBox.Y + (targetBBox.Height / 2);

        // Compute perpendicular offset for the arc control point
        var dx = tx - sx;
        var dy = ty - sy;
        var dist = Math.Sqrt((dx * dx) + (dy * dy));

        // Arc height is proportional to the distance between centers (clamped to a minimum)
        var arcOffset = Math.Max(dist * ArcOffsetRatio, MinimumArcOffset);

        // Perpendicular direction (rotate 90 degrees)
        double px, py;
        if (dist > 0)
        {
            px = -dy / dist;
            py = dx / dist;
        }
        else
        {
            px = 0;
            py = -1;
        }

        // Control point for the quadratic bezier curve
        var cx = ((sx + tx) / 2) + (px * arcOffset);
        var cy = ((sy + ty) / 2) + (py * arcOffset);

        // Build arrowhead marker as a defs element
        var refX = ArrowheadWidth;
        var refY = (ArrowheadHeight / 2.0).ToString(inv);
        var markerXml = $"""<marker xmlns="http://www.w3.org/2000/svg" id="{markerId}" markerWidth="{ArrowheadWidth}" markerHeight="{ArrowheadHeight}" refX="{refX}" refY="{refY}" orient="auto"><polygon points="0 0, {ArrowheadWidth} {refY}, 0 {ArrowheadHeight}" fill="{DefaultArrowColor}" /></marker>""";
        var defs = new SvgUnknown("defs")
        {
            Id = Guid.NewGuid().ToString(),
            Attributes = [],
            InnerXml = markerXml
        };

        // Build the arched arrow path using a quadratic bezier curve
        var pathD = string.Create(inv, $"M {sx} {sy} Q {cx} {cy} {tx} {ty}");
        var arrow = new SvgPath
        {
            Id = arrowId,
            Attributes = new Dictionary<string, string>
            {
                ["d"] = pathD,
                ["fill"] = "none",
                ["stroke"] = DefaultArrowColor,
                ["stroke-width"] = DefaultArrowStrokeWidth,
                ["marker-end"] = $"url(#{markerId})",
                ["data-element-id"] = arrowId
            }
        };

        return (defs, arrow);
    }

    private void ApplyAddArrowBetweenSelection(string sourceElementId, string targetElementId)
    {
        if (editorState.Document is null)
            return;

        var source = editorState.Document.FindById(sourceElementId);
        var target = editorState.Document.FindById(targetElementId);
        if (source is null || target is null)
            return;

        var sourceBBox = source.GetBoundingBox();
        var targetBBox = target.GetBoundingBox();
        if (sourceBBox is null || targetBBox is null)
            return;

        var markerId = $"arrowhead-{Guid.NewGuid():N}";
        var arrowId = Guid.NewGuid().ToString();

        var (defs, arrow) = BuildArrowElements(sourceBBox, targetBBox, markerId, arrowId);

        // Add defs and arrow path to the document
        var doc = editorState.Document;
        var newElements = new List<SvgElement>(doc.Elements.Count + 2);

        // Insert defs before other elements (at the start, or after existing defs)
        var defsInserted = false;
        foreach (var el in doc.Elements)
        {
            if (!defsInserted && el is not SvgUnknown { Tag: "defs" })
            {
                newElements.Add(defs);
                defsInserted = true;
            }

            newElements.Add(el);
        }

        if (!defsInserted)
        {
            newElements.Add(defs);
        }

        newElements.Add(arrow);

        editorState.Document = new SvgDocument
        {
            ViewBox = doc.ViewBox,
            Width = doc.Width,
            Height = doc.Height,
            Elements = newElements,
            Attributes = new Dictionary<string, string>(doc.Attributes)
        };

        editorState.NotifyStateChanged();
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
