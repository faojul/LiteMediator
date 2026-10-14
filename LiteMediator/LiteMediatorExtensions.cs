using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace LiteMediator
{
    public static class LiteMediatorExtensions
    {
        // For monolithic applications, layered architecture, or simple projects
        public static IServiceCollection AddLiteMediator(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (assemblies is null) throw new ArgumentNullException(nameof(assemblies));

            foreach (var assembly in assemblies.Distinct())
            {
                services.RegisterHandlersWithMediator(assembly);
                services.RegisterBehaviorsWithMediator(assembly);
            }

            services.AddCoreServices();
            return services;
        }

        // For modular monoliths / microservices: register shared + module-specific handlers separately
        public static IServiceCollection AddLiteMediatorCore(this IServiceCollection services, Assembly sharedAssembly)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (sharedAssembly is null) throw new ArgumentNullException(nameof(sharedAssembly));

            services.RegisterHandlersWithMediator(sharedAssembly);
            services.RegisterBehaviorsWithMediator(sharedAssembly);
            services.AddCoreServices();
            return services;
        }

        public static IServiceCollection AddLiteMediatorModule(this IServiceCollection services, Assembly moduleAssembly)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (moduleAssembly is null) throw new ArgumentNullException(nameof(moduleAssembly));

            services.RegisterHandlersWithMediator(moduleAssembly);
            return services;
        }

        private static void AddCoreServices(this IServiceCollection services)
        {
            services.TryAddScoped<IMediator, Mediator>();

            // The built-in FluentValidation behavior is registered automatically (as documented in the README).
            // It is added after the scanned behaviors so user behaviors (logging, etc.) wrap around it.
            // With no validators registered it just calls next(). TryAddEnumerable prevents duplicates.
            services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));
        }
    }
}
