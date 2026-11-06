using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.SmartStandards;
using System.Text;
using UniversalBFF.OobModules.UserManagement.Frontend.Contract;

namespace UniversalBFF.OobModules.UserManagement {


  public partial class BffUserService : IOAuthService {

    private const int _LocalJwtTokenLifetimeMinutes = 240;
    private const int _AuthFlowSessionLifetimeMinutes = 10;

    private static readonly object _JwtToolsSyncRoot = new object();
    private static LocalJwtIssuer _LocalIssuer = null;
    private static LocalJwtIntrospector _LocalIntrospector = null;

    private LocalCredentialService _LocalCredentialService = new LocalCredentialService();

    /// <summary>
    /// Lazily creates the issuer/introspector pair using the persisted (or configured) signing secret.
    /// </summary>
    private static void EnsureJwtTools() {
      if (_LocalIssuer != null && _LocalIntrospector != null) {
        return;
      }
      lock (_JwtToolsSyncRoot) {
        if (_LocalIssuer == null) {
          _LocalIssuer = new LocalJwtIssuer(
            BffTokenSecrets.SigningSecret, _LocalJwtTokenLifetimeMinutes,
            passtroughAllRequestedClaims: true, enforcedIssuer: _LocalJwtIssuer
          );
        }
        if (_LocalIntrospector == null) {
          _LocalIntrospector = new LocalJwtIntrospector(BffTokenSecrets.SigningSecret);
        }
      }
    }

    #region " TypeIndexer (Instance-Discovery-Getter) "

    internal ITypeIndexer TypeIndexer {
      get {
        ITypeIndexer typeIndexer = InstanceDiscoveryContext.Current.GetInstance<ITypeIndexer>(true);
        return typeIndexer;
      }
    }

    #endregion

    #region " Singleton ProviderRepository<IAccessTokenIssuer> "

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ProviderRepository<IAccessTokenIssuer> _Issuers = null;

    internal IAccessTokenIssuer[] Issuers {
      get {
        if(_Issuers == null) {
          ITypeIndexer typeIndexer = InstanceDiscoveryContext.Current.GetInstance<ITypeIndexer>();
          _Issuers = new ProviderRepository<IAccessTokenIssuer>(typeIndexer);
        }
        return _Issuers.Providers;
      }
    }

    #endregion

    #region " Singleton ProviderRepository<IAccessTokenIntrospector> "

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private ProviderRepository<IAccessTokenIntrospector> _Introspectors = null;

    internal IAccessTokenIntrospector[] Introspectors {
      get {
        if (_Introspectors == null) {
          ITypeIndexer typeIndexer = InstanceDiscoveryContext.Current.GetInstance<ITypeIndexer>();
          _Introspectors = new ProviderRepository<IAccessTokenIntrospector>(typeIndexer);
        }
        return _Introspectors.Providers;
      }
    }

    #endregion

    #region " Factory-Data "

