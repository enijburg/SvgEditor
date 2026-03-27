using SvgEditor.Web.Shared.Storage;

namespace SvgEditor.Web.Tests.Fakes;

public sealed class InMemoryStorageService : IStorageService
{
    private readonly Dictionary<string, string> _store = [];

    public Task<T?> GetAsync<T>(string key)
    {
        if (!_store.TryGetValue(key, out var json)) return Task.FromResult<T?>(default);
        return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(json));
    }

    public Task SetAsync<T>(string key, T value)
    {
        _store[key] = System.Text.Json.JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public bool ContainsKey(string key) => _store.ContainsKey(key);
    public void Clear() => _store.Clear();
}
