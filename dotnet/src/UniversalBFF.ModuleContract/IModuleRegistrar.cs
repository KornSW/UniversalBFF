using System;
using System.IO.Abstraction;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public interface IModuleRegistrar {

    void RegisterModule(ModuleDescription moduleDescription);

    /// <summary>
    /// Registers an UShell Module Application
    /// </summary>
    void RegisterFrontendExtension(IAfsRepository staticFilesForHosting);

    /// <summary>
    /// Registers an Service-Endpoint (UJMW Dynamic-Controller) for the given Backend-Service
    /// </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="factory"></param>
    /// <returns></returns>
    void RegisterBackendExtension<TServiceContract>(Func<TServiceContract> factory);

    void RegisterServerCommands(IServerCommandExecutor executor);

  }

}