    private void CheckIfInitialStateShouldInstaled() {

      UserManagementDbContext.Migrate();

      using (UserManagementDbContext db = new UserManagementDbContext()) {

        if (db.TenantScopes.Any()) {
          return;
        }

        TenantScopeEntity tenantScope = new TenantScopeEntity {
          TenantUid = 1111111111111111111L,
          AvailablePortfolios = "*",
          DisplayLabel = "default",
          PermittedScopes = "DefaultTenant"
        };
        db.TenantScopes.Add(tenantScope);

        tenantScope.Roles.Add(new RoleEntity {
          RoleName = "User",
          RoleDescriptiveLabel = "Standard-User",
          IsDefaultRoleForNewUsers = true, //everybody will automatically assignes to this role!
          PermittedScopes = "",
        });

        tenantScope.Roles.Add(new RoleEntity {        
          RoleName = "Administrator",
          RoleDescriptiveLabel = "Administrator",
          PermittedScopes = "ABL:UserManagement",
        });

        OAuthProxyTargetEntity oauth = new OAuthProxyTargetEntity();

        //always differrent (for security-reasons)
        oauth.Uid = Snowflake44.Generate();
        oauth.ClientId = Snowflake44.Generate().ToString();
        oauth.ClientSecret = Snowflake44.Generate().ToString();

        oauth.TenantUid = tenantScope.TenantUid;
        oauth.AuthUrl = _OurProxyAuthUrl;
        oauth.DisplayLabel = "Logon (System-User)";
        oauth.DisplayIconUrl = "";
        oauth.RetrivalUrl = _OurProxyRetrivalUrl;

        oauth.ProviderClassName = typeof(LocalCredentialService).FullName;
        oauth.AdditionalParamsJson = "{ }";
        oauth.IntrospectorParamsJson = "{ }";
        oauth.IframeSupported = true;

        db.OAuthProxyTargets.Add(oauth);

        string salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).Substring(0, 4).ToLower();
        long subjectId = Snowflake44.Generate();
        string first4DigitsOfSubjectId = subjectId.ToString().Substring(0, 4);
        LocalCredentialEntity localAdmin = new LocalCredentialEntity {
          SubjectId = subjectId,
          DisplayName = "Admin (" + salt + ")",
          //INITIAL PASSWORD IS THE FIRST 4 DIGITS OF THE SUBJECTID + SALT (both can be seen in db)
          PasswordHash = _LocalCredentialService.GetPasswordHash(first4DigitsOfSubjectId + salt),
          CreationDate = DateTime.Now,
          EmailAddress = "admin@localhost",
          IsValidated = true,
        };

        db.LocalCredentials.Add(localAdmin);

        db.CachedUserIdentities.Add(new CachedUserIdentityEntity {
           OriginUid = oauth.Uid,
           OriginSpecificSubjectId = localAdmin.SubjectId.ToString(),
           CachedDisplayName = localAdmin.DisplayName,
           CachedEmailAddress = localAdmin.EmailAddress,
           CreationDate = localAdmin.CreationDate,
           Disabled = false,
           LastLogonDate = localAdmin.CreationDate,
           PermittedScopes = ""
        });

        db.KnownUserLegitimations.Add(new KnownUserLegitimationEntity {
           OriginUid = oauth.Uid,
           OriginSpecificSubjectId = localAdmin.SubjectId.ToString(),
           TenantUid = tenantScope.TenantUid,
           RoleName = "Administrator",
        });

        db.SaveChanges();
      }
    }

    #endregion


    //NUR WENN WIR LOKAL ARBETEN
    public bool TryAuthenticate(
      string apiClientId, string login, string password, bool noPasswordNeeded, string clientProvidedState,
      out string sessionId, out string message
    ) {

      if (noPasswordNeeded) {
        message = $"Passtrough-Auth is currently not supported!";
        SecLogger.LogError(2079222383703567499L, 77307, "TryAuthenticate failed: " + message);
        sessionId = null;
        return false;
      }

      OAuthProxyTargetEntity target = this.FindTargetByPublicClientId(apiClientId);

      if (target == null) {
        message = $"Invalid apiClientId '{apiClientId}'";
        SecLogger.LogError(2079222383703567498L, 77305, "TryAuthenticate failed: " + message);
        sessionId = null;
        return false;
      }

      if (!IsLocalTarget(target)) {
        //external targets are handled exclusively via CodeFlowDelegationRequired (redirect + StateBag),
        //a credential-prompt must never be served for them
        message = $"Logon via '{target.DisplayLabel}' requires a redirect to the external provider.";
        SecLogger.LogError(2085310457913180162L, 77306, "TryAuthenticate failed: " + message);
        sessionId = null;
        return false;
      }

      if (!_LocalCredentialService.TryAuthenticate(login, password, out message, out LocalCredentialEntity credential)) {
        sessionId = null;
        return false;
      }

      //canonical subject: the numeric SubjectId - regardless if the logon was done via email or id
      sessionId = this.CreateSession(credential.SubjectId.ToString(), target.Uid, target.ProviderClassName);
      this.UpdateSessionIdentityInfo(sessionId, credential.DisplayName, credential.EmailAddress);
      return true;

    }

    public bool TryGetAvailableScopesBySessionId(
      string apiClientId, string sessionId, string[] prefferedScopes,
      out ScopeDescriptor[] availableScopes, out string message
    ) {

      if (TryValidateSessionId(sessionId, out AuthFlowSession session)) {
        availableScopes = this.GetAvailableScopes(session.LogonNameOrSubject, prefferedScopes);
        message = null;
        return true;
      }
      else {
        availableScopes = Array.Empty<ScopeDescriptor>();
        message = "Invalid or expired sessionOtp";
        return false;
      }

    }

