using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public interface IBackendServiceRegistrar {

    //TODO: brauchen wir hier noch etwas wie "RegisterSingleton" oder soll allem ambient gehen? / eingang in s ms-di-framework macht ggf sinn...

    /// <summary> </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint (slashes are allowed)!</param>
    /// <param name="factory"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwServiceEndpoint<TServiceContract>(
      string moduleScopingKey, string endpointAlias, Func<TServiceContract> factory, int apiV = 1
    ) where TServiceContract : class;

    /// <summary> </summary>
    /// <param name="contractType"></param>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint (slashes are allowed)!</param>
    /// <param name="factory"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwServiceEndpoint(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1
    );

    /// <summary> </summary>
    /// <typeparam name="TServiceContract"></typeparam>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint (slashes are allowed)!</param>
    /// <param name="externalHostedUrlGetter"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwProxy<TServiceContract>(
      string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1
    ) where TServiceContract : class;

    /// <summary> </summary>
    /// <param name="contractType"></param>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint (slashes are allowed)!</param>
    /// <param name="externalHostedUrlGetter"></param>
    /// <param name="apiV">The exposed api-version, under which this endpoint is registered for!</param>
    void RegisterUjmwProxy(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<string> externalHostedUrlGetter = null, int apiV = 1
    );

    /// <summary> </summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint (slashes are allowed)!</param>
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

    #region " CyclicTriggering "

    /// <summary>
    /// Registers a trigger target to be executed when a trigger is received.
    /// </summary>
    /// <param name="target">
    ///  A really (short running) method that will be invoked each time.
    ///  NOTE: the given method MUST NOT be long running, because the cyclic triggering
    ///  service will reschedule it again and again - AVOID any LONG RUNNING LOOP implementations
    ///  inside of that returned task - it's the job of the framework!
    /// </param>
    /// <param name="minWaitSeconds">
    ///  During this period any re-triggering will be ignored, 
    ///  even if the previous execution is already finished.
    ///  The framework recommends a best practive interval of 10-20s.
    /// </param>
    /// <param name="rescheduleWhileExecuting">
    ///  Controls the behavior if a trigger is received while the previous execution is still running.
    ///  If set to true, the trigger will be rescheduled to execute immediately after the current execution finishes. 
    ///  If set to false, any trigger received during an ongoing execution will be ignored and not rescheduled.
    /// </param>
    void RegisterCyclicTriggerTarget(
      Action<CancellationToken> target, int minWaitSeconds = 5, bool rescheduleWhileExecuting = true
    );

    /// <summary>
    /// Registers a trigger target to be executed when a trigger is received.
    /// </summary>
    /// <param name="taskFactory">
    ///  A really (short running) factory that creates the task to be executed cyclically.
    ///  NOTE: the returned task itself MUST NOT be long running, because the cyclic triggering
    ///  service will reschedule it again and again - AVOID any LONG RUNNING LOOP implementations
    ///  inside of that returned task - it's the job of the framework!
    /// </param>
    /// <param name="minWaitSeconds">
    ///  During this period any re-triggering will be ignored, 
    ///  even if the previous execution is already finished.
    ///  The framework recommends a best practive interval of 10-20s.
    /// </param>
    /// <param name="rescheduleWhileExecting">
    ///  Controls the behavior if a trigger is received while the previous execution is still running.
    ///  If set to true, the trigger will be rescheduled to execute immediately after the current execution finishes. 
    ///  If set to false, any trigger received during an ongoing execution will be ignored and not rescheduled.
    /// </param>
    void RegisterCyclicTriggerTarget(
      Func<CancellationToken, Task> taskFactory, int minWaitSeconds = 5, bool rescheduleWhileExecting = true
    );

    #endregion

  }

}
