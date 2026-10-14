# LiteMediator

A super-lightweight, high-performance alternative to MediatR for .NET 6 → .NET 11, with CI/CD via GitHub Actions and FluentValidation integration built in.

[![Build Status](https://github.com/faojul/LiteMediator/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/faojul/LiteMediator/actions/workflows/nuget-publish.yml)
[![NuGet Version](https://img.shields.io/nuget/v/LiteMediator.Lite.svg?style=flat-square)](https://www.nuget.org/packages/LiteMediator.Lite/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A lightweight .NET mediator library designed for clean CQRS architecture and fast performance.

---

## Why LiteMediator?
- Minimal overhead — only depends on FluentValidation and `Microsoft.Extensions.DependencyInjection.Abstractions`
- Full Clean Architecture & CQRS support
- Integrates FluentValidation out of the box
- Easy setup — one line to replace MediatR
- Works with `public` **and** `internal` requests and handlers
- Tested on every supported target framework in CI

## 🎯 Supported frameworks

| .NET 6 | .NET 7 | .NET 8 | .NET 9 | .NET 10 | .NET 11 |
| ------ | ------ | ------ | ------ | ------- | ------- |
| ✅      | ✅      | ✅      | ✅      | ✅       | ✅       |

  
## ✨ Features

| Feature                                  | Supported | Description                                               |
| ---------------------------------------- | --------- | --------------------------------------------------------- |
| `IMediator.Send()`                       | ✅         | Core command/request dispatching                          |
| `IRequest<TResponse>`                    | ✅         | Request/command abstraction                               |
| `IRequestHandler<TRequest, TResponse>`   | ✅         | Custom request handler logic (public or internal)         |
| `IPipelineBehavior<TRequest, TResponse>` | ✅         | Middleware-style behavior support ([customizing](#-customizing-validation-behavior))                        |
| `ValidationBehavior` (FluentValidation)  | ✅         | Registered automatically ([guide](#-customizing-validation-behavior))              |
| `INotification` + `Publish()`            | ✅         | Event publishing to multiple handlers                     |
| `INotificationHandler<T>`                | ✅         | Handle published domain events                            |
| `RegisterHandlersWithMediator()`         | ✅         | Auto-registration of all request/notification handlers    |
| `RegisterBehaviorsWithMediator()`        | ✅         | Auto-registration of pipeline behaviors                   |
| `AddLiteMediator()` extensions           | ✅         | Simplified registration for monoliths & modular monoliths |


---

## 🚀 Super Easy MediatR Replacement
✅ All your existing code can stay unchanged:
- `Send(request)` ✔️
- `Publish(notification)` ✔️
- `IRequest<T>` ✔️
- `IRequestHandler<T>` ✔️
- `INotification` ✔️
- `INotificationHandler<T>`  ✔️

The only change is in `Program.cs / Startup.cs`:
````csharp
// Replace this:
services.AddMediatR(...);

// With this:
services.AddLiteMediator(Assembly.GetExecutingAssembly());
````


## 📦 Installation

```bash
dotnet add package LiteMediator.Lite
````

## 🧩 How to Use


**1. Startup Setup**  
   Add `services.AddLiteMediator(...)` in your Program.cs or Startup.cs.


```csharp
services.AddLiteMediator(Assembly.GetExecutingAssembly());
````
   Or, Multiple Assemblies
```csharp
//You can pass multiple assemblies in one go
services.AddLiteMediator(
    typeof(SharedModule.SomeType).Assembly,
    typeof(ModuleA.SomeHandler).Assembly,
    typeof(ModuleB.SomeHandler).Assembly
);

````
    
This registers:
   - All `IRequestHandler`s and `INotificationHandler`s (including `internal` ones)
   - All `IPipelineBehavior`s found in the assemblies
   - The built-in `ValidationBehavior<,>` (FluentValidation)
   - `IMediator` service

Calling it more than once with the same assembly is safe — nothing is registered twice.

✅ Minimal setup for monolithic or layered apps.



**2. Modular Support**  
   Use `AddLiteMediatorCore()` and `AddLiteMediatorModule()` for clean modular monoliths.

```csharp
// Shared module
services.AddLiteMediatorCore(typeof(Shared.AssemblyMarker).Assembly);

// Module1
services.AddLiteMediatorModule(typeof(Module1.AssemblyMarker).Assembly);

// Module2
services.AddLiteMediatorModule(typeof(Module2.AssemblyMarker).Assembly);
````
    
    
| Method                    | Usage                                             |
| ------------------------- | ------------------------------------------------- |
| `AddLiteMediator()`       | For monoliths                                     |
| `AddLiteMediatorCore()`   | Register shared `Mediator + Behaviors + Handlers` |
| `AddLiteMediatorModule()` | Register only module-specific handlers            |



**3. Validation**  
   Just register your FluentValidation validators—they’ll run automatically.

   1. Add your validators as normal:
    
````csharp

public class MyCommandValidator : AbstractValidator<MyCommand>
{
    public MyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
````
    
   2. Register them in DI (if not auto-registered):
    
````csharp
services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
````
✅ Your validators will automatically be executed before calling the handler, thanks to the ValidationBehavior. A failure throws `LiteMediator.ValidationException` (see `.Errors`).


## 🔍 Example

   **1. Sending a Request**
      
````csharp

public class HelloRequest : IRequest<string>
{
    public string Name { get; set; }
}

public class HelloHandler : IRequestHandler<HelloRequest, string>
{
    public Task<string> Handle(HelloRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"Hello, {request.Name}");
}
````

````csharp
var result = await mediator.Send(new HelloRequest { Name = "World" });
// => "Hello, World"
````

   **2. Publishing a Notification**
````csharp
public record UserCreated(string Name) : INotification;

public class SendWelcomeEmail : INotificationHandler<UserCreated>
{
    public Task Handle(UserCreated notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Welcome email sent to {notification.Name}");
        return Task.CompletedTask;
    }
}
````

````csharp
await mediator.Publish(new UserCreated("Alice"));
````

Notification handlers run sequentially, in registration order.

## 🛠 Customizing Validation Behavior
LiteMediator includes `ValidationBehavior<TRequest, TResponse>` using FluentValidation.
You can extend it to add logic, or implement `IPipelineBehavior` to replace it entirely.

   **1. Extending `ValidationBehavior`**

````csharp
using FluentValidation;
using LiteMediator;

public class MyCustomValidationBehavior<TRequest, TResponse>
    : ValidationBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public MyCustomValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        : base(validators) { }

    public override async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Custom before validation");
        var result = await base.Handle(request, next, cancellationToken);
        Console.WriteLine("Custom after validation");
        return result;
    }
}

````

   **2. Implementing `IPipelineBehavior` from Scratch**
````csharp
using LiteMediator;

public class MyNoOpValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        Console.WriteLine("Skipping validation");
        return next();
    }
}

````
  Registration:
````csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MyNoOpValidationBehavior<,>));
````

Behaviors run in registration order: the first registered behavior is the outermost one.

## 🔒 Internal types & visibility
Requests, handlers and notification handlers may be `internal`, so a feature assembly doesn't have to expose its implementation details:

```csharp
internal record GetOrder(int Id) : IRequest<OrderDto>;

internal class GetOrderHandler : IRequestHandler<GetOrder, OrderDto>
{
    public GetOrderHandler(IOrderRepository repo) { /* ... */ }   // constructor must be public
    public Task<OrderDto> Handle(GetOrder request, CancellationToken ct) { /* ... */ }
}
```

Things to keep in mind:
- Pass the assembly that contains the handlers to `AddLiteMediator(...)`.
- The handler's **constructor must be `public`** — that's a Microsoft DI rule, not a LiteMediator one.
- Open-generic handler classes (`class H<T> : IRequestHandler<Req<T>, T>`) are not auto-registered; register them yourself.

## 🧪 Testing
- ✅ Unit + integration tests for Send, Publish, validation pipeline, internal types and registration
- ✅ The test project multi-targets the same frameworks as the library (net6.0 → net11.0)
- ✅ GitHub Actions CI/CD

```bash
dotnet test                         # net6.0 … net10.0 (needs those runtimes installed)
dotnet test -p:IncludeNet11=true    # also net11.0 (needs the .NET 11 SDK)
```


## 🔧 CI/CD
LiteMediator uses GitHub Actions to automatically:
- Build & test on every push and pull request (all target frameworks)
- On a version tag (e.g., `v2.1.0`): verify the tag matches the package version, publish to NuGet, and create a GitHub release

See [RELEASE.md](RELEASE.md) for the checklist.


## 📁 Folder Structure
````
LiteMediator/                  # Main library
LiteMediator.Tests/            # xUnit integration and unit tests
Directory.Build.props          # Shared target frameworks
.github/
  workflows/nuget-publish.yml  # CI/CD pipeline
````

## 📃 License
Licensed under the MIT License.

## Author
**Faojul Ahsan** – Senior .NET Backend Engineer passionate about clean architecture, API-first development, and remote collaboration. Seeking global remote opportunities.
