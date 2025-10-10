using System;
using System.Collections.Generic;
using System.Text;
using UShell;
using ComponentDiscovery;
using System.Reflection;

[assembly: AssemblyMetadata("SourceContext", "UniversalBFF-Core")]

namespace UniversalBFF {

  public class ModuleLoader {

    private ModuleRegistrar _Registrar;

    public ModuleRegistrar Registrar {  
      get { 
        return _Registrar;
      } 
    }

    public ModuleLoader(ModuleRegistrar registrar) {
       _Registrar = registrar;
    }

    public void Load() {
      this.UnLoad();

      Type[] foundProvderTypes = BffApplication.Current.TypeIndexer.GetApplicableTypes<IFrontendModuleProvider>(true);
      foreach (Type t in foundProvderTypes) {
        IFrontendModuleProvider provider = (IFrontendModuleProvider) Activator.CreateInstance(t);
        provider.RegisterModule(_Registrar);
      }

    }

    public void UnLoad() {

    }

  }

}