    protected ScopeDescriptor[] GetAvailableScopes(
      string loginOrClientId, string[] scopesToSelect
    ) {

      //im universalbff wollen wir keine consent-prompts!
      return new ScopeDescriptor[] { };

      ////aus db holen? nur lokal oder für alle???
      //IOAuthServiceWithDelegation rakommen?

      //return new ScopeDescriptor[] {
      //  new ScopeDescriptor {
      //    Expression = "read", Label = "Read Data",
      //    Selected = true,//mandatory!
      //    ReadOnly= true, Invisible= false
      //  },
      //  new ScopeDescriptor {
      //    Expression = "write", Label = "Write Data",
      //    Selected = scopesToSelect.Contains("write"),
      //    ReadOnly= false, Invisible= false
      //  },
      //};

    }

    #region " IMPLICIT - FLOW "

    public bool TryValidateSessionIdAndCreateToken(
      string apiClientId, string sessionId, string[] selectedScopes,
      out TokenIssuingResult tokenResult
    ) {

      tokenResult = new TokenIssuingResult();

      if (TryValidateSessionId(sessionId, out AuthFlowSession session)) {

        //for security selectedScopes needs be be filtered again because some value could have been injected
        selectedScopes = this.GetAvailableScopes(session.LogonNameOrSubject, selectedScopes).ToStringArray();

        using (UserManagementDbContext db = new UserManagementDbContext()) {

          //OAuthProxyTargetEntity origin = db.OAuthProxyTargets.Where(o => o.ClientId == apiClientId).FirstOrDefault();
          OAuthProxyTargetEntity origin = db.OAuthProxyTargets.Where(o => o.Uid == session.OriginUid).FirstOrDefault();

          if (origin == null) {
            tokenResult.error = $"The client_id '{apiClientId}' is not valid!";
            tokenResult.error_description = $"The client_id '{apiClientId}' is not valid!";
            SecLogger.LogError($"The client_id '{apiClientId}' is not valid (in {nameof(TryValidateSessionIdAndCreateToken)})!");
            return false;
          }

          List<string> allScopes = new List<string>();
          allScopes.Add($"Tenant:{origin.TenantUid}");  
          foreach (string s in origin.TenantScope.PermittedScopes.Split(' ')) {
            if (!string.IsNullOrEmpty(s) && !allScopes.Contains(s)) {
              allScopes.Add(s);
            }
          }

          CachedUserIdentityEntity cachedIdentity = db.CachedUserIdentities.Where(
            c => c.OriginUid == origin.Uid && c.OriginSpecificSubjectId == session.LogonNameOrSubject //provider-resolved-subject
          ).FirstOrDefault();

          RoleEntity[] rolesToAssign;

    
          if (cachedIdentity != null) {

            if (cachedIdentity.Disabled) {
              tokenResult.error = $"IDENTITY is DISABLED!";
              tokenResult.error_description = $"IDENTITY is DISABLED!"; ;
              SecLogger.LogError($"IDENTITY '{session.LogonNameOrSubject}' is DISABLED! (in {nameof(TryValidateSessionIdAndCreateToken)})!");
              return false;
            }

            rolesToAssign = db.KnownUserLegitimations.Where(
              (r) => r.OriginUid == origin.Uid && r.OriginSpecificSubjectId == session.LogonNameOrSubject
            ).Select((l)=> l.Role).ToArray();

            foreach (string s in cachedIdentity.PermittedScopes.Split(' ')) {
              if (!string.IsNullOrEmpty(s) && !allScopes.Contains(s)) {
                allScopes.Add(s);
              }
            }

          }
          else {

            cachedIdentity = new CachedUserIdentityEntity();
            cachedIdentity.OriginUid = origin.Uid;
            cachedIdentity.OriginSpecificSubjectId = session.LogonNameOrSubject;
            cachedIdentity.Disabled = false;
            cachedIdentity.CreationDate = DateTime.Now;
            cachedIdentity.PermittedScopes = "";  //NOT FROM tokenResult.scope, because we'll maintain this primary in the db;

            db.CachedUserIdentities.Add(cachedIdentity);

            rolesToAssign = db.Roles.Where((r) => r.TenantUid == origin.TenantUid && r.IsDefaultRoleForNewUsers).ToArray();

            foreach (RoleEntity roleToAssign in rolesToAssign) {
              db.KnownUserLegitimations.Add(new KnownUserLegitimationEntity {
                OriginUid = origin.Uid,
                OriginSpecificSubjectId = session.LogonNameOrSubject,
                TenantUid = roleToAssign.TenantUid,
                RoleName = roleToAssign.RoleName
              });
            }

          }

          cachedIdentity.CachedDisplayName = session.UserDisplayName;
          cachedIdentity.CachedEmailAddress = session.UserEmailAddress;
          cachedIdentity.CachedImage = session.UserImage;
          cachedIdentity.LastLogonDate = DateTime.Now;

          foreach (RoleEntity roleToAssign in rolesToAssign) {
            allScopes.Add("Role:" + roleToAssign.RoleName);
            foreach (string s in roleToAssign.PermittedScopes.Split(' ')) {
              if (!string.IsNullOrEmpty(s) && !allScopes.Contains(s)) {
                allScopes.Add(s);
              }
            }
          }

          string scopeString = string.Join(' ', allScopes.Distinct().OrderBy((s) => s));

          Dictionary<string, object> claimsToRequest = new Dictionary<string, object> {
            { "iss", _LocalJwtIssuer },
            { "aud", _LocalJwtAud },
            { "jti", sessionId },
            { "sub", cachedIdentity.OriginSpecificSubjectId },
            { "scope", scopeString },
            { "ori", origin.Uid.ToString() }
          };

          if (session.TokenResultFromDelegate != null && !string.IsNullOrWhiteSpace(session.TokenResultFromDelegate.access_token)) {
            string salt = WrappedTokenProtector.CreateSalt();
            claimsToRequest["slt"] = salt;
            claimsToRequest["wrp"] = WrappedTokenProtector.Protect(
              session.TokenResultFromDelegate.access_token, salt, BffTokenSecrets.EncryptionSecret
            );
          }

          EnsureJwtTools();
          _LocalIssuer.TryRequestAccessToken(claimsToRequest, out TokenIssuingResult atWrap);
          tokenResult.access_token = atWrap.access_token;
          tokenResult.token_type = atWrap.token_type;
          tokenResult.expires_in = atWrap.expires_in;
          tokenResult.scope = atWrap.scope;

          //_LocalIssuer.TryRequestAccessToken(
          //  new Dictionary<string, object> {
          //    { "iss", $"{_LocalJwtIssuer}"},
          //    { "aud", $"{_LocalJwtAud}"},
          //    { "jti", sessionId },
          //    { "sub", cachedIdentity.OriginSpecificSubjectId },
          //    { "scope",scopeString },
          //    { "ori", origin.Uid },
          //    { "wrp", session.TokenResultFromDelegate?.id_token }
          //  },
          //  out TokenIssuingResult idWrap
          //);
          //tokenResult.id_token = idWrap.id_token;

          //_LocalIssuer.TryRequestAccessToken(
          //  new Dictionary<string, object> {
          //    { "iss", $"{_LocalJwtIssuer}"},
          //    { "aud", $"{_LocalJwtAud}"},
          //    { "jti", sessionId },
          //    { "sub", cachedIdentity.OriginSpecificSubjectId },
          //    { "scope",scopeString },
          //    { "ori", origin.Uid },
          //    { "wrp", session.TokenResultFromDelegate?.refresh_token }
          //  },
          //  out TokenIssuingResult rfWrap
          //);
          //tokenResult.refresh_token = rfWrap.refresh_token;

          db.SaveChanges();

        }

        //a logon-session can only be redeemed once
        this.RemoveSession(sessionId);

        return true;
      }
      else {
        tokenResult.error = "Invalid or expired logon-session";
        tokenResult.error_description = "Invalid or expired logon-session";
        return false;
      }

    }

