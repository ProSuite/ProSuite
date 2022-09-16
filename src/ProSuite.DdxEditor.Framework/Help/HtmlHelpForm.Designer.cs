namespace ProSuite.DdxEditor.Framework.Help
{
	partial class HtmlHelpForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._webView = new Microsoft.Web.WebView2.WinForms.WebView2();
			((System.ComponentModel.ISupportInitialize)(this._webView)).BeginInit();
			this.SuspendLayout();
			// 
			// _webView
			// 
			this._webView.AllowExternalDrop = true;
			this._webView.CreationProperties = null;
			this._webView.DefaultBackgroundColor = System.Drawing.Color.White;
			this._webView.Dock = System.Windows.Forms.DockStyle.Fill;
			this._webView.Location = new System.Drawing.Point(0, 0);
			this._webView.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this._webView.Name = "_webView";
			this._webView.Size = new System.Drawing.Size(1143, 750);
			this._webView.TabIndex = 0;
			this._webView.ZoomFactor = 1D;
			// 
			// HtmlHelpForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1143, 750);
			this.Controls.Add(this._webView);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "HtmlHelpForm";
			this.Text = "Instance Descriptor";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.HtmlHelpForm_FormClosed);
			this.Load += new System.EventHandler(this.HtmlDocumentationForm_Load);
			((System.ComponentModel.ISupportInitialize)(this._webView)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private Microsoft.Web.WebView2.WinForms.WebView2 _webView;
	}
}
