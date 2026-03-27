using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.History.UndoRedo;

public sealed class RedoHandler(EditorState editorState, HistoryStack historyStack) : IRequestHandler<RedoCommand, Unit>
{
    public Task<Unit> Handle(RedoCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null || !historyStack.CanRedo) return Task.FromResult(Unit.Value);

        var restored = historyStack.Redo(editorState.Document);
        if (restored is not null)
        {
            editorState.Document = restored;
            editorState.NotifyStateChanged();
        }

        return Task.FromResult(Unit.Value);
    }
}
