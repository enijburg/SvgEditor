using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.ResizeElement;

public sealed class ResizeElementHandler(EditorState editorState) : IRequestHandler<ResizeElementCommand, Unit>
{
    public Task<Unit> Handle(ResizeElementCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
            throw new InvalidOperationException("No document loaded.");

        var doc = editorState.Document;

        // First pass: resize every selected element via its own WithResize.
        // For SvgText elements whose LinkedLineId matches a line being resized,
        // WithResize is a no-op; the path attribute update happens in the second pass.
        foreach (var elementId in request.ElementIds)
        {
            var element = doc.FindById(elementId);
            if (element is null) continue;

            var resized = element.WithResize(request.OriginalBounds, request.UpdatedBounds);
            doc = doc.ReplaceElement(resized);
        }

        // Second pass: for each resized SvgLine, find selected SvgText elements that are
        // linked to it (via data-line-id) and update their SVG 'path' attribute so the text
        // continues to follow the new line geometry.  Also update the rotation angle stored
        // in the text's transform attribute so it keeps matching the (possibly changed) slope.
        //
        // Build maps of lineId → new path data and lineId → new angle from the
        // (already-updated) document so we avoid redundant FindById calls.
        Dictionary<string, string>? linePathMap = null;
        Dictionary<string, double>? lineAngleMap = null;
        foreach (var elementId in request.ElementIds)
        {
            if (doc.FindById(elementId) is SvgLine resizedLine)
            {
                linePathMap ??= new Dictionary<string, string>(StringComparer.Ordinal);
                linePathMap[resizedLine.Id] = resizedLine.ToPathData();

                lineAngleMap ??= new Dictionary<string, double>(StringComparer.Ordinal);
                lineAngleMap[resizedLine.Id] = Math.Atan2(resizedLine.Y2 - resizedLine.Y1, resizedLine.X2 - resizedLine.X1) * 180.0 / Math.PI;
            }
        }

        if (linePathMap is not null)
        {
            foreach (var otherId in request.ElementIds)
            {
                if (doc.FindById(otherId) is not SvgText text)
                    continue;

                if (text.LinkedLineId is not { } linkedId || !linePathMap.TryGetValue(linkedId, out var newPathData))
                    continue;

                var updatedAttrs = new Dictionary<string, string>(text.Attributes)
                {
                    ["path"] = newPathData
                };

                // Keep the text's rotation transform in sync with the line's new slope and
                // remap the rotation pivot with the same resize transform so label placement
                // remains aligned after grouped resize operations.
                if (lineAngleMap!.TryGetValue(linkedId, out var newAngle) &&
                    updatedAttrs.TryGetValue("transform", out var existingTransform))
                {
                    var updated = UpdateRotationTransform(existingTransform, newAngle, request.OriginalBounds, request.UpdatedBounds);
                    if (updated is not null)
                        updatedAttrs["transform"] = updated;
                }

                doc = doc.ReplaceElement(new SvgText { Id = text.Id, Attributes = updatedAttrs, Content = text.Content });
            }
        }

        editorState.Document = doc;
        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }

    /// <summary>
    /// Replaces the rotation angle in a transform string of the form
    /// <c>rotate(angle)</c> or <c>rotate(angle, cx, cy)</c> (SVG's two legal arities).
    /// For the 3-argument form, the pivot coordinates are remapped through the same
    /// bounding-box resize transform used for the selected elements.
    /// Returns <see langword="null"/> when the transform is not a simple rotate.
    /// </summary>
    private static string? UpdateRotationTransform(string transform, double newAngle, BoundingBox original, BoundingBox updated)
    {
        if (!transform.StartsWith("rotate(", StringComparison.Ordinal))
            return null;

        var close = transform.LastIndexOf(')');
        if (close < "rotate(".Length)
            return null;

        var inner = transform["rotate(".Length..close];
        var parts = inner.Split([',', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var angleStr = newAngle.ToString(System.Globalization.CultureInfo.InvariantCulture);

        // SVG rotate() takes exactly 1 argument (angle) or exactly 3 (angle, cx, cy).
        if (parts.Length == 1)
            return $"rotate({angleStr})";

        if (parts.Length != 3)
            return null;

        if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cx) ||
            !double.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cy))
            return null;

        var sx = original.Width > 0 ? updated.Width / original.Width : 1;
        var sy = original.Height > 0 ? updated.Height / original.Height : 1;
        var remappedCx = updated.X + ((cx - original.X) * sx);
        var remappedCy = updated.Y + ((cy - original.Y) * sy);
        var cxStr = remappedCx.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var cyStr = remappedCy.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return $"rotate({angleStr},{cxStr},{cyStr})";
    }
}
