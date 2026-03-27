using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Features.Copilot.Models;

namespace SvgEditor.Web.Features.Copilot.Services;

public sealed class EditorContextBuilder(EditorState editorState)
{
    private int _version;

    public string CurrentVersion => Volatile.Read(ref _version).ToString(System.Globalization.CultureInfo.InvariantCulture);

    public void IncrementVersion() => Interlocked.Increment(ref _version);

    public CopilotEditorContext Build()
    {
        var doc = editorState.Document;
        if (doc is null)
        {
            return new CopilotEditorContext
            {
                DocumentId = "current",
                DocumentVersion = CurrentVersion,
                Canvas = new CopilotCanvasSize { Width = 0, Height = 0 },
                Selection = [],
                Elements = []
            };
        }

        var selectedIds = editorState.SelectedElementIds.ToList();
        var elements = new List<CopilotElementSummary>();

        foreach (var id in selectedIds)
        {
            var element = doc.FindById(id);
            if (element is not null)
            {
                elements.Add(MapElement(element));
            }
        }

        return new CopilotEditorContext
        {
            DocumentId = "current",
            DocumentVersion = CurrentVersion,
            Canvas = new CopilotCanvasSize { Width = doc.Width, Height = doc.Height },
            Selection = selectedIds,
            Elements = elements
        };
    }

    private static CopilotElementSummary MapElement(SvgElement element)
    {
        var bbox = element.GetBoundingBox();
        return new CopilotElementSummary
        {
            Id = element.Id,
            Type = element.Tag,
            X = bbox?.X,
            Y = bbox?.Y,
            Width = bbox?.Width,
            Height = bbox?.Height,
            Fill = element.Attributes.GetValueOrDefault("fill"),
            Stroke = element.Attributes.GetValueOrDefault("stroke")
        };
    }
}
