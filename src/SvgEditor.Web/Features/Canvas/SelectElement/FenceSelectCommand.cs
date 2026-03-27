using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.SelectElement;

public sealed record FenceSelectCommand(BoundingBox Fence) : IRequest<Unit>;
