# UniversalBFF Backlog

Clearly planned to-dos. Larger concepts and ideas that are not yet committed live in [`../epics/`](../epics/) (see [future directions](../epics/epic-future-directions.md)).

- Update the User Management OAuth skill and [user-management-oauth.md](../user-management-oauth.md) for the decision that the current identity is resolved via the ambient `AccessTokenManager` (AuthTokenHandling).
- Add integration tests for the database-backed part of `CanCurrentIdentityAccessProduct`.
- Implement `Passthrough` as an explicit `OAuthProxyTarget` mode (see [architecture.md](../architecture.md), "Authentication Use Cases And Responsibilities").
