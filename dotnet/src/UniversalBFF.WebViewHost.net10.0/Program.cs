using Logging.SmartStandards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace UniversalBFF {

  public static partial class Program {

    internal static WebViewAspNetCoreAdapter _AspApplication;
    internal static FormMain _MainWndow;

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

    [STAThread]
    public static void Main() {

      Application.SetHighDpiMode(HighDpiMode.SystemAware);
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      try {

        _AspApplication = new WebViewAspNetCoreAdapter(
          Assembly.GetExecutingAssembly(),
          (s,c) => OnConfigureServices(s,c),
          (a,c,s,e,l) => OnRunApplication(a,c,s,e,l)
        );

        _MainWndow = new FormMain();

         Application.Run(_MainWndow);

      }
      finally {

        if(_MainWndow != null) _MainWndow.Dispose();

        if(_AspApplication != null) _AspApplication.Dispose();

      }

    }

  }

}
