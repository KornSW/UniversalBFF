using System;
using System.Collections.Generic;
using System.IO.Abstraction;
using System.Text;
using UShell;
using ComponentDiscovery;
using System.Reflection;
using Composition.InstanceDiscovery;
using System.Diagnostics;

[assembly: AssemblyMetadata("SourceContext", "UniversalBFF-Core")]

namespace UniversalBFF {

  [SupportsInstanceDiscovery]
  public class BffApplication {

    #region " Singleton "

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static BffApplication _Current = null;

    [ProvidesDiscoverableInstance]
    public static BffApplication Current { 
      get {
        if(_Current == null) {
          _Current = new BffApplication();
        }
        return _Current;
      }
    }
    private BffApplication() {
    }

    #endregion

    #region " TypeIndexer & AssemblyIndexer "

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private AssemblyIndexer _AssemblyIndexer = new AssemblyIndexer(
      enableResolvePathsBinding: true,
      enableAppDomainBinding: true
    );

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private TypeIndexer _TypeIndexer = null;

    public ITypeIndexer TypeIndexer {
      get {
        if(_TypeIndexer == null) {
          _TypeIndexer = new TypeIndexer(_AssemblyIndexer);
        }
        return _TypeIndexer;
      }
    }

    [ProvidesDiscoverableInstance, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static ITypeIndexer DiscoverableTypeIndexer {
      get {
        return _Current.TypeIndexer;
      }
    }

    [ProvidesDiscoverableInstance, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private static IAssemblyIndexer DiscoverableAssemblyIndexer {
      get {
        return _Current._AssemblyIndexer;
      }
    }

    #endregion

    #region " PortfolioService "

    //[DebuggerBrowsable(DebuggerBrowsableState.Never)]
    //private PortfolioService _PortfolioService = null;

    //public IPortfolioService PortfolioService {
    //  get {
    //    if (_PortfolioService == null) {
    //      _PortfolioService = new PortfolioService();
    //    }
    //    return _PortfolioService;
    //  }
    //}

    //[ProvidesDiscoverableInstance, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    //private static IPortfolioService DiscoverablePortfolioService {
    //  get {
    //    return _Current._PortfolioService;
    //  }
    //}

    #endregion 

  }

}
