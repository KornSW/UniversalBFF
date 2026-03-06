using System;
using System.Reflection;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public interface IBackendServiceRegistrar {

    //TODO: brauchen wir hier noch etwas wie "RegisterSingleton" oder soll allem ambient gehen? / eingang in s ms-di-framework macht ggf sinn...

    /// <summary> </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="factory"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwServiceEndpoint<TServiceContract>(
      string moduleScopingKey, string endpointAlias, Func<TServiceContract> factory, int apiV = 1
    ) where TServiceContract : class;

    /// <summary> </summary>
    /// <param name="contractType"></param>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="factory"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwServiceEndpoint(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1
    );

    /// <summary> </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="externalHostedUrlGetter"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwProxy<TServiceContract>(
      string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1
    ) where TServiceContract : class;

    /// <summary> </summary>
    /// <param name="contractType"></param>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="externalHostedUrlGetter"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwProxy(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1
    );

    /// <summary> </summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="forwardingAddress"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterHttpProxy(
      string moduleScopingKey, string endpointAlias, string forwardingAddress, int apiV = 1
    );

    /// <summary> 
    /// Registers server commands for a specific module with the wellknown endpointAlias 'commands'
    /// </summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="executor"></param>
    void RegisterServerCommands(
      string moduleScopingKey, IServerCommandExecutor executor
    );

  }

}
