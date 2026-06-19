using Logging.SmartStandards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Security;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UJMW;
using System.Windows.Forms;

namespace UniversalBFF {

  public partial class FormMain : Form {

    public FormMain() {
      this.InitializeComponent();
    }

    private void FormMain_Load(object sender, EventArgs e) {

      Program._AspApplication.Attach(webView2Control);

    }

  }

}
