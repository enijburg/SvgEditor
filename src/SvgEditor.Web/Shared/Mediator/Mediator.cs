using Microsoft.Extensions.DependencyInjection;

namespace SvgEditor.Web.Shared.Mediator;

public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var method = handlerType.GetMethod("Handle")!;
        return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
    }
}
