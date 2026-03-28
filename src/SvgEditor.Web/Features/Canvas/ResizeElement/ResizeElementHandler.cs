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
        // continues to follow the new line geometry.
        //
        // Build a map of lineId → new path data from the (already-updated) document so we
        // avoid redundant FindById calls and keep the loop O(n) instead of O(n²).
        Dictionary<string, string>? linePathMap = null;
        foreach (var elementId in request.ElementIds)
        {
            if (doc.FindById(elementId) is SvgLine resizedLine)
            {
                linePathMap ??= new Dictionary<string, string>(StringComparer.Ordinal);
                linePathMap[resizedLine.Id] = resizedLine.ToPathData();
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
                doc = doc.ReplaceElement(new SvgText { Id = text.Id, Attributes = updatedAttrs, Content = text.Content });
            }
        }

        editorState.Document = doc;
        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }
}
