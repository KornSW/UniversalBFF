using System;
using System.Collections.Generic;
using System.IO.Abstraction;
using System.Text;
using UShell;

namespace UniversalBFF {

  public interface IModuleRegistrar {

    public void RegisterModule(ModuleDescription moduleDescription) {


    }

    /// <summary>
    /// Registers an UShell Module Application
    /// </summary>
    public void RegisterFrontendExtension(IAfsRepository staticFilesForHosting) {



    }

    /// <summary>
    /// Registers an Service-Endpoint (UJMW Dynamic-Controller) for the given Backend-Service
    /// </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="factory"></param>
    /// <returns></returns>
    public void RegisterBackendExtension<TServiceContract>(Func<TServiceContract> factory) {



    }

  }

}
