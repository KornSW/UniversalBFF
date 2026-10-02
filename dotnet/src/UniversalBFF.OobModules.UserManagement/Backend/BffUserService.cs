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

    //HACK: muss unbeingt weg!
    private static byte[] _LocalJwtSingKey = Encoding.ASCII.GetBytes("GEFHARDASMUSSWEG");
    private static int _LocalJwtTokenLifetimeMinutes = 240;
    private static string _LocalJwtIssuer = "UniversalBFF";
    private static string _LocalJwtAud = "UniversalBFF";

    private static LocalJwtIssuer _LocalIssuer = new LocalJwtIssuer(
      _LocalJwtSingKey, _LocalJwtTokenLifetimeMinutes, 
      passtroughAllRequestedClaims: true, enforcedIssuer: _LocalJwtIssuer
    );

    private static LocalJwtIntrospector _LocalIntrospector = new LocalJwtIntrospector(_LocalJwtSingKey);
    private LocalCredentialService _LocalCredentialService = new LocalCredentialService();

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

      using (UserManagementDbContext db = new UserManagementDbContext()) {

        OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.ClientId == apiClientId).FirstOrDefault();

        if(target == null) {
          message = $"Invalid apiClientId '{apiClientId}'";
          SecLogger.LogError(2079222383703567498L, 77305, "TryAuthenticate failed: " + message);
          sessionId = null;
          return false;
        }

        //bool isLocal = (target.AuthUrl == _OurProxyAuthUrl);
        bool isLocal = (target.ProviderClassName == typeof(LocalCredentialService).FullName);

        if (isLocal) {
          sessionId = this.CreateSession(login, target.Uid, target.ProviderClassName);
          return _LocalCredentialService.TryAuthenticate(login, password, out message);
        }
        else {
          sessionId = this.CreateSession(login, target.Uid, target.ProviderClassName);
        }

        //REMOTE AUTH REDIRECTION

        message = $"Redirection to 3rd.pt OAuth Prpovider '{target.DisplayLabel}' not Possible!";
        SecLogger.LogCritical(2079222383703567498L, 77305, "TryAuthenticate failed: " + message);
        sessionId = null;
        return false;

       // throw new NotImplementedException("TODO: hier reparieren");
       // //TODO: hier reparieren:
       ////Authtokenhandling// muss nächsten hop 
       //   //im state die original redirecturl + scopes einpacken und spärter /
       // ///wieder auspacken wenn das token da ist

       // return true;

      }

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

          _LocalIssuer.TryRequestAccessToken(
            new Dictionary<string, object> {
              { "iss", $"{_LocalJwtIssuer}" },
              { "aud", $"{_LocalJwtAud}" },
              { "jti", sessionId },
              { "sub", cachedIdentity.OriginSpecificSubjectId },
              { "scope", scopeString },
              { "ori", origin.Uid },
              { "wrp", session.TokenResultFromDelegate?.access_token }
            },
            out TokenIssuingResult atWrap
          );
          tokenResult.access_token = atWrap.access_token;

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

        return true;
      }
      else {
        tokenResult.error = "Invalid or expired logon-session";
        tokenResult.error_description = "Invalid or expired logon-session";
        return false;
      }

    }

    #endregion

    #region " CODE - FLOW "

    public bool TryValidateSessionIdAndCreateRetrievalCode(
      string apiClientId, string sessionId, string[] selectedScopes,
      out string code, out string message
    ) {

      bool success = this.TryValidateSessionIdAndCreateToken(
        apiClientId, sessionId, selectedScopes,
        out TokenIssuingResult tokenResult
      );

      if (success) {
        long retrievalCode = Snowflake44.Generate();

        lock (_TokensPerRetrievalCode) {
          //stage the token for retrieval
          _TokensPerRetrievalCode[retrievalCode] = tokenResult;
        }

        code = retrievalCode.ToString();
        message = null;
        return true;
      }
      else {
        code = null;
        message = tokenResult?.error;
        return false;
      }
    }

    public TokenIssuingResult RetrieveTokenByCode(string clientId, string clientSecret, string code) {
      TokenIssuingResult result = new TokenIssuingResult();

      if (!this.TryValidateApiClientSecret(clientId, clientSecret)) {
        result.error = "invalid_client";
        result.error_description = "Unknown client";
        return result;
      }

      lock (_TokensPerRetrievalCode) {

        if (long.TryParse(code, out long codeLong)) {

          //code is only valid for 1 minute
          if (Snowflake44.DecodeDateTime(codeLong).AddMinutes(1) > DateTime.UtcNow) {

            if (_TokensPerRetrievalCode.ContainsKey(codeLong)) {

              result = _TokensPerRetrievalCode[codeLong];

              //make sure the code can only be used once
              _TokensPerRetrievalCode.Remove(codeLong);

              return result;
            }

          }

        }

      }

      result.error = "invalid_code";
      result.error_description = "Invalid Code";
      return result;
    }

    #endregion

    #region " CLIENT CREDENTIAL - FLOW "

    public TokenIssuingResult ValidateClientAndCreateToken(
      string clientId, string clientSecret, string[] selectedScopes
    ) {

      TokenIssuingResult tokenResult = new TokenIssuingResult();

      if (!this.TryValidateApiClientSecret(clientId, clientSecret)) {
        tokenResult.error = "invalid_client";
        tokenResult.error_description = "Unknown client";
        return tokenResult;
      }

      //for security selectedScopes needs be be filtered again because some value could have been injected
      selectedScopes = this.GetAvailableScopes("API_" + clientId, selectedScopes).ToStringArray();

      //this is to keep the demo simple,
      //in a real world scenario not a good idea...
      string subject = "API_" + clientId;

      throw new NotImplementedException("TODO: hier reparieren");
      //TODO: hier reparieren:
      //bool success = _JwtIssuer.RequestAccessToken(
      //  nameof(DemoOAuthService), subject, "Everybody", selectedScopes, out tokenResult
      //);

      return tokenResult;
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

    public void IntrospectAccessToken(string rawToken, out bool isActive, out Dictionary<string, object> claims) {

      _LocalIntrospector.IntrospectAccessToken(rawToken, out isActive, out claims);

      if (claims == null) {
        claims = new Dictionary<string, object>();
      }




      //UserManagement-check ob user disabled ist

      //ablauf weiterleiten an ori!!!! -> passenden introspector provider nutzen oder an lolkalen diest weiter geben




      //if(claims.TryGetValue("ori", out object value)) {
      //  musslakal sein

      //}
      //if (claims.TryGetValue("wrp", out object value)) {
      //  muss da sein wenn nicht lokal

      //}




      //rawToken






      throw new NotImplementedException("TODO: hier reparieren");
      //TODO: hier reparieren:
      //_JwtIntropector.IntrospectAccessToken(rawToken, out isActive, out claims);

      //in addition to that we could check here, if the token was revoked!

    }

    #endregion

    public bool TryValidateApiClient(
      string apiClientId, string apiCallerHost, string redirectUri,
      out string message
    ) {
      message = string.Empty;

      if (long.TryParse(apiClientId, out long targetUid)) {
        using (UserManagementDbContext db = new UserManagementDbContext()) {

          //OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();
          OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.ClientId == apiClientId).FirstOrDefault();

          if (target != null) {
            return true;
            //if (target.AuthUrl != _OurProxyAuthUrl) {
            //}

          }   

        }

      }

      message = $"The client_id '{apiClientId}' is not valid in this context.";
      return false;

      //TODO: für die  apiClientIds in der normal liste muss dann aber die redirecturi immer die des bff sein!!!!!

      //sonderlösung für die vom locaauth - DataMisalignedException ists dann die tabelle!
      //redirectUri

    }

    public bool TryValidateApiClientSecret(
      string apiClientId, string apiClientSecret
    ) {

      if (long.TryParse(apiClientId, out long targetUid)) {
        using (UserManagementDbContext db = new UserManagementDbContext()) {

          //OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();
          OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.ClientId == apiClientId).FirstOrDefault();

          if (target != null) {
            return (apiClientSecret == target.ClientSecret);
          }
        }
      }

      return false;
    }

    private bool TryValidateSessionId(string sessionId, out AuthFlowSession session) {
      lock (_AuthFlowSessions) {

        if (long.TryParse(sessionId, out long sid)) {

          if (Snowflake44.DecodeDateTime(sid).AddMinutes(1) > DateTime.UtcNow) {

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

    private Dictionary<long, TokenIssuingResult> _TokensPerRetrievalCode = new Dictionary<long, TokenIssuingResult>();

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
          if (Snowflake44.DecodeDateTime(sid).AddMinutes(1) < DateTime.UtcNow) {
            _AuthFlowSessions.Remove(sid);
          }
        }
      }

      lock (_TokensPerRetrievalCode) {
        foreach (long code in _TokensPerRetrievalCode.Keys.ToArray()) {
          if (Snowflake44.DecodeDateTime(code).AddMinutes(1) < DateTime.UtcNow) {
            _TokensPerRetrievalCode.Remove(code);
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
