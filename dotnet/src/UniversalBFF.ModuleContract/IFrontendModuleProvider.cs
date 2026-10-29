
namespace UniversalBFF {

  public interface IFrontendModuleProvider {

    void RegisterModule(IFrontendModuleRegistrar registrar, FrontendInitializationContext context);

    string ModuleKey { get; }
    string ModuleVersion { get; }

  }

  public class FrontendInitializationContext {

    public delegate bool IsOtherModuleExisitingDelegate(string key, out string version);

    private IsOtherModuleExisitingDelegate _OnAskIsOtherModuleExisiting;

    public FrontendInitializationContext(
      IsOtherModuleExisitingDelegate onAskIsOtherModuleExisiting
    ) {
      _OnAskIsOtherModuleExisiting = onAskIsOtherModuleExisiting;
    }

    public bool IsOtherModuleExisiting(string key, out string version) {
      return _OnAskIsOtherModuleExisiting(key, out version);
    }

  }

}
