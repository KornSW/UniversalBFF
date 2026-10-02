# UniversalBFF Architecture

## Abstract

UniversalBFF is a modular ASP.NET Core backend-for-frontend for U-Shell applications. Its architecture centers on discovering modules, collecting their frontend and backend registrations, exposing U-Shell portfolio metadata, and hosting both static frontend assets and UJMW-based backend service endpoints.

The system follows a capability-first model: product code should compose reusable standards rather than rebuilding discovery, token handling, logging, remote communication, or portfolio modeling infrastructure locally.

## High-Level Structure

The solution is organized into shared source projects and framework-specific wrapper projects.

| Area | Role |
|---|---|
| `UniversalBFF` | Core composition model, application singleton, module loading, module registrar, portfolio service implementation. |
| `UniversalBFF.ModuleContract` | Contracts exposed to modules, including frontend module providers, backend service providers, registrars, product definitions, and static hosting abstractions. |
| `UniversalBFF.AspNetCore` | ASP.NET Core integration layer, service registration, middleware/pipeline setup, UJMW controller wiring, UShell and static frontend hosting. |
| `UniversalBFF.AspHost` | Executable ASP.NET Core host projects for direct web hosting. |
| `UniversalBFF.WebViewHost` | WinForms/WebView2 host that runs the ASP.NET Core pipeline in-memory and bridges WebView requests into it. |
| `UniversalBFF.OobModules.*` | Out-of-box module projects. User Management is the most complete current module; other modules are currently skeleton or model-oriented areas. |
| `UniversalBFF.Tests` | MSTest-based test project, currently minimal. |

## Core Runtime Flow

At startup, the ASP.NET Core integration creates the composition objects and loads modules:

1. SmartStandards Logging is registered.
2. Optional plugin assembly resolve paths can be added through configuration.
3. Instance discovery ambience is connected to a shared `InstanceDiscoveryContext`.
4. Optional providers such as `IPortfolioSecurityProvider` are discovered.
5. A product definition provider is selected, with a file-based fallback when discovery does not provide one.
6. An ASP.NET-specific `ModuleRegistrar` is created.
7. `ModuleLoader.Load()` discovers module providers and invokes them.
8. The registrar is added to dependency injection as portfolio service and module registrar.
9. U-Shell portfolio controllers, MVC controllers, UJMW controllers, authentication, authorization, Swagger support, and static frontend hosting are configured.
10. The ASP.NET Core pipeline runs with static hosting, routing, CORS, authentication, authorization, and controller mapping.

## ComponentDiscovery Role

`BffApplication` owns a central `AssemblyIndexer` and lazy `TypeIndexer`. These are exposed as discoverable instances and are used to find module provider types.

`ModuleLoader` queries the type indexer for:

- `IFrontendModuleProvider`
- `IBackendServiceProvider`

Each discovered frontend provider receives an `IFrontendModuleRegistrar`. Each discovered backend provider receives an `IBackendServiceRegistrar`. Provider instances are currently created through `Activator.CreateInstance`, so discoverable providers are expected to be instantiable in that model unless this lifecycle is changed later.

## Module Contract Layer

The module contract layer defines the extension points modules use to integrate with the BFF:

- `IFrontendModuleProvider` registers frontend/module metadata.
- `IBackendServiceProvider` registers backend services.
- `IFrontendModuleRegistrar` accepts UShell module descriptions and frontend extension registrations.
- `IBackendServiceRegistrar` accepts backend endpoint registrations.
- `IStaticHostingRegistrar` abstracts static frontend mount registration.
- `IProductDefinitionProvider` supplies product definitions that drive portfolio composition.

This contract layer is intentionally small. Modules should not need to know the internal ASP.NET Core hosting details to register their capabilities.

## Module Registrar

`ModuleRegistrar` is the central composition object. It collects registrations and implements portfolio-facing behavior.

Its responsibilities include:

- keeping the configured application base URL,
- collecting `ModuleDescription` registrations,
- collecting delayed static frontend registrations,
- registering embedded or externally hosted frontend extensions,
- exposing backend registration APIs implemented by host-specific subclasses,
- composing portfolio descriptions from product definitions and registered modules,
- integrating security, tenancy, and application-scope information where available.

`AspModuleRegistrar` provides the ASP.NET Core-specific implementation for UJMW endpoint registration. It maps a typed service contract to a dynamic UJMW controller route and registers a service factory in dependency injection.

## UJMW Backend Endpoint Architecture

UniversalBFF uses UJMW dynamic controllers for typed HTTP/RPC-style service contracts.

The default route shape is:

```text
{moduleScopingKey}/api/v{apiV}/{endpointAlias}
```

