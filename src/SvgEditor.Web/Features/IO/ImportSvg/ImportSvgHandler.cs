using System.Xml.Linq;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.IO.ImportSvg;

public sealed class ImportSvgHandler : IRequestHandler<ImportSvgCommand, SvgDocument>
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    public Task<SvgDocument> Handle(ImportSvgCommand request, CancellationToken cancellationToken = default)
    {
        var xDoc = XDocument.Parse(request.SvgContent);
        var root = xDoc.Root ?? throw new InvalidOperationException("Invalid SVG: no root element.");

        var attrs = root.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToDictionary(a => a.Name.LocalName, a => a.Value);

        attrs.TryGetValue("viewBox", out var viewBox);

        double.TryParse(attrs.GetValueOrDefault("width"), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var width);
        double.TryParse(attrs.GetValueOrDefault("height"), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var height);

        var elements = root.Elements()
            .Select(ParseElement)
            .Where(e => e is not null)
            .Select(e => e!)
            .ToList();

        var document = new SvgDocument
        {
            ViewBox = viewBox,
            Width = width,
            Height = height,
            Elements = elements,
            Attributes = attrs
        };

        return Task.FromResult(document);
    }

    private static SvgElement? ParseElement(XElement el)
    {
        var localName = el.Name.LocalName;
        var attrs = el.Attributes()
            .Where(a => !a.IsNamespaceDeclaration)
            .ToDictionary(a => a.Name.LocalName, a => a.Value);

        // Assign a stable ID if none present
        if (!attrs.TryGetValue("data-element-id", out var id) || string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
            attrs["data-element-id"] = id;
        }

        return localName switch
        {
            "rect" => new SvgRect { Id = id, Attributes = attrs },
            "circle" => new SvgCircle { Id = id, Attributes = attrs },
            "ellipse" => new SvgEllipse { Id = id, Attributes = attrs },
            "line" => new SvgLine { Id = id, Attributes = attrs },
            "polyline" => new SvgPolyline { Id = id, Attributes = attrs },
            "polygon" => new SvgPolygon { Id = id, Attributes = attrs },
            "path" => new SvgPath { Id = id, Attributes = attrs },
            "text" => new SvgText
            {
                Id = id,
                Attributes = attrs,
                Content = el.Value
            },
            "g" => new SvgGroup
            {
                Id = id,
                Attributes = attrs,
                Children = el.Elements()
                    .Select(ParseElement)
                    .Where(e => e is not null)
                    .Select(e => e!)
                    .ToList()
            },
            _ => new SvgUnknown(localName)
            {
                Id = id,
                Attributes = attrs,
                InnerXml = string.Concat(el.Nodes().Select(n => n.ToString()))
            }
        };
    }
}
