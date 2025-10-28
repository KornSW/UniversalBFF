using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace UniversalBFF.AspSupport {

  public partial class EmbeddedBundleFileProvider {

    /// <summary>
    /// Wraps an existing IFileInfo instance and allows in-memory transformation of file contents,
    /// such as dynamic base URL replacement or injection of source information.
    /// </summary>
    private class WrappedFileInfo : IFileInfo {

      private readonly IFileInfo _Inner;
      private readonly string _BaseUrlNormalized;
      private readonly string _SourceInfo;
      private readonly object _Lock = new object();

      private Func<string, bool> _FileNameFilter = (string name) => {
        return name.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
      };

      private Action<string, StringBuilder> _ReplacementHook;
      private byte[] _ModifiedBytes;
      private string _ModifiedText;
      private DateTimeOffset _CachedFrom = DateTimeOffset.MinValue;

      private static readonly Regex _HtmlOpenTagRx = new Regex(
        @"<html\b([^>]*)>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
      );

      private static readonly Regex _BaseHrefRootRx = new Regex(
        @"<base\s+href\s*=\s*['""]\/['""]\s*\/?>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
      );

      /// <summary>
      /// Initializes a new instance of the <see cref="WrappedFileInfo"/> class.
      /// </summary>
      public WrappedFileInfo(IFileInfo innerFileInfo, string webAppBaseUrl, string sourceInfoString) {

        if (innerFileInfo == null) {
          throw new ArgumentNullException(nameof(innerFileInfo));
        }

        _Inner = innerFileInfo;
        _BaseUrlNormalized = WrappedFileInfo.NormalizeBaseUrl(webAppBaseUrl);
        _SourceInfo = sourceInfoString ?? string.Empty;

      }

      /// <summary>
      /// Enables content replacement functionality using a filename filter and optional hook.
      /// </summary>
      public void EnableContentReplacement(Func<string, bool> fileNameFilter, Action<string, StringBuilder> replacementHook) {
     
        _FileNameFilter = fileNameFilter ?? _FileNameFilter;
        _ReplacementHook = replacementHook;

        this.InvalidateCache();

      }

      public bool Exists {
        get { return _Inner.Exists; }
      }

      public string PhysicalPath {
        get { return _Inner.PhysicalPath; }
      }

      public string Name {
        get { return _Inner.Name; }
      }

      public DateTimeOffset LastModified {
        get { return _Inner.LastModified; }
      }

      public bool IsDirectory {
        get { return _Inner.IsDirectory; }
      }

      public long Length {
        get {
          if (this.ShouldProcess(this.Name)) {

            this.EnsureFreshModified();

            if (_ModifiedBytes != null) {
              return _ModifiedBytes.LongLength;
            } else {
              return 0L;
            }
          } else {
            return _Inner.Length;
          }
        }
      }

      public Stream CreateReadStream() {

        if (!this.ShouldProcess(this.Name)) {
          return _Inner.CreateReadStream();
        }

        this.EnsureFreshModified();

        return new MemoryStream(_ModifiedBytes, 0, _ModifiedBytes.Length, false, true);
      }

      private bool ShouldProcess(string fileName) {

        if (_FileNameFilter == null) {
          return false;
        }

        return _FileNameFilter.Invoke(fileName);
      }

      private static string NormalizeBaseUrl(string url) {

        if (string.IsNullOrWhiteSpace(url)) {
          return "/";
        }

        if (url.EndsWith("/")) {
          return url;
        }
        else {
          return url + "/";
        }
      }

      private void InvalidateCache() {
        lock (_Lock) {
          _ModifiedBytes = null;
          _ModifiedText = null;
          _CachedFrom = DateTimeOffset.MinValue;
        }
      }

      private static bool IsHtml(string fileName) {
        return fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
      }

      private void EnsureFreshModified() {
        DateTimeOffset lastModified = _Inner.LastModified;

        if (_ModifiedBytes != null && _CachedFrom >= lastModified) {
          return;
        }

        lock (_Lock) {
          lastModified = _Inner.LastModified;

          if (_ModifiedBytes != null && _CachedFrom >= lastModified) {
            return;
          }

          string text = string.Empty;
          using (Stream stream = _Inner.CreateReadStream()) {
            using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false), true)) {
              text = reader.ReadToEnd();
            }
          }

          StringBuilder sb = new StringBuilder(text.Length + 256);
          sb.Append(text);

          if (WrappedFileInfo.IsHtml(this.Name)) {
            sb = WrappedFileInfo.ReplaceHtmlOpenTagWithComment(sb, _SourceInfo);
            sb = WrappedFileInfo.ReplaceBaseHrefRoot(sb, _BaseUrlNormalized);
          }

          if (!string.IsNullOrEmpty(_BaseUrlNormalized)) {
            sb.Replace("_PUBLIC_PATH_", _BaseUrlNormalized);
          }

          if (_ReplacementHook != null) {
            _ReplacementHook.Invoke(this.Name, sb);
          }

          _ModifiedText = sb.ToString();
          _ModifiedBytes = Encoding.UTF8.GetBytes(_ModifiedText);
          _CachedFrom = lastModified;
        }
      }

      private static StringBuilder ReplaceHtmlOpenTagWithComment(StringBuilder sb, string sourceInfo) {
        Match match = WrappedFileInfo._HtmlOpenTagRx.Match(sb.ToString());
        if (!match.Success) {
          return sb;
        }

        string replacement = $"<html{match.Groups[1].Value}>\n  <!-- from '{sourceInfo}' -->";

        sb.Remove(match.Index, match.Length);
        sb.Insert(match.Index, replacement);

        return sb;
      }

      private static StringBuilder ReplaceBaseHrefRoot(StringBuilder sb, string newHref) {
       
        string replaced = WrappedFileInfo._BaseHrefRootRx.Replace(sb.ToString(), (Match m) => {
          return "<base href=\"" + newHref + "\" />";
        });

        if (!object.ReferenceEquals(replaced, sb.ToString())) {
          sb.Clear();
          sb.Append(replaced);
        }

        return sb;
      }

    }

  }

}

