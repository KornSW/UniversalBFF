
namespace UniversalBFF {

  public interface IFrontendModuleProvider {

    void RegisterModule(IFrontendModuleRegistrar registrar);

  }

  public interface IBackendServiceProvider {

    void RegisterServices(IBackendServiceRegistrar registrar);

  }

}