    #endregion

    #region " CLIENT CREDENTIAL - FLOW "

    public TokenIssuingResult ValidateClientAndCreateToken(
      string clientId, string clientSecret, string[] selectedScopes
    ) {

      if (!long.TryParse(clientId, out long numericClientId) || string.IsNullOrEmpty(clientSecret)) {
        return TokenIssuingResult.FromError("invalid_client", "Unknown client");
      }

      using (UserManagementDbContext db = new UserManagementDbContext()) {

        ApiOAuthClientEntity apiClient = db.ApiOAuthClients.Where((c) => c.ClientId == numericClientId).FirstOrDefault();

        if (apiClient == null || !FixedTimeStringEquals(apiClient.ClientSecret, clientSecret)) {
          SecLogger.LogWarning(2085310457913180163L, 77320, $"Client-Credentials: invalid client '{clientId}'");
          return TokenIssuingResult.FromError("invalid_client", "Unknown client");
        }
        if (apiClient.ValidUntil.HasValue && apiClient.ValidUntil.Value < DateTime.Now) {
          SecLogger.LogWarning(2085310457913180164L, 77321, $"Client-Credentials: client '{clientId}' has expired");
          return TokenIssuingResult.FromError("invalid_client", "Client expired");
        }

        //the api-client acts on behalf of a local credential (MappedSubjectId) -> resolve its local origin
        LocalCredentialEntity mappedCredential = db.LocalCredentials.Where((c) => c.SubjectId == apiClient.MappedSubjectId).FirstOrDefault();
        OAuthProxyTargetEntity localOrigin = db.OAuthProxyTargets.Where(
          (o) => o.ProviderClassName == LocalCredentialProviderClassName
        ).FirstOrDefault();

        if (mappedCredential == null || localOrigin == null) {
          SecLogger.LogError(2085310457913180165L, 77322, $"Client-Credentials: client '{clientId}' is not mapped to a valid local identity");
          return TokenIssuingResult.FromError("invalid_client", "Client is not mapped to a valid identity");
        }

        string subject = mappedCredential.SubjectId.ToString();

        if (!this.TryBuildPermittedScopes(db, localOrigin, subject, out string scopeString, out string errorMessage)) {
          return TokenIssuingResult.FromError("invalid_client", errorMessage);
        }

        //requested scopes can only narrow the server-granted scopes
        scopeString = NarrowScopes(scopeString, selectedScopes);

        EnsureJwtTools();
        _LocalIssuer.TryRequestAccessToken(
          new Dictionary<string, object> {
            { "iss", _LocalJwtIssuer },
            { "aud", _LocalJwtAud },
            { "jti", Snowflake44.Generate().ToString() },
            { "sub", subject },
            { "scope", scopeString },
            { "ori", localOrigin.Uid.ToString() },
            { "cid", clientId }
          },
          out TokenIssuingResult tokenResult
        );

        return tokenResult;
      }
    }

