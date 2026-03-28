using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.DeleteElement;

public sealed record DeleteElementCommand(IReadOnlyCollection<string> ElementIds) : IRequest<Unit>;
