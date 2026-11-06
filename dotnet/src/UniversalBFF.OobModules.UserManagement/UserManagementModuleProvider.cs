using Logging.SmartStandards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniversalBFF.OobModules.UserManagement.Frontend.Contract;
using UShell;

[assembly: AssemblyMetadata("SourceContext", "UserManagementModule")]

namespace UniversalBFF.OobModules.UserManagement {

  public class UserManagementModuleProvider : IFrontendModuleProvider, IBackendServiceProvider {

    /// <summary>
    /// An technical name (URL-SAFE!) to discriminate application modules from each other.
    /// </summary>
    internal const string ModuleScopingKey = "oob-usrmgmt";

    //Folderstructure within Project, where EMBEDDED files are located ('-' will be '_')
    private const string EmbeddedWebappFilesNamespace = "UniversalBFF.OobModules.UserManagement.Frontend.webapp_files";

    public string ModuleKey {
      get { return ModuleScopingKey; }
    }
    public string ModuleVersion {
      get { return Assembly.GetExecutingAssembly().GetName().Version.ToString(3); }
    }

    public void RegisterServices(IBackendServiceRegistrar registrar, BackendInitializationContext context) {

      //OAuth ist ja in der basis da, das einlinken der custom BL aus diesem modul, welche
      //während des logins benutzt wird wird über andere hooks eingehangen...

      //the tokens issued by this BFF must be accepted by the AccessTokenValidator (core)
      BffUserService.RegisterTokenSourceAtAccessTokenManager();

      //es folgen nur die "Management" services, welche z.B. user anlegen, rollen zuweisen etc können

      registrar.RegisterUjmwServiceEndpoint<IUserManagementService>(
        ModuleScopingKey, nameof(UserManagementService), () => new UserManagementService()
      );

      registrar.RegisterUjmwServiceEndpoint<ILocalCredentialManagementService>(
        ModuleScopingKey, nameof(LocalCredentialService), () => new LocalCredentialService()
      );

      //the FUSE store, which is consumed by the datastore declared in the 'module.json'
      ManagementStore.RegisterTo(registrar, ModuleScopingKey);

    }

    public void RegisterModule(IFrontendModuleRegistrar registrar, FrontendInitializationContext context) {

      //the module description is an artifact of the web project (js/UniversalBFF.OobModules.UserManagement/src/module.json)
      string moduleJsonResourceName = $"{EmbeddedWebappFilesNamespace}.module.json";
      if (Assembly.GetExecutingAssembly().GetManifestResourceInfo(moduleJsonResourceName) == null) {
        //the web project was not built before this assembly (the backend services are still available)
        DevLogger.LogError(
          $"The embedded '{moduleJsonResourceName}' is missing - the UI of the module '{ModuleScopingKey}' will not be available! " +
          "Build the web project 'js/UniversalBFF.OobModules.UserManagement' before this assembly."
        );
        return;
      }
      ModuleDescription moduleDesc = ModuleDescription.FromEmbeddedResFile(
        moduleJsonResourceName, Assembly.GetExecutingAssembly()
      );

      ModuleDescriptionNormalizer.ConvertJTokensToPlainObjects(moduleDesc);

      //only host-bound values are supplemented here (see 'ushell-hosting' M-3)
      moduleDesc.ModuleScopingKey = ModuleScopingKey;
      ResolveRelativeDatastoreUrls(moduleDesc, ModuleScopingKey);

      registrar.RegisterModule(moduleDesc);
      lock (_SyncRoot) {
        _RegisteredModuleDescription = moduleDesc;
        ApplyPrimaryUiTokenSource();
      }

      //////////////////////// MODULE BUNDLE (FEDERATED WIDGETS) ////////////////////////

      registrar.RegisterFrontendExtension(
        ModuleScopingKey,
        Assembly.GetExecutingAssembly(),
        $"{EmbeddedWebappFilesNamespace}.ui"
      );

      //registrar.RegisterModule(
      //  new UShell.ModuleDescription() {
      //     Commands = new List<UShell.CommandDescription> {
      //       new ServerCommandDescription
      //     }
      //  }
      //);

      //var commands = new CommandExecutor();
      //commands.RegisterCommand(
      //  "Foo",
      //  () => {
      //
      //  }
      //);
      //registrar.RegisterServerCommands(commands);

    }

    /// <summary>
    /// Inside of the 'module.json', urls are relative to the module (as defined by 'ushell-hosting' M-4).
    /// The shell currently resolves only federated widget urls that way - but datastore urls
    /// relative to the document (see 'ushell-design-debt' DD-07). Until this is fixed in the shell,
    /// we resolve them here against the module base url (which is a host-bound value).
    /// </summary>
    private static void ResolveRelativeDatastoreUrls(ModuleDescription moduleDesc, string moduleScopingKey) {
      if (moduleDesc.Datastores == null) {
        return;
      }
      foreach (DatastoreDescription datastore in moduleDesc.Datastores) {
        IDictionary<string, object> providerArguments = datastore.ProviderArguments as IDictionary<string, object>;
        if (providerArguments == null) {
          continue;
        }
        foreach (string urlArgumentName in new string[] { "url", "entitySchemaUrl" }) {
          if (!providerArguments.TryGetValue(urlArgumentName, out object rawUrl)) {
            continue;
          }
          string url = rawUrl as string;
          if (IsModuleRelativeUrl(url)) {
            providerArguments[urlArgumentName] = $"{moduleScopingKey}/{url}";
          }
        }
      }
    }

    #region " Token Source Assignment (host-bound) "

    private static object _SyncRoot = new object();
    private static ModuleDescription _RegisteredModuleDescription = null;
    private static string _PrimaryUiTokenSourceId = null;

    /// <summary>
    /// The token source ids are generated per installation (they are derived from the 'OAuthProxyTargets'),
    /// so they cannot be part of the 'module.json'. Token source assignments are host-bound values
    /// (see 'ushell-hosting' M-3): every datastore without an own 'tokenSourceUid' gets the
    /// first token source, which is available for the primary UI logon.
    /// LIMITATION: if there are multiple primary UI token sources, a user who logged on via another one
    /// will have no token for the datastore (this needs the central frontend AuthTokenManager, UB-26).
    /// </summary>
    internal static void AnnouncePrimaryUiTokenSource(string tokenSourceId) {
      lock (_SyncRoot) {
        if (_PrimaryUiTokenSourceId != null) {
          return;
        }
        _PrimaryUiTokenSourceId = tokenSourceId;
        ApplyPrimaryUiTokenSource();
      }
    }

    private static void ApplyPrimaryUiTokenSource() {
      if (_RegisteredModuleDescription == null || _PrimaryUiTokenSourceId == null) {
        return;
      }
      foreach (DatastoreDescription datastore in _RegisteredModuleDescription.Datastores ?? Enumerable.Empty<DatastoreDescription>()) {
        IDictionary<string, object> providerArguments = datastore.ProviderArguments as IDictionary<string, object>;
        if (providerArguments == null) {
          continue;
        }
        if (providerArguments.TryGetValue("tokenSourceUid", out object current) && !string.IsNullOrWhiteSpace(current as string)) {
          continue;
        }
        providerArguments["tokenSourceUid"] = _PrimaryUiTokenSourceId;
      }
    }

    #endregion

    private static bool IsModuleRelativeUrl(string url) {
      if (string.IsNullOrWhiteSpace(url)) {
        return false;
      }
      if (url.StartsWith("/") || url.Contains("://")) {
        return false;
      }
      return true;
    }

  }

}
