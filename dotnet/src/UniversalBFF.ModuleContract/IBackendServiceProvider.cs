
namespace UniversalBFF {

  public interface IBackendServiceProvider {

    void RegisterServices(IBackendServiceRegistrar registrar, BackendInitializationContext context);

    string ModuleKey { get; }
    string ModuleVersion { get; }

  }

  public class BackendInitializationContext {

    public delegate bool IsOtherModuleExisitingDelegate(string key, out string version);

    private IsOtherModuleExisitingDelegate _OnAskIsOtherModuleExisiting;

    public BackendInitializationContext(
      IsOtherModuleExisitingDelegate onAskIsOtherModuleExisiting, bool gatewayOnly
    ) {

      _OnAskIsOtherModuleExisiting = onAskIsOtherModuleExisiting;

       this.GatewayOnly = gatewayOnly;
   
    }

    public bool GatewayOnly { get; private set; } = false;

    public bool IsOtherModuleExisiting(string key, out string version) {
      return _OnAskIsOtherModuleExisiting(key, out version);
    }

  }

}
