using Microsoft.AspNetCore.Server.Kestrel.Core;
using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// APPLICATION-Base! -> usually just '/' (first and last char must be a slash!)
    /// </summary>
    private string _BaseUrl;
    
    private bool _AutoCreateChooserPortfolio;

    /// <summary></summary>
    /// <param name="baseUrl">  usually just '/' (first and last char must be a slash!) </param>
    /// <param name="securityProvider"></param>
    /// <param name="tenancyProvider"></param>
    /// <param name="productDefinitionProvider"></param>
    /// <param name="autoCreateChooserPortfolio"></param>
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

    #region " Frontend-Extensions "

    //must be collected and executed later, because were under inversion of control here!
    private List<Action<IStaticHostingRegistrar>> _ModuleFileRegistrationMethods = new List<Action<IStaticHostingRegistrar>>();

    /// <summary></summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="extensionAlias">An technical name (URL-SAFE!), used as alias to address this extension!</param>
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
    public void RegisterFrontendExtension(string moduleScopingKey, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace, string extensionAlias = "ui", string defaultDoc = "index.html") {
      string mountpointUrl = $"{HttpUtility.UrlEncode(moduleScopingKey)}/{HttpUtility.UrlEncode(extensionAlias)}/";
 
      lock (_ModuleFileRegistrationMethods) {
        _ModuleFileRegistrationMethods.Add(
          (r) => {
            r.Register(_BaseUrl, mountpointUrl, assemblyWithEmbeddedFiles, embeddedFilesNamespace);
            r.SetDefaultDoc(mountpointUrl, defaultDoc, true);
          }
        );
      }
    }

    public void RegisterFrontendExtension(string moduleScopingKey, string externalHostedUrl, string extensionAlias = "ui") {
      string mountpointUrl = $"{HttpUtility.UrlEncode(moduleScopingKey)}/{HttpUtility.UrlEncode(extensionAlias)}/";
      lock (_FrontendExtensionUrlsByAlias) {
        _FrontendExtensionUrlsByAlias[mountpointUrl] = externalHostedUrl;
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
      //TODO: das muss noch gemerget werdnen mit:
      var DUMMY = nameof(_FrontendExtensionUrlsByAlias);
    }

    public void RegisterServerCommands(string moduleScopingKey, IServerCommandExecutor executor) {

      //TODO: dringend implementieren:
      throw new NotImplementedException();


    }

    private Dictionary<string, string> _FrontendExtensionUrlsByAlias = new Dictionary<string, string>();

    public abstract void RegisterHttpProxy(string moduleScopingKey, string endpointAlias, string forwardingAddress, int apiV = 1);

    public void RegisterUjmwServiceEndpoint<TServiceContract>(string moduleScopingKey, string endpointAlias, Func<TServiceContract> factory, int apiV = 1) where TServiceContract : class {
      this.RegisterUjmwServiceEndpoint(typeof(TServiceContract), moduleScopingKey, endpointAlias, () => factory.Invoke(), apiV);
    }

    public abstract void RegisterUjmwServiceEndpoint(Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1);

    public void RegisterUjmwProxy<TServiceContract>(string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1) where TServiceContract : class {
      this.RegisterUjmwServiceEndpoint<TServiceContract>(
        moduleScopingKey, endpointAlias,
        () => {
          if(externalHostedUrlGetter == null) {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance<TServiceContract>();
          }
          else {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance<TServiceContract>(
              externalHostedUrlGetter, null
            );
          }
        },
        apiV
      );
    }

    public void RegisterUjmwProxy(Type contractType, string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1) {
      this.RegisterUjmwServiceEndpoint(
        contractType, moduleScopingKey, endpointAlias,
        () => {
          if (externalHostedUrlGetter == null) {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance(contractType);
          }
          else {
            return System.Web.UJMW.DynamicClientFactory.CreateInstance(
              contractType, externalHostedUrlGetter, null
            );
          }
        },
        apiV
      );
    }

    #endregion

  }

}
