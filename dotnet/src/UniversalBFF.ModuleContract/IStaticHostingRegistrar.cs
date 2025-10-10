using System;
using System.Reflection;

namespace UniversalBFF {

  /// <summary>
  /// Registrar interface exposed to the caller for registering mounts and defaults.
  /// </summary>
  public interface IStaticHostingRegistrar {

    /// <summary></summary>
    /// <param name="applicationBase"> usually just '/' (first and last char must be a slash!)</param>
    /// <param name="mountPointPathRelativeToApplicationBase"> for example 'ui/spaX/' </param>
    /// <param name="assemblyWithEmbeddedFiles"></param>
    /// <param name="embeddedFilesNamespace">
    /// WARNING: Errors here are very hard to track, because simply no file is returned!
    ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
    ///  * Folder separators are dots (.)
    ///  * Case-Sensitive
    ///  * Replace from '-' to '_', ' ' to '_'
    /// </param>
    /// <exception cref="InvalidOperationException"></exception>
    public void Register(
      string applicationBase, string mountPointPathRelativeToApplicationBase, 
      Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace
    );

    /// <summary>
    /// Set the default document for a mount and whether it is a SPA (deep-link fallback).
    /// </summary>
    void SetDefaultDoc(string requestPathRelativeToApplicationBase, string defaultDocument, bool spa);

  }

}
