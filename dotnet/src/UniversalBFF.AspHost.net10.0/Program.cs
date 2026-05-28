using Logging.SmartStandards; 
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using System.Reflection;
using System.Web.UJMW;

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

    public static void Main(string[] args) {

      WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

      IConfiguration config = builder.Configuration;

      OnConfigureServices(builder.Services, config);

      WebApplication app = builder.Build();

      OnRunApplication(app, config, app.Services, app.Environment, app.Lifetime);

      app.Run();
    }

  }

}
