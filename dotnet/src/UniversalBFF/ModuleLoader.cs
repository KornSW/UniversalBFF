using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UShell;
using ComponentDiscovery;
using System.Reflection;
using Logging.SmartStandards;

[assembly: AssemblyMetadata("SourceContext", "UniversalBFF-Core")]

namespace UniversalBFF {

  public class ModuleLoader {

    private ModuleRegistrar _Registrar;
    private Func<IFrontendModuleProvider[]> _FrontendModuleProviderRessolver = null;
    private Func<IBackendServiceProvider[]> _BackendServiceProviderRessolver = null;

    public ModuleRegistrar Registrar {  
      get { 
        return _Registrar;
      } 
    }

    /// <summary> </summary>
    /// <param name="registrar"></param>
    /// <param name="frontendModuleProviderRessolver">(keep null to use the internal auto-discovery)</param>
    /// <param name="backendServiceProviderRessolver">(keep null to use the internal auto-discovery)</param>
    public ModuleLoader(
      ModuleRegistrar registrar, 
      Func<IFrontendModuleProvider[]> frontendModuleProviderRessolver = null,
      Func<IBackendServiceProvider[]> backendServiceProviderRessolver = null
    ) {

      _Registrar = registrar;
      _FrontendModuleProviderRessolver = frontendModuleProviderRessolver;
      _BackendServiceProviderRessolver = backendServiceProviderRessolver;

      if (_FrontendModuleProviderRessolver == null) {
        _FrontendModuleProviderRessolver = () => {
          Type[] foundProvderTypes = BffApplication.Current.TypeIndexer.GetApplicableTypes<IFrontendModuleProvider>(true);
          IFrontendModuleProvider[] instances = foundProvderTypes.Select(
            (t) => (IFrontendModuleProvider)Activator.CreateInstance(t)
          ).ToArray();
          return instances;
        };
      }

      if (_BackendServiceProviderRessolver == null) {
        _BackendServiceProviderRessolver = () => {
          Type[] foundProvderTypes = BffApplication.Current.TypeIndexer.GetApplicableTypes<IBackendServiceProvider>(true);
          IBackendServiceProvider[] instances = foundProvderTypes.Select(
            (t) => (IBackendServiceProvider)Activator.CreateInstance(t)
          ).ToArray();
          return instances;
        };
      }

    }

    public void Load() {

      this.UnLoad();

      IFrontendModuleProvider[] frontendProivders = _FrontendModuleProviderRessolver.Invoke();
      foreach (IFrontendModuleProvider provider in frontendProivders) {
        DevLogger.LogInformation(2097061253866330689L,0, $"Using FRONTEND module provider '{provider.GetType().FullName}'...");
        try {
          provider.RegisterModule(_Registrar);
        }
        catch (Exception ex) {
          DevLogger.LogCritical(2097061253866330690L, 0, ex.Wrap($"Error while using FRONTEND module provider '{provider.GetType().FullName}': {ex.Message}"));
        }
      }

      IBackendServiceProvider[] backendProviders = _BackendServiceProviderRessolver.Invoke();
      foreach (IBackendServiceProvider provider in backendProviders) {
        DevLogger.LogInformation(2097061267357304270L, 0, $"Using BACKEND service provider '{provider.GetType().FullName}'...");
        try {
          provider.RegisterServices(_Registrar);
        }
        catch (Exception ex) {
          DevLogger.LogCritical(2097061267357304271L, 0, ex.Wrap($"Error while using BACKEND service provider '{provider.GetType().FullName}': {ex.Message}"));
        }
      }

    }

    public void UnLoad() {

    }

  }

}
