using SvgEditor.Web.Features.Canvas.Models;

namespace SvgEditor.Web.Features.History.Models;

public sealed record HistoryEntry(string Description, SvgDocument Snapshot);
