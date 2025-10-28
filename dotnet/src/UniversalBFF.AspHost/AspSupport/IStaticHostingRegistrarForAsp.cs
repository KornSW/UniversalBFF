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
  /// Registrar interface exposed to the caller for registering mounts and defaults.
  /// </summary>
  public interface IStaticHostingRegistrarForAsp : IStaticHostingRegistrar {

    /// <summary>
    /// Register a mount under the given request path with a file provider.
    /// Example: "app-1/" or "/app-1/".
    /// </summary>
    /// <param name="applicationBase"> usually just '/' (first and last char must be a slash!)</param>
    /// <param name="mountPointPathRelativeToApplicationBase"> for example 'ui/spaX/' </param>
    /// <param name="fileProvider"></param>
    void Register(string applicationBase, string mountPointPathRelativeToApplicationBase, IFileProvider fileProvider);

  }

}