    #endregion

    #region " REFRESH TOKEN - FLOW "

    public TokenIssuingResult CreateFollowUpToken(string refreshToken) {
      TokenIssuingResult tokenResult = new TokenIssuingResult();

      tokenResult.error = "invalid_request";
      tokenResult.error_description = "Refresh-Token currently not supported";

      return tokenResult;
    }

    #endregion

    #region " Introspection (RFC7662) "

    /// <summary>
    /// Validates a BFF-issued token (fail closed):
    /// 1. signature/exp/nbf, 2. iss/aud, 3. origin ('ori') exists,
    /// 4. identity is known and not disabled, 5. for non-local origins the encrypted upstream token
    /// ('wrp' + 'slt') is decrypted and validated by the issuing provider (if it supports that).
    /// The wrapped upstream token is never exposed via the returned claims.
    /// </summary>
    public void IntrospectAccessToken(string rawToken, out bool isActive, out Dictionary<string, object> claims) {

      EnsureJwtTools();
      _LocalIntrospector.IntrospectAccessToken(rawToken, out isActive, out claims);

      if (claims == null) {
        claims = new Dictionary<string, object>();
      }
      if (!isActive) {
        return;
      }

      if (!string.Equals(GetClaimString(claims, "iss"), _LocalJwtIssuer, StringComparison.Ordinal) ||
          !string.Equals(GetClaimString(claims, "aud"), _LocalJwtAud, StringComparison.Ordinal)) {
        Deactivate(out isActive, out claims, "Token was not issued by this BFF");
        return;
      }

      string subject = GetClaimString(claims, "sub");
      if (string.IsNullOrWhiteSpace(subject) || !long.TryParse(GetClaimString(claims, "ori"), out long originUid)) {
        Deactivate(out isActive, out claims, "Token has no valid origin or subject");
        return;
      }

      string wrapped = GetClaimString(claims, "wrp");
      string salt = GetClaimString(claims, "slt");

      //never leak the (encrypted) upstream token or its salt to callers
      claims.Remove("wrp");
      claims.Remove("slt");

      using (UserManagementDbContext db = new UserManagementDbContext()) {

        OAuthProxyTargetEntity origin = db.OAuthProxyTargets.Where((o) => o.Uid == originUid).FirstOrDefault();
        if (origin == null) {
          Deactivate(out isActive, out claims, "Unknown origin");
          return;
        }

        CachedUserIdentityEntity identity = db.CachedUserIdentities.Where(
          (c) => c.OriginUid == originUid && c.OriginSpecificSubjectId == subject
        ).FirstOrDefault();

        if (identity == null) {
          Deactivate(out isActive, out claims, "Unknown identity");
          return;
        }
        if (identity.Disabled) {
          Deactivate(out isActive, out claims, "Identity is disabled");
          return;
        }

        if (IsLocalTarget(origin)) {
          //local origin: the BFF itself is the authority, no wrapped token expected
          if (!string.IsNullOrWhiteSpace(wrapped)) {
            Deactivate(out isActive, out claims, "Unexpected wrapped token for local origin");
          }
          return;
        }

        //proxy origin: the upstream token must be present and is validated by its issuing provider
        if (!WrappedTokenProtector.TryUnprotect(wrapped, salt, BffTokenSecrets.EncryptionSecret, out string upstreamToken)) {
          Deactivate(out isActive, out claims, "Wrapped upstream token is missing or invalid");
          return;
        }

        IOAuthOperationsProvider provider = this.GetOAuthOperationsProvider(origin);
        if (provider.HasCapability("introspection")) {
          if (provider.TryValidateToken(upstreamToken, out bool upstreamActive, out DateTime? upstreamValidUntil, out string invalidReason)) {
            if (!upstreamActive) {
              Deactivate(out isActive, out claims, "Upstream token is inactive: " + invalidReason);
              return;
            }
          }
          else {
            Deactivate(out isActive, out claims, "Upstream token could not be validated by its provider");
            return;
          }
        }
      }

    }

