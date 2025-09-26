using System;
using System.Collections.Generic;
using UShell;

namespace UniversalBFF {

  public interface IApplicationScopeProvider {

    /// <summary>
    /// Registers one or more application scopes (dimensions) for the given product.
    /// </summary>
    /// <param name="productName">The TechnicalName of the product</param>
    /// <param name="metaAttributes">
    /// (aka 'Tags') mostly relevant for the user when choosing between multiple products (Portfolio-Selection),
    /// but potentially also relevant for declaring scope-switching constraints....
    /// </param>
    /// <param name="registrationCallback"></param>
    void RegisterScopes(
      string productName,
      Dictionary<string, string> metaAttributes,
      Action<ApplicationScopeDefinition> registrationCallback
    );

  }

}
