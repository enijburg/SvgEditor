using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.History.UndoRedo;

public sealed record UndoCommand : IRequest<Unit>;
