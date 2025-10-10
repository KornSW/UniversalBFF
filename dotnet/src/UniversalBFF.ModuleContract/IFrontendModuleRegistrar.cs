using System;
//using System.IO.Abstraction;
using System.Reflection;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public interface IFrontendModuleRegistrar {

    void RegisterModule(ModuleDescription moduleDescription);

    /// <summary></summary>
    /// <param name="endpointAlias">should be url-safe! </param>
    /// <param name="assemblyWithEmbeddedFiles"></param>
    /// <param name="embeddedFilesNamespace">
    /// WARNING: Errors here are very hard to track, because simply no file is returned!
    ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
    ///  * Folder separators are dots (.)
    ///  * Case-Sensitive
    ///  * Replace from '-' to '_', ' ' to '_'
    /// </param>
    /// <param name="defaultDoc"></param> 
    /// <exception cref="InvalidOperationException"></exception>
    void RegisterFrontendExtension(string endpointAlias, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace, string defaultDoc = "index.html");

    void RegisterFrontendExtension(string endpointAlias, string externalHostedUrl);

    ///// <summary>
    ///// Registers an UShell Module Application
    ///// </summary>
    //void RegisterFrontendExtension(string endpointAlias, IAfsRepository staticFilesForHosting);

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
