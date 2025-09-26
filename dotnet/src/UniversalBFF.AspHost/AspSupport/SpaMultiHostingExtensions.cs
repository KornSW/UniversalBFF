using Logging.SmartStandards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
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
    void Register(string requestPath, IFileProvider fileProvider);

  }

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

  /// <summary>
  /// Mount registration record.
  /// </summary>
  internal sealed class MountRegistration {

    private string _RequestPath;
    private IFileProvider _FileProvider;

    public MountRegistration(string requestPath, IFileProvider fileProvider) {
      if (string.IsNullOrWhiteSpace(requestPath)) {
        throw new ArgumentException("requestPath must not be null or empty.", nameof(requestPath));
      }
      if (fileProvider == null) {
        throw new ArgumentNullException(nameof(fileProvider));
      }

      _RequestPath = requestPath;
      _FileProvider = fileProvider;
    }

    /// <summary>
    /// Normalized virtual mount path (e.g., "/app-1").
    /// </summary>
    public string RequestPath {
      get { return _RequestPath; }
      set { _RequestPath = value; }
    }

    /// <summary>
    /// IFileProvider for this mount.
    /// </summary>
    public IFileProvider FileProvider {
      get { return _FileProvider; }
      set { _FileProvider = value; }
    }
  }

  /// <summary>
  /// Default document and SPA flag bound to a mount.
  /// </summary>
  internal sealed class DefaultDocumentConfig {
    private string _RequestPath;
    private string _DefaultDocument;
    private bool _IsSpa;

    public DefaultDocumentConfig(string requestPath, string defaultDocument, bool isSpa) {
      if (string.IsNullOrWhiteSpace(requestPath)) {
        throw new ArgumentException("requestPath must not be null or empty.", nameof(requestPath));
      }
      if (string.IsNullOrWhiteSpace(defaultDocument)) {
        throw new ArgumentException("defaultDocument must not be null or empty.", nameof(defaultDocument));
      }

      _RequestPath = requestPath;
      _DefaultDocument = defaultDocument;
      _IsSpa = isSpa;
    }

    public string RequestPath {
      get { return _RequestPath; }
      set { _RequestPath = value; }
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

  /// <summary>
  /// Consolidator implementation for IApplicationBuilder.
  /// </summary>
  internal sealed class StaticFileConsolidatorIApp : IStaticHostingRegistrarForAsp {

    private readonly IApplicationBuilder _App;
    private readonly List<MountRegistration> _Mounts;
    private readonly List<DefaultDocumentConfig> _Defaults;

    public StaticFileConsolidatorIApp(IApplicationBuilder app) {
      if (app == null) {
        throw new ArgumentNullException(nameof(app));
      }

      _App = app;
      _Mounts = new List<MountRegistration>();
      _Defaults = new List<DefaultDocumentConfig>();
    }

    /// <summary></summary>
    /// <param name="requestPath"></param>
    /// <param name="assemblyWithEmbeddedFiles"></param>
    /// <param name="embeddedFilesNamespace">
    /// WARNING: Errors here are very hard to track, because simply no file is returned!
    ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
    ///  * Folder separators are dots (.)
    ///  * Case-Sensitive
    ///  * Replace from '-' to '_', ' ' to '_'
    /// </param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Register(string requestPath, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace) {
      string normalized = NormalizeMount(requestPath);

      // Prevent duplicate mount registration of same path; last write wins behavior could be confusing.
      // We choose to throw to keep configuration deterministic.
      if (ContainsMount(normalized)) {
        throw new InvalidOperationException("Duplicate mount registration for path: " + normalized);
      }

      EmbeddedBundleFileProvider fileProvider = new EmbeddedBundleFileProvider(normalized + "/", assemblyWithEmbeddedFiles, embeddedFilesNamespace);
      MountRegistration reg = new MountRegistration(normalized, fileProvider);
      _Mounts.Add(reg);

      DevLogger.LogTrace(0, 99999, "Registered mount " + normalized);
    }

    /// <summary>
    /// Register a mount under a given path with a specific file provider.
    /// </summary>
    public void Register(string requestPath, IFileProvider fileProvider) {
      string normalized = NormalizeMount(requestPath);

      // Prevent duplicate mount registration of same path; last write wins behavior could be confusing.
      // We choose to throw to keep configuration deterministic.
      if (ContainsMount(normalized)) {
        throw new InvalidOperationException("Duplicate mount registration for path: " + normalized);
      }

      MountRegistration reg = new MountRegistration(normalized, fileProvider);
      _Mounts.Add(reg);

      DevLogger.LogTrace(0, 99999, "Registered mount " + normalized);
    }

    /// <summary>
    /// Set default document (and optionally SPA fallback) for a mount.
    /// </summary>
    public void SetDefaultDoc(string requestPath, string defaultDocument, bool spa) {
      string normalized = NormalizeMount(requestPath);

      DefaultDocumentConfig existing = FindDefault(normalized);
      if (existing != null) {
        // Keep it simple and strict: do not allow multiple defaults for same mount.
        throw new InvalidOperationException("Default already configured for mount: " + normalized);
      }

      DefaultDocumentConfig cfg = new DefaultDocumentConfig(normalized, defaultDocument, spa);
      _Defaults.Add(cfg);

      string spaFlagText = "false";
      if (spa) {
        spaFlagText = "true";
      }
      DevLogger.LogTrace(0, 99999, "Configured default for " + normalized + " (spa=" + spaFlagText + ")");
    }

    /// <summary>
    /// Apply all registrations to the pipeline:
    /// - UseDefaultFiles + UseStaticFiles per mount,
    /// - UseRouting + UseEndpoints(... MapFallbackToFile) for SPA mounts.
    /// </summary>
    public void Apply() {
      try {
        // 1) Default files and static files per mount.
        foreach (MountRegistration reg in _Mounts) {
          DefaultDocumentConfig cfg = FindDefault(reg.RequestPath);

          if (cfg != null) {
            DefaultFilesOptions dfo = BuildDefaultFilesOptions(reg, cfg);
            _App.UseDefaultFiles(dfo);
          }

          StaticFileOptions sfo = BuildStaticFileOptions(reg);
          _App.UseStaticFiles(sfo);
        }

        // ... in StaticFileConsolidatorIApp.Apply()

        // 2) Routing + SPA fallbacks (only if any SPA is configured).
        if (HasSpaFallbacks()) {
          _App.UseRouting();

          _App.UseEndpoints((IEndpointRouteBuilder endpoints) => {
            foreach (DefaultDocumentConfig cfg in _Defaults) {
              if (cfg.IsSpa) {
                // OLD (remove): endpoints.MapFallbackToFile(pattern, file);
                // NEW: map a catch-all under the mount and serve the default doc from the correct provider
                string pattern = cfg.RequestPath + "/{*path}";

                endpoints.Map(pattern, (Microsoft.AspNetCore.Http.RequestDelegate)((Microsoft.AspNetCore.Http.HttpContext ctx) => {
                  try {
                    // Find the mount to get its FileProvider.
                    MountRegistration reg = FindMount(cfg.RequestPath);
                    if (reg == null) {
                      ctx.Response.StatusCode = 404;
                      return System.Threading.Tasks.Task.CompletedTask;
                    }

                    // Resolve the SPA default file from the mount's provider.
                    Microsoft.Extensions.FileProviders.IFileInfo fileInfo = reg.FileProvider.GetFileInfo(cfg.DefaultDocument);
                    if (fileInfo == null || !fileInfo.Exists) {
                      // If the index is missing, return 404 to avoid loops.
                      ctx.Response.StatusCode = 404;
                      return System.Threading.Tasks.Task.CompletedTask;
                    }

                    // Content-Type detection (defaults to text/html if unknown).
                    string contentType = "text/html";
                    Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ctp = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
                    string name = fileInfo.Name;
                    string ext = System.IO.Path.GetExtension(name);
                    string detected;
                    if (!string.IsNullOrEmpty(ext)) {
                      if (ctp.TryGetContentType(name, out detected)) {
                        contentType = detected;
                      }
                    }
                    ctx.Response.ContentType = contentType;
                    ctx.Response.StatusCode = 200;

                    // Stream the file.
                    using (System.IO.Stream stream = fileInfo.CreateReadStream()) {
                      return stream.CopyToAsync(ctx.Response.Body);
                    }
                  }
                  catch (Exception ex) {
                    DevLogger.LogCritical(ex);
                    ctx.Response.StatusCode = 500;
                    return System.Threading.Tasks.Task.CompletedTask;
                  }
                }));

                DevLogger.LogTrace(0, 99999, "Mapped SPA fallback " + pattern + " -> " + cfg.DefaultDocument + " (served from mount provider)");
              }
            }
          });
        }


        DevLogger.LogTrace(0, 99999, "StaticFileConsolidatorIApp applied all registrations.");
      }
      catch (Exception ex) {
        DevLogger.LogCritical(ex);
        throw;
      }
    }

    // Helper to find a mount by normalized path.
    private MountRegistration FindMount(string normalizedMount) {
      foreach (MountRegistration m in _Mounts) {
        if (string.Equals(m.RequestPath, normalizedMount, StringComparison.Ordinal)) {
          return m;
        }
      }
      return null;
    }

    /// <summary>
    /// Build StaticFileOptions for a mount.
    /// </summary>
    private StaticFileOptions BuildStaticFileOptions(MountRegistration reg) {
      StaticFileOptions options = new StaticFileOptions();
      options.RequestPath = reg.RequestPath;
      options.FileProvider = reg.FileProvider;
      return options;
    }

    /// <summary>
    /// Build DefaultFilesOptions for a mount, honoring its specific default document.
    /// </summary>
    private DefaultFilesOptions BuildDefaultFilesOptions(MountRegistration reg, DefaultDocumentConfig cfg) {
      DefaultFilesOptions options = new DefaultFilesOptions();
      options.RequestPath = reg.RequestPath;
      options.FileProvider = reg.FileProvider;
      options.DefaultFileNames.Clear();
      options.DefaultFileNames.Add(cfg.DefaultDocument);
      return options;
    }

    /// <summary>
    /// True if at least one SPA fallback is configured.
    /// </summary>
    private bool HasSpaFallbacks() {
      foreach (DefaultDocumentConfig cfg in _Defaults) {
        if (cfg.IsSpa) {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Find default config for an exact mount path.
    /// </summary>
    private DefaultDocumentConfig FindDefault(string normalizedMount) {
      foreach (DefaultDocumentConfig cfg in _Defaults) {
        if (string.Equals(cfg.RequestPath, normalizedMount, StringComparison.Ordinal)) {
          return cfg;
        }
      }
      return null;
    }

    /// <summary>
    /// Checks whether a mount is already registered.
    /// </summary>
    private bool ContainsMount(string normalizedMount) {
      foreach (MountRegistration m in _Mounts) {
        if (string.Equals(m.RequestPath, normalizedMount, StringComparison.Ordinal)) {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Normalize any user-provided path to "/name" form (leading slash, no trailing slash).
    /// </summary>
    private static string NormalizeMount(string requestPath) {
      if (string.IsNullOrWhiteSpace(requestPath)) {
        throw new ArgumentException("requestPath must not be null or empty.", nameof(requestPath));
      }

      string trimmed = requestPath.Trim();
      trimmed = trimmed.Replace('\\', '/');

      if (!trimmed.StartsWith("/", StringComparison.Ordinal)) {
        trimmed = "/" + trimmed;
      }

      if (trimmed.Length > 1) {
        if (trimmed.EndsWith("/", StringComparison.Ordinal)) {
          trimmed = trimmed.Substring(0, trimmed.Length - 1);
        }
      }

      return trimmed;
    }

  }


  public class EmbeddedBundleFileProvider : IFileProvider {

    private string _WebAppBaseUrl;
    private string _SourceInfoString;
    private EmbeddedFileProvider _InnerProvider;

    /// <summary></summary>
    /// <param name="webAppBaseUrl"></param>
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
      throw new System.NotImplementedException();
    }

    private class WrappedFileInfo : IFileInfo {

      private IFileInfo _InnerFileInfo;
      private string _WebAppBaseUrl;
      private string _SourceInfoString;

      public WrappedFileInfo(IFileInfo innerFileInfo, string webAppBaseUrl, string sourceInfoString) {
        _InnerFileInfo = innerFileInfo;
        _WebAppBaseUrl = webAppBaseUrl;
        _SourceInfoString = sourceInfoString;
      }

      public bool Exists => _InnerFileInfo.Exists;
      public string PhysicalPath => _InnerFileInfo.PhysicalPath;
      public string Name => _InnerFileInfo.Name;
      public DateTimeOffset LastModified => _InnerFileInfo.LastModified;
      public bool IsDirectory => _InnerFileInfo.IsDirectory;

      public long Length {
        get {
          if (this.Name.EndsWith(".html")) {
            return this.GetModifiedContent().Length;
          }
          return _InnerFileInfo.Length;
        }
      }

      public Stream CreateReadStream() {

        if (this.Name.EndsWith(".html")) {

          MemoryStream ms = new MemoryStream(Convert.ToInt32(this.Length));
          StreamWriter sw = new StreamWriter(ms);
          sw.Write(this.GetModifiedContent());
          sw.Flush();
          ms.Position = 0;

          return ms;
        }

        return _InnerFileInfo.CreateReadStream();
      }

      private StringBuilder _ModifiedContent = null;

      /// <summary>
      /// Man-in-the-middle, um den HTML-Content (speziell den Base-Path) anzupassen!
      /// </summary>
      /// <returns></returns>
      private StringBuilder GetModifiedContent() {

        if (_ModifiedContent == null) {

          using (Stream stream = _InnerFileInfo.CreateReadStream()) {

            _ModifiedContent = new StringBuilder(Convert.ToInt32(stream.Length + 200));

            StreamReader sr = new StreamReader(stream);

            _ModifiedContent.Append(
              sr.ReadToEnd()
            );

            _ModifiedContent.Replace("<html>", $"<html>\n  <!-- from '{_SourceInfoString}' -->");
            _ModifiedContent.Replace("<base href=\"/\" />", $"<base href=\"{_WebAppBaseUrl}\" />");
            _ModifiedContent.Replace("<base href=\"/\"/>", $"<base href=\"{_WebAppBaseUrl}\"/>");

          }

        }
        return _ModifiedContent;
      }

    }

  }

}
