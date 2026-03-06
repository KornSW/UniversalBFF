using System;
//using System.IO.Abstraction;
using System.Reflection;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  public interface IFrontendModuleRegistrar {

    void RegisterModule(ModuleDescription moduleDescription);

    /// <summary></summary>
    /// <param name="moduleScopingKey">An technical name (URL-SAFE!) to discriminate application modules from each other.</param>
    /// <param name="extensionAlias">An technical name (URL-SAFE!), used as alias to address this extension!</param>
    /// <param name="assemblyWithEmbeddedFiles"></param>
    /// <param name="embeddedFilesNamespace">
    /// WARNING: Errors here are very hard to track, because simply no file is returned!
    ///  * basically {DefaultNamespaceOfTheProject}.{Folder}
    ///  * Folder separators are dots (.)
    ///  * Case-Sensitive
    ///  * Replace from '-' to '_', ' ' to '_'
    /// </param>
    /// <param name="defaultDoc"></param> 
    /// <exception cref="InvalidOperationException"></exception>
    void RegisterFrontendExtension(string moduleScopingKey, Assembly assemblyWithEmbeddedFiles, string embeddedFilesNamespace, string extensionAlias = "ui", string defaultDoc = "index.html");

    /// <summary></summary>
    /// <param name="moduleScopingKey"></param>
    /// <param name="extensionAlias"></param>
    /// <param name="externalHostedUrl">usually the location where the remote-entrypoint for javascript is loacted</param>
    void RegisterFrontendExtension(string moduleScopingKey, string externalHostedUrl, string extensionAlias = "ui");

    ///// <summary>
    ///// Registers an UShell Module Application
    ///// </summary>
    //void RegisterFrontendExtension(string endpointAlias, IAfsRepository staticFilesForHosting);

  }

}
