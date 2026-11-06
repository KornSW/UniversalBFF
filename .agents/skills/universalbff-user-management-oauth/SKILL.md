---
name: universalbff-user-management-oauth
description: Implementation reference for the UniversalBFF OOB UserManagement module acting as OAuth server, OAuth proxy and portfolio security provider (BffUserService implementing IOAuthService, IOAuthServiceWithDelegation and IPortfolioSecurityProvider). Tightly coupled to the SmartStandards skill auth-token-handling. Use when completing, reviewing or changing the self-contained local logon (quickstart with DB credentials), the proxied logon via external providers (Google, GitHub, ...), multi-provider selection, token issuing/introspection routing, or the no-loopback rule for local validation.
metadata:
	version: "2026-10-22"
---

# UniversalBFF UserManagement as OAuth Server and Proxy

## 1. Purpose and Coupling

This skill documents how `UniversalBFF.OobModules.UserManagement` implements the server side of SmartStandards AccessTokenHandling, and which parts are still open. It is the working basis for completing the interplay between:

- **UC-LOCAL** (out-of-box quickstart): the BFF is completely self-contained; identities, credentials, roles and tenants live in its own database.
- **UC-PROXY** (pass-through): the BFF delegates the user logon to an external OAuth provider (Google, GitHub, Entra, ...) and wraps the result into its own token.
- **UC-MULTI**: several sources (local and/or external) are offered to the user for selection.

Mandatory companion skills (load them first, they are normative for everything not BFF-specific):

| Skill | Location | Relevant parts |
|---|---|---|
| `auth-token-handling` | AuthTokenHandling repository, `.agents/skills/auth-token-handling` | §4 core abstractions, §10.3 PKCE, §11 introspection, §12 fail-closed, §13 HTTP mapping, §14 scopes, §16 `ConfigureTokenValidation`, §18 introspector routing, §23 `IOAuthOperationsProvider`, §24 OAuth server and `IOAuthService` |
| `most-important-details-for-auth-token-handling` | same repository | contract rules (UJMW-transportable, synchronous), additive-only compatibility, controller owns PKCE/resource/routes |
| `most-important-details-for-universalbff` | this repository | host startup order, module model, documentation rules |

Division of responsibility (do not blur it):

- **AuthTokenHandling** owns the protocol: `OAuth2Controller` routes, HTML pages via `IAuthPageBuilder`, PKCE S256 verification, resource matching, the delegation `StateBag`, `AccessTokenValidator`.
- **UniversalBFF UserManagement** owns the policy: which clients exist, how identities are authenticated or delegated, how identities map to tenants/roles/scopes, what the BFF token contains and when it is active.
- Changes that need new controller behavior go to AuthTokenHandling as **additive** `IOAuthService` extensions; never re-implement protocol logic inside the BFF.

## 2. Role Model

`BffUserService` is a single discoverable singleton (`[SupportsInstanceDiscovery]`, `[ProvidesDiscoverableInstance] GetInstance()`), split into partial files:

| File | Implements | Responsibility |
|---|---|---|
| `Backend/BffUserService.IPortfolioSecurityProvider.cs` | `IPortfolioSecurityProvider` | Seeds factory data, maps every `OAuthProxyTargetEntity` to an `AuthTokenConfig` in the portfolio, product/scope access checks (currently always `true`). |
| `Backend/BffUserService.cs` | `IOAuthService` (major part) | Logon (`TryAuthenticate`), scope offer, implicit token creation, client credentials, refresh, introspection, client validation, auth-flow sessions. |
| `Backend/BffUserService.OAuthCodeFlow.cs` | `IOAuthService` (code flow, discovery, registration) | One-time codes bound to client, token, opaque PKCE challenge, resource and expiry; capabilities; rejects dynamic registration. |
| `Backend/BffUserService.OAuthDelegation.cs` | `IOAuthServiceWithDelegation` | Decides delegation, redeems the upstream code via `IOAuthOperationsProvider`, resolves the upstream subject. |
| Local credential store (password hash, lockout, validation flag). The OAuth-related interface members are placeholders (`NotImplementedException`). Target role: §9.3. |

Host wiring (`dotnet/src/UniversalBFF.AspNetCore/Programm.cs`, same in `AspHost.net8.0/Startup.cs`):

1. `IPortfolioSecurityProvider` is resolved via `BffStartupArgs.PortfolioSecurityProviderResolver` or InstanceDiscovery.
2. If it is also `IOAuthService`, it is registered as `IOAuthService`, an `IAuthPageBuilder` is registered (own implementation or `DefaultAuthPageBuilder`), and `services.AddOAuthServerController()` exposes `/oauth2/*`.
3. `UjmwHostConfiguration.SetAuthHeaderEvaluator(new AuthHeaderValidationMethod(AccessTokenValidator.TryValidateHttpAuthHeader))`.
4. `AccessTokenValidator.ConfigureTokenValidationViaAccessTokenManager()` - the core configures no introspector itself (fail closed without registered token sources). The module registers the BFF token source during `RegisterServices` (`BffUserService.RegisterTokenSourceAtAccessTokenManager`: `ApplyCustomTokenSource("UniversalBFF", issuer, introspector)` with the BFF signing secret + `MapIssuer`), i.e. in-process validation without loopback (D-02).

`ModuleRegistrar.Init.cs` calls `RegisterAuthTokenSources` per product and fills `portfolio.AuthTokenConfigs` and `portfolio.AuthenticatedAccess.PrimaryUiTokenSources`.

## 3. Data Model (EF, `UserManagementDbContext`)

