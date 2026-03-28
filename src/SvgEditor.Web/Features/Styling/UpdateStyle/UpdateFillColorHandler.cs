using System.Xml.Linq;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Features.Styling.UpdateStyle;

public sealed class UpdateFillColorHandler(EditorState editorState) : IRequestHandler<UpdateFillColorCommand, Unit>
{
    private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";
    private static readonly string[] MarkerAttributes = ["marker-start", "marker-mid", "marker-end"];

    public Task<Unit> Handle(UpdateFillColorCommand request, CancellationToken cancellationToken = default)
    {
        if (editorState.Document is null)
            throw new InvalidOperationException("No document loaded.");

        var markerIds = new HashSet<string>(StringComparer.Ordinal);
        var selectedElements = new List<SvgElement>();

        foreach (var elementId in request.ElementIds)
        {
            var element = editorState.Document.FindById(elementId)
                ?? throw new InvalidOperationException($"Element '{elementId}' not found.");

            var attr = element.GetForegroundColorAttribute();
            element.Attributes[attr] = request.Color;

            CollectReferencedMarkerIds(element, markerIds);
            selectedElements.Add(element);
        }

        if (markerIds.Count > 0)
        {
            // Determine which markers are shared with non-selected elements
            var sharedMarkerIds = FindSharedMarkerIds(
                editorState.Document.Elements, markerIds, request.ElementIds);

            if (sharedMarkerIds.Count > 0)
            {
                // Clone shared markers and re-point selected elements to the clones
                var idMapping = CloneSharedMarkers(
                    editorState.Document.Elements, sharedMarkerIds);

                // Update selected elements' marker attributes to reference cloned markers
                foreach (var element in selectedElements)
                {
                    RemapMarkerReferences(element, idMapping);
                }

                // Only color the cloned markers (originals stay unchanged)
                var clonedIds = new HashSet<string>(
                    idMapping.Values, StringComparer.Ordinal);
                UpdateMarkerColors(editorState.Document.Elements, clonedIds, request.Color);

                // Also color any non-shared markers that only we reference
                var exclusiveIds = new HashSet<string>(
                    markerIds.Except(sharedMarkerIds), StringComparer.Ordinal);
                if (exclusiveIds.Count > 0)
                {
                    UpdateMarkerColors(editorState.Document.Elements, exclusiveIds, request.Color);
                }
            }
            else
            {
                // No sharing — update markers in place
                UpdateMarkerColors(editorState.Document.Elements, markerIds, request.Color);
            }
        }

        editorState.NotifyStateChanged();
        return Task.FromResult(Unit.Value);
    }

    internal static void CollectReferencedMarkerIds(SvgElement element, HashSet<string> markerIds)
    {
        foreach (var key in MarkerAttributes)
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

    /// <summary>
    /// Finds marker IDs from <paramref name="markerIds"/> that are also referenced
    /// by elements NOT in <paramref name="selectedElementIds"/>.
    /// </summary>
    internal static HashSet<string> FindSharedMarkerIds(
        List<SvgElement> elements,
        HashSet<string> markerIds,
        IReadOnlyCollection<string> selectedElementIds)
    {
        var selectedSet = new HashSet<string>(selectedElementIds, StringComparer.Ordinal);
        var shared = new HashSet<string>(StringComparer.Ordinal);
        CollectSharedMarkerIds(elements, markerIds, selectedSet, shared);
        return shared;
    }

    private static void CollectSharedMarkerIds(
        List<SvgElement> elements,
        HashSet<string> markerIds,
        HashSet<string> selectedSet,
        HashSet<string> shared)
    {
        foreach (var element in elements)
        {
            if (element is SvgGroup group)
            {
                CollectSharedMarkerIds(group.Children, markerIds, selectedSet, shared);
                continue;
            }

            if (selectedSet.Contains(element.Id))
                continue;

            foreach (var key in MarkerAttributes)
            {
                if (element.Attributes.TryGetValue(key, out var value))
                {
                    var id = ParseUrlReference(value);
                    if (id is not null && markerIds.Contains(id))
                        shared.Add(id);
                }
            }
        }
    }

    /// <summary>
    /// Clones each marker whose ID is in <paramref name="sharedMarkerIds"/> within defs XML,
    /// appending the clone with a new unique ID. Returns a mapping of original ID → cloned ID.
    /// </summary>
    internal static Dictionary<string, string> CloneSharedMarkers(
        List<SvgElement> elements,
        HashSet<string> sharedMarkerIds)
    {
        var mapping = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            if (element is SvgUnknown { Tag: "defs" } defs && !string.IsNullOrEmpty(defs.InnerXml))
            {
                defs.InnerXml = CloneMarkersInXml(defs.InnerXml, sharedMarkerIds, mapping);
            }
            else if (element is SvgGroup group)
            {
                var childMapping = CloneSharedMarkers(group.Children, sharedMarkerIds);
                foreach (var (k, v) in childMapping)
                    mapping[k] = v;
            }
        }

        return mapping;
    }

    internal static string CloneMarkersInXml(
        string innerXml,
        HashSet<string> sharedMarkerIds,
        Dictionary<string, string> mapping)
    {
        try
        {
            var wrapper = XElement.Parse($"<wrapper xmlns='{SvgNs}'>{innerXml}</wrapper>");
            var clones = new List<XElement>();

            foreach (var marker in wrapper.Elements())
            {
                if (!string.Equals(marker.Name.LocalName, "marker", StringComparison.Ordinal))
                    continue;

                var id = marker.Attribute("id")?.Value;
                if (id is null || !sharedMarkerIds.Contains(id))
                    continue;

                var newId = $"{id}-{Guid.NewGuid():N}";
                var clone = new XElement(marker);
                clone.SetAttributeValue("id", newId);
                clones.Add(clone);
                mapping[id] = newId;
            }

            foreach (var clone in clones)
            {
                wrapper.Add(clone);
            }

            if (clones.Count > 0)
            {
                return string.Concat(wrapper.Nodes().Select(n => n.ToString()));
            }
        }
        catch
        {
            // If parsing fails, leave unchanged
        }

        return innerXml;
    }

    /// <summary>
    /// Re-points an element's marker-start/mid/end from original IDs to cloned IDs.
    /// </summary>
    internal static void RemapMarkerReferences(
        SvgElement element,
        Dictionary<string, string> idMapping)
    {
        foreach (var key in MarkerAttributes)
        {
            if (element.Attributes.TryGetValue(key, out var value))
            {
                var id = ParseUrlReference(value);
                if (id is not null && idMapping.TryGetValue(id, out var newId))
                {
                    element.Attributes[key] = $"url(#{newId})";
                }
            }
        }
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
