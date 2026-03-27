using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.UpdateElement;

public sealed class UpdateElementHandler(EditorState editorState) : IRequestHandler<UpdateElementCommand, Unit>
{
    public Task<Unit> Handle(UpdateElementCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
            throw new InvalidOperationException("No document loaded.");

        var element = editorState.Document.FindById(request.ElementId)
            ?? throw new InvalidOperationException($"Element '{request.ElementId}' not found.");

        var moved = element.WithOffset(request.Dx, request.Dy);
        editorState.Document = editorState.Document.ReplaceElement(moved);
        editorState.NotifyStateChanged();

        return Task.FromResult(Unit.Value);
    }
}