| Entity | Key fields | Meaning |
|---|---|---|
| `TenantScopeEntity` | `TenantUid`, `AvailablePortfolios` (`*` or `;`-list), `PermittedScopes` | Tenant and the portfolios it applies to. Scopes granted to everybody in the tenant. |
| `RoleEntity` | `TenantUid`, `RoleName`, `PermittedScopes`, `IsDefaultRoleForNewUsers` | Role per tenant. Default roles are assigned automatically on first logon. |
| `OAuthProxyTargetEntity` | `Uid`, `ClientId`, `ClientSecret`, `AuthUrl`, `RetrivalUrl`, `TenantUid`, `ProviderClassName`, `AdditionalParamsJson`, `IntrospectorParamsJson`, `DisplayLabel`, `DisplayIconUrl`, `IframeSupported` | One logon source. Either the local source (`ProviderClassName` = `LocalCredentialService` full name, `AuthUrl` = `/oauth2/authorize`) or an external provider. |
| `CachedUserIdentityEntity` | `OriginUid` + `OriginSpecificSubjectId`, `PermittedScopes`, `Disabled`, cached display data | Known identity per origin. Individually granted scopes. Disable switch. |
| `KnownUserLegitimationEntity` | `OriginUid`, `OriginSpecificSubjectId`, `TenantUid`, `RoleName` | Role assignment of an identity. |
| `LocalCredentialEntity` | `SubjectId` (Int64), `EmailAddress`, `PasswordHash`, `IsValidated`, `LockedUntil`, `WrongPasswordCount` | Local credentials (UC-LOCAL only). |
| `ApiOAuthClientEntity` | `ClientId` (Int64), `ClientSecret`, `MappedSubjectId`, `RedirectUrl`, `ValidUntil` | Technical API clients of a local subject. Not used by the service yet. |
| `ActiveRefreshTokenEntity` | `Token`, `SubjectId`, `ValidUntil` | Prepared for refresh tokens. Not used yet. |

Identity key everywhere: **(`OriginUid`, `OriginSpecificSubjectId`)**. For UC-LOCAL the origin is the local proxy target and the subject is `LocalCredentialEntity.SubjectId` as string.

## 4. Factory Data (`CheckIfInitialStateShouldInstaled`)

Executed on every `RegisterAuthTokenSources` call; runs `UserManagementDbContext.Migrate()` and seeds only when no tenant exists:

- tenant `1111111111111111111` (`AvailablePortfolios = "*"`, scope `DefaultTenant`),
- roles `User` (default for new users) and `Administrator` (`ABL:UserManagement`),
- one local `OAuthProxyTargetEntity` with fresh Snowflake `Uid`, `ClientId`, `ClientSecret` (different per installation by design), `DisplayLabel = "Logon (System-User)"`,
- one local admin credential; **initial password = first 4 digits of `SubjectId` + 4-char salt shown in `DisplayName`** (both visible in the database),
- cached identity plus `Administrator` legitimation for that admin.

## 5. Portfolio Registration (`RegisterAuthTokenSources`)

For every proxy target of every tenant available for the product:

| `AuthTokenConfig` property | Value | Note |
|---|---|---|
| source uid (callback key) | `Snowflake44.ConvertToGuid(target.Uid)` | Referenced by `PrimaryUiTokenSources`. |
| `DisplayLabel`, `DisplayIconUrl` | from target | Label empty or starting with `_` means: not offered for primary UI logon. |
| `Claims` | from `AdditionalParamsJson` | Flattened to strings. |
| `IssueMode` | `OAUTH_IMPLICIT_FLOW` | Always, even for external targets: the UI only talks to the BFF proxy. |
| `AuthEndpointUrl` / `RetrieveEndpointUrl` | `/oauth2/authorize` / `/oauth2/token` | The BFF proxy, never the external provider. |
| `ClientId` | `target.ClientId` | **Inconsistent, see §10 G-01.** |
| `ClientSecret` | `null` | Implicit flow. |
| `AuthEndpointRejectsIframe` | `!target.IframeSupported` | |
| `ValidationMode` / `ValidationEndpointUrl` | `OAUTH_INTROSPECTION_ENDPOINT` / `/oauth2/introspect` | For consumers outside the BFF process only (see §9). |

## 6. Runtime Flows (controller call order)

The call order is defined by `OAuth2Controller` (AuthTokenHandling.MvcSupport). The BFF only implements the callee side.

### 6.1 UC-LOCAL (interactive)

```text
GET  /oauth2/authorize?response_type=token|code&client_id=..&redirect_uri=..&state=..[&code_challenge..]
		 -> TryValidateApiClient(clientId, host, redirectUri)
		 -> (IOAuthServiceWithDelegation) CodeFlowDelegationRequired(clientId, ...)   => false for local target
		 -> logon form (IAuthPageBuilder.GetAuthForm)
POST /oauth2/authorize (login, password)
		 -> TryValidateApiClient
		 -> TryAuthenticate(clientId, login, password, noPasswordNeeded=false, state, out sessionId)
				- local target: CreateSession(login, target.Uid, providerClassName) + LocalCredentialService.TryAuthenticate
		 -> redirect GET /oauth2/authorize?...&otp=<sessionId>
GET  /oauth2/authorize ... otp=<sessionId>
		 -> TryGetAvailableScopesBySessionId   => empty array (no consent wanted)
		 -> scope confirmation form (still rendered, see G-05)
POST /oauth2/authorize (otp, scope_*)
		 -> token: TryValidateSessionIdAndCreateToken  -> redirect with access_token
		 -> code : TryValidateSessionIdAndCreateRetrievalCode -> redirect with code
POST /oauth2/token grant_type=authorization_code
		 -> RetrieveTokenByCode (consumes code; checks expiry, client binding, client secret)
		 -> controller verifies PKCE and resource
```

### 6.2 UC-PROXY (delegated code flow)