//public class EmbeddedBundleFileProvider : IFileProvider {

//  /// <summary>
//  /// IMPORTANT: this must include the full base-path of the BFF-Application + the Mount-Point of the WebApp (/BffProductX/Alpha/app/)
//  /// </summary>
//  private string _WebAppBaseUrl;

//  private string _SourceInfoString;
//  private EmbeddedFileProvider _InnerProvider;

//  /// <summary></summary>
//  /// <param name="webAppBaseUrl">
//  /// IMPORTANT: this must include the full base-path of the BFF-Application + the Mount-Point of the WebApp (/BffProductX/Alpha/app/) 
//  /// </param>
//  /// <param name="assemblyWithEmbeddedFiles"></param>
//  /// <param name="embeddedFilesNamespace">
//  /// WARNING: Errors here are very hard to track, because simply no file is returned!
//  ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
//  ///  * Folder separators are dots (.)
//  ///  * Case-Sensitive
//  ///  * Replace from '-' to '_', ' ' to '_'
//  /// </param>
//  [MethodImpl(MethodImplOptions.NoInlining)]
//  public EmbeddedBundleFileProvider(string webAppBaseUrl, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace) {

//    _WebAppBaseUrl = webAppBaseUrl;

//    _SourceInfoString = assemblyWithEmbeddedFiles.FullName;

//    _InnerProvider = new EmbeddedFileProvider(
//       assemblyWithEmbeddedFiles,
//       embeddedFilesNamespace
//     );

//  }

//  public IDirectoryContents GetDirectoryContents(string subpath) {

//    if (subpath.StartsWith(_WebAppBaseUrl)) {
//      subpath = subpath.Substring(_WebAppBaseUrl.Length);
//    }

//    return _InnerProvider.GetDirectoryContents(subpath);
//  }

//  public IFileInfo GetFileInfo(string subpath) {

//    if (subpath.StartsWith(_WebAppBaseUrl)) {
//      subpath = subpath.Substring(_WebAppBaseUrl.Length);
//    }

//    IFileInfo innerFileInfo = _InnerProvider.GetFileInfo(subpath);

