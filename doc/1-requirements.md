# UniversalBFF Requirements

## Abstract

UniversalBFF is an ASP.NET Core based backend-for-frontend composition layer for U-Shell applications and UJMW-based API gateways. It hosts the U-Shell frontend shell, discovers backend and frontend modules, exposes module metadata as portfolio descriptions, and wires module backend services into HTTP endpoints.

The project exists to keep product-specific BFF applications small while reusing established SmartStandards capabilities for discovery, remote communication, ambience, authentication, logging, persistence abstractions, and U-Shell portfolio modeling.

## Motivation

Modern composite applications often need a lightweight server-side composition point that can assemble independently developed frontend modules, backend services, and product definitions into one runnable application. UniversalBFF provides this composition point for U-Shell based frontends.

The core motivation is to avoid rewriting cross-cutting infrastructure in every product. A UniversalBFF-based application should mainly define modules, product-specific metadata, business services, and minimal host configuration, while reusable standards provide logging, token handling, module discovery, UJMW controller generation, and UShell portfolio contracts.

## Target Audiences

- Developers building U-Shell based composite web applications.
- Developers exposing typed UJMW service contracts through an ASP.NET Core host.
- Module authors who want to package frontend metadata, embedded web assets, and backend services as discoverable extensions.
- Framework maintainers evolving SmartStandards, UJMW, UShell, ComponentDiscovery, and related integration patterns.

## Active Project Goals

UniversalBFF should:

- Host the U-Shell frontend bundle as the default web application.
- Provide UShell portfolio and module descriptions for one or more products.
- Discover frontend module providers and backend service providers through ComponentDiscovery.
- Allow modules to register `ModuleDescription` metadata.
- Allow modules to register embedded or externally hosted frontend extensions.
- Allow modules to register typed backend service contracts as dynamic UJMW HTTP endpoints.
- Support ASP.NET Core based hosting as a primary runtime model.
- Support an in-memory WebView2 host for desktop-style scenarios where the ASP.NET Core pipeline is bridged into a local WebView.
- Keep product-specific composition code small by relying on reusable SmartStandards capabilities.

## Functional Requirements

### Module Discovery And Registration

- The application must provide a central type discovery mechanism for locating module providers.
- Implementations of `IFrontendModuleProvider` must be discoverable and invoked during module loading.
- Implementations of `IBackendServiceProvider` must be discoverable and invoked during module loading.
- A frontend module provider must be able to register one or more UShell `ModuleDescription` instances.
- A backend service provider must be able to register typed backend service contracts.

### Portfolio Composition

- The BFF must expose portfolio descriptions consumed by U-Shell.
- A product definition must be able to select enabled modules and a landing workspace.
- If no explicit product definition is available, the system should remain runnable through a fallback/default portfolio.
- Multiple products should be representable through product definitions and portfolio entries.

### Frontend Hosting

- The BFF must host the U-Shell frontend bundle.
- Modules must be able to register embedded frontend assets under module-specific mount points.
- Modules must be able to reference externally hosted frontend applications.
- Static file hosting must support default documents and SPA deep-link fallback behavior where configured.

### Backend Service Hosting

- Modules must be able to expose typed service contracts through UJMW dynamic controllers.
- UJMW routes must be deterministic and module-scoped.
- The default endpoint route shape is `{moduleScopingKey}/api/v{apiV}/{endpointAlias}`.
- UJMW authentication header evaluation should be delegated to SmartStandards AuthTokenHandling.

### Security And Authentication

- Authentication/token interpretation should be handled by SmartStandards AuthTokenHandling rather than product-local token parsing.
- A portfolio security provider may contribute authentication-related portfolio configuration.
- Anonymous access metadata may be represented in portfolio descriptions.
- The current implementation must remain honest about incomplete security-related paths and must not document TODO code as complete behavior.

### Logging And Diagnostics

- Logging should use SmartStandards Logging where runtime code emits diagnostic events.
- Startup diagnostics should provide enough information to understand base URL, working directory, discovered providers, and missing optional providers.
- Documentation must not require manual maintenance of `doc/changelog.md`; release history is generated by the versioning process.

## Non-Functional Requirements

- The system should keep product-specific code minimal and push reusable concerns into shared standards.
- Runtime wiring must respect dependency ordering between discovery, logging, ambience, authentication, UJMW, static hosting, and application readiness.
- Unit tests must not accidentally reach real external infrastructure.
- Documentation must be clear about MVP or incomplete areas.
- Public documentation must be written in English.
- Cross-cutting behavior such as token parsing, discovery, logging, and remote service invocation should not be reimplemented locally when an applicable reusable capability exists.

## Constraints

- Current main target frameworks include `.NET 8` and `.NET 10`; some workspace context also includes `.NET Framework 4.8`.
- The repository uses shared projects for common source across framework-specific wrapper projects.
- The changelog is build/versioning maintained and must not be edited manually.
- The repository currently has minimal human-readable documentation and an MVP-style implementation state in several areas.
- Static API documentation is not maintained by default; generated API descriptions such as Swagger/OpenAPI are preferred where practical.

## Boundaries And Non-Goals

UniversalBFF is not intended to:

- Replace U-Shell as the frontend shell.
- Replace UJMW as the typed HTTP/RPC communication mechanism.
- Replace SmartStandards AuthTokenHandling, SmartAmbience, ComponentDiscovery, or SmartStandards Logging.
- Provide a complete enterprise runtime initializer by itself.
- Hide unfinished implementation areas behind documentation that implies completeness.
- Maintain release notes manually in `doc/changelog.md`.

## Current MVP Limitations

The current repository state contains several incomplete or experimental areas:

- Some OOB modules are skeletons or commented examples rather than active modules.
- Server command registration is declared but not implemented.
- Some portfolio selection and security paths contain TODO or fallback logic.
- Automated tests are currently minimal.
- Human-readable documentation is being introduced after the initial implementation.

These limitations are part of the current project state and should be improved deliberately through future committed requirements.