```text
GET  /oauth2/authorize?client_id=<target>&...
		 -> TryValidateApiClient
		 -> CodeFlowDelegationRequired(clientId, ref loginHint, out targetAuthorizeUrl, out targetClientId, out anonymousSessionId)
				- BFF: target looked up by Uid; non-local => anonymous session "=>{targetUid}"
		 -> controller builds StateBag {OriginalState, SessionId, OriginalRedirectUri, OriginalScope,
				OriginalResponseType, OriginalClientId, OriginalCodeChallenge, OriginalResource, ViewMode},
				base64 JSON as upstream 'state'
		 -> 302 to target.AuthUrl with response_type=code, client_id=targetClientId,
				redirect_uri=<this /oauth2/authorize URL without query>   (must be registered at the provider)
GET  /oauth2/authorize?code=<upstream code>&state=<StateBag>
		 -> TryHandleCodeflowDelegationResult(code, StateBag.SessionId, thisRedirectUri)
				- InitializeOAuthOperationsProvider(target): TypeIndexer lookup of target.ProviderClassName,
					fallback GenericOAuthOperationsProvider, ApplyCommonConfigurationValues(label, AuthUrl, RetrivalUrl, "", iframe, idToken=false, additionalParams)
				- TryGetAccessTokenViaOAuthCode(code, target.ClientId, target.ClientSecret, thisRedirectUri)
				- subject: TryResolveSubjectAndScopes -> fallback: non-validating JWT 'sub' -> fallback: SHORT_LIVING_IDENTITY_{MD5(access_token)}
				- session is impersonated: LogonNameOrSubject, OriginUid, TokenResultFromDelegate
		 -> controller restores original request values from StateBag, re-validates the original client
		 -> continues as 6.1 from "otp" (scope page, then token or code)
```

### 6.3 Introspection endpoint

`POST /oauth2/introspect` (anonymous, form `token`) -> `IntrospectAccessToken(token, out active, out claims)`; the controller adds `active`. Technical exceptions are mapped to `active=false` with `inactive_reason`.

### 6.4 Not supported yet

- `client_credentials` -> `ValidateClientAndCreateToken` throws `NotImplementedException`.
- `refresh_token` -> `CreateFollowUpToken` returns `invalid_request`.
- Windows pass-through (`noPasswordNeeded`) -> rejected in `TryAuthenticate`.
- Dynamic client registration -> rejected by design.

## 7. Sessions and Codes

- Auth-flow sessions: in-memory `Dictionary<long, AuthFlowSession>`, key = Snowflake44 id, **valid for 1 minute after creation** (derived from the Snowflake timestamp). Cleanup runs on every `CreateSession`.
- `AuthFlowSession`: `LogonNameOrSubject`, `OriginProviderName`, `OriginUid`, display data, `TokenResultFromDelegate`.
- Codes: `PendingAuthorizationCode` (client id, token result, opaque challenge, resource, 1 minute expiry), consumed atomically, also on failed redemption.
- Everything is process-local: no scale-out, lost on restart. This is acceptable for UC-LOCAL and single-instance hosting only.

## 8. BFF Token Contract

Issued by `LocalJwtIssuer` (static `_LocalIssuer`, HMAC key `_LocalJwtSingKey`, lifetime 240 minutes, pass-through of requested claims, enforced issuer `UniversalBFF`):

| Claim | Value |
|---|---|
| `iss`, `aud` | `UniversalBFF` |
| `jti` | auth-flow session id |
| `sub` | `CachedUserIdentityEntity.OriginSpecificSubjectId` (UC-LOCAL: may be the email address, D-08) |
| `ori` | `OAuthProxyTargetEntity.Uid` (origin of the identity) |
| `wrp` | upstream `access_token` (UC-PROXY only, otherwise null). Target state (D-16): encrypted, base64url ciphertext |
| `slt` | target state (D-16): random per-token salt used to derive the `wrp` encryption key (UC-PROXY only) |
| `scope` | space-separated, sorted, distinct union of: `Tenant:{TenantUid}`, tenant `PermittedScopes`, identity `PermittedScopes`, `Role:{RoleName}` per assigned role, role `PermittedScopes` |

Rules:

- Scopes are **only** derived from the database. Client-requested scopes are filtered through `GetAvailableScopes` (currently empty) and never trusted (`auth-token-handling` §14.5).
- First logon of an unknown identity creates a `CachedUserIdentityEntity` and assigns all default roles of the origin's tenant.
- A disabled cached identity gets no token.
- `id_token` and `refresh_token` wrapping is prepared but commented out.

## 9. Local vs. Proxy Routing and the No-Loopback Rule

Discriminator for "local": `target.ProviderClassName == typeof(LocalCredentialService).FullName` (older code used `target.AuthUrl == _OurProxyAuthUrl`; `CodeFlowDelegationRequired` checks both).

Normative rules (desired state; existing code partly violates them, see §10):

1. **No HTTP loopback inside the BFF process.** The BFF must never call its own `/oauth2/authorize`, `/oauth2/token` or `/oauth2/introspect` over HTTP. Those endpoints exist for the browser/UI and for consumers outside the process.
2. **In-process validation uses the service instance.** `IOAuthService` already extends `IAccessTokenIntrospector`; when the discovered `IPortfolioSecurityProvider` is an `IOAuthService`, the host must hand that instance to `AccessTokenValidator` (directly or via an `IntrospectorLookupMethod`, `auth-token-handling` §16/§18). Signing key and validation key are then identical by construction.
3. **Local logon never leaves the process.** For the local target, `TryAuthenticate` verifies credentials directly against `LocalCredentialService`. No `IOAuthOperationsProvider` round trip, no generated entry URL. The placeholder `IOAuthOperationsProvider` members of `LocalCredentialService` must stay unused.
4. **External calls only toward real external providers.** `IOAuthOperationsProvider` is used only for non-local targets (code redemption, subject resolution, optional upstream validation).
5. **The advertised `OAUTH_INTROSPECTION_ENDPOINT` is for others.** UI or remote modules may use it. The BFF host itself does not.
6. **Fail closed** (`auth-token-handling` §12): unknown `ori`, missing or invalid signature, disabled identity, technical errors produce inactive results. A technical failure must not be cached as inactive.
7. **Authenticity upstream, authorization here.** An external provider is used only to prove that the identity is authentic. Permissions (scopes, tenant, roles) are always granted by the BFF from its own database. Upstream scopes or claims never become BFF permissions.

### 9.1 Client Id Mapping (D-01)

Two identifiers must never be mixed:

