namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	partial class InstanceParameterConfigControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._panelMain = new System.Windows.Forms.Panel();
			this._panelParametersEdit = new System.Windows.Forms.Panel();
			this._panelParametersTop = new System.Windows.Forms.Panel();
			this._linkDocumentation = new System.Windows.Forms.LinkLabel();
			this._panelMain.SuspendLayout();
			this._panelParametersTop.SuspendLayout();
			this.SuspendLayout();
			// 
			// _panelMain
			// 
			this._panelMain.Controls.Add(this._panelParametersEdit);
			this._panelMain.Controls.Add(this._panelParametersTop);
			this._panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelMain.Location = new System.Drawing.Point(0, 0);
			this._panelMain.Name = "_panelMain";
			this._panelMain.Size = new System.Drawing.Size(488, 316);
			this._panelMain.TabIndex = 0;
			// 
			// _panelParametersEdit
			// 
			this._panelParametersEdit.BackColor = System.Drawing.Color.Transparent;
			this._panelParametersEdit.Dock = System.Windows.Forms.DockStyle.Fill;
			this._panelParametersEdit.Location = new System.Drawing.Point(0, 27);
			this._panelParametersEdit.Name = "_panelParametersEdit";
			this._panelParametersEdit.Size = new System.Drawing.Size(488, 289);
			this._panelParametersEdit.TabIndex = 3;
			// 
			// _panelParametersTop
			// 
			this._panelParametersTop.BackColor = System.Drawing.Color.Transparent;
			this._panelParametersTop.Controls.Add(this._linkDocumentation);
			this._panelParametersTop.Dock = System.Windows.Forms.DockStyle.Top;
			this._panelParametersTop.Location = new System.Drawing.Point(0, 0);
			this._panelParametersTop.Name = "_panelParametersTop";
			this._panelParametersTop.Size = new System.Drawing.Size(488, 27);
			this._panelParametersTop.TabIndex = 2;
			// 
			// _linkDocumentation
			// 
			this._linkDocumentation.AutoSize = true;
			this._linkDocumentation.Location = new System.Drawing.Point(6, 9);
			this._linkDocumentation.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
			this._linkDocumentation.Name = "_linkDocumentation";
			this._linkDocumentation.Size = new System.Drawing.Size(179, 15);
			this._linkDocumentation.TabIndex = 23;
			this._linkDocumentation.TabStop = true;
			this._linkDocumentation.Text = "Show Parameter Documentation";
			this._linkDocumentation.TextAlign = System.Drawing.ContentAlignment.TopRight;
			this._linkDocumentation.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._linkDocumentation_LinkClicked);
			// 
			// InstanceParameterConfigControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._panelMain);
			this.Name = "InstanceParameterConfigControl";
			this.Size = new System.Drawing.Size(488, 316);
			this._panelMain.ResumeLayout(false);
			this._panelParametersTop.ResumeLayout(false);
			this._panelParametersTop.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel _panelMain;
		private System.Windows.Forms.Panel _panelParametersEdit;
		private System.Windows.Forms.Panel _panelParametersTop;
		private System.Windows.Forms.LinkLabel _linkDocumentation;
	}
}
