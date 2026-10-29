# UniversalBFF

## About

UniversalBFF is an ASP.NET Core based backend-for-frontend composition layer for U-Shell applications and UJMW-based API gateways. It hosts the U-Shell frontend shell, discovers frontend and backend modules, exposes portfolio metadata, and wires typed backend services into module-scoped HTTP endpoints.

The repository contains the reusable BFF core, module contracts, ASP.NET Core integration, executable host projects, a WebView2 host, out-of-box module projects, and tests.

## Motivation

UniversalBFF exists to keep product-specific backend-for-frontend applications small. Product code should primarily define modules, product metadata, and individual business services, while reusable SmartStandards capabilities provide discovery, typed remote communication, ambience, authentication, logging, repository abstractions, and U-Shell portfolio contracts.

## Examples

- Host the U-Shell frontend bundle from an ASP.NET Core application.
- Discover module providers through ComponentDiscovery.
- Register a module-specific frontend extension with embedded static files.
- Expose a typed UJMW service contract under a deterministic route such as `{moduleScopingKey}/api/v1/{endpointAlias}`.
- Compose one or more U-Shell portfolios from product definitions and registered modules.
- Run the same shared ASP.NET Core pipeline through a WebView2 desktop-style host.

## Differentiation

UniversalBFF is not a generic web framework and does not try to replace U-Shell, UJMW, SmartAmbience, AuthTokenHandling, ComponentDiscovery, or SmartStandards Logging. Its role is to compose these capabilities into a small backend-for-frontend host for modular U-Shell applications.

The target architecture favors reusable standards over product-local infrastructure. Module authors register metadata and services; the host handles discovery, portfolio exposure, static frontend hosting, and UJMW endpoint wiring.

## Documentation

- [Requirements](doc/requirements.md)
- [Architecture](doc/architecture.md)
- [Quickstart](doc/quickstart.md)
- [Ecosystem](doc/ecosystem.md)
- [User Management as OAuth server and proxy](doc/user-management-oauth.md)
- [Changelog](doc/changelog.md)
- [Epics](doc/epics/) (e.g. [future directions](doc/epics/epic-future-directions.md))
- [Work: backlog](doc/work/backlog.md) and [tech debt](doc/work/tech-debt.md)
