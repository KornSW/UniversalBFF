using System;
using System.Collections.Generic;
using System.Data.Fuse;
using System.Data.Fuse.Ef;
using System.Data.Fuse.Ef.InstanceManagement;
using System.Linq;
using System.Reflection;
using System.Text;
using UniversalBFF.OobModules.UserManagement.Frontend.Contract;
using UShell;
using UShell.ServerCommands;

[assembly: AssemblyMetadata("SourceContext", "UserManagementModule")]

namespace UniversalBFF.OobModules.UserManagement {

  public class UserManagementModuleProvider : IFrontendModuleProvider, IBackendServiceProvider {

    /// <summary>
    /// An technical name (URL-SAFE!) to discriminate application modules from each other.
    /// </summary>
    internal const string ModuleScopingKey = "oob-usrmgmt";

    public void RegisterServices(IBackendServiceRegistrar registrar) {

      //OAuth ist ja in der basis da, das einlinken der custom BL aus diesem modul, welche
      //während des logins benutzt wird wird über andere hooks eingehangen...

      //es folgen nur die "Management" services, welche z.B. user anlegen, rollen zuweisen etc können

      registrar.RegisterUjmwServiceEndpoint<IUserManagementService>(
        ModuleScopingKey, nameof(UserManagementService), () => new UserManagementService()
      );


      registrar.RegisterUjmwServiceEndpoint<ILocalCredentialManagementService>(
        ModuleScopingKey, nameof(LocalCredentialService), () => new LocalCredentialService()
      );

      registrar.RegisterUjmwServiceEndpoint<IRepository<LocalCredentialEntity, Int64>>(
        ModuleScopingKey, $"Store/LocalCredentials",
        () => new EfRepository<LocalCredentialEntity, Int64>(
          new ShortLivingDbContextInstanceProvider<UserManagementDbContext>(
            () => new UserManagementDbContext()
          )
        )
      );

    }

    public void RegisterModule(IFrontendModuleRegistrar registrar) {


      //TODO: das hier muss eigentlich per datei aus der webapp kommen!!!!

      registrar.RegisterModule(
        new ModuleDescription() {
          ModuleTitle = "User Management",
          ModuleUid = "2092362137326596206",
          ModuleScopingKey = ModuleScopingKey,
          Commands = new CommandDescription[] {
               new CommandDescription() {
                  ActionName = ModuleScopingKey + ".nav-users",
                  UniqueCommandKey = "2092355901753303639",
                  Label = "Manage Users",
                  Description ="",
                  IconKey = "fa-user",
                  CommandType = "activate-workspace",
                  TargetWorkspaceKey = ModuleScopingKey + ".Manage",
                  MenuFolder = "Administration"
               }
            }.ToList(),
          Workspaces = new WorkspaceDescription[] {
               new WorkspaceDescription() {
                 WorkspaceKey = ModuleScopingKey + ".Manage",
                 WorkspaceTitle = "Manage Users",
                 IconName = "fa-user",
                 IsSidebar = false,
                 WorkspaceAppearance = "default"
               }
            }.ToList(),
          StaticUsecaseAssignments = new StaticUsecaseAssignment[] {
            new StaticUsecaseAssignment() {
              TargetWorkspaceKey = ModuleScopingKey + ".Manage",
              UsecaseKey = ModuleScopingKey + ".Manage.LocalIdentities",
                InitUnitOfWork = new IDynamicParamObject {
                  { "Name", "LocalIdentities" },
                }
            },
            new StaticUsecaseAssignment() {
              TargetWorkspaceKey = ModuleScopingKey + ".Manage",
              UsecaseKey = ModuleScopingKey + ".Manage.OAuthClients",
                InitUnitOfWork = new IDynamicParamObject {
                  { "Name", "OAuthClients" },
                }
            }
          }.ToList(),
          Usecases = new UsecaseDescription[] {
            new UsecaseDescription() {
              Title = "Local Identities",
              UsecaseKey =  ModuleScopingKey + ".Manage.LocalIdentities",
              SingletonActionkey = ModuleScopingKey + ".Manage.LocalIdentities",
              //IconName = "dummy-icon",
              WidgetClass = "demo",
            },
            new UsecaseDescription() {
              Title = "OAuth Clients",
              UsecaseKey = ModuleScopingKey + ".Manage.OAuthClients",
              SingletonActionkey = ModuleScopingKey + ".Manage.OAuthClients",
              //IconName = "dummy-icon",
              WidgetClass = "demo",
            }
          }.ToList()
        }
      );

      //registrar.RegisterModule(
      //  new UShell.ModuleDescription() {
      //     Commands = new List<UShell.CommandDescription> {
      //       new ServerCommandDescription
      //     }


      //  }
      //);

      //registrar.RegisterFrontendExtension(...);

      //registrar.RegisterBackendExtension<IDemoBackendService>(
      //  () => new DemoBackendService()
      //);

      //var commands = new CommandExecutor();
      //commands.RegisterCommand(
      //  "Foo",
      //  () => {

      //  }
      //);
      //registrar.RegisterServerCommands(commands);


      ////fuse-repo registireren mit Filebased-db


      ////ServerCommands mit extra überladung





      //////////////////////// CUSTOM-FRONTEND-APPLICATION (SPA) ////////////////////////

      //Folderstructure within Project, where EMBEDDED files are located ('-' will be '_')
      const string subNamespaceOfEmbeddedSpaFiles = "Frontend.webapp_files";
      const string defaultNamespace = "UniversalBFF.OobModules.UserManagement";
      registrar.RegisterFrontendExtension(
        ModuleScopingKey,
        Assembly.GetExecutingAssembly(),
        $"{defaultNamespace}.{subNamespaceOfEmbeddedSpaFiles}"
      );

    }

  }

}
