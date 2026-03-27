using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tools;

public sealed class MoveSelectionTool : ISvgTool
{
    public string Name => "MoveSelection";

    public object Execute(EditorContext context, IReadOnlyList<string> selection)
    {
        return new MoveSelectionCommand { Dx = 0, Dy = 0 };
    }
}