| Identifier | Meaning | Where it may appear |
|---|---|---|
| **outer client id** = `OAuthProxyTargetEntity.Uid` (decimal string) | Identifies a logon source at the BFF proxy. This is the `client_id` seen by `/oauth2/*`. | Portfolio `AuthTokenConfig.ClientId`, every `IOAuthService` method parameter named `apiClientId`/`clientId`, `StateBag.OriginalClientId`, code binding in `PendingAuthorizationCode.ClientId`, claim `ori`. |
| **inner client id** = `OAuthProxyTargetEntity.ClientId` (+ `ClientSecret`) | Credentials of the BFF as a client at the external provider. | Only on the server side, only toward the external provider: `CodeFlowDelegationRequired` `out targetClientId`, `TryGetAccessTokenViaOAuthCode(code, target.ClientId, target.ClientSecret, ...)`, later upstream validation/refresh calls. |

Rules:

- Every lookup that receives a `client_id` from the controller parses it as `Int64` and resolves the target by `Uid`. A lookup by `ClientId` with a controller-supplied value is a defect.
- The inner `ClientId`/`ClientSecret` never leave the server toward UI or portfolio, and are never compared against values presented by a caller of `/oauth2/token`.
- The portfolio source uid (`Snowflake44.ConvertToGuid(Uid)`) is a third representation of the same `Uid` and is only used as portfolio key.
- For the local target the inner `ClientId`/`ClientSecret` have no upstream meaning (see D-11 for their future use).

Audit checklist of all code paths (state before the fix; each must be aligned and covered by a test):

| Code path | Current behavior | Required behavior |
|---|---|---|
| `RegisterAuthTokenSources` | `mappedConfig.ClientId = oauthConfig.ClientId` (inner id leaks to UI) | `oauthConfig.Uid.ToString()` |
| `TryValidateApiClient` | parses long, then looks up by `ClientId` | look up by `Uid` (plus redirect URI check, G-07) |
| `TryAuthenticate` | looks up by `ClientId` | look up by `Uid` |
| `TryValidateApiClientSecret` (used by `RetrieveTokenByCode`, `ValidateClientAndCreateToken`) | looks up by `ClientId`, compares inner `ClientSecret` | look up by `Uid`; never compare the inner secret of external targets (see D-11) |
| `TryValidateSessionIdAndCreateToken` | resolves origin by `session.OriginUid` (correct); error text uses `apiClientId` | keep; additionally check that `apiClientId` equals `session.OriginUid` |
| `TryValidateSessionIdAndCreateRetrievalCode` / `RetrieveTokenByCode` | binds and compares the outer id | keep (consistent once all lookups use `Uid`) |
| `CodeFlowDelegationRequired` | looks up by `Uid`, returns inner `ClientId` as `targetClientId` | correct |
| `TryHandleCodeflowDelegationResult` | target from session `=>{Uid}`, redeems with inner credentials | correct |
| Controller `StateBag` | carries the outer id as `OriginalClientId` | correct (no BFF change) |
| Factory data | local target gets random `ClientId` and `ClientSecret` | keep or repurpose (D-11) |

### 9.2 Introspection Pipeline (D-02, D-03)

The same `BffUserService.IntrospectAccessToken` serves both in-process validation (handed directly to `AccessTokenValidator`, no loopback) and the `/oauth2/introspect` endpoint for external consumers.

```text
raw token
  -> BFF JWT validation (signature with the BFF key, exp/nbf, iss)        invalid -> inactive
  -> resolve 'ori' to OAuthProxyTargetEntity by Uid                        unknown -> inactive
  -> resolve cached identity (ori, sub); Disabled                          -> inactive
  -> local target:    no upstream step
  -> external target: extract 'wrp'                                        missing -> inactive
                      validate 'wrp' with the introspector matching the provider of the target
                        upstream inactive          -> inactive
                        technical failure          -> inactive, NOT cached as negative result
  -> active: return OUR claims (sub, ori, scope from BFF policy, ...); upstream claims/scopes are discarded
```

Notes:

- The upstream check proves authenticity only; it never adds permissions (rule 7).
are tracked as D-12 to D-16.

### 9.3 Token Kinds and Authority Boundary (D-06)

There is exactly one token format issued by the BFF (signed with the BFF key), in two kinds:

| Kind | Recognized by | Authenticity proven by | Permissions from |
|---|---|---|---|
| **Leading local token** (UC-LOCAL) | `ori` = Uid of the local target, no `wrp` | `BffUserService` alone (BFF signature, expiry, DB state of identity) | BFF database |
| **Wrapper token** (UC-PROXY) | `ori` = Uid of an external target, `wrp` present | outer: `BffUserService`; inner `wrp`: introspector of the target's provider (D-12) | BFF database |

Responsibilities:

| Component | Owns | Must not |
|---|---|---|
| `BffUserService` | Signing key, `LocalJwtIssuer`/`LocalJwtIntrospector` instances, issuing of both kinds, `IntrospectAccessToken` for both kinds (§9.2), routing to provider introspectors, scope computation. It is the single `IAccessTokenIntrospector` handed to `AccessTokenValidator`. | Call any `/oauth2/*` endpoint over HTTP. |
| `LocalCredentialService` | Credential verification (`TryAuthenticate(login, password)`), credential management (`ILocalCredentialManagementService`), provider metadata for the local source (`ProviderInvariantName`, `ProviderDisplayTitle`, `ProviderIconUrl`, `HasCapability`). As `IOAuthOperationsProvider` it may describe **our own** endpoint: `GenerateEntryUrlFor*` returns `/oauth2/authorize` URLs with the local target Uid as `client_id` (pure URL building, no I/O). | Own a key, issue tokens, or validate tokens by itself. `IntrospectAccessToken`/`TryValidateToken`/`TryResolveSubjectAndScopes`, if implemented, delegate in-process to `BffUserService`. Token retrieval members (`TryGetAccessTokenVia*`, `TryRequestAccessToken`) stay unsupported, because their only implementation would be an HTTP loopback. |
| External `IOAuthOperationsProvider` (per target) | Upstream code redemption, upstream subject resolution, upstream token validation. | Grant BFF permissions. |

