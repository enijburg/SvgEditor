using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.SelectElement;

public sealed class FenceSelectHandler(EditorState editorState) : IRequestHandler<FenceSelectCommand, Unit>
{
    public Task<Unit> Handle(FenceSelectCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
        {
            return Task.FromResult(Unit.Value);
        }

        var fence = request.Fence;
        var ids = new HashSet<string>();
        CollectIntersecting(editorState.Document.Elements, fence, ids);

        editorState.SelectedElementIds = ids;
        editorState.SelectedElementId = ids.Count == 1 ? ids.First() : null;
        editorState.NotifyStateChanged();

        return Task.FromResult(Unit.Value);
    }

    private static void CollectIntersecting(List<SvgElement> elements, BoundingBox fence, HashSet<string> ids)
    {
        foreach (var element in elements)
        {
            if (element is SvgGroup group)
            {
                CollectIntersecting(group.Children, fence, ids);
                continue;
            }

            var bb = element.GetBoundingBox();
            if (bb is not null && Intersects(fence, bb))
            {
                ids.Add(element.Id);
            }
        }
    }

    public static bool Intersects(BoundingBox a, BoundingBox b) =>
        a.X < b.X + b.Width &&
        a.X + a.Width > b.X &&
        a.Y < b.Y + b.Height &&
        a.Y + a.Height > b.Y;
}
