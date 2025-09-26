using System;
using System.Reflection;

namespace UniversalBFF {

  /// <summary>
  /// Registrar interface exposed to the caller for registering mounts and defaults.
  /// </summary>
  public interface IStaticHostingRegistrar {

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
    public void Register(string requestPath, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace);

    /// <summary>
    /// Set the default document for a mount and whether it is a SPA (deep-link fallback).
    /// </summary>
    void SetDefaultDoc(string requestPath, string defaultDocument, bool spa);

  }

}
