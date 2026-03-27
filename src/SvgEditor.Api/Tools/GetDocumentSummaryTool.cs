using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tools;

public sealed class GetDocumentSummaryTool : ISvgTool
{
    public string Name => "GetDocumentSummary";

    public object Execute(EditorContext context, IReadOnlyList<string> selection)
    {
        return new
        {
            documentId = context.DocumentId,
            documentVersion = context.DocumentVersion,
            canvas = context.Canvas,
            elementCount = context.Elements.Count,
            selectionCount = context.Selection.Count
        };
    }
}
