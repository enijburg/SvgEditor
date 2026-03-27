using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.History.UndoRedo;

public sealed record RedoCommand : IRequest<Unit>;
