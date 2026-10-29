using CyclicTriggering;
using Microsoft.Extensions.DependencyInjection;
using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.SmartStandards;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UJMW;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF.AspSupport {

  public sealed class AspBackendServiceRegistrar : BackendServiceRegistrar {

    /// <summary>
    /// Convenience for AspNetCore-WebApi Applications that are NOT ment to be a full
    /// BFF, but sill want to bootstrap a composite set of backend services/endpoints
    /// delivered by ModuleProviders (NOTE: this will only use the 'IBackendServiceProvider'-
    /// part of the given ModuleProviders - not any frontend-related parts)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="providers"></param>
    /// <param name="gatewayOnly">
    ///   just a hint which will passed to the providers, in order to tell them,
    ///   that the current application is ment to be just a api-gateway. this should
    ///   lead to a different registration-behaviour like rigistering proxies instead of real
    ///   service implementation (BUT THiS IN NOT GUARANTEED - IT STILL RELIES ON THE CONCRETE PROVIDER)
    /// </param>
    public static void InitializeFromProviders(
      IServiceCollection services, 
      IBackendServiceProvider[] providers,
      bool gatewayOnly = false
    ) {

      AspBackendServiceRegistrar registrar = new AspBackendServiceRegistrar(services, gatewayOnly);

      BackendInitializationContext backendInitializationContext = new BackendInitializationContext(
        (string moduleToFind, out string discoveredVersion) => {
          IBackendServiceProvider match = providers.Where((p) => p.ModuleKey == moduleToFind).FirstOrDefault();
          discoveredVersion = match?.ModuleVersion;
          return (match != null);
        },
        gatewayOnly: gatewayOnly
      );

      foreach (IBackendServiceProvider provider in providers) {
        provider.RegisterServices(registrar, backendInitializationContext);
      }

    }

    private IServiceCollection _Services;
    private bool _ForceGatewayOnly = false;

    internal AspBackendServiceRegistrar(IServiceCollection services, bool forceGatewayOnly = false) {
      _Services = services;
      _ForceGatewayOnly = forceGatewayOnly;
    }


    #region " Services (BackendExtension / UjmwServiceEndpoint / UjmwProxy)' "

    public override void RegisterUjmwServiceEndpoint(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1
    ) {

      //HACK: muss irgendwie zusammengefasst werdenm, wegen dem einzel-overhead

      if (_ForceGatewayOnly) {
        _Services.AddSingleton(contractType, (sp) => {
          return DynamicClientFactory.CreateInstance(contractType); 
        });
      }
      else {
        _Services.AddSingleton(contractType, (sp) => {
          return factory.Invoke(); 
        });
      }

      _Services.AddDynamicUjmwControllers((ujmw) => {

        ujmw.AddControllerFor(
          contractType,
          new DynamicUjmwControllerOptions {
            ControllerRoute = $"{moduleScopingKey}/api/v{apiV}/{endpointAlias}"
          }
        );

      });

    }

    public override void RegisterHttpProxy(string moduleScopingKey, string endpointAlias, string forwardingAddress, int apiV = 1) {

      //TODO: dringend implementieren!
      throw new NotImplementedException();

    }

    /// <summary>
    ///   returns "{moduleScopingKey}/api/v{apiV}/{endpointAlias}"
    /// </summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="endpointAlias">An technical name (URL-SAFE!), used as alias to address this endpoint!</param>
    /// <param name="apiV"></param>
    /// <returns></returns>
    public static string BuildEndpointRoute(string moduleScopingKey, string endpointAlias, int apiV = 1) {
      return $"{moduleScopingKey}/api/v{apiV}/{endpointAlias}";
    }

    #endregion

    #region " CyclicTriggering "

    private IAspTriggerTargetRegistrar _CyclicTriggeringRegistrar = null;

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
    public override void RegisterCyclicTriggerTarget(
      Action<CancellationToken> target, int minWaitSeconds = 5, bool rescheduleWhileExecuting = true
    ) {

      if(_CyclicTriggeringRegistrar == null) {
        _Services.AddCyclicTriggering((r) => {
          _CyclicTriggeringRegistrar = r;
          r.EnableTriggeringEndpoint();
          r.EnableLoopbackSelftrigger();
        });
      }

      _CyclicTriggeringRegistrar.AddTriggerTarget(
        target, minWaitSeconds, rescheduleWhileExecuting,
        (Exception ex, int exCounter, ref DateTime suspendUntilUtc) => {
          suspendUntilUtc = DateTime.UtcNow.AddSeconds(10 * exCounter * exCounter);
        }
      );

    }

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
    /// <param name="rescheduleWhileExecuting">
    ///  Controls the behavior if a trigger is received while the previous execution is still running.
    ///  If set to true, the trigger will be rescheduled to execute immediately after the current execution finishes. 
    ///  If set to false, any trigger received during an ongoing execution will be ignored and not rescheduled.
    /// </param>
    public override void RegisterCyclicTriggerTarget(
      Func<CancellationToken, Task> taskFactory, int minWaitSeconds = 5, bool rescheduleWhileExecuting = true
    ) {

      if (_CyclicTriggeringRegistrar == null) {
        _Services.AddCyclicTriggering((r) => {
          _CyclicTriggeringRegistrar = r;
          r.EnableTriggeringEndpoint();
          r.EnableLoopbackSelftrigger();
        });
      }

      _CyclicTriggeringRegistrar.AddTriggerTarget(
        taskFactory, minWaitSeconds, rescheduleWhileExecuting,
        (Exception ex, int exCounter, ref DateTime suspendUntilUtc) => {
          suspendUntilUtc = DateTime.UtcNow.AddSeconds(10 * exCounter * exCounter);
        }
      );

    }

    #endregion

    #region " ServerCommands "

    public override void RegisterServerCommands(string moduleScopingKey, IServerCommandExecutor executor) {

      //TODO: dringend implementieren!
      throw new NotImplementedException();
      
    }

    #endregion

  }

  internal class AspModuleRegistrar : ModuleRegistrar {

    /// <summary></summary>
    /// <param name="baseUrl">  usually just '/' (first and last char must be a slash!) </param>
    /// <param name="services"></param>
    /// <param name="securityProvider"></param>
    /// <param name="tenancyProvider"></param>
    /// <param name="productDefinitionProvider"></param>
    /// <param name="autoCreateChooserPortfolio"></param>
    public AspModuleRegistrar(
      string baseUrl,
      IServiceCollection services,
      IPortfolioSecurityProvider securityProvider,
      ITenancyProvider tenancyProvider,
      IProductDefinitionProvider productDefinitionProvider,
      bool autoCreateChooserPortfolio
    ) : base(baseUrl, securityProvider, tenancyProvider, productDefinitionProvider, autoCreateChooserPortfolio) {

      _BackendServiceRegistrar = new AspBackendServiceRegistrar(services);
      _Services = services;

    }

    private AspBackendServiceRegistrar _BackendServiceRegistrar;
    private IServiceCollection _Services;

    #region " Proxy to 'AspBackendServiceRegistrar' "

    public override void RegisterUjmwServiceEndpoint(
      Type contractType, string moduleScopingKey, string endpointAlias, Func<object> factory, int apiV = 1
    ) {
      _BackendServiceRegistrar.RegisterUjmwServiceEndpoint(
        contractType, moduleScopingKey, endpointAlias, factory, apiV
      );
    }

    public override void RegisterHttpProxy(
      string moduleScopingKey, string endpointAlias, string forwardingAddress, int apiV = 1
    ) {
      _BackendServiceRegistrar.RegisterHttpProxy(
        moduleScopingKey, endpointAlias, forwardingAddress, apiV
      );
    }

    #endregion

  }

}
