//using Logging.SmartStandards;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.DependencyInjection.Extensions;
//using Microsoft.Extensions.DependencyInjection.ServiceLookup;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Collections.Concurrent;

//namespace Microsoft.AspNetCore.Builder {

//  public class FooToInstanciate {

//    public void TestVomFoo() {

//    }



//  }

//  /// <summary>
//  /// Provides a fallback singleton resolution for unregistered class types.
//  /// If the inner service provider cannot resolve a requested type but the type
//  /// is a class, this provider will:
//  /// 1. Create an instance via Activator.CreateInstance(...)
//  /// 2. Cache it as a singleton
//  /// 3. Return the same instance for all subsequent resolutions of that type.
//  /// </summary>
//  public sealed class SmartSingletonFallbackServiceProvider : IServiceProvider, ISupportRequiredService, IDisposable {
    
//    private readonly IServiceProvider _InnerServiceProvider;
//    private Boolean _IsDisposed;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="SmartSingletonFallbackServiceProvider"/> class.
//    /// </summary>
//    /// <param name="innerServiceProvider">The inner service provider built from the Microsoft DI container.</param>
//    public SmartSingletonFallbackServiceProvider(IServiceProvider innerServiceProvider) {

//      if (innerServiceProvider == null) {
//        throw new ArgumentNullException("innerServiceProvider");
//      }

//      _InnerServiceProvider = innerServiceProvider;
//      _SingletonCache = new ConcurrentDictionary<Type, object>();
//      _IsDisposed = false;

//    }

//    /// <summary>
//    /// Resolves a service instance of the specified type.
//    /// If the inner provider cannot resolve the service and the type is a class,
//    /// a singleton instance will be created via Activator.CreateInstance and cached.
//    /// </summary>
//    /// <param name="serviceType">The service type to resolve.</param>
//    /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
//    public Object GetService(Type serviceType) {

//      if (serviceType == null) {
//        throw new ArgumentNullException("serviceType");
//      }

//      this.ThrowIfDisposed();

//      DevLogger.LogTrace(0, 99999, "Resolving service type: " + serviceType.FullName);

//      // First try the inner provider.
//      Object resolvedInstance = _InnerServiceProvider.GetService(serviceType);
//      if (resolvedInstance != null) {
//        return resolvedInstance;
//      }

//      // If there is no registration, but the type is a class, create a singleton instance as fallback.
//      if (serviceType.IsClass) {
//        Object cachedInstance;
//        if (_SingletonCache.TryGetValue(serviceType, out cachedInstance)) {
//          return cachedInstance;
//        }

//        Object createdInstance = this.CreateSingletonInstance(serviceType);

//        // If creation failed, we return null (consistent with GetService behavior).
//        if (createdInstance == null) {
//          return null;
//        }

//        Object existingInstance = this._SingletonCache.GetOrAdd(serviceType, (type) => createdInstance);

//        return existingInstance;
//      }

//      // For non-class types (interfaces, abstract, etc.) we do not provide a fallback.
//      return null;
//    }

//    /// <summary>
//    /// Resolves a required service instance of the specified type.
//    /// Throws an exception if the service cannot be resolved.
//    /// </summary>
//    /// <param name="serviceType">The service type to resolve.</param>
//    /// <returns>The resolved service instance.</returns>
//    public Object GetRequiredService(Type serviceType) {
//      Object instance = this.GetService(serviceType);
//      if (instance == null) {
//        throw new InvalidOperationException("Unable to resolve required service for type: " + serviceType.FullName);
//      }

//      return instance;
//    }

//    /// <summary>
//    /// Disposes the provider and the underlying service provider if it implements IDisposable.
//    /// </summary>
//    public void Dispose() {
//      if (this._IsDisposed) {
//        return;
//      }

//      this._IsDisposed = true;

//      IDisposable disposableInner = this._InnerServiceProvider as IDisposable;
//      if (disposableInner != null) {
//        disposableInner.Dispose();
//      }
//    }

//    /// <summary>
//    /// Creates a singleton instance of the specified class type via Activator.CreateInstance.
//    /// </summary>
//    /// <param name="serviceType">The class type for which to create an instance.</param>
//    /// <returns>The created instance or null if creation fails.</returns>
//    private Object CreateSingletonInstance(Type serviceType) {
//      try {
//        DevLogger.LogTrace(0, 99999, "Creating fallback singleton instance for type: " + serviceType.FullName);

