//using Microsoft.AspNetCore.Server.Kestrel.Core;
using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.SmartStandards;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UShell;
using UShell.ServerCommands;
using static System.Formats.Asn1.AsnWriter;

namespace UniversalBFF {

  public abstract class BackendServiceRegistrar : IBackendServiceRegistrar {

    #region " Services (BackendExtension / UjmwServiceEndpoint / UjmwProxy)' "

    //BRUACHEN WIR DANN KONKRET!
    public abstract void RegisterHttpProxy(string moduleScopingKey, string endpointAlias, string forwardingAddress, int apiV = 1);
    public abstract void RegisterUjmwServiceEndpoint(Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1);

    ///// <summary>
    ///// Registers an Service-Endpoint (UJMW Dynamic-Controller) for the given Backend-Service
    ///// </summary>
    ///// <typeparam name="TServiceContract"></typeparam>
    ///// <param name="factory"></param>
    ///// <returns></returns>
    //public virtual void RegisterBackendExtension<TServiceContract>(Func<TServiceContract> factory) {


    //  this.RegisterUjmwServiceEndpoint

    //}

    public virtual void RegisterUjmwServiceEndpoint<TServiceContract>(string moduleScopingKey, string endpointAlias, Func<TServiceContract> factory, int apiV = 1) where TServiceContract : class {
      this.RegisterUjmwServiceEndpoint(typeof(TServiceContract), moduleScopingKey, endpointAlias, () => factory.Invoke(), apiV);
    }


