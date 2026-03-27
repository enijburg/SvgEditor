using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SvgEditor.Web.Shared.Validation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services, Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var validatorInterface = typeof(IValidator<>);
        var validatorTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorInterface)
                .Select(i => (Implementation: t, Interface: i)));

        foreach (var (implementation, @interface) in validatorTypes)
        {
            services.AddScoped(@interface, implementation);
        }

        return services;
    }
}
