using Security.AccessTokenHandling;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Registers the tokens issued by this BFF as token source at the AccessTokenManager of AuthTokenHandling,
  /// so that the AccessTokenValidator (wired via 'ConfigureTokenValidationViaAccessTokenManager' in the
  /// UniversalBFF core) can validate them for every incoming UJMW call.
  /// </summary>
  public partial class BffUserService {

    private static bool _TokenSourceRegistered = false;

    /// <summary>
    /// Registers issuer + introspector of the BFF tokens (using the persisted or configured signing secret)
    /// and routes every token with the BFF issuer to them. Must be called during startup.
    /// </summary>
    internal static void RegisterTokenSourceAtAccessTokenManager() {
      lock (_JwtToolsSyncRoot) {
        if (_TokenSourceRegistered) {
          return;
        }
        _TokenSourceRegistered = true;
      }

      EnsureJwtTools();

      AccessTokenManager.ConfigureTokenSupply((cfg) => {
        cfg.ApplyCustomTokenSource(BffTokenSourceId, _LocalIssuer, _LocalIntrospector);
      });
      AccessTokenManager.MapIssuer(BffTokenSourceId, _LocalJwtIssuer);
    }

  }

}
