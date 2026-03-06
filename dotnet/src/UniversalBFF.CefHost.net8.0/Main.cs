using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UniversalBFF {

  internal static class BffApplication {

    public static int Main(string[] commandlineArgs) {

      //using mainForm as new mainForm(commandlineArgs) {
      //  Application.Run(a);
      //}

      //cefcontronl.CefHostApplication.Initialize(commandlineArgs);

      Application.Run();

      return 0;
    }

  }

}
