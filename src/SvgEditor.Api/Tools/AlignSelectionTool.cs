using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tools;

public sealed class AlignSelectionTool : ISvgTool
{
    public string Name => "AlignSelection";

    public object Execute(EditorContext context, IReadOnlyList<string> selection)
    {
        return new AlignSelectionCommand { Alignment = "center" };
    }
}
