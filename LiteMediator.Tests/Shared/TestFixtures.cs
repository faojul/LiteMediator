using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteMediator.Tests.Shared
{
    public class TestRequest : IRequest<string>
    {
        public string Name { get; set; } = "";
    }

    public class TestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
            => Task.FromResult($"Hello {request.Name}");
    }

    public class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name must not be empty.");
        }
    }

    public class TestNotification : INotification { }

    public class FirstHandler : INotificationHandler<TestNotification>
    {
        private readonly List<string> _called;

        public FirstHandler(List<string> called)
        {
            _called = called;
        }

        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            _called.Add("First");
            return Task.CompletedTask;
        }
    }

    public class SecondHandler : INotificationHandler<TestNotification>
    {
        private readonly List<string> _called;

        public SecondHandler(List<string> called)
        {
            _called = called;
        }

        public Task Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            _called.Add("Second");
            return Task.CompletedTask;
        }
    }


    public class TestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
            => next();
    }

    // ---- Non-public types: these must work exactly like public ones ----

    internal class InternalRequest : IRequest<string>
    {
        public string Name { get; set; } = "";
    }

    internal class InternalHandler : IRequestHandler<InternalRequest, string>
    {
        public Task<string> Handle(InternalRequest request, CancellationToken cancellationToken)
            => Task.FromResult($"Internal {request.Name}");
    }

    internal class InternalNotification : INotification { }

    internal class InternalNotificationHandler : INotificationHandler<InternalNotification>
    {
        private readonly List<string> _called;

        public InternalNotificationHandler(List<string> called)
        {
            _called = called;
        }

        public Task Handle(InternalNotification notification, CancellationToken cancellationToken)
        {
            _called.Add("Internal");
            return Task.CompletedTask;
        }
    }

    // ---- Open-generic handler: must be skipped by auto-registration, not break the container ----

    public class OpenGenericRequest<T> : IRequest<T> { }

    public class OpenGenericHandler<T> : IRequestHandler<OpenGenericRequest<T>, T>
    {
        public Task<T> Handle(OpenGenericRequest<T> request, CancellationToken cancellationToken)
            => Task.FromResult(default(T)!);
    }
}
