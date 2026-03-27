using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.History.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.History.PushHistory;

public sealed class PushHistoryHandler(EditorState editorState, HistoryStack historyStack) : IRequestHandler<PushHistoryCommand, Unit>
{
    public Task<Unit> Handle(PushHistoryCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null) return Task.FromResult(Unit.Value);

        historyStack.Push(new HistoryEntry(request.Description, editorState.Document.DeepClone()));
        return Task.FromResult(Unit.Value);
    }
}
