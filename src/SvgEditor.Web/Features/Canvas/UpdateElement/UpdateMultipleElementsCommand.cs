using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.UpdateElement;

public sealed record UpdateMultipleElementsCommand(IReadOnlyCollection<string> ElementIds, double Dx, double Dy) : IRequest<Unit>;
