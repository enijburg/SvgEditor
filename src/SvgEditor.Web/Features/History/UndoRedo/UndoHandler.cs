using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.History.UndoRedo;

public sealed class UndoHandler(EditorState editorState, HistoryStack historyStack) : IRequestHandler<UndoCommand, Unit>
{
    public Task<Unit> Handle(UndoCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null || !historyStack.CanUndo) return Task.FromResult(Unit.Value);

        var restored = historyStack.Undo(editorState.Document);
        if (restored is not null)
        {
            editorState.Document = restored;
            editorState.NotifyStateChanged();
        }

        return Task.FromResult(Unit.Value);
    }
}
