using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Canvas.SelectElement;

public sealed record SelectElementCommand(string? ElementId) : IRequest<Unit>;