Consequence: the introspector for local tokens lives in `BffUserService`, not in `LocalCredentialService`. Unsupported members should throw `NotSupportedException` with a clear message instead of `NotImplementedException`.

## 10. Known Gaps and Defects (current code)

| Id | Location | Finding |
|---|---|---|
Result: delegation is effectively never triggered, and for external targets the upstream client id would be published to the UI. Target behavior: §9.1. |
| G-02 | `Programm.cs` / `Startup.cs` | RESOLVED 2026-11-06: validation is wired via the AccessTokenManager; the module registers the BFF token source (issuer routing). Still open from §9.2: `ori` resolution, disabled-identity check, `wrp` handling (see G-03). |
Missing: local JWT check, `ori` resolution, disabled-identity check, handling of `wrp`. Target behavior: §9.2. |
| G-04 | `TryAuthenticate` (local branch) | The session stores the entered login (email or SubjectId). The seeded identity uses the SubjectId, so an email logon creates a second identity with default roles only. A session is also created when the password check fails. |
| G-05 | `GetAvailableScopes` + controller | No consent wanted (empty scope list), but the controller always renders the scope confirmation page: an empty extra click. |
| G-06 | `TryAuthenticate` (non-local branch) | Dead end with critical log. Only reachable when delegation did not trigger (see G-01). |
| G-07 | `TryValidateApiClient` | `redirectUri` is not validated (open-redirect risk). TODO in code: for proxy clients the redirect URI must always be the BFF. |
| G-08 | Sessions | 1-minute session lifetime also applies to the anonymous delegation session. External logons taking longer fail at `TryGetAvailableScopesBySessionId`. |
| G-09 | `TryHandleCodeflowDelegationResult` | Display name and email are not taken from provider claims. The `SHORT_LIVING_IDENTITY_*` fallback creates a new cached identity per logon. `IntrospectorParamsJson` is ignored. Operations-provider instances are not cached. |
| G-10 | `ValidateClientAndCreateToken`, `CreateFollowUpToken` | Client credentials and refresh not implemented (discovery correctly does not advertise them). |
| G-11 | `_LocalJwtSingKey` | Hard-coded HMAC key (`HACK` marker). Must come from configuration or a per-installation persisted secret. |
| G-12 | `/oauth2/introspect` | Anonymous; no caller authentication. Acceptable for in-process-only usage, not for remote consumers. |
| G-13 | `CanCurrentIdentityAccessProduct`, `CanCurrentIdentityAccessScope` | Always `true`. |
| G-14 | `LocalCredentialService` | `IOAuthOperationsProvider`, `IAccessTokenIssuer`, `IAccessTokenIntrospector` members throw `NotImplementedException`. Role undecided (see D-06). |

## 11. Open Decisions (decision log)

Record every decision here and in `doc/architecture.md` where

| Id | Question | Options | Proposed default | Status |
|---|---|---|---|---|
| D-01 | Which `client_id` does the UI send to the BFF proxy? | a) `OAuthProxyTarget.Uid` as internal routing key; b) `ClientId` everywhere; c) separate BFF client table (`ApiOAuthClientEntity`) | **a) `Uid`.** All code paths must be audited so that outer (`Uid`) and inner (`ClientId`) ids are never mixed and are mapped cleanly in both directions (§9.1). | DECIDED 2026-10-22 |
| D-02 | How is in-process validation wired? | a) `ConfigureTokenValidation(service as IAccessTokenIntrospector)`; b) `IntrospectorLookupMethod` routing by `iss`/`ori`; c) `ConfigureTokenValidationViaAccessTokenManager` | **BFF tokens are validated by the internal service instance directly, without loopback** (§9.2). | DECIDED 2026-10-22 |
| D-03 | How does introspection treat `wrp`? | a) BFF JWT is authoritative; b) also re-validate upstream token; c) configurable per target | **b) For external origins, `wrp` is extracted and validated by the introspector matching the actual provider. If valid, the response carries the claims/scopes defined by the BFF; the provider is used for authenticity only, permissions are granted by the BFF** (§9, rule 7; §9.2). | DECIDED 2026-10-22 |
| D-04 | How to avoid the empty consent click? | a) additive AuthTokenHandling extension (skip consent when no scopes are available); b) BFF `IAuthPageBuilder` auto-submit; c) accept for now | **a), as part of a fundamental rework of the AuthTokenHandling server UI (PoC state).** Requirement handed over to the AuthTokenHandling repository; no BFF-side workaround. | DECIDED 2026-10-22 |
| D-05 | Where does the user pick among several providers? | a) U-Shell UI via `PrimaryUiTokenSources`; b) BFF logon page offers local form plus provider buttons; c) both | **a) U-Shell.** The BFF logon page only ever serves the one source identified by `client_id`. U-Shell gaps: §13. | DECIDED 2026-10-22 |
| D-06 | Role of `LocalCredentialService` | a) pure credential store; b) full in-process provider | **Small credential verifier + provider metadata; it may describe our own `/oauth2/*` endpoint, but issuing and introspection belong exclusively to `BffUserService`** (§9.3). | DECIDED 2026-10-22 (agent proposal, delegated) |
| D-07 | UI flow toward the proxy | a) keep `OAUTH_IMPLICIT_FLOW`; b) switch to code + PKCE (public client) | **Phase 1 (MVP): keep implicit, harden it. Phase 2: additive issue mode for code + PKCE (public client) in U-Shell and BFF, published per target. Phase 3: deprecate implicit** (§14). | DECIDED 2026-10-22 (agent proposal, delegated) |
| D-08 | Subject for UC-LOCAL | a) always `SubjectId`; b) email address allowed | **b) Deliberate exception for the local mode: the `OriginSpecificSubjectId` (and thus `sub`) of a local identity may be the email address.** The value must be one canonical form per local credential (trimmed, case-insensitive comparison), so that logon by email and logon by numeric `SubjectId` resolve to the same cached identity; factory seeding (currently `SubjectId.ToString()`) must follow the same rule. External origins keep the provider-specific subject. | DECIDED 2026-10-22 |
| D-09 | Signing key source | configuration value vs. generated per installation and persisted in DB | **Generated per installation and persisted, overridable via configuration.** | DECIDED 2026-10-22 |
| D-10 | Session lifetime / storage | longer lifetime for delegation sessions; in-memory vs. persisted | **Separate lifetime of 10 minutes for delegation sessions; stay in-memory.** | DECIDED 2026-10-22 |
| **a) for the U-Shell browser (matches D-07), b) only if confidential server clients are needed later; c) never.** See §11.1. | DECIDED 2026-10-22 |
| **c). The outer wrapper JWT is always validated by the BFF itself (signature, `iss`, `aud`, `exp`); the extracted upstream token is validated by the issuing provider.** See §11.1. | DECIDED 2026-10-22 |
| D-13 | Caching of the upstream check | a) none; b) positive results cached for a short time (e.g. 60 s, never beyond upstream `exp`); c) per target configurable | **b), technical errors never cached.** | DECIDED 2026-10-22 |
| D-14 | Scope freshness | a) scopes from BFF JWT (issue-time snapshot); b) recomputed from DB on every introspection | **b) (permission revocation takes effect immediately).** | DECIDED 2026-10-22 |
| D-15 | Upstream token expires before BFF JWT | a) BFF JWT `exp` capped to upstream expiry at issue time; b) inactive when upstream expires, re-logon required; c) silent upstream refresh | **a) + b), applied only when the upstream expiry is known** (`expires_in` of the token response or `exp` of a JWT upstream token). With unknown expiry the BFF lifetime applies and the upstream check (D-13) decides. | DECIDED 2026-10-22 |
| **c) Stateless and encrypted: `wrp` stays in the BFF JWT but is symmetrically encrypted. Each token gets a random salt claim `slt`; the key is derived from `slt` plus an internal BFF secret stored in the DB.** See §11.1. | DECIDED 2026-10-22 |
| D-17 | Which token do BFF datastores use when several logon sources exist (U-09)? | a) U-Shell: alias - a datastore `tokenSourceUid` may reference "the primary UI token" (e.g. the empty Guid or a well-known alias), resolved to the source that actually logged on; b) BFF publishes one virtual token source for all its sources and stores every logon under it; c) datastores list several acceptable sources | **a) (additive in U-Shell, all sources issue the same BFF token type anyway).** | DECIDED 2026-10-22 |
| Resolved by D-20: for Authority-Proxy targets `AdditionalParamsJson` holds upstream authorize parameters only and is no longer published as portfolio `Claims`; for Passthrough targets it is published and represents real scopes at the identity provider. Optional: `PermittedScopes` on the target for per-source permissions. | DECIDED 2026-10-22 (via D-20) |
Human-readable model incl. responsibility tables: `doc/architecture.md`,
| D-19 | Token storage in the browser (U-13) | a) keep `localStorage`; b) `sessionStorage`; c) BFF cookie pattern (HttpOnly session cookie, token stays server-side) | **c) is the explicit target and is pursued as far as backward compatibility allows; `localStorage` remains as legacy mode during the transition.** Larger U-Shell refactoring, tracked as UB-24 in `UShell.Docs/doc/backlog.md`. | DECIDED 2026-10-22 |

