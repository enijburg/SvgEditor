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
            var added = ids.Add(request.ElementId);
            if (!added)
            {
                ids.Remove(request.ElementId);
            }

            editorState.SelectedElementIds = ids;
            editorState.SelectedElementId = added ? request.ElementId
                : ids.Count > 0 ? ids.First() : null;
        }
        else if (editorState.SelectedElementIds.Contains(request.ElementId))
        {
            // Regular click on an already-selected element — keep multiselection to enable dragging
            editorState.SelectedElementId = request.ElementId;
        }
        else
        {
            // Regular click on an unselected element — select only this element
            editorState.SelectedElementId = request.ElementId;
            editorState.SelectedElementIds = [request.ElementId];
        }

        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }
}
