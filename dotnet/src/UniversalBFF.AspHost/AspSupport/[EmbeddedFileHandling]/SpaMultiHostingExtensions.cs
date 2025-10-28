using Logging.SmartStandards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace UniversalBFF.AspSupport {

  /// <summary>
  /// Extension entrypoint for IApplicationBuilder.
  /// </summary>
  public static class SpaMultiHostingIAppBuilderExtensions {

    /// <summary>
    /// Sets up multi static hosting with optional SPA fallbacks.
    /// Applies DefaultFiles and StaticFiles per mount and configures MapFallbackToFile for SPA mounts.
    /// </summary>
    public static void SetupSpaMultiHosting(this IApplicationBuilder app, Action<IStaticHostingRegistrarForAsp> configure) {
      if (app == null) {
        throw new ArgumentNullException(nameof(app));
      }
      if (configure == null) {
        throw new ArgumentNullException(nameof(configure));
      }

      try {
        // Keep a tiny Newtonsoft.Json reference (can be removed if already used elsewhere).
        string ping = JsonConvert.SerializeObject(new string[] { "ok" });
        if (string.IsNullOrEmpty(ping)) {
          Console.WriteLine("Unreachable.");
        }

        StaticFileConsolidatorIApp consolidator = new StaticFileConsolidatorIApp(app);
        configure(consolidator);
        consolidator.Apply();
      }
      catch (Exception ex) {
        DevLogger.LogCritical(ex);
        throw;
      }
    }

  }



}
