using SvgEditor.Api.Contracts;

namespace SvgEditor.Api.Tools;

public interface ISvgTool
{
    string Name { get; }
    object Execute(EditorContext context, IReadOnlyList<string> selection);
}
