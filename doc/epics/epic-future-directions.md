# Epic: UniversalBFF Future Directions

This epic collects possible future improvements for UniversalBFF. Items listed here are not active requirements unless they are explicitly promoted into [`requirements.md`](../requirements.md) and [`work/backlog.md`](../work/backlog.md) and implemented through a committed change.

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

### Module isolation in composite applications (open topic)

Host and modules are loaded into one process and share one output folder (`dotnet/dist/`) and one assembly load context. Diverging dependency versions therefore collide (first case: `SmartStandards.Logging.Centralized` pulled `FUSE-fx.RepositoryContract` 1.0.2 while the modules need 3.0.2; currently solved by aligning/pinning the versions). Questions to answer:

- Policy first: which shared libraries form the "platform" whose versions are owned and aligned by the host (contracts that cross module boundaries, e.g. FUSE, UShell, AuthTokenHandling, UJMW), and which ones a module may bring in private versions.
- Sandboxing **without** additional load contexts: version alignment rules, build-time checks (detect diverging versions of the same assembly in the composed output), separate output folders per module plus a deterministic resolve order.
- Sandboxing **with** `AssemblyLoadContext`s per module: only for module-private dependencies; platform contracts must stay in the default context, otherwise types no longer match across the module boundary (the "decoupling hell": duplicate type identities, failing casts, serializer and DI surprises).
- How `ComponentDiscovery`/`TypeIndexer` and the `PluginDir` resolving would behave with isolated contexts.

## U-Shell Hosting Alignment

U-Shell defines the hostability contract for shell and module bundles in the `ushell-hosting` skill of `ProjectUShell/UShell.Docs` (`.agents/skills/ushell-hosting/SKILL.md`). The following items would align UniversalBFF with that target state:

- Let module providers register the embedded `module.json` emitted by the module's React build (`ModuleDescription.FromEmbeddedResFile`) instead of assembling workspaces, use cases and commands in C#. Providers only supplement host-bound values (module UID, token source assignments, absolute URLs of non-co-located backends); the exact set is open decision D-09 in UShell.Docs.
- Embed the React build output in every build configuration (the current copy target runs in Debug only) and include `module.json` (currently `webapp-files\*.json` is excluded).
- Keep the canonical module layout `{moduleScopingKey}/module.json`, `{moduleScopingKey}/ui/`, `{moduleScopingKey}/api/v{n}/`, so relative URLs inside `module.json` resolve without rewriting.
- Keep replacing `_PUBLIC_PATH_` for existing bundles; new bundles use webpack `publicPath: "auto"`.
- Set cache headers for embedded bundles: `no-cache` for `.html`, `.json` and `remoteEntry.js`, long-term caching for hashed chunks.
- User Management reference module: replace the C#-built module description and `WidgetClass = "demo"`, rename the federation name `srViewer`, and stop passing `backendApiUrl` through the UnitOfWork.

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