    /// <summary>
    /// Marks an introspection result as inactive (with a reason) and logs it.
    /// </summary>
    private static void Deactivate(out bool isActive, out Dictionary<string, object> claims, string reason) {
      isActive = false;
      claims = new Dictionary<string, object>();
      claims["inactive_reason"] = reason;
      SecLogger.LogWarning(2085310457913180166L, 77330, "Token introspection: " + reason);
    }

    private static string GetClaimString(Dictionary<string, object> claims, string claimName) {
      if (claims.TryGetValue(claimName, out object value) && value != null) {
        return value.ToString();
      }
      return null;
    }

    #endregion

    public bool TryValidateApiClient(
      string apiClientId, string apiCallerHost, string redirectUri,
      out string message
    ) {
      message = string.Empty;

      OAuthProxyTargetEntity target = this.FindTargetByPublicClientId(apiClientId);
      if (target == null) {
        message = $"The client_id '{apiClientId}' is not valid in this context.";
        return false;
      }

      //the public client_ids (= OAuthProxyTarget.Uid) are only used by the BFF-hosted UI,
      //so the redirect_uri must always point to the BFF itself (same host as the authorize call)
      if (!IsRedirectUriOnCallerHost(redirectUri, apiCallerHost)) {
        message = $"The redirect_uri is not permitted for client_id '{apiClientId}'.";
        SecLogger.LogWarning(2085310457913180167L, 77340, $"Rejected redirect_uri '{redirectUri}' for client '{apiClientId}' (caller host '{apiCallerHost}')");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Validates the secret of a public BFF client. The public client_id is the OAuthProxyTarget.Uid,
    /// the inner (upstream) ClientId/ClientSecret never leave the server, so the BFF-hosted UI
    /// (a public client) authenticates without secret. A supplied secret is never accepted.
    /// </summary>
    public bool TryValidateApiClientSecret(
      string apiClientId, string apiClientSecret
    ) {

      OAuthProxyTargetEntity target = this.FindTargetByPublicClientId(apiClientId);
      if (target == null) {
        return false;
      }

      return string.IsNullOrEmpty(apiClientSecret);
    }

    #region " Helpers "

    private const string LocalCredentialProviderClassName = "UniversalBFF.OobModules.UserManagement.LocalCredentialService";

    private static bool IsLocalTarget(OAuthProxyTargetEntity target) {
      return (target.ProviderClassName == typeof(LocalCredentialService).FullName);
    }

    /// <summary>
    /// Resolves an OAuthProxyTarget by its PUBLIC client id (= Uid).
    /// </summary>
    private OAuthProxyTargetEntity FindTargetByPublicClientId(string apiClientId) {
      if (!long.TryParse(apiClientId, out long targetUid)) {
        return null;
      }
      using (UserManagementDbContext db = new UserManagementDbContext()) {
        return db.OAuthProxyTargets.Where((o) => o.Uid == targetUid).FirstOrDefault();
      }
    }

    /// <summary>
    /// Accepts relative redirect uris and absolute ones targeting the calling host.
    /// </summary>
    private static bool IsRedirectUriOnCallerHost(string redirectUri, string apiCallerHost) {
      if (string.IsNullOrWhiteSpace(redirectUri)) {
        return false;
      }
      if (redirectUri.StartsWith("/") && !redirectUri.StartsWith("//")) {
        return true;
      }
      if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri parsed)) {
        return false;
      }
      if (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) {
        return false;
      }
      return string.Equals(parsed.Host, apiCallerHost, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the DB-derived scope string for an identity (tenant, identity, roles).
    /// </summary>
    private bool TryBuildPermittedScopes(
      UserManagementDbContext db, OAuthProxyTargetEntity origin, string subject,
      out string scopeString, out string errorMessage
    ) {
      scopeString = null;
      errorMessage = null;

      List<string> allScopes = new List<string>();
      allScopes.Add($"Tenant:{origin.TenantUid}");

      TenantScopeEntity tenant = db.TenantScopes.Where((t) => t.TenantUid == origin.TenantUid).FirstOrDefault();
      if (tenant != null) {
        AddScopes(allScopes, tenant.PermittedScopes);
      }

      CachedUserIdentityEntity identity = db.CachedUserIdentities.Where(
        (c) => c.OriginUid == origin.Uid && c.OriginSpecificSubjectId == subject
      ).FirstOrDefault();

      if (identity == null) {
        errorMessage = "Unknown identity";
        return false;
      }
      if (identity.Disabled) {
        errorMessage = "Identity is disabled";
        return false;
      }
      AddScopes(allScopes, identity.PermittedScopes);

      RoleEntity[] roles = db.KnownUserLegitimations.Where(
        (r) => r.OriginUid == origin.Uid && r.OriginSpecificSubjectId == subject
      ).Select((l) => l.Role).ToArray();

      foreach (RoleEntity role in roles) {
        allScopes.Add("Role:" + role.RoleName);
        AddScopes(allScopes, role.PermittedScopes);
      }

      scopeString = string.Join(' ', allScopes.Distinct().OrderBy((s) => s));
      return true;
    }

    private static void AddScopes(List<string> target, string spaceSeparatedScopes) {
      if (string.IsNullOrWhiteSpace(spaceSeparatedScopes)) {
        return;
      }
      foreach (string s in spaceSeparatedScopes.Split(' ')) {
        if (!string.IsNullOrEmpty(s) && !target.Contains(s)) {
          target.Add(s);
        }
      }
    }

    /// <summary>
    /// Reduces the granted scopes to the requested ones (if any were requested).
    /// Requested scopes can never extend the granted ones.
    /// </summary>
    private static string NarrowScopes(string grantedScopes, string[] requestedScopes) {
      if (requestedScopes == null || requestedScopes.Length == 0) {
        return grantedScopes;
      }
      string[] granted = grantedScopes.Split(' ');
      string[] narrowed = granted.Where((s) => requestedScopes.Contains(s)).ToArray();
      return string.Join(' ', narrowed);
    }

    private static bool FixedTimeStringEquals(string left, string right) {
      if (left == null || right == null) {
        return false;
      }
      byte[] leftBytes = Encoding.UTF8.GetBytes(left);
      byte[] rightBytes = Encoding.UTF8.GetBytes(right);
      if (leftBytes.Length != rightBytes.Length) {
        return false;
      }
      return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    #endregion

    private bool TryValidateSessionId(string sessionId, out AuthFlowSession session) {
      lock (_AuthFlowSessions) {

        if (long.TryParse(sessionId, out long sid)) {

          if (Snowflake44.DecodeDateTime(sid).AddMinutes(_AuthFlowSessionLifetimeMinutes) > DateTime.UtcNow) {

            if (_AuthFlowSessions.TryGetValue(sid, out session)) {

              return true;
            }
          }
        }
      }

      session = null;
      return false;
    }

    #region " Sessions & Codes "

    private Dictionary<long, AuthFlowSession> _AuthFlowSessions = new Dictionary<long, AuthFlowSession>();

    [DebuggerDisplay("{OriginQualifiedSubjectIdentity}")]
    private class AuthFlowSession {

      public TokenIssuingResult TokenResultFromDelegate;

      public string LogonNameOrSubject { get; set; } = null;
      public string OriginProviderName { get; set; }
      public long OriginUid { get; set; }
      public string UserDisplayName { get; set; }
      public string UserEmailAddress { get; set; }
      public byte[] UserImage { get; set; }

      public string OriginQualifiedSubjectIdentity {
        get {
          return $"{LogonNameOrSubject}@{OriginProviderName}#{OriginUid}";
        }
      }

    }

    private string CreateSession(string loginOrSubject,long originUid, string originProviderName) {

      long newSessionId = Snowflake44.Generate();

      AuthFlowSession newSession = new AuthFlowSession {
        LogonNameOrSubject = loginOrSubject,
        OriginProviderName = originProviderName,
        OriginUid = originUid,
        //temp -> can be updated later:
        UserDisplayName = loginOrSubject,
        UserEmailAddress = "",
        UserImage = null
      };

      lock (_AuthFlowSessions) {
        _AuthFlowSessions[newSessionId] = newSession;
      }

      this.CleanupExpiredCodesAndSessions();

      return newSessionId.ToString();
    }

    private void CleanupExpiredCodesAndSessions() {

      lock (_AuthFlowSessions) {
        foreach (long sid in _AuthFlowSessions.Keys.ToArray()) {
          if (Snowflake44.DecodeDateTime(sid).AddMinutes(_AuthFlowSessionLifetimeMinutes) < DateTime.UtcNow) {
            _AuthFlowSessions.Remove(sid);
          }
        }
      }

      this.CleanupExpiredRetrievalCodes();

    }

    private void RemoveSession(string sessionId) {
      if (long.TryParse(sessionId, out long sid)) {
        lock (_AuthFlowSessions) {
          _AuthFlowSessions.Remove(sid);
        }
      }
    }

    private void UpdateSessionIdentityInfo(string sessionId, string displayName, string emailAddress) {
      if (long.TryParse(sessionId, out long sid)) {
        lock (_AuthFlowSessions) {
          if (_AuthFlowSessions.TryGetValue(sid, out AuthFlowSession session)) {
            session.UserDisplayName = displayName;
            session.UserEmailAddress = emailAddress;
          }
        }
      }
    }

    #endregion

    #region " IsLocalAPICLient "

    //private Dictionary<string, Boolean> _IsLocalAPICLientInfoCache = new Dictionary<string, bool>();

    //private bool IsClientOfLocalCredentialService(string oauthClientId) {
    //  bool isLocal = false;
    //  lock (_IsLocalAPICLientInfoCache) {
    //    if (_IsLocalAPICLientInfoCache.TryGetValue(oauthClientId, out isLocal)) {
    //      return isLocal;
    //    }
    //    using (UserManagementDbContext db = new UserManagementDbContext()) {

    //      OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.ClientId == oauthClientId).FirstOrDefault();
    //      if (target != null) {
    //        //we have a configuration for this
    //        if (target.AuthUrl == _OurProxyAuthUrl) {
    //          isLocal = true;
    //          _IsLocalAPICLientInfoCache[oauthClientId] = true;
    //          return true;
    //        }
    //      }

    //      _IsLocalAPICLientInfoCache[oauthClientId] = false;
    //      return false;

    //      //TODO: hier ggf die lokalen ApiOauthClients prüfen

    //    }
    //  }
    //}

    #endregion

  }

}
