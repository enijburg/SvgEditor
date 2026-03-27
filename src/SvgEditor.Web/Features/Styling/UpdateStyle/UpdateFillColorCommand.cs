using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Styling.UpdateStyle;

public sealed record UpdateFillColorCommand(IReadOnlyCollection<string> ElementIds, string Color) : IRequest<Unit>;
