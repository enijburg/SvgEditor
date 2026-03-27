using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SvgEditor.Web.Shared.Mediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var handlerInterface = typeof(IRequestHandler<,>);
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (Implementation: t, Interface: i)));

        foreach (var (implementation, @interface) in handlerTypes)
        {
            services.AddScoped(@interface, implementation);
        }

        services.AddScoped<IMediator, Mediator>();
        return services;
    }
}
