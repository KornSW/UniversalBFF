using Microsoft.Extensions.Configuration;
using System;

namespace UniversalBFF {

  public class BffStartupArgs {

    private static string _BaseUrl = "/";
    private static string _PluginDir;

    private Func<IPortfolioSecurityProvider> _PortfolioSecurityProviderResolver = () => null; //dummy (provide NO instance)
    private Func<ITenancyProvider> _TenancyProviderResolver = () => null; //dummy (provide NO instance)
    private Func<IProductDefinitionProvider> _ProductDefinitionProviderResolver = () => null; //dummy (provide NO instance)
    private Func<IFrontendModuleProvider[]> _FrontendModuleProvidersResolver = () => null; //dummy (provide NO instance)
    private Func<IBackendServiceProvider[]> _BackendServiceProvidersResolver = () => null; //dummy (provide NO instance)

    internal static BffStartupArgs CreateDefault(IConfiguration config) {

      return new BffStartupArgs() {
        BaseUrl = config.GetValue<string>("BaseUrl"),
        PluginDir = config.GetValue<string>("PluginDir"),
        PortfolioSecurityProviderResolver = null, //<< null = auto-discover!
        TenancyProviderResolver = null,           //<< null = auto-discover!
        ProductDefinitionProviderResolver = null, //<< null = auto-discover!
        FrontendModuleProvidersResolver = null,   //<< null = auto-discover!
        BackendServiceProvidersResolver = null    //<< null = auto-discover!
      };

    }

    public string BaseUrl {
      get { return _BaseUrl; }
      set { _BaseUrl = value; }
    }

    public string PluginDir {
      get { return _PluginDir; }
      set { _PluginDir = value; }
    }

    public string HtmlPageTitle { get; set; } = "Universal BFF";

    /// <summary>
    /// The 'IPortfolioSecurityProvider' can decide, 
    /// which user is allowed to access which product, based on the current identity.
    /// (keep this resolver=null, if you want to use the internal auto-discovery)
    /// </summary>
    public Func<IPortfolioSecurityProvider> PortfolioSecurityProviderResolver {
      get { return _PortfolioSecurityProviderResolver; }
      set { _PortfolioSecurityProviderResolver = value; }
    }

    /// <summary>
    /// The 'ITenancyProvider' supplies mapping between products and tenants, 
    /// and which tenants are available at all.
    /// (keep this resolver=null, if you want to use the internal auto-discovery)
    /// </summary>
    public Func<ITenancyProvider> TenancyProviderResolver {
      get { return _TenancyProviderResolver; }
      set { _TenancyProviderResolver = value; }
    }

    /// <summary>
    /// The 'IProductDefinitionProvider' supplies definitions of products (metadata-layer, not yet portfolios).
    /// (keep this resolver=null, if you want to use the internal auto-discovery)
    /// </summary>
    public Func<IProductDefinitionProvider> ProductDefinitionProviderResolver {
      get { return _ProductDefinitionProviderResolver; }
      set { _ProductDefinitionProviderResolver = value; }
    }

    /// <summary>
    /// The set of available 'IFrontendModuleProvider's represents the aggration-source
    /// of all frontend-extensions (UI-Modules) that should be loaded when the BFF ist starting...
    /// (keep this resolver=null, if you want to use the internal auto-discovery)
    /// </summary>
    public Func<IFrontendModuleProvider[]> FrontendModuleProvidersResolver {
      get { return _FrontendModuleProvidersResolver; }
      set { _FrontendModuleProvidersResolver = value; }
    }

    /// <summary>
    /// The set of available 'IBackendServiceProvider's represents the aggration-source
    /// of all backend-services (API-Endpoints) that should be loaded when the BFF ist starting...
    /// (keep this resolver=null, if you want to use the internal auto-discovery)
    /// </summary>
    public Func<IBackendServiceProvider[]> BackendServiceProvidersResolver {
      get { return _BackendServiceProvidersResolver; }
      set { _BackendServiceProvidersResolver = value; }
    }

  }

}
