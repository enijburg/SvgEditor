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
        foreach (var elementId in request.ElementIds)
        {
            var element = doc.FindById(elementId);
            if (element is null) continue;

            var resized = element.WithResize(request.OriginalBounds, request.UpdatedBounds);
            doc = doc.ReplaceElement(resized);
        }

        editorState.Document = doc;
        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }
}
