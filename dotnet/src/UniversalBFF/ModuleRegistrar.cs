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

  public class ModuleRegistrar : IFrontendModuleRegistrar, IPortfolioService {

    private PortfolioDescription _Portfolio = null;
    private List< ModuleDescription> _Modules = new List<ModuleDescription>();
    private string _BaseUrl;

    public ModuleRegistrar(string baseUrl) {
      _BaseUrl = baseUrl;
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

    #endregion

  }

}
