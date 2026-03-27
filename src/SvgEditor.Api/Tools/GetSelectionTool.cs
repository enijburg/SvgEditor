using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tools;

public sealed class GetSelectionTool : ISvgTool
{
    public string Name => "GetSelection";

    public object Execute(EditorContext context, IReadOnlyList<string> selection)
    {
        return new
        {
            selectedIds = context.Selection,
            elements = context.Elements
                .Where(e => context.Selection.Contains(e.Id))
                .ToList()
        };
    }
}
