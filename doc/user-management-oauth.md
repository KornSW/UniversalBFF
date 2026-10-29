# UniversalBFF User Management as OAuth Server and Proxy

## Abstract

The out-of-box module `UniversalBFF.OobModules.UserManagement` turns UniversalBFF into a small OAuth authorization server for its U-Shell frontend. It can authenticate users itself (local credentials stored in the BFF database) or delegate the authentication to an external identity provider such as Entra ID, Google or GitHub and wrap the result into a BFF-issued token. In both cases the permissions inside the token (tenant, roles, scopes) are granted exclusively by the BFF from its own database.

The protocol itself (routes, logon pages, PKCE verification, delegation state, token validation) is provided by SmartStandards AuthTokenHandling. The User Management module only supplies the policy: which logon sources exist, how identities are authenticated, which permissions they receive and what the BFF token contains.

## Motivation

A modular U-Shell application needs one consistent answer to "who is the user and what may the user do" across all modules hosted by the BFF. Typical problems this module addresses:

- **Out-of-box start without infrastructure.** A fresh installation must be usable without a corporate identity provider. The module seeds a local administrator and a local logon source on first start.
- **Corporate logon without corporate permission management.** Organizations want users to log on with their existing corporate account, but the application owner wants to manage application permissions locally, independent of the identity provider's group model.
- **Several logon sources at once.** Local accounts for service staff and corporate accounts for regular users must be offered side by side in the U-Shell logon page.
- **No permission escalation via the browser.** Scopes requested by the browser are never trusted; permissions are always computed on the server.

## Differentiation

| Approach | What it does | Difference to UniversalBFF User Management |
|---|---|---|
| Direct OIDC in the SPA (browser talks to Entra/Google) | The identity provider issues the token and its scopes | UniversalBFF keeps permission management for BFF-hosted resources local; the external provider only proves authenticity. Direct OIDC remains possible as `Passthrough` for foreign APIs. |
| Full identity server (e.g. IdentityServer, Keycloak) | Separate product with its own operation, UI and storage | UniversalBFF User Management is embedded, needs no additional service and seeds a working setup automatically. It is not intended to become a general-purpose identity server. |
| Classic BFF cookie pattern | Tokens stay on the server, browser holds a session cookie | Planned as target mode (see "Browser Token Storage"); today U-Shell holds a raw bearer token in `localStorage`, which stays available as legacy mode. |
| Product-specific token parsing | Each product validates tokens itself | UniversalBFF delegates all validation to SmartStandards AuthTokenHandling and validates its own tokens in-process without HTTP loopback. |

## Getting Started

### 1. Start with the local logon

On first start the module migrates its database and seeds (only if no tenant exists):

| Seeded item | Value |
|---|---|
| Tenant | `1111111111111111111`, available for all portfolios, scope `DefaultTenant` |
| Roles | `User` (default for new users), `Administrator` (scope `ABL:UserManagement`) |
| Logon source | One local `OAuthProxyTarget` with label `Logon (System-User)` |
| Administrator | Local credential `admin@localhost`, initial password derived from data visible in the database (see module skill) |

The portfolio delivered by the BFF then contains one `AuthTokenConfig` for the local source. U-Shell shows a logon button, opens the BFF logon page (`/oauth2/authorize`), and after a successful logon receives a BFF token whose scopes are computed from tenant, identity and roles.

### 2. Add a corporate logon source

Add a row to `OAuthProxyTargets` for the external provider:

| Column | Example | Meaning |
|---|---|---|
| `Uid` | generated Snowflake44 id | Public identifier of this logon source. Used as `client_id` by U-Shell. |
| `ClientId` / `ClientSecret` | app registration at the provider | Credentials of the BFF at the external provider. Never leave the server. |
| `AuthUrl` / `RetrivalUrl` | provider authorize / token endpoint | Used by the BFF only. |
| `ProviderClassName` | e.g. a provider-specific `IOAuthOperationsProvider` | Falls back to a generic OAuth operations provider. |
| `TenantUid` | `1111111111111111111` | Tenant whose scopes and default roles new identities receive. |
| `AdditionalParamsJson` | `{ "prompt": "select_account" }` | Provider-specific parameters for the authorize request toward the provider. |
| `DisplayLabel` / `DisplayIconUrl` | `Corporate Logon` | Label shown in U-Shell. An empty label or a label starting with `_` hides the source from the primary logon. |

