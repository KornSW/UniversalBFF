# UniversalBFF Quickstart

## Overview

This quickstart explains how to build and run UniversalBFF locally, how the main host projects relate to each other, and how a module integrates with the BFF at a high level.

UniversalBFF is primarily an ASP.NET Core based backend-for-frontend for U-Shell. It can run as a normal ASP.NET Core web host or through the WebView2 host that bridges an in-memory ASP.NET Core pipeline into a desktop shell.

## Prerequisites

Use a development environment that can build the target frameworks used by the repository. The current solution contains projects targeting `.NET 8` and `.NET 10`, and the workspace context may also contain `.NET Framework 4.8` projects.

Recommended tools:

- Visual Studio with the relevant .NET SDK workloads installed.
- A .NET SDK capable of building the selected target framework.
- Local NuGet package restore access for the SmartStandards, UJMW, UShell, FUSE-fx, and Microsoft dependencies referenced by the projects.

## Solution Entry Points

Open the solution:

```text
dotnet/UniversalBFF.sln
```

Important projects:

| Project | Purpose |
|---|---|
| `UniversalBFF.AspHost.net10.0` | Main ASP.NET Core executable host for the current .NET 10 path. |
| `UniversalBFF.AspHost.net8.0` | ASP.NET Core executable host for the .NET 8 path. |
| `UniversalBFF.WebViewHost.net10.0` | WinForms/WebView2 host using the shared ASP.NET Core pipeline in-memory. |
| `UniversalBFF.AspNetCore.net10.0` | Reusable ASP.NET Core integration library. |
| `UniversalBFF.net10.0` | Core BFF library. |
| `UniversalBFF.ModuleContract.net10.0` | Module contract library. |
| `UniversalBFF.OobModules.UserManagement` | Current most complete out-of-box module. |

## Build

From the repository root, build the solution with the normal .NET tooling or through Visual Studio:

```text
dotnet build dotnet/UniversalBFF.sln
```

If the installed SDK does not support a target framework used by the solution, build the matching project variant available in the environment or install the required SDK.

## Run The ASP.NET Core Host

The primary web-host entry point is:

```text
dotnet/src/UniversalBFF.AspHost.net10.0/UniversalBFF.AspHost.net10.0.csproj
```

Typical local launch profiles are defined in:

```text
dotnet/src/UniversalBFF.AspHost.net10.0/Properties/launchSettings.json
```

The default local URLs are currently:

```text
http://localhost:5202
https://localhost:7251
```

When the host starts, it delegates service setup and pipeline setup to the shared `UniversalBFF.AspNetCore` layer. The U-Shell frontend is mounted at the configured base URL, usually `/`.

## Run The WebView Host

The WebView host entry point is:

```text
dotnet/src/UniversalBFF.WebViewHost.net10.0/UniversalBFF.WebViewHost.net10.0.csproj
```

This host starts a WinForms application, creates a WebView2 shell, and runs the shared ASP.NET Core pipeline in-memory through a test server bridge. It is useful when the same BFF and U-Shell composition should be presented as a local desktop-style application.

## Configuration Basics

The main ASP.NET Core host configuration is in:

```text
dotnet/src/UniversalBFF.AspHost.net10.0/appsettings.json
```

Common settings:

| Setting | Description |
|---|---|
| `BaseUrl` | Application base path. Use `/` for root hosting. |
| `PluginDir` | Optional plugin directory added as an assembly resolve path. |
| `ProdMode` | When false, developer exception pages can be enabled. |
| `EnableSwaggerUi` | Enables Swagger UI support where host packages configure it. |
| `OAuthClientIdForSwaggerUi` | OAuth client ID used for Swagger UI. |
| `OAuthScopeExpressionForSwaggerUi` | OAuth scope expression used for Swagger UI. |
| `Logging:SmartStandards` | SmartStandards Logging configuration. |

## How Module Loading Works

UniversalBFF loads modules through discovery:

1. `BffApplication` exposes a central ComponentDiscovery type indexer.
2. `ModuleLoader` searches for `IFrontendModuleProvider` implementations.
3. Each frontend module provider registers UShell module descriptions and optional frontend assets.
4. `ModuleLoader` searches for `IBackendServiceProvider` implementations.
5. Each backend service provider registers backend services through the backend registrar.
6. The ASP.NET Core registrar maps registered UJMW services to dynamic controller routes.

The route pattern for a registered UJMW service is:

```text
{moduleScopingKey}/api/v{apiV}/{endpointAlias}
```

## Creating A Module

A typical module project should reference the matching `UniversalBFF.ModuleContract` target package/project and implement one or both provider interfaces:

- `IFrontendModuleProvider`
- `IBackendServiceProvider`

A frontend module provider normally registers:

- a `ModuleDescription`,
- workspaces,
- commands,
- usecases,
- optional embedded or externally hosted frontend extensions.

A backend service provider normally registers:

- typed UJMW service contracts,
- service factories,
- repository endpoints where appropriate.

The User Management OOB module is the current best in-repository example of a module that registers both frontend metadata and backend services.

## Embedded Frontend Assets

A module can register embedded frontend files through `RegisterFrontendExtension`. The embedded namespace must match the generated manifest resource names.

Important details:

- Folder separators become dots in embedded resource namespaces.
- Namespaces are case-sensitive.
- Characters such as `-` or spaces may be transformed by project/resource naming rules.
- Incorrect embedded namespace values often result in files simply not being found.

## Product Definitions And Portfolios

A product definition controls:

- the technical product name,
- display title,
- optional logos,
- enabled modules,
- landing workspace,
- metadata used by the portfolio chooser and application scopes.

If no product definition is discovered or configured, UniversalBFF currently creates a fallback default product so the application remains runnable.

## Swagger And API Discovery

The ASP.NET Core host references Swagger/OpenAPI-related packages and configures SmartStandards-flavored Swagger support. Generated API descriptions are preferred over manually maintained API documentation.

Do not add static `doc/api.md` documentation unless the repository explicitly commits to maintaining it.

## Testing

The repository contains an MSTest project:

```text
dotnet/test/UniversalBFF.Tests/UniversalBFF.Tests.csproj
```

Current test coverage is minimal. When adding tests, avoid accidental calls to real external infrastructure. Prefer mockable services, in-memory hosts, or explicitly controlled test infrastructure.

## Documentation Rules

- Keep human-readable documentation in English.
- Keep `README.md` links relative.
- Do not manually edit `doc/changelog.md`; it is generated by the versioning pipeline.
- Update `doc/1-requirements.md` when behavior requirements change.
- Update `doc/2-architecture.md` when major structure or data flow changes.
- Update this quickstart when setup, run, or module-development workflows change.
- Update the project AI skill when implementation constraints or maintenance pitfalls change.
