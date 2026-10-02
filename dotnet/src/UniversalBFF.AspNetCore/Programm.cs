using ComponentDiscovery;
using Composition.InstanceDiscovery;
using Logging.SmartStandards;
using Logging.SmartStandards.AspSupport;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualBasic;
using Security.AccessTokenHandling;
using Security.AccessTokenHandling.OAuth.Server;
using System;
using System.Web.UJMW;
using UniversalBFF.AspSupport;
using UShell;

namespace UniversalBFF {

  public static partial class Program {

    private static InstanceDiscoveryContext _InstanceDiscoveryContext = new InstanceDiscoveryContext();

    static partial void OnConfigureServices(IServiceCollection services, IConfiguration config) {
      ConfigureServices(services, config, null);
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration config, BffStartupArgs args) {

      if(args == null) {
        args = BffStartupArgs.CreateDefault(config);
      }

      #region " Basiscs (environment & logging) "

      //SmartStandardsCheapInitializer.OnConfigureServices(services, config);

      services.AddSmartStandardsLogging(config);

      if (!string.IsNullOrWhiteSpace(args.PluginDir)) {
        AssemblyResolving.AddResolvePath(args.PluginDir);
      }

      DevLogger.LogInformation(
        0, 77109, 
        "BFF IS INITIALIZING... (Base-URL: '{baseUrl}', Workdir: '{Workdir}')", 
        args.BaseUrl, AppDomain.CurrentDomain.BaseDirectory
      );

      services.AddSingleton(args);

      #endregion

      #region " Find provider-instances for core-functionality " 

      // Activate InstanceDiscovery and try to find providers-instances for core-functionality...
      services.LinkToInstanceDiscovery();

      //can decide, which user is allowed to access which product, based on the current identity
      IPortfolioSecurityProvider psp;

      if (args.PortfolioSecurityProviderResolver != null) {
        psp = args.PortfolioSecurityProviderResolver();
      }
      else {
        psp = _InstanceDiscoveryContext.GetInstance<IPortfolioSecurityProvider>(false);
      }

      if (psp == null) {
        SecLogger.LogCritical(0, 77110, $"Could not discover any available implementation of '{nameof(IPortfolioSecurityProvider)}' (via InstanceDiscovery)! Only Anonymous parts will work!");
      }
      else {
        services.AddSingleton<IPortfolioSecurityProvider>(psp);
        SecLogger.LogInformation(0, 77111, $"Discovered and added '{psp.GetType().FullName}' as implementation of '{nameof(IPortfolioSecurityProvider)}'!");

        if (psp is IOAuthService) {

          //OPTIONAL: If the discovered security-provider also implements IOAuthService,
          //we can use it to provide an OAuth2-Endpoint for the BFF.

          services.AddSingleton<IOAuthService>((IOAuthService)psp);
          SecLogger.LogInformation(0, 77112, $"Discovered and added '{psp.GetType().FullName}' as implementation of '{nameof(IOAuthService)}'!");

          if (psp is IAuthPageBuilder) {
            services.AddSingleton<IAuthPageBuilder>((IAuthPageBuilder)psp);
          }
          else {
            services.AddSingleton<IAuthPageBuilder>(new DefaultAuthPageBuilder("Logon", "", ""));
          }

          services.AddOAuthServerController();
          SecLogger.LogInformation(0, 77113, $"Enabled OAuth2 Controller as Facade over '{psp.GetType().FullName}'!");

        }
      }


      //supplies mapping between products and tenants, and which tenants are available at all
      ITenancyProvider tp;

      if (args.TenancyProviderResolver != null) {
        tp = args.TenancyProviderResolver();
      }
      else {
        tp = _InstanceDiscoveryContext.GetInstance<ITenancyProvider>(false);
      }

      if (tp == null) {
        DevLogger.LogInformation(0, 77114, $"Could not discover any available implementation of '{nameof(ITenancyProvider)}' (via InstanceDiscovery)!");
      }
      else {
        services.AddSingleton<ITenancyProvider>(tp);
        DevLogger.LogInformation(0, 77115, $"Discovered and added '{tp.GetType().FullName}' as implementation of '{nameof(ITenancyProvider)}'!");
      }


      //supplies definitions of products (metadata-layer, not yet portfolios)
      IProductDefinitionProvider pdp;

      if (args.ProductDefinitionProviderResolver != null) {
        pdp = args.ProductDefinitionProviderResolver();
      }
      else {
        pdp = _InstanceDiscoveryContext.GetInstance<IProductDefinitionProvider>(false);
      }

      if (pdp == null) {
        DevLogger.LogInformation(0, 77116, $"Could not discover any available implementation of '{nameof(IProductDefinitionProvider)}' (via InstanceDiscovery)! Switching to fallback 'FileBasedProductDefinitionProvider'...");
        pdp = new FileBasedProductDefinitionProvider(AppDomain.CurrentDomain.BaseDirectory);
      }
      services.AddSingleton<IProductDefinitionProvider>(pdp);

      #endregion

      #region " Load modules "

      //stores and aggregates all frontend-extensions and backend-services
      ModuleRegistrar registrar = new AspModuleRegistrar(args.BaseUrl, services, psp, tp, pdp, true);

      //Finds and Crawls over available Modules.
      //Will push the registrar into each module-provider durig load,
      //so that the modules can register their frontend-extensions and backend-services...
      ModuleLoader loader = new ModuleLoader(
        registrar,
        args.FrontendModuleProvidersResolver, //if null -> ComponentDiscovery will be used...
        args.BackendServiceProvidersResolver  //if null -> ComponentDiscovery will be used...
      );

      //GO!
      loader.Load();

      services.AddSingleton<ModuleLoader>(loader);

      services.AddSingleton<IFrontendModuleRegistrar>(registrar);
      services.AddSingleton<ModuleRegistrar>(registrar);

      //the registrar also represents the IPortfolioService
      //(which can act as catalog for all available portfolios)
      services.AddSingleton<IPortfolioService>(registrar);

      //...expose it!
      services.AddControllerForUShellPortfolioService();

      services.AddControllers();

      #endregion

      #region " Setup authentication & authorization "

      bool useWinAuth = false;
      UjmwHostConfiguration.AuthHeaderEvaluator = AccessTokenValidator.TryValidateHttpAuthHeader;
      AccessTokenValidator.ConfigureTokenValidation(
        new LocalJwtIntrospector("TheSignKey"),
        (cfg) => {
        }
      );

      //TODO: das hier - anhand der konfig-struktur, welche aus der appsetings geladen werden soll
      //AccessTokenValidator.ConfigureByConfig(loader);

      AuthenticationBuilder auth = services.AddAuthentication((opt) => {
        //opt.DefaultAuthenticateScheme = useWinAuth ? NegotiateDefaults.AuthenticationScheme : "dummy";
        //opt.DefaultChallengeScheme = useWinAuth ? NegotiateDefaults.AuthenticationScheme : "dummy";
      });

      if (useWinAuth) {
        auth.AddNegotiate();
      }
      else {
        // Nur nötig, damit [Authorize] nicht "no authentication handler..." wirft
        auth.AddScheme<AuthenticationSchemeOptions, Security.AccessTokenHandling.AspNetCore.AllowAllAuthHandler>("dummy", _ => { });
      }

      // Wichtig: KEINE FallbackPolicy setzen, sonst wäre alles per Default geschützt!
      // services.AddAuthorization(); reicht.
      services.AddAuthorization();

      if (useWinAuth) {

        AuthenticationBuilder authBuilder = services.AddAuthentication(NegotiateDefaults.AuthenticationScheme);

        authBuilder.AddNegotiate();
        authBuilder.AddScheme<AuthenticationSchemeOptions, Security.AccessTokenHandling.AspNetCore.AllowAllAuthHandler>("dummy", null);

      }
      else {

        //dummy ist nötig, damit die controller mit Authorize-Attribut überhaupt angesteurt werden können
        //sonst schreit die asp-eigene middleware...
        services.AddAuthentication("dummy2").AddScheme<                      // vvv liegt noch hier im proejkt...
          AuthenticationSchemeOptions, Security.AccessTokenHandling.AspNetCore.AllowAllAuthHandler
        >("dummy2", null);

      }

      #endregion

      services.AddSwaggerGenSmartStandardsFlavored();

    }

