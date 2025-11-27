using Azure.Identity;
using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
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
      using (UserManagementDbContext db = new UserManagementDbContext()) {

        if (db.TenantScopes.Any()) {
          return;
        }

        TenantScopeEntity tenantScope = new TenantScopeEntity {
          TenantUid = 1111111111111111111L,
          AvailablePortfolios = "*",
          DisplayLabel = "default",
          PermittedScopes = ""
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
          PermittedScopes = "UserManagement",
        });

        OAuthProxyTargetEntity oauth = new OAuthProxyTargetEntity();

        //always differrent (for security-reasons)
        oauth.Uid = Snowflake44.Generate();
        oauth.ClientId = Snowflake44.Generate().ToString();
        oauth.ClientSecret = Snowflake44.Generate().ToString();

        oauth.TenantUid = tenantScope.TenantUid;
        oauth.AuthUrl = _OurProxyAuthUrl;
        oauth.DisplayLabel = "Logon (System-User)";
        oauth.RetrivalUrl = _OurProxyRetrivalUrl;

        db.OAuthProxyTargets.Add(oauth);

        string salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).Substring(0, 4).ToLower();
        long subjectId = Snowflake44.Generate();
        string first4DigitsOfSubjectId = subjectId.ToString().Substring(0, 4);
        LocalCredentialEntity localAdmin = new LocalCredentialEntity {
          SubjectId = subjectId,
          DisplayName = "Admin (" + salt + ")",
          PasswordHash = _LocalCredentialService.GetPasswordHash(first4DigitsOfSubjectId + salt),
          CreationDate = DateTime.Now,
          EmailAddress = "admin",
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

    public bool TryAuthenticate(
      string apiClientId, string login, string password, bool noPasswordNeeded, string clientProvidedState,
      out string sessionId, out string message
    ) {

      this.CheckIfInitialStateShouldInstaled();


      throw new NotImplementedException("TODO: hier reparieren");
      //TODO: hier reparieren:
      //Validieren //der redirect uri!!!!! (DivideByZeroException müssen wir hier noch liefern!!):

      if (noPasswordNeeded) {
        message = $"Passtrough-Auth is currently not supported!";
        SecLogger.LogError(2079222383703567499L, 73907, "TryAuthenticate failed: " + message);
        sessionId = null;
        return false;
      }


      sessionId = this.CreateSessionId(login);

      if (this.IsClientOfLocalCredentialService(apiClientId)) {
        return _LocalCredentialService.TryAuthenticate(login, password, out message);
      }








      //DER REST IST FALSCH - WIR LEITEN NICHT BEIM AUTH WEITER SODNERN SCHON BEIM LANDING!!!!!!!!
      ////////////////////////


      using (UserManagementDbContext db = new UserManagementDbContext()) {

        OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.ClientId == apiClientId).FirstOrDefault();

        if(target == null) {
          message = $"Invalid apiClientId '{apiClientId}'";
          SecLogger.LogError(2079222383703567498L, 73905, "TryAuthenticate failed: " + message);
          sessionId = null;
          return false;
        }
      



          //REMOTE AUTH REDIRECTION

          message = $"Redirection to 3rd.pt OAuth Prpovider '{target.DisplayLabel}' not Possible!";
          SecLogger.LogCritical(2079222383703567498L, 73905, "TryAuthenticate failed: " + message);
          sessionId = null;
          return false;




        throw new NotImplementedException("TODO: hier reparieren");
        //TODO: hier reparieren:
       //Authtokenhandling// muss nächsten hop 
          //im state die original redirecturl + scopes einpacken und spärter /
        ///wieder auspacken wenn das token da ist




        return true;

      }

    }

    public bool TryGetAvailableScopesBySessionId(
      string apiClientId, string sessionId, string[] prefferedScopes,
      out ScopeDescriptor[] availableScopes, out string message
    ) {

      if (TryValidateSessionId(sessionId, out string login)) {
        availableScopes = this.GetAvailableScopes(login, prefferedScopes);
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

      return new ScopeDescriptor[] {
      new ScopeDescriptor {
        Expression = "read", Label = "Read Data",
        Selected = true,//mandatory!
        ReadOnly= true, Invisible= false
      },
      new ScopeDescriptor {
        Expression = "write", Label = "Write Data",
        Selected = scopesToSelect.Contains("write"),
        ReadOnly= false, Invisible= false
      },
    };

    }

    #region " IMPLICIT - FLOW "



    public bool TryValidateSessionIdAndCreateToken(
      string apiClientId, string sessionId, string[] selectedScopes,
      out TokenIssuingResult tokenResult
    ) {




      tokenResult = new TokenIssuingResult();

      //KANN EIGENTLOCH NUR DEN LOOPBACK BETREFFEN - die proxy-targets schicken uns ja zur retrieval-url

      if (TryValidateSessionId(sessionId, out string login)) {

        //for security selectedScopes needs be be filtered again because some value could have been injected
        selectedScopes = this.GetAvailableScopes(login, selectedScopes).ToStringArray();

        //this is to keep the demo simple,
        //in a real world scenario not a good idea...
        string subject = login;




        throw new NotImplementedException("TODO: hier reparieren");
        //TODO: hier reparieren:

        //return _JwtIssuer.RequestAccessToken(
        //  nameof(DemoOAuthService), subject, "Everybody", selectedScopes, out tokenResult
        //);







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

          OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();
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
          OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.Uid == targetUid).FirstOrDefault();
          if (target != null) {
            return (apiClientSecret == target.ClientSecret);
          }
        }
      }

      return false;
    }

    private bool TryValidateSessionId(string sessionId, out string login) {
      lock (_LoginsPerSessionId) {

        if (long.TryParse(sessionId, out long sid)) {

          if (Snowflake44.DecodeDateTime(sid).AddMinutes(1) > DateTime.UtcNow) {

            if (_LoginsPerSessionId.TryGetValue(sid, out login)) {

              return true;
            }
          }
        }
      }

      login = null;
      return false;
    }

    #region " Sessions & Codes "

    private Dictionary<long, string> _LoginsPerSessionId = new Dictionary<long, string>();

    private Dictionary<long, TokenIssuingResult> _TokensPerRetrievalCode = new Dictionary<long, TokenIssuingResult>();

    private string CreateSessionId(string login) {

      long sid = Snowflake44.Generate();
      string newSessionId = sid.ToString();

      lock (_LoginsPerSessionId) {
        _LoginsPerSessionId[sid] = login;
      }

      this.CleanupExpiredCodesAndSessions();

      return newSessionId;
    }

    private void CleanupExpiredCodesAndSessions() {

      lock (_LoginsPerSessionId) {
        foreach (long sid in _LoginsPerSessionId.Keys.ToArray()) {
          if (Snowflake44.DecodeDateTime(sid).AddMinutes(1) < DateTime.UtcNow) {
            _LoginsPerSessionId.Remove(sid);
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

    private Dictionary<string, Boolean> _IsLocalAPICLientInfoCache = new Dictionary<string, bool>();

    private bool IsClientOfLocalCredentialService(string oauthClientId) {
      bool isLocal = false;
      lock (_IsLocalAPICLientInfoCache) {
        if (_IsLocalAPICLientInfoCache.TryGetValue(oauthClientId, out isLocal)) {
          return isLocal;
        }
        using (UserManagementDbContext db = new UserManagementDbContext()) {

          OAuthProxyTargetEntity target = db.OAuthProxyTargets.Where(o => o.ClientId == oauthClientId).FirstOrDefault();
          if (target != null) {
            //we have a configuration for this
            if (target.AuthUrl == _OurProxyAuthUrl) {
              isLocal = true;
              _IsLocalAPICLientInfoCache[oauthClientId] = true;
              return true;
            }
          }

          _IsLocalAPICLientInfoCache[oauthClientId] = false;
          return false;

          //TODO: hier ggf die lokalen ApiOauthClients prüfen

        }
      }
    }

    #endregion

  }

}
