using System;
using System.IO.Abstraction;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public interface IFrontendModuleRegistrar {

    void RegisterModule(ModuleDescription moduleDescription);

    /// <summary>
    /// Registers an UShell Module Application
    /// </summary>
    void RegisterFrontendExtension(IAfsRepository staticFilesForHosting);

  }

  public interface IBackendServiceRegistrar {

    /// <summary>
    /// Registers an Service-Endpoint (UJMW Dynamic-Controller) for the given Backend-Service
    /// </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="factory"></param>
    /// <returns></returns>
    void RegisterUjmwServiceEndpoint<TServiceContract>(Func<TServiceContract> factory);

    void RegisterUjmwProxy<TServiceContract>();

    void RegisterHttpProxy(string providedSubRoute, string forwardingAddress);

    void RegisterServerCommands(IServerCommandExecutor executor);

  }

}