    public virtual void RegisterUjmwProxy<TServiceContract>(string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1) where TServiceContract : class {
      this.RegisterUjmwServiceEndpoint<TServiceContract>(
        moduleScopingKey, endpointAlias,
        () => {
          if (externalHostedUrlGetter == null) {
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

    public virtual void RegisterUjmwProxy(Type contractType, string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1) {
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

    #region " CyclicTriggering "

    /// <summary>
    /// Registers a trigger target to be executed when a trigger is received.
    /// </summary>
    /// <param name="target">
    ///  A really (short running) method that will be invoked each time.
    ///  NOTE: the given method MUST NOT be long running, because the cyclic triggering
    ///  service will reschedule it again and again - AVOID any LONG RUNNING LOOP implementations
    ///  inside of that returned task - it's the job of the framework!
    /// </param>
    /// <param name="minWaitSeconds">
    ///  During this period any re-triggering will be ignored, 
    ///  even if the previous execution is already finished.
    ///  The framework recommends a best practive interval of 10-20s.
    /// </param>
    /// <param name="rescheduleWhileExecuting">
    ///  Controls the behavior if a trigger is received while the previous execution is still running.
    ///  If set to true, the trigger will be rescheduled to execute immediately after the current execution finishes. 
    ///  If set to false, any trigger received during an ongoing execution will be ignored and not rescheduled.
    /// </param>
    public virtual void RegisterCyclicTriggerTarget(
      Action<CancellationToken> target, int minWaitSeconds = 5, bool rescheduleWhileExecuting = true
    ) {
      throw new NotImplementedException("Comes woth ASP-LAYER");
    }

    /// <summary>
    /// Registers a trigger target to be executed when a trigger is received.
    /// </summary>
    /// <param name="taskFactory">
    ///  A really (short running) factory that creates the task to be executed cyclically.
    ///  NOTE: the returned task itself MUST NOT be long running, because the cyclic triggering
    ///  service will reschedule it again and again - AVOID any LONG RUNNING LOOP implementations
    ///  inside of that returned task - it's the job of the framework!
    /// </param>
    /// <param name="minWaitSeconds">
    ///  During this period any re-triggering will be ignored, 
    ///  even if the previous execution is already finished.
    ///  The framework recommends a best practive interval of 10-20s.
    /// </param>
    /// <param name="rescheduleWhileExecuting">
    ///  Controls the behavior if a trigger is received while the previous execution is still running.
    ///  If set to true, the trigger will be rescheduled to execute immediately after the current execution finishes. 
    ///  If set to false, any trigger received during an ongoing execution will be ignored and not rescheduled.
    /// </param>
    public virtual void RegisterCyclicTriggerTarget(
      Func<CancellationToken, Task> taskFactory, int minWaitSeconds = 5, bool rescheduleWhileExecuting = true
    ) {
      throw new NotImplementedException("Comes woth ASP-LAYER");
    }

    #endregion

    #region " ServerCommands "

    public virtual void RegisterServerCommands(string moduleScopingKey, IServerCommandExecutor executor) {
      throw new NotImplementedException("Comes woth ASP-LAYER");
    }

    #endregion

  }

  /// <summary>
  /// : IFrontendModuleRegistrar, :IBackendServiceRegistrar, :IPortfolioService
  /// </summary>
  public abstract partial class ModuleRegistrar : BackendServiceRegistrar, IFrontendModuleRegistrar, IPortfolioService {

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
      //this.EnsurePortfolioIsInitialized();
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
    public void RegisterFrontendExtension(string moduleScopingKey, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace, string extensionAlias = "ui", string defaultDoc = null) {
      string mountpointUrl = $"{HttpUtility.UrlEncode(moduleScopingKey)}/{HttpUtility.UrlEncode(extensionAlias)}/";
 
      lock (_ModuleFileRegistrationMethods) {
        _ModuleFileRegistrationMethods.Add(
          (r) => {
            r.Register(_BaseUrl, mountpointUrl, assemblyWithEmbeddedFiles, embeddedFilesNamespace);
            if(defaultDoc != null) {
              r.SetDefaultDoc(mountpointUrl, defaultDoc, true);
            }
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

    ///// <summary>
    ///// Registers an Service-Endpoint (UJMW Dynamic-Controller) for the given Backend-Service
    ///// </summary>
    ///// <typeparam name="TServiceContract"></typeparam>
    ///// <param name="factory"></param>
    ///// <returns></returns>
    //public override void RegisterBackendExtension<TServiceContract>(Func<TServiceContract> factory) {
    //  this.EnsurePortfolioIsInitialized();
    //  base.RegisterBackendExtension<TServiceContract>(factory);
    //}

    private Dictionary<string, string> _FrontendExtensionUrlsByAlias = new Dictionary<string, string>();


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

    /// <summary> </summary>
    /// <param name="portfolioName">Only the technical name (URL-SAFE!) of a portfolio. NOT a full name like 'foo.portfolio.json'.</param>
    /// <returns></returns>
    PortfolioDescription IPortfolioService.GetPortfolioDescription(string portfolioName) {
      this.EnsurePortfolioIsInitialized();

      if (!_PortfoliosPerName.ContainsKey(portfolioName)) {

        if (portfolioName == "default" && _PortfoliosPerName.Count == 1) {
          return _PortfoliosPerName.First().Value;
        }
        if (portfolioName != "default" && _PortfoliosPerName.ContainsKey("default")) {
          return _PortfoliosPerName["default"];
        }

        throw new Exception($"Portfolio '{portfolioName}' not found. Available portfolios: {string.Join(", ", _PortfoliosPerName.Keys)}");
      }

      return _PortfoliosPerName["default"]; //TODO: reicht nicht
    }


    /// <summary> </summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <returns></returns>
    ModuleDescription IPortfolioService.GetModuleDescription(string moduleScopingKey) {

      if (moduleScopingKey.Equals(DummyModuleName, StringComparison.OrdinalIgnoreCase)) {
        return GetDummyModule();
      }

      lock (_RegisteredModules) {
        ModuleDescription module = _RegisteredModules.Where(
          (m) => m.ModuleUid == moduleScopingKey || m.ModuleScopingKey == moduleScopingKey
        ).FirstOrDefault();
        return module;
      }

      //TODO: das muss noch gemerget werdnen mit:
      string DUMMY = nameof(_FrontendExtensionUrlsByAlias);

    }

    #endregion

  }

}
