using Security.AccessTokenHandling;
using System;
using System.Collections.Generic;
using System.IO.Abstraction;
using System.Linq;
using System.SmartStandards;
using System.Text;
using UShell;
using UShell.ServerCommands;

namespace UniversalBFF {

  internal class AspModuleRegistrar : ModuleRegistrar {

    public AspModuleRegistrar(string baseUrl) : base(baseUrl) {
    }

    public override void RegisterFrontendExtension(string endpointAlias, IAfsRepository staticFilesForHosting) {
      base.RegisterFrontendExtension(endpointAlias, staticFilesForHosting);
      
      //TODO: hier AFS in MS-FileProvider wrappen!

    }
    public override void RegisterHttpProxy(string endpointAlias, string forwardingAddress) {

      throw new NotImplementedException("RegisterHttpProxy is comming soon...");

    }

    public override void RegisterUjmwServiceEndpoint(Type contractType, string endpointAlias, Func<object> factory) {

      throw new NotImplementedException("Hier fehlt noch dass der service als UJWM dynamic facade eingehangen wird");

    }

  }

}
