using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.UpdateElement;

public sealed record UpdateElementCommand(string ElementId, double Dx, double Dy) : IRequest<Unit>;
