using Composition.InstanceDiscovery.AspNetCore;
using Logging.SmartStandards;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Composition.InstanceDiscovery.AspNetCore {

  public class DiscoverableInstanceProviderForAspServiceCollection : IDiscoverableInstanceProvider, IServiceProvider {

    private static IServiceCollection _ServiceCollection;
    private static IServiceProvider _Provider;

    internal static void BindToServiceCollection(IServiceCollection services, IServiceProvider provider = null) {
      _ServiceCollection = services;
      _Provider = provider;
    }

    public Type RepresentingOriginType {
      get {
        return typeof(IServiceCollection);
      }
    }

    public Type[] DedicatedDiscoverableTypes {
      get {
        return _ServiceCollection.Select((s) => s.ServiceType).ToArray();
      }
    }

    public void DeclarePriorizationRules(Action<Type, bool> callback) {
    }

    public bool TryGetInstance(
      InstanceDiscoveryContext requestingContext, Type requestedType,
      ref object instance, ref LifetimeResponsibility lifetimeResponsibility
    ) {

      IServiceProvider sp = _Provider;
      if (sp == null) {
        sp = this;
      }

      instance = null;
      lifetimeResponsibility = LifetimeResponsibility.Managed;

      ServiceDescriptor sDesc = _ServiceCollection.Where((s) => s.ServiceType == requestedType).FirstOrDefault();

      if (sDesc != null) {
        if (sDesc.ImplementationInstance != null) {
          instance = sDesc.ImplementationInstance;
        }
        else if (sDesc.ImplementationFactory != null) {
          instance = sDesc.ImplementationFactory.Invoke(sp);
          lifetimeResponsibility = LifetimeResponsibility.Delegated; //self created
        }
        else if (sDesc.ImplementationType != null) {
          instance = ActivatorUtilities.CreateInstance(sp, sDesc.ImplementationType);
          lifetimeResponsibility = LifetimeResponsibility.Delegated; //self created
        }
      }

      return (instance != null);
    }


    object IServiceProvider.GetService(Type serviceType) {
      object instance = null;
      LifetimeResponsibility lr = LifetimeResponsibility.Managed;
      this.TryGetInstance(null, serviceType, ref instance, ref lr);
      return instance;
    }

  }

  internal sealed class AspServiceProviderWithInstanceDiscoveryFallback : IServiceProvider, ISupportRequiredService {

    private readonly IServiceProvider _InnerServiceProvider;

    public AspServiceProviderWithInstanceDiscoveryFallback(IServiceProvider innerServiceProvider) {
      _InnerServiceProvider = innerServiceProvider;
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// If the inner provider returns null and the type is a class,
    /// a fallback singleton instance is created via Activator.CreateInstance(...)
    /// and cached.
    /// </summary>
    /// <param name="serviceType">The requested service type.</param>
    /// <returns>The resolved service instance or null.</returns>
    public Object GetService(Type serviceType) {

      if (serviceType == null) {
        throw new ArgumentNullException("serviceType");
      }

      DevLogger.LogTrace(0, 74504, nameof(AspServiceProviderWithInstanceDiscoveryFallback) + " resolving: " + serviceType.FullName);

      object resolvedInstance = _InnerServiceProvider.GetService(serviceType);
      if (resolvedInstance != null) {
        DevLogger.LogTrace(0, 74505, nameof(AspServiceProviderWithInstanceDiscoveryFallback) + " provides '" + resolvedInstance.GetType().FullName + "' from original ServiceProvider.");
        return resolvedInstance;
      }

      try {
        InstanceDiscoveryContext.Current.TryGetInstanceOf(serviceType, ref resolvedInstance);
      }
      catch (Exception ex) {
        DevLogger.LogError(ex.Wrap(74508, nameof(AspServiceProviderWithInstanceDiscoveryFallback) + " got exception from InstanceDiscovery while resolving '" + serviceType.FullName + "': " + ex.Message));
        return resolvedInstance;
      }

      if (resolvedInstance != null) {
        DevLogger.LogTrace(0, 74506, nameof(AspServiceProviderWithInstanceDiscoveryFallback) + " provides '" + resolvedInstance.GetType().FullName + "' from InstanceDiscovery.");
      }
      else {
        DevLogger.LogWarning(0, 74507, nameof(AspServiceProviderWithInstanceDiscoveryFallback) + " could not find any '" + serviceType.FullName + "'.");
      }

      return resolvedInstance;
    }

    /// <summary>
    /// Gets a required service of the specified type or throws an exception if it cannot be resolved.
    /// </summary>
    /// <param name="serviceType">The requested service type.</param>
    /// <returns>The resolved service instance.</returns>
    public Object GetRequiredService(Type serviceType) {
      Object instance = this.GetService(serviceType);

      //if (instance == null) {
      //  throw new InvalidOperationException("Unable to resolve required service for type: " + serviceType.FullName);
      //}

      return instance;
    }

  }

  /// <summary>
  /// Controller activator that uses ActivatorUtilities together with
  /// a fallback-enabled service provider to create controllers.
  /// If a dependency is not registered but is a class, it will be created
  /// once via Activator and reused as a singleton.
  /// </summary>
  internal sealed class ControllerActivatorWithInstanceDiscoverySupport : IControllerActivator {

    public ControllerActivatorWithInstanceDiscoverySupport() {
    }

    /// <summary>
    /// Creates the controller instance for the given context.
    /// </summary>
    /// <param name="context">The controller context.</param>
    /// <returns>The created controller instance.</returns>
    public object Create(ControllerContext context) {

      if (context == null) {
        throw new ArgumentNullException("context");
      }

      Type controllerType = context.ActionDescriptor.ControllerTypeInfo.AsType();

      DevLogger.LogTrace(0, 74502, nameof(ControllerActivatorWithInstanceDiscoverySupport) + " creating controller: " + controllerType.FullName);

      IServiceProvider requestServices = context.HttpContext.RequestServices;
      if (requestServices == null) {
        throw new InvalidOperationException("HttpContext.RequestServices is null, cannot create controller instance.");
      }

      var wrapperProvider = new AspServiceProviderWithInstanceDiscoveryFallback(requestServices);

      // Use ActivatorUtilities so that normal DI works, but missing class dependencies
      // will be created via our wrapper.
      object controllerInstance = ActivatorUtilities.CreateInstance(wrapperProvider, controllerType);

      return controllerInstance;
    }

    /// <summary>
    /// Releases the controller.
    /// </summary>
    /// <param name="context">The controller context.</param>
    /// <param name="controller">The controller instance.</param>
    public void Release(ControllerContext context, object controller) {

      if (controller == null) {
        return;
      }

      IDisposable disposableController = controller as IDisposable;

      if (disposableController != null) {
        disposableController.Dispose();
      }

    }

  }

}

namespace Microsoft.AspNetCore.Builder {

  public static class InstanceDiscoveryServiceCollectionExtensions {

    /// <summary>
    /// Replaces the default IControllerActivator with <see cref="ControllerActivatorWithInstanceDiscoverySupport"/>,
    /// which in turn uses a fallback-enabled service provider for controller creation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="consumeFromInstanceDiscovery">If there is no service registered to the ServiceCollection, a Fallback (wrapped ControllerActivator) will try to find one via instance discovery.</param>
    /// <param name="provideToInstanceDiscovery">Exposes the ServiceCollection to instance discovery in order to make the instances usable for other consumers.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection LinkToInstanceDiscovery(
      this IServiceCollection services,
      bool consumeFromInstanceDiscovery = true,
      bool provideToInstanceDiscovery = true
    ) {

      if (services == null) {
        throw new ArgumentNullException("services");
      }

      if (provideToInstanceDiscovery) {

        //TODO: Pass IServiceProvider from BuildServiceProvider

        DiscoverableInstanceProviderForAspServiceCollection.BindToServiceCollection(services);
        DevLogger.LogTrace(0, 74501, "IServiceCollection will now provide instances for Instance Discovery.");
      }

      if (consumeFromInstanceDiscovery) { 
        services.Replace(
          ServiceDescriptor.Transient<IControllerActivator, ControllerActivatorWithInstanceDiscoverySupport>()
        );
        DevLogger.LogTrace(0, 74502, "IServiceCollection will now use Instance Discovery as Fallback.");
      }

      return services;
    }

  }

}
