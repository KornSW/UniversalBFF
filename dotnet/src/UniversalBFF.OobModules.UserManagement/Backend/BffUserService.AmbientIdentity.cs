using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalBFF.OobModules.UserManagement {

  /// <summary>
  /// Resolves the ambient ("current") identity via the AccessTokenManager of AuthTokenHandling.
  /// The AccessTokenValidator pushes every validated incoming token into the per-interaction register
  /// of the AccessTokenManager, so subject and scopes of the current caller are available there
  /// without any additional lookup.
  /// </summary>
  public partial class BffUserService {

    private const string _TenantScopePrefix = "Tenant:";

    private readonly List<string> _KnownTokenSourceIds = new List<string>();

    /// <summary>
    /// Remembers a token source id which belongs to this BFF, so that its current token is
    /// considered when resolving the ambient identity (in registration order).
    /// </summary>
    internal void RegisterKnownTokenSourceId(string tokenSourceId) {
      if (string.IsNullOrWhiteSpace(tokenSourceId)) {
        return;
      }
      lock (_KnownTokenSourceIds) {
        if (!_KnownTokenSourceIds.Contains(tokenSourceId, StringComparer.OrdinalIgnoreCase)) {
          _KnownTokenSourceIds.Add(tokenSourceId);
        }
      }
    }

    /// <summary>
    /// Returns the token source ids to be checked for an ambient token: the default token source
    /// first, followed by all token sources registered by this BFF.
    /// </summary>
    private string[] GetCandidateTokenSourceIds() {
      List<string> candidates = new List<string>();
      candidates.Add(AccessTokenManager.DefaultTokenSourceId);
      lock (_KnownTokenSourceIds) {
        candidates.AddRange(_KnownTokenSourceIds);
      }
      return candidates.ToArray();
    }

    /// <summary>
    /// Tries to resolve subject and scopes of the ambient identity from the AccessTokenManager.
    /// Returns false (fail closed) if there is no current token for any of the candidate token sources.
    /// </summary>
    internal bool TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes) {
      foreach (string candidate in this.GetCandidateTokenSourceIds()) {
        if (AccessTokenManager.HasCurrentAccessToken(candidate)) {
          tokenSourceId = candidate;
          subject = AccessTokenManager.GetCurrentSubject(candidate);
          scopes = AccessTokenManager.GetCurrentScopes(candidate);
          return true;
        }
      }
      tokenSourceId = null;
      subject = null;
      scopes = new string[0];
      return false;
    }

    /// <summary>
    /// Evaluates whether the given scopes contain 'scopeName:scopeValue'.
    /// If scopeValue is empty, the plain scope name (or any value of it) is sufficient.
    /// </summary>
    internal static bool ContainsScope(string[] scopes, string scopeName, string scopeValue) {
      if (scopes == null || string.IsNullOrWhiteSpace(scopeName)) {
        return false;
      }
      if (string.IsNullOrEmpty(scopeValue)) {
        string prefix = scopeName + ":";
        return scopes.Any((s) => string.Equals(s, scopeName, StringComparison.Ordinal) || s.StartsWith(prefix, StringComparison.Ordinal));
      }
      string expected = scopeName + ":" + scopeValue;
      return scopes.Any((s) => string.Equals(s, expected, StringComparison.Ordinal));
    }

    /// <summary>
    /// Extracts all tenant uids from scopes like 'Tenant:1111111111111111111'.
    /// </summary>
    internal static long[] GetTenantUidsFromScopes(string[] scopes) {
      List<long> tenantUids = new List<long>();
      if (scopes == null) {
        return tenantUids.ToArray();
      }
      foreach (string scope in scopes) {
        if (scope != null && scope.StartsWith(_TenantScopePrefix, StringComparison.Ordinal)) {
          if (long.TryParse(scope.Substring(_TenantScopePrefix.Length), out long tenantUid) && !tenantUids.Contains(tenantUid)) {
            tenantUids.Add(tenantUid);
          }
        }
      }
      return tenantUids.ToArray();
    }

    /// <summary>
    /// Evaluates the 'AvailablePortfolios' value of a tenant ('*' or a ';'-separated list of product names).
    /// </summary>
    internal static bool IsProductAvailable(string availablePortfolios, string productName) {
      if (string.IsNullOrWhiteSpace(availablePortfolios) || string.IsNullOrWhiteSpace(productName)) {
        return false;
      }
      if (availablePortfolios.Trim() == "*") {
        return true;
      }
      string[] products = availablePortfolios.Split(';');
      return products.Any((p) => string.Equals(p.Trim(), productName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns true, if at least one of the given 'AvailablePortfolios' values contains the product.
    /// </summary>
    internal static bool CanAccessProduct(string[] availablePortfoliosOfLegitimatedTenants, string productName) {
      if (availablePortfoliosOfLegitimatedTenants == null) {
        return false;
      }
      return availablePortfoliosOfLegitimatedTenants.Any((ap) => IsProductAvailable(ap, productName));
    }

  }

}
