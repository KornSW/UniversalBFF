using Microsoft.AspNetCore.Server.Kestrel.Core;
using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.IO.Abstraction;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.SmartStandards;
using System.Text;
using System.Web;
using UShell;
using UShell.ServerCommands;
using static System.Formats.Asn1.AsnWriter;

namespace UniversalBFF {

  public abstract partial class ModuleRegistrar : IFrontendModuleRegistrar, IBackendServiceRegistrar, IPortfolioService {

    private IPortfolioSecurityProvider _SecurityProvider;
    private ITenancyProvider _TenancyProvider;    //not related to securtiy!!!
    private IProductDefinitionProvider _ProductDefinitionProvider;


    //TODO: IProductDefinitionProvider auch als IApplicationScopeProvider adden, wenn er einer ist!
    //TODO: ITenancyProvider auch als IApplicationScopeProvider  adden, wenn er einer ist!
    //TODO: IPortfolioSecurityProvider auch als IApplicationScopeProvider  adden, wenn er einer ist!
    private IApplicationScopeProvider[] _ApplicationScopeProviders;


    private Dictionary<string, PortfolioDescription> _PortfoliosPerName = null;
    private List<PortfolioEntry> _PortfolioEntries = new List<PortfolioEntry>();

    private List<ModuleDescription> _RegisteredModules = new List<ModuleDescription>();
    private string _BaseUrl;
    
    private bool _AutoCreateChooserPortfolio;

    public ModuleRegistrar(
      string baseUrl,
      IPortfolioSecurityProvider securityProvider,
      ITenancyProvider tenancyProvider,
      IProductDefinitionProvider productDefinitionProvider,
      bool autoCreateChooserPortfolio
    ) {

      _BaseUrl = baseUrl;
      _SecurityProvider = securityProvider;
      _TenancyProvider = tenancyProvider;
      _ProductDefinitionProvider = productDefinitionProvider;
      _AutoCreateChooserPortfolio = autoCreateChooserPortfolio;
      //_ApplicationScopeProviders         via compoenntdiscovery


      //umgekehrt gibt es für security einfach rivate fallback-provider!

      //TODO: initialisierung durch Knowninterfaces an den providern!!!
      //if appicationscopeoprovider is ITenancyProvider
      //vllt auch - if appicationscopeoprovider is securtyrovider

    }

    public void RegisterModule(ModuleDescription moduleDescription) {
      this.EnsurePortfolioIsInitialized();
      if (string.IsNullOrWhiteSpace(moduleDescription.ModuleUid)) {
        moduleDescription.ModuleUid = Snowflake44.Generate().ToString();
      }
      lock (_RegisteredModules) {
        _RegisteredModules.Add(moduleDescription);
      }
      //var urls = newPortfolio.ModuleDescriptionUrls.ToList();
      //urls.Add(moduleDescription.ModuleUid);
      //newPortfolio.ModuleDescriptionUrls = urls.ToArray();
    }

    /// <summary>
    /// Registers an UShell Module Application
    /// </summary>
    public void RegisterFrontendExtension(IAfsRepository staticFilesForHosting) {
      this.EnsurePortfolioIsInitialized();

      throw new NotImplementedException();

    }

    #region " Frontend-Extensions "

    //must be collected and executed later, because were under inversion of control here!
    private List<Action<IStaticHostingRegistrar>> _ModuleFileRegistrationMethods = new List<Action<IStaticHostingRegistrar>>();

    /// <summary></summary>
    /// <param name="endpointAlias">MUST BE URL-COMPATIBLE!</param>
    /// <param name="assemblyWithEmbeddedFiles"></param>
    /// <param name="embeddedFilesNamespace">
    /// WARNING: Errors here are very hard to track, because simply no file is returned!
    ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
    ///  * Folder separators are dots (.)
    ///  * Case-Sensitive
    ///  * Replace from '-' to '_', ' ' to '_'
    /// </param>
    /// <param name="defaultDoc"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RegisterFrontendExtension(string endpointAlias, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace, string defaultDoc = "index.html") {
      string aliasUrl = $"ui/{HttpUtility.UrlEncode(endpointAlias)}/";
      lock (_ModuleFileRegistrationMethods) {
        _ModuleFileRegistrationMethods.Add(
          (r) => {
            r.Register(aliasUrl, assemblyWithEmbeddedFiles, embeddedFilesNamespace);
            r.SetDefaultDoc(aliasUrl, defaultDoc, true);
          }
        );
      }
    }

