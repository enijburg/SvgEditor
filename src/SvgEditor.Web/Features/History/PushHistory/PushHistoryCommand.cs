using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.History.PushHistory;

public sealed record PushHistoryCommand(string Description) : IRequest<Unit>;
