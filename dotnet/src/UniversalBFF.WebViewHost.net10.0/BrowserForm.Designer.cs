namespace AuthTokenHandling.TestApp {
  partial class BrowserForm {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      components = new System.ComponentModel.Container();
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BrowserForm));
      chromiumWebBrowser1 = new CefSharp.WinForms.ChromiumWebBrowser();
      timer1 = new System.Windows.Forms.Timer(components);
      txtUrl = new System.Windows.Forms.TextBox();
      this.SuspendLayout();
      // 
      // chromiumWebBrowser1
      // 
      chromiumWebBrowser1.ActivateBrowserOnCreation = false;
      chromiumWebBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
      chromiumWebBrowser1.Location = new System.Drawing.Point(0, 20);
      chromiumWebBrowser1.Name = "chromiumWebBrowser1";
      chromiumWebBrowser1.Size = new System.Drawing.Size(800, 430);
      chromiumWebBrowser1.TabIndex = 0;
      // 
      // timer1
      // 
      timer1.Interval = 1000;
      // 
      // txtUrl
      // 
      txtUrl.BackColor = System.Drawing.SystemColors.Window;
      txtUrl.Dock = System.Windows.Forms.DockStyle.Top;
      txtUrl.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
      txtUrl.Location = new System.Drawing.Point(0, 0);
      txtUrl.Name = "txtUrl";
      txtUrl.ReadOnly = true;
      txtUrl.Size = new System.Drawing.Size(800, 20);
      txtUrl.TabIndex = 1;
      txtUrl.Text = "https://www.google.de";
      txtUrl.TextChanged += this.txtUrl_TextChanged;
      // 
      // BrowserForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.White;
      this.ClientSize = new System.Drawing.Size(800, 450);
      this.Controls.Add(chromiumWebBrowser1);
      this.Controls.Add(txtUrl);
      this.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
      this.Name = "BrowserForm";
      this.Text = "Logon-Window (Browser)";
      this.Load += this.BrowserForm_Load;
      this.ResumeLayout(false);
      this.PerformLayout();
    }

    #endregion

    private CefSharp.WinForms.ChromiumWebBrowser chromiumWebBrowser1;
    private System.Windows.Forms.Timer timer1;
    private System.Windows.Forms.TextBox txtUrl;
  }
}