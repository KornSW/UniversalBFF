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
     
          if (oauthConfig.AdditionalParamsJson != null && oauthConfig.AdditionalParamsJson.StartsWith("{")) {
            try {
              Dictionary<string,object> kvps = JsonConvert.DeserializeObject<Dictionary<string, object>>(oauthConfig.AdditionalParamsJson);
              foreach (KeyValuePair<string, object> kvp in kvps) {
                mappedConfig.Claims[kvp.Key] = kvp.Value?.ToString();
              }
            }
            catch (Exception ex) {
              DevLogger.LogError($"Error parsing AdditionalParamsJson for OAuthProxyTargetEntity {oauthConfig.Uid}: {ex.Message}");
            }
          }
          //mappedConfig.Claims["scope"] = $"Tenant:{oauthConfig.TenantUid}";

          //oauthConfig.AuthUrl
          //oauthConfig.ProviderClassName
          //mappedConfig.ClientId = oauthConfig.ClientId;
          //mappedConfig.ClientSecret = oauthConfig.ClientSecret;


          //ACHTUNG: das hier lenkt die UI auf unseren proxy-endpunkt, der dann die anfragen weiterleitet
          //hier ist also lediglich das mapping von metadaten nötig um dann pro request individuell routen zu können

          mappedConfig.IssueMode = "OAUTH_IMPLICIT_FLOW"; //at first, we only want to support this!
          mappedConfig.AuthEndpointUrl = _OurProxyAuthUrl;
          mappedConfig.RetrieveEndpointUrl = _OurProxyRetrivalUrl;



          //TODO: fehler odernicht????
          //mappedConfig.ClientId = oauthConfig.Uid.ToString();
          mappedConfig.ClientId = oauthConfig.ClientId.ToString();




          mappedConfig.ClientSecret = null; //not needed for implicit flow!
          mappedConfig.AuthEndpointRejectsIframe = !oauthConfig.IframeSupported; //our will redirect, so its relevant
          //mappedConfig.AdditionalAuthArgs = new Dictionary<string, string> {
          //  {"scope", "Tenant"}
          //};  

          mappedConfig.ValidationMode = "OAUTH_INTROSPECTION_ENDPOINT";
          mappedConfig.ValidationEndpointUrl = _OurProxyIntrospectionUrl;

          registerAuthTokenSourceCallback(
            Snowflake44.ConvertToGuid(oauthConfig.Uid).ToString(),
            mappedConfig,
            availableForPrimaryUiLogon
          );

        }

      }

    }

    public bool CanCurrentIdentityAccessProduct(string productName, Dictionary<string, string> metaAttributes) {
      //TODO: implement real logic here
      return true;
    }

    public bool CanCurrentIdentityAccessScope(string scopeName, string scopeValue) {
      //TODO: implement real logic here
      return true;
    }

  }

}
