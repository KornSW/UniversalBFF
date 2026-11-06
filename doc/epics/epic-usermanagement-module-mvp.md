# Epic: User Management Module MVP and Reference Module

## Goal

Bring the out-of-box module `UniversalBFF.OobModules.UserManagement` to an MVP state and, at the same time, make it the **exemplary reference for a well-cut U-Shell module** hosted by UniversalBFF.

Two goals are pursued together and weighed against each other on every decision:

1. **MVP:** administrators can maintain tenants, roles, users, role assignments, local accounts, identity providers and API clients through the U-Shell UI.
2. **Reference module:** the module shows how far a module gets with on-board means only - configuration first, generic CommonComponents widgets, and the FUSE `IRepository` contract as the **only** communication contract with the backend.

## Guiding Principles

| Principle | Consequence for this module |
|---|---|
| Configuration first | Workspaces, use cases, commands, datastores and table layouts are declared in `module.json`. Widgets are generic and configured through their UnitOfWork. |
| `module.json` is a React artifact | The description lives in the web project (`js/UniversalBFF.OobModules.UserManagement/src/module.json`), is emitted by the build and embedded into the assembly. The C# provider does not assemble workspaces, use cases or commands. |
| Host supplements only host-bound values | The C# provider only adds values that depend on the hosting situation (`ushell-hosting` M-3, decision D-09), for example compensation of open shell gaps such as DD-07. |
| FUSE `IRepository` only | Every backend interaction goes through `IRepository<TEntity, TKey>` endpoints plus `ISchemaProvider` (both FUSE RepositoryContract). No additional service contracts. A real showstopper is raised and discussed instead of silently adding a contract. |
| Generic before specific | A gap that is not user-management specific is solved generically (first inside the bundle, flagged as promotion candidate) and proposed to CommonComponents or the shell. |
| Minimal invasiveness | Changes stay in the module (web project and module provider). Changes to the UniversalBFF core logic are discussed first. |

Leading external sources: `ushell-module-development`, `ushell-hosting`, `ushell-declarative-model`, `ushell-data-auth-scope` and `ushell-design-debt` in `UShell.Docs`.

## Responsibilities (Target Cut)