    static partial void OnRunApplication(
      WebApplication app, IConfiguration config, IServiceProvider services,
      IWebHostEnvironment environment, IHostApplicationLifetime lifetime
    ) {

      //Vorsicht: Diese Zeil muss so weit oben stehen, sonst ist sie wirkungslos
      app.UseAmbientFieldAdapterMiddleware();

      BffStartupArgs args = services.GetRequiredService<BffStartupArgs>();
      ModuleRegistrar moduleRegistrar = services.GetRequiredService<ModuleRegistrar>();
      IFeatureCollection serverFeatures = ((IApplicationBuilder)app).ServerFeatures;

      app.SetupSpaMultiHosting((IStaticHostingRegistrarForAsp registrar) => {

        string baseUrl = config.GetValue<string>("BaseUrl");

        registrar.Register(
          baseUrl, "/",
          new UShellBundleFileProvider(
            new UShellHostingOptions {
              BaseUrl = baseUrl,
              HtmlPageTitle = args.HtmlPageTitle,
              PortfolioUrl = baseUrl //+ "portfolio" das war fürher!
            }
          )
        );

        registrar.SetDefaultDoc("/", "index.html", true);

        moduleRegistrar.CollectAndRegisterFrontendExtensionsTo(registrar);

      });

      if (!config.GetValue<bool>("ProdMode")) {
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

      app.MapControllers();

    }

  }

}
