using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Net;
using UShell;

namespace UniversalBFF {

  public interface IPortfolioSecurityProvider {

    //void GetAccessDescriptions(
    //  string productName,
    //  Dictionary<string, string> metaAttributes,
    //  out AuthenticatedAccessDescription authenticatedAccessDescription,
    //  out AnonymousAccessDescription anonymousAccessDescription,
    //  out AuthTokenConfig[] authTokenConfigs
    //);

    void RegisterAuthTokenSources(
      string productName,
      IEnumerable<KeyValuePair<string, string>> metaAttributes,
      RegisterAuthTokenSourcesCallbackDelegate registerAuthTokenSourceCallback
    );

    delegate void RegisterAuthTokenSourcesCallbackDelegate(
      string authTokenSourceUid, AuthTokenConfig cfg, bool availableForPrimaryUiLogon
    );

    /// <summary>
    /// will be evaluated contextual (using ambient user identity)
    /// </summary>
    /// <param name="productName">The TechnicalName of the product</param>
    /// <param name="metaAttributes">
    /// (aka 'Tags') mostly relevant for the user when choosing between multiple products (Portfolio-Selection),
    /// but potentially also relevant for declaring scope-switching constraints....
    /// </param>
    /// <returns></returns>
    bool CanCurrentIdentityAccessProduct(
      string productName,
      Dictionary<string, string> metaAttributes
    );

    /// <summary>
    /// will be evaluated contextual (using ambient user identity)
    /// </summary>
    /// <param name="scopeName">usually relevant for scopeName 'Tenant'</param>
    /// <param name="scopeValue"></param>
    /// <returns></returns>
    bool CanCurrentIdentityAccessScope(string scopeName, string scopeValue);

  }

}
