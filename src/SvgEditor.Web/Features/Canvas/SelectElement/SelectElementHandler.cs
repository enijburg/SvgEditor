using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.SelectElement;

public sealed class SelectElementHandler(EditorState editorState) : IRequestHandler<SelectElementCommand, Unit>
{
    public Task<Unit> Handle(SelectElementCommand request, CancellationToken cancellationToken = default)
    {
        if (request.ElementId is null)
        {
            // Click on empty canvas — clear selection
            editorState.SelectedElementId = null;
            editorState.SelectedElementIds = [];
        }
        else if (request.CtrlKey)
        {
            // Ctrl+click — toggle element in selection
            var ids = new HashSet<string>(editorState.SelectedElementIds);
            if (!ids.Remove(request.ElementId))
            {
                ids.Add(request.ElementId);
            }

            editorState.SelectedElementIds = ids;
            editorState.SelectedElementId = ids.Count > 0 ? request.ElementId : null;
        }
        else
        {
            // Regular click — select only this element
            editorState.SelectedElementId = request.ElementId;
            editorState.SelectedElementIds = [request.ElementId];
        }

        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }
}
