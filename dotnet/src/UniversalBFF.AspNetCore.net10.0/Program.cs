using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace UniversalBFF {

  public static partial class Program {
  
    static partial void OnConfigureServices(
      IServiceCollection services,
      IConfiguration config
    );

    static partial void OnRunApplication(
      WebApplication app,
      IConfiguration config,
      IServiceProvider services,
      IWebHostEnvironment environment,
      IHostApplicationLifetime lifetime
    );

    /// <summary>
    /// ASP-Layer!
    /// Exposed to be called from within a superior 'ConfigureServices' implementation (non IoC)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    public static void ConfigureServices(
      IServiceCollection services,
      IConfiguration config
    ) {
      OnConfigureServices(services, config);
    }

    /// <summary>
    /// ASP-Layer!
    /// Exposed to be called from within a superior 'RunApplication' implementation (non IoC)
    /// </summary>
    /// <param name="app"></param>
    /// <param name="config"></param>
    /// <param name="services"></param>
    /// <param name="environment"></param>
    /// <param name="lifetime"></param>
    public static void RunApplication(
      WebApplication app,
      IConfiguration config,
      IServiceProvider services,
      IWebHostEnvironment environment,
      IHostApplicationLifetime lifetime
    ) {
      OnRunApplication(app, config, services, environment, lifetime);
    }

    /// <summary>
    /// Full IoC - just run the UniversalBFF
    /// (this will internally use 'WebApplication.CreateBuilder()'/'Build()'/'Run()')
    /// </summary>
    /// <param name="args"></param>
    public static void RunUniversalBFF(string[] args) {

      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      IConfiguration config = builder.Configuration;

      OnConfigureServices(builder.Services, config);

      WebApplication app = builder.Build();

      OnRunApplication(app, config, app.Services, app.Environment, app.Lifetime);

      app.Run();
    }

  }

}