    public void CollectAndRegisterFrontendExtensionsTo(IStaticHostingRegistrar registrar) {
      //now is the right time to execute the registrations
      lock (_ModuleFileRegistrationMethods) {
        foreach(Action<IStaticHostingRegistrar> moduleFileRegistrationMethod in _ModuleFileRegistrationMethods) {
          moduleFileRegistrationMethod(registrar);
        }
      }
    }

    #endregion

    /// <summary>
    /// Registers an Service-Endpoint (UJMW Dynamic-Controller) for the given Backend-Service
    /// </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="factory"></param>
    /// <returns></returns>
    public void RegisterBackendExtension<TServiceContract>(Func<TServiceContract> factory) {
      this.EnsurePortfolioIsInitialized();


    }

    #region " IPortfolioService "

    PortfolioEntry[] IPortfolioService.GetPortfolioIndex() {
      this.EnsurePortfolioIsInitialized();

      return new PortfolioEntry[] {
        new PortfolioEntry() {
          Label = "Default",//TODO: reicht nicht
          PortfolioUrl = "default.portfolio.json"
        }
      };

    }

    PortfolioDescription IPortfolioService.GetPortfolioDescription(string nameInUrl) {
      this.EnsurePortfolioIsInitialized();
      return _PortfoliosPerName["default"]; //TODO: reicht nicht
    }

    ModuleDescription IPortfolioService.GetModuleDescription(string nameInUrl) {
      lock (_RegisteredModules) {
        return _RegisteredModules.Where((m)=>m.ModuleUid == nameInUrl).FirstOrDefault();
      }
    }

    public void RegisterServerCommands(IServerCommandExecutor executor) {




    }

    private Dictionary<string, string> _FrontendExtensionUrlsByAlias = new Dictionary<string, string>();


    public virtual void RegisterFrontendExtension(string endpointAlias, IAfsRepository staticFilesForHosting) {
      string relativeApplicationRoute = $"/ui/{endpointAlias}/";
      lock (_FrontendExtensionUrlsByAlias) {
        _FrontendExtensionUrlsByAlias[endpointAlias] = relativeApplicationRoute;
      }
    }

    public void RegisterFrontendExtension(string endpointAlias, string externalHostedUrl) {
      lock (_FrontendExtensionUrlsByAlias) {
        _FrontendExtensionUrlsByAlias[endpointAlias] = externalHostedUrl;
      }
    }

    public abstract void RegisterHttpProxy(string endpointAlias, string forwardingAddress);

    public void RegisterUjmwServiceEndpoint<TServiceContract>(string endpointAlias, Func<TServiceContract> factory) where TServiceContract : class {
      this.RegisterUjmwServiceEndpoint(typeof(TServiceContract), endpointAlias, () => factory.Invoke());
    }

    public abstract void RegisterUjmwServiceEndpoint(Type contractType, string endpointAlias, Func<object> factory);

    public void RegisterUjmwProxy<TServiceContract>(string endpointAlias, Func<string> externalHostedUrlGetter = null) where TServiceContract : class {
      this.RegisterUjmwServiceEndpoint<TServiceContract>(
        endpointAlias,
        () => {
          if(externalHostedUrlGetter == null) {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance<TServiceContract>();
          }
          else {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance<TServiceContract>(
              externalHostedUrlGetter, null
            );
          }
        }
      );
    }

    public void RegisterUjmwProxy(Type contractType, string endpointAlias, Func<string> externalHostedUrlGetter = null) {
      this.RegisterUjmwServiceEndpoint(
        contractType, endpointAlias,
        () => {
          if (externalHostedUrlGetter == null) {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance(contractType);
          }
          else {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance(
              contractType, externalHostedUrlGetter, null
            );
          }
        }
      );
    }

    #endregion

  }

}