| Part | Owns | Must not own |
|---|---|---|
| `module.json` (web project) | use-case graph, workspaces, menu commands, datastore declaration, table layouts, labels | URLs of foreign hosts, tokens, business logic |
| Module bundle (`oobUsrMgmt`) | only generic widgets that CommonComponents/the shell cannot render by name yet | entity-specific pages, URLs, token handling, own routing |
| Module provider (C#) | registration of FUSE endpoints, embedding of the web build, host-bound overlays | workspaces, use cases, commands, layouts |
| FUSE repositories (C#) | persistence, validation, protection of sensitive fields | UI concerns |

## Iterations

### I1 - Something visible (done, except the blocker B-1)

- Replace the copied "srViewer" template by a clean module bundle (federation name `oobUsrMgmt`, TypeScript, Tailwind, shared singletons `react`, `react-dom`, `ushell-modulebase`, `ushell-common-components`).
- Canonical build layout `dist/module.json` + `dist/ui/**`, embedded as `Frontend/webapp-files/module.json` + `Frontend/webapp-files/ui/**`.
- Generic `EntityTableWidget` (thin entry + host-agnostic page around CommonComponents `EntityTable`). Its UnitOfWork contract is a superset of the CommonComponents `EntityTableModule` contract (`entityName`, `layoutDescription`, `pageSizes`), so it can later be replaced by a built-in widget class without changing the use-case configuration.
- One datastore `oob-usrmgmt.store` (`fuse`, route pattern `route`) backed by `ISchemaProvider` plus one `IRepository` endpoint per administrable entity.
- The store schema is the designed model `UserManagement.Model.json` (complete keys, composite keys and relations); entity names in `module.json` are model names (`Role`, `LocalCredential`, ...).
- Workspaces: "Users & Access", "Tenants & Roles", "Sign-in Providers & API Clients"; menu folder `Administration/User Management`.
- The provider loads the embedded `module.json`, resolves relative datastore URLs against the module base (compensation for DD-07) and assigns the primary UI token source to the datastore (host-bound value, M-3).
- Secrets (`PasswordHash`, `ClientSecret`) are already masked on read and kept on write by the store decorator (pulled forward from I3, because the store exposes them).
- Verified: listing, counting, entity refs (composite keys) and masked write round trips via HTTP; the tables render in the shell (headless browser) when a token is accepted (see B-1).

### I2 - Configuration quality

- Complete `layoutDescription` per entity: labels, visible table fields, field order, lookups via entity refs, read-only fields.
- Master-detail flows declared through commands and `mapDynamic` (tenant -> roles -> assignments, user -> assignments) instead of widget glue.
- Evaluate the built-in `guifad` widget for entity-graph navigation versus explicit use cases.
- Reload restores every open use case.

### I3 - Sensitive fields and credentials

The pure `IRepository` contract exposes every persisted field. Several entities carry secrets (`PasswordHash`, `ClientSecret`, refresh tokens).

- `ActiveRefreshToken` is not exposed at all.
- Done (pulled into I1): the store decorator `ManagementStoreRepository` masks secret fields on read (`********`) and keeps the persisted value when the mask or an empty value is written back (write-only fields). The contract stays `IRepository`.
- Writing `PasswordHash` is currently rejected, so local accounts cannot be created through the UI yet.

**Decided direction: UI field transformers (first use case for additional hooks in the web world).**
Setting a password is the first use case that requires the UI to put a thin layer over the generic table/form: fields
that are **read-only** for the user can still be **set by special transformer logic** of the UI. For the password:
the form shows `PasswordHash` read-only and offers a "set password" input; a registered transformer turns the input
into the value that is written into `PasswordHash` through the normal `IRepository` write. The backend contract stays
pure.

To be designed (generic, not user-management specific - promotion candidate for CommonComponents/shell):

- The hook: where transformers are registered (module bundle) and how a layout references them (e.g. a `FieldLayout`
  property naming the transformer), and how `EntityTable`/`EntityForm` invoke them before saving.
- The password scheme: today unsalted SHA-256 (`LocalCredentialService.GetPasswordHash`). A transformer can reproduce
  it in the browser, but the scheme itself should be upgraded (salt, slow hash); then the transformer and the store
  must agree on the format (e.g. a versioned hash string), and the store has to accept transformer output for
  `PasswordHash` instead of rejecting every write.
- The same mechanism for generating client secrets of new API clients.

### I4 - Security

- Restrict the management endpoints to an administrative scope (server side; RuntimeTags only for UI visibility).
- Assign the token source of the datastore as host-bound value (M-3) once the portfolio's primary UI token source is known to the provider.
- Menu visibility via `RequiredRuntimeTagsForVisibility`.

### I5 - Hostability, build and cleanup

- Embed the web build in every build configuration (today only Debug copies the output).
- Cache headers for `module.json` and `remoteEntry.js` (no-cache) versus hashed chunks.
- Follow decision D-12 (`publicPath`); the bundle keeps `_PUBLIC_PATH_` until then.
- Remove the placeholder contracts `IUserManagementService` and `ILocalCredentialManagementService` and their endpoints if nothing needs them.
- Developer documentation for the module (how to run, how to extend the configuration).

## Blockers and Findings Outside the Module

| Id | Finding | Status / decision |
|---|---|---|
| B-1 | The core validated all UJMW calls with `new LocalJwtIntrospector("TheSignKey")`: BFF-issued tokens were always rejected, and anybody could forge tokens with the public key. | **Resolved.** The core now wires the validator via `AccessTokenValidator.ConfigureTokenValidationViaAccessTokenManager()` (independent of whether an auth module is loaded; without registered token sources everything is rejected - fail closed). The User Management module registers the BFF token source (`BffUserService.RegisterTokenSourceAtAccessTokenManager`: issuer + introspector of the BFF signing secret, routed by `MapIssuer`). Verified: real BFF token 200, forged token 401, no token 401. |
| B-2 | `SmartStandards.Logging.Centralized` 3.4.4 depends on `FUSE-fx.RepositoryContract` 1.0.2, the modules use 3.0.2; host and modules share `dotnet/dist/`. | **Resolved** by aligning the versions: `FUSE-fx.RepositoryContract` 3.0.2 is pinned in the hosts and AspNetCore wrappers (Logging.Centralized only derives two interfaces from `IRepository`). The general question of module isolation is an open topic in [future directions](epic-future-directions.md) ("Module isolation in composite applications"). |
| B-3 | Module descriptions lose all nested untyped values when served (Newtonsoft read, System.Text.Json write). | **NO GO - open.** Detailed analysis and options: [issue-moduledescription-serialization.md](../work/issue-moduledescription-serialization.md). Stopgap: `ModuleDescriptionNormalizer` in the module. |
| B-4 | The shell initializes datastores once at boot. Before the logon the schema request fails (401) and the datastore stays unusable until reload. | Open (shell). Proposal: (re-)initialize datastores lazily / after token changes (relates to UB-20). |
| B-5 | The initial administrator only gets the role `User`; the role `Administrator` is not assigned. | To do (backlog). |
| B-6 | Datastores reference exactly one token source; with several primary UI token sources a user logged on via another one has no token for the store. | Open. Central frontend AuthTokenManager (UB-26) or a shell-side "primary UI token" reference. |

## Promotion Candidates (outside this repository)

| Candidate | Target | Reason |
|---|---|---|
| Built-in widget class for `EntityTable` (export `EntityTableModule`, wire it in `WorkspaceManager.renderWidget`) | CommonComponents + ReactFrontend (DD-04) | Lets this module drop its bundle completely. |
| Resolve datastore/datasource/service URLs against the `module.json` URL | ReactFrontend (DD-07) | Removes the URL overlay from the C# provider. |
| Write-only/masked field support in layouts or FUSE | CommonComponents / FUSE-fx | Secrets are a generic concern. |
| UI field transformers (read-only fields set by transformer logic, see I3) | CommonComponents (`EntityTable`/`EntityForm`, `FieldLayout`) + module bundle registration | First use case for module-contributed UI hooks. |
| Explicit Newtonsoft serialization of the description endpoints (B-3) | UShell.PortfolioHosting.Mvc / UShell.PortfolioDescription | Removes `ModuleDescriptionNormalizer`. |

## Definition of Done

- Checklists of `ushell-module-development` section 5 and `ushell-hosting` are fulfilled or deviations are documented with their design-debt reference.
- All administrable entities can be listed, searched, created, edited and deleted where this is semantically allowed.
- No secret is readable through the UI.
- `module.json` is the only place that describes the UI; the C# provider contains no UI description.
- `doc/` and the skill `most-important-details-for-universalbff` describe the module as the reference example.

## Known Constraints

- DD-07: the shell resolves datastore URLs against the document base, not against the module. Compensated in the provider for now.
- DD-02/DD-04: the CommonComponents `EntityTableModule` is not exported and not wired as built-in widget; hence the generic widget in the bundle.
- `IWidgetHost` token handling and side channel are shell concerns (UB-20, UB-22, UB-26); the module does not work around them.
