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
        CollectSelected(editorState.Document.Elements, fence, ids);

        editorState.SelectedElementIds = ids;
        editorState.SelectedElementId = ids.Count == 1 ? ids.First() : null;
        editorState.NotifyStateChanged();

        return Task.FromResult(Unit.Value);
    }

    private static void CollectSelected(List<SvgElement> elements, BoundingBox fence, HashSet<string> ids)
    {
        foreach (var element in elements)
        {
            if (element is SvgGroup group)
            {
                CollectSelected(group.Children, fence, ids);
                continue;
            }

            if (IsSelectedByFence(element, fence))
            {
                ids.Add(element.Id);
            }
        }
    }

    internal static bool IsSelectedByFence(SvgElement element, BoundingBox fence)
    {
        if (element is SvgText text)
        {
            return ContainsPoint(fence, text.X, text.Y);
        }

        var bb = element.GetBoundingBox();
        return bb is not null && Contains(fence, bb);
    }

    public static bool Contains(BoundingBox fence, BoundingBox element) =>
        fence.X <= element.X &&
        fence.Y <= element.Y &&
        fence.X + fence.Width >= element.X + element.Width &&
        fence.Y + fence.Height >= element.Y + element.Height;

    public static bool ContainsPoint(BoundingBox fence, double x, double y) =>
        x >= fence.X && x <= fence.X + fence.Width &&
        y >= fence.Y && y <= fence.Y + fence.Height;
}
