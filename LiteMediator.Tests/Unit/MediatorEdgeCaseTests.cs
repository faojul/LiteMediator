using FluentAssertions;
using FluentValidation;
using LiteMediator.Tests.Shared;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LiteMediator.Tests.Unit
{
    public class MediatorEdgeCaseTests
    {
        private static ServiceProvider BuildProvider(List<string> called, Action<IServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(called);
            services.AddLiteMediator(typeof(TestHandler).Assembly);
            configure?.Invoke(services);
            return services.BuildServiceProvider();
        }

        // ---------- internal (non-public) types ----------

        [Fact]
        public async Task Send_Should_Work_With_Internal_Request_And_Handler()
        {
            using var provider = BuildProvider(new List<string>());
            var mediator = provider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new InternalRequest { Name = "World" });

            result.Should().Be("Internal World");
        }

        [Fact]
        public async Task Publish_Should_Work_With_Internal_NotificationHandler()
        {
            var called = new List<string>();
            using var provider = BuildProvider(called);
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Publish(new InternalNotification());

            called.Should().Contain("Internal");
        }

        [Fact]
        public async Task Pipeline_Behaviors_Should_Wrap_Internal_Requests()
        {
            var called = new List<string>();
            using var provider = BuildProvider(called);
            var mediator = provider.GetRequiredService<IMediator>();

            await mediator.Send(new InternalRequest { Name = "x" });

            // BehaviorA / BehaviorB (PipelineBehaviorSequenceTests) are open generics picked up by scanning
            called.Should().Contain(new[] { "Before A", "After A", "Before B", "After B" });
        }

        [Fact]
        public void RegisterHandlers_Should_Register_Internal_Handlers()
        {
            var services = new ServiceCollection();
            services.RegisterHandlersWithMediator(typeof(InternalHandler).Assembly);

            using var sp = services.BuildServiceProvider();
            var handler = sp.GetRequiredService<IRequestHandler<InternalRequest, string>>();

            handler.Should().BeOfType<InternalHandler>();
        }

        // ---------- registration behaviour ----------

        [Fact]
        public void Open_Generic_Handlers_Should_Not_Break_The_Container()
        {
            var services = new ServiceCollection();
            services.RegisterHandlersWithMediator(typeof(OpenGenericHandler<>).Assembly);

            Action act = () =>
            {
                using var sp = services.BuildServiceProvider();
                sp.GetRequiredService<IRequestHandler<TestRequest, string>>();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public async Task Registering_Same_Assembly_Twice_Should_Not_Duplicate_Notification_Handlers()
        {
            var called = new List<string>();
            var services = new ServiceCollection();
            services.AddSingleton(called);
            services.AddLiteMediator(Assembly.GetExecutingAssembly());
            services.AddLiteMediator(Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly());

            using var provider = services.BuildServiceProvider();
            await provider.GetRequiredService<IMediator>().Publish(new TestNotification());

            called.Should().BeEquivalentTo(new[] { "First", "Second" });
        }

        [Fact]
        public async Task ValidationBehavior_Should_Be_Registered_Automatically()
        {
            // Only the *test* assembly is scanned here, so ValidationBehavior<,> (library assembly)
            // can only be present because AddLiteMediator registers it itself.
            using var provider = BuildProvider(new List<string>(),
                s => s.AddScoped<IValidator<TestRequest>, TestValidator>());
            var mediator = provider.GetRequiredService<IMediator>();

            var ex = await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(new TestRequest { Name = "" }));

            ex.Errors.Should().Contain("Name must not be empty.");
        }

        // ---------- error handling ----------

        [Fact]
        public async Task Send_Should_Throw_InvalidOperation_When_Handler_Is_Not_Registered()
        {
            var services = new ServiceCollection();
            services.AddLiteMediator(typeof(IMediator).Assembly); // library only: no handlers
            using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            Func<Task> act = () => mediator.Send(new TestRequest { Name = "x" });

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*TestRequest*");
        }

        [Fact]
        public async Task Send_Should_Throw_When_Request_Is_Null()
        {
            using var provider = BuildProvider(new List<string>());
            var mediator = provider.GetRequiredService<IMediator>();

            Func<Task> act = () => mediator.Send<string>(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task Publish_Should_Throw_When_Notification_Is_Null()
        {
            using var provider = BuildProvider(new List<string>());
            var mediator = provider.GetRequiredService<IMediator>();

            Func<Task> act = () => mediator.Publish(null!);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task Publish_Without_Handlers_Should_Complete()
        {
            var services = new ServiceCollection();
            services.AddLiteMediator(typeof(IMediator).Assembly);
            using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            Func<Task> act = () => mediator.Publish(new TestNotification());

            await act.Should().NotThrowAsync();
        }
    }
}
