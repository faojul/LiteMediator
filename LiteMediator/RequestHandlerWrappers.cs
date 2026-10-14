namespace LiteMediator
{
    // These wrappers let the mediator call handlers through their *public interfaces*
    // (IRequestHandler<,>, IPipelineBehavior<,>, INotificationHandler<>) with real generic
    // types instead of `dynamic`. That is what makes internal request/handler classes work:
    // the dynamic binder can't see members of non-public types, the interfaces are always public.

    internal abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public override Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typedRequest = (TRequest)request;
            var handler = (IRequestHandler<TRequest, TResponse>?)serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>))
                          ?? throw new InvalidOperationException(
                              $"No handler registered for request '{typeof(TRequest)}'. " +
                              "Make sure its assembly is passed to AddLiteMediator(...) and the handler has a public constructor.");

            RequestHandlerDelegate<TResponse> pipeline = () => handler.Handle(typedRequest, cancellationToken);

            var behaviors = ((IEnumerable<IPipelineBehavior<TRequest, TResponse>>?)
                serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>)))?.ToArray()
                ?? Array.Empty<IPipelineBehavior<TRequest, TResponse>>();

            // First registered behavior is the outermost one.
            for (var i = behaviors.Length - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var next = pipeline;
                pipeline = () => behavior.Handle(typedRequest, next, cancellationToken);
            }

            return pipeline();
        }
    }

    internal abstract class NotificationHandlerWrapper
    {
        public abstract Task Handle(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
        where TNotification : INotification
    {
        public override async Task Handle(INotification notification, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var typed = (TNotification)notification;
            var handlers = ((IEnumerable<INotificationHandler<TNotification>>?)
                serviceProvider.GetService(typeof(IEnumerable<INotificationHandler<TNotification>>)))
                ?? Enumerable.Empty<INotificationHandler<TNotification>>();

            foreach (var handler in handlers)
            {
                await handler.Handle(typed, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
