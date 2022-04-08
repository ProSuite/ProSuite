namespace ProSuite.DdxEditor.Content.Connections
{
    partial class SdeDirectConnProviderCtrl<T>
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
			this._labelDatabaseType = new System.Windows.Forms.Label();
			this._textBoxDatabaseName = new System.Windows.Forms.TextBox();
			this._labelDatabaseName = new System.Windows.Forms.Label();
			this._comboBoxDatabaseType = new System.Windows.Forms.ComboBox();
			this._textBoxRepositoryNameInfo = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// _labelDatabaseType
			// 
			this._labelDatabaseType.AutoSize = true;
			this._labelDatabaseType.Location = new System.Drawing.Point(15, 12);
			this._labelDatabaseType.Name = "_labelDatabaseType";
			this._labelDatabaseType.Size = new System.Drawing.Size(79, 13);
			this._labelDatabaseType.TabIndex = 12;
			this._labelDatabaseType.Text = "Database type:";
			this._labelDatabaseType.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _textBoxDatabaseName
			// 
			this._textBoxDatabaseName.Location = new System.Drawing.Point(100, 36);
			this._textBoxDatabaseName.Name = "_textBoxDatabaseName";
			this._textBoxDatabaseName.Size = new System.Drawing.Size(298, 20);
			this._textBoxDatabaseName.TabIndex = 1;
			// 
			// _labelDatabaseName
			// 
			this._labelDatabaseName.AutoSize = true;
			this._labelDatabaseName.Location = new System.Drawing.Point(43, 39);
			this._labelDatabaseName.Name = "_labelDatabaseName";
			this._labelDatabaseName.Size = new System.Drawing.Size(51, 13);
			this._labelDatabaseName.TabIndex = 10;
			this._labelDatabaseName.Text = "Instance:";
			this._labelDatabaseName.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// _comboBoxDatabaseType
			// 
			this._comboBoxDatabaseType.FormattingEnabled = true;
			this._comboBoxDatabaseType.Location = new System.Drawing.Point(100, 9);
			this._comboBoxDatabaseType.Name = "_comboBoxDatabaseType";
			this._comboBoxDatabaseType.Size = new System.Drawing.Size(298, 21);
			this._comboBoxDatabaseType.TabIndex = 0;
			// 
			// _textBoxRepositoryNameInfo
			// 
			this._textBoxRepositoryNameInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._textBoxRepositoryNameInfo.BackColor = System.Drawing.SystemColors.Info;
			this._textBoxRepositoryNameInfo.Location = new System.Drawing.Point(418, 31);
			this._textBoxRepositoryNameInfo.MaximumSize = new System.Drawing.Size(278, 38);
			this._textBoxRepositoryNameInfo.Multiline = true;
			this._textBoxRepositoryNameInfo.Name = "_textBoxRepositoryNameInfo";
			this._textBoxRepositoryNameInfo.ReadOnly = true;
			this._textBoxRepositoryNameInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this._textBoxRepositoryNameInfo.Size = new System.Drawing.Size(278, 38);
			this._textBoxRepositoryNameInfo.TabIndex = 13;
			this._textBoxRepositoryNameInfo.TabStop = false;
			this._textBoxRepositoryNameInfo.Text = "Oracle: database name\r\nSQL Server/PostgreSQL: server instance name";
			// 
			// SdeDirectConnProviderCtrl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._textBoxRepositoryNameInfo);
			this.Controls.Add(this._labelDatabaseType);
			this.Controls.Add(this._textBoxDatabaseName);
			this.Controls.Add(this._labelDatabaseName);
			this.Controls.Add(this._comboBoxDatabaseType);
			this.Name = "SdeDirectConnProviderCtrl";
			this.Size = new System.Drawing.Size(700, 75);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labelDatabaseType;
        private System.Windows.Forms.TextBox _textBoxDatabaseName;
        private System.Windows.Forms.Label _labelDatabaseName;
        private System.Windows.Forms.ComboBox _comboBoxDatabaseType;
		private System.Windows.Forms.TextBox _textBoxRepositoryNameInfo;
    }
}
