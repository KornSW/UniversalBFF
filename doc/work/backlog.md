# UniversalBFF Backlog

Clearly planned to-dos. Larger concepts and ideas that are not yet committed live in [`../epics/`](../epics/) (see [future directions](../epics/epic-future-directions.md)).

- User Management module MVP and reference module: see [epic](../epics/epic-usermanagement-module-mvp.md); next: iterations I2 to I5.
- Solve the module description serialization NO GO centrally: [issue](issue-moduledescription-serialization.md).
- Assign the role `Administrator` to the initial local admin created by `BffUserService.CheckIfInitialStateShouldInstaled` (epic B-5).
- Fix the lock duration in `LocalCredentialService.TryAuthenticate`: `5 ^ (count - allowedFails)` is a XOR, not a power.
- Update the User Management OAuth skill and [user-management-oauth.md](../user-management-oauth.md) for the decision that the current identity is resolved via the ambient `AccessTokenManager` (AuthTokenHandling).
- Add integration tests for the database-backed part of `CanCurrentIdentityAccessProduct`.
- Implement `Passthrough` as an explicit `OAuthProxyTarget` mode (see [architecture.md](../architecture.md), "Authentication Use Cases And Responsibilities").