The redirect URI to register at the provider is the BFF's own `/oauth2/authorize` URL without query.

After a restart U-Shell offers both sources. A corporate user logging on for the first time is stored as cached identity and receives the default roles of the tenant.

### 3. Grant permissions

Permissions are managed in the BFF database only:

| Where | Effect |
|---|---|
| `TenantScope.PermittedScopes` | Granted to every identity of every logon source of this tenant |
| `Role.PermittedScopes` + role assignment | Granted to all identities assigned to the role |
| `CachedUserIdentity.PermittedScopes` | Granted to one individual identity |
| `CachedUserIdentity.Disabled` | Blocks token issuance for this identity |

The resulting token scope contains `Tenant:{TenantUid}`, the tenant scopes, the identity scopes, `Role:{RoleName}` per role and the role scopes.

## Advanced Scenarios

### Corporate logon plus direct access to a provider API

A mail widget needs a Microsoft Graph token, while all other modules use BFF-hosted services.

- The primary logon uses an **Authority-Proxy** source. All BFF modules use the resulting BFF token.
- The mail widget's datastore uses an additional **Passthrough** source that obtains a Graph token directly from Entra ID with real Graph scopes.
- Rationale: Graph permissions are owned by Entra ID, BFF permissions are owned by the BFF. Each authority grants permissions only for the resources it owns.

### Several logon sources, one set of datastores

Local service accounts and corporate accounts both use the same BFF datastores.

- Every source issues the same BFF token type.
- Datastores reference the alias "primary UI token" instead of a fixed source id (planned U-Shell extension), so they work regardless of the logon source used.

### Upstream token lifetime shorter than the BFF token

- When the provider reports the expiry of its token, the BFF token expiry is capped to it, and the token becomes inactive when the upstream token expires.
- When the expiry is unknown, the BFF lifetime applies and the upstream validity check decides.

### Revoking permissions immediately

- Scopes are recomputed from the database on every introspection, so removing a role or disabling an identity takes effect without waiting for token expiry.

## Authentication Model

The cut between "U-Shell talks to identity providers directly" and "U-Shell talks to the BFF proxy" is made per token source. Every token source has exactly one role:

| Role | Who issues the token? | Who grants permissions? | Intended for |
|---|---|---|---|
| **Authority-Local** | UniversalBFF | UniversalBFF database | Local users, initial setup, offline or isolated installations |
| **Authority-Proxy** | UniversalBFF (the upstream token is carried encrypted inside the BFF token) | UniversalBFF database; the external provider only proves authenticity | Everything hosted by the BFF: modules, UJMW endpoints, repositories |
| **Passthrough** | The external identity provider, directly to the browser | The identity provider (or the resource behind it) | Foreign APIs that the BFF neither owns nor protects |

