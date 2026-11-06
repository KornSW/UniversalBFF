using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
using Newtonsoft.Json;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.SmartStandards;
using System.Text;
using UniversalBFF.OobModules.UserManagement.Frontend.Contract;
using UShell;
using static UniversalBFF.IPortfolioSecurityProvider;

namespace UniversalBFF.OobModules.UserManagement {

  [SupportsInstanceDiscovery]
  public partial class BffUserService : IPortfolioSecurityProvider {

    #region " Singleton (discoverable) "

    private static BffUserService _Singleton = null;

    [ProvidesDiscoverableInstance]
    internal static IPortfolioSecurityProvider GetInstance() {
      if(_Singleton == null) {
        _Singleton = new BffUserService();
      }
      return _Singleton;
    }

    #endregion 
    
    private string _OurProxyAuthUrl = "/oauth2/authorize";
    private string _OurProxyRetrivalUrl = "/oauth2/token";
    private string _OurProxyIntrospectionUrl = "/oauth2/introspect";

    public void RegisterAuthTokenSources(
      string productName,
      IEnumerable<KeyValuePair<string, string>> metaAttributes,
      RegisterAuthTokenSourcesCallbackDelegate registerAuthTokenSourceCallback
    ) {

      this.CheckIfInitialStateShouldInstaled();

      using (UserManagementDbContext db = new UserManagementDbContext()) {

        long[] tenantIds = db.TenantScopes.Where(
          (t) => t.AvailablePortfolios == "*" || (";" + t.AvailablePortfolios + ";").Contains(";" + productName + ";")
          ).Select(
          (t)=>t.TenantUid
        ).ToArray();

        OAuthProxyTargetEntity[] oauthConfigs = db.OAuthProxyTargets.Where((pt) => tenantIds.Contains(pt.TenantUid)).ToArray();

        List<AuthTokenConfig> mappedAuthTokenConfigs = new List<AuthTokenConfig>();
        foreach (OAuthProxyTargetEntity oauthConfig in oauthConfigs) {
          AuthTokenConfig mappedConfig = new AuthTokenConfig();

          bool availableForPrimaryUiLogon = !(
            string.IsNullOrWhiteSpace(oauthConfig.DisplayLabel) ||
            oauthConfig.DisplayLabel.StartsWith("_")
          );

          mappedConfig.DisplayIconUrl = oauthConfig.DisplayIconUrl;
          mappedConfig.DisplayLabel = oauthConfig.DisplayLabel;

          if(mappedConfig.Claims == null) {
            mappedConfig.Claims = new Dictionary<string, string>();
          }

          //NOTE: 'AdditionalParamsJson' contains the upstream authorize parameters (proxy mode),
          //which are applied server-side only (see InitializeOAuthOperationsProvider) and must not
          //be exposed as portfolio claims. BFF scopes are derived from the database.

          //ACHTUNG: das hier lenkt die UI auf unseren proxy-endpunkt, der dann die anfragen weiterleitet
          //hier ist also lediglich das mapping von metadaten nötig um dann pro request individuell routen zu können

          mappedConfig.IssueMode = "OAUTH_IMPLICIT_FLOW"; //at first, we only want to support this!
          mappedConfig.AuthEndpointUrl = _OurProxyAuthUrl;
          mappedConfig.RetrieveEndpointUrl = _OurProxyRetrivalUrl;

          //the PUBLIC client id is the Uid of the target - the inner (upstream) ClientId/ClientSecret
          //never leave the server
          mappedConfig.ClientId = oauthConfig.Uid.ToString();

          mappedConfig.ClientSecret = null; //not needed for implicit flow!
          mappedConfig.AuthEndpointRejectsIframe = !oauthConfig.IframeSupported; //our will redirect, so its relevant
          //mappedConfig.AdditionalAuthArgs = new Dictionary<string, string> {
          //  {"scope", "Tenant"}
          //};  

          mappedConfig.ValidationMode = "OAUTH_INTROSPECTION_ENDPOINT";
          mappedConfig.ValidationEndpointUrl = _OurProxyIntrospectionUrl;

          string tokenSourceId = Snowflake44.ConvertToGuid(oauthConfig.Uid).ToString();
          this.RegisterKnownTokenSourceId(tokenSourceId);
          if (availableForPrimaryUiLogon) {
            UserManagementModuleProvider.AnnouncePrimaryUiTokenSource(tokenSourceId);
          }

          registerAuthTokenSourceCallback(
            tokenSourceId,
            mappedConfig,
            availableForPrimaryUiLogon
          );

        }

      }

    }

    /// <summary>
    /// Evaluates (using the ambient identity from the AccessTokenManager), whether the current identity
    /// is legitimated for at least one tenant which has the given product in its 'AvailablePortfolios'.
    /// </summary>
    public bool CanCurrentIdentityAccessProduct(string productName, Dictionary<string, string> metaAttributes) {
      if (!this.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes)) {
        return false;
      }
      long[] tenantUids = GetTenantUidsFromScopes(scopes);
      if (tenantUids.Length == 0) {
        return false;
      }
      using (UserManagementDbContext db = new UserManagementDbContext()) {
        string[] availablePortfolios = db.TenantScopes.Where(
          (t) => tenantUids.Contains(t.TenantUid)
        ).Select(
          (t) => t.AvailablePortfolios
        ).ToArray();
        return CanAccessProduct(availablePortfolios, productName);
      }
    }

    /// <summary>
    /// Evaluates (using the ambient identity from the AccessTokenManager), whether the scopes of the
    /// current token contain the given scope ('scopeName:scopeValue').
    /// </summary>
    public bool CanCurrentIdentityAccessScope(string scopeName, string scopeValue) {
      if (!this.TryGetCurrentIdentity(out string tokenSourceId, out string subject, out string[] scopes)) {
        return false;
      }
      return ContainsScope(scopes, scopeName, scopeValue);
    }

  }

}
