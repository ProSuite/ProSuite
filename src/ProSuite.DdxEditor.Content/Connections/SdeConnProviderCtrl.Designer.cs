namespace ProSuite.DdxEditor.Content.Connections
{
    partial class SdeConnProviderCtrl<T>
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
			this._textBoxRepositoryName = new System.Windows.Forms.TextBox();
			this._labelRepositoryName = new System.Windows.Forms.Label();
			this._textBoxVersionName = new System.Windows.Forms.TextBox();
			this._labelVersionName = new System.Windows.Forms.Label();
			this._textBoxRepositoryNameInfo = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// _textBoxRepositoryName
			// 
			this._textBoxRepositoryName.Location = new System.Drawing.Point(100, 10);
			this._textBoxRepositoryName.Name = "_textBoxRepositoryName";
			this._textBoxRepositoryName.Size = new System.Drawing.Size(298, 20);
			this._textBoxRepositoryName.TabIndex = 0;
			// 
			// _labelRepositoryName
			// 
			this._labelRepositoryName.AutoSize = true;
			this._labelRepositoryName.Location = new System.Drawing.Point(14, 13);
			this._labelRepositoryName.Name = "_labelRepositoryName";
			this._labelRepositoryName.Size = new System.Drawing.Size(80, 13);
			this._labelRepositoryName.TabIndex = 12;
			this._labelRepositoryName.Text = "SDE repository:";
			this._labelRepositoryName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxVersionName
			// 
			this._textBoxVersionName.Location = new System.Drawing.Point(100, 36);
			this._textBoxVersionName.Name = "_textBoxVersionName";
			this._textBoxVersionName.Size = new System.Drawing.Size(298, 20);
			this._textBoxVersionName.TabIndex = 1;
			// 
			// _labelVersionName
			// 
			this._labelVersionName.AutoSize = true;
			this._labelVersionName.Location = new System.Drawing.Point(20, 39);
			this._labelVersionName.Name = "_labelVersionName";
			this._labelVersionName.Size = new System.Drawing.Size(74, 13);
			this._labelVersionName.TabIndex = 12;
			this._labelVersionName.Text = "Version name:";
			this._labelVersionName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxRepositoryNameInfo
			// 
			this._textBoxRepositoryNameInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxRepositoryNameInfo.BackColor = System.Drawing.SystemColors.Info;
			this._textBoxRepositoryNameInfo.Location = new System.Drawing.Point(418, 4);
			this._textBoxRepositoryNameInfo.MaximumSize = new System.Drawing.Size(278, 38);
			this._textBoxRepositoryNameInfo.Multiline = true;
			this._textBoxRepositoryNameInfo.Name = "_textBoxRepositoryNameInfo";
			this._textBoxRepositoryNameInfo.ReadOnly = true;
			this._textBoxRepositoryNameInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxRepositoryNameInfo.Size = new System.Drawing.Size(278, 38);
			this._textBoxRepositoryNameInfo.TabIndex = 0;
			this._textBoxRepositoryNameInfo.TabStop = false;
			this._textBoxRepositoryNameInfo.Text = "Oracle: SDE schema name (usually SDE)\r\nSQL Server/PostgreSQL: database name";
			// 
			// SdeConnProviderCtrl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxVersionName);
			this.Controls.Add(this._textBoxRepositoryNameInfo);
			this.Controls.Add(this._textBoxRepositoryName);
			this.Controls.Add(this._labelVersionName);
			this.Controls.Add(this._labelRepositoryName);
			this.Name = "SdeConnProviderCtrl";
			this.Size = new System.Drawing.Size(700, 61);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _textBoxRepositoryName;
        private System.Windows.Forms.Label _labelRepositoryName;
		private System.Windows.Forms.TextBox _textBoxVersionName;
		private System.Windows.Forms.Label _labelVersionName;
		private System.Windows.Forms.TextBox _textBoxRepositoryNameInfo;
    }
}
