using Security.AccessTokenHandling.OAuth;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.SmartStandards;

namespace UniversalBFF.OobModules.UserManagement {

  public partial class BffUserService {

    private readonly Dictionary<long, PendingAuthorizationCode> _TokensPerRetrievalCode = new Dictionary<long, PendingAuthorizationCode>();

    /// <summary>
    /// Creates a short-lived, client-bound code and retains the opaque PKCE challenge and resource.
    /// PKCE remains optional for existing clients; the OAuth controller verifies it when supplied.
    /// </summary>
    public bool TryValidateSessionIdAndCreateRetrievalCode(
      string apiClientId, string sessionId, string[] selectedScopes, string codeChallenge, string resource,
      out string code, out string message
    ) {
      code = null;
      if (string.IsNullOrWhiteSpace(apiClientId)) {
        message = "The client id is required.";
        return false;
      }

      if (!this.TryValidateSessionIdAndCreateToken(apiClientId, sessionId, selectedScopes, out TokenIssuingResult tokenResult)) {
        message = null;
        if (tokenResult != null) {
          message = tokenResult.error;
        }
        return false;
      }

      this.CleanupExpiredRetrievalCodes();
      long retrievalCode = Snowflake44.Generate();
      PendingAuthorizationCode pendingCode = new PendingAuthorizationCode(
        apiClientId, tokenResult, codeChallenge, resource, DateTime.UtcNow.AddMinutes(1)
      );
      lock (_TokensPerRetrievalCode) {
        _TokensPerRetrievalCode.Add(retrievalCode, pendingCode);
      }

      code = retrievalCode.ToString();
      message = null;
      return true;
    }

    /// <summary>
    /// Consumes a code once and returns its original PKCE challenge and resource for controller verification.
    /// Expired codes and failed redemption attempts cannot be retried.
    /// </summary>
    public TokenIssuingResult RetrieveTokenByCode(
      string clientId, string clientSecret, string code, out string codeChallenge, out string resource
    ) {
      codeChallenge = null;
      resource = null;
      if (!long.TryParse(code, out long retrievalCode)) {
        return TokenIssuingResult.FromError("invalid_grant", "The code is invalid.");
      }

      PendingAuthorizationCode pendingCode;
      lock (_TokensPerRetrievalCode) {
        if (!_TokensPerRetrievalCode.TryGetValue(retrievalCode, out pendingCode)) {
          return TokenIssuingResult.FromError("invalid_grant", "The code is invalid or was already used.");
        }
        _TokensPerRetrievalCode.Remove(retrievalCode);
      }

      if (pendingCode.ExpiresUtc <= DateTime.UtcNow) {
        return TokenIssuingResult.FromError("invalid_grant", "The code expired.");
      }
      if (!string.Equals(pendingCode.ClientId, clientId, StringComparison.Ordinal)) {
        return TokenIssuingResult.FromError("invalid_grant", "The code was issued to another client.");
      }
      if (!this.TryValidateApiClientSecret(clientId, clientSecret)) {
        return TokenIssuingResult.FromError("invalid_client", "Unknown client.");
      }

      codeChallenge = pendingCode.CodeChallenge;
      resource = pendingCode.Resource;
      return pendingCode.TokenResult;
    }

    /// <summary>
    /// Describes the existing code and legacy implicit flows without advertising unsupported grants.
    /// Endpoint routes and PKCE S256 verification belong to the AuthTokenHandling controllers.
    /// </summary>
    public AuthorizationServerCapabilities GetAuthorizationServerCapabilities() {
      AuthorizationServerCapabilities capabilities = new AuthorizationServerCapabilities();
      capabilities.ResponseTypesSupported = new string[] { "code", "token" };
      capabilities.GrantTypesSupported = new string[] { "authorization_code", "implicit" };
      capabilities.CodeChallengeMethodsSupported = new string[] { "S256" };
      capabilities.TokenEndpointAuthMethodsSupported = new string[] { "client_secret_post" };
      capabilities.ScopesSupported = Array.Empty<string>();
      capabilities.SupportsDynamicClientRegistration = false;
      return capabilities;
    }

    /// <summary>
    /// Rejects dynamic registration; BFF clients continue to be provisioned through the existing database.
    /// </summary>
    public bool TryRegisterClient(
      string clientName, string[] redirectUris, string tokenEndpointAuthMethod,
      out string clientId, out string error, out string errorDescription
    ) {
      clientId = null;
      error = "invalid_client_metadata";
      errorDescription = "Dynamic client registration is not supported. Use a provisioned BFF client.";
      return false;
    }

    /// <summary>
    /// Removes expired codes together with their token and bound authorization metadata.
    /// </summary>
    private void CleanupExpiredRetrievalCodes() {
      DateTime utcNow = DateTime.UtcNow;
      lock (_TokensPerRetrievalCode) {
        foreach (long code in _TokensPerRetrievalCode.Keys.ToArray()) {
          if (_TokensPerRetrievalCode[code].ExpiresUtc <= utcNow) {
            _TokensPerRetrievalCode.Remove(code);
          }
        }
      }
    }

    private sealed class PendingAuthorizationCode {

      private readonly string _ClientId;
      private readonly TokenIssuingResult _TokenResult;
      private readonly string _CodeChallenge;
      private readonly string _Resource;
      private readonly DateTime _ExpiresUtc;

      /// <summary>
      /// Keeps all values bound to an authorization code in one entry with a common lifetime.
      /// </summary>
      public PendingAuthorizationCode(
        string clientId, TokenIssuingResult tokenResult, string codeChallenge, string resource, DateTime expiresUtc
      ) {
        _ClientId = clientId;
        _TokenResult = tokenResult;
        _CodeChallenge = codeChallenge;
        _Resource = resource;
        _ExpiresUtc = expiresUtc;
      }

      public string ClientId {
        get { return _ClientId; }
      }

      public TokenIssuingResult TokenResult {
        get { return _TokenResult; }
      }

      public string CodeChallenge {
        get { return _CodeChallenge; }
      }

      public string Resource {
        get { return _Resource; }
      }

      public DateTime ExpiresUtc {
        get { return _ExpiresUtc; }
      }

    }

  }

}
