using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System.IO;


/*
NUGET:

 Microsoft.AspNetCore
   .Mvc
   .Mvc.Core
   .SpaServices
   .SpaServices.Extensions
   .StaticFiles

 Microsoft.Extensions.FileProviders.Embedded
 
*/

//namespace Microsoft.AspNetCore {

//  public static class UserManagementHostingSetupExtensions {

//    public static void ConfigureCteModuleHosting(this IApplicationBuilder app, string baseUrl = "/module/") {
      
//      var opt = new UserManagementHostingOptions {
//        BaseUrl = baseUrl,
//      };

//      var spo = new StaticFileOptions {
//        ServeUnknownFileTypes = true,
//        OnPrepareResponse = ctx => {
//          var resp = ctx.Context.Response;
//          resp.Headers[HeaderNames.CacheControl] = "no-cache, no-store, must-revalidate";
//          resp.Headers[HeaderNames.Expires] = "0";
//          resp.Headers[HeaderNames.Pragma] = "no-cache";
//        },
//        FileProvider = new UserManagementBundleFileProvider(opt),
//        ContentTypeProvider = new FileExtensionContentTypeProvider(),
//        RequestPath = baseUrl.TrimEnd('/') //die sub-route unter der die dateien gehostet werden
//      };

//      app.UseSpaStaticFiles(spo);


//    }

//  }

//}
