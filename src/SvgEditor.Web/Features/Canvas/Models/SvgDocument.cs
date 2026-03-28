namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class SvgDocument
{
    public string? ViewBox { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public List<SvgElement> Elements { get; init; } = [];
    public Dictionary<string, string> Attributes { get; init; } = [];

    public SvgElement? FindById(string id) => FindInList(Elements, id);

    private static SvgElement? FindInList(List<SvgElement> elements, string id)
    {
        foreach (var element in elements)
        {
            if (element.Id == id) return element;
            if (element is SvgGroup group)
            {
                var found = FindInList(group.Children, id);
                if (found is not null) return found;
            }
        }
        return null;
    }

    public SvgDocument DeepClone() => new SvgDocument
    {
        ViewBox = ViewBox,
        Width = Width,
        Height = Height,
        Elements = Elements.Select(e => e.DeepClone()).ToList(),
        Attributes = new Dictionary<string, string>(Attributes)
    };

    public SvgDocument RemoveElements(IReadOnlyCollection<string> ids)
    {
        var newElements = RemoveFromList(Elements, ids);
        return new SvgDocument
        {
            ViewBox = ViewBox,
            Width = Width,
            Height = Height,
            Elements = newElements,
            Attributes = new Dictionary<string, string>(Attributes)
        };
    }

    private static List<SvgElement> RemoveFromList(List<SvgElement> elements, IReadOnlyCollection<string> ids)
    {
        var result = new List<SvgElement>(elements.Count);
        foreach (var element in elements)
        {
            if (ids.Contains(element.Id)) continue;
            if (element is SvgGroup group)
            {
                result.Add(new SvgGroup
                {
                    Id = group.Id,
                    Attributes = new Dictionary<string, string>(group.Attributes),
                    Children = RemoveFromList(group.Children, ids)
                });
            }
            else
            {
                result.Add(element);
            }
        }
        return result;
    }

    public SvgDocument ReplaceElement(SvgElement updated)
    {
        var newElements = ReplaceInList(Elements, updated);
        return new SvgDocument
        {
            ViewBox = ViewBox,
            Width = Width,
            Height = Height,
            Elements = newElements,
            Attributes = new Dictionary<string, string>(Attributes)
        };
    }

    private static List<SvgElement> ReplaceInList(List<SvgElement> elements, SvgElement updated)
    {
        var result = new List<SvgElement>(elements.Count);
        foreach (var element in elements)
        {
            if (element.Id == updated.Id)
            {
                result.Add(updated);
            }
            else if (element is SvgGroup group)
            {
                result.Add(new SvgGroup
                {
                    Id = group.Id,
                    Attributes = new Dictionary<string, string>(group.Attributes),
                    Children = ReplaceInList(group.Children, updated)
                });
            }
            else
            {
                result.Add(element);
            }
        }
        return result;
    }
}
