using ComponentDiscovery;
using Composition.InstanceDiscovery;
using DistributedDataFlow;
using Logging.SmartStandards;
using Logging.SmartStandards.AspSupport;
using Logging.SmartStandards.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web.UJMW;
using System.Web.UJMW.SelfAnnouncement;
using UniversalBFF.AspSupport;
using UShell;

[assembly: AssemblyMetadata("SourceContext", "UniversalBFF-Core")]

namespace UniversalBFF {

  public class Startup {

    public Startup(IConfiguration configuration) {
      _Configuration = configuration;
    }

    private static IConfiguration _Configuration = null;
    public static IConfiguration Configuration { get { return _Configuration; } }

    string _ApiTitle = "UniversalBFF-Demo";
    Version _ApiVersion = null;

    private static InstanceDiscoveryContext _InstanceDiscoveryContext =  new InstanceDiscoveryContext(); 

    public void ConfigureServices(IServiceCollection services) {

      services.AddSmartStandardsLogging(_Configuration);

      var baseUrl = _Configuration.GetValue<string>("BaseUrl");
      string outDir = AppDomain.CurrentDomain.BaseDirectory;

      var pluginDir = _Configuration.GetValue<string>("PluginDir");
      if (!string.IsNullOrWhiteSpace(pluginDir)) {
        AssemblyResolving.AddResolvePath(pluginDir);
      }

      DevLogger.LogInformation("BFF IS INITIALIZING... (Base-URL: '{baseUrl}', Workdir: '{Workdir}')", baseUrl, outDir);

      _ApiVersion = typeof(Startup).Assembly.GetName().Version;

      // InstanceDiscoveryContext sharedContext = new InstanceDiscoveryContext();
      // InstanceDiscoveryContext.HookAmbienceManagment(() => sharedContext, (c) => { }, (c) => { });

      //TODO: es muss im InstanceDiscovery der default sein, dass es einfach ein singleton ist
      //und der clore-basierte verwenden muss dann im setup per opt-in kommen!
      InstanceDiscoveryContext.HookAmbienceManagment(
        () => _InstanceDiscoveryContext,
        (c) => { /* no action after create */ }, (c) => { /* no action after dispose */ }
      );

      services.LinkToInstanceDiscovery();


      IPortfolioSecurityProvider psp = _InstanceDiscoveryContext.GetInstance<IPortfolioSecurityProvider>(false);



      //IPortfolioSecurityProvider psp = InstanceDiscoveryContext.Current?.
      //  GetInstance<IPortfolioSecurityProvider>(false);





      if (psp == null) {
        SecLogger.LogCritical(0, 0, $"Could not discover any available implementation of '{nameof(IPortfolioSecurityProvider)}' (via InstanceDiscovery)! Only Anonymous parts will work!");
      }
      else {
        services.AddSingleton<IPortfolioSecurityProvider>(psp);
        SecLogger.LogInformation(0, 0, $"Discovered and added '{psp.GetType().FullName}' as implementation of '{nameof(IPortfolioSecurityProvider)}'!");

        if (psp is IOAuthService) {
          services.AddSingleton<IOAuthService>((IOAuthService)psp);
          SecLogger.LogInformation(0, 0, $"Discovered and added '{psp.GetType().FullName}' as implementation of '{nameof(IOAuthService)}'!");

          if (psp is IAuthPageBuilder) {
            services.AddSingleton<IAuthPageBuilder>((IAuthPageBuilder)psp);
          }
          else {
            services.AddSingleton<IAuthPageBuilder>(new DefaultAuthPageBuilder("Logon","",""));
          }

          services.AddOAuthServerController();
          SecLogger.LogInformation(0, 0, $"Enabled OAuth2 Controller as Facade over '{psp.GetType().FullName}'!");

        }
      }

      ITenancyProvider tp = null;//InstanceDiscoveryContext.Current.GetInstance<ITenancyProvider>(false);
      if (tp == null) {
        DevLogger.LogInformation(0, 0, $"Could not discover any available implementation of '{nameof(ITenancyProvider)}' (via InstanceDiscovery)!");
      }
      else {
        services.AddSingleton<ITenancyProvider>(tp);
        DevLogger.LogInformation(0, 0, $"Discovered and added '{tp.GetType().FullName}' as implementation of '{nameof(ITenancyProvider)}'!");
      }


      IProductDefinitionProvider pdp = null;//InstanceDiscoveryContext.Current.GetInstance<IProductDefinitionProvider>(false);
      if (pdp == null) {
        DevLogger.LogInformation(0, 0, $"Could not discover any available implementation of '{nameof(IProductDefinitionProvider)}' (via InstanceDiscovery)! Switching to fallback 'FileBasedProductDefinitionProvider'...");
        pdp = new FileBasedProductDefinitionProvider(outDir); 
      }
      services.AddSingleton<IProductDefinitionProvider>(pdp);

      ModuleRegistrar registrar = new AspModuleRegistrar(baseUrl, services, psp, tp, pdp, true);
      ModuleLoader loader = new ModuleLoader(registrar);

      services.AddSingleton<ModuleLoader>(loader);
      services.AddSingleton<IPortfolioService>(registrar);
      services.AddSingleton<IFrontendModuleRegistrar>(registrar);
      services.AddSingleton<ModuleRegistrar>(registrar);

      services.AddControllers();


      loader.Load();

      UjmwHostConfiguration.AuthHeaderEvaluator = AccessTokenValidator.TryValidateHttpAuthHeader;
      AccessTokenValidator.ConfigureTokenValidation(
        new LocalJwtIntrospector("TheSignKey"),
        (cfg) => {
        }
      );

      //TODO: das hier - anhand der konfig-struktur, welche aus der appsetings geladen werden soll
      //AccessTokenValidator.ConfigureByConfig(loader);

      services.AddDynamicUjmwControllers(r => {

        //r.AddControllerFor<FooToInstanciate>();

        ////NOTE: the '.svc' suffix is only to have the same url as in the WCF-Demo
        //r.AddControllerFor<IDemoService>(new DynamicUjmwControllerOptions {
        //  ControllerRoute = "v1/[Controller].svc"
        //});

        //r.AddControllerFor<IDemoFileService>(new DynamicUjmwControllerOptions {
        //  ControllerRoute = "FileStore"
        //});

        //var repoControllerOptions = new DynamicUjmwControllerOptions {
        //  ControllerRoute = "Repo/{0}",
        //  ControllerTitle = "Gen ({0})",
        //  ControllerNamePattern = "{0}Repository"
        //};
        //r.AddControllerFor<IGenericInterface<Foo, int>>(repoControllerOptions);
        //r.AddControllerFor<IGenericInterface<Bar, string>>(repoControllerOptions);

        //r.AddControllerFor<IFooStore>();
        //r.AddControllerFor<IBarStore>();

        //r.AddAnnouncementTriggerEndpoint();

      });

      bool useWinAuth = false;

      services.AddUjmwStandardSwaggerGen("OAuth");

      if (useWinAuth) {
        services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
      }
      else {
        //dummy ist nötig, damit die controller mit Authorize-Attribut überhaupt angesteurt werden können
        //sonst schreit die asp-eigene middleware...
        services.AddAuthentication("dummy").AddScheme<                      // vvv liegt noch hier im proejkt...
          AuthenticationSchemeOptions, Security.AccessTokenHandling.AspNetCore.AllowAllAuthHandler
        >("dummy", null);
      }

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(
      IApplicationBuilder app, IWebHostEnvironment env,
      ILoggerFactory loggerfactory, IHostApplicationLifetime lifetimeEvents, ModuleRegistrar moduleRegistrar
    ) {

      string baseUrl = _Configuration.GetValue<string>("BaseUrl");

      //required for the www-root
      app.UseStaticFiles();

      app.UseAmbientFieldAdapterMiddleware();

      if (!_Configuration.GetValue<bool>("ProdMode")) {
        app.UseDeveloperExceptionPage();
      }

      app.UseHttpsRedirection();
      
      app.UseRouting();

      //CORS: muss zwischen 'UseRouting' und 'UseEndpoints' liegen!
      app.UseCors(p =>
          p.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
      );

      app.UseAuthentication(); //<< WINDOWS-AUTH
      app.UseAuthorization();
      
      app.UseEndpoints(endpoints => {
        endpoints.MapControllers();
      });

      app.UseUjmwStandardSwagger(_Configuration, "OAuth");

      //ACHTUNG: Das muss zwingend VOR dem UseSpa aufgerufen werden!
      //app.ConfigureCteModuleHosting("/ui/modulename/");

      //app.ConfigureUShellSpaHosting(
      //  baseUrl + "portfolio",
      //  $"CTE ({_BffAssemblyVersion.ToString(3)})",
      //  baseUrl
      //);

      app.SetupSpaMultiHosting((IStaticHostingRegistrarForAsp registrar) => {

        UShellBundleFileProvider uShellFiles = new UShellBundleFileProvider(
          new UShellHostingOptions {
            BaseUrl = baseUrl,
            HtmlPageTitle = "Universal BFF",
            PortfolioUrl = baseUrl + "portfolio"
          }
        );
        registrar.Register(baseUrl, "app", uShellFiles);
        registrar.SetDefaultDoc("app", "index.html", true);

        moduleRegistrar.CollectAndRegisterFrontendExtensionsTo(registrar);

      });

      //app.ConfigureUShellSpaHosting(
      //  baseUrl + "portfolio",
      //  "Universal BFF",
      //  baseUrl + "app"
      //);

      SelfAnnouncementHelper.Configure(
        lifetimeEvents, app.ServerFeatures,
        (string[] baseUrls, EndpointInfo[] endpoints, bool act, ref string info) => {

          var sb = new StringBuilder(); 
          string timestamp = DateTime.Now.ToLongTimeString();

          Console.WriteLine("--------------------------------------");
          if (act) {
            Console.WriteLine("ANNOUNCE:");
          }
          else {
            Console.WriteLine("UN-ANNOUNCE:");
          }
          Console.WriteLine("--------------------------------------");
          foreach (EndpointInfo ep in endpoints) {
            foreach (string url in baseUrls) {
              Console.WriteLine(ep.ToString(url));
              sb.Append(ep.ToString(url));
              if (act) {
                sb.AppendLine(" >> ONLINE @" + timestamp);
              }
              else {
                sb.AppendLine(" >> offline @" + timestamp);
              }

            }
          }
          Console.WriteLine("--------------------------------------");

          File.WriteAllText("_AnnouncementInfo.txt", sb.ToString());

          info = "was additionally written into file '_AnnouncementInfo.txt'";

        },
        autoTriggerInterval: 1
      );

    }

  }

}
