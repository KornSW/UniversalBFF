---
name: most-important-details-for-universalbff
description: Project-specific operational knowledge for maintaining UniversalBFF safely and consistently.
---

# Most Important Details For UniversalBFF

## Purpose

This skill captures project-specific operational knowledge for UniversalBFF. It complements the human-readable documentation in `README.md` and `doc/` with implementation details, maintenance constraints, known incomplete areas, and safe modification guidance for AI-assisted development.

Human-readable documentation remains authoritative for product intent, externally visible behavior, and supported workflows. This skill adds deeper maintenance knowledge and must not contradict the documentation.

## Repository Role

UniversalBFF is an ASP.NET Core backend-for-frontend composition layer for U-Shell applications and UJMW-based API gateways.

The project composes reusable SmartStandards ecosystem capabilities:

- ComponentDiscovery for provider/type discovery.
- UJMW for typed dynamic HTTP/RPC endpoints.
- SmartStandards AuthTokenHandling for token interpretation and OAuth-related integration.
- SmartAmbience for ambient context flow and ASP.NET Core middleware support.
- SmartStandards Logging for diagnostics.
- UShell for portfolio, module, command, workspace, and usecase contracts.
- FUSE-fx for repository abstractions used by data-oriented services.

Do not recreate these cross-cutting capabilities locally unless a real gap has been identified and discussed.

## Documentation Rules

When changing UniversalBFF, keep these representations synchronized where affected:

- `README.md`
- `doc/1-requirements.md`
- `doc/2-architecture.md`
- `doc/3-quickstart.md`
- `doc/ideas.md`
- this skill

Do not manually edit `doc/changelog.md`; it is maintained by the versioning/build process.

Do not create static API documentation such as `doc/api.md` unless the developer explicitly opts in. Prefer generated Swagger/OpenAPI descriptions for technical API contracts.

All human-readable documentation files in `doc/` and `README.md` must be written in English.

## Solution Structure

Important project areas:

| Area | Maintenance meaning |
|---|---|
| `dotnet/src/UniversalBFF` | Shared core source for `BffApplication`, `ModuleLoader`, and `ModuleRegistrar`. |
| `dotnet/src/UniversalBFF.net8.0` and `dotnet/src/UniversalBFF.net10.0` | Framework-specific wrapper projects importing the core shared project. |
| `dotnet/src/UniversalBFF.ModuleContract` | Shared module contract source. |
| `dotnet/src/UniversalBFF.ModuleContract.net8.0` and `dotnet/src/UniversalBFF.ModuleContract.net10.0` | Framework-specific wrapper projects for module contracts. |
| `dotnet/src/UniversalBFF.AspNetCore` | Shared ASP.NET Core integration source. |
| `dotnet/src/UniversalBFF.AspNetCore.net8.0` and `dotnet/src/UniversalBFF.AspNetCore.net10.0` | Framework-specific wrapper projects for ASP.NET Core integration. |
| `dotnet/src/UniversalBFF.AspHost.net8.0` and `dotnet/src/UniversalBFF.AspHost.net10.0` | Executable ASP.NET Core hosts. |
| `dotnet/src/UniversalBFF.WebViewHost.net10.0` | WinForms/WebView2 in-memory ASP.NET Core host. |
| `dotnet/src/UniversalBFF.OobModules.*` | Out-of-box modules and module candidates. |
| `dotnet/test/UniversalBFF.Tests` | MSTest test project. |

The repository uses shared projects (`.shproj`/`.projitems`) heavily. When changing shared source, verify the effect on every wrapper project that imports it.

## Core Discovery Model

`BffApplication` is a singleton that owns:

- an `AssemblyIndexer`,
- a lazy `TypeIndexer`,
- discoverable static accessors for both.

`ModuleLoader.Load()` uses `BffApplication.Current.TypeIndexer.GetApplicableTypes<T>(true)` to find:

- `IFrontendModuleProvider`,
- `IBackendServiceProvider`.

Provider instances are currently created with `Activator.CreateInstance`. Avoid assuming constructor dependency injection for module providers unless the provider lifecycle is deliberately changed.

## Module Registration Model

A module may implement one or both provider interfaces:

- `IFrontendModuleProvider.RegisterModule(IFrontendModuleRegistrar registrar)`
- `IBackendServiceProvider.RegisterServices(IBackendServiceRegistrar registrar)`

Frontend providers can register:

- UShell `ModuleDescription` objects,
- embedded frontend extensions,
- externally hosted frontend URLs.

Backend providers can register:

- typed UJMW service endpoints,
- UJMW proxies,
- repository endpoints or other service contracts where appropriate.

The active in-repository example is `UniversalBFF.OobModules.UserManagement.UserManagementModuleProvider`.

## ModuleRegistrar Details

`ModuleRegistrar` is the central composition object. It collects module registrations and implements portfolio-facing behavior.

Important fields and responsibilities include:

- `_RegisteredModules` for collected UShell module descriptions.
- `_ModuleFileRegistrationMethods` for delayed frontend/static file registrations.
- `_FrontendExtensionUrlsByAlias` for externally hosted frontend extensions.
- `_PortfoliosPerName` and `_PortfolioEntries` for composed portfolio state.
- `_BaseUrl` for application base path handling.
- `_SecurityProvider`, `_TenancyProvider`, and `_ProductDefinitionProvider` for product/security/tenancy integration.

Static frontend registrations are delayed because module discovery happens before the ASP.NET Core static hosting pipeline is configured. Later, `CollectAndRegisterFrontendExtensionsTo` executes the captured registrations against an `IStaticHostingRegistrar`.

## ASP.NET Core Integration

