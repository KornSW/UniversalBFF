using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp.WinForms;
using CefSharp;

namespace AuthTokenHandling.TestApp {

  public partial class BrowserForm : Form {

    public BrowserForm() {
      this.SuspendLayout();
      this.InitializeComponent();
      this.Shown += this.BrowserForm_Shown;
      this.FormClosed += this.BrowserForm_FormClosed;
      this.timer1.Tick += this.Timer1_Tick;
    }

    private ChromiumWebBrowser _CefBrowser;

    private bool subscribed = false;

    private void BrowserForm_Shown(object sender, EventArgs e) {

      var wa = Screen.PrimaryScreen.WorkingArea;
      this.Top = wa.Top + 50;
      this.Left = wa.Left + 50;
      this.Width = wa.Width - 100;
      this.Height = wa.Height - 100;

      this.chromiumWebBrowser1.Dock = DockStyle.Fill;
      this.chromiumWebBrowser1.Visible = true;
      this.chromiumWebBrowser1.Show();
      this.chromiumWebBrowser1.LoadUrl(txtUrl.Text);

      if (!subscribed) {
        //this.chromiumWebBrowser1.LocationChanged += this.ChromiumWebBrowser1_LocationChanged;
        subscribed = true;
      }

      timer1.Enabled = true;
      this.ResumeLayout();
    }

    public String Url {
      get {
        return this.txtUrl.Text;
      }
      set {
        this.txtUrl.Text = value;
      }
    }
    public String ReturnOn { get; set; } = "DUMMY";

    private void BrowserForm_FormClosed(object sender, FormClosedEventArgs e) {
      if (subscribed) {
        //this.chromiumWebBrowser1.LocationChanged += this.ChromiumWebBrowser1_LocationChanged;
        subscribed = false;
      }
      timer1.Enabled = false;
    }

    private void Timer1_Tick(object sender, EventArgs e) {
      if (this.chromiumWebBrowser1.IsBrowserInitialized) {
        txtUrl.Text = this.chromiumWebBrowser1.GetBrowser().MainFrame.Url;

        if (txtUrl.Text.StartsWith(this.ReturnOn, StringComparison.InvariantCultureIgnoreCase) && txtUrl.Text.Contains("code=", StringComparison.InvariantCultureIgnoreCase)) {
          this.Close();
        }

      }
    }

    private void txtUrl_TextChanged(object sender, EventArgs e) {
    }

    private void BrowserForm_Load(object sender, EventArgs e) {

    }
  }

}
