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

  internal sealed class MountRegistration {

    private string _RequestPathRelativeToApplicationBase;
    private IFileProvider _FileProvider;

    public MountRegistration(string requestPathRelativeToApplicationBase, IFileProvider fileProvider) {
      if (string.IsNullOrWhiteSpace(requestPathRelativeToApplicationBase)) {
        throw new ArgumentException("requestPath must not be null or empty.", nameof(requestPathRelativeToApplicationBase));
      }
      if (fileProvider == null) {
        throw new ArgumentNullException(nameof(fileProvider));
      }

      _RequestPathRelativeToApplicationBase = requestPathRelativeToApplicationBase;
      _FileProvider = fileProvider;
    }

    /// <summary>
    /// Normalized virtual mount path (e.g., "/app-1").
    /// </summary>
    public string RequestPathRelativeToApplicationBase {
      get { return _RequestPathRelativeToApplicationBase; }
      set { _RequestPathRelativeToApplicationBase = value; }
    }

    /// <summary>
    /// IFileProvider for this mount.
    /// </summary>
    public IFileProvider FileProvider {
      get { return _FileProvider; }
      set { _FileProvider = value; }
    }
  }

}
