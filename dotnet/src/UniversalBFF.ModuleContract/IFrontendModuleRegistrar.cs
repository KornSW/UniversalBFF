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
    void RegisterFrontendExtension(string endpointAlias, IAfsRepository staticFilesForHosting);

    void RegisterFrontendExtension(string endpointAlias, string externalHostedUrl);

  }

  public interface IBackendServiceRegistrar {

    void RegisterUjmwServiceEndpoint<TServiceContract>(string endpointAlias, Func<TServiceContract> factory) 
      where TServiceContract : class;

    void RegisterUjmwServiceEndpoint(Type contractType, string endpointAlias, Func<object> factory);

    void RegisterUjmwProxy<TServiceContract>(string endpointAlias, Func<string> externalHostedUrlGetter = null)
      where TServiceContract : class;

    void RegisterUjmwProxy(Type contractType, string endpointAlias, Func<string> externalHostedUrlGetter = null);

    void RegisterHttpProxy(string endpointAlias, string forwardingAddress);

    void RegisterServerCommands(IServerCommandExecutor executor);

  }

}
