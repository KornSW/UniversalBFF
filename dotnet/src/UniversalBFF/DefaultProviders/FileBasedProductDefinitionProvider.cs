using Newtonsoft.Json;
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

  }

}
