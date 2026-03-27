using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Styling.UpdateStyle;

public sealed class UpdateFillColorHandler(EditorState editorState) : IRequestHandler<UpdateFillColorCommand, Unit>
{
    public Task<Unit> Handle(UpdateFillColorCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
            throw new InvalidOperationException("No document loaded.");

        foreach (var elementId in request.ElementIds)
        {
            var element = editorState.Document.FindById(elementId)
                ?? throw new InvalidOperationException($"Element '{elementId}' not found.");

            element.Attributes["fill"] = request.Color;
        }

        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }
}
