using System.Xml.Linq;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Styling.UpdateStyle;

public sealed class UpdateFillColorHandler(EditorState editorState) : IRequestHandler<UpdateFillColorCommand, Unit>
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

    public Task<Unit> Handle(UpdateFillColorCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
            throw new InvalidOperationException("No document loaded.");

        var markerIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var elementId in request.ElementIds)
        {
            var element = editorState.Document.FindById(elementId)
                ?? throw new InvalidOperationException($"Element '{elementId}' not found.");

            var attr = element.GetForegroundColorAttribute();
            element.Attributes[attr] = request.Color;

            CollectReferencedMarkerIds(element, markerIds);
        }

        if (markerIds.Count > 0)
        {
            UpdateMarkerColors(editorState.Document.Elements, markerIds, request.Color);
        }

        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }

    internal static void CollectReferencedMarkerIds(SvgElement element, HashSet<string> markerIds)
    {
        foreach (var key in new[] { "marker-start", "marker-mid", "marker-end" })
        {
            if (element.Attributes.TryGetValue(key, out var value))
            {
                var id = ParseUrlReference(value);
                if (!string.IsNullOrEmpty(id))
                    markerIds.Add(id);
            }
        }
    }

    internal static string? ParseUrlReference(string value)
    {
        // Parses "url(#id)" → "id"
        var trimmed = value.Trim();
        if (trimmed.StartsWith("url(#", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
            return trimmed[5..^1].Trim();
        return null;
    }

    internal static void UpdateMarkerColors(List<SvgElement> elements, HashSet<string> markerIds, string color)
    {
        foreach (var element in elements)
        {
            if (element is SvgUnknown { Tag: "defs" } defs && !string.IsNullOrEmpty(defs.InnerXml))
            {
                defs.InnerXml = UpdateMarkersInXml(defs.InnerXml, markerIds, color);
            }
            else if (element is SvgGroup group)
            {
                UpdateMarkerColors(group.Children, markerIds, color);
            }
        }
    }

    internal static string UpdateMarkersInXml(string innerXml, HashSet<string> markerIds, string color)
    {
        try
        {
            var wrapper = XElement.Parse($"<wrapper xmlns='{SvgNs}'>{innerXml}</wrapper>");
            var changed = false;

            foreach (var marker in wrapper.Elements())
            {
                var localName = marker.Name.LocalName;
                if (!string.Equals(localName, "marker", StringComparison.Ordinal))
                    continue;

                var id = marker.Attribute("id")?.Value;
                if (id is null || !markerIds.Contains(id))
                    continue;

                foreach (var child in marker.Elements())
                {
                    // Update fill on marker children (the shapes that form the arrowhead)
                    child.SetAttributeValue("fill", color);
                    changed = true;

                    // Also update stroke if present on marker children
                    if (child.Attribute("stroke") is not null)
                    {
                        child.SetAttributeValue("stroke", color);
                    }
                }
            }

            if (changed)
            {
                return string.Concat(wrapper.Nodes().Select(n => n.ToString()));
            }
        }
        catch
        {
            // If parsing fails, leave the InnerXml unchanged
        }

        return innerXml;
    }
}
