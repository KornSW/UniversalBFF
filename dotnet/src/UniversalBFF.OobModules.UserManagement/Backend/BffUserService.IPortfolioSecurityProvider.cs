using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    
    private string _OurProxyAuthUrl = null;
    private string _OurProxyRetrivalUrl = null;
    private string _OurProxyIntrospectionUrl = null;

    public  void RegisterAuthTokenSources(
      string productName,
      IEnumerable<KeyValuePair<string, string>> metaAttributes,
      RegisterAuthTokenSourcesCallbackDelegate registerAuthTokenSourceCallback
    ) { 

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

          //TODO: kommt erst mit der n version von authtokenhandling!
          //mappedConfig.DisplayIconUrl = oauthConfig.DisplayIconUrl;
          //mappedConfig.DisplayLabel = oauthConfig.DisplayLabel;

          //ACHTUNG: das hier lenkt die UI auf unseren proxy-endpunkt, der dann die anfragen weiterleitet
          //hier ist also lediglich das mapping von metadaten nötig um dann pro request individuell routen zu können

          mappedConfig.IssueMode = "OAUTH_IMPLICIT_FLOW"; //at first, we only want to support this!
          mappedConfig.AuthEndpointUrl = _OurProxyAuthUrl;
          mappedConfig.RetrieveEndpointUrl = _OurProxyRetrivalUrl;
          mappedConfig.ClientId = oauthConfig.Uid.ToString();
          mappedConfig.ClientSecret = null; //not needed for implicit flow!
          mappedConfig.AuthEndpointRejectsIframe = oauthConfig.IframeSupported; //our will redirect, so its relevant
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
