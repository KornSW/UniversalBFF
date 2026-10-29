using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UShell;

namespace UniversalBFF {

  //TODO: muss auch mit einer urls zu einem portfolioindex zurecht kommen!

  //TODO: konfigurativ muss neben protfolio-GET-url auch eine UJMW verbindung mölich sein???

  public class FileBasedProductDefinitionProvider : IProductDefinitionProvider {

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private DirectoryInfo _PortfolioDefinitionFileDirectory;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private ProductDefinition[] _ProductDefinitions = null;

    public FileBasedProductDefinitionProvider(string portfolioDefinitionFileDirectory) {
      _PortfolioDefinitionFileDirectory = new DirectoryInfo(portfolioDefinitionFileDirectory);
    }

    public string PortfolioDefinitionFileDirectory {
      get {
        return _PortfolioDefinitionFileDirectory.FullName;
      }
    }

    public ProductDefinition[] GetProductDefinitions() {

      if(_ProductDefinitions != null) {
        return _ProductDefinitions;
      }

      List<ProductDefinition> result = new List<ProductDefinition>();
      FileInfo[] portfolioFiles = _PortfolioDefinitionFileDirectory.GetFiles("*.portfolio.json");

      foreach (FileInfo portfolioFile in portfolioFiles) {

        string portfolioFileJson = File.ReadAllText(portfolioFile.FullName,System.Text.Encoding.Default);
        PortfolioDescription descr = JsonConvert.DeserializeObject<PortfolioDescription>(portfolioFileJson);

        result.Add(new ProductDefinition {  
          Title = descr.ApplicationTitle,
          LogoUrlLight = descr.LogoUrlLight,
          LogoUrlDark = descr.LogoUrlDark,
          TechnicalName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(portfolioFile.Name)),
          EnabledModules = descr.ModuleDescriptionUrls, 
          MetaAttributes = new Dictionary<string, string>(),
          LandingWorkspaceName = descr.LandingWorkspaceName
        });
      }

      _ProductDefinitions = result.ToArray();
      return _ProductDefinitions;
    }

    //solves the problem, that this service does not decide about the concrete module-urls...
    //after all local module-provider have been loaded, this method is used to announce the module-urls,
    //so that we can replace the module-keys in the enabled-modules-list with the actual urls
    //(or append additinal renote urls). 
    public void AnnounceModuleUrl(string moduleKey, string url) {

      ProductDefinition[] definitions = this.GetProductDefinitions();

      foreach (ProductDefinition definition in definitions) {
        for (int i = 0; i < definition.EnabledModules.Length; i++) {
          if (definition.EnabledModules[i] == moduleKey) {
            definition.EnabledModules[i] = url;
          }
        }
      }

    }

  }

  /// <summary>
  /// Quick-Start version that hold a single Portfolio completely in-memory
  /// </summary>
  public class SinglePortfolioDefinitionProvider : IProductDefinitionProvider {

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private PortfolioDescription _Portfolio = null;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private bool _AutoAppendModuleUrls = true;

    public SinglePortfolioDefinitionProvider(string applicationTitle, params string[] moduleDescriptionUrls) {

      _Portfolio = new PortfolioDescription {
        ApplicationTitle = applicationTitle ,
        LogoUrlLight = "/assets/logo-light.png",
        LogoUrlDark = "/assets/logo-dark.png",
        LandingWorkspaceName = "default",
        ModuleDescriptionUrls = moduleDescriptionUrls,
      };

      if(_Portfolio.ModuleDescriptionUrls == null) {
        _Portfolio.ModuleDescriptionUrls = new string[] { };
      }

      if (_Portfolio.ModuleDescriptionUrls.Length == 0) {
        _AutoAppendModuleUrls = true;
      } 

    }

    public SinglePortfolioDefinitionProvider(PortfolioDescription descr) {
      _Portfolio = descr;
    }

    public PortfolioDescription Portfolio {
      get {
        return _Portfolio;
      }
    }

    public ProductDefinition[] GetProductDefinitions() {

      return new ProductDefinition[] {
        new ProductDefinition {
          Title = _Portfolio.ApplicationTitle,
          LogoUrlLight = _Portfolio.LogoUrlLight,
          LogoUrlDark = _Portfolio.LogoUrlDark,
          TechnicalName = _Portfolio.ApplicationTitle.ToLower().Replace(" ", "-"),
          EnabledModules = _Portfolio.ModuleDescriptionUrls,
          MetaAttributes = new Dictionary<string, string>(),
          LandingWorkspaceName = _Portfolio.LandingWorkspaceName
        }
      };

    }

    //solves the problem, that this service does not decide about the concrete module-urls...
    //after all local module-provider have been loaded, this method is used to announce the module-urls,
    //so that we can replace the module-keys in the enabled-modules-list with the actual urls
    //(or append additinal renote urls). 
    public void AnnounceModuleUrl(string moduleKey, string url) {

      bool found = false;

      for (int i = 0; i < _Portfolio.ModuleDescriptionUrls.Length; i++) {
        if (_Portfolio.ModuleDescriptionUrls[i] == moduleKey) {
          _Portfolio.ModuleDescriptionUrls[i] = url;
          found = true;
          break;
        }
      }

      if(!found && _AutoAppendModuleUrls) {
        List<string> newList = new List<string>(_Portfolio.ModuleDescriptionUrls);
        newList.Add(url);
        _Portfolio.ModuleDescriptionUrls = newList.ToArray();
      }
    }

  }

}
