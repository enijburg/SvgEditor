using SvgEditor.Web.Tests.Fakes;

namespace SvgEditor.Web.Tests.Shared.Storage;

[TestClass]
public sealed class InMemoryStorageServiceTests
{
    [TestMethod]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        var svc = new InMemoryStorageService();

        await svc.SetAsync("key1", "hello");
        var result = await svc.GetAsync<string>("key1");

        Assert.AreEqual("hello", result);
    }

    [TestMethod]
    public async Task GetAsync_MissingKey_ReturnsDefault()
    {
        var svc = new InMemoryStorageService();

        var result = await svc.GetAsync<string>("missing");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task RemoveAsync_RemovesKey()
    {
        var svc = new InMemoryStorageService();
        await svc.SetAsync("key1", 42);

        await svc.RemoveAsync("key1");
        var result = await svc.GetAsync<int?>("key1");

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SetAsync_OverwritesExistingValue()
    {
        var svc = new InMemoryStorageService();
        await svc.SetAsync("key1", "first");
        await svc.SetAsync("key1", "second");

        var result = await svc.GetAsync<string>("key1");

        Assert.AreEqual("second", result);
    }
}
