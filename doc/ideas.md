# UniversalBFF Ideas And Future Directions

This document collects possible future improvements for UniversalBFF. Items listed here are not active requirements unless they are explicitly promoted into `doc/1-requirements.md` and implemented through a committed change.

## Documentation

- Add deeper examples for writing a custom module once the module authoring workflow is stable.
- Add a full product-definition example with multiple products, landing workspaces, logos, metadata, and enabled modules.
- Add a static architecture diagram if the project structure stabilizes enough to justify one.
- Consider generated API documentation examples if the Swagger/OpenAPI setup becomes part of the supported user workflow.

## Module System

- Complete `RegisterServerCommands` support and document how server-side commands should be exposed to U-Shell.
- Complete or remove `RegisterBackendExtension<TServiceContract>` so the public contract surface matches implemented behavior.
- Define lifecycle expectations for module provider instantiation beyond the current `Activator.CreateInstance` model.
- Clarify whether module providers should support dependency injection directly or remain simple discoverable classes.
- Add deterministic conflict handling for duplicate module UIDs, module scoping keys, command keys, workspace keys, and usecase keys.

## Portfolio And Product Definitions

- Complete portfolio index handling for multiple products instead of returning only a default entry.
- Complete product-specific portfolio lookup instead of always returning the default portfolio.
- Harden fallback product creation and make its behavior thread-safe.
- Improve portfolio chooser behavior for multi-product deployments.
- Add validation for product definitions, enabled module references, landing workspace references, and metadata shape.

## Security And Authentication

- Complete the currently bypassed portfolio security provider integration path.
- Document supported authentication flows once the runtime behavior is stable.
- Clarify the relationship between anonymous access metadata, authenticated access metadata, runtime tags, token scopes, and workspace/usecase/command authorization.
- Provide safe development defaults that do not hide production security requirements.

## Static Frontend Hosting

- Add diagnostics for missing embedded resources and invalid embedded namespaces.
- Review base URL normalization for application roots and nested hosting scenarios.
- Add tests for duplicate mount handling, default documents, SPA fallback behavior, and placeholder replacement in embedded JavaScript files.
- Clarify cache-control behavior for embedded frontend assets.

## Out-Of-Box Modules

- Implement the Diagnostics module as a real module or remove misleading skeleton code.
- Implement the FileStore module as a real module if file/blob storage becomes a committed requirement.
- Implement the ModuleManager module if runtime module inspection or management becomes a committed requirement.
- Clarify the role of the EDMX project and whether it should become a modern module, a migration aid, or be removed.
- Expand the User Management module with documented configuration, migrations, storage setup, and integration tests.

## Runtime Composition

- Decide whether UniversalBFF needs a small explicit runtime initializer role or whether the current ASP.NET Core startup composition remains sufficient.
- Make bootstrap diagnostics, logging setup, ambience setup, token handling, UJMW setup, and application readiness ordering explicit if startup complexity grows.
- Add health/readiness endpoints only if they become a real supported deployment requirement.
- Consider service discovery and self-announcement integration if UniversalBFF is deployed as part of a distributed runtime.

## Testing

- Replace the empty smoke test with meaningful tests for module loading and registrar behavior.
- Add tests for UJMW route generation.
- Add tests for fallback product/portfolio creation.
- Add tests for static hosting normalization and duplicate mount detection.
- Add WebView host tests only where they can run reliably without UI/environment flakiness.

## Packaging And Versioning

- Review whether package metadata should be normalized across all framework-specific wrapper projects.
- Review the relationship between `net8.0` and `net10.0` packages as the project evolves.
- Keep using generated changelog/versioning instead of manually editing release history.

## Developer Experience

- Add a minimal sample module project once the module contract is stable.
- Add a template or checklist for module authors.
- Add better logging around provider discovery and skipped/failed provider activation.
- Improve startup messages so a developer can quickly see which modules, portfolios, services, and static mounts were registered.
