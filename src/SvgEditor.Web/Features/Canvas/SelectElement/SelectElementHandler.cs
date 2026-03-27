using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.SelectElement;

public sealed class SelectElementHandler(EditorState editorState) : IRequestHandler<SelectElementCommand, Unit>
{
    public Task<Unit> Handle(SelectElementCommand request, CancellationToken cancellationToken = default)
    {
        editorState.SelectedElementId = request.ElementId;
        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }
}
