namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	partial class ImportQualitySpecificationsForm
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
			this._groupBoxOptions = new System.Windows.Forms.GroupBox();
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets = new System.Windows.Forms.CheckBox();
			this._checkBoxUpdateDescriptorProperties = new System.Windows.Forms.CheckBox();
			this._checkBoxUpdateDescriptorNames = new System.Windows.Forms.CheckBox();
			this._groupBoxImportFrom = new System.Windows.Forms.GroupBox();
			this._labelXmlFile = new System.Windows.Forms.Label();
			this._fileSystemPathControl = new ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl();
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this._buttonOK = new System.Windows.Forms.Button();
			this._buttonCancel = new System.Windows.Forms.Button();
			this._groupBoxOptions.SuspendLayout();
			this._groupBoxImportFrom.SuspendLayout();
			this._statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _groupBoxOptions
			// 
			this._groupBoxOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxOptions.Controls.Add(this._checkBoxIgnoreQualityConditionsForUnknownDatasets);
			this._groupBoxOptions.Controls.Add(this._checkBoxUpdateDescriptorProperties);
			this._groupBoxOptions.Controls.Add(this._checkBoxUpdateDescriptorNames);
			this._groupBoxOptions.Location = new System.Drawing.Point(14, 76);
			this._groupBoxOptions.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxOptions.Name = "_groupBoxOptions";
			this._groupBoxOptions.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxOptions.Size = new System.Drawing.Size(440, 107);
			this._groupBoxOptions.TabIndex = 1;
			this._groupBoxOptions.TabStop = false;
			this._groupBoxOptions.Text = "Options";
			// 
			// _checkBoxIgnoreQualityConditionsForUnknownDatasets
			// 
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.AutoSize = true;
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Location = new System.Drawing.Point(19, 22);
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Name = "_checkBoxIgnoreQualityConditionsForUnknownDatasets";
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Size = new System.Drawing.Size(275, 19);
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.TabIndex = 0;
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.Text = "Ignore quality conditions for unknown datasets";
			this._checkBoxIgnoreQualityConditionsForUnknownDatasets.UseVisualStyleBackColor = true;
			// 
			// _checkBoxUpdateDescriptorProperties
			// 
			this._checkBoxUpdateDescriptorProperties.AutoSize = true;
			this._checkBoxUpdateDescriptorProperties.Location = new System.Drawing.Point(19, 75);
			this._checkBoxUpdateDescriptorProperties.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxUpdateDescriptorProperties.Name = "_checkBoxUpdateDescriptorProperties";
			this._checkBoxUpdateDescriptorProperties.Size = new System.Drawing.Size(270, 19);
			this._checkBoxUpdateDescriptorProperties.TabIndex = 2;
			this._checkBoxUpdateDescriptorProperties.Text = "Update other properties of existing descriptors";
			this._checkBoxUpdateDescriptorProperties.UseVisualStyleBackColor = true;
			// 
			// _checkBoxUpdateDescriptorNames
			// 
			this._checkBoxUpdateDescriptorNames.AutoSize = true;
			this._checkBoxUpdateDescriptorNames.Location = new System.Drawing.Point(19, 48);
			this._checkBoxUpdateDescriptorNames.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._checkBoxUpdateDescriptorNames.Name = "_checkBoxUpdateDescriptorNames";
			this._checkBoxUpdateDescriptorNames.Size = new System.Drawing.Size(221, 19);
			this._checkBoxUpdateDescriptorNames.TabIndex = 1;
			this._checkBoxUpdateDescriptorNames.Text = "Update names of existing descriptors";
			this._checkBoxUpdateDescriptorNames.UseVisualStyleBackColor = true;
			// 
			// _groupBoxImportFrom
			// 
			this._groupBoxImportFrom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._groupBoxImportFrom.Controls.Add(this._labelXmlFile);
			this._groupBoxImportFrom.Controls.Add(this._fileSystemPathControl);
			this._groupBoxImportFrom.Location = new System.Drawing.Point(14, 7);
			this._groupBoxImportFrom.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxImportFrom.Name = "_groupBoxImportFrom";
			this._groupBoxImportFrom.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._groupBoxImportFrom.Size = new System.Drawing.Size(440, 62);
			this._groupBoxImportFrom.TabIndex = 0;
			this._groupBoxImportFrom.TabStop = false;
			this._groupBoxImportFrom.Text = "Import from";
			// 
			// _labelXmlFile
			// 
			this._labelXmlFile.AutoSize = true;
			this._labelXmlFile.Location = new System.Drawing.Point(15, 28);
			this._labelXmlFile.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this._labelXmlFile.Name = "_labelXmlFile";
			this._labelXmlFile.Size = new System.Drawing.Size(28, 15);
			this._labelXmlFile.TabIndex = 1;
			this._labelXmlFile.Text = "File:";
			// 
			// _fileSystemPathControl
			// 
			this._fileSystemPathControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._fileSystemPathControl.ControlPathType = ProSuite.Commons.UI.WinForms.Controls.FileSystemPathType.ChooseFileName;
			this._fileSystemPathControl.FileCheckFileExists = false;
			this._fileSystemPathControl.FileCheckPathExists = true;
			this._fileSystemPathControl.FileDefaultExtension = null;
			this._fileSystemPathControl.FileFilter = null;
			this._fileSystemPathControl.FolderGroupTitle = "Choose Folder";
			this._fileSystemPathControl.FolderShowNewFolderButton = true;
			this._fileSystemPathControl.Location = new System.Drawing.Point(52, 21);
			this._fileSystemPathControl.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._fileSystemPathControl.Name = "_fileSystemPathControl";
			this._fileSystemPathControl.Size = new System.Drawing.Size(380, 30);
			this._fileSystemPathControl.TabIndex = 0;
			this._fileSystemPathControl.ValueChanged += new System.EventHandler(this._fileSystemPathControl_ValueChanged);
			// 
			// _statusStrip
			// 
			this._statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._toolStripStatusLabel});
			this._statusStrip.Location = new System.Drawing.Point(0, 226);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
			this._statusStrip.Size = new System.Drawing.Size(468, 22);
			this._statusStrip.TabIndex = 10;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _toolStripStatusLabel
			// 
			this._toolStripStatusLabel.Name = "_toolStripStatusLabel";
			this._toolStripStatusLabel.Size = new System.Drawing.Size(54, 17);
			this._toolStripStatusLabel.Text = "<status>";
			// 
			// _buttonOK
			// 
			this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonOK.Location = new System.Drawing.Point(272, 190);
			this._buttonOK.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonOK.Name = "_buttonOK";
			this._buttonOK.Size = new System.Drawing.Size(88, 27);
			this._buttonOK.TabIndex = 2;
			this._buttonOK.Text = "OK";
			this._buttonOK.UseVisualStyleBackColor = true;
			this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
			// 
			// _buttonCancel
			// 
			this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._buttonCancel.Location = new System.Drawing.Point(366, 190);
			this._buttonCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this._buttonCancel.Name = "_buttonCancel";
			this._buttonCancel.Size = new System.Drawing.Size(88, 27);
			this._buttonCancel.TabIndex = 3;
			this._buttonCancel.Text = "Cancel";
			this._buttonCancel.UseVisualStyleBackColor = true;
			this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
			// 
			// ImportQualitySpecificationsForm
			// 
			this.AcceptButton = this._buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._buttonCancel;
			this.ClientSize = new System.Drawing.Size(468, 248);
			this.Controls.Add(this._groupBoxOptions);
			this.Controls.Add(this._groupBoxImportFrom);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._buttonOK);
			this.Controls.Add(this._buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.MaximumSize = new System.Drawing.Size(11664, 337);
			this.MinimumSize = new System.Drawing.Size(382, 287);
			this.Name = "ImportQualitySpecificationsForm";
			this.ShowInTaskbar = false;
			this.Text = "Import Quality Specifications";
			this._groupBoxOptions.ResumeLayout(false);
			this._groupBoxOptions.PerformLayout();
			this._groupBoxImportFrom.ResumeLayout(false);
			this._groupBoxImportFrom.PerformLayout();
			this._statusStrip.ResumeLayout(false);
			this._statusStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox _groupBoxOptions;
		private System.Windows.Forms.CheckBox _checkBoxUpdateDescriptorNames;
		private System.Windows.Forms.GroupBox _groupBoxImportFrom;
		private System.Windows.Forms.Label _labelXmlFile;
		private global::ProSuite.Commons.UI.WinForms.Controls.FileSystemPathControl _fileSystemPathControl;
		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel _toolStripStatusLabel;
		private System.Windows.Forms.Button _buttonOK;
		private System.Windows.Forms.Button _buttonCancel;
		private System.Windows.Forms.CheckBox _checkBoxIgnoreQualityConditionsForUnknownDatasets;
		private System.Windows.Forms.CheckBox _checkBoxUpdateDescriptorProperties;
	}
}
