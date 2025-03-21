﻿using System;
using System.Collections.Generic;
using System.IO.Abstraction;
using System.Text;
using UShell;
using ComponentDiscovery;

namespace UniversalBFF {

  public class ModuleLoader {

    private ModuleRegistrar _Registrar;

    public ModuleRegistrar Registrar {  
      get { 
        return _Registrar;
      } 
    }

    public ModuleLoader(string baseUrl) {
      _Registrar = new ModuleRegistrar(baseUrl);
    }

    private static AssemblyIndexer _AssemblyIndexer = new AssemblyIndexer(
      enableResolvePathsBinding: true,
      enableAppDomainBinding: true
    );

    private static TypeIndexer _TypeIndexer = new TypeIndexer(_AssemblyIndexer);

    public void Load() {
      this.UnLoad();

      Type[] foundProvderTypes = _TypeIndexer.GetApplicableTypes<IModuleProvider>(true);
      foreach (Type t in foundProvderTypes) {
        IModuleProvider provider = (IModuleProvider) Activator.CreateInstance(t);
        provider.RegisterModule(_Registrar);
      }

    }

    public void UnLoad() {

    }

  }

}