For example, the User Management module registers contracts under the `oob-usrmgmt` module scope. The route is built by `AspModuleRegistrar.BuildEndpointRoute` and by the dynamic controller options in `AspModuleRegistrar.RegisterUjmwServiceEndpoint`.

Authentication header evaluation is delegated to SmartStandards AuthTokenHandling through `UjmwHostConfiguration.AuthHeaderEvaluator = AccessTokenValidator.TryValidateHttpAuthHeader`.

## U-Shell And Portfolio Hosting

UniversalBFF hosts the U-Shell frontend bundle and exposes portfolio/module metadata consumed by U-Shell.

The main U-Shell frontend is registered as a static file provider at the application root. Portfolio data is exposed through UShell portfolio hosting support. Product definitions control which modules are enabled and which workspace is used as the landing workspace.

When explicit product definitions are missing, UniversalBFF currently creates a fallback/default product so the application remains runnable and can still expose available module metadata.

## Static Frontend Hosting

Static hosting is configured through `SetupSpaMultiHosting` and `StaticFileConsolidatorIApp`.

The static hosting flow is:

1. Create a consolidator for the ASP.NET Core application builder.
2. Register the root U-Shell bundle.
3. Register module frontend extensions collected by the module registrar.
4. Apply the consolidated registrations.

The consolidator registers default files, static files, and SPA fallback endpoints for configured mounts. It throws on duplicate mounts to keep static hosting deterministic.

Embedded frontend assets are exposed through an embedded bundle file provider. Module authors must use the correct embedded resource namespace; these namespaces are case-sensitive and can be hard to diagnose when incorrect.

## Host Models

### ASP.NET Core Host

`UniversalBFF.AspHost` is the direct ASP.NET Core executable host. It builds a `WebApplication`, applies branch-specific configuration, supports launch profile URL selection, delegates service setup to the shared ASP.NET Core layer, and runs the application.

### WebView Host

`UniversalBFF.WebViewHost` is a WinForms/WebView2 host. It creates an in-memory ASP.NET Core host through `TestServer` and bridges WebView2 requests into that pipeline. This allows the same shared BFF and U-Shell hosting logic to run in a local desktop-style shell.

## Out-Of-Box Modules

### User Management

`UniversalBFF.OobModules.UserManagement` is currently the most complete OOB module. It registers:

- user management service contracts,
- local credential management service contracts,
- a FUSE-fx repository endpoint for local credential entities,
- a UShell module description with administration workspace/usecases,
- embedded frontend assets under the `oob-usrmgmt` module scope.

### Diagnostics, FileStore, ModuleManager

These projects currently contain skeleton or commented module-provider examples. They indicate planned module areas but should not be documented as finished runtime modules until implemented.

### Edmx

The EDMX module project contains an Entity Data Model and generated/model files. It is model-oriented and does not currently follow the same active module-provider pattern as User Management.

## Configuration

Common host configuration includes:

| Setting | Purpose |
|---|---|
| `BaseUrl` | Application base path, usually `/`. |
| `PluginDir` | Optional directory added as assembly resolve path for plugin/module discovery. |
| `ProdMode` | Controls whether developer exception pages are enabled. |
| `EnableSwaggerUi` | Enables Swagger UI support where configured by the host packages. |
| `OAuthClientIdForSwaggerUi` | OAuth client ID used by Swagger UI configuration. |
| `OAuthAuthorizeUrlForSwaggerUi_` | Optional authorization URL setting pattern for Swagger UI. |
| `OAuthScopeExpressionForSwaggerUi` | Optional Swagger UI OAuth scope expression. |

SmartStandards Logging is configured through the normal `Logging:SmartStandards` configuration section.

## Cross-Cutting Capabilities

| Capability | Architectural Use |
|---|---|
| ComponentDiscovery | Locates module and backend service provider types. |
| UJMW | Exposes typed backend contracts as dynamic HTTP endpoints and can create dynamic clients. |
| SmartStandards AuthTokenHandling | Owns token evaluation and OAuth-related integration. |
| SmartAmbience | Provides ambient context flow and MVC middleware integration. |
| SmartStandards Logging | Provides runtime diagnostics and ASP.NET Core logging integration. |
| UShell | Defines frontend shell, portfolio descriptions, module metadata, commands, workspaces, and usecases. |
| FUSE-fx | Provides repository abstractions used by data-oriented module services. |

## Current Architectural Limitations

Some areas are intentionally documented as incomplete current state:

- `RegisterServerCommands` is declared but not implemented.
- `RegisterBackendExtension<TServiceContract>` exists but does not currently add meaningful behavior.
- Some portfolio index/description code still returns default values and contains TODO markers.
- Some security integration paths are currently bypassed or incomplete.
- Several OOB module projects are skeletons.
- Test coverage is very limited.

Future changes should either complete these areas or keep them listed as ideas rather than treating them as active documented behavior.
