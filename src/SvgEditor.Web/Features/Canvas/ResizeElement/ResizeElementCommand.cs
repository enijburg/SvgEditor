using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.ResizeElement;

public sealed record ResizeElementCommand(
    IReadOnlyCollection<string> ElementIds,
    BoundingBox OriginalBounds,
    BoundingBox UpdatedBounds) : IRequest<Unit>;
