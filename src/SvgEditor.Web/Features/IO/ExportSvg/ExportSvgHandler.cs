using System.Xml.Linq;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.IO.ExportSvg;

public sealed class ExportSvgHandler : IRequestHandler<ExportSvgCommand, string>
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    public Task<string> Handle(ExportSvgCommand request, CancellationToken cancellationToken = default)
    {
        var doc = request.Document;

        var root = new XElement(SvgNs + "svg");

        // Add standard namespace declarations
        root.Add(new XAttribute(XNamespace.Xmlns + "xlink", "http://www.w3.org/1999/xlink"));

        foreach (var (key, value) in doc.Attributes)
        {
            if (key is "xmlns" or "xlink") continue;
            root.SetAttributeValue(key, value);
        }

        foreach (var element in doc.Elements)
        {
            root.Add(ElementToXml(element));
        }

        var xDoc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            root);

        return Task.FromResult(xDoc.ToString());
    }

    private static XElement ElementToXml(SvgElement element)
    {
        var el = new XElement(SvgNs + element.Tag);

        foreach (var (key, value) in element.Attributes)
        {
            el.SetAttributeValue(key, value);
        }

        if (element is SvgText textEl)
        {
            el.Value = textEl.Content;
        }
        else if (element is SvgGroup group)
        {
            foreach (var child in group.Children)
            {
                el.Add(ElementToXml(child));
            }
        }

        return el;
    }
}
