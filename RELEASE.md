## ✅ Release Checklist for LiteMediator

- [ ] All features for this version are complete
- [ ] `dotnet test` passes locally (`dotnet test -p:IncludeNet11=true` if the .NET 11 SDK is installed)
- [ ] `README.md` is up to date (supported frameworks, new features, breaking changes)
- [ ] Version is updated in `LiteMediator/LiteMediator.csproj` (e.g. `2.1.0`) — **the tag must match it**, CI fails the publish step otherwise
- [ ] Commit and push to `main`, wait for the **Build, Test & Publish** workflow to go green
- [ ] Create and push the Git tag: `git tag v2.1.0 && git push origin v2.1.0`
- [ ] CI re-runs build + tests on net6.0 … net11.0, then publishes to NuGet and creates the GitHub release (with auto-generated notes)
- [ ] Confirm the package is available on [nuget.org](https://www.nuget.org/packages/LiteMediator.Lite) and lists every target framework
- [ ] Edit the GitHub release notes if you want to highlight breaking changes

### Supported target frameworks
`net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0`, `net11.0`

- `net11.0` is only built when `IncludeNet11=true` (CI sets it; see `Directory.Build.props`).
  Once .NET 11 is GA, make it the default and remove the `dotnet-quality: "preview"` line from the workflow.
- When a new .NET version ships: add it to `Directory.Build.props` and to the `setup-dotnet` steps in `.github/workflows/nuget-publish.yml`.
- FluentAssertions is intentionally pinned to 6.x (8.x+ is commercially licensed) and FluentValidation to 11.x (12.x drops net6/net7).

### Required repository secret
`LITEMEDIATOR_API_KEY` — NuGet API key with push permission for `LiteMediator.Lite`.

### Changelog
#### 2.1.0
- Added `net9.0`, `net10.0` and `net11.0` targets
- **Fix:** `internal` requests, handlers and notification handlers now work (the mediator no longer uses `dynamic`)
- Faster `Send`/`Publish` (cached typed wrappers instead of dynamic dispatch)
- `ValidationBehavior<,>` is now registered automatically by `AddLiteMediator` / `AddLiteMediatorCore`
- Registration is idempotent (calling `AddLiteMediator` twice no longer duplicates handlers); open-generic handler classes are skipped instead of breaking the container
- Clear `InvalidOperationException` when no handler is registered; `ArgumentNullException` for null requests/notifications
- Depends on `Microsoft.Extensions.DependencyInjection.Abstractions` instead of the full DI package
