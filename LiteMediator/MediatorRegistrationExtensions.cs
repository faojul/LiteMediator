using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace LiteMediator
{
    internal static class MediatorRegistrationExtensions
    {
        // Note: assembly.GetTypes() already returns internal/private types. Registration was never the
        // problem for non-public classes; the failure used to happen later, when Mediator invoked the
        // handler through `dynamic`. See Mediator / RequestHandlerWrappers.

        public static IServiceCollection RegisterHandlersWithMediator(this IServiceCollection services, Assembly assembly)
        {
            // IRequestHandler<,>  (open-generic handler classes can't be closed automatically, so they're skipped)
            foreach (var (impl, iface) in FindImplementations(assembly, typeof(IRequestHandler<,>)))
            {
                if (impl.IsGenericTypeDefinition) continue;
                services.TryAddEnumerable(ServiceDescriptor.Scoped(iface, impl));
            }

            // INotificationHandler<>
            foreach (var (impl, iface) in FindImplementations(assembly, typeof(INotificationHandler<>)))
            {
                if (impl.IsGenericTypeDefinition) continue;
                services.TryAddEnumerable(ServiceDescriptor.Scoped(iface, impl));
            }

            return services;
        }

        public static IServiceCollection RegisterBehaviorsWithMediator(this IServiceCollection services, Assembly assembly)
        {
            foreach (var (impl, iface) in FindImplementations(assembly, typeof(IPipelineBehavior<,>)))
            {
                if (impl.IsGenericTypeDefinition)
                {
                    // Open generics like ValidationBehavior<,>
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), impl));
                }
                else
                {
                    // Closed generics like LoggingBehavior<CreateUserCommand, string>
                    services.TryAddEnumerable(ServiceDescriptor.Scoped(iface, impl));
                }
            }

            return services;
        }

        private static IEnumerable<(Type Implementation, Type Interface)> FindImplementations(Assembly assembly, Type openInterface)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface) continue;

                foreach (var iface in type.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == openInterface)
                        yield return (type, iface);
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // One broken optional dependency shouldn't prevent registering everything else.
                return ex.Types.OfType<Type>();
            }
        }
    }
}
