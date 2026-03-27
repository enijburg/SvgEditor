using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tools;

public sealed class SetFillTool : ISvgTool
{
    public string Name => "SetFill";

    public object Execute(EditorContext context, IReadOnlyList<string> selection)
    {
        return selection
            .Where(id => context.Elements.Any(e => e.Id == id))
            .Select(id => new SetFillCommand { ElementId = id, Fill = "#0000FF" })
            .ToList();
    }
}