### 11.1 Explanations for open decisions

**Terminology used below.** *Wrapper token* = the JWT issued and signed by the BFF (`iss`/`aud` = `UniversalBFF`). *Upstream token* = the `access_token` that the external provider (e.g. Entra ID, Google) returned to the BFF during delegation. In UC-PROXY the upstream token is stored as the claim `wrp` inside the wrapper token. The wrapper token is ours and is therefore always validated by the BFF itself; `wrp` is not a second BFF token.

**D-11 (client authentication at BFF code redemption).** In code flow, U-Shell receives a BFF authorization code and redeems it at the BFF `/oauth2/token`. The `client_id` sent there is the target `Uid`. A browser cannot keep a secret, so the BFF must not demand one from U-Shell. PKCE replaces the secret: U-Shell sends `code_challenge` at `/authorize` and the matching `code_verifier` at `/token`; AuthTokenHandling verifies S256. The inner `ClientSecret` of an external target belongs to the BFF as client of that provider and must never be accepted from or compared against a caller (otherwise it would have to be handed to the browser). If server-side confidential clients are needed later, they get their own BFF secret (option b), not the provider secret.

3) For external targets decrypt `wrp` with the key derived from `slt` (D-16) and let

Option a is acceptable for the MVP if upstream scopes are minimal (`openid profile email`); D-19 (cookie mode) reduces the exposure further.

Decided design (option c, stateless):

- Issue: generate 16 random bytes as `slt` (base64url). Derive `key = HMAC-SHA256(wrpMasterSecret, "wrp-v1:" + slt)` (32 bytes). Encrypt the upstream token with AES-256-CBC (PKCS7, IV derived from the same HMAC with label `"wrp-iv-v1:"`, first 16 bytes) and store base64url ciphertext in `wrp`.
- Integrity: no separate MAC needed because the whole JWT, including `slt` and `wrp`, is covered by the BFF signature, which is verified before decryption. Decrypt only after successful signature validation (prevents padding-oracle use). AES-GCM would be preferable but is not available on .NET Framework 4.8.
- Key separation: `wrpMasterSecret` must be different from the JWT signing key; both are generated per installation and persisted in the DB (D-09), overridable via configuration.
- Decryption failure => token inactive (fail closed). Key rotation is out of scope for the MVP; a later `kid`-like version prefix (`v1`) in the derivation labels allows it.

Proposal: `AdditionalParamsJson` is a JSON object of string parameters that the BFF appends in `CodeFlowDelegationRequired` to `targetAuthorizeUrl`; it is no longer exposed to the portfolio. Reserved OAuth parameters (`response_type`, `client_id`, `redirect_uri`, `state`) cannot be overridden.

Findings from the code (2026-10-22), relevant for the permission concern:

