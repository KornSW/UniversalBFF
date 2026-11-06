namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Constants identifying the tokens issued by this BFF. Kept in a separate file without further
  /// dependencies, so that test projects can link it together with single production partials.
  /// </summary>
  public partial class BffUserService {

    private const string _LocalJwtIssuer = "UniversalBFF";
    private const string _LocalJwtAud = "UniversalBFF";

    /// <summary>
    /// The token source id of the tokens issued by this BFF (equal to their issuer).
    /// </summary>
    internal const string BffTokenSourceId = _LocalJwtIssuer;

  }

}
