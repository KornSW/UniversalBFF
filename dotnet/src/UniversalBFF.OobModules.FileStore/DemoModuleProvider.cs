//using System;
//using System.Collections.Generic;
//using System.Text;
//using UShell;
//using UShell.ServerCommands;

//namespace UniversalBFF.Demo {

//  public class DemoModuleProvider : IFrontendModuleProvider, IBackendServiceProvider {
   
//    public void RegisterServices(IBackendServiceRegistrar registrar) {

//    }

//    public void RegisterModule(IFrontendModuleRegistrar registrar) {

//      var module = ModuleDescription.Build("Demo");

//      string workspaceKey1 = module.AddWorkspace("Workspace 1", (w) => {
//      });
//      string workspaceKey2 = module.AddWorkspace("Workspace 2", (w) => {
//      });

//      string usecaseKey1 = module.AddUsecase("UseCase 1", (u) => {
//        u.WidgetClass = "";
//        u.UnitOfWorkDefaults["Abc"] = "Hallo";
//      });





//      //registrar.RegisterModule(
//      //  new UShell.ModuleDescription() {
//      //     Commands = new List<UShell.CommandDescription> {
//      //       new ServerCommandDescription
//      //     }


//      //  }
//      //);

//      //registrar.RegisterFrontendExtension(...);

//      //registrar.RegisterBackendExtension<IDemoBackendService>(
//      //  () => new DemoBackendService()
//      //);

//      //var commands = new CommandExecutor();
//      //commands.RegisterCommand(
//      //  "Foo",
//      //  () => {

//      //  }
//      //);
//      //registrar.RegisterServerCommands(commands);


//      ////fuse-repo registireren mit Filebased-db


//      ////ServerCommands mit extra überladung







//    }

//  }

//}
