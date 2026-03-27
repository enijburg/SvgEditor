using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.IO.ImportSvg;

public sealed record ImportSvgCommand(string SvgContent) : IRequest<SvgDocument>;
