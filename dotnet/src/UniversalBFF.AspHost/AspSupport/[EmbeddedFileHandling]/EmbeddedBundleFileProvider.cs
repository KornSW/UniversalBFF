using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace UniversalBFF.AspSupport {

  public partial class EmbeddedBundleFileProvider : IFileProvider {

    /// <summary>
    /// IMPORTANT: this must include the full base-path of the BFF-Application + the Mount-Point of the WebApp (/BffProductX/Alpha/app/)
    /// </summary>
    private string _WebAppBaseUrl;

    private string _SourceInfoString;
    private EmbeddedFileProvider _InnerProvider;

    /// <summary></summary>
    /// <param name="webAppBaseUrl">
    /// IMPORTANT: this must include the full base-path of the BFF-Application + the Mount-Point of the WebApp (/BffProductX/Alpha/app/) 
    /// </param>
    /// <param name="assemblyWithEmbeddedFiles"></param>
    /// <param name="embeddedFilesNamespace">
    /// WARNING: Errors here are very hard to track, because simply no file is returned!
    ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
    ///  * Folder separators are dots (.)
    ///  * Case-Sensitive
    ///  * Replace from '-' to '_', ' ' to '_'
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public EmbeddedBundleFileProvider(string webAppBaseUrl, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace) {

      _WebAppBaseUrl = webAppBaseUrl;

      _SourceInfoString = assemblyWithEmbeddedFiles.FullName;

      _InnerProvider = new EmbeddedFileProvider(
         assemblyWithEmbeddedFiles,
         embeddedFilesNamespace
       );

    }

    public IDirectoryContents GetDirectoryContents(string subpath) {

      if (subpath.StartsWith(_WebAppBaseUrl)) {
        subpath = subpath.Substring(_WebAppBaseUrl.Length);
      }

      return _InnerProvider.GetDirectoryContents(subpath);
    }

    public IFileInfo GetFileInfo(string subpath) {

      if (subpath.StartsWith(_WebAppBaseUrl)) {
        subpath = subpath.Substring(_WebAppBaseUrl.Length);
      }

      IFileInfo innerFileInfo = _InnerProvider.GetFileInfo(subpath);

      return new WrappedFileInfo(innerFileInfo, _WebAppBaseUrl, _SourceInfoString);
    }

    public IChangeToken Watch(string filter) {
      throw new NotImplementedException();
    }

  }

}
