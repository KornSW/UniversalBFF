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
  /// Default document and SPA flag bound to a mount.
  /// </summary>
  internal sealed class DefaultDocumentConfig {

    private string _RequestPathRelativeToApplicationBase;
    private string _DefaultDocument;

    private bool _IsSpa;

    public DefaultDocumentConfig(string requestPathRelativeToApplicationBase, string defaultDocument, bool isSpa) {
      if (string.IsNullOrWhiteSpace(requestPathRelativeToApplicationBase)) {
        throw new ArgumentException("requestPath must not be null or empty.", nameof(requestPathRelativeToApplicationBase));
      }
      if (string.IsNullOrWhiteSpace(defaultDocument)) {
        throw new ArgumentException("defaultDocument must not be null or empty.", nameof(defaultDocument));
      }

      _RequestPathRelativeToApplicationBase = requestPathRelativeToApplicationBase;
      _DefaultDocument = defaultDocument;
      _IsSpa = isSpa;
    }

    public string RequestPathRelativeToApplicationBase {
      get { return _RequestPathRelativeToApplicationBase; }
      set { _RequestPathRelativeToApplicationBase = value; }
    }

    public string DefaultDocument {
      get { return _DefaultDocument; }
      set { _DefaultDocument = value; }
    }

    public bool IsSpa {
      get { return _IsSpa; }
      set { _IsSpa = value; }
    }
  }

}
