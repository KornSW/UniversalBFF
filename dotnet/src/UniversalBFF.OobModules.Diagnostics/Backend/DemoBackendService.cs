using Logging.SmartStandards;
using System;

namespace UniversalBFF.Demo {

  public class DemoBackendService : IDemoBackendService {

    public void Test() {

      DevLogger.LogInformation("DemoBackendService says Test!");

    }

  }

}
