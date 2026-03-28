using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.DeleteElement;

public sealed class DeleteElementHandler(EditorState editorState) : IRequestHandler<DeleteElementCommand, Unit>
{
    public Task<Unit> Handle(DeleteElementCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
            throw new InvalidOperationException("No document loaded.");

        editorState.Document = editorState.Document.RemoveElements(request.ElementIds);
        editorState.SelectedElementId = null;
        editorState.SelectedElementIds = [];
        editorState.NotifyStateChanged();

        return Task.FromResult(Unit.Value);
    }
}
