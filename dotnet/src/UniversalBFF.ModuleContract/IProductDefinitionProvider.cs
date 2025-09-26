
using System.Collections.Generic;

namespace UniversalBFF {

  public interface IProductDefinitionProvider {

    public ProductDefinition[] GetProductDefinitions();

  }

  public class ProductDefinition {

    /// <summary>
    /// A Display label for the Product
    /// (will be used as ApplicationName)
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// URL to a 128x128 PNG Image ('http(s):'- or 'data:...'-URL)
    /// </summary>
    public string LogoUrlLight { get; set; }

    /// <summary>
    /// URL to a 128x128 PNG Image ('http(s):'- or 'data:...'-URL)
    /// </summary>
    public string LogoUrlDark { get; set; } = null;

    /// <summary>
    /// No special characters, no spaces, just a..z A..Z 0..9 and '-'
    /// (will be used as name for Portfolio-Files, never visible to the user)
    /// </summary>
    public string TechnicalName { get; set; }

    /// <summary>
    /// (aka 'Tags') mostly relevant for he user when choosing between multiple products (Portfolio-Selection)
    /// </summary>
    public Dictionary<string, string> MetaAttributes { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// each field can contain either a http-url to an externally hosted ModuleDescription-JSON-File
    /// OR
    /// '{AnyModuleUid}' to reference a module that is already registered via ModuleProvider (BFF)
    /// </summary>
    public string[] EnabledModules { get; set; } = new string[] { };

    public string LandingWorkspaceName { get; set; }

  }


}