[Architecture](architecture.md#authentication-use-cases-and-responsibilities)

## Flows

### Local logon

```text
U-Shell -> GET /oauth2/authorize?client_id=<source Uid>&...
BFF     -> logon form
User    -> POST credentials (email address or numeric subject id)
BFF     -> verifies local credential, creates short auth-flow session
BFF     -> computes scopes from database, issues BFF token (or authorization code)
U-Shell <- token via redirect
```

### Proxied logon

```text
U-Shell -> GET /oauth2/authorize?client_id=<source Uid>&...
BFF     -> 302 to provider authorize URL with the provider ClientId and a state bag
User    -> authenticates at the provider
Provider-> redirect back to BFF /oauth2/authorize with code and state
BFF     -> redeems the code at the provider with ClientId/ClientSecret (server-side)
BFF     -> resolves the provider subject, maps it to a cached identity
BFF     -> computes scopes from database, issues BFF token containing the encrypted upstream token
U-Shell <- token via redirect
```

### Token validation

```text
raw token
	-> BFF validates its own JWT (signature, issuer, audience, expiry)
	-> resolve origin (ori) and cached identity (must exist and not be disabled)
	-> proxied origins: decrypt the upstream token and let the issuing provider validate it
	-> recompute scopes from the database
	-> active / inactive (fail closed on any error)
```

In-process validation calls the service directly; the BFF never calls its own `/oauth2/*` endpoints over HTTP.

## Browser Token Storage

| Mode | Status | Description |
|---|---|---|
| Raw bearer token in `localStorage` | Current, legacy | U-Shell stores the token per source and sends it in the `Authorization` header |
| HttpOnly session cookie | Target | The BFF keeps the token server-side; the browser only holds an HttpOnly, Secure, SameSite cookie. Introduced backward-compatible as opt-in mode (U-Shell backlog UB-24). |

## Requirements

| Id | Requirement |
|---|---|
| R-01 | Every token source has exactly one role: Authority-Local, Authority-Proxy or Passthrough. |
| R-02 | The public `client_id` of a source is its `Uid`; provider `ClientId`/`ClientSecret` never leave the server. |
| R-03 | Permissions for BFF-hosted resources are granted exclusively from BFF database state; client-requested scopes may only narrow them. |
| R-04 | External providers used via the proxy only prove authenticity; their scopes never become BFF permissions. |
| R-05 | BFF tokens are validated in-process without HTTP loopback. |
| R-06 | For proxied tokens the embedded upstream token is validated by the issuing provider; positive results may be cached briefly, technical errors never. |
| R-07 | The upstream token inside the BFF token is symmetrically encrypted with a key derived from a per-token salt and an installation secret. |
| R-08 | Validation fails closed. |
| R-09 | Scopes are recomputed on every introspection. |
| R-10 | Local identities may use the email address as subject; logon by email and by numeric subject id resolve to the same identity. |
| R-11 | Delegation sessions are valid for 10 minutes and are kept in memory. |
| R-12 | The browser client redeems BFF authorization codes as public client with PKCE. |
| R-13 | Signing and encryption secrets are generated per installation, persisted and overridable via configuration. |

Not all requirements are implemented yet; the implementation status is tracked in the module skill `universalbff-user-management-oauth`.

## Artifacts

| Artifact | Kind | Responsibility |
|---|---|---|
| `BffUserService` | Service (singleton, discoverable) | Implements `IOAuthService`, `IOAuthServiceWithDelegation` and `IPortfolioSecurityProvider`. Sole issuer and introspector of BFF tokens. |
| `BffUserService.IPortfolioSecurityProvider.cs` | Partial | Seeds factory data and maps logon sources into portfolio `AuthTokenConfig` entries. |
| `BffUserService.OAuthCodeFlow.cs` | Partial | One-time authorization codes, server capabilities, rejection of dynamic client registration. |
| `BffUserService.OAuthDelegation.cs` | Partial | Delegation to external providers and upstream code redemption. |
| `LocalCredentialService` | Service | Verifies and manages local credentials; provides metadata of the local source. Does not issue or introspect tokens. |
| `OAuthProxyTargetEntity` | Entity | One logon source (local or external). |
| `TenantScopeEntity`, `RoleEntity` | Entities | Tenant-wide and role-based permissions. |
| `CachedUserIdentityEntity`, `KnownUserLegitimationEntity` | Entities | Known identities per origin and their role assignments. |
| `LocalCredentialEntity` | Entity | Local credentials with password hash and lockout state. |
| `OAuth2Controller` (AuthTokenHandling) | Controller | Protocol routes `/oauth2/*`, logon pages, PKCE and delegation state. |
| `AccessTokenValidator` (AuthTokenHandling) | Static facade | Validates incoming tokens for UJMW endpoints using the BFF service. |