The shared ASP.NET Core implementation is in `dotnet/src/UniversalBFF.AspNetCore/Programm.cs`.

Startup responsibilities include:

1. Register SmartStandards Logging.
2. Read `BaseUrl`, output directory, and optional `PluginDir`.
3. Add `PluginDir` to assembly resolving when configured.
4. Hook `InstanceDiscoveryContext` ambience management.
5. Link dependency injection to instance discovery.
6. Discover or fall back for portfolio/security/product providers.
7. Create `AspModuleRegistrar`.
8. Run `ModuleLoader.Load()`.
9. Register portfolio services and controllers.
10. Configure UJMW authentication header evaluation.
11. Configure Swagger, authentication, and authorization.
12. Configure U-Shell and module static hosting in `OnRunApplication`.

Be careful when reordering this flow. Discovery, logging, authentication, ambience, UJMW setup, and static hosting have lifecycle dependencies.

## UJMW Endpoint Routing

`AspModuleRegistrar.RegisterUjmwServiceEndpoint` registers a service factory and adds a dynamic UJMW controller.

The route shape is:

```text
{moduleScopingKey}/api/v{apiV}/{endpointAlias}
```

`AspModuleRegistrar.BuildEndpointRoute` returns the same shape. Keep route shape changes synchronized with documentation and module/client expectations. A route-shape change is likely a breaking change.

UJMW authentication header evaluation is wired through:

```text
UjmwHostConfiguration.AuthHeaderEvaluator = AccessTokenValidator.TryValidateHttpAuthHeader
```

Token semantics belong to SmartStandards AuthTokenHandling, not to business services.

## Static Frontend Hosting

Static hosting is configured through:

- `SetupSpaMultiHosting`,
- `StaticFileConsolidatorIApp`,
- `MountRegistration`,
- `DefaultDocumentConfig`.

The consolidator collects mounts/defaults and then applies:

- default files,
- static files,
- SPA fallback mappings.

Duplicate mount registration throws an exception intentionally to keep hosting deterministic.

Embedded frontend namespace values are fragile and case-sensitive. Incorrect namespaces often result in missing files rather than clear compile-time errors. When module frontend files are moved or renamed, check resource namespace values carefully.

## Product And Portfolio Composition

`IProductDefinitionProvider.GetProductDefinitions()` returns product definitions. Product definitions control:

- technical product name,
- title,
- logos,
- enabled modules,
- landing workspace,
- metadata.

When no product definitions exist, `ModuleRegistrar.EnsurePortfolioIsInitialized` currently creates a fallback/default product. This keeps the host runnable but should not be mistaken for a complete multi-product implementation.

Known portfolio limitations:

- `IPortfolioService.GetPortfolioIndex()` currently returns a default entry.
- `IPortfolioService.GetPortfolioDescription(string portfolioName)` currently returns the default portfolio.
- Multi-product behavior requires careful completion before being documented as finished.

## Security State

Security integration exists conceptually through:

- `IPortfolioSecurityProvider`,
- SmartStandards AuthTokenHandling,
- OAuth service support,
- anonymous/authenticated access metadata in portfolio descriptions.

Current implementation contains incomplete and bypassed paths, including early return behavior in security-related portfolio configuration. Do not document these paths as complete production security behavior until implemented and tested.

## Out-Of-Box Modules

### User Management

`UniversalBFF.OobModules.UserManagement` is the most complete OOB module. It registers:

- `IUserManagementService`,
- `ILocalCredentialManagementService`,
- `IRepository<LocalCredentialEntity, Int64>`,
- UShell module/workspace/usecase metadata,
- embedded frontend assets under `oob-usrmgmt`.

Use this as the primary current module example.

### Diagnostics, FileStore, ModuleManager

These projects currently contain skeleton or commented example provider code. Treat them as planned or placeholder module areas, not finished capabilities.

### Edmx

The EDMX project contains model/generated files and does not currently act like the active module-provider based OOB modules.

## WebView Host Details

`UniversalBFF.WebViewHost.net10.0` hosts the shared ASP.NET Core pipeline in-memory and bridges requests into WebView2.

Important details:

- It uses a custom scheme by default (`app://local`).
- It uses ASP.NET Core `TestServer` to create an `HttpClient` for virtual request handling.
- WebView2 JSON serialization support currently adds NewtonsoftJson support due to a documented System.Text.Json/WebView2 issue.
- UI-hosted tests can be flaky; prefer non-UI tests unless the WebView behavior is explicitly being changed.

## Known Incomplete Areas

Be explicit and honest about these areas:

- `RegisterServerCommands` throws `NotImplementedException`.
- `RegisterBackendExtension<TServiceContract>` is currently empty.
- Some portfolio methods still return default values regardless of input.
- Some security integration paths are incomplete or bypassed.
- Several OOB module projects are skeletons.
- Tests are currently minimal.

When completing any of these areas, update the relevant human-readable docs and this skill.

## Testing Guidance

Current tests are minimal. When adding behavior, consider tests for:

- module discovery and provider invocation,
- UJMW route generation,
- portfolio fallback creation,
- product-definition handling,
- static mount normalization,
- duplicate static mount detection,
- SPA fallback behavior.

Tests must not accidentally call real external infrastructure. Use in-memory hosts, explicit fakes, or test-only services where possible.

## Change Classification Notes

Documentation-only and skill-only commits should end with `[skip ci]` according to the cowork-process skill.

Changing UJMW route shapes, public contracts, configuration names, module loading behavior, or supported host workflows may be a breaking change. Adding a new completed module or supported workflow may be a new feature. Internal documentation updates and clarifications are normal descriptive changes.
