using Microsoft.Extensions.Logging;
using SvgEditor.Web.Features.Canvas.Models;
using SvgEditor.Web.Shared.Storage;

namespace SvgEditor.Web.Features.Canvas.CanvasPage;

public sealed class AutoSaveService : IDisposable
{
    private readonly EditorState _editorState;
    private readonly IStorageService _storageService;
    private readonly ILogger<AutoSaveService> _logger;
    private Timer? _debounceTimer;
    private const int DebounceMs = 1500;

    public AutoSaveService(EditorState editorState, IStorageService storageService, ILogger<AutoSaveService> logger)
    {
        _editorState = editorState;
        _storageService = storageService;
        _logger = logger;
        _editorState.OnStateChanged += OnStateChanged;
    }

    private void OnStateChanged()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(SaveCallback, null, DebounceMs, Timeout.Infinite);
    }

    // async void is required for Timer callback compatibility.
    // The try-catch ensures unhandled exceptions do not crash the application.
    private async void SaveCallback(object? state)
    {
        try
        {
            if (_editorState.Document is not null)
            {
                await _storageService.SetAsync(StorageKeys.ActiveDocument, _editorState.Document);
                _logger.LogDebug("Document auto-saved to storage.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save document.");
        }
    }

    public void Dispose()
    {
        _editorState.OnStateChanged -= OnStateChanged;
        _debounceTimer?.Dispose();
    }
}
