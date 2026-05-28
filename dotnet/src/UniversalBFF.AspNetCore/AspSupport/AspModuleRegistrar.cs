using Microsoft.Extensions.DependencyInjection;
using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.SmartStandards;
using System.Text;
using System.Web.UJMW;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  internal class AspModuleRegistrar : ModuleRegistrar {

    /// <summary></summary>
    /// <param name="baseUrl">  usually just '/' (first and last char must be a slash!) </param>
    /// <param name="services"></param>
    /// <param name="securityProvider"></param>
    /// <param name="tenancyProvider"></param>
    /// <param name="productDefinitionProvider"></param>
    /// <param name="autoCreateChooserPortfolio"></param>
    public AspModuleRegistrar(
      string baseUrl,
      IServiceCollection services,
      IPortfolioSecurityProvider securityProvider,
      ITenancyProvider tenancyProvider,
      IProductDefinitionProvider productDefinitionProvider,
      bool autoCreateChooserPortfolio
    ) : base(baseUrl, securityProvider, tenancyProvider, productDefinitionProvider, autoCreateChooserPortfolio) {

      _Services = services;

    }

    private IServiceCollection _Services;


    public override void RegisterHttpProxy(
      string moduleScopingKey, string endpointAlias, string forwardingAddress, int apiV = 1
    ) {

      throw new NotImplementedException("RegisterHttpProxy is comming soon...");

    }

    public override void RegisterUjmwServiceEndpoint(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1
    ) {
      //HACK: muss irgendwie zusammengefasst werdenm, wegen dem einzel-overhead

      _Services.AddSingleton(contractType, (sp) => { return factory.Invoke(); });

      _Services.AddDynamicUjmwControllers((ujmw) => {

        ujmw.AddControllerFor(
          contractType,
          new DynamicUjmwControllerOptions {
            ControllerRoute = $"{moduleScopingKey}/api/v{apiV}/{endpointAlias}"
          }
        );

      });

    }

    /// <summary>
    ///   returns "{moduleScopingKey}/api/v{apiV}/{endpointAlias}"
    /// </summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="apiV"></param>
    /// <returns></returns>
    public static string BuildEndpointRoute(string moduleScopingKey, string endpointAlias, int apiV = 1) {
      return $"{moduleScopingKey}/api/v{apiV}/{endpointAlias}";

    }

  }

}
