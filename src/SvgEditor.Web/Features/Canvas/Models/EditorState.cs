namespace SvgEditor.Web.Features.Canvas.Models;

public sealed class EditorState
{
    public SvgDocument? Document { get; set; }
    public string? SelectedElementId { get; set; }

    public event Action? OnStateChanged;

    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}
