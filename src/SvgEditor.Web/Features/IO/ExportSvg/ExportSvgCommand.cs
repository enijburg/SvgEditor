using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.IO.ExportSvg;

public sealed record ExportSvgCommand(SvgDocument Document) : IRequest<string>;
