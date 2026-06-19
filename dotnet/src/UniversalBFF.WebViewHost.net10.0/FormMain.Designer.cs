using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace UniversalBFF {

  partial class FormMain {

    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
      webView2Control = new WebView2();
      ((System.ComponentModel.ISupportInitialize)webView2Control).BeginInit();
      this.SuspendLayout();
      // 
      // webView2Control
      // 
      webView2Control.AllowExternalDrop = true;
      webView2Control.BackColor = System.Drawing.Color.White;
      webView2Control.CreationProperties = null;
      webView2Control.DefaultBackgroundColor = System.Drawing.Color.White;
      webView2Control.Dock = DockStyle.Fill;
      webView2Control.Location = new System.Drawing.Point(0, 0);
      webView2Control.Name = "webView2Control";
      webView2Control.Size = new System.Drawing.Size(970, 673);
      webView2Control.TabIndex = 0;
      webView2Control.ZoomFactor = 1D;
      // 
      // FormMain
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(970, 673);
      this.Controls.Add(webView2Control);
      this.Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
      this.MinimumSize = new System.Drawing.Size(1100, 400);
      this.Size = new System.Drawing.Size(1100, 700);
      this.Name = "FormMain";
      this.StartPosition = FormStartPosition.CenterScreen;
      this.Text = "Universal BFF";
      this.Load += this.FormMain_Load;
      ((System.ComponentModel.ISupportInitialize)webView2Control).EndInit();
      this.ResumeLayout(false);
    }

    #endregion

    private Microsoft.Web.WebView2.WinForms.WebView2 webView2Control;

  }

}
