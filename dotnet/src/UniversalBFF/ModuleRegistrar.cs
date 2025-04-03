using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.IO.Abstraction;
using System.Linq;
using System.SmartStandards;
using System.Text;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public abstract class ModuleRegistrar : IFrontendModuleRegistrar, IBackendServiceRegistrar, IPortfolioService {

    private PortfolioDescription _Portfolio = null;
    private List< ModuleDescription> _Modules = new List<ModuleDescription>();
    private string _BaseUrl;

    public ModuleRegistrar(string baseUrl) {
      _BaseUrl = baseUrl;
    }

    private void EnsurePortfolioIsInitialized() {
      if(_Portfolio == null) {

        _Portfolio = new PortfolioDescription();
        _Portfolio.ApplicationTitle = "UniversalBFF";

        // ##### AUTH ############################################################

        string authTokenSourceUid = Guid.NewGuid().ToString().ToLower();

        _Portfolio.AnonymousAccess = new AnonymousAccessDescription();
        _Portfolio.AnonymousAccess.AuthIndependentWorkspaces = new string[] {};
        _Portfolio.AnonymousAccess.AuthIndependentCommands = new string[] {};
        _Portfolio.AnonymousAccess.AuthIndependentUsecases = new string[] {};

        _Portfolio.AuthenticatedAccess = new AuthenticatedAccessDescription();
        _Portfolio.AuthenticatedAccess.PrimaryUiTokenSources = new string[] {
          authTokenSourceUid
        };

        _Portfolio.AuthTokenConfigs = new Dictionary<string, AuthTokenConfig>();
        _Portfolio.AuthTokenConfigs.Add(authTokenSourceUid, new AuthTokenConfig() {
          IssueMode = "OAUTH_CIBA_CODEGRAND",
          AuthEndpointRejectsIframe = false,
          AuthEndpointUrl = _BaseUrl + "oauth",
          RetrieveEndpointUrl = _BaseUrl + "oauth",
          ValidationEndpointUrl = _BaseUrl + "oauth"
        });

        // #######################################################################




      }
    }




    public void RegisterModule(ModuleDescription moduleDescription) {
      this.EnsurePortfolioIsInitialized();
      if (string.IsNullOrWhiteSpace(moduleDescription.ModuleUid)) {
        moduleDescription.ModuleUid = Snowflake44.Generate().ToString();
      }
      lock (_Modules) {
        _Modules.Add(moduleDescription);
      }
      var urls = _Portfolio.ModuleDescriptionUrls.ToList();
      urls.Add(moduleDescription.ModuleUid);
      _Portfolio.ModuleDescriptionUrls = urls.ToArray();
    }

    /// <summary>
    /// Registers an UShell Module Application
    /// </summary>
    public void RegisterFrontendExtension(IAfsRepository staticFilesForHosting) {
      this.EnsurePortfolioIsInitialized();

    }

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
          Label =_Portfolio.ApplicationTitle,
          PortfolioUrl = "default.portfolio.json"
        }
      };
    }

    PortfolioDescription IPortfolioService.GetPortfolioDescription(string nameInUrl) {
      this.EnsurePortfolioIsInitialized();
      return _Portfolio;
    }

    ModuleDescription IPortfolioService.GetModuleDescription(string nameInUrl) {
      lock (_Modules) {
        return _Modules.Where((m)=>m.ModuleUid == nameInUrl).FirstOrDefault();
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
