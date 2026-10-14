using System.Collections.Concurrent;

namespace LiteMediator
{
    internal class Mediator : IMediator
    {
        // Wrappers are created once per (request, response) / notification type and then reused.
        private static readonly ConcurrentDictionary<(Type Request, Type Response), object> RequestWrappers = new();
        private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> NotificationWrappers = new();

        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var wrapper = (RequestHandlerWrapper<TResponse>)RequestWrappers.GetOrAdd(
                (request.GetType(), typeof(TResponse)),
                static key => Activator.CreateInstance(
                    typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(key.Request, key.Response))!);

            return await wrapper.Handle(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
        }

        public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
        {
            if (notification is null) throw new ArgumentNullException(nameof(notification));

            var wrapper = NotificationWrappers.GetOrAdd(
                notification.GetType(),
                static type => (NotificationHandlerWrapper)Activator.CreateInstance(
                    typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(type))!);

            await wrapper.Handle(notification, _serviceProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
