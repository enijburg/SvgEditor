using Microsoft.Extensions.DependencyInjection;
using SvgEditor.Web.Shared.Mediator;

namespace SvgEditor.Web.Tests.Shared.Mediator;

[TestClass]
public sealed class MediatorTests
{
    private sealed record TestRequest(int Value) : IRequest<int>;

    private sealed class TestHandler : IRequestHandler<TestRequest, int>
    {
        public Task<int> Handle(TestRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(request.Value * 2);
    }

    [TestMethod]
    public async Task Send_DispatchesToRegisteredHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, int>, TestHandler>();
        services.AddScoped<IMediator, SvgEditor.Web.Shared.Mediator.Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>()!;

        var result = await mediator.Send(new TestRequest(5));

        Assert.AreEqual(10, result);
    }

    [TestMethod]
    public async Task Send_ThrowsWhenNoHandlerRegistered()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMediator, SvgEditor.Web.Shared.Mediator.Mediator>();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>()!;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => mediator.Send(new TestRequest(1)));
    }
}