//    return new WrappedFileInfo(innerFileInfo, _WebAppBaseUrl, _SourceInfoString);
//  }

//  public IChangeToken Watch(string filter) {
//    throw new System.NotImplementedException();
//  }

//private class WrappedFileInfo : IFileInfo {

//  private IFileInfo _InnerFileInfo;
//  private string _WebAppBaseUrl;
//  private string _SourceInfoString;

//  private Func<string, bool> _FileNameFilter = (string fileName)=> fileName.EndsWith(".html") || fileName.EndsWith(".js");
//  private Action<string, StringBuilder> _ReplacementHook = null;

//  /// <summary> </summary>
//  /// <param name="innerFileInfo"></param>
//  /// <param name="webAppBaseUrl">
//  /// IMPORTANT: this must include the full base-path of the BFF-Application + the Mount-Point of the WebApp (/BffProductX/Alpha/app/)
//  /// </param>
//  /// <param name="sourceInfoString"></param>
//  public WrappedFileInfo(IFileInfo innerFileInfo, string webAppBaseUrl, string sourceInfoString) {
//    _InnerFileInfo = innerFileInfo;
//    _WebAppBaseUrl = webAppBaseUrl;
//    _SourceInfoString = sourceInfoString;
//  }


//  /// <summary> </summary>
//  /// <param name="fileNameFilter"></param>
//  /// <param name="replacementHook">will be called for every relevant file passing (p1) again the filename and (p2) a StringBuilder already containing the whole file-content</param>
//  public void EnableContentReplacement(Func<string, bool> fileNameFilter, Action<string, StringBuilder> replacementHook) {
//    _FileNameFilter = fileNameFilter;
//    _ReplacementHook = replacementHook;
//  }

//  public bool Exists => _InnerFileInfo.Exists;
//  public string PhysicalPath => _InnerFileInfo.PhysicalPath;
//  public string Name => _InnerFileInfo.Name;
//  public DateTimeOffset LastModified => _InnerFileInfo.LastModified;
//  public bool IsDirectory => _InnerFileInfo.IsDirectory;

//  public long Length {
//    get {
//      if(_FileNameFilter != null && _FileNameFilter.Invoke(this.Name)) {
//        return this.GetModifiedContent().Length;
//      }
//      return _InnerFileInfo.Length;
//    }
//  }

//  public Stream CreateReadStream() {

//    if (_FileNameFilter != null && _FileNameFilter.Invoke(this.Name)) {

//      MemoryStream ms = new MemoryStream(Convert.ToInt32(this.Length));
//      StreamWriter sw = new StreamWriter(ms);
//      sw.Write(this.GetModifiedContent());
//      sw.Flush();
//      ms.Position = 0;

//      return ms;
//    }

//    return _InnerFileInfo.CreateReadStream();
//  }

//  private StringBuilder _ModifiedContent = null;

//  /// <summary>
//  /// Man-in-the-middle, um den HTML-Content (speziell den Base-Path) anzupassen!
//  /// </summary>
//  /// <returns></returns>
//  private StringBuilder GetModifiedContent() {

//    if (_ModifiedContent == null) {

//      using (Stream stream = _InnerFileInfo.CreateReadStream()) {

//        _ModifiedContent = new StringBuilder(Convert.ToInt32(stream.Length + 200));

//        StreamReader sr = new StreamReader(stream);

//        _ModifiedContent.Append(
//          sr.ReadToEnd()
//        );

//        _ModifiedContent.Replace("<html>", $"<html>\n  <!-- from '{_SourceInfoString}' -->");
//        _ModifiedContent.Replace("<base href=\"/\" />", $"<base href=\"{_WebAppBaseUrl}\" />");
//        _ModifiedContent.Replace("<base href=\"/\"/>", $"<base href=\"{_WebAppBaseUrl}\"/>");

//        _ModifiedContent.Replace("_PUBLIC_PATH_", _WebAppBaseUrl);

//        if (_ReplacementHook != null) {
//          _ReplacementHook.Invoke(this.Name, _ModifiedContent); 
//        }

//      }

//    }
//    return _ModifiedContent;
//  }

//}
