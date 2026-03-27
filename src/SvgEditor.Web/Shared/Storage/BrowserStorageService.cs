using System.Text.Json;
using Microsoft.JSInterop;

namespace SvgEditor.Web.Shared.Storage;

public sealed class BrowserStorageService(IJSRuntime jsRuntime) : IStorageService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await jsRuntime.InvokeAsync<string?>("svgEditorStorage.getItem", key);
        if (json is null) return default;
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        await jsRuntime.InvokeVoidAsync("svgEditorStorage.setItem", key, json);
    }

    public async Task RemoveAsync(string key)
    {
        await jsRuntime.InvokeVoidAsync("svgEditorStorage.removeItem", key);
    }
}