//        // Important: this will require a public parameterless constructor.
//        // If you need constructor injection here, you would have to use ActivatorUtilities.
//        Object instance = Activator.CreateInstance(serviceType);
//        return instance;
//      }
//      catch (Exception ex) {
//        // These exceptions are expected in certain scenarios (e.g., no parameterless ctor).
//        DevLogger.LogError(ex);

//        // Caller will interpret null as "could not create instance".
//        return null;
//      }
//    }

//    /// <summary>
//    /// Throws an ObjectDisposedException if this provider has been disposed.
//    /// </summary>
//    private void ThrowIfDisposed() {
//      if (_IsDisposed) {
//        throw new ObjectDisposedException("FallbackSingletonServiceProvider");
//      }
//    }


//    #region UMABU AUF SMARTSINGLEON SOBALD DA

//    private readonly ConcurrentDictionary<Type, object> _SingletonCache;




//    #endregion




//  }

//  /// <summary>
//  /// Service provider factory that wraps the default Microsoft DI service provider
//  /// with <see cref="SmartSingletonFallbackServiceProvider"/>.
//  /// </summary>
//  public sealed class SmartSingletonFallbackServiceProviderFactory : IServiceProviderFactory<IServiceCollection> {
    
//    /// <summary>
//    /// Creates the container builder (here simply the IServiceCollection).
//    /// </summary>
//    /// <param name="services">The service collection.</param>
//    /// <returns>The builder object (same IServiceCollection).</returns>
//    public IServiceCollection CreateBuilder(IServiceCollection services) {
     
//      if (services == null) {
//        throw new ArgumentNullException("services");
//      }

//      return services;
//    }

//    /// <summary>
//    /// Creates the service provider which will be used by the host.
//    /// Wraps the default provider with <see cref="SmartSingletonFallbackServiceProvider"/>.
//    /// </summary>
//    /// <param name="containerBuilder">The service collection to build from.</param>
//    /// <returns>The wrapped service provider.</returns>
//    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) {
     
//      if (containerBuilder == null) {
//        throw new ArgumentNullException("containerBuilder");
//      }

//      IServiceProvider innerProvider = containerBuilder.BuildServiceProvider();
//      SmartSingletonFallbackServiceProvider wrappedProvider = new SmartSingletonFallbackServiceProvider(innerProvider);
//      return wrappedProvider;
//    }
//  }

//  /// <summary>
//  /// Extension methods for integrating the fallback singleton provider into an ASP.NET Core host.
//  /// </summary>
//  public static class FallbackSingletonServiceProviderExtensions {

//    /// <summary>
//    /// Configures the host to use <see cref="SmartSingletonFallbackServiceProviderFactory"/>
//    /// as the service provider factory. This enables fallback singleton creation for
//    /// unregistered class types throughout the application.
//    /// </summary>
//    /// <param name="hostBuilder">The host builder.</param>
//    /// <returns>The same host builder instance for chaining.</returns>
//    public static IHostBuilder UseResolverFallbackToSmartSingleton(this IHostBuilder hostBuilder) {
//      if (hostBuilder == null) {
//        throw new ArgumentNullException("hostBuilder");
//      }

//      hostBuilder.UseServiceProviderFactory(new SmartSingletonFallbackServiceProviderFactory());

//      return hostBuilder;
//    }

//    /// <summary>
//    /// Replaces the ApplicationServices with a wrapped provider
//    /// that supports fallback singleton resolution.
//    /// </summary>
//    /// <param name="app">The application builder.</param>
//    /// <returns>The same application builder.</returns>
//    public static IApplicationBuilder UseResolverFallbackToSmartSingleton(this IApplicationBuilder app) {
//      if (app == null) {
//        throw new ArgumentNullException("app");
//      }

//      IServiceProvider oldProvider = app.ApplicationServices;
//      SmartSingletonFallbackServiceProvider wrappedProvider = new SmartSingletonFallbackServiceProvider(oldProvider);

//      app.ApplicationServices = wrappedProvider;

//      return app;
//    }
  
//  }
//}