- `AdditionalParamsJson` is already used twice: `InitializeOAuthOperationsProvider` passes it as `additionalParams` to the upstream `IOAuthOperationsProvider`, and `RegisterAuthTokenSources` publishes it as portfolio `Claims`.
- U-Shell turns `Claims` into the requested `scope` (`key:value`, or `value` alone) for the BFF `/authorize` call.
- The BFF ignores requested scopes: `GetAvailableScopes` returns an empty set, and `TryValidateSessionIdAndCreateToken` builds the scope exclusively from the DB (tenant of the target, identity, roles). This is identical for local and proxy logons. Today the published `Claims` therefore grant nothing.
- Client-requested scopes can never be a grant mechanism, because the URL is under control of the browser user. They can only be a *selection/narrowing* (requested intersected with granted).
- Simple "everybody from source X gets Y" scenarios are already covered server-side: the target belongs to a `TenantScope` whose `PermittedScopes` and default roles apply to every identity of that source. A gap only exists if two targets of the same tenant need different permissions; that would need a server-side `PermittedScopes` on the target, not portfolio claims.

## 12. Change Rules

- Keep the protocol in AuthTokenHandling; extend `IOAuthService` only additively and only in that repository (see `most-important-details-for-auth-token-handling`).
- Keep `RegisterAuthTokenSources` output compatible with U-Shell portfolio consumers; changing `IssueMode`, endpoint URLs or the source uid scheme is a breaking change for frontends.
- Never put upstream client secrets or upstream tokens into portfolio data. `wrp` is the only carrier of the upstream token and lives inside the signed BFF JWT.
- Scopes are always granted from database state, never from request values.
update this skill, `doc/architecture.md` and the security
- Tests: MSTest in `dotnet/test/UniversalBFF.Tests`. Follow the linked-source pattern of `BffOAuthCodeFlowTests` for DB-free tests. Never call real external providers.

## 13. U-Shell Compatibility (D-05)

Analyzed sources (sibling repositories): `UShell.ReactFrontend/ushell-ts/src/authentication/TokenService.ts`, `.../Components/LogonPage.tsx`, `PopupLogin.tsx`, `IFrameLogin.tsx`, `App.tsx`, `workspace-handling/WorkspaceManager.tsx`, `portfolio-handling/PortfolioBasedMenuService.tsx`, `datasource-handling/DatasourceManager.ts`, `UShell.PortfolioDescription/js/src/portfolioDescription.ts`, `ushell-common-components` `FuseDataStore`, and the C# model `AuthTokenConfig (from UShell).cs` in AuthTokenHandling.

Flow as implemented in U-Shell:

1. `WorkspaceManager` renders `LogonPage` when `TokenService.isUiAuthenticated()` is false (no token stored for any entry of `authenticatedAccess.primaryUiTokenSources`).
2. `LogonPage` renders one button per entry of `primaryUiTokenSources`: popup if `authEndpointRejectsIframe`, otherwise iframe. **Multi-source selection is therefore basically supported.**
3. `buildParams` sends `client_id`, `redirect_uri` (shell origin + base path), `scope` (built from `claims`), `response_type=token` (hard-coded), `state` = base64 JSON `{tokenSourceUid, portfolio, popup}`, plus `additionalAuthArgs`.
4. Callback: `App.tsx` reads `code`/`token`/`access_token`/`state` from the query (fragment only as Google fallback), stores the token in `localStorage` under the source uid, closes the popup, and the opener polls `localStorage`.
5. Backend calls: `FuseDataStore` sets `Authorization: <raw token>` (no `Bearer` prefix; `AccessTokenValidator` accepts both) using the `tokenSourceUid` from the datastore's `providerArguments`.
6. Logoff deletes the local token only.

Findings (status before fixes; owner in brackets):

