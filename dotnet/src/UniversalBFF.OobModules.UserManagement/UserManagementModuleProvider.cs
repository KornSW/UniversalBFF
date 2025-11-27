using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UniversalBFF.OobModules.UserManagement.Frontend.Contract;
using UShell;
using UShell.ServerCommands;

[assembly: AssemblyMetadata("SourceContext", "UserManagementModule")]

namespace UniversalBFF.OobModules.UserManagement {

    public class UserManagementModuleProvider : IFrontendModuleProvider, IBackendServiceProvider {

    public void RegisterServices(IBackendServiceRegistrar registrar) {

      //OAuth ist ja in der basis da, das einlinken der custom BL aus diesem modul, welche
      //während des logins benutzt wird wird über andere hooks eingehangen...

      //es folgen nur die "Management" services, welche z.B. user anlegen, rollen zuweisen etc können

      registrar.RegisterUjmwServiceEndpoint<IUserManagementService>("oob-usrmgmt", () => new UserManagementService());


      registrar.RegisterUjmwServiceEndpoint<ILocalCredentialManagementService>("oob-usrmgmt", () => new LocalCredentialService());


    }

    public void RegisterModule(IFrontendModuleRegistrar registrar) {

      var module = ModuleDescription.Build("Demo");

      string workspaceKey1 = module.AddWorkspace("Workspace 1", (w) => {
      });
      string workspaceKey2 = module.AddWorkspace("Workspace 2", (w) => {
      });

      string usecaseKey1 = module.AddUsecase("UseCase 1", (u) => {
        u.WidgetClass = "";
        //u.UnitOfWorkDefaults["Abc"] = "Hallo";
      });





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
        "oob-usrmgmt",
        Assembly.GetExecutingAssembly(),
        $"{defaultNamespace}.{subNamespaceOfEmbeddedSpaFiles}",
        "index.html"
      );

    }

  }

}
