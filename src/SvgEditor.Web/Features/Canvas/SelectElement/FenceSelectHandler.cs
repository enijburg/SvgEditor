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
        CollectContained(editorState.Document.Elements, fence, ids);

        editorState.SelectedElementIds = ids;
        editorState.SelectedElementId = ids.Count == 1 ? ids.First() : null;
        editorState.NotifyStateChanged();

        return Task.FromResult(Unit.Value);
    }

    private static void CollectContained(List<SvgElement> elements, BoundingBox fence, HashSet<string> ids)
    {
        foreach (var element in elements)
        {
            if (element is SvgGroup group)
            {
                CollectContained(group.Children, fence, ids);
                continue;
            }

            var bb = element.GetBoundingBox();
            if (bb is not null && Contains(fence, bb))
            {
                ids.Add(element.Id);
            }
        }
    }

    public static bool Contains(BoundingBox fence, BoundingBox element) =>
        fence.X <= element.X &&
        fence.Y <= element.Y &&
        fence.X + fence.Width >= element.X + element.Width &&
        fence.Y + fence.Height >= element.Y + element.Height;
}