| Id | Finding | Impact | Proposed fix |
|---|---|---|---|
| U-01 | Button text is `Login with ${localLogonNameInputLabel}`; `displayLabel`/`displayIconUrl` are not in the TS model and are ignored. [U-Shell, PortfolioDescription] | All BFF sources show "Login with Username"; the user cannot distinguish providers. | Add `displayLabel`, `displayIconUrl` to the TS `AuthTokenConfig` (C# already has them) and use them in `LogonPage`. Additive. |
| U-02 | `issueMode` is ignored; `response_type=token` is hard-coded; the TS union does not even contain `OAUTH_IMPLICIT_FLOW`. [U-Shell] | Fine for MVP; blocks D-07 phase 2. | Honor `issueMode`; add the missing values to the TS union. |
| U-03 | `scope` is concatenated from `claims` without separator; the BFF maps `AdditionalParamsJson` (meant for the upstream provider) into `Claims`. [BFF + U-Shell] | Garbage `scope` reaches the BFF; upstream parameters do not reach the upstream provider. | BFF stops publishing `AdditionalParamsJson` as `Claims` and appends upstream parameters itself to `targetAuthorizeUrl` during delegation (D-18). U-Shell joins scopes with a space. |
| U-04 | `buildOAuthUrl` does not URL-encode values (`redirect_uri`, base64 `state` with `+/=`). [U-Shell] | Broken parameters depending on content. | `encodeURIComponent` for every value. |
| U-05 | `state` has no random nonce and is not compared with a stored value. [U-Shell] | Login CSRF / token injection. | Add a nonce, keep it in `sessionStorage`, verify on callback. |
| U-06 | Callback removes `state`, `code`, `token` from the URL but **not `access_token`**; the controller sends the implicit token in the **query**. [U-Shell + AuthTokenHandling] | Token stays in URL, history and server logs. | U-Shell also removes `access_token`; AuthTokenHandling returns implicit tokens in the fragment (UI requirement R4). U-Shell already parses fragments. |
| U-07 | `isAuthenticated` checks only presence; the `exp` check is commented out; no reaction to 401. [U-Shell] | Expired token leads to failing calls instead of a new logon. | Enable `exp` check; on 401 delete the token and show `LogonPage`. |
| U-08 | `getTokenFromCode` uses GET without `grant_type`, `redirect_uri`, `code_verifier`; on errors it returns `"token_todo"`, which is then stored as a valid token. [U-Shell] | Code flow unusable; on failure the UI believes it is logged in. | Return `null` on failure (immediate fix). Proper POST + PKCE in D-07 phase 2. |
| U-09 | Datastores reference a **fixed** `tokenSourceUid`. With several logon sources, the token of the source actually used is stored under a different key. [U-Shell + BFF portfolio] | With multi-provider logon, backend calls go out without a token. | D-17. |
| U-10 | The UserManagement module frontend (`js/UniversalBFF.OobModules.UserManagement/src/bl/BackendService.ts`) hard-codes `tokenSourceUid` `00000000-0000-0000-0000-111111111111` and `baseUrl` `http://localhost:45357`. [this repo] | Does not work with real source uids or hosts. | Resolve both from the shell or portfolio (same mechanism as D-17). |
| U-11 | `PopupLogin` calls `window.open` during render; `IFrameLogin` creates intervals during render without cleanup; `performPopupOAuthLogin` clears its interval immediately. [U-Shell] | Duplicate popups and leaking timers. | Move into `useEffect` with cleanup. |
| U-12 | Iframe vs. delegation: in iframe mode the BFF delegates to Google/GitHub inside the iframe; those providers reject framing. [BFF data] | Logon fails silently. | Rule: for every external target `IframeSupported` must be `false` (enforce in `RegisterAuthTokenSources`). |
| Readable by any XSS. | Legacy mode for MVP. Target: BFF cookie pattern, introduced backward-compatible as opt-in mode (D-19, U-Shell UB-24). |
| U-14 | Logoff deletes only the local token; no server-side session end, no upstream logout. [U-Shell + BFF] | Accepted for MVP (tokens are stateless). | Later: optional end-session endpoint. |
| U-15 | `runtimeTagsFromTokenScope` decodes the JWT on the client; with D-14 (scopes recomputed on introspection) the UI sees the issue-time snapshot. [U-Shell] | UI tags can be stale until a new logon. | Accept; document. |

## 14. Incremental Roadmap

Guiding principle: build on the existing implementation in small steps, avoid breaking changes. Every step ends with a build and MSTest coverage of the touched path.

**Phase A: MVP (UC-LOCAL, UC-PROXY, UC-MULTI work end to end with implicit flow)**

1. Client id mapping per §9.1 (G-01) plus tests for every row of the audit table.
2. Finish `IntrospectAccessToken` per §9.2 (G-03); wire `BffUserService` directly into `AccessTokenValidator` (G-02).
3. Use one canonical local subject (email address allowed, D-08) for logon by email and by `SubjectId`, and create sessions only after successful credential checks (G-04).
4. Enforce `IframeSupported = false` for external targets (U-12).
5. U-Shell: `displayLabel`/`displayIconUrl` (U-01), URL encoding (U-04), remove `access_token` from the URL (U-06), no `"token_todo"` (U-08).
6. Token binding for datastores with several sources (U-09, D-17).

**Phase B: Robustness**

7. Longer lifetime for delegation sessions (G-08, D-10).
8. Display name and email from provider claims; stable cached identities (G-09); cache operations providers.
9. Upstream validation cache with fail-closed semantics (D-13); BFF JWT expiry capped to upstream expiry (D-15).
10. U-Shell `exp` check and 401 handling (U-07); timer and popup cleanup (U-11).
11. Skip empty consent (D-04, AuthTokenHandling UI rework).

**Phase C: Security**

12. Redirect URI validation per target (G-07).
13. Signing key from configuration or persisted per installation (G-11, D-09).
14. `state` nonce in U-Shell (U-05); implicit token in fragment (U-06, AuthTokenHandling).
15. Code + PKCE as additive issue mode (D-07 phase 2, D-11); afterwards deprecate implicit.
16. Caller authentication for `/oauth2/introspect` when exposed to remote consumers (G-12).
17. Real product and scope checks in `CanCurrentIdentityAccess*` (G-13).

### 14.1 Phase A test matrix (MSTest, `dotnet/test/UniversalBFF.Tests`)

The test project currently links only `BffUserService.OAuthCodeFlow.cs` and replaces DB-dependent siblings with stand-ins. The paths below need a test seam first (proposal: `UserManagementDbContext` configurable via `OnConfiguringCustom` with the EF InMemory provider, test-only package). Production changes require developer approval.

| Test | Covers | Expected |
|---|---|---|
| `RegisterAuthTokenSources_PublishesUidAsClientId` | G-01, §9.1 | `AuthTokenConfig.ClientId == target.Uid.ToString()`, inner `ClientId` absent |
| `RegisterAuthTokenSources_DoesNotPublishUpstreamParamsForProxyTargets` | D-18/D-20 | no `Claims` from `AdditionalParamsJson` for `Proxy` mode |
| `TryValidateApiClient_ResolvesByUid` | §9.1 | valid for `Uid`, invalid for inner `ClientId` |
| `TryAuthenticate_ResolvesTargetByUid` | §9.1 | local logon succeeds with `Uid` |
| `TryAuthenticate_WrongPassword_CreatesNoSession` | G-04 | `false`, no session |
| `LocalLogon_ByEmailAndBySubjectId_SameIdentity` | D-08 | same `sub`, one cached identity |
| `TryValidateApiClientSecret_NeverAcceptsUpstreamSecret` | D-11 | inner `ClientSecret` of external target rejected |
| `IntrospectAccessToken_ValidLocalToken_Active` | G-03, §9.2 | active, scopes recomputed from DB (D-14) |
| `IntrospectAccessToken_DisabledIdentity_Inactive` | §9.2 | inactive |
| `IntrospectAccessToken_UnknownOri_Inactive` | §9 rule 6 | inactive |
| `IntrospectAccessToken_ForeignSignature_Inactive` | §9.2 | inactive |
| `WrpEncryption_RoundTrip` / `_TamperedSlt_Inactive` | D-16 | decrypts with matching `slt`; tampering invalidates signature |
| `DelegationSession_ValidFor10Minutes` | D-10 | valid after 9 min, invalid after 11 min (injected clock) |
