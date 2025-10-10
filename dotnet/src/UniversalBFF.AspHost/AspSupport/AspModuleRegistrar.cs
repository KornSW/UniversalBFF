using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.SmartStandards;
using System.Text;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  internal class AspModuleRegistrar : ModuleRegistrar {

    /// <summary></summary>
    /// <param name="baseUrl">  usually just '/' (first and last char must be a slash!) </param>
    /// <param name="securityProvider"></param>
    /// <param name="tenancyProvider"></param>
    /// <param name="productDefinitionProvider"></param>
    /// <param name="autoCreateChooserPortfolio"></param>
    public AspModuleRegistrar(string baseUrl,
      IPortfolioSecurityProvider securityProvider,
      ITenancyProvider tenancyProvider,
      IProductDefinitionProvider productDefinitionProvider,
      bool autoCreateChooserPortfolio
    ) : base(baseUrl, securityProvider, tenancyProvider, productDefinitionProvider, autoCreateChooserPortfolio) {
    }

    public override void RegisterHttpProxy(string endpointAlias, string forwardingAddress) {

      throw new NotImplementedException("RegisterHttpProxy is comming soon...");

    }

    public override void RegisterUjmwServiceEndpoint(Type contractType, string endpointAlias, Func<object> factory) {

      throw new NotImplementedException("Hier fehlt noch dass der service als UJWM dynamic facade eingehangen wird");

    }

  }

}
